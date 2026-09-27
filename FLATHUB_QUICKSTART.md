# 🚀 Flathub Publication Quick Start Guide

## What You Need to Do

Publishing ScratchClip to Flathub involves three main steps:

1. **Test the build locally** ✓ (Optional but recommended)
2. **Create a Flathub application repository** ✓ (Manual setup)
3. **Submit to Flathub for review** ✓ (Online or manual)

---

## Step 1: Test Locally (Optional but Recommended)

Before submitting to Flathub, verify your Flatpak builds correctly.

### Quick Start

```bash
cd /home/taufiq/RiderProjects/ClipboardManagerX
./build-flatpak.sh
```

### Full Manual Build

```bash
# Install required tools
sudo apt install flatpak flatpak-builder

# Add Flathub repository
flatpak remote-add --if-not-exists flathub https://flathub.org/repo/flathub.flatpakrepo

# Navigate to project directory
cd /home/taufiq/RiderProjects/ClipboardManagerX

# Build the Flatpak
flatpak-builder --user --install --force-clean flatpak-build-dir com.github.taufiq.scratchclip.json

# Run the application
flatpak run com.github.taufiq.scratchclip
```

If the build succeeds and the application runs, you're ready for Flathub submission! ✓

---

## Step 2: Prepare Your Repository

Ensure your GitHub repository is ready:

```bash
cd /home/taufiq/RiderProjects/ClipboardManagerX

# Add all Flathub-related files
git add com.github.taufiq.scratchclip.json
git add com.github.taufiq.scratchclip.desktop
git add com.github.taufiq.scratchclip.appdata.xml
git add FLATHUB_SUBMISSION.md
git add FLATPAK_MANIFEST_README.md
git add FLATHUB_CHECKLIST.md
git add build-flatpak.sh
git add .github/workflows/flatpak-builder.yml

# Commit and push
git commit -m "Prepare for Flathub submission"
git push origin main

# Create a release tag (if not already done)
git tag v0.0.56
git push origin v0.0.56
```

---

## Step 3: Submit to Flathub

### Option A: Online Submission (Easiest)

1. Go to: https://flathub.org/en/submit/
2. Click "I want to submit a new application"
3. Enter your repository URL: `https://github.com/taufiq/ClipboardManagerX`
4. Fill out the form with:
   - **Application Name**: ScratchClip
   - **Category**: Utility
   - **License**: All rights reserved (or specify your actual license)
   - **Application Description**: See readme.md for full details
   - **Repository**: Your GitHub repo URL
5. Click "Submit for Review"

Flathub will review your submission and contact you with any required changes.

### Option B: Manual Submission

If you have Flathub organization access:

1. Create a new repository at GitHub:
   - URL: `https://github.com/flathub/com.github.taufiq.scratchclip`
   - Name: `com.github.taufiq.scratchclip`
   - Description: "A lightweight, modern clipboard history manager"
   - Make it public

2. Create the directory structure:
   ```bash
   mkdir -p com/github/taufiq/scratchclip
   ```

3. Copy your manifest files:
   ```bash
   cp com.github.taufiq.scratchclip.json com/github/taufiq/scratchclip/
   cp com.github.taufiq.scratchclip.desktop com/github/taufiq/scratchclip/
   cp com.github.taufiq.scratchclip.appdata.xml com/github/taufiq/scratchclip/
   ```

4. Add a README:
   ```bash
   cat > com/github/taufiq/scratchclip/README.md << 'EOF'
   # ScratchClip Flatpak
   
   A lightweight, modern clipboard history manager for Linux.
   
   See the [upstream repository](https://github.com/taufiq/ClipboardManagerX) for more information.
   EOF
   ```

5. Commit and push:
   ```bash
   git add .
   git commit -m "Initial Flathub submission"
   git push origin main
   ```

---

## What Happens Next?

### After Submission

1. **Flathub Review** (1-7 days typically)
   - Team reviews your manifest
   - Verifies your application is legitimate
   - Checks permissions and security
   - May ask for changes

2. **Building** (if approved)
   - Flathub's CI/CD builds your application
   - Automated tests run
   - Binary is created and published

3. **Publication**
   - Your app appears on https://flathub.org/apps/
   - Users can install via:
     ```bash
     flatpak install flathub com.github.taufiq.scratchclip
     ```

### Expected Timeline
- Submission → Initial Review: 1-3 days
- Review → Approval: 2-5 days
- Approval → Publication: 1-2 days
- **Total**: ~1 week

---

## Key Files Explained

| File | Purpose |
|------|---------|
| `com.github.taufiq.scratchclip.json` | Main Flatpak manifest - defines how to build and run your app |
| `com.github.taufiq.scratchclip.desktop` | Desktop entry - integrates with application menus |
| `com.github.taufiq.scratchclip.appdata.xml` | App store metadata - shown on Flathub store page |
| `build-flatpak.sh` | Helper script to build and test locally |
| `FLATHUB_SUBMISSION.md` | Detailed submission instructions |
| `FLATPAK_MANIFEST_README.md` | Technical manifest documentation |
| `FLATHUB_CHECKLIST.md` | Pre-submission verification checklist |

---

## Important Notes

⚠️ **Critical Points:**

1. **App ID is permanent**: `com.github.taufiq.scratchclip` cannot be changed without significant issues
2. **Permissions must be justified**: Every permission must serve a purpose
3. **Test before submitting**: Always test locally with `./build-flatpak.sh`
4. **Updates are automatic**: Once approved, Flathub automatically rebuilds on new repository commits
5. **Version tracking**: Version must match both your project and appdata.xml

---

## Troubleshooting

### Build Fails Locally

```bash
# Check for updates
flatpak update

# Try with verbose output
flatpak-builder --verbose --user --install --force-clean flatpak-build-dir com.github.taufiq.scratchclip.json

# Check .NET 10 SDK availability
flatpak list --app | grep dotnet
```

### Permission Issues

If the application doesn't work after installation:
1. Check error with: `flatpak run --devel com.github.taufiq.scratchclip`
2. Review required permissions in manifest
3. Contact Flathub support at https://discourse.flathub.org/

### Submission Review Feedback

Flathub reviewers might request:
- Additional justification for permissions
- Changes to metadata or descriptions
- Updates to icons or screenshots
- License clarification

**All requests should be addressed promptly** to avoid delays.

---

## Monitoring After Publication

Once published, monitor:

1. **User Reviews**: Check Flathub store page for feedback
2. **Download Stats**: Track installation numbers
3. **Issues**: Monitor GitHub for Flatpak-specific issues
4. **Updates**: Keep version synchronized between:
   - `ScratchClip.csproj` (AssemblyVersion)
   - `com.github.taufiq.scratchclip.appdata.xml` (release version)

---

## Resources

- 📚 [Flathub Documentation](https://docs.flathub.org/)
- 📚 [Flatpak Documentation](https://docs.flatpak.org/)
- 💬 [Flathub Community Forum](https://discourse.flathub.org/)
- 📋 [Submission Checklist](FLATHUB_CHECKLIST.md)
- 📖 [Detailed Submission Guide](FLATHUB_SUBMISSION.md)

---

## Need Help?

1. **Local build issues?** → Read `FLATPAK_MANIFEST_README.md`
2. **Unsure about process?** → Follow `FLATHUB_SUBMISSION.md`
3. **Checking prerequisites?** → Use `FLATHUB_CHECKLIST.md`
4. **Flathub specific questions?** → Ask at https://discourse.flathub.org/

---

**You're ready to publish ScratchClip to Flathub!** 🎉

