# TI ZILL DIGITAL Suite

Native **C# WinForms (.NET 8)** desktop application with SQLite storage and ESC/POS thermal printing.

## Prerequisites

- .NET 8 SDK
- Git
- Windows 10/11 for running the WinForms app

> WinForms is Windows-only at runtime. You can still **build/publish for Windows from macOS**.

---

## Build on Windows (develop + run)

### 1) Clone and enter project
```bash
git clone <your-repo-url>
cd tizilldigital-business-app/TiZillDigital
```

### 2) Restore dependencies
```bash
dotnet restore
```

### 3) Build debug
```bash
dotnet build -c Debug
```

### 4) Run locally on Windows
```bash
dotnet run -c Debug
```

### 5) Publish release EXE (self-contained)
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish/
```

Output binary:
- `publish/TiZillDigital.exe`

You can also use the helper scripts:
- `setup.bat`
- `build.bat`

---

## Build on macOS for Windows (cross-publish)

You **cannot run** WinForms on macOS, but you can produce a Windows binary.

### 1) Clone and enter project
```bash
git clone <your-repo-url>
cd tizilldigital-business-app/TiZillDigital
```

### 2) Install .NET 8 SDK (macOS)
```bash
brew install --cask dotnet-sdk
```

Then open a new terminal and verify:
```bash
dotnet --info
```

### 3) Restore and build
```bash
dotnet restore
dotnet build -c Release
```

### 4) Cross-publish for Windows x64
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish-win/
```

Output binary to transfer to Windows machine:
- `publish-win/TiZillDigital.exe`

> Note: If native Windows-specific workloads are requested by your SDK, run:
> ```bash
> dotnet workload update
> ```

---

## Recommended verification on Windows machine

After publishing from macOS, copy `publish-win/` to a Windows 10/11 PC and run:

1. `TiZillDigital.exe`
2. Open **Settings → Thermal Printer**
3. Configure printer connection
4. Click **Test Print**

---

## Troubleshooting

### `dotnet: command not found`
Install .NET 8 SDK and restart terminal.

### Restore/publish fails due to NuGet network issues
Retry with:
```bash
dotnet restore --disable-parallel
```

### App builds but won't run on macOS
Expected behavior: WinForms executables are Windows-only.

### Printing issues
- Verify printer type (USB / Serial / Network / Windows)
- Verify COM port/IP/port values
- Use **Test Print** first

---

## Project quick commands

From `TiZillDigital/`:

```bash
# Restore
dotnet restore

# Build
dotnet build

# Run (Windows only)
dotnet run

# Publish single-file Windows EXE
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish/
```
