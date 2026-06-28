# Building SlayTheSpire2.LAN.Multiplayer

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Slay the Spire 2 installed (the game DLLs are used as references)

## Setup

The project references four DLLs from the game's installation directory. By default the `.csproj` expects them at:

```
C:\path_to_game\data_sts2_windows_x86_64\
```

If your game is installed elsewhere, edit `SlayTheSpire2.LAN.Multiplayer\SlayTheSpire2.LAN.Multiplayer.csproj` and update the `HintPath` values for the following references to point to your actual install path:

- `0Harmony.dll`
- `GodotSharp.dll`
- `Steamworks.NET.dll`
- `sts2.dll`

## Build

From the repository root, run:

```
dotnet build SlayTheSpire2.LAN.Multiplayer.sln --configuration Release
```

The compiled output will be placed in:

```
SlayTheSpire2.LAN.Multiplayer\bin\Release\net9.0\
```

## Install

Copy the built mod files into the game's `mods` folder, then place the `mods` folder in the game directory.

Alternatively, download a pre-built release archive, extract it, and copy the resulting `mods` folder into the game directory.
