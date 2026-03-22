use serde::{Deserialize, Serialize};
use std::process::Command;

#[derive(Serialize, Deserialize)]
pub struct SystemInfo {
    pub os: String,
    pub arch: String,
    pub dotnet_installed: bool,
    pub dotnet_version: Option<String>,
    pub ffmpeg_installed: bool,
    pub ytdlp_installed: bool,
    pub docker_installed: bool,
    pub docker_version: Option<String>,
}

#[derive(Serialize, Deserialize)]
pub struct TokenValidation {
    pub valid: bool,
    pub username: Option<String>,
    pub discriminator: Option<String>,
    pub error: Option<String>,
}

#[derive(Serialize, Deserialize)]
pub struct InstallResult {
    pub success: bool,
    pub message: String,
}

#[derive(Serialize, Deserialize)]
pub struct UpdateInfo {
    pub current_version: String,
    pub latest_version: Option<String>,
    pub update_available: bool,
    pub download_url: Option<String>,
}

fn run_command(cmd: &str, args: &[&str]) -> Option<String> {
    Command::new(cmd)
        .args(args)
        .output()
        .ok()
        .and_then(|o| {
            if o.status.success() {
                String::from_utf8(o.stdout).ok()
            } else {
                None
            }
        })
        .map(|s| s.trim().to_string())
}

#[tauri::command]
pub fn get_system_info() -> SystemInfo {
    let os = std::env::consts::OS.to_string();
    let arch = std::env::consts::ARCH.to_string();

    let dotnet_version = run_command("dotnet", &["--version"]);
    let docker_version = run_command("docker", &["--version"]);

    SystemInfo {
        os,
        arch,
        dotnet_installed: dotnet_version.is_some(),
        dotnet_version,
        ffmpeg_installed: run_command("ffmpeg", &["-version"]).is_some(),
        ytdlp_installed: run_command("yt-dlp", &["--version"]).is_some(),
        docker_installed: docker_version.is_some(),
        docker_version,
    }
}

#[tauri::command]
pub async fn validate_token(token: String) -> TokenValidation {
    let client = reqwest::Client::new();
    let res = client
        .get("https://discord.com/api/v10/users/@me")
        .header("Authorization", format!("Bot {}", token))
        .send()
        .await;

    match res {
        Ok(response) => {
            if response.status().is_success() {
                if let Ok(json) = response.json::<serde_json::Value>().await {
                    return TokenValidation {
                        valid: true,
                        username: json["username"].as_str().map(String::from),
                        discriminator: json["discriminator"].as_str().map(String::from),
                        error: None,
                    };
                }
            }
            TokenValidation {
                valid: false,
                username: None,
                discriminator: None,
                error: Some("Invalid token".to_string()),
            }
        }
        Err(e) => TokenValidation {
            valid: false,
            username: None,
            discriminator: None,
            error: Some(format!("Connection error: {}", e)),
        },
    }
}

#[tauri::command]
pub fn check_dependencies() -> SystemInfo {
    get_system_info()
}

#[tauri::command]
pub async fn install_dependency(name: String) -> InstallResult {
    let result = match name.as_str() {
        "dotnet" => {
            if cfg!(target_os = "linux") {
                run_command("bash", &["-c", "curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 8.0"])
            } else if cfg!(target_os = "macos") {
                run_command("bash", &["-c", "curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 8.0"])
            } else {
                // Windows — user should download from Microsoft
                None
            }
        }
        "ffmpeg" => {
            if cfg!(target_os = "linux") {
                run_command("bash", &["-c", "sudo apt-get install -y ffmpeg 2>/dev/null || sudo yum install -y ffmpeg 2>/dev/null"])
            } else {
                None
            }
        }
        "yt-dlp" => {
            run_command("bash", &["-c", "pip install --user yt-dlp 2>/dev/null || curl -L https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp -o /usr/local/bin/yt-dlp && chmod a+rx /usr/local/bin/yt-dlp"])
        }
        _ => None,
    };

    InstallResult {
        success: result.is_some(),
        message: result.unwrap_or_else(|| format!("Failed to install {}", name)),
    }
}

