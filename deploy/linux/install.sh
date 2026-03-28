#!/bin/bash
# ╔══════════════════════════════════════════════════════════════╗
# ║              SantiBot Linux Installer                        ║
# ║                                                              ║
# ║  Usage: curl -fsSL https://raw.githubusercontent.com/        ║
# ║         iSnuggs/SantiBot/develop/deploy/linux/install.sh     ║
# ║         | sudo bash                                          ║
# ╚══════════════════════════════════════════════════════════════╝

set -euo pipefail

INSTALL_DIR="/opt/santibot"
SERVICE_USER="santibot"
REPO="iSnuggs/SantiBot"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

info()  { echo -e "${BLUE}[INFO]${NC} $1"; }
ok()    { echo -e "${GREEN}[OK]${NC} $1"; }
warn()  { echo -e "${YELLOW}[WARN]${NC} $1"; }
error() { echo -e "${RED}[ERROR]${NC} $1"; exit 1; }

echo ""
echo -e "${BLUE}╔══════════════════════════════════════╗${NC}"
echo -e "${BLUE}║       SantiBot Linux Installer       ║${NC}"
echo -e "${BLUE}╚══════════════════════════════════════╝${NC}"
echo ""

# Check root
if [ "$EUID" -ne 0 ]; then
    error "Please run as root: sudo bash install.sh"
fi

# Detect architecture
ARCH=$(uname -m)
case "$ARCH" in
    x86_64)  RUNTIME="linux-x64" ;;
    aarch64) RUNTIME="linux-arm64" ;;
    *)       error "Unsupported architecture: $ARCH" ;;
esac
info "Detected architecture: $ARCH ($RUNTIME)"

# Step 1: Install dependencies
info "Installing dependencies..."
apt-get update -qq
apt-get install -y -qq curl unzip ffmpeg libsodium23 libopus0 > /dev/null 2>&1
ok "Dependencies installed"

# Step 2: Install yt-dlp
if ! command -v yt-dlp &> /dev/null; then
    info "Installing yt-dlp..."
    if [ "$ARCH" = "aarch64" ]; then
        YT_DLP_BIN="yt-dlp_linux_aarch64"
    else
        YT_DLP_BIN="yt-dlp_linux"
    fi
    curl -L -o /usr/local/bin/yt-dlp "https://github.com/yt-dlp/yt-dlp/releases/latest/download/${YT_DLP_BIN}" 2>/dev/null
    chmod 755 /usr/local/bin/yt-dlp
    ok "yt-dlp installed"
else
    ok "yt-dlp already installed"
fi

# Step 3: Create service user
if ! id "$SERVICE_USER" &>/dev/null; then
    info "Creating service user: $SERVICE_USER"
    useradd -r -s /usr/sbin/nologin -d "$INSTALL_DIR" "$SERVICE_USER"
    ok "User created"
else
    ok "User $SERVICE_USER already exists"
fi

# Step 4: Download latest release
info "Fetching latest release..."
LATEST=$(curl -s "https://api.github.com/repos/$REPO/releases/latest" | grep -oP '"tag_name": "\K[^"]+')
if [ -z "$LATEST" ]; then
    warn "No release found, using develop branch build"
    LATEST="develop"
fi
info "Latest version: $LATEST"

DOWNLOAD_URL="https://github.com/$REPO/releases/download/$LATEST/santi-${RUNTIME}.tar.gz"
info "Downloading $DOWNLOAD_URL..."

mkdir -p "$INSTALL_DIR"
curl -L -o /tmp/santibot.tar.gz "$DOWNLOAD_URL" 2>/dev/null
tar -xzf /tmp/santibot.tar.gz -C "$INSTALL_DIR" --strip-components=1
rm /tmp/santibot.tar.gz
chmod +x "$INSTALL_DIR/SantiBot"
ok "SantiBot downloaded and extracted"

# Step 5: Set up data directory
if [ ! -f "$INSTALL_DIR/data/creds.yml" ]; then
    info "Creating default config..."
    mkdir -p "$INSTALL_DIR/data"
    if [ -f "$INSTALL_DIR/data_init/creds_example.yml" ]; then
        cp "$INSTALL_DIR/data_init/creds_example.yml" "$INSTALL_DIR/data/creds.yml"
    elif [ -f "$INSTALL_DIR/data/creds_example.yml" ]; then
        cp "$INSTALL_DIR/data/creds_example.yml" "$INSTALL_DIR/data/creds.yml"
    fi
    ok "Config created at $INSTALL_DIR/data/creds.yml"
    warn "You MUST edit creds.yml with your Discord bot token before starting!"
else
    ok "Existing config preserved"
fi

# Step 6: Set ownership
chown -R "$SERVICE_USER:$SERVICE_USER" "$INSTALL_DIR"
ok "Permissions set"

# Step 7: Install systemd service
info "Installing systemd service..."
SCRIPT_DIR="$(cd "$(dirname "$0")" 2>/dev/null && pwd)" || SCRIPT_DIR=""
if [ -f "$SCRIPT_DIR/santibot.service" ]; then
    cp "$SCRIPT_DIR/santibot.service" /etc/systemd/system/
elif [ -f "$INSTALL_DIR/deploy/linux/santibot.service" ]; then
    cp "$INSTALL_DIR/deploy/linux/santibot.service" /etc/systemd/system/
else
    # Download service file
    curl -fsSL "https://raw.githubusercontent.com/$REPO/develop/deploy/linux/santibot.service" \
        -o /etc/systemd/system/santibot.service 2>/dev/null
fi
systemctl daemon-reload
systemctl enable santibot
ok "Systemd service installed and enabled"

# Step 8: Install update script
info "Installing update script..."
if [ -f "$SCRIPT_DIR/update.sh" ]; then
    cp "$SCRIPT_DIR/update.sh" "$INSTALL_DIR/update.sh"
else
    curl -fsSL "https://raw.githubusercontent.com/$REPO/develop/deploy/linux/update.sh" \
        -o "$INSTALL_DIR/update.sh" 2>/dev/null || true
fi
chmod +x "$INSTALL_DIR/update.sh" 2>/dev/null || true
ok "Update script installed"

echo ""
echo -e "${GREEN}╔══════════════════════════════════════╗${NC}"
echo -e "${GREEN}║       Installation Complete!         ║${NC}"
echo -e "${GREEN}╚══════════════════════════════════════╝${NC}"
echo ""
echo -e "  Install location: ${BLUE}$INSTALL_DIR${NC}"
echo -e "  Config file:      ${BLUE}$INSTALL_DIR/data/creds.yml${NC}"
echo ""
echo -e "  ${YELLOW}Next steps:${NC}"
echo -e "  1. Edit your config:  ${BLUE}sudo nano $INSTALL_DIR/data/creds.yml${NC}"
echo -e "     Add your Discord bot token and owner ID"
echo -e "  2. Start the bot:     ${BLUE}sudo systemctl start santibot${NC}"
echo -e "  3. Check status:      ${BLUE}sudo systemctl status santibot${NC}"
echo -e "  4. View logs:         ${BLUE}journalctl -u santibot -f${NC}"
echo -e "  5. Update later:      ${BLUE}sudo $INSTALL_DIR/update.sh${NC}"
echo ""
