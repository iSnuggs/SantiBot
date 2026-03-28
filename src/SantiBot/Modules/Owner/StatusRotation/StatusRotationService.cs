using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using SantiBot.Common.ModuleBehaviors;
using SantiBot.Db.Models;

namespace SantiBot.Modules.Owner;

public sealed class StatusRotationService : INService, IReadyExecutor
{
    private readonly DbService _db;
    private readonly DiscordSocketClient _client;
    private readonly List<RotatingStatus> _statuses = new();
    private readonly object _statusLock = new();
    private Timer? _timer;
    private int _currentIndex;

    public StatusRotationService(DbService db, DiscordSocketClient client)
    {
        _db = db;
        _client = client;
    }

    public async Task OnReadyAsync()
    {
        await using var ctx = _db.GetDbContext();
        var all = await ctx.GetTable<RotatingStatus>().ToListAsyncLinqToDB();

        lock (_statusLock)
        {
            _statuses.Clear();
            _statuses.AddRange(all);
        }

        if (_statuses.Count > 0)
            StartTimer();
    }

    private void StartTimer()
    {
        _timer?.Dispose();
        _timer = new Timer(async _ =>
        {
            try
            {
                RotatingStatus? status;
                lock (_statusLock)
                {
                    if (_statuses.Count == 0)
                        return;

                    _currentIndex = _currentIndex % _statuses.Count;
                    status = _statuses[_currentIndex];
                    _currentIndex++;
                }

                var text = status.Status
                    .Replace("{0}", _client.Guilds.Count.ToString());

                var activityType = (ActivityType)status.Type;
                await _client.SetActivityAsync(new Game(text, activityType));
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error rotating status");
            }
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(60));
    }

    private void StopTimer()
    {
        _timer?.Dispose();
        _timer = null;
    }

    public async Task AddStatusAsync(string text, int type)
    {
        await using var ctx = _db.GetDbContext();
        var entry = new RotatingStatus { Status = text, Type = type };

        var id = await ctx.GetTable<RotatingStatus>().InsertWithInt32IdentityAsync(() => new RotatingStatus
        {
            Status = text,
            Type = type
        });

        entry.Id = id;

        lock (_statusLock)
        {
            _statuses.Add(entry);
        }

        if (_timer is null)
            StartTimer();
    }

    public async Task<bool> RemoveStatusAsync(int id)
    {
        await using var ctx = _db.GetDbContext();
        var deleted = await ctx.GetTable<RotatingStatus>()
            .Where(x => x.Id == id)
            .DeleteAsync();

        if (deleted == 0)
            return false;

        lock (_statusLock)
        {
            _statuses.RemoveAll(x => x.Id == id);
            if (_statuses.Count == 0)
                StopTimer();
        }

        return true;
    }

    public List<RotatingStatus> ListStatuses()
    {
        lock (_statusLock)
        {
            return _statuses.ToList();
        }
    }

    public async Task ClearAsync()
    {
        await using var ctx = _db.GetDbContext();
        await ctx.GetTable<RotatingStatus>().DeleteAsync();

        lock (_statusLock)
        {
            _statuses.Clear();
        }

        StopTimer();
        await _client.SetActivityAsync(null);
    }
}
