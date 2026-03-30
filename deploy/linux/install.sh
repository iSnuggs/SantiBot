#!/bin/bash
# ╔══════════════════════════════════════════════════════════════╗
# ║              SantiBot Linux Installer v2.0                   ║
# ║                                                              ║
# ║  Interactive menu installer with install, update, backup,    ║
# ║  and management options.                                     ║
# ║                                                              ║
# ║  Usage: curl -fsSL https://raw.githubusercontent.com/        ║
# ║         iSnuggs/SantiBot/develop/deploy/linux/install.sh     ║
# ║         | sudo bash                                          ║
# ╚══════════════════════════════════════════════════════════════╝

set -euo pipefail

INSTALL_DIR="/opt/santibot"
SERVICE_USER="santibot"
REPO="iSnuggs/SantiBot"
BRANCH="develop"
BACKUP_DIR="/opt/santibot-backups"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
MAGENTA='\033[0;35m'
BOLD='\033[1m'
NC='\033[0m'

info()  { echo -e "${BLUE}[INFO]${NC} $1"; }
ok()    { echo -e "${GREEN}[  OK]${NC} $1"; }
warn()  { echo -e "${YELLOW}[WARN]${NC} $1"; }
err()   { echo -e "${RED}[ERR ]${NC} $1"; }
fatal() { echo -e "${RED}[FAIL]${NC} $1"; exit 1; }

# ── Banner ──────────────────────────────────────────────────────
show_banner() {
    clear
    echo -e "${CYAN}"
    echo "  ╔══════════════════════════════════════════════════╗"
    echo "  ║                                                  ║"
    echo "  ║   🐕 SantiBot — Linux Installer & Manager v2.0   ║"
    echo "  ║                                                  ║"
    echo "  ║   Named after Santi, a beloved companion.        ║"
    echo "  ║   1,690 commands • 1,000 raid bosses • AI chat   ║"
    echo "  ║                                                  ║"
    echo "  ╚══════════════════════════════════════════════════╝"
    echo -e "${NC}"
}

# ── Main Menu ───────────────────────────────────────────────────
show_menu() {
    echo -e "${BOLD}  What would you like to do?${NC}"
    echo ""
    echo -e "  ${GREEN}1)${NC} Install SantiBot          (fresh install)"
    echo -e "  ${GREEN}2)${NC} Update SantiBot           (keep config & data)"
    echo -e "  ${GREEN}3)${NC} Backup data               (save config + database)"
    echo -e "  ${GREEN}4)${NC} Restore from backup       (restore a previous backup)"
    echo -e "  ${GREEN}5)${NC} Start / Stop / Restart     (manage the service)"
    echo -e "  ${GREEN}6)${NC} View logs                  (live bot output)"
    echo -e "  ${GREEN}7)${NC} Edit config                (open creds.yml)"
    echo -e "  ${GREEN}8)${NC} Install dashboard          (web management panel)"
    echo -e "  ${GREEN}9)${NC} System info                (check requirements)"
    echo -e "  ${GREEN}0)${NC} Uninstall SantiBot         (remove everything)"
    echo -e "  ${GREEN}q)${NC} Quit"
    echo ""
    echo -ne "  ${BOLD}Enter choice [1-9, 0, q]: ${NC}"
}

# ── Detect Architecture ────────────────────────────────────────
detect_arch() {
    ARCH=$(uname -m)
    case "$ARCH" in
        x86_64)  RUNTIME="linux-x64" ;;
        aarch64) RUNTIME="linux-arm64" ;;
        armv7l)  RUNTIME="linux-arm" ;;
        *)       fatal "Unsupported architecture: $ARCH" ;;
    esac
}

# ── Check Root ──────────────────────────────────────────────────
check_root() {
    if [ "$EUID" -ne 0 ]; then
        fatal "Please run as root: sudo bash install.sh"
    fi
}

