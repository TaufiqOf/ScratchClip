# Flathub Submission Checklist for ScratchClip

Use this checklist to ensure everything is ready before submitting to Flathub.

## Pre-Submission Requirements

### Repository Setup
- [ ] GitHub repository is public
- [ ] Repository URL is correct: `https://github.com/taufiq/ClipboardManagerX`
- [ ] Repository has a proper README
- [ ] Repository has a LICENSE file (preferably at root level)
- [ ] All code is committed and pushed to main branch

### Manifest Files
- [ ] `com.github.taufiq.scratchclip.json` created and valid
- [ ] `com.github.taufiq.scratchclip.desktop` created and valid
- [ ] `com.github.taufiq.scratchclip.appdata.xml` created and valid
- [ ] All manifest files committed to repository
- [ ] Manifest file references the correct GitHub URL

### Icons and Assets
- [ ] Application icon is 256x256 pixels or larger
- [ ] Icon is PNG format
- [ ] Icon is recognizable at small sizes (16x16)
- [ ] Icons are committed to the repository at `ScratchClip/Assets/`
- [ ] Appdata XML includes valid screenshot URLs (must be HTTPS)

### Application Metadata
- [ ] Application name is correct: "ScratchClip"
- [ ] Application ID is correct: `com.github.taufiq.scratchclip`
- [ ] Short description is concise and informative
- [ ] Full description explains key features
- [ ] License is specified (currently marked as proprietary)
- [ ] Developer/author information is complete
- [ ] Keywords are relevant and accurate

## Build Testing

### Local Build Test
- [ ] Flatpak and flatpak-builder are installed
- [ ] Build succeeds: `flatpak-builder --user --install --force-clean build-dir com.github.taufiq.scratchclip.json`
- [ ] Application launches successfully: `flatpak run com.github.taufiq.scratchclip`
- [ ] Core features work correctly:
  - [ ] Clipboard monitoring functions
  - [ ] History is displayed
  - [ ] Entries can be copied
  - [ ] Search functionality works
  - [ ] Settings can be accessed
  - [ ] Tray integration works
- [ ] No obvious errors or warnings in console
- [ ] Application exits cleanly

### Version Consistency
- [ ] Version in `ScratchClip.csproj` (AssemblyVersion) is set
- [ ] Version in appdata.xml `<release>` matches above
- [ ] Version follows semantic versioning (e.g., 0.0.56)

## Submission Method Selection

### Option A: Online Submission (Recommended for First-Time)
- [ ] Visit https://flathub.org/en/submit/
- [ ] Have GitHub repository URL ready
- [ ] Application description finalized
- [ ] Have a way to receive Flathub notifications

### Option B: Manual Repository Setup
- [ ] GitHub account with write permissions
- [ ] Flathub organization membership request made
- [ ] Repository created at: `https://github.com/flathub/com.github.taufiq.scratchclip`
- [ ] Files added to proper directory structure:
  ```
  com/
  └── github/
      └── taufiq/
          └── scratchclip/
              ├── com.github.taufiq.scratchclip.json
              ├── com.github.taufiq.scratchclip.desktop
              └── com.github.taufiq.scratchclip.appdata.xml
  ```
- [ ] Proper permissions set in Flathub organization

## Quality Assurance

### Code Quality
- [ ] Application builds without warnings
- [ ] No hardcoded credentials or API keys in code
- [ ] Third-party dependencies are legitimate and maintained
- [ ] All dependencies are compatible with .NET 10

### Permissions Review
- [ ] Permissions in manifest are justified
- [ ] No unnecessary permissions requested:
  - [ ] `--share=network` - needed for YouTube previews ✓
  - [ ] `--filesystem=home` - needed for history storage ✓
  - [ ] System bus access - needed for tray/clipboard ✓
  - Other permissions reviewed and justified

### Documentation
- [ ] README.md has installation instructions
- [ ] FLATHUB_SUBMISSION.md is accurate
- [ ] FLATPAK_MANIFEST_README.md explains the manifest
- [ ] Comments in manifest files are clear where necessary

## Common Issues Prevention

- [ ] No references to hardcoded paths that won't work in Flatpak sandbox
- [ ] Application doesn't require elevated privileges
- [ ] No direct X11 or Wayland assumptions; uses portals where applicable
- [ ] Temporary files use `$TMPDIR` or XDG directories
- [ ] Config files stored in `$XDG_CONFIG_HOME` or `$HOME/.config`
- [ ] Data stored in `$XDG_DATA_HOME` or `$HOME/.local/share`
- [ ] No assumptions about system-wide installations
- [ ] All external resources use HTTPS URLs

## Post-Submission Steps

### After Creating the Flathub Repository
- [ ] Create initial commit with manifest files
- [ ] Set up branch protection rules on main branch:
  - [ ] Require pull request reviews
  - [ ] Require status checks to pass
  - [ ] Require branches to be up to date

### After Initial Approval
- [ ] Application appears on https://flathub.org/apps/
- [ ] Test installation from Flathub:
  ```bash
  flatpak install flathub com.github.taufiq.scratchclip
  flatpak run com.github.taufiq.scratchclip
  ```
- [ ] Monitor Flathub statistics and user feedback
- [ ] Set up automated CI/CD if not already done
- [ ] Configure automatic updates in appdata.xml as versions are released

## Release Process (For Future Updates)

For each new release:
1. [ ] Update version in `ScratchClip.csproj`
2. [ ] Update appdata.xml with release notes
3. [ ] Test locally with flatpak-builder
4. [ ] Create git tag: `git tag v0.0.X`
5. [ ] Push changes to upstream
6. [ ] Flathub CI automatically rebuilds and updates

## Contact Information

For questions or issues:
- **Flathub Docs**: https://docs.flathub.org/
- **Flathub Discourse**: https://discourse.flathub.org/
- **Flatpak Docs**: https://docs.flatpak.org/
- **GitHub Flathub Issues**: https://github.com/flathub/flathub/issues

## Notes

- Application ID (`com.github.taufiq.scratchclip`) cannot be changed after submission without significant Flathub migration process
- Always test the Flatpak build locally before submission
- Respond promptly to Flathub review comments during submission
- Keep manifest and appdata files updated with each application release

