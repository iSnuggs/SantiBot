# Migrating from NadekoBot

SantiBot is a fork of NadekoBot v6, so migration is straightforward.

## Database Compatibility

SantiBot uses the same database schema as NadekoBot v6 with additional tables for new features. Your existing NadekoBot database will work directly — new tables are created automatically on first run.

## Steps

### 1. Back up your NadekoBot data

```bash
cp -r /path/to/nadekobot/data /path/to/backup
```

### 2. Clone SantiBot

```bash
git clone https://github.com/iSnuggs/SantiBot.git
cd SantiBot
```

### 3. Copy your data

```bash
cp /path/to/backup/NadekoBot.db src/SantiBot/data/SantiBot.db
cp /path/to/backup/creds.yml src/SantiBot/data/creds.yml
```

### 4. Run SantiBot

```bash
dotnet run --project src/SantiBot
```

All your existing settings, XP, currency, expressions, and configurations will carry over.

## What's New

After migrating, you'll have access to all SantiBot-exclusive features:

- Starboard
- Polls & Suggestions
- Forms
- Auto Purge
- Enhanced logging (NicknameChanged, RoleChanged, EmojiUpdated)
- Multi-winner giveaways with role requirements
- Recurring reminders
- Reddit feed integration
- Customizable rank cards
- Quote usage statistics
- Web dashboard
