# Shipping Guide for Evoq.Ethereum

This document outlines the complete process for shipping a new release of Evoq.Ethereum to ensure consistency and prevent version mismatches.

## Pre-Release Checklist

Before starting the release process, ensure:

- [ ] All tests pass (`dotnet test`)
- [ ] No critical warnings in build
- [ ] All public APIs are documented
- [ ] No TODO comments remain
- [ ] Changes are committed and pushed to main branch
- [ ] Working directory is clean (`git status`)

## Release Process

### 1. Version Management

**CRITICAL**: Always check current versions before proceeding:

```bash
# Check current project version
grep '<Version>' src/Evoq.Ethereum/Evoq.Ethereum.csproj

# Check latest git tag
git tag --list --sort=-version:refname | head -1

# Check latest NuGet version
curl -s "https://api.nuget.org/v3/registration5-semver1/evoq.ethereum/index.json" | grep -o '"version":"[^"]*"' | tail -1
```

**Version Bump Rules**:
- **PATCH** (3.2.0 → 3.2.1): Bug fixes, no new features
- **MINOR** (3.2.0 → 3.3.0): New features, backward compatible
- **MAJOR** (3.2.0 → 4.0.0): Breaking changes

### 2. Update Version

Update the version in `src/Evoq.Ethereum/Evoq.Ethereum.csproj`:

```xml
<Version>3.3.0</Version>
```

### 3. Update CHANGELOG.md

Add a new entry at the top of `CHANGELOG.md`:

```markdown
## [3.3.0] - 2026-01-13

### Added
- Added optional `IHttpClientFactory` support to `Chain` and `ChainClient` for better HTTP client management
- Added comprehensive tests for HttpClientFactory functionality

### Updated
- Updated `Microsoft.Extensions.Http` dependency from 8.0.0 to 10.0.1
- Updated test project dependencies to 10.0.1

### Changed
- Improved comments in `AbiDecoder.cs` for clarity
- Removed unused using statement in `JsonRpcClient.cs`
```

### 4. Build and Test

```bash
# Run full build and test suite
./build.sh

# Verify package was created
ls -la artifacts/
```

**Note**: Some tests require a running Hardhat node on `localhost:8545`. These tests will fail if the node is not running, but this is expected and does not block the release.

### 5. Commit Version Changes

```bash
# Commit version and changelog updates
git add .
git commit -m "v3.3.0: Add IHttpClientFactory support and update dependencies"
git push origin master
```

### 6. Create Git Tag

```bash
# Create annotated tag
git tag -a v3.3.0 -m "v3.3.0: Add IHttpClientFactory support and update dependencies

- Added optional IHttpClientFactory support to Chain and ChainClient
- Updated Microsoft.Extensions.Http dependency from 8.0.0 to 10.0.1
- Updated test project dependencies to 10.0.1
- Code cleanup and comment improvements"

# Push tag to remote
git push origin v3.3.0
```

### 7. Create GitHub Release

**Option A: Using GitHub CLI (Recommended)**
```bash
# Create release with changelog from file
gh release create v3.3.0 \
  --title "v3.3.0: Add IHttpClientFactory support and update dependencies" \
  --notes-file CHANGELOG.md

# Or create with inline notes
gh release create v3.3.0 \
  --title "v3.3.0: Add IHttpClientFactory support and update dependencies" \
  --notes "## Added

- Added optional \`IHttpClientFactory\` support to \`Chain\` and \`ChainClient\`..."
```

**Option B: Using GitHub Web Interface**
1. Go to https://github.com/lukepuplett/evoq-ethereum/releases
2. Click "Create a new release"
3. Select the tag you just pushed (v3.3.0)
4. Set release title: "v3.3.0: Add IHttpClientFactory support and update dependencies"
5. Copy the changelog content from `CHANGELOG.md`
6. **DO NOT** upload the .nupkg file manually - publish to NuGet separately
7. Click "Publish release"

### 8. Publish to NuGet

