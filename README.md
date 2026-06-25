# ⚡ WattWise

> Windows desktop app that monitors your PC's real-time power draw, calculates electricity cost, and helps you lower your energy bill.

[![Built with ByJT UI](https://img.shields.io/badge/UI-ByJT-indigo)](https://ui.byjtt.com)
[![Astro](https://img.shields.io/badge/Astro-6-black)](https://astro.build)
[![Tauri](https://img.shields.io/badge/Tauri-2-blue)](https://tauri.app)
[![.NET](https://img.shields.io/badge/.NET-8-purple)](https://dotnet.microsoft.com)

## Features

### Free Forever
- **Live power gauge** — real-time CPU + GPU wattage meter with arc gradient
- **Cost per hour** — see exactly what your PC costs to run right now
- **Power breakdown** — CPU vs GPU vs system draw as a stacked bar
- **24h sparkline** — power usage trend built from live sensor data
- **24h history** — hourly data with CSV export

### Pro (£4.99 one-time)
- **Optimization engine** — AI-powered suggestions to reduce idle waste and save money
- **Idle waste detection** — tracks idle hours and calculates wasted cost
- **7d/30d full history** — extended charts and sortable data tables
- **Week/month/year estimates** — cost projections over time
- **One-click fixes** — apply powercfg optimizations directly from the app
- **7-day free trial** — try everything before buying

## System Requirements

| Requirement | Minimum |
|---|---|
| OS | Windows 10 or Windows 11 (x64) |
| Runtime | .NET 8 Runtime (included in installer) |
| Permissions | **Administrator rights required** (to read hardware sensors) |
| CPU | Any x64 processor with power sensors (Intel Core / AMD Ryzen) |
| GPU | NVIDIA (recommended) or AMD with power reporting |
| Display | 800×600 minimum |

## How It Works

WattWise reads CPU Package Power and GPU Power sensors directly from your hardware using LibreHardwareMonitorLib.

```
Total System Watts = CPU + GPU + Base (45W default, adjustable)

Cost = (Watts / 1000) × Rate × Hours
```

0W readings usually mean the app isn't running as administrator. Use the "Relaunch as Administrator" button that appears on first launch.

## Install

### Option 1: MSI Installer (Recommended)

1. Download `WattWise_1.0.0_x64_en-US.msi` from the [latest release](https://github.com/jordan-thirkle/wattwise/releases/latest)
2. Run the installer — accepts the UAC prompt when it appears
3. WattWise launches automatically after install

### Option 2: Portable

1. Download `wattwise.exe` from the [latest release](https://github.com/jordan-thirkle/wattwise/releases/latest)
2. Right-click → **Run as administrator**
3. No install needed — runs from any folder

### Option 3: Setup.exe

1. Download `WattWise_1.0.0_x64-setup.exe` from the [latest release](https://github.com/jordan-thirkle/wattwise/releases/latest)
2. Run and follow the prompts

## Licence

WattWise is **free forever** with live monitoring, cost/hour, and 24h history. Pro unlocks the full toolkit.

| Tier | Price | Features |
|---|---|---|
| **Free** | £0 | Live gauge, cost/hr, power breakdown, 24h sparkline, CSV export |
| **Pro** | £4.99 one-time | Everything in Free + optimization engine, 7d/30d history, idle waste detection, week/month/year estimates, one-click fixes |

**To go Pro:**
1. Start your **7-day free trial** from the licence modal (full Pro features, no payment needed)
2. Purchase from [Gumroad](https://jordanthirkle.gumroad.com/l/wattwise) — **£4.99 one-time**
3. Paste your licence key in Settings → Licence → Activate

**Offline use:** Once activated, WattWise trusts your locally-stored licence. No internet required after activation.

## Tech Stack

| Layer | Tech |
|---|---|
| Desktop Shell | Tauri v2 (Rust) |
| Frontend | Astro 6 + Tailwind CSS |
| Design System | [ByJT UI](https://ui.byjtt.com) |
| Sensor Reader | LibreHardwareMonitorLib (.NET 8) |
| Database | SQLite |
| Charts | SVG (Canvas-free) |

## Architecture

```
wattwise.exe
├── Frontend: Astro + ByJT UI (embedded web UI)
├── Tauri Bridge (Rust, IPC + sidecar management)
└── Sidecar: wattwise-sensor.exe (.NET 8)
    ├── LibreHardwareMonitorLib → CPU/GPU power sensors
    ├── SQLite → history storage
    ├── CostCalculator → wattage × rate × time
    ├── LicenseService → trial tracking + Gumroad validation
    └── REST API (localhost:45892)
```

## Development

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 22+](https://nodejs.org)
- [Rust](https://rustup.rs) (MSVC toolchain on Windows)
- [Visual Studio 2022 Build Tools](https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022) (C++ workload)

### Quick Start

```bash
git clone https://github.com/jordan-thirkle/wattwise.git
cd wattwise

# Install frontend deps
cd frontend && npm install && cd ..

# Run in dev mode
cargo tauri dev
```

### Build

```bash
cargo tauri build
```

Produces three artifacts:
- `wattwise.exe` — portable executable
- `WattWise_1.0.0_x64_en-US.msi` — Windows installer
- `WattWise_1.0.0_x64-setup.exe` — self-extracting installer

## Settings

| Setting | Default | Description |
|---|---|---|
| Rate (p/kWh) | 23.09 | Your electricity unit rate (check your bill) |
| Standing Charge (p/day) | 64.29 | Daily fixed charge from your energy provider |
| Currency | GBP | Display currency (£, $, €) |
| Base System Watts | 45 | Estimated draw from RAM, drives, fans, chipset |
| Data Retention | 90 days | How long to keep history before auto-purge |

## Roadmap

- [x] Live power monitoring with auto-spawning sidecar
- [x] Cost per hour + standing charge tracking
- [x] 24h/7d/30d history with CSV export
- [x] Optimization suggestions with one-click fixes
- [x] 7-day trial + Gumroad licence validation
- [x] Administrator rights detection + elevation
- [ ] macOS support (battery charge cost per cycle)
- [ ] Smart plug integration (wall power vs sensor estimate)
- [ ] Cloud sync (opt-in, encrypted)
- [ ] Home Assistant integration

## Links

- [byjtt.com/projects](https://byjtt.com/projects)
- [ui.byjtt.com](https://ui.byjtt.com) — design system
- [Buy Licence](https://jordanthirkle.gumroad.com/l/wattwise) — £4.99 one-time
- [x.com/jordanthirkle](https://x.com/jordanthirkle)
- [Report an issue](https://github.com/jordan-thirkle/wattwise/issues)