# ── 1) Fresh Install ───────────────────────────────────────────
do_install() {
    check_root
    detect_arch

    echo ""
    info "Starting fresh SantiBot installation..."
    echo ""

    # Check if already installed
    if [ -d "$INSTALL_DIR" ] && [ -f "$INSTALL_DIR/SantiBot" ]; then
        warn "SantiBot is already installed at $INSTALL_DIR"
        echo -ne "  Overwrite? This will ${RED}NOT${NC} delete your config/data. [y/N]: "
        read -r answer
        if [[ ! "$answer" =~ ^[Yy]$ ]]; then
            info "Installation cancelled."
            return
        fi
    fi

    # Step 1: Dependencies
    info "Step 1/7: Installing dependencies..."
    if command -v apt-get &>/dev/null; then
        apt-get update -qq
        apt-get install -y -qq curl unzip ffmpeg libsodium23 libopus0 > /dev/null 2>&1
    elif command -v dnf &>/dev/null; then
        dnf install -y -q curl unzip ffmpeg libsodium opus > /dev/null 2>&1
    elif command -v pacman &>/dev/null; then
        pacman -Sy --noconfirm curl unzip ffmpeg libsodium opus > /dev/null 2>&1
    else
        warn "Unknown package manager — please install curl, ffmpeg, libsodium, opus manually"
    fi
    ok "Dependencies installed"

    # Step 2: yt-dlp
    info "Step 2/7: Installing yt-dlp..."
    if ! command -v yt-dlp &>/dev/null; then
        if [ "$ARCH" = "aarch64" ]; then
            YT_DLP_BIN="yt-dlp_linux_aarch64"
        else
            YT_DLP_BIN="yt-dlp_linux"
        fi
        curl -L -o /usr/local/bin/yt-dlp "https://github.com/yt-dlp/yt-dlp/releases/latest/download/${YT_DLP_BIN}" 2>/dev/null
        chmod 755 /usr/local/bin/yt-dlp
        ok "yt-dlp installed"
    else
        ok "yt-dlp already installed ($(yt-dlp --version 2>/dev/null || echo 'unknown'))"
    fi

    # Step 3: Service user
    info "Step 3/7: Creating service user..."
    if ! id "$SERVICE_USER" &>/dev/null; then
        useradd -r -s /usr/sbin/nologin -d "$INSTALL_DIR" "$SERVICE_USER"
        ok "User '$SERVICE_USER' created"
    else
        ok "User '$SERVICE_USER' already exists"
    fi

    # Step 4: Download
    info "Step 4/7: Downloading SantiBot..."
    LATEST=$(curl -s "https://api.github.com/repos/$REPO/releases/latest" | grep -oP '"tag_name": "\K[^"]+' || echo "")
    if [ -z "$LATEST" ]; then
        warn "No release found — building from source"
        if command -v dotnet &>/dev/null; then
            info "Cloning repository..."
            TMPDIR=$(mktemp -d)
            git clone --depth 1 -b "$BRANCH" "https://github.com/$REPO.git" "$TMPDIR/SantiBot" 2>/dev/null
            info "Building (this may take a few minutes)..."
            dotnet publish "$TMPDIR/SantiBot/src/SantiBot/SantiBot.csproj" -c Release -r "$RUNTIME" --self-contained -o "$INSTALL_DIR" 2>/dev/null
            rm -rf "$TMPDIR"
            ok "Built from source"
        else
            fatal "No release available and .NET SDK not installed. Install .NET 8 SDK first."
        fi
    else
        info "Latest version: $LATEST"
        DOWNLOAD_URL="https://github.com/$REPO/releases/download/$LATEST/santi-${RUNTIME}.tar.gz"
        mkdir -p "$INSTALL_DIR"
        curl -L -o /tmp/santibot.tar.gz "$DOWNLOAD_URL" 2>/dev/null
        tar -xzf /tmp/santibot.tar.gz -C "$INSTALL_DIR" --strip-components=1
        rm -f /tmp/santibot.tar.gz
        chmod +x "$INSTALL_DIR/SantiBot"
        ok "Downloaded and extracted ($LATEST)"
    fi

    # Step 5: Config
    info "Step 5/7: Setting up configuration..."
    mkdir -p "$INSTALL_DIR/data"
    if [ ! -f "$INSTALL_DIR/data/creds.yml" ]; then
        if [ -f "$INSTALL_DIR/data_init/creds_example.yml" ]; then
            cp "$INSTALL_DIR/data_init/creds_example.yml" "$INSTALL_DIR/data/creds.yml"
        elif [ -f "$INSTALL_DIR/data/creds_example.yml" ]; then
            cp "$INSTALL_DIR/data/creds_example.yml" "$INSTALL_DIR/data/creds.yml"
        fi
        ok "Config created (needs Discord token!)"
    else
        ok "Existing config preserved"
    fi

    # Step 6: Permissions
    info "Step 6/7: Setting permissions..."
    chown -R "$SERVICE_USER:$SERVICE_USER" "$INSTALL_DIR"
    ok "Permissions set"

    # Step 7: Systemd service
    info "Step 7/7: Installing systemd service..."
    cat > /etc/systemd/system/santibot.service << 'SVCEOF'
[Unit]
Description=SantiBot Discord Bot
After=network.target

[Service]
Type=simple
User=santibot
Group=santibot
WorkingDirectory=/opt/santibot
ExecStart=/opt/santibot/SantiBot
Restart=always
RestartSec=10
SyslogIdentifier=santibot

# Security hardening
NoNewPrivileges=true
ProtectSystem=strict
ProtectHome=true
ReadWritePaths=/opt/santibot

[Install]
WantedBy=multi-user.target
SVCEOF
    systemctl daemon-reload
    systemctl enable santibot
    ok "Systemd service installed and enabled"

    echo ""
    echo -e "${GREEN}  ╔══════════════════════════════════════╗${NC}"
    echo -e "${GREEN}  ║     Installation Complete! 🐕        ║${NC}"
    echo -e "${GREEN}  ╚══════════════════════════════════════╝${NC}"
    echo ""
    echo -e "  Install path: ${BLUE}$INSTALL_DIR${NC}"
    echo -e "  Config file:  ${BLUE}$INSTALL_DIR/data/creds.yml${NC}"
    echo ""
    echo -e "  ${YELLOW}Next steps:${NC}"
    echo -e "  1. Edit config:  ${CYAN}sudo nano $INSTALL_DIR/data/creds.yml${NC}"
    echo -e "     → Add your Discord bot token and owner ID"
    echo -e "  2. Start bot:    ${CYAN}sudo systemctl start santibot${NC}"
    echo -e "  3. Check status: ${CYAN}sudo systemctl status santibot${NC}"
    echo -e "  4. View logs:    ${CYAN}journalctl -u santibot -f${NC}"
    echo ""
}

