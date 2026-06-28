# Building from Source

## Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9)
- Slay the Spire 2 installed (the mod references game DLLs)
- Windows (the default DLL paths are Windows paths)

## 1. Update DLL references

The project references game DLLs in `SlayTheSpire2.LAN.Multiplayer/SlayTheSpire2.LAN.Multiplayer.csproj`. If your STS2 install is not at `D:\Slay the Spire 2\`, create a `Directory.Build.props` file in the repo root to override the path:

```xml
<!-- Directory.Build.props  (do not commit — it's gitignored) -->
<Project>
  <PropertyGroup>
    <STS2GameDir>C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64</STS2GameDir>
  </PropertyGroup>
</Project>
```

The four DLLs you need are all in `<STS2 install dir>\data_sts2_windows_x86_64\`:

- `0Harmony.dll`
- `GodotSharp.dll`
- `Steamworks.NET.dll`
- `sts2.dll`

## 2. Build

```powershell
cd SlayTheSpire2.LAN.Multiplayer
dotnet build
```

Output: `SlayTheSpire2.LAN.Multiplayer/bin/Debug/net9.0/SlayTheSpire2.LAN.Multiplayer.dll`

## 3. Install into the game

The game must be closed before copying (it locks the DLL while running).

```powershell
$gameDir = "D:\Slay the Spire 2"
$modDir  = "$gameDir\mods\SlayTheSpire2.LAN.Multiplayer"
New-Item -ItemType Directory -Force $modDir
Copy-Item SlayTheSpire2.LAN.Multiplayer\bin\Debug\net9.0\SlayTheSpire2.LAN.Multiplayer.dll $modDir
Copy-Item SlayTheSpire2.LAN.Multiplayer\mod_manifest.json $modDir
```

Or manually: copy those two files into `mods\SlayTheSpire2.LAN.Multiplayer\` inside your STS2 directory.

## Project structure

```
SlayTheSpire2.LAN.Multiplayer/
├── SlayTheSpire2.LAN.Multiplayer/   # Mod source (C# library)
│   ├── Components/                  # UI components injected into game screens
│   ├── Helpers/                     # Handshake packet helpers
│   ├── Models/                      # Data models and settings
│   ├── Patchs/                      # Harmony patches (ENet, screens, messages)
│   ├── Services/                    # Player name, settings, run state services
│   └── mod_manifest.json
├── localization/                    # Translations (eng, deu, fra, esp, ...)
│   └── eng/
│       ├── main_menu_ui.json
│       ├── gameplay_ui.json
│       └── settings_ui.json
├── Screenshot/                      # Screenshots for README
└── docs/
    └── building.md                  # This file
```
