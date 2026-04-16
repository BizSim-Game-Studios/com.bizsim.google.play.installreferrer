# BizSim Google Play Install Referrer

Last reviewed: 2026-04-16

## Overview

BizSim Google Play Install Referrer is a Unity package that wraps the Google Play Install Referrer API (v2.2). It provides a clean C# interface for retrieving install attribution data on Android, including UTM campaign parameters, referrer URLs, and install timestamps.

The package follows a connect-read-disconnect state machine pattern. A single call to `FetchInstallReferrerAsync()` handles the entire lifecycle: connecting to the Play Store service, reading referrer data, and disconnecting. Results are cached locally with configurable TTL and automatic invalidation on app reinstall or SDK version change.

A GDPR consent toggle allows disabling referrer data collection entirely. A mock provider enables testing in the Unity Editor without a device.

## Table of Contents

| File | Description |
|------|-------------|
| [getting-started.md](getting-started.md) | Installation, EDM4U resolution, and first API call |
| [api-reference.md](api-reference.md) | Full public API: controller, data types, enums, interfaces |
| [configuration.md](configuration.md) | InstallReferrerSettings asset and Editor window walkthrough |
| [architecture.md](architecture.md) | JNI bridge diagram, thread model, provider selection |
| [troubleshooting.md](troubleshooting.md) | Common errors with root causes and fixes |
| [DATA_SAFETY.md](DATA_SAFETY.md) | Play Store Data Safety form guidance |

## Links

- [README](../README.md) -- quick-start experience
- [CHANGELOG](../CHANGELOG.md) -- version history
- [GitHub Repository](https://github.com/BizSim-Game-Studios/com.bizsim.google.play.installreferrer)
