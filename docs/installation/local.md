# Local Installation

## Requirements

- .NET 8 SDK
- Git
- ffmpeg (for music features)
- yt-dlp (for music features)

## Steps

### 1. Install .NET 8

**Linux (no sudo needed):**
```bash
curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 8.0
export PATH=$PATH:$HOME/.dotnet
```

**Windows:** Download from https://dotnet.microsoft.com/download/dotnet/8.0

**macOS:**
```bash
brew install dotnet-sdk
```

### 2. Install music dependencies (optional)

**Linux:**
```bash
sudo apt install ffmpeg
pip install yt-dlp
```

**Windows:** Download ffmpeg and yt-dlp from their respective websites and add to PATH.

### 3. Clone and configure

```bash
git clone https://github.com/iSnuggs/SantiBot.git
cd SantiBot
cp src/SantiBot/data/creds_example.yml src/SantiBot/data/creds.yml
# Edit creds.yml with your token and owner ID
```

### 4. Build and run

```bash
dotnet build SantiBot.sln
dotnet run --project src/SantiBot
```

### 5. Run as a service (Linux)

Create `/etc/systemd/system/santibot.service`:

```ini
[Unit]
Description=SantiBot Discord Bot
After=network.target

[Service]
Type=simple
User=YOUR_USERNAME
WorkingDirectory=/path/to/SantiBot
ExecStart=/path/to/.dotnet/dotnet run --project src/SantiBot
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
```

Then:
```bash
sudo systemctl enable santibot
sudo systemctl start santibot
```
