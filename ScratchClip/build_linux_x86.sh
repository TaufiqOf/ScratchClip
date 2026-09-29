#!/usr/bin/env bash

set -euo pipefail

# ============================================================
# ClipboardManagerX - Linux AppImage build script
# ============================================================

APP_NAME="ScratchClip"
PROJECT_FILE="ScratchClip.csproj"

RUNTIME="linux-x64"
CONFIGURATION="Release"
FRAMEWORK="net10.0"

OUTPUT_DIR="dist/linux"
PUBLISH_DIR="$OUTPUT_DIR/publish"
APPDIR="$OUTPUT_DIR/AppDir"

APPIMAGE="$OUTPUT_DIR/${APP_NAME}-x86_64.AppImage"

ICON_SOURCE="Assets/icon-light.png"
ICON_NAME="ScratchClip"

DESKTOP_FILE="$APPDIR/$ICON_NAME.desktop"

APPIMAGETOOL="$OUTPUT_DIR/appimagetool"
APPIMAGETOOL_URL="https://github.com/AppImage/appimagetool/releases/latest/download/appimagetool-x86_64.AppImage"

echo
echo "============================================================"
echo " Building $APP_NAME AppImage"
echo "============================================================"
echo

# ============================================================
# Check prerequisites
# ============================================================

if ! command -v dotnet >/dev/null 2>&1; then
    echo "ERROR: dotnet was not found."
    exit 1
fi

if ! command -v wget >/dev/null 2>&1; then
    echo "ERROR: wget was not found."
    echo
    echo "Install with:"
    echo
    echo "  sudo apt install wget"
    exit 1
fi

if ! command -v xclip >/dev/null 2>&1; then
    echo "ERROR: xclip was not found."
    echo
    echo "Install with:"
    echo
    echo "  sudo apt install xclip"
    exit 1
fi

if ! command -v xdotool >/dev/null 2>&1; then
    echo "ERROR: xdotool was not found."
    echo
    echo "Install with:"
    echo
    echo "  sudo apt install xdotool"
    exit 1
fi

if [ ! -f "$PROJECT_FILE" ]; then
    echo "ERROR: $PROJECT_FILE was not found."
    exit 1
fi

if [ ! -f "$ICON_SOURCE" ]; then
    echo "ERROR: Icon not found:"
    echo "  $ICON_SOURCE"
    exit 1
fi

# ============================================================
# Clean
# ============================================================

echo "==> Cleaning previous build..."

rm -rf "$OUTPUT_DIR"

mkdir -p "$PUBLISH_DIR"
mkdir -p "$APPDIR/usr/bin"
mkdir -p "$APPDIR/usr/share/icons/hicolor/256x256/apps"

# ============================================================
# Restore
# ============================================================

echo
echo "==> Restoring dependencies..."

dotnet restore "$PROJECT_FILE" \
    -r "$RUNTIME"

# ============================================================
# Publish
# ============================================================

echo
echo "==> Publishing $APP_NAME..."

dotnet publish "$PROJECT_FILE" \
    -c "$CONFIGURATION" \
    -f "$FRAMEWORK" \
    -r "$RUNTIME" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:EnableCompressionInSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:IncludeAllContentForSelfExtract=true \
    -p:DebugType=None \
    -p:DebugSymbols=false \
    -o "$PUBLISH_DIR"

# ============================================================
# Check executable
# ============================================================

GENERATED_EXECUTABLE="$PUBLISH_DIR/$APP_NAME"

if [ ! -f "$GENERATED_EXECUTABLE" ]; then
    echo
    echo "ERROR: $APP_NAME executable was not generated."
    exit 1
fi

# ============================================================
# Copy executable & Native Libraries
# ============================================================

echo
echo "==> Installing executable and native dependencies..."

cp -a "$PUBLISH_DIR/." "$APPDIR/usr/bin/"

NUGET_NATIVE_DIR="$HOME/.nuget/packages/treesitter.dotnet"

if [ -d "$NUGET_NATIVE_DIR" ]; then
    find "$NUGET_NATIVE_DIR" \
        -type f \
        -name "*.so" \
        -exec cp {} "$APPDIR/usr/bin/" \; \
        2>/dev/null || true
fi

