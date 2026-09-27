#!/bin/bash
# Validation Script for ScratchClip Flathub Submission
# This script checks manifest syntax and configuration

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MANIFEST="$SCRIPT_DIR/com.github.taufiq.scratchclip.json"
DESKTOP="$SCRIPT_DIR/com.github.taufiq.scratchclip.desktop"
APPDATA="$SCRIPT_DIR/com.github.taufiq.scratchclip.appdata.xml"

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}═══════════════════════════════════════${NC}"
echo -e "${BLUE}ScratchClip Flathub Validation Script${NC}"
echo -e "${BLUE}═══════════════════════════════════════${NC}"
echo ""

PASSED=0
FAILED=0

# Helper functions
pass() {
    echo -e "${GREEN}✓${NC} $1"
    ((PASSED++))
}

fail() {
    echo -e "${RED}✗${NC} $1"
    ((FAILED++))
}

warn() {
    echo -e "${YELLOW}⚠${NC} $1"
}

check_file() {
    if [ -f "$1" ]; then
        pass "Found: $1"
        return 0
    else
        fail "Missing: $1"
        return 1
    fi
}

# Check required files
echo -e "${BLUE}📋 Checking Required Files${NC}"
echo "─────────────────────────────"
check_file "$MANIFEST" || exit 1
check_file "$DESKTOP" || exit 1
check_file "$APPDATA" || exit 1
echo ""

# Check JSON syntax
echo -e "${BLUE}📝 Checking JSON Syntax${NC}"
echo "─────────────────────────────"
if command -v jq &> /dev/null; then
    if jq empty "$MANIFEST" 2>/dev/null; then
        pass "Manifest JSON is valid"
    else
        fail "Manifest JSON syntax error"
    fi
else
    warn "jq not installed, skipping JSON validation"
    warn "Install with: sudo apt install jq"
fi
echo ""

# Check XML syntax
echo -e "${BLUE}🏷️  Checking XML Syntax${NC}"
echo "─────────────────────────────"
if command -v xmllint &> /dev/null; then
    if xmllint --noout "$APPDATA" 2>/dev/null; then
        pass "AppData XML is valid"
    else
        fail "AppData XML syntax error"
    fi
else
    warn "xmllint not installed, skipping XML validation"
    warn "Install with: sudo apt install libxml2-utils"
fi
echo ""

# Check manifest content
echo -e "${BLUE}🔍 Checking Manifest Configuration${NC}"
echo "─────────────────────────────"

if grep -q '"app-id": "com.github.taufiq.scratchclip"' "$MANIFEST"; then
    pass "App ID is correct"
else
    fail "App ID is incorrect or missing"
fi

if grep -q '"runtime": "org.freedesktop.Platform"' "$MANIFEST"; then
    pass "Runtime is correct"
else
    fail "Runtime is incorrect"
fi

if grep -q '"sdk": "org.freedesktop.Sdk"' "$MANIFEST"; then
    pass "SDK is correct"
else
    fail "SDK is incorrect"
fi

if grep -q 'org.freedesktop.Sdk.Extension.dotnet10' "$MANIFEST"; then
    pass ".NET 10 SDK extension is specified"
else
    fail ".NET 10 SDK extension is missing"
fi

if grep -q '"command": "scratchclip"' "$MANIFEST"; then
    pass "Command is set correctly"
else
    fail "Command is incorrect"
fi

echo ""

# Check desktop file
echo -e "${BLUE}🖥️  Checking Desktop Entry${NC}"
echo "─────────────────────────────"

if grep -q "^Name=ScratchClip" "$DESKTOP"; then
    pass "Desktop entry name is correct"
else
    fail "Desktop entry name is incorrect"
fi

if grep -q "^Icon=com.github.taufiq.scratchclip" "$DESKTOP"; then
    pass "Desktop entry icon is correct"
else
    fail "Desktop entry icon is incorrect"
fi

if grep -q "^Exec=scratchclip" "$DESKTOP"; then
    pass "Desktop entry exec is correct"
else
    fail "Desktop entry exec is incorrect"
fi

echo ""

# Check AppData
echo -e "${BLUE}📱 Checking AppData Metadata${NC}"
echo "─────────────────────────────"

if grep -q '<id>com.github.taufiq.scratchclip</id>' "$APPDATA"; then
    pass "AppData ID is correct"
else
    fail "AppData ID is incorrect"
fi

if grep -q '<name>ScratchClip</name>' "$APPDATA"; then
    pass "AppData name is present"
else
    fail "AppData name is missing"
fi

if grep -q '<summary>' "$APPDATA"; then
    pass "AppData summary is present"
else
    fail "AppData summary is missing"
fi

if grep -q '<description>' "$APPDATA"; then
    pass "AppData description is present"
else
    fail "AppData description is missing"
