# Lossless Cut Launcher

A cross-platform launcher for [LosslessCut](https://github.com/mifi/lossless-cut). It automatically checks for and downloads the latest version of LosslessCut before launching it.

## Usage

Download the latest release from the project's GitHub Releases page.

### Installer (Windows)

Download and run [`LosslessCutLauncher-win-x64-Setup.exe`](https://github.com/hazre/lossless-cut-launcher/releases/latest/download/LosslessCutLauncher-win-x64-Setup.exe) to install the application.

### Portable

- **Windows**: Download the [`LosslessCutLauncher-win-x64-portable.zip`](https://github.com/hazre/lossless-cut-launcher/releases/latest/download/LosslessCutLauncher-win-x64-portable.zip) file, unzip it, and run `Lossless Cut Launcher.exe`.
- **Linux**: Download the [`LosslessCutLauncher-linux-x64.AppImage`](https://github.com/hazre/lossless-cut-launcher/releases/latest/download/LosslessCutLauncher-linux-x64.AppImage) file, make it executable (`chmod +x *.AppImage`), and then run it.

## Building from Source

1.  Clone the repository.
2.  Install the .NET 10 SDK, or let `global.json` pin `10.0.202` for you.
3.  Run `dotnet build` from the project's root directory.

## Releases

Create a change file with `knope document-change`.
Knope uses `knope.toml` to open a release PR from `prepare-release`.
Merging that release PR publishes the release and uploads any assets from `release-assets/*`.
