# SantiBot

**SantiBot** is an open-source, self-hostable Discord bot that combines the best features of NadekoBot and Dyno into one powerful package. Named after Santi, a beloved companion.

Built on a fork of [NadekoBot](https://github.com/nadeko-bot/nadekobot) v6, SantiBot extends the foundation with additional features, a modern web dashboard, and a polished installer — all free and open source.

## Features

- **Moderation** — Anti-spam, anti-raid, anti-alt, warnings, muting, banning, pruning, and more
- **Music** — Play from YouTube, SoundCloud, and 1000+ sources via yt-dlp
- **Economy** — Currency system, gambling, slots, blackjack, banking, shops, fishing
- **Games** — Trivia, hangman, tic-tac-toe, minesweeper, typing contests, collaborative pixel art
- **XP & Leveling** — Experience system with leaderboards and customizable rank cards
- **Stream Notifications** — Twitch, YouTube, Kick, Trovo, Facebook, Picarto go-live alerts
- **Custom Commands** — Expressions and auto-responses with full customization
- **Roles** — Reaction roles, button roles, self-assignable roles, autoroles
- **Giveaways** — Create and manage giveaways with winner selection
- **Starboard** — Highlight popular messages *(coming soon)*
- **Polls & Suggestions** — Community voting *(coming soon)*
- **Web Dashboard** — Full server management from your browser *(coming soon)*
- **And much more** — AFK, reminders, quotes, scheduled commands, repeaters, feeds...

## Quick Start

### Docker (Recommended)
```bash
git clone https://github.com/iSnuggs/SantiBot.git
cd SantiBot
# Edit src/SantiBot/data/creds_example.yml with your bot token
docker compose up -d
```

### Local
```bash
git clone https://github.com/iSnuggs/SantiBot.git
cd SantiBot
dotnet run --project src/SantiBot
```

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [ffmpeg](https://ffmpeg.org/) (for music)
- [yt-dlp](https://github.com/yt-dlp/yt-dlp) (for music)

## Links

- **Creator:** [Snuggs](https://www.twitch.tv/Snuggle) | [YouTube](https://www.youtube.com/@oSnuggleBunnyo) | [Twitter/X](https://x.com/oSnuggleBunnyo)
- **Based on:** [NadekoBot](https://github.com/nadeko-bot/nadekobot) by the NadekoBot team

## License

SantiBot is licensed under the [GNU AGPLv3](LICENSE.md). This is a fork of NadekoBot, which is also licensed under AGPLv3.
