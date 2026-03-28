<p align="center">
  <img src="assets/image(4)TP.png" alt="SantiBot" width="180" />
</p>

<h1 align="center">SantiBot</h1>

<p align="center">
  <strong>An open-source, self-hostable Discord bot combining the best of NadekoBot and Dyno.</strong><br/>
  Named after Santi, a beloved companion.
</p>

<p align="center">
  <a href="https://github.com/iSnuggs/SantiBot/actions"><img src="https://github.com/iSnuggs/SantiBot/actions/workflows/ci.yml/badge.svg?branch=develop" alt="CI/CD" /></a>
  <a href="LICENSE.md"><img src="https://img.shields.io/badge/license-AGPLv3-blue.svg" alt="License" /></a>
  <a href="https://github.com/iSnuggs/SantiBot/releases"><img src="https://img.shields.io/github/v/release/iSnuggs/SantiBot?include_prereleases" alt="Release" /></a>
</p>

---

Built on a fork of [NadekoBot](https://github.com/nadeko-bot/nadekobot) v6, SantiBot merges every feature from NadekoBot and Dyno into one bot — with a full web dashboard, slash commands, and features Dyno charges for, all completely free.

## Features

### Core (inherited from NadekoBot)
- **400+ Commands** — text prefix + slash commands side by side
- **Music** — YouTube, SoundCloud, and 1000+ sources via yt-dlp
- **Economy** — currency, gambling, slots, blackjack, banking, shops, fishing
- **Games** — trivia, hangman, tic-tac-toe, minesweeper, typing contests, pixel art
- **XP & Leveling** — experience system with leaderboards and customizable rank cards
- **Stream Notifications** — Twitch, YouTube, Kick, Trovo, Facebook, Picarto go-live alerts
- **Custom Expressions** — auto-responses with full customization
- **Roles** — reaction roles, button roles, self-assignable roles, autoroles

### Dyno Features (free in SantiBot)
- **Advanced Automod** — 19 filter types with per-rule actions, escalating punishments
- **Auto-Responder** — keyword triggers with cooldowns, channel restrictions
- **Ticket System** — button-based tickets with claiming, logging, categories
- **Autoban** — auto-ban by account age, username patterns, no-avatar
- **Auto Delete** — per-channel message auto-deletion with filters
- **Auto Message** — scheduled one-time and recurring messages
- **Anti-Raid** — verification gates, auto-lockdown, mass join ban
- **Voice Text Linking** — auto-grant text access when joining voice
- **Auto-Dehoist** — remove hoisting characters from nicknames
- **Custom Embed Builder** — build and send rich embeds via commands
- **TikTok Feed** — follow TikTok accounts for new post notifications
- **Enhanced Welcome Images** — custom banners with avatar, text, backgrounds
- **Mod Cases** — case numbering, mod notes, auto-punish escalation

### Community Features
- **Starboard** — highlight popular messages with star reactions
- **Polls** — button-based voting with timed expiry
- **Suggestions** — approve/deny community suggestions with reasons
- **Giveaways** — role requirements, multi-winner, auto-end
- **Enhanced Reminders** — recurring reminders with auto-reschedule

### Web Dashboard
- **32 config pages** — manage every feature from your browser
- **Real-time updates** — SignalR live sync between dashboard users
- **Discord OAuth login** — secure authentication with permission checks
- **Embed builder** — visual builder with save/load templates

### Distribution
- **Desktop Installer** — Tauri-based app for Windows, Mac, and Linux
- **Docker** — multi-platform images (amd64 + arm64)
- **Medusa Plugins** — extend SantiBot with community plugins

## Quick Start

### Docker (Recommended)
```bash
git clone https://github.com/iSnuggs/SantiBot.git
cd SantiBot
cp src/SantiBot/data/creds_example.yml src/SantiBot/data/creds.yml
# Edit creds.yml with your Discord bot token and owner ID
docker compose up -d
```

### Local
```bash
git clone https://github.com/iSnuggs/SantiBot.git
cd SantiBot
cp src/SantiBot/data/creds_example.yml src/SantiBot/data/creds.yml
# Edit creds.yml with your Discord bot token and owner ID
dotnet run --project src/SantiBot
```

### Dashboard
```bash
cd dashboard
npm install
npm run dev
# Open http://localhost:3000
```

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/) (for dashboard)
- [ffmpeg](https://ffmpeg.org/) (for music)
- [yt-dlp](https://github.com/yt-dlp/yt-dlp) (for music)

## Links

- **Creator:** [Snuggs](https://www.twitch.tv/Snuggle) | [YouTube](https://www.youtube.com/@oSnuggleBunnyo) | [Twitter/X](https://x.com/oSnuggleBunnyo)
- **Based on:** [NadekoBot](https://github.com/nadeko-bot/nadekobot) by the NadekoBot team

## License

SantiBot is licensed under the [GNU AGPLv3](LICENSE.md). This is a fork of NadekoBot, which is also licensed under AGPLv3.
