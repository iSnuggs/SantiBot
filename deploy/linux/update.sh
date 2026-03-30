#!/bin/bash
# ╔══════════════════════════════════════════════════════════════╗
# ║              SantiBot Auto-Updater v2.0                      ║
# ║                                                              ║
# ║  Can be run manually or via cron for automatic updates.      ║
# ║                                                              ║
# ║  Manual:  sudo /opt/santibot/update.sh                       ║
# ║  Cron:    0 4 * * * /opt/santibot/update.sh                  ║
# ╚══════════════════════════════════════════════════════════════╝

set -euo pipefail

INSTALL_DIR="/opt/santibot"
SERVICE_USER="santibot"
REPO="iSnuggs/SantiBot"
BACKUP_DIR="/opt/santibot-backups"

log() { echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1"; }

log "=== SantiBot Update Check ==="

if [ ! -d "$INSTALL_DIR" ]; then
    log "ERROR: Not installed at $INSTALL_DIR"
    exit 1
fi

ARCH=$(uname -m)
case "$ARCH" in
    x86_64)  RUNTIME="linux-x64" ;;
    aarch64) RUNTIME="linux-arm64" ;;
    *)       log "ERROR: Unsupported arch: $ARCH"; exit 1 ;;
esac

# Check for new version
LATEST=$(curl -s "https://api.github.com/repos/$REPO/releases/latest" 2>/dev/null | grep -oP '"tag_name": "\K[^"]+' || echo "")
if [ -z "$LATEST" ]; then
    log "No release found. Skipping."
    exit 0
fi

CURRENT=""
[ -f "$INSTALL_DIR/.version" ] && CURRENT=$(cat "$INSTALL_DIR/.version")

if [ "$LATEST" = "$CURRENT" ]; then
    log "Up to date ($CURRENT)"
    exit 0
fi

log "Update: $CURRENT -> $LATEST"

# Backup
mkdir -p "$BACKUP_DIR"
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
[ -d "$INSTALL_DIR/data" ] && tar -czf "$BACKUP_DIR/santibot-backup-$TIMESTAMP.tar.gz" -C "$INSTALL_DIR" data/ 2>/dev/null
ls -t "$BACKUP_DIR"/santibot-backup-*.tar.gz 2>/dev/null | tail -n +11 | xargs rm -f 2>/dev/null || true
log "Backup saved"

# Stop
systemctl stop santibot 2>/dev/null || true
sleep 2

# Download
DOWNLOAD_URL="https://github.com/$REPO/releases/download/$LATEST/santi-${RUNTIME}.tar.gz"
log "Downloading $LATEST..."
curl -L -o /tmp/santibot-update.tar.gz "$DOWNLOAD_URL" 2>/dev/null

if [ ! -s /tmp/santibot-update.tar.gz ]; then
    log "ERROR: Download failed. Restarting current version."
    systemctl start santibot 2>/dev/null || true
    exit 1
fi

# Extract (keep data)
tar -xzf /tmp/santibot-update.tar.gz -C "$INSTALL_DIR" --strip-components=1 --exclude='data'
rm -f /tmp/santibot-update.tar.gz
chmod +x "$INSTALL_DIR/SantiBot" 2>/dev/null || true
echo "$LATEST" > "$INSTALL_DIR/.version"
chown -R "$SERVICE_USER:$SERVICE_USER" "$INSTALL_DIR"

# Restart
systemctl start santibot 2>/dev/null || true
sleep 5

if systemctl is-active --quiet santibot; then
    log "SUCCESS: Updated to $LATEST"
else
    log "WARNING: Bot may not have started. Restoring backup..."
    tar -xzf "$BACKUP_DIR/santibot-backup-$TIMESTAMP.tar.gz" -C "$INSTALL_DIR" 2>/dev/null || true
    systemctl start santibot 2>/dev/null || true
fi

log "=== Done ==="
