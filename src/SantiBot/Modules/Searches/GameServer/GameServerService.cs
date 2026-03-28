using System.Net.Sockets;
using System.Text;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Searches;

public sealed class GameServerService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly NonBlocking.ConcurrentDictionary<int, GameServerWatch> _watches = new();

    private Timer? _updateTimer;

    public GameServerService(DbService db, DiscordSocketClient client)
    {
        _db = db;
        _client = client;
    }

    public async Task OnReadyAsync()
    {
        await using var ctx = _db.GetDbContext();
        var allWatches = await ctx.GetTable<GameServerWatch>()
            .Where(x => x.AutoUpdate)
            .ToListAsyncLinqToDB();

        foreach (var w in allWatches)
            _watches[w.Id] = w;

        _updateTimer = new Timer(async _ => await UpdateAllServersAsync(),
            null,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(5));
    }

    private async Task UpdateAllServersAsync()
    {
        foreach (var watch in _watches.Values.ToList())
        {
            try
            {
                await UpdateServerStatusAsync(watch);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error updating game server {Address}:{Port}", watch.ServerAddress, watch.ServerPort);
            }
        }
    }

    private async Task UpdateServerStatusAsync(GameServerWatch watch)
    {
        var status = await QueryServerAsync(watch.GameType, watch.ServerAddress, watch.ServerPort);

        var guild = _client.GetGuild(watch.GuildId);
        var channel = guild?.GetTextChannel(watch.ChannelId);
        if (channel is null)
            return;

        var eb = BuildStatusEmbed(watch, status);

        if (watch.StatusMessageId.HasValue)
        {
            try
            {
                var msg = await channel.GetMessageAsync(watch.StatusMessageId.Value) as IUserMessage;
                if (msg is not null)
                {
                    await msg.ModifyAsync(m => m.Embed = eb.Build());
                    return;
                }
            }
            catch
            {
                // Message was deleted, send a new one
            }
        }

        var newMsg = await channel.SendMessageAsync(embed: eb.Build());
        watch.StatusMessageId = newMsg.Id;

        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<GameServerWatch>()
            .Where(x => x.Id == watch.Id)
            .Set(x => x.StatusMessageId, newMsg.Id)
            .UpdateAsync();
    }

    public async Task<ServerStatus> QueryServerAsync(string gameType, string address, int port)
    {
        return gameType.ToLower() switch
        {
            "minecraft" => await QueryMinecraftAsync(address, port),
            _ => await QueryGenericTcpAsync(address, port)
        };
    }

    private static async Task<ServerStatus> QueryMinecraftAsync(string address, int port)
    {
        try
        {
            using var client = new TcpClient();
            client.SendTimeout = 5000;
            client.ReceiveTimeout = 5000;

            var connectTask = client.ConnectAsync(address, port);
            if (await Task.WhenAny(connectTask, Task.Delay(5000)) != connectTask)
                return new ServerStatus { IsOnline = false, ServerName = address };

            await connectTask;

            var stream = client.GetStream();

            // Minecraft Server List Ping protocol
            // Handshake packet
            using var handshake = new MemoryStream();
            WriteVarInt(handshake, 0x00); // Packet ID
            WriteVarInt(handshake, 767);   // Protocol version (1.21)
            WriteString(handshake, address);
            handshake.WriteByte((byte)(port >> 8));
            handshake.WriteByte((byte)(port & 0xFF));
            WriteVarInt(handshake, 1); // Next state: Status

            var handshakeData = handshake.ToArray();
            using var packet = new MemoryStream();
            WriteVarInt(packet, handshakeData.Length);
            packet.Write(handshakeData);
            await stream.WriteAsync(packet.ToArray());

            // Status request packet
            using var statusReq = new MemoryStream();
            WriteVarInt(statusReq, 1); // Length
            WriteVarInt(statusReq, 0x00); // Packet ID
            await stream.WriteAsync(statusReq.ToArray());

            // Read response
            var buffer = new byte[32768];
            var readTask = stream.ReadAsync(buffer, 0, buffer.Length);
            if (await Task.WhenAny(readTask, Task.Delay(5000)) != readTask)
                return new ServerStatus { IsOnline = false, ServerName = address };

            var bytesRead = await readTask;
            if (bytesRead == 0)
                return new ServerStatus { IsOnline = false, ServerName = address };

            // Parse the response - skip the packet length and packet ID VarInts
            var responseStream = new MemoryStream(buffer, 0, bytesRead);
            ReadVarInt(responseStream); // Packet length
            ReadVarInt(responseStream); // Packet ID
            var jsonLength = ReadVarInt(responseStream);

            var jsonBytes = new byte[Math.Min(jsonLength, bytesRead - (int)responseStream.Position)];
            responseStream.Read(jsonBytes, 0, jsonBytes.Length);
            var json = Encoding.UTF8.GetString(jsonBytes);

            var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            var serverName = address;
            if (root.TryGetProperty("description", out var desc))
            {
                if (desc.ValueKind == System.Text.Json.JsonValueKind.String)
                    serverName = desc.GetString() ?? address;
                else if (desc.TryGetProperty("text", out var text))
                    serverName = text.GetString() ?? address;
            }

            var online = 0;
            var max = 0;
            if (root.TryGetProperty("players", out var players))
            {
                online = players.GetProperty("online").GetInt32();
                max = players.GetProperty("max").GetInt32();
            }

            var version = "";
            if (root.TryGetProperty("version", out var ver))
                version = ver.GetProperty("name").GetString() ?? "";

            return new ServerStatus
            {
                IsOnline = true,
                ServerName = serverName,
                PlayersOnline = online,
                MaxPlayers = max,
                Version = version
            };
        }
        catch
        {
            return new ServerStatus { IsOnline = false, ServerName = address };
        }
    }

    private static async Task<ServerStatus> QueryGenericTcpAsync(string address, int port)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(address, port);
            if (await Task.WhenAny(connectTask, Task.Delay(5000)) != connectTask)
                return new ServerStatus { IsOnline = false, ServerName = address };

            await connectTask;

            return new ServerStatus
            {
                IsOnline = true,
                ServerName = address,
                PlayersOnline = -1, // Unknown
                MaxPlayers = -1
            };
        }
        catch
        {
            return new ServerStatus { IsOnline = false, ServerName = address };
        }
    }

    private static EmbedBuilder BuildStatusEmbed(GameServerWatch watch, ServerStatus status)
    {
        var eb = new EmbedBuilder()
            .WithTitle($"{watch.GameType.ToUpper()} Server Status")
            .WithColor(status.IsOnline ? new Color(0x2ecc71) : new Color(0xe74c3c))
            .AddField("Server", $"`{watch.ServerAddress}:{watch.ServerPort}`", true)
            .AddField("Status", status.IsOnline ? "Online" : "Offline", true)
            .WithCurrentTimestamp();

        if (status.IsOnline)
        {
            if (!string.IsNullOrEmpty(status.ServerName) && status.ServerName != watch.ServerAddress)
                eb.AddField("Name", status.ServerName, false);

            if (status.PlayersOnline >= 0)
                eb.AddField("Players", $"{status.PlayersOnline}/{status.MaxPlayers}", true);

            if (!string.IsNullOrEmpty(status.Version))
                eb.AddField("Version", status.Version, true);
        }

        return eb;
    }

    public async Task<GameServerWatch> AddServerAsync(ulong guildId, ulong channelId, string gameType,
        string address, int port)
    {
        await using var ctx = _db.GetDbContext();

        var id = await ctx.GetTable<GameServerWatch>().InsertWithInt32IdentityAsync(() => new GameServerWatch
        {
            GuildId = guildId,
            ChannelId = channelId,
            GameType = gameType,
            ServerAddress = address,
            ServerPort = port,
            AutoUpdate = true,
            StatusMessageId = null
        });

        var watch = new GameServerWatch
        {
            Id = id,
            GuildId = guildId,
            ChannelId = channelId,
            GameType = gameType,
            ServerAddress = address,
            ServerPort = port,
            AutoUpdate = true
        };

        _watches[id] = watch;
        return watch;
    }

    public async Task<bool> RemoveServerAsync(ulong guildId, int id)
    {
        await using var ctx = _db.GetDbContext();
        var deleted = await ctx.GetTable<GameServerWatch>()
            .Where(x => x.GuildId == guildId && x.Id == id)
            .DeleteAsync();

        _watches.TryRemove(id, out _);
        return deleted > 0;
    }

    public async Task<List<GameServerWatch>> ListServersAsync(ulong guildId)
    {
        await using var ctx = _db.GetDbContext();
        return await ctx.GetTable<GameServerWatch>()
            .Where(x => x.GuildId == guildId)
            .ToListAsyncLinqToDB();
    }

    #region Minecraft Protocol Helpers

    private static void WriteVarInt(Stream stream, int value)
    {
        var unsigned = (uint)value;
        while (unsigned > 127)
        {
            stream.WriteByte((byte)(unsigned & 0x7F | 0x80));
            unsigned >>= 7;
        }
        stream.WriteByte((byte)unsigned);
    }

    private static void WriteString(Stream stream, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteVarInt(stream, bytes.Length);
        stream.Write(bytes);
    }

    private static int ReadVarInt(Stream stream)
    {
        var result = 0;
        var shift = 0;
        byte b;
        do
        {
            var readByte = stream.ReadByte();
            if (readByte == -1) return 0;
            b = (byte)readByte;
            result |= (b & 0x7F) << shift;
            shift += 7;
        } while ((b & 0x80) != 0);

        return result;
    }

    #endregion
}

public class ServerStatus
{
    public bool IsOnline { get; set; }
    public string ServerName { get; set; } = "";
    public int PlayersOnline { get; set; }
    public int MaxPlayers { get; set; }
    public string Version { get; set; } = "";
}
