# GitHub Setup Instructions - BrewedCode-Core

**Status**: ✅ Local Git Repository Ready
**Location**: `Assets/BrewedCode-UPM/`
**Branch**: `main`
**Tag**: `v1.0.0`
**Files**: 276 total (214 C#, 31 asmdef, 28 docs, configs)

---

## 📋 Current Local Git Status

```
✅ Git initialized
✅ Initial commit created (c52cf1b)
✅ All 276 files staged and committed
✅ main branch configured
✅ v1.0.0 tag created
⏳ Remote not yet configured
```

### Commit Summary
```
Author: BrewedCode Team <brewedcode@example.com>
Hash: c52cf1b
Message: Initial release: BrewedCode Core Framework v1.0.0
Files: 276 changed, 38302 insertions
```

---

## 🚀 Step-by-Step GitHub Setup

### Step 1: Create GitHub Repository

1. Go to **https://github.com/new**
2. Fill in the form:
   - **Repository name**: `BrewedCode-Core`
   - **Description**: "Production-grade modular framework for Unity with event-driven architecture, logging, timers, and game systems"
   - **Visibility**: Public (recommended for open source)
   - **Add .gitignore**: No (already included in package)
   - **Add license**: MIT (already included in package)
   - ✅ Check "Initialize this repository with a README" **DO NOT CHECK** (we have our own)
   - ✅ Check "Add .gitignore" **DO NOT CHECK** (already included)
   - ✅ Check "Add a license" **DO NOT CHECK** (already MIT)

3. Click "Create repository"

4. Copy the HTTPS URL:
   ```
   https://github.com/yourusername/BrewedCode-Core.git
   ```
   (Replace `yourusername` with your actual GitHub username)

---

### Step 2: Add Remote and Push to GitHub

Replace `yourusername` with your actual GitHub username in these commands:

```bash
cd "C:/dev/unity/projects/farm_space/FarmSpace/Assets/BrewedCode-UPM"

# Add remote
git remote add origin https://github.com/yourusername/BrewedCode-Core.git

# Push main branch
git push -u origin main

# Push the v1.0.0 tag
git push origin v1.0.0

# Verify
git remote -v
```

### Step 3: Verify on GitHub

1. Go to your repository: `https://github.com/yourusername/BrewedCode-Core`
2. Check:
   - ✅ Code is on `main` branch (276 files)
   - ✅ Commit message visible (Initial release: BrewedCode Core Framework v1.0.0)
   - ✅ README.md displays correctly
   - ✅ package.json visible

---

## 📝 GitHub Release Setup

After pushing to GitHub, create an official release:

1. Go to: `https://github.com/yourusername/BrewedCode-Core/releases/new`

2. Fill in:
   - **Tag version**: `v1.0.0` (select from dropdown after push)
   - **Release title**: `BrewedCode Core Framework v1.0.0`
   - **Description**: Copy from [CHANGELOG.md](CHANGELOG.md)

3. Click "Publish release"

---

## 🔗 Installation Instructions (for users)

After pushing to GitHub, users can install the package using:

### Via Package Manager UI (Easiest)
```
Window → Package Manager → + button → Add package from git URL
https://github.com/yourusername/BrewedCode-Core.git#1.0.0
```

### Via manifest.json
```json
{
  "dependencies": {
    "com.brewedcode.core": "https://github.com/yourusername/BrewedCode-Core.git#1.0.0"
  }
}
```

### Via Direct Clone
```bash
cd Packages/
git clone https://github.com/yourusername/BrewedCode-Core.git com.brewedcode.core
cd com.brewedcode.core
git checkout 1.0.0
```

---

## 📋 Quick Reference Commands

### View current remote
```bash
cd "C:/dev/unity/projects/farm_space/FarmSpace/Assets/BrewedCode-UPM"
git remote -v
```

### Add remote (if not done yet)
```bash
git remote add origin https://github.com/yourusername/BrewedCode-Core.git
```

### Push all branches and tags
```bash
git push -u origin main
git push origin --tags
```

### Push specific tag only
```bash
git push origin v1.0.0
```

### View tags
```bash
git tag -l
```

### View commit details
```bash
git show v1.0.0
```

---

## 📊 Package Details for Users

**Package Name**: `com.brewedcode.core`
**Version**: `1.0.0`
**Unity**: 2021.3+
**Dependencies**: TextMeshPro 3.0.6+

**Includes**:
- ✅ Foundations: Events, Logging, Singleton, TimerManager
- ✅ Systems: ItemHub, ResourceBay, Crafting, Theme
- ✅ Utils: Core, VitalGauge, Signals, Animancer, MoreMountains, GraphToolkit
- ✅ Documentation: ARCHITECTURE.md + component guides
- ✅ Tests: 20+ unit tests
- ✅ License: MIT

---

## 🔐 Security Notes

- ⚠️ No secrets in repository (no .env, credentials, etc.)
- ✅ .gitignore configured to exclude .meta files
- ✅ .gitattributes configured for consistent line endings
- ✅ MIT License allows commercial and private use

---

## 🚨 Troubleshooting

### "fatal: remote origin already exists"
```bash
git remote remove origin
git remote add origin https://github.com/yourusername/BrewedCode-Core.git
```

### "Everything up-to-date"
Means remote already has the commits. Verify with:
```bash
git log --oneline -5
```

### "fatal: not a git repository"
Ensure you're in the correct directory:
```bash
cd "C:/dev/unity/projects/farm_space/FarmSpace/Assets/BrewedCode-UPM"
git status
```

### Authentication Issues
If you see "fatal: unable to access":
1. Check your GitHub username and password
2. If using 2FA, use a Personal Access Token instead of password
3. See: https://github.com/settings/tokens

---

## ✅ Completion Checklist

- [ ] Create GitHub repository at github.com/new
- [ ] Copy repository HTTPS URL
- [ ] Run `git remote add origin ...`
- [ ] Run `git push -u origin main`
- [ ] Run `git push origin v1.0.0`
- [ ] Verify code on GitHub website
- [ ] Create GitHub Release from v1.0.0 tag
- [ ] Test installation with Git URL in Package Manager
- [ ] Update main project to use package (Phase 4)

---

## 📞 Support

For issues with the package itself, see:
- [ARCHITECTURE.md](Documentation~/ARCHITECTURE.md) - System design
- [README.md](README.md) - Installation & overview
- Component documentation in `Documentation~/`

For Git/GitHub issues:
- Git docs: https://git-scm.com/doc
- GitHub docs: https://docs.github.com

---

**Generated**: 2026-01-26
**Status**: Ready for GitHub
**Next Step**: Create repository and push
