# Changelog

All notable changes to WattWise are documented in this file.

---

## v1.0.0 (2026-06-24)

### Added

**Live Power Monitoring (Phase 1)**
- Real-time power gauge displaying total system wattage with arc gradient (green→orange→red)
- Cost-per-hour counter showing live electricity cost in configurable currency
- Power breakdown bar: CPU (indigo) / GPU (emerald) / System (translucent)
- System info cards: CPU/GPU temp and load percentages
- 24-hour sparkline chart built incrementally from live sensor data
- Auto-spawning .NET sidecar via Tauri shell plugin — no manual backend start needed
- "Sensor Offline" overlay with pulsing indicator when backend is unreachable

**Cost Analytics (Phase 2)**
- Today / Yesterday / This Week / Month Estimate / Year Estimate stat cards
- Standing charge tracker (p/day from your energy bill)
- Hourly history bar chart with colour coding (indigo <150W, orange <300W, red ≥300W)
- Sortable history table: time, avg watts, peak watts, CPU/GPU avg, kWh, cost
- CSV export for full 30-day history
- 24h / 7d / 30d period selectors

**Optimization Engine (Phase 3)**
- Idle waste detection — tracks idle hours and calculates wasted cost
- Background app detection — flags high-power moments during idle periods
- Tariff comparison — suggests checking rates when above UK average
- Undervolting recommendation when average draw exceeds 200W
- One-click powercfg fixes (hibernate timers, etc.)
- Monthly savings estimates per suggestion

**Licence System**
- 7-day free trial with countdown timer
- Gumroad licence key validation (`POST /api/license/validate`)
- Offline fallback: trusts locally-stored active licence when Gumroad is unreachable
- Trial expiry enforces read-only mode (historical data accessible, live monitoring locked)
- Licence status shown in Settings page

**Permissions**
- First-run elevation check — detects insufficient admin rights for hardware sensors
- "Relaunch as Administrator" button via Tauri command + UAC elevation
- No fallback to estimated values — prevents false "shows 0W" reviews

**Electronics**
- ByJT UI design system integration (dark theme, cards, stat cards, badges)
- Offline overlay with animated pulse indicator
- Licence modal (trial + expired states)
- Permissions modal

### Technical

- .NET 8 backend with LibreHardwareMonitorLib (CPU Package Power, GPU Power)
- SQLite storage (snapshots + hourly aggregation + settings key-value store)
- Tauri v2 desktop shell with shell plugin sidecar management
- Astro 6 + Tailwind CSS frontend with server-side rendering
- Single-command build: `cargo tauri build` produces portable .exe, .msi, and setup.exe
- PowerShell-compatible build pipeline (Rename-Item, not cmd.exe `ren`)

### Known Limitations

- Total system power is CPU + GPU + 45W base estimate (adjustable in settings). True wall power requires a smart plug.
- Hardware sensor readings require administrator rights on Windows.
- GPU power sensor naming varies by vendor — may show 0W on some AMD or Intel GPUs.
- macOS support planned for a future release (battery charge cost tracking).
