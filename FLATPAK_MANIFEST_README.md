# Flatpak Manifest Files for ScratchClip

This directory contains the Flatpak manifest and related files needed to build and distribute ScratchClip via Flathub.

## Files

- **`com.github.taufiq.scratchclip.json`** - Main Flatpak manifest
  - Defines application metadata
  - Specifies runtime and SDK requirements
  - Lists build commands and installation steps
  - Declares system permissions

- **`com.github.taufiq.scratchclip.desktop`** - Desktop Entry File
  - Integrates the application with the desktop environment
  - Defines application launcher properties
  - Specifies application categories and keywords

- **`com.github.taufiq.scratchclip.appdata.xml`** - AppData Metadata
  - Provides metadata for app stores (Flathub, GNOME Software, etc.)
  - Contains application description, screenshots, and release notes
  - Defines license and project information

## Building Locally

To test the Flatpak build locally:

```bash
# Install flatpak and build tools
sudo apt install flatpak flatpak-builder

# Add Flathub repository
flatpak remote-add --if-not-exists flathub https://flathub.org/repo/flathub.flatpakrepo

# Build the application
flatpak-builder --user --install --force-clean build-dir com.github.taufiq.scratchclip.json

# Run the built application
flatpak run com.github.taufiq.scratchclip
```

## Submitting to Flathub

See [FLATHUB_SUBMISSION.md](../FLATHUB_SUBMISSION.md) for detailed submission instructions.

## Key Configuration Notes

### Application ID Format
The application ID follows the reverse domain notation convention:
```
com.github.taufiq.scratchclip
```

This should NOT be changed after Flathub submission without migrating the entire repository.

### Runtime and SDK
- Runtime: `org.freedesktop.Platform` (version 24.08)
- SDK: `org.freedesktop.Sdk` (version 24.08)
- Extensions: `.Extension.dotnet10` (for .NET 10 support)

### Permissions (finish-args)

The manifest declares the following permissions:

| Permission | Reason |
|------------|--------|
| `--share=ipc` | Inter-process communication |
| `--socket=fallback-x11` | X11 display support |
| `--socket=wayland` | Wayland display support |
| `--socket=system-bus` | System D-Bus access (required for clipboard monitoring) |
| `--socket=session-bus` | Session D-Bus access |
| `--share=network` | Network access (YouTube previews) |
| `--device=all` | Device access |
| `--filesystem=home` | Home directory access (for history storage) |
| `--talk-name=org.freedesktop.Notifications` | Send desktop notifications |
| `--talk-name=org.freedesktop.portal.Desktop` | Desktop portals |
| `--talk-name=org.kde.StatusNotifier` | KDE system tray integration |

### Build Process

The manifest executes these build steps:

1. Publishes the .NET 10 application in Release mode
2. Copies the binary to `/app/bin/scratchclip`
3. Installs the desktop entry file
4. Installs the appdata metadata
5. Copies application icons

## Updates and Maintenance

### Updating the Manifest

When updating ScratchClip:

1. Update the version in `ScratchClip.csproj` (AssemblyVersion)
2. Add a new release entry to `com.github.taufiq.scratchclip.appdata.xml`
3. Test the build with `flatpak-builder`
4. Commit and push changes
5. Create a new git tag for the release

### Icons

Current icons used:
- `ScratchClip/Assets/icon-dark.png` (256x256)
- `ScratchClip/Assets/icon-light.png` (128x128)

Ensure icons are:
- At least 256x256 pixels
- PNG format
- Clear and recognizable at small sizes

## Troubleshooting

### Build Issues

If the build fails:
1. Check that you have the latest flatpak-builder:
   ```bash
   flatpak update
   ```

2. Check the full error output by adding `--verbose`:
   ```bash
   flatpak-builder --verbose --user --install --force-clean build-dir com.github.taufiq.scratchclip.json
   ```

3. Verify .NET 10 SDK extension is available:
   ```bash
   flatpak list --app | grep dotnet
   ```

### Runtime Issues

If the app doesn't run properly:
1. Check logs:
   ```bash
   flatpak run --devel com.github.taufiq.scratchclip
   ```

2. Add missing permissions if needed
3. Test without the sandbox: `--env=FLATPAK_DISABLE_SANDBOX=1`

## References

- [Flathub Documentation](https://docs.flathub.org/)
- [Flatpak Documentation](https://docs.flatpak.org/)
- [AppData Specification](https://www.freedesktop.org/software/appstream/docs/)
- [Desktop Entry Specification](https://specifications.freedesktop.org/desktop-entry-spec/latest/)

