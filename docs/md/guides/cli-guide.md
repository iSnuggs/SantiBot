# SantiBot CLI Guide (via Bash Installer)

### Supported Operating Systems

--8<-- "md/snippets/supported-platforms.md:linux"
--8<-- "md/snippets/supported-platforms.md:macos"

### Prerequisites

macOS:

- [Homebrew](https://brew.sh/)
- [Curl](#__tabbed_1_5)

Linux:

- [Curl](#__tabbed_1_1)

---

??? note "24/7 Up-time via VPS (Digital Ocean Guide)"
    --8<-- "md/guides/vps-linux-guide.md"

??? note "Creating a Discord Bot & Getting Credentials"
    --8<-- "md/creds-guide.md"

---

## Installation Instructions

Open Terminal (if you're on an installation with a window manager) and navigate to the location where you want to install the bot (for example `cd ~`)

1. First make sure that curl is installed

    /// tab | Ubuntu | Debian | Mint

    ```bash
    sudo apt install curl
    ```

    ///
    /// tab | Rocky | Alma | Fedora

    ```bash
    sudo dnf install curl
    ```

    ///
    /// tab | openSUSE

    ```bash
    sudo zypper install curl
    ```

    ///
    /// tab | Arch | Artix

    ```bash
    sudo pacman -S curl
    ```

    ///
    /// tab | macOS

    ```bash
    brew install curl
    ```

    ///

2. Download and run the **new** installer script
    ``` sh
    cd ~
    curl -L -o n-install.sh https://raw.githubusercontent.com/santi-bot/bash-installer/refs/heads/v6/n-install.sh
    bash n-install.sh
    ```
3. Install the bot (type `1` and press enter)
4. Edit creds (type `3` and press enter)
    - *ALTERNATIVELY*, you can exit the installer (option `6`) and edit `santi/creds.yml` file yourself
5. Follow the instruction [below](#creating-your-own-discord-bot) to create your own Discord bot and obtain the credentials needed to run it.
    - After you're done, you can close nano (and save the file) by inputting, in order:
        - `CTRL` + `X`
        - `Y`
        - `Enter`
6. Run the installer script again
    - `bash n-install.sh`
7. Run the bot (type `3` and press enter)
8. Done!

## Update Instructions

1. ⚠ Stop the bot ⚠
2. Navigate to your bot's folder, we'll use home directory as an example
    - `cd ~`
3. Simply re-install the bot with a newer version by running the installer script
    - `curl -L -o n-install.sh https://raw.githubusercontent.com/santi-bot/bash-installer/refs/heads/v6/n-install.sh && bash n-install.sh`
4. Select option 1, and select a NEWER version

## Running Santi

There are two main methods to run SantiBot: using `tmux` (macOS and Linux) or using `systemd` with a script (Linux only).

/// tab | Tmux (Preferred Method)

Using `tmux` is the simplest method, and is therefore recommended for most users.

!!! warning
    Before proceeding, make sure your bot is not currently running by either running `.die` in your Discord server or exiting the process with `Ctrl+C`.

1. Access the directory where `n-install.sh` and `santi` is located.
2. Create a new tmux session: `tmux new -s santi`
    - The above command will create a new session named **santi**. You may replace **santi** with any name you prefer.
3. Run the installer: `bash n-install.sh`
4. Start the bot by typing `3` and pressing `Enter`.
5. Detach from the tmux session, allowing the bot to run in the background:
    - Press `Ctrl` + `B`
    - Then press `D`

Now check your Discord server, the bot should be online. Santi should now be running in the background of your system.

To re-open the tmux session to either update, restart, or whatever, execute `tmux a -t santi`. *(Make sure to replace "santi" with your session name. If you didn't change it, leave it as it is.)*

///
/// tab | Systemd

!!! note
    Systemd is only available on Linux. macOS utilizes Launchd, which is not covered in this guide. If you're on macOS, please use the `tmux` method, or use [Upeko](desktop-guide.md) to run SantiBot.

This method is a bit more complex and involved, but comes with the added benefit of better error logging and control over what happens before and after the startup of Santi.

1. Access the directory where `n-install.sh` and `santi` is located.
2. Use the following command to create a service that will be used to execute `SantiRun.bash`:
    ```bash
    echo "[Unit]
    Description=SantiBot service
    After=network.target
    StartLimitIntervalSec=60
    StartLimitBurst=2

    [Service]
    Type=simple
    User=$USER
    WorkingDirectory=$PWD
    ExecStart=/bin/bash SantiRun.bash
    #ExecStart=./santi/SantiBot
    Restart=on-failure
    RestartSec=5
    StandardOutput=journal
    StandardError=journal
    SyslogIdentifier=SantiBot

    [Install]
    WantedBy=multi-user.target" | sudo tee /etc/systemd/system/santi.service
    ```
3. Make the new service available: `sudo systemctl daemon-reload`
4. Use the following command to create a script that will be used to start Santi:
    ```bash
    cat <<EOF > SantiRun.bash
    #!/bin/bash

    export PATH="$HOME/.local/bin:$PATH"

    is_python3_installed=\$(command -v python3 &>/dev/null && echo true || echo false)
    is_yt_dlp_installed=\$(command -v yt-dlp &>/dev/null && echo true || echo false)

    [[ \$is_python3_installed == true ]] \\
        && echo "[INFO] python3 path: \$(which python3)" \\
        && echo "[INFO] python3 version: \$(python3 --version)"
    [[ \$is_yt_dlp_installed == true ]] \\
        && echo "[INFO] yt-dlp path: \$(which yt-dlp)"

    echo "[INFO] Running SantiBot in the background with auto restart"
    if [[ \$is_yt_dlp_installed == true ]]; then
        yt-dlp -U || echo "[ERROR] Failed to update 'yt-dlp'" >&2
    fi

    echo "[INFO] Starting SantiBot..."

    while true; do
        if [[ -d $PWD/santi ]]; then
            cd "$PWD/santi" || {
                echo "[ERROR] Failed to change working directory to '$PWD/santi'" >&2
                echo "[INFO] Exiting..."
                exit 1
            }
        else
            echo "[WARN] '$PWD/santi' doesn't exist" >&2
            echo "[INFO] Exiting..."
            exit 1
        fi

        ./SantiBot || {
            echo "[ERROR] An error occurred when trying to start SantiBot" >&2
            echo "[INFO] Exiting..."
            exit 1
        }

        echo "[INFO] Waiting 5 seconds..."
        sleep 5
        if [[ \$is_yt_dlp_installed == true ]]; then
            yt-dlp -U || echo "[ERROR] Failed to update 'yt-dlp'" >&2
        fi
        echo "[INFO] Restarting SantiBot..."
    done

    echo "[INFO] Stopping SantiBot..."
    EOF
    ```

With everything set up, you can run SantiBot in one of three modes:

1. **Auto-Restart Mode**: SantiBot will restart automatically if you restart it via the `.die` command.
    - To enable this mode, start the service: `sudo systemctl start santi`
2. **Auto-Restart on Reboot Mode**: In addition to auto-restarting after `.die`, SantiBot will also start automatically on system reboot.
    - To enable this mode, run:
        ```bash
        sudo systemctl enable santi
        sudo systemctl start santi
        ```
3. **Standard Mode**: SantiBot will stop completely when you use `.die`, without restarting automatically.
    - To switch to this mode:
        1. Stop the service: `sudo systemctl stop santi`
        2. Edit the service file: `sudo <editor> /etc/systemd/system/santi.service`
        3. Modify the `ExecStart` line:
            - **Comment out**: `ExecStart=/bin/bash SantiRun.bash`
            - **Uncomment**: `#ExecStart=./santi/SantiBot`
        4. Save and exit the editor.
        5. Reload systemd: `sudo systemctl daemon-reload`
        6. Disable automatic startup: `sudo systemctl disable santi`
        7. Start SantiBot manually: `sudo systemctl start santi`

///