# ── 2) Update ──────────────────────────────────────────────────
do_update() {
    check_root
    detect_arch

    if [ ! -d "$INSTALL_DIR" ]; then
        fatal "SantiBot is not installed. Run option 1 first."
    fi

    echo ""
    info "Updating SantiBot..."

    # Auto-backup before update
    do_backup_silent

    # Stop service
    systemctl stop santibot 2>/dev/null || true
    info "Bot stopped for update"

    LATEST=$(curl -s "https://api.github.com/repos/$REPO/releases/latest" | grep -oP '"tag_name": "\K[^"]+' || echo "")
    if [ -z "$LATEST" ]; then
        warn "No release found — pulling latest source"
        if [ -d "$INSTALL_DIR/.git" ]; then
            cd "$INSTALL_DIR" && git pull 2>/dev/null
        else
            fatal "No release and no git repo. Reinstall with option 1."
        fi
    else
        info "Downloading $LATEST..."
        DOWNLOAD_URL="https://github.com/$REPO/releases/download/$LATEST/santi-${RUNTIME}.tar.gz"
        curl -L -o /tmp/santibot.tar.gz "$DOWNLOAD_URL" 2>/dev/null
        # Preserve data directory
        tar -xzf /tmp/santibot.tar.gz -C "$INSTALL_DIR" --strip-components=1 --exclude='data'
        rm -f /tmp/santibot.tar.gz
        chmod +x "$INSTALL_DIR/SantiBot"
        ok "Updated to $LATEST"
    fi

    chown -R "$SERVICE_USER:$SERVICE_USER" "$INSTALL_DIR"
    systemctl start santibot
    ok "Bot restarted"

    echo ""
    echo -e "  ${GREEN}Update complete!${NC} Backup saved before update."
    echo ""
}