chmod +x "$APPDIR/usr/bin/$APP_NAME"

# ============================================================
# Copy xclip + xdotool
# ============================================================

echo
echo "==> Bundling xclip and xdotool..."

XCLIP_PATH="$(command -v xclip)"
XDOTOOL_PATH="$(command -v xdotool)"

if [ -z "$XCLIP_PATH" ]; then
    echo "ERROR: xclip was not found."
    exit 1
fi

if [ -z "$XDOTOOL_PATH" ]; then
    echo "ERROR: xdotool was not found."
    exit 1
fi

echo "    System xclip:"
echo "      $XCLIP_PATH"

echo "    System xdotool:"
echo "      $XDOTOOL_PATH"

cp "$XCLIP_PATH" "$APPDIR/usr/bin/xclip"
cp "$XDOTOOL_PATH" "$APPDIR/usr/bin/xdotool"

chmod +x "$APPDIR/usr/bin/xclip"
chmod +x "$APPDIR/usr/bin/xdotool"

# ============================================================
# Create AppRun
# ============================================================

echo
echo "==> Creating AppRun..."

cat > "$APPDIR/AppRun" <<EOF
#!/usr/bin/env bash

HERE="\$(dirname "\$(readlink -f "\$0")")"

export PATH="\$HERE/usr/bin:\$PATH"
export LD_LIBRARY_PATH="\$HERE/usr/bin:\$LD_LIBRARY_PATH"

exec "\$HERE/usr/bin/$APP_NAME" "\$@"
EOF

chmod +x "$APPDIR/AppRun"

# ============================================================
# Install icon
# ============================================================

echo
echo "==> Installing icon..."

cp "$ICON_SOURCE" "$APPDIR/$APP_NAME.png"

cp "$ICON_SOURCE" \
    "$APPDIR/usr/share/icons/hicolor/256x256/apps/$APP_NAME.png"

# ============================================================
# Create desktop file
# ============================================================

echo
echo "==> Creating desktop entry..."

DESKTOP_FILE="$APPDIR/$APP_NAME.desktop"

cat > "$DESKTOP_FILE" <<EOF
[Desktop Entry]
Name=$APP_NAME
Comment=Clipboard Manager
Exec=$APP_NAME
Icon=$APP_NAME
Terminal=false
Type=Application
Categories=Utility;
StartupNotify=true
EOF

# ============================================================
# Show AppDir
# ============================================================

echo
echo "==> AppDir structure:"
echo

find "$APPDIR" -type f -printf '  %P\n'

echo

# ============================================================
# Download appimagetool
# ============================================================

if [ ! -f "$APPIMAGETOOL" ]; then

    echo "==> Downloading appimagetool..."

    wget \
        -O "$APPIMAGETOOL" \
        "$APPIMAGETOOL_URL"

    chmod +x "$APPIMAGETOOL"

else

    echo "==> Using existing appimagetool..."

fi

# ============================================================
# Create AppImage
# ============================================================

echo
echo "==> Creating AppImage..."

rm -f "$APPIMAGE"

ARCH=x86_64 "$APPIMAGETOOL" \
    "$APPDIR" \
    "$APPIMAGE"

# ============================================================
# Verify
# ============================================================

if [ ! -f "$APPIMAGE" ]; then
    echo
    echo "ERROR: AppImage was not created."
    exit 1
fi

chmod +x "$APPIMAGE"

# ============================================================
# Cleanup
# ============================================================

echo
echo "==> Cleaning temporary files..."

rm -rf "$PUBLISH_DIR"
rm -rf "$APPDIR"
rm -f "$APPIMAGETOOL"

# ============================================================
# Result
# ============================================================

echo
echo "============================================================"
echo " AppImage created successfully!"
echo "============================================================"
echo
echo "Output:"
echo
echo "  $APPIMAGE"
echo

ls -lh "$APPIMAGE"

