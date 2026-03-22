# Getting Started with SantiBot

Get SantiBot running on your Discord server in under 15 minutes.

## Prerequisites

1. A **Discord Bot Token** — [Create one here](https://discord.com/developers/applications)
2. **.NET 8 SDK** — [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
3. **Git** — [Download](https://git-scm.com/)

## Quick Start

### 1. Clone the repository

```bash
git clone https://github.com/iSnuggs/SantiBot.git
cd SantiBot
```

### 2. Configure your bot token

```bash
cp src/SantiBot/data/creds_example.yml src/SantiBot/data/creds.yml
```

Edit `src/SantiBot/data/creds.yml` and add your bot token and owner ID:

```yaml
token: "YOUR_BOT_TOKEN_HERE"
ownerIds:
  - YOUR_DISCORD_USER_ID
```

### 3. Run the bot

```bash
dotnet run --project src/SantiBot
```

### 4. Invite the bot to your server

Go to the [Discord Developer Portal](https://discord.com/developers/applications), select your application, go to **OAuth2 > URL Generator**, select `bot` and `applications.commands` scopes, then select the permissions you want. Copy the generated URL and open it in your browser.

### 5. Test it

Type `.help` in any channel where the bot has access.

## Next Steps

- [Docker Installation](installation/docker.md) — Run with Docker
- [VPS Installation](installation/vps.md) — Deploy on a remote server
- [Features Overview](features/overview.md) — See what SantiBot can do
- [Dashboard Setup](dashboard/setup.md) — Configure the web dashboard

## Need Help?

- [GitHub Issues](https://github.com/iSnuggs/SantiBot/issues)
- [GitHub Discussions](https://github.com/iSnuggs/SantiBot/discussions)
