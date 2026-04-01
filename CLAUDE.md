# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A .NET 9.0 console application that reads and displays Xbox 360 controller input in real time using the Windows XInput API via P/Invoke.

## Build & Run

```bash
# Build
dotnet build X360Joystic/X360Joystic/X360Joystic.csproj

# Run
dotnet run --project X360Joystic/X360Joystic/X360Joystic.csproj

# Build release
dotnet build X360Joystic/X360Joystic/X360Joystic.csproj -c Release
```

## Backup

```powershell
# Creates a timestamped zip of source files (excludes bin/, obj/, .vs/) in the parent directory
.\backup.ps1
```

## Architecture

Single-file console app (`X360Joystic/X360Joystic/Program.cs`) with no external NuGet dependencies:

- **P/Invoke layer**: `XInputGetState` and `XInputSetState` imported directly from `xinput1_4.dll`
- **Structs**: `XINPUT_STATE`, `XINPUT_GAMEPAD`, `XINPUT_VIBRATION` mirror the native XInput layout with `[StructLayout(LayoutKind.Sequential)]`
- **`GamepadButtons` enum**: bitmask flags matching XInput button bit positions
- **Main loop**: polls at ~60 fps (`Thread.Sleep(16)`), redraws only when `dwPacketNumber` changes
- **Deadzone**: constant `DEADZONE = 8000` applied to analog sticks via `ApplyDeadzone()`
- **Controller discovery**: `FindConnectedController()` scans player indices 0–3

## Runtime Requirement

`xinput1_4.dll` must be present (ships with Windows 8+). The app will fail to load on systems without it.
