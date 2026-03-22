#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

mod commands;

fn main() {
    tauri::Builder::default()
        .plugin(tauri_plugin_shell::init())
        .invoke_handler(tauri::generate_handler![
            commands::validate_token,
            commands::check_dependencies,
            commands::install_dependency,
            commands::install_local,
            commands::install_docker,
            commands::check_update,
            commands::get_system_info,
        ])
        .run(tauri::generate_context!())
        .expect("error while running SantiBot Installer");
}
