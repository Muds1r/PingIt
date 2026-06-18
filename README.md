# PingIt

A lightweight Windows network overlay that shows live **download**, **upload**, and **ping** on top of your screen. It runs in the **system tray** (near the clock/Wi‑Fi/battery icons), uses **click-through** so it never blocks games or apps, and only becomes draggable when you enable **Move overlay** from the tray menu.

**Repository:** [github.com/Muds1r/PingIt](https://github.com/Muds1r/PingIt)

---

## Features

| Feature | Description |
|---------|-------------|
| Live download / upload | Real-time Mbps from your network adapters |
| Ping | ICMP latency in ms (default host: `1.1.1.1`) |
| Click-through overlay | Mouse passes through — safe for gaming |
| System tray control | All settings from the icon near the clock |
| First-run setup | Choose which stats to show on first launch |
| Move overlay | Drag to reposition only when unlocked from tray |
| Transparency & text size | 35%–100% opacity, Small / Medium / Large |
| Start with Windows | Optional auto-launch on login |
| Low overhead | Cached NIC list, repaint only on change |
| Windows installer | Single `PingIt-Setup.exe` for end users |

---

## For end users

### Install

1. Download `PingIt-Setup-1.0.0.exe` from [Releases](https://github.com/Muds1r/PingIt/releases) (or build it yourself — see below).
2. Run the installer and finish the wizard.
3. On **first launch**, pick which stats to show (Download, Upload, Ping).
4. Drag the overlay where you want it, then open the **PingIt tray icon** → turn off **Move overlay**.

PingIt keeps running in the background. It does **not** appear on the taskbar — only in the **system tray**.

### Daily use

| Action | How |
|--------|-----|
| Change what’s shown | Tray icon → **Show** → tick/untick stats |
| Move the overlay | Tray icon → enable **Move overlay**, drag, then disable |
| Transparency / text size | Tray icon → **Transparency** or **Text size** |
| Hide overlay | Tray icon → untick **Show overlay** |
| Show overlay again | Double-click tray icon, or tick **Show overlay** |
| Start on boot | Tray icon → **Start with Windows** |
| Quit completely | Tray icon → **Exit** |

### Overlay display

Each stat is on **its own line**:

```
▼   12.5 Mbps    ← Download
▲    3.2 Mbps    ← Upload
●     24 ms      ← Ping
```

When **Move overlay** is on, a dashed blue border appears so you know the overlay is unlocked.

### Settings file

`%AppData%\PingIt\settings.json`

```json
{
  "X": 20,
  "Y": 20,
  "PingHost": "1.1.1.1",
  "TextSize": 1,
  "Opacity": 0.85,
  "ShowDownload": true,
  "ShowUpload": true,
  "ShowPing": true,
  "StartWithWindows": false,
  "SetupCompleted": true
}
```

---

## Requirements

- **Windows 10 or 11**
- ICMP ping allowed (for the ping line)
- **.NET 8 SDK** only if building from source — end users do **not** need it when using the installer

> WinForms + `net8.0-windows` — **Windows only**. Cannot run natively on macOS/Linux.

---

## Build from source

### Run in development

```powershell
git clone https://github.com/Muds1r/PingIt.git
cd PingIt
dotnet run --project PingIt/PingIt.csproj
```

### Release build

```powershell
dotnet build PingIt.sln -c Release
```

Output: `PingIt\bin\Release\net8.0-windows\PingIt.exe`

### Build installer (setup.exe)

On Windows, with [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) and [Inno Setup 6](https://jrsoftware.org/isdl.php):

```powershell
.\scripts\build-installer.ps1
```

Output: `dist\installer\PingIt-Setup-1.0.0.exe`

---

## Project structure

```
PingIt/
├── PingIt.sln
├── README.md
├── installer/PingIt.iss
├── scripts/build-installer.ps1
└── PingIt/
    ├── Program.cs              # Entry, first-run wizard, single instance
    ├── OverlayForm.cs          # Overlay window + timers
    ├── OverlayRenderer.cs      # Drawing
    ├── OverlayMenu.cs          # Settings menu (used by tray)
    ├── TrayHost.cs             # System tray icon + click-through control
    ├── SetupWizardForm.cs      # First-run stat picker
    ├── MonitorSession.cs       # Coordinates network + ping monitors
    ├── NetworkMonitor.cs       # Mbps from adapter counters
    ├── PingMonitor.cs          # Async ICMP ping
    ├── AppSettings.cs          # Persistent JSON settings
    ├── AppConstants.cs
    ├── MetricFormatter.cs
    ├── TextSize.cs
    ├── StartupHelper.cs        # Windows Run registry (boot startup)
    └── Win32Window.cs          # Topmost + click-through Win32 APIs
```

---

## How it works

- **Speed** — Every second, reads IPv4 byte counters from active adapters (skips loopback and most virtual NICs), computes Mbps delta.
- **Ping** — Every 3 seconds, ICMP ping to `PingHost` (2 s timeout).
- **Click-through** — `WS_EX_TRANSPARENT` when overlay is locked; removed when **Move overlay** or tray menu is open.
- **Tray** — App stays alive in background; closing the overlay hides it unless you use **Exit**.

---

## Troubleshooting

| Issue | Fix |
|-------|-----|
| Can't find PingIt | Click the **^** arrow in the taskbar to show hidden tray icons |
| Can't click the overlay | Normal — it's click-through. Use tray → **Move overlay** |
| Ping shows `— ms` | Firewall/VPN blocking ICMP; edit `PingHost` in settings JSON |
| Speed is `0.00 Mbps` | Normal when idle; first second is always 0 |
| Two copies running | Only one instance allowed; second launch is ignored |

---

## License

MIT (add a `LICENSE` file if you publish formally)

---

## Roadmap

- [ ] Ping host picker in tray menu
- [ ] Custom app icon
- [ ] GitHub Actions — auto-build installer on release
- [ ] Per-adapter selection
- [ ] Color-coded ping (green / yellow / red)
