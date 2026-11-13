# Discretion

A deadly-serious-looking .NET 11 MAUI app (Android + macOS via Mac Catalyst) for requesting that someone sign a
Non-Disclosure Agreement about matters such as karaoke performances, restroom incidents, or a post-extramarital affair.

> For entertainment purposes only. Generated documents are not legal advice.

## Demo

<p align="center">
  <img src="docs/demo.gif" alt="Screencast: saving the Disclosing Party's details, then creating, signing and executing a Post-Extramarital Affair NDA on Android" width="300">
</p>

Recorded on Android: set up *Your Details* (Disclosing Party: Jordan Huang), then create a Post-Extramarital Affair NDA
for Alice Huang, sign it for both parties on the device and open the executed agreement.
[Full-quality video (MP4)](docs/demo.mp4)

All names, contact details and signatures shown are fictional.

## Workflow

1. **Create New NDA** (first run asks for *Your Details*, used as the Disclosing Party)
2. Select a template (13 included, e.g. Confidential Discussion, Post-Extramarital Affair, Recreational Substance Use)
3. Term of the agreement (months/years)
4. Whether confidentiality survives expiration
5. Receiving party (name, phone, email, address)
6. Governing state law
7. Effective date
8. Review & **Save** – the PDF is generated on the device, then the Disclosing Party is prompted to sign,
   followed by a prompt to hand the device to the Receiving Party.

Each signature regenerates the PDF. Executed copies can be shared, emailed to the receiving party, or followed up by text.

## Project layout

| Path | Purpose |
| --- | --- |
| `Services/TemplateCatalog.cs` | NDA templates (recitals, definitions, special covenants) |
| `Services/NdaComposer.cs` | Assembles the legal text into a renderer-agnostic document model |
| `Services/Pdf/` | Dependency-free PDF writer (standard Times fonts, justified text, vector signatures) |
| `Services/AgreementStore.cs` | JSON persistence in the app data directory |
| `Controls/SignaturePad.cs` | Touch/mouse signature capture (`GraphicsView`) |
| `Views/`, `ViewModels/` | Dashboard, 7-step wizard, agreement/execution page, signing, document preview |

## Build & run

Requires the .NET 11 SDK and the MAUI workload.

```bash
dotnet workload install maui-android maui-maccatalyst   # maccatalyst on macOS only

# Android (device or emulator)
dotnet build -t:Run -f net11.0-android

# macOS (on a Mac with Xcode installed)
dotnet build -t:Run -f net11.0-maccatalyst
```

The `net11.0-maccatalyst` target is only included when building on macOS.

## CI/CD & releasing

GitHub Actions workflows live in `.github/workflows/`:

| Workflow | Triggered by | Does |
| --- | --- | --- |
| `ci.yml` | pull requests, pushes to `main` | Builds the Android APK and the macOS app as `0.0.0-ci.<run>` snapshots, kept for 3 days as workflow artifacts. Docs-only changes are skipped |
| `release.yml` | a `v*` tag push, or a manual run | Builds both apps at a semantic version and publishes them as a GitHub release |
| `build.yml` | the two above | The shared build: publish, check the version, signature and architectures, upload |

### Releasing

Versions follow [Semantic Versioning](https://semver.org): `MAJOR.MINOR.PATCH`, optionally with a prerelease label such
as `1.4.0-rc.1`. The git tag (`v1.4.0`) is the source of truth, and every release carries two assets:

- `Discretion-<version>-android.apk`
- `Discretion-<version>-macos.zip` (a universal app for Apple silicon and Intel)

Either let the workflow pick the next version (run it on `main`; the tag is only created once both builds pass):

```bash
gh workflow run release.yml -f bump=minor                       # 1.2.3 -> 1.3.0
gh workflow run release.yml -f bump=patch -f prerelease=rc.1    # 1.2.3 -> 1.2.4-rc.1
gh workflow run release.yml -f bump=major -f dry_run=true       # build and verify, publish nothing
```

or push the tag yourself:

```bash
git tag v1.3.0 && git push origin v1.3.0
```

`bump` increments the highest stable `vX.Y.Z` tag, counting from `0.0.0` when there is none, so the first release is
`0.0.1`, `0.1.0` or `1.0.0` depending on the bump (or push `v1.0.0` yourself). Prereleases never move that base, so
`rc.1`, `rc.2` and the final release share one version. Prereleases are flagged as such on GitHub.

The version is stamped into the apps at build time, so the `1.0` / `1` placeholders in `Discretion.csproj` only matter
for local builds. Android gets `versionName` = the full version and `versionCode` = the workflow run number; macOS gets
`CFBundleShortVersionString` = `X.Y.Z` (it cannot carry a prerelease label) and `CFBundleVersion` = the run number.
Don't rename or recreate `release.yml` once releases exist: the run number would start over and Android refuses to
install a lower `versionCode` over a higher one.

### Android signing

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

### macOS app

The zip holds a universal `Discretion.app` (macOS 14 or later) that is ad-hoc signed but **not notarized**, which needs a
paid Apple Developer account. Gatekeeper therefore blocks the first launch of a downloaded copy: move the app to
Applications, then choose **Open Anyway** in System Settings > Privacy & Security, or run:

```bash
xattr -dr com.apple.quarantine /Applications/Discretion.app
```

### Good to know

- The macOS job runs on the `xcode-27` runner image (Apple silicon only) because the .NET 11 Mac Catalyst SDK needs
  Xcode 27.2, which `macos-latest` does not have yet. Switch `runs-on` in `build.yml` to `macos-latest` once it does.
- macOS runner minutes cost 10x Linux minutes in private repositories. CI cancels superseded runs and skips docs-only
  changes to keep that down.
- A skipped workflow never satisfies a required status check, so if you add branch protection that requires the build,
  remove `paths-ignore` from `ci.yml`.
- A workflow's "Run workflow" button only appears once its file is on the default branch.
- CI runs no unit tests yet because the project has none; add a `dotnet test` step to `ci.yml` when that changes.
