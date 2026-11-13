# CI/CD & releasing

GitHub Actions workflows live in [`.github/workflows/`](../.github/workflows/):

| Workflow | Triggered by | Does |
| --- | --- | --- |
| [`ci.yml`](../.github/workflows/ci.yml) | pull requests, pushes to `main` | Builds the Android APK and the macOS app as `0.0.0-ci.<run>` snapshots, kept for 3 days as workflow artifacts. Docs-only changes are skipped |
| [`release.yml`](../.github/workflows/release.yml) | pushes to `main`, or a manual run on `main` | Uses semantic-release to determine the next version from commits, build both apps, and publish a GitHub release |
| [`build.yml`](../.github/workflows/build.yml) | the two above | The shared build: publish, check the version, signature and architectures, upload |

## Releasing

The [semantic-release action](https://github.com/cycjimmy/semantic-release-action) manages versions, release notes, git
tags and GitHub releases automatically. Configuration lives in [`.releaserc.json`](../.releaserc.json); this is an app release, not an npm
package, so no npm publishing or npm token is needed. The workflow uses GitHub's built-in `GITHUB_TOKEN`.

Use [Conventional Commits](https://www.conventionalcommits.org/) for commits merged into `main` (or the PR title when
squash-merging). semantic-release examines all commits since the last release and selects the largest required bump:

| Commit | Version change |
| --- | --- |
| `fix: correct signature export` or `perf: speed up PDF rendering` | Patch (`1.2.3` -> `1.2.4`) |
| `feat: add a new agreement template` | Minor (`1.2.3` -> `1.3.0`) |
| `feat!: change the agreement format`, or a `BREAKING CHANGE:` footer | Major (`1.2.3` -> `2.0.0`) |
| `docs:`, `chore:`, `ci:`, `test:`, or other non-releasing commits without a breaking change | No release |

The first releasable commit produces `v1.0.0`. Only stable releases from `main` are configured; don't create release tags
manually. If there are no releasable changes, the release builds and publishing are skipped.

The workflow first previews the next version without publishing, stamps it into both apps, and only then runs
semantic-release to create the tag and release. A guard prevents publishing if the version changes while the builds
are running. Every release carries two assets:

- `Discretion-<version>-android.apk`
- `Discretion-<version>-macos.zip` (a universal app for Apple silicon and Intel)

For a manual run using the same commit-based versioning:

```bash
gh workflow run release.yml --ref main                     # release if there are unreleased changes
gh workflow run release.yml --ref main -f dry_run=true      # build the next version, publish nothing
```

The version is stamped into the apps at build time, so the `1.0` / `1` placeholders in `Discretion.csproj` only matter
for local builds. Android gets `versionName` = the full version and `versionCode` = the workflow run number; macOS gets
`CFBundleShortVersionString` = `X.Y.Z` (it cannot carry a prerelease label) and `CFBundleVersion` = the run number.
Don't rename or recreate `release.yml` once releases exist: the run number would start over and Android refuses to
install a lower `versionCode` over a higher one.

## Android signing

Android installs an update over an existing app only when both APKs are signed with the same key, so releases are signed
with a keystore kept in repository secrets. CI snapshots use a throwaway debug key.

```bash
# Once. Keep the keystore and its password safe: without them existing installs can no longer be updated
keytool -genkeypair -v -keystore discretion-release.keystore -alias discretion \
  -keyalg RSA -keysize 2048 -validity 10000

base64 < discretion-release.keystore | gh secret set ANDROID_KEYSTORE_BASE64
gh secret set ANDROID_KEYSTORE_PASSWORD
gh secret set ANDROID_KEY_ALIAS --body discretion
```

Use the same password for the keystore and the key (PKCS12 keystores require it). A legacy JKS keystore with a separate
key password also needs the `ANDROID_KEY_PASSWORD` secret. Until `ANDROID_KEYSTORE_BASE64` exists, releases fall back to
the debug key and the run shows a warning; that APK cannot update an app installed from a differently signed APK, and
uninstalling the old one deletes its data.

## macOS app

The zip holds a universal `Discretion.app` (macOS 14 or later) that is ad-hoc signed but **not notarized**, which needs a
paid Apple Developer account. Gatekeeper therefore blocks the first launch of a downloaded copy: move the app to
Applications, then choose **Open Anyway** in System Settings > Privacy & Security, or run:

```bash
xattr -dr com.apple.quarantine /Applications/Discretion.app
```

## Good to know

- The macOS job runs on the `xcode-27` runner image (Apple silicon only) because the .NET 11 Mac Catalyst SDK needs
  Xcode 27.2, which `macos-latest` does not have yet. Switch `runs-on` in `build.yml` to `macos-latest` once it does.
- macOS runner minutes cost 10x Linux minutes in private repositories. CI cancels superseded runs and skips docs-only
  changes to keep that down.
- A skipped workflow never satisfies a required status check, so if you add branch protection that requires the build,
  remove `paths-ignore` from `ci.yml`.
- A workflow's "Run workflow" button only appears once its file is on the default branch.
- CI runs no unit tests yet because the project has none; add a `dotnet test` step to `ci.yml` when that changes.