echo
echo "Run with:"
echo
echo "  ./$APPIMAGE"
echo
{
  "app-id": "com.github.taufiq.scratchclip",
  "runtime": "org.freedesktop.Platform",
  "runtime-version": "24.08",
  "sdk": "org.freedesktop.Sdk",
  "sdk-extensions": [
    "org.freedesktop.Sdk.Extension.dotnet10"
  ],
  "command": "scratchclip",
  "finish-args": [
    "--share=ipc",
    "--socket=fallback-x11",
    "--socket=wayland",
    "--socket=system-bus",
    "--socket=session-bus",
    "--share=network",
    "--device=all",
    "--filesystem=home",
    "--talk-name=org.freedesktop.Notifications",
    "--talk-name=org.freedesktop.portal.Desktop",
    "--talk-name=org.kde.StatusNotifier",
    "--talk-name=org.freedesktop.secrets"
  ],
  "cleanup": [
    "/include",
    "/lib/pkgconfig",
    "/man",
    "/share/doc",
    "/share/man",
    "*.la",
    "*.a"
  ],
  "build-options": {
    "build-args": [
      "--share=network"
    ],
    "env": {
      "DOTNET_CLI_TELEMETRY_OPTOUT": "1",
      "DOTNET_ROOT": "/usr/lib/sdk/dotnet10",
      "PATH": "/usr/lib/sdk/dotnet10/bin:/usr/bin:/bin"
    }
  },
  "modules": [
    {
      "name": "libsecret",
      "buildsystem": "meson",
      "config-opts": [
        "-Dvapi=false",
        "-Dgtk_doc=false",
        "-Dintrospection=false",
        "-Dman=false"
      ],
      "sources": [
        {
          "type": "archive",
          "url": "https://download.gnome.org/sources/libsecret/0.21/libsecret-0.21.4.tar.xz",
          "sha256": "3fb3033230c30953a5c1be7fae448b3b3aee1ef1dcb8dd1b8a514d4834ff4c00"
        }
      ]
    },
    {
      "name": "xclip",
      "buildsystem": "autotools",
      "sources": [
        {
          "type": "archive",
          "url": "https://github.com/astrand/xclip/archive/refs/tags/0.13.tar.gz",
          "sha256": "ca1f4354fa821ea2cc1f681580aa136009ecb78ea97c38c8bc1e6cc72bb46ca0"
        }
      ]
    },
    {
      "name": "xdotool",
      "buildsystem": "simple",
      "build-commands": [
        "make",
        "make install PREFIX=/app"
      ],
      "sources": [
        {
          "type": "archive",
          "url": "https://github.com/jordansissel/xdotool/archive/refs/tags/v3.20211022.1.tar.gz",
          "sha256": "27038a395c8038743a129ef31818dd1e3d37613ba2a4f488663ed8e583e74a88"
        }
      ]
    },
    {
      "name": "scratchclip",
      "buildsystem": "simple",
      "build-options": {
        "build-args": [
          "--share=network"
        ]
      },
      "build-commands": [
        "mkdir -p /app/bin",
        "/usr/lib/sdk/dotnet10/bin/dotnet publish -c Release -r linux-x64 --self-contained false -o /app/bin ScratchClip/ScratchClip.csproj",
        "find /var/data/nuget /root/.nuget/packages ~/.nuget/packages -name '*.so*' -exec cp {} /app/bin/ \\; 2>/dev/null || true",
        "ln -sf /app/bin/ScratchClip /app/bin/scratchclip",
        "chmod +x /app/bin/*",
        "mkdir -p /app/share/applications",
        "mkdir -p /app/share/icons/hicolor/128x128/apps",
        "mkdir -p /app/share/icons/hicolor/256x256/apps",
        "magick ScratchClip/Assets/icon-dark.png -background none -gravity center -extent 256x256 /app/share/icons/hicolor/256x256/apps/com.github.taufiq.scratchclip.png",
        "magick ScratchClip/Assets/icon-light.png -background none -gravity center -extent 128x128 /app/share/icons/hicolor/128x128/apps/com.github.taufiq.scratchclip.png"
      ],
      "sources": [
        {
          "type": "dir",
          "path": "."
        }
      ],
      "post-install": [
        "install -Dm644 com.github.taufiq.scratchclip.desktop /app/share/applications/com.github.taufiq.scratchclip.desktop",
        "install -Dm644 com.github.taufiq.scratchclip.appdata.xml /app/share/app-info/xmls/com.github.taufiq.scratchclip.appdata.xml"
      ]
    }
  ]
}