# ── 3) Backup ──────────────────────────────────────────────────
do_backup() {
    echo ""
    info "Creating backup..."
    do_backup_silent
    echo ""
    echo -e "  ${GREEN}Backup complete!${NC}"
    echo ""
}

do_backup_silent() {
    mkdir -p "$BACKUP_DIR"
    TIMESTAMP=$(date +%Y%m%d-%H%M%S)
    BACKUP_FILE="$BACKUP_DIR/santibot-backup-$TIMESTAMP.tar.gz"

    if [ -d "$INSTALL_DIR/data" ]; then
        tar -czf "$BACKUP_FILE" -C "$INSTALL_DIR" data/ 2>/dev/null
        ok "Backup saved: $BACKUP_FILE"

        # Keep only last 10 backups
        ls -t "$BACKUP_DIR"/santibot-backup-*.tar.gz 2>/dev/null | tail -n +11 | xargs rm -f 2>/dev/null || true
    else
        warn "No data directory found to backup"
    fi
}

# ── 4) Restore ─────────────────────────────────────────────────
do_restore() {
    check_root

    echo ""
    info "Available backups:"
    echo ""

    if [ ! -d "$BACKUP_DIR" ] || [ -z "$(ls -A $BACKUP_DIR 2>/dev/null)" ]; then
        warn "No backups found in $BACKUP_DIR"
        return
    fi

    local i=1
    declare -a backups
    while IFS= read -r file; do
        local size=$(du -h "$file" | cut -f1)
        local date=$(basename "$file" | sed 's/santibot-backup-//;s/.tar.gz//')
        echo -e "  ${GREEN}$i)${NC} $date ($size)"
        backups[$i]="$file"
        ((i++))
    done < <(ls -t "$BACKUP_DIR"/santibot-backup-*.tar.gz 2>/dev/null)

    echo ""
    echo -ne "  ${BOLD}Enter backup number to restore (or 'q' to cancel): ${NC}"
    read -r choice

    if [[ "$choice" == "q" ]] || [[ -z "$choice" ]]; then
        info "Restore cancelled."
        return
    fi

    local backup_file="${backups[$choice]:-}"
    if [ -z "$backup_file" ] || [ ! -f "$backup_file" ]; then
        err "Invalid selection."
        return
    fi

    warn "This will overwrite current data. Continue? [y/N]"
    read -r confirm
    if [[ ! "$confirm" =~ ^[Yy]$ ]]; then
        info "Restore cancelled."
        return
    fi

    systemctl stop santibot 2>/dev/null || true
    tar -xzf "$backup_file" -C "$INSTALL_DIR" 2>/dev/null
    chown -R "$SERVICE_USER:$SERVICE_USER" "$INSTALL_DIR"
    systemctl start santibot 2>/dev/null || true
    ok "Restored from $(basename "$backup_file")"
    echo ""
}

# ── 5) Service Management ─────────────────────────────────────
do_service() {
    echo ""
    local status=$(systemctl is-active santibot 2>/dev/null || echo "not installed")
    echo -e "  Current status: ${BOLD}$status${NC}"
    echo ""
    echo -e "  ${GREEN}1)${NC} Start"
    echo -e "  ${GREEN}2)${NC} Stop"
    echo -e "  ${GREEN}3)${NC} Restart"
    echo -e "  ${GREEN}4)${NC} Status (detailed)"
    echo -e "  ${GREEN}q)${NC} Back"
    echo ""
    echo -ne "  ${BOLD}Choice: ${NC}"
    read -r choice

    case "$choice" in
        1) systemctl start santibot && ok "Started" ;;
        2) systemctl stop santibot && ok "Stopped" ;;
        3) systemctl restart santibot && ok "Restarted" ;;
        4) systemctl status santibot --no-pager ;;
        *) return ;;
    esac
    echo ""
}

