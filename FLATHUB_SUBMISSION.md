# Flathub Submission Guide for ScratchClip

This guide outlines the steps to publish ScratchClip to Flathub.

## Prerequisites

1. **GitHub Account** - You need a GitHub account
2. **GitHub Repository** - Your project should be in a public GitHub repository
3. **Flatpak Tools** - Install flatpak and flatpak-builder on your system
4. **Flathub Account** - Set up an account on Flathub (uses GitHub authentication)

## Files Created for Flathub

The following files have been created to support Flathub submission:

- **`com.github.taufiq.scratchclip.json`** - Main Flatpak manifest file
- **`com.github.taufiq.scratchclip.desktop`** - Desktop entry file for the application menu
- **`com.github.taufiq.scratchclip.appdata.xml`** - AppData file with application metadata

## Submission Steps

### Step 1: Prepare Your Repository

1. Ensure your GitHub repository is public and accessible at:
   ```
   https://github.com/taufiq/ClipboardManagerX
   ```

2. Add the Flatpak manifest files to your repository:
   ```bash
   git add com.github.taufiq.scratchclip.json
   git add com.github.taufiq.scratchclip.desktop
   git add com.github.taufiq.scratchclip.appdata.xml
   git commit -m "Add Flatpak manifest for Flathub submission"
   git push origin main
   ```

3. Create a release/tag for your application version (e.g., v0.0.56):
   ```bash
   git tag v0.0.56
   git push origin v0.0.56
   ```

### Step 2: Test the Flatpak Build Locally

Before submitting, test that your manifest builds correctly:

```bash
# Install Flatpak and build tools if not already installed
sudo apt install flatpak flatpak-builder

# Add Flathub repository
flatpak remote-add --if-not-exists flathub https://flathub.org/repo/flathub.flatpakrepo

# Build the application
flatpak-builder --user --install --force-clean build-dir com.github.taufiq.scratchclip.json

# Run the application
flatpak run com.github.taufiq.scratchclip
```

### Step 3: Fork Flathub Repository

1. Visit https://github.com/flathub/flathub
2. Click "Fork" to create your own fork

### Step 4: Create Application Repository on Flathub

1. Go to https://github.com/flathub and create a new repository
2. Name it: `com.github.taufiq.scratchclip`
3. Make it public
4. Add this content to the repository:

#### Structure:
```
com.github.taufiq.scratchclip/
├── .github/
│   ├── workflows/
│   │   └── flatpak-builder.yml
├── com.github.taufiq.scratchclip.json
├── com.github.taufiq.scratchclip.desktop
├── com.github.taufiq.scratchclip.appdata.xml
├── README.md
└── .gitignore
```

### Step 5: Submit to Flathub

There are two submission methods:

#### Method A: Direct Submission (Recommended)

1. Visit https://flathub.org/en/submit/
2. Select "I want to submit a new application"
3. Provide your application repository URL:
   ```
   https://github.com/flathub/com.github.taufiq.scratchclip.git
   ```
4. Fill out the application details form
5. Submit for review

#### Method B: Manual PR Submission

1. Clone the Flathub registry:
   ```bash
   git clone https://github.com/flathub/flathub.git
   cd flathub
   ```

2. Create a new branch:
   ```bash
   git checkout -b com.github.taufiq.scratchclip
   ```

3. Create application directory:
   ```bash
   mkdir -p com/github/taufiq/scratchclip
   ```

4. Copy your manifest files:
   ```bash
   cp /path/to/com.github.taufiq.scratchclip.json com/github/taufiq/scratchclip/
   cp /path/to/com.github.taufiq.scratchclip.desktop com/github/taufiq/scratchclip/
   cp /path/to/com.github.taufiq.scratchclip.appdata.xml com/github/taufiq/scratchclip/
   ```

5. Commit and push:
   ```bash
   git add com/github/taufiq/scratchclip/
   git commit -m "Add com.github.taufiq.scratchclip"
   git push origin com.github.taufiq.scratchclip
   ```

6. Create a Pull Request on GitHub

## Manifest Details

### Application ID
- **ID**: `com.github.taufiq.scratchclip`
- **Format**: Reverse domain notation (recommended to follow: `com.github.username.appname`)

### Permissions (finish-args)

The manifest includes the following permissions:

| Permission | Purpose |
|------------|---------|
| `--share=ipc` | Shared memory for IPC |
| `--socket=fallback-x11` | X11 display socket |
| `--socket=wayland` | Wayland display server |
| `--socket=system-bus` | D-Bus system access |
| `--socket=session-bus` | D-Bus session access |
| `--share=network` | Network access (for YouTube preview functionality) |
| `--device=all` | Device access |
| `--filesystem=home` | Home directory access (needed for clipboard history storage) |
| `--talk-name=org.freedesktop.Notifications` | Desktop notifications |
| `--talk-name=org.freedesktop.portal.Desktop` | Desktop portal |
| `--talk-name=org.kde.StatusNotifier` | KDE status notifier (system tray) |

### Runtime

- **Runtime**: `org.freedesktop.Platform`
- **Runtime Version**: `24.08` (Latest stable as of 2024)
- **SDK Extensions**: `org.freedesktop.Sdk.Extension.dotnet10` (for .NET 10 support)

## Important Notes

1. **App ID Changes**: If you need to change the app ID later, it requires moving the entire Flathub repository.

2. **License**: Update the `project_license` field in the appdata.xml with your actual license.

3. **Icons**: Ensure you have proper icon files (256x256 minimum for Flathub).

4. **Release Notes**: Keep the appdata.xml `<releases>` section updated with each new version.

5. **Screenshots**: Add actual screenshot URLs to the appdata.xml for better visibility on Flathub.

6. **Build System**: This manifest uses the .NET runtime. If you encounter build issues, ensure:
   - The manifest correctly points to your GitHub repository
   - Your repository is public
   - All dependencies are available in the Flathub repository

## Troubleshooting

### Build Failures

If the build fails:
1. Check the error messages carefully
2. Verify all dependencies are listed in the manifest
3. Ensure the project builds locally with the command specified
4. Check Flathub documentation: https://docs.flathub.org/

### Runtime Issues

If the app crashes when running:
1. Check the permissions (finish-args)
2. Verify required libraries are installed
3. Test with `--devel` flag: `flatpak run --devel com.github.taufiq.scratchclip`

### Store Listing Issues

If your app isn't showing up correctly on Flathub:
1. Validate your appdata.xml file
2. Ensure all required fields are present
3. Check icon dimensions and format

## Support

- Flathub Documentation: https://docs.flathub.org/
- Flatpak Documentation: https://docs.flatpak.org/
- GitHub Flathub Repository: https://github.com/flathub/flathub
- Flathub Community: https://discourse.flathub.org/

## Maintenance

After submission and approval:

1. Keep the manifest updated with your application
2. Update version numbers in appdata.xml with each release
3. Add release notes to the `<releases>` section
4. Monitor for any CI/CD issues in the Flathub repository

