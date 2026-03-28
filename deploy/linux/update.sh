#!/bin/bash
# ╔══════════════════════════════════════════════════════════════╗
# ║              SantiBot Linux Updater                          ║
# ║                                                              ║
# ║  Usage: sudo /opt/santibot/update.sh                         ║
# ╚══════════════════════════════════════════════════════════════╝

set -euo pipefail

INSTALL_DIR="/opt/santibot"
REPO="iSnuggs/SantiBot"

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
echo -e "${BLUE}║        SantiBot Linux Updater        ║${NC}"
echo -e "${BLUE}╚══════════════════════════════════════╝${NC}"
echo ""

if [ "$EUID" -ne 0 ]; then
    error "Please run as root: sudo $0"
fi

# Detect architecture
ARCH=$(uname -m)
case "$ARCH" in
    x86_64)  RUNTIME="linux-x64" ;;
    aarch64) RUNTIME="linux-arm64" ;;
    *)       error "Unsupported architecture: $ARCH" ;;
esac

# Get current version (if available)
CURRENT="unknown"
if [ -f "$INSTALL_DIR/version.txt" ]; then
    CURRENT=$(cat "$INSTALL_DIR/version.txt")
fi

# Fetch latest version
info "Checking for updates..."
LATEST=$(curl -s "https://api.github.com/repos/$REPO/releases/latest" | grep -oP '"tag_name": "\K[^"]+')
if [ -z "$LATEST" ]; then
    error "Could not fetch latest version. Check your internet connection."
fi

info "Current version: $CURRENT"
info "Latest version:  $LATEST"

if [ "$CURRENT" = "$LATEST" ]; then
    ok "Already up to date!"
    exit 0
fi

# Stop the bot
info "Stopping SantiBot..."
systemctl stop santibot 2>/dev/null || true
ok "Bot stopped"

# Backup current install (keep data)
info "Creating backup..."
if [ -d "$INSTALL_DIR.bak" ]; then
    rm -rf "$INSTALL_DIR.bak"
fi
cp -r "$INSTALL_DIR" "$INSTALL_DIR.bak"
ok "Backup created at $INSTALL_DIR.bak"

# Download new version
DOWNLOAD_URL="https://github.com/$REPO/releases/download/$LATEST/santi-${RUNTIME}.tar.gz"
info "Downloading $LATEST..."
curl -L -o /tmp/santibot-update.tar.gz "$DOWNLOAD_URL" 2>/dev/null

# Extract (preserve data directory)
info "Installing update..."
# Save data directory
mv "$INSTALL_DIR/data" /tmp/santibot-data-preserve

# Extract new files
rm -rf "$INSTALL_DIR"
mkdir -p "$INSTALL_DIR"
tar -xzf /tmp/santibot-update.tar.gz -C "$INSTALL_DIR" --strip-components=1

# Restore data directory
rm -rf "$INSTALL_DIR/data"
mv /tmp/santibot-data-preserve "$INSTALL_DIR/data"

# Clean up
rm /tmp/santibot-update.tar.gz
chmod +x "$INSTALL_DIR/SantiBot"

# Save version
echo "$LATEST" > "$INSTALL_DIR/version.txt"

# Fix permissions
chown -R santibot:santibot "$INSTALL_DIR"
ok "Update installed"

# Restart the bot
info "Starting SantiBot..."
systemctl start santibot
ok "Bot started"

echo ""
echo -e "${GREEN}╔══════════════════════════════════════╗${NC}"
echo -e "${GREEN}║        Update Complete!              ║${NC}"
echo -e "${GREEN}║   $CURRENT → $LATEST${NC}"
echo -e "${GREEN}╚══════════════════════════════════════╝${NC}"
echo ""
echo -e "  Check status: ${BLUE}sudo systemctl status santibot${NC}"
echo -e "  View logs:    ${BLUE}journalctl -u santibot -f${NC}"
echo -e "  Rollback:     ${BLUE}sudo rm -rf $INSTALL_DIR && sudo mv $INSTALL_DIR.bak $INSTALL_DIR && sudo systemctl restart santibot${NC}"
echo ""
