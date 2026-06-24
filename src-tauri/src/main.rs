use tauri::Manager;
use tauri_plugin_shell::ShellExt;

#[tauri::command]
fn relaunch_admin(app: tauri::AppHandle) {
    let exe = std::env::current_exe().unwrap_or_default();
    let exe_path = exe.to_string_lossy();
    std::process::Command::new("powershell")
        .args([
            "-Command",
            &format!("Start-Process -FilePath '{}' -Verb RunAs", exe_path),
        ])
        .spawn()
        .ok();
    app.exit(0);
}

fn main() {
    tauri::Builder::default()
        .plugin(tauri_plugin_shell::init())
        .setup(|app| {
            // Spawn sidecar — process dies when app handle is dropped
            app.shell()
                .sidecar("wattwise-sensor")
                .expect("failed to create sidecar command: wattwise-sensor binary not found")
                .spawn()
                .expect("failed to spawn wattwise-sensor sidecar");

            app.get_webview_window("main")
                .expect("no main window")
                .eval("window.__SIDECAR_SPAWNED__ = true;")
                .ok();

            Ok(())
        })
        .invoke_handler(tauri::generate_handler![relaunch_admin])
        .build(tauri::generate_context!())
        .expect("error while building tauri application")
        .run(|_app_handle, event| {
            if let tauri::RunEvent::Exit = event {
                // Sidecar child processes are killed when the app exits
                // via the shell plugin's internal process management
            }
        });
}