fi

if grep -q '<license>' "$APPDATA" || grep -q 'project_license' "$APPDATA"; then
    pass "AppData license is specified"
else
    warn "AppData license may need verification"
fi

echo ""

# Check icons
echo -e "${BLUE}🎨 Checking Application Icons${NC}"
echo "─────────────────────────────"

ICON_DARK="$SCRIPT_DIR/ScratchClip/Assets/icon-dark.png"
ICON_LIGHT="$SCRIPT_DIR/ScratchClip/Assets/icon-light.png"

if [ -f "$ICON_DARK" ]; then
    if command -v identify &> /dev/null; then
        SIZE=$(identify -format "%wx%h" "$ICON_DARK")
        pass "Dark icon found ($SIZE)"
    else
        pass "Dark icon found"
    fi
else
    fail "Dark icon not found at: $ICON_DARK"
fi

if [ -f "$ICON_LIGHT" ]; then
    if command -v identify &> /dev/null; then
        SIZE=$(identify -format "%wx%h" "$ICON_LIGHT")
        pass "Light icon found ($SIZE)"
    else
        pass "Light icon found"
    fi
else
    fail "Light icon not found at: $ICON_LIGHT"
fi

echo ""

# Check repository
echo -e "${BLUE}🔗 Checking Repository Setup${NC}"
echo "─────────────────────────────"

if [ -d "$SCRIPT_DIR/.git" ]; then
    pass "Git repository found"
    
    if cd "$SCRIPT_DIR" && git remote get-url origin &> /dev/null; then
        REMOTE=$(cd "$SCRIPT_DIR" && git remote get-url origin)
        pass "Remote repository: $REMOTE"
    else
        fail "No git remote configured"
    fi
    
    if cd "$SCRIPT_DIR" && git status --porcelain &> /dev/null; then
        pass "Git status OK"
    fi
else
    fail "Git repository not found"
fi

echo ""

# Check directory structure
echo -e "${BLUE}📁 Checking Directory Structure${NC}"
echo "─────────────────────────────"

if [ -d "$SCRIPT_DIR/ScratchClip" ]; then
    pass "Main project directory found"
else
    fail "Main project directory not found"
fi

if [ -f "$SCRIPT_DIR/ScratchClip/ScratchClip.csproj" ]; then
    pass "Project file found"
else
    fail "Project file not found"
fi

if [ -f "$SCRIPT_DIR/readme.md" ]; then
    pass "README found"
else
    warn "README not found in root"
fi

echo ""

# Check documentation
echo -e "${BLUE}📚 Checking Documentation${NC}"
echo "─────────────────────────────"

if [ -f "$SCRIPT_DIR/FLATHUB_SUBMISSION.md" ]; then
    pass "FLATHUB_SUBMISSION.md found"
else
    warn "FLATHUB_SUBMISSION.md not found"
fi

if [ -f "$SCRIPT_DIR/FLATHUB_CHECKLIST.md" ]; then
    pass "FLATHUB_CHECKLIST.md found"
else
    warn "FLATHUB_CHECKLIST.md not found"
fi

if [ -f "$SCRIPT_DIR/FLATHUB_QUICKSTART.md" ]; then
    pass "FLATHUB_QUICKSTART.md found"
else
    warn "FLATHUB_QUICKSTART.md not found"
fi

echo ""

# Check build script
echo -e "${BLUE}🔨 Checking Build Script${NC}"
echo "─────────────────────────────"

if [ -x "$SCRIPT_DIR/build-flatpak.sh" ]; then
    pass "build-flatpak.sh is executable"
else
    if [ -f "$SCRIPT_DIR/build-flatpak.sh" ]; then
        fail "build-flatpak.sh exists but is not executable"
        warn "Fix with: chmod +x build-flatpak.sh"
    else
        fail "build-flatpak.sh not found"
    fi
fi

echo ""

# Final Summary
echo -e "${BLUE}═══════════════════════════════════════${NC}"
echo -e "${BLUE}Validation Summary${NC}"
echo -e "${BLUE}═══════════════════════════════════════${NC}"
echo ""

echo -e "Checks passed: ${GREEN}$PASSED${NC}"
echo -e "Checks failed: ${RED}$FAILED${NC}"
echo ""

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}✓ All validation checks passed!${NC}"
    echo ""
    echo "Your Flathub submission is ready."
    echo ""
    echo "Next steps:"
    echo "1. Review FLATHUB_QUICKSTART.md"
    echo "2. Push changes to GitHub"
    echo "3. Submit at https://flathub.org/en/submit/"
    echo ""
    exit 0
else
    echo -e "${RED}✗ Validation failed with $FAILED error(s)${NC}"
    echo ""
    echo "Please fix the errors above before submitting to Flathub."
    echo ""
    exit 1
fi


