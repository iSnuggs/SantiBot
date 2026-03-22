# VPS Installation

Deploy SantiBot on a remote Linux server.

## Requirements

- A Linux VPS (Ubuntu 22.04+ recommended)
- SSH access
- At least 1GB RAM

## Steps

### 1. Connect to your VPS

```bash
ssh user@your-server-ip
```

### 2. Install prerequisites

```bash
# .NET 8 SDK
curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 8.0
echo 'export PATH=$PATH:$HOME/.dotnet' >> ~/.bashrc
source ~/.bashrc

# Music dependencies (optional)
sudo apt update && sudo apt install -y ffmpeg
pip install yt-dlp
```

### 3. Clone and configure

```bash
git clone https://github.com/iSnuggs/SantiBot.git
cd SantiBot
cp src/SantiBot/data/creds_example.yml src/SantiBot/data/creds.yml
nano src/SantiBot/data/creds.yml  # Add your token and owner ID
```

### 4. Build

```bash
dotnet build SantiBot.sln -c Release
```

### 5. Set up as a service

```bash
sudo tee /etc/systemd/system/santibot.service << 'EOF'
[Unit]
Description=SantiBot Discord Bot
After=network.target

[Service]
Type=simple
User=$USER
WorkingDirectory=$HOME/SantiBot
ExecStart=$HOME/.dotnet/dotnet run --project src/SantiBot -c Release
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
EOF

sudo systemctl daemon-reload
sudo systemctl enable santibot
sudo systemctl start santibot
```

### 6. Verify

```bash
sudo systemctl status santibot
sudo journalctl -u santibot -f
```

## Alternative: Docker on VPS

If your VPS has Docker, see the [Docker guide](docker.md) — it's even simpler.