**Manual Upload to NuGet.org** (Recommended):
1. Go to https://www.nuget.org/packages/manage/upload
2. Upload the .nupkg file from `./artifacts/Evoq.Ethereum.3.3.0.nupkg`
3. Verify package metadata and click "Submit"

**Alternative: Using publish script** (if automated publishing is configured):
```bash
# Set your NuGet API key
export NUGET_API_KEY="your-nuget-api-key"

# Publish to NuGet.org
./publish.sh
```

**Note**: The GitHub Actions workflow (`.github/workflows/publish.yml`) is configured to automatically publish when a tag is pushed, but manual upload is the current standard practice.

### 9. Verify Release

After publishing, verify:

```bash
# Check NuGet.org has the new version
curl -s "https://api.nuget.org/v3/registration5-semver1/evoq.ethereum/index.json" | grep -o '"version":"[^"]*"' | tail -1

# Should show: "version":"3.3.0"
```

## Common Issues and Solutions

### Version Mismatch
If you see different versions in different places:
1. **Project file**: Check `src/Evoq.Ethereum/Evoq.Ethereum.csproj`
2. **Git tags**: Check `git tag --list`
3. **NuGet.org**: Check the API response above

### Missing Git Tag
If a version was published to NuGet but no git tag exists:
1. Create the missing tag: `git tag -a v3.2.0 -m "Version 3.2.0 - Previous release"`
2. Push the tag: `git push origin v3.2.0`
3. Create a GitHub release for that tag

### Build Failures
If `./build.sh` fails:
1. Check for test failures: `dotnet test`
2. Check for build warnings: `dotnet build --configuration Release`
3. Fix issues before proceeding

### Test Failures Due to Missing Hardhat Node
Some tests require a running Hardhat node. If tests fail with "Connection refused (localhost:8545)", this is expected when the node is not running. These tests can be skipped for release purposes, but should pass in CI/CD environments where the node is available.

## Release Templates

### Git Tag Message Template
```
vX.Y.Z: Brief description of main features

- Feature 1 description
- Feature 2 description
- Fix 1 description
```

### GitHub Release Title Template
```
vX.Y.Z: Main Feature Description
```

### Changelog Entry Template
```markdown
## [X.Y.Z] - YYYY-MM-DD

### Added
- New feature 1
- New feature 2

### Fixed
- Bug fix 1
- Bug fix 2

### Changed
- Breaking change 1
- Enhancement 1

### Updated
- Dependency update 1
- Dependency update 2
```

## Managing Releases with GitHub CLI

### Useful GitHub CLI Commands
```bash
# List all releases
gh release list

# View a specific release
gh release view v3.3.0

# Edit an existing release
gh release edit v3.3.0 --title "New Title" --notes "New notes"

# Create a draft release for review
gh release create v3.3.0 --title "v3.3.0" --notes "Notes" --draft

# Upload assets to an existing release
gh release upload v3.3.0 artifacts/Evoq.Ethereum.3.3.0.nupkg
```

### Authentication
Make sure you're authenticated with GitHub CLI:
```bash
# Check authentication status
gh auth status

# Login if needed
gh auth login
```

## Emergency Procedures

### Reverting a Release
If a release needs to be reverted:

1. **DO NOT** delete the git tag (it's immutable)
2. **DO NOT** unpublish from NuGet (it's permanent)
3. Create a new patch release with fixes
4. Document the issue in the changelog

### Hotfix Release
For critical bug fixes:

1. Create a hotfix branch: `git checkout -b hotfix/3.3.1`
2. Make minimal changes to fix the issue
3. Bump version to 3.3.1
4. Follow normal release process
5. Merge hotfix branch back to master

## Automation Notes

The repository includes a GitHub Actions workflow (`.github/workflows/publish.yml`) that is configured to automatically publish to NuGet when a tag matching `v*` is pushed. However, manual upload to NuGet.org is currently the standard practice. The workflow may be enabled in the future once the `NUGET_API_KEY` secret is properly configured and tested.
