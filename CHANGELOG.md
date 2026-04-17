# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2026-04-17

### Added
- **K8 PackageVersion schema unification (Plan G).** Three new `public const string` fields on `PackageVersion`: `NativeSdkVersion` (`"2.2"`), `NativeSdkLabel` (`"Install Referrer"`), `NativeSdkArtifactCoord` (`"com.android.installreferrer:installreferrer:2.2"`). First introduction for this package — the `NativeSdkLabel` correctly identifies Install Referrer as its own SDK family (NOT "Play Core") in the dashboard. See `development-plans/plans/2026-04-17-enterprise-quality-bar/06-conventions/06-package-version-schema.md`.
- `PackageVersionSchemaTest` drift guard.

## [1.0.3] - 2026-04-17

### Fixed
- **GDPR right-to-erasure: `ConsentGranted` now persists across app restarts** (C2.2 compliance). `InstallReferrerController.SetConsentGranted(bool)` writes the flag to `PlayerPrefs` key `BizSim.InstallReferrer.ConsentGranted`; `Awake()` reads it on controller init. Prior to this release, revocations (`SetConsentGranted(false)`) were in-memory only and silently reset to `true` on the next app boot — a GDPR Article 17 compliance gap. Defaults to `true` on fresh install for backward compat with v1.0.2-and-earlier consumers. Added `ConsentPersistenceTest` (4 assertions: set-false, set-true, default-true, key-namespacing). `SECURITY.md` updated with persistence documentation.

## [1.0.2] - 2026-04-16

### Added
- `ReleaseDate` field in PackageVersion.cs for dashboard version display
- `[InitializeOnLoad]` EditorInit registering `BIZSIM_INSTALLREFERRER_INSTALLED` define

## [1.0.1] - 2026-04-15

### Fixed
- Relaxed runtime asmdef `includePlatforms` from `["Android", "Editor"]` to `[]`
  to fix a consumer-side `CS0246: The type or namespace name 'BizSim' could not
  be found` regression that appeared during Addressables content build on Android
  target. The Editor compile pass resolved the auto-reference correctly, but the
  Player script compile pass did not — a known Unity issue when `autoReferenced`
  library assemblies are platform-gated at the asmdef level.

  Runtime platform safety is preserved by the existing `#if UNITY_ANDROID && !UNITY_EDITOR`
  guards around every JNI call site; non-Android builds continue to route through
  `Mock<Api>Provider` per CROSS-PACKAGE-INVARIANTS §4.

  No API surface change. Consumers with existing `using BizSim.Google.Play.InstallReferrer;`
  imports require no action — the fix is transparent on the next package install.

## [1.0.0] - 2026-04-14

### Added

- Initial release of `com.bizsim.google.play.installreferrer` — Unity bridge for the Google Play Install Referrer API (v2.2).
- Java JNI bridge with `.androidlib` subproject and ProGuard keep rules.
- C# singleton controller with async API surface for install attribution, UTM campaign tracking, and referrer data caching.
- Mock provider for editor testing without a device.
- Optional Firebase Analytics integration via `BIZSIM_FIREBASE` versionDefine.
- `Samples~/BasicIntegration` minimal usage example.
- `Samples~/MockPresets` pre-configured mock scenarios (organic, campaign, fake referrer, error).
- Tests under `Tests/Editor/` and `Tests/Runtime/`.

### Notes

- This is the first release under the new `com.bizsim.google.play.*` family naming. The previous incarnation (`com.bizsim.gplay.installreferrer`) at version 0.2.5 is archived and no longer maintained.
- Floor: Unity 6.0 LTS (`6000.0`).