# ── 6) View Logs ──────────────────────────────────────────────
do_logs() {
    echo ""
    info "Showing live logs (Ctrl+C to exit)..."
    echo ""
    journalctl -u santibot -f --no-pager
}

# ── 7) Edit Config ────────────────────────────────────────────
do_config() {
    if [ ! -f "$INSTALL_DIR/data/creds.yml" ]; then
        fatal "Config file not found. Install SantiBot first."
    fi

    local editor="${EDITOR:-nano}"
    $editor "$INSTALL_DIR/data/creds.yml"

    echo ""
    echo -ne "  Restart bot to apply changes? [Y/n]: "
    read -r restart
    if [[ ! "$restart" =~ ^[Nn]$ ]]; then
        systemctl restart santibot 2>/dev/null && ok "Bot restarted with new config"
    fi
}

# ── 8) Install Dashboard ─────────────────────────────────────
do_dashboard() {
    check_root

    echo ""
    info "Installing SantiBot Web Dashboard..."

    if ! command -v node &>/dev/null; then
        info "Installing Node.js..."
        curl -fsSL https://deb.nodesource.com/setup_20.x | bash - > /dev/null 2>&1
        apt-get install -y -qq nodejs > /dev/null 2>&1
        ok "Node.js installed"
    else
        ok "Node.js already installed ($(node --version))"
    fi

    if [ ! -d "$INSTALL_DIR/dashboard" ]; then
        info "Downloading dashboard..."
        TMPDIR=$(mktemp -d)
        git clone --depth 1 -b "$BRANCH" "https://github.com/$REPO.git" "$TMPDIR/SantiBot" 2>/dev/null
        cp -r "$TMPDIR/SantiBot/dashboard" "$INSTALL_DIR/dashboard"
        rm -rf "$TMPDIR"
    fi

    cd "$INSTALL_DIR/dashboard"
    info "Installing dependencies..."
    npm install --production > /dev/null 2>&1
    ok "Dependencies installed"

    info "Building dashboard..."
    npm run build > /dev/null 2>&1
    ok "Dashboard built"

    # Create dashboard systemd service
    cat > /etc/systemd/system/santibot-dashboard.service << 'DASHEOF'
[Unit]
Description=SantiBot Web Dashboard
After=network.target santibot.service

[Service]
Type=simple
User=santibot
Group=santibot
WorkingDirectory=/opt/santibot/dashboard
ExecStart=/usr/bin/node .next/standalone/server.js
Environment=PORT=3000
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
DASHEOF
    systemctl daemon-reload
    systemctl enable santibot-dashboard
    systemctl start santibot-dashboard
    ok "Dashboard service installed and started"

    echo ""
    echo -e "  ${GREEN}Dashboard installed!${NC}"
    echo -e "  URL: ${CYAN}http://$(hostname -I | awk '{print $1}'):3000${NC}"
    echo ""
}