#[tauri::command]
pub async fn install_local(install_path: String, token: String, prefix: String, owner_id: String) -> InstallResult {
    // Clone or download SantiBot
    let clone = run_command("git", &[
        "clone",
        "--branch", "develop",
        "https://github.com/iSnuggs/SantiBot.git",
        &install_path,
    ]);

    if clone.is_none() {
        return InstallResult {
            success: false,
            message: "Failed to clone SantiBot repository".to_string(),
        };
    }

    // Write creds.yml
    let creds_path = format!("{}/src/SantiBot/data/creds.yml", install_path);
    let creds_content = format!(
        "token: \"{}\"\nownerIds:\n  - {}\n",
        token, owner_id
    );

    if std::fs::write(&creds_path, &creds_content).is_err() {
        return InstallResult {
            success: false,
            message: "Failed to write credentials file".to_string(),
        };
    }

    InstallResult {
        success: true,
        message: format!("SantiBot installed to {}", install_path),
    }
}

#[tauri::command]
pub async fn install_docker(install_path: String, token: String, owner_id: String) -> InstallResult {
    // Create directory
    if std::fs::create_dir_all(&install_path).is_err() {
        return InstallResult {
            success: false,
            message: "Failed to create install directory".to_string(),
        };
    }

    // Write docker-compose.yml
    let compose = format!(
        r#"version: "3.8"

services:
  santibot:
    image: ghcr.io/isnuggs/santibot:latest
    container_name: santibot
    restart: unless-stopped
    volumes:
      - ./data:/app/data
    environment:
      - TZ=UTC
"#
    );

    let compose_path = format!("{}/docker-compose.yml", install_path);
    if std::fs::write(&compose_path, &compose).is_err() {
        return InstallResult {
            success: false,
            message: "Failed to write docker-compose.yml".to_string(),
        };
    }

    // Write creds.yml
    let data_path = format!("{}/data", install_path);
    let _ = std::fs::create_dir_all(&data_path);
    let creds_content = format!(
        "token: \"{}\"\nownerIds:\n  - {}\n",
        token, owner_id
    );
    let creds_path = format!("{}/creds.yml", data_path);
    if std::fs::write(&creds_path, &creds_content).is_err() {
        return InstallResult {
            success: false,
            message: "Failed to write credentials".to_string(),
        };
    }

    // Run docker compose
    let up = run_command("docker", &["compose", "-f", &compose_path, "up", "-d"]);

    InstallResult {
        success: up.is_some(),
        message: if up.is_some() {
            "SantiBot is running in Docker!".to_string()
        } else {
            "Docker compose failed. Make sure Docker is installed and running.".to_string()
        },
    }
}

#[tauri::command]
pub async fn check_update() -> UpdateInfo {
    let client = reqwest::Client::new();
    let res = client
        .get("https://api.github.com/repos/iSnuggs/SantiBot/releases/latest")
        .header("User-Agent", "SantiBot-Installer")
        .send()
        .await;

    let current = "1.0.0".to_string();

    match res {
        Ok(response) => {
            if let Ok(json) = response.json::<serde_json::Value>().await {
                let latest = json["tag_name"]
                    .as_str()
                    .unwrap_or("unknown")
                    .trim_start_matches('v')
                    .to_string();

                let download_url = json["assets"]
                    .as_array()
                    .and_then(|a| a.first())
                    .and_then(|a| a["browser_download_url"].as_str())
                    .map(String::from);

                return UpdateInfo {
                    update_available: latest != current,
                    current_version: current,
                    latest_version: Some(latest),
                    download_url,
                };
            }
            UpdateInfo {
                current_version: current,
                latest_version: None,
                update_available: false,
                download_url: None,
            }
        }
        Err(_) => UpdateInfo {
            current_version: current,
            latest_version: None,
            update_available: false,
            download_url: None,
        },
    }
}
