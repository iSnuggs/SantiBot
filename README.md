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

> **1,690 text commands** | **60 curated slash commands** | **1000+ features**

### Core
- **1,690 Commands** — text prefix commands with 60 curated slash commands
- **Music** — YouTube, SoundCloud, and 1000+ sources via yt-dlp
- **Economy** — currency, gambling, slots, blackjack, banking, shops, fishing, crafting, jobs, stock market, crypto sim, auctions, real estate
- **RPG Dungeon System** — 11 classes, 8 races, equipment, skill trees, prestige, party dungeons
- **1,000 Raid Bosses** — 50 handcrafted + 950 procedural, 4 phases, 6 tiers (Common to Mythic)
- **Pet System** — 30 species with evolution, battles, and adventures
- **XP & Leveling** — experience system with multipliers, challenges, battle pass, prestige
- **500 Achievements** — 12 categories including secret/hidden achievements
- **100 Badges + 50 Titles** — collectible progression with display on profiles
- **Crafting & Gathering** — 10 skills (mining, woodcutting, farming, cooking, alchemy, blacksmithing, etc.)
- **PvP Arena** — 1v1 duels with Elo rating, tournaments with brackets
- **Housing** — buy/upgrade houses, decorate rooms, guest books, 10 styles
- **Quests** — daily, weekly, and story quests with 4 factions
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
- **Mod Mail** — DM-to-staff relay with threaded channels, transcripts, and block list

### Social & Community
- **30 RP Actions** — hug, pat, kiss, slap, yeet, and 25 more with flavor text
- **20 Fun Commands** — 8-ball, tarot, jokes, facts, roasts, would-you-rather, horoscope
- **8 Mini-Games** — Wordle, Minesweeper, Geography Quiz, Math Race, Emoji Quiz, and more
- **Starboard** — highlight popular messages with star reactions
- **Polls & Suggestions** — button-based voting with timed expiry
- **Events & Scheduling** — RSVP system, movie night polls, game night, study sessions
- **Giveaways** — role requirements, multi-winner, auto-end
- **Marriage, Karma, Profiles** — full social system with friendship tracking

### Server Tools
- **Advanced Automod** — 19 filter types, anti-nuke protection, phishing detection
- **Security Suite** — token leak scanner, honeypot channels, risk scoring, account age gates
- **Server Analytics** — activity heatmaps, growth charts, peak hours, word frequency
- **20 Server Themes** — Dark, Neon, Ocean, Galaxy, Halloween, Christmas, and more
- **Custom Commands** — create server-specific commands with no code
- **Channel Points** — Twitch-style points with predictions and rewards
- **Lore System** — 50 lore entries, monster bestiary, treasure maps, world events
- **Feed Subscriptions** — YouTube, Twitch, Reddit, RSS, Twitter, Steam, Weather
- **Uptime Monitoring** — watch websites/game servers for downtime

### AI & Smart Features
- **Smart FAQ** — keyword-matching Q&A system per server
- **Sentiment Analysis** — detect positive/negative message tone
- **Topic Detection** — auto-categorize conversations (Gaming, Music, Tech, etc.)
- **Name Generator** — character, fantasy, elf, dwarf, band, superhero names
- **Writing Helper** — typo correction, passive voice detection

### Developer Tools
- **Feature Flags** — enable/disable features per server with rollout %
- **Webhook Endpoints** — trigger actions via external webhooks
- **Command Analytics** — usage stats, success rates, response times
- **XP Multipliers** — global, per-channel, per-role, event-based, timed
- **Premium Tiers** — Free, Basic, Pro, Enterprise with feature gating

### Web Dashboard
- **32+ config pages** — manage every feature from your browser
- **Real-time updates** — SignalR live sync between dashboard users
- **Discord OAuth login** — secure authentication with permission checks
- **Swagger API docs** — full API documentation at /swagger
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

### Mod Mail Setup

Mod Mail lets users DM the bot to contact your staff team. Each DM creates a private channel where staff can reply, and the bot relays messages both ways.

1. **Enable mod mail:**
   ```
   .mmenable
   ```
2. **Set the category** where thread channels are created:
   ```
   .mmcat Mod Mail
   ```
3. **Set the staff role** (who can see and reply to threads):
   ```
   .mmrole @Staff
   ```
4. **Set a log channel** for transcripts when threads close (optional):
   ```
   .mmlog #modmail-logs
   ```
5. **Customize messages** sent to users (optional):
   ```
   .mmopenmsg Thanks for reaching out! Our staff will reply shortly.
   .mmclosemsg Your thread has been resolved. DM again if you need more help!
   ```

**How it works:** A user DMs the bot, a private `mm-username` channel appears in your Mod Mail category, staff type in that channel to reply, and the bot delivers it to the user's DMs. Use `.mmclose` or the Close Thread button to end the conversation and log a transcript.

**Other commands:** `.mmblock @user` / `.mmunblock @user` to manage blocked users, `.mmlist` to see recent threads, `.mmsetup` to view current config.

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