# ── 9) System Info ────────────────────────────────────────────
do_sysinfo() {
    echo ""
    echo -e "  ${BOLD}System Information${NC}"
    echo -e "  ─────────────────────────────────────"
    echo -e "  OS:           $(lsb_release -ds 2>/dev/null || cat /etc/os-release 2>/dev/null | grep PRETTY | cut -d= -f2 | tr -d '"' || echo 'Unknown')"
    echo -e "  Kernel:       $(uname -r)"
    echo -e "  Architecture: $(uname -m)"
    echo -e "  CPU:          $(nproc) cores"
    echo -e "  RAM:          $(free -h | awk '/Mem:/ {print $2}') total, $(free -h | awk '/Mem:/ {print $7}') available"
    echo -e "  Disk:         $(df -h / | awk 'NR==2 {print $4}') free"
    echo ""
    echo -e "  ${BOLD}Dependencies${NC}"
    echo -e "  ─────────────────────────────────────"
    echo -e "  .NET:         $(dotnet --version 2>/dev/null || echo '${RED}not installed${NC}')"
    echo -e "  Node.js:      $(node --version 2>/dev/null || echo '${RED}not installed${NC}')"
    echo -e "  ffmpeg:       $(ffmpeg -version 2>/dev/null | head -1 | awk '{print $3}' || echo '${RED}not installed${NC}')"
    echo -e "  yt-dlp:       $(yt-dlp --version 2>/dev/null || echo '${RED}not installed${NC}')"
    echo -e "  git:          $(git --version 2>/dev/null | awk '{print $3}' || echo '${RED}not installed${NC}')"
    echo ""
    echo -e "  ${BOLD}SantiBot${NC}"
    echo -e "  ─────────────────────────────────────"
    if [ -d "$INSTALL_DIR" ]; then
        echo -e "  Installed:    ${GREEN}Yes${NC} ($INSTALL_DIR)"
        echo -e "  Service:      $(systemctl is-active santibot 2>/dev/null || echo 'not configured')"
        echo -e "  Config:       $([ -f "$INSTALL_DIR/data/creds.yml" ] && echo '${GREEN}exists${NC}' || echo '${RED}missing${NC}')"
        echo -e "  Database:     $([ -f "$INSTALL_DIR/data/SantiBot.db" ] && echo "$(du -h "$INSTALL_DIR/data/SantiBot.db" | cut -f1)" || echo 'not created yet')"
        local backups=$(ls "$BACKUP_DIR"/santibot-backup-*.tar.gz 2>/dev/null | wc -l)
        echo -e "  Backups:      $backups saved"
    else
        echo -e "  Installed:    ${RED}No${NC}"
    fi
    echo ""
}

# ── 0) Uninstall ──────────────────────────────────────────────
do_uninstall() {
    check_root

    echo ""
    echo -e "  ${RED}${BOLD}WARNING: This will remove SantiBot completely!${NC}"
    echo -e "  This includes: bot files, service, and user account."
    echo -e "  Your data/config will be backed up first."
    echo ""
    echo -ne "  ${BOLD}Type 'REMOVE' to confirm: ${NC}"
    read -r confirm

    if [ "$confirm" != "REMOVE" ]; then
        info "Uninstall cancelled."
        return
    fi

    # Backup first
    do_backup_silent

    systemctl stop santibot 2>/dev/null || true
    systemctl disable santibot 2>/dev/null || true
    systemctl stop santibot-dashboard 2>/dev/null || true
    systemctl disable santibot-dashboard 2>/dev/null || true
    rm -f /etc/systemd/system/santibot.service
    rm -f /etc/systemd/system/santibot-dashboard.service
    systemctl daemon-reload
    rm -rf "$INSTALL_DIR"
    userdel "$SERVICE_USER" 2>/dev/null || true

    echo ""
    ok "SantiBot has been removed."
    echo -e "  Backups preserved at: ${BLUE}$BACKUP_DIR${NC}"
    echo ""
}

# ── Main Loop ──────────────────────────────────────────────────
main() {
    while true; do
        show_banner
        show_menu
        read -r choice

        case "$choice" in
            1) do_install ;;
            2) do_update ;;
            3) do_backup ;;
            4) do_restore ;;
            5) do_service ;;
            6) do_logs ;;
            7) do_config ;;
            8) do_dashboard ;;
            9) do_sysinfo ;;
            0) do_uninstall ;;
            q|Q) echo ""; echo -e "  ${CYAN}Goodbye! 🐕${NC}"; echo ""; exit 0 ;;
            *) warn "Invalid choice" ;;
        esac

        if [[ "$choice" != "6" ]]; then
            echo -ne "  Press Enter to continue..."
            read -r
        fi
    done
}

# If piped (curl | bash), run install directly
if [ ! -t 0 ]; then
    check_root
    do_install
else
    main
fi
