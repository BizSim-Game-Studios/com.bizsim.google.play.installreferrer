# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
