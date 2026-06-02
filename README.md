# BizSim Google Play Install Referrer Bridge

[![Unity 6000.0+](https://img.shields.io/badge/Unity-6000.0%2B-blue.svg)](https://unity.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE.md)
[![Version](https://img.shields.io/badge/Version-1.4.1-orange.svg)](CHANGELOG.md)

Unity bridge for the [Google Play Install Referrer API](https://developer.android.com/google/play/installreferrer) (v2.2).
Retrieves install attribution data including UTM parameters, install timestamps, and referrer URLs.

> **⚠️ Unofficial package.** This is a community-built Unity bridge for the Google Play Install Referrer API. It is **not** an official Google product.

## Features

- **Java-to-C# Bridge** — Full lifecycle management with state machine (IDLE → CONNECTING → CONNECTED → IDLE)
- **UTM Parsing** — Automatic extraction of `utm_source`, `utm_medium`, `utm_campaign`, `utm_content`, `utm_term`
- **Smart Caching** — Persistent cache with `appInstallTime` + `sdkVersion` validation
- **Retry Logic** — Exponential backoff (3 attempts) for transient failures
- **Mock provider** — ScriptableObject presets covering organic, campaign, fake referrer, and error scenarios for editor + non-Android builds
- **Analytics adapter** — Optional `IInstallReferrerAnalyticsAdapter` with a Firebase implementation guarded by `BIZSIM_FIREBASE`
- **UniTask support** — Optional extension assembly guarded by `BIZSIM_UNITASK`
- **Editor integration** — Auto-registered `BIZSIM_INSTALLREFERRER_INSTALLED` define via `editor.core`

## Installation

This package depends on Google's [External Dependency Manager for Unity (EDM4U)](https://github.com/googlesamples/unity-jar-resolver), which is published to the OpenUPM scoped registry. Add EDM4U's registry to your project's `Packages/manifest.json` once, then add this package as a Git URL — UPM will auto-install EDM4U on first import.

**Step 1 — Add the OpenUPM scoped registry (one-time per project):**

```json
{
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.google.external-dependency-manager"
      ]
    }
  ]
}
```

If you already have other OpenUPM-distributed packages, you may already have this registry — just add `com.google.external-dependency-manager` to the existing `scopes` array.

**Step 2 — Install this package via Git URL:**

```json
{
  "dependencies": {
    "com.bizsim.google.play.installreferrer": "https://github.com/BizSim-Game-Studios/com.bizsim.google.play.installreferrer.git#v1.4.1"
  }
}
```

After the package imports, EDM4U is automatically resolved by UPM — no manual `.unitypackage` import required. EDM4U then resolves the Android Maven dependency declared in `Editor/Dependencies.xml` (`com.android.installreferrer:installreferrer:2.2`) at the next Android build, or immediately via `Assets → External Dependency Manager → Android Resolver → Force Resolve`.

## Quick Start

1. Add `InstallReferrerController` to a persistent GameObject (or access the `Instance` singleton).

2. Fetch referrer data:
   ```csharp
   var data = await InstallReferrerController.Instance.FetchInstallReferrerAsync();
   Debug.Log($"Source: {data.UtmSource}, Campaign: {data.UtmCampaign}");
   ```

3. Referrer data is cached automatically after the first successful fetch — subsequent calls return the cached result without re-querying Google Play.

## Use Cases

### Friend Invitation / Referral System

Use `utm_source` to identify who referred the install. The inviter shares a deep link containing their user ID as the UTM source. On first launch, the invited player receives an instant reward; the inviter earns a reward when the invited player reaches a milestone level.

```csharp
var data = await InstallReferrerController.Instance.FetchInstallReferrerAsync();

if (!data.IsOrganic && data.UtmCampaign == "invite")
{
    string inviterUserId = data.UtmSource;
    GrantInstantRewardToInvitee();
    RegisterInviterForMilestoneReward(inviterUserId);
}
```

### Other Common Scenarios

| Scenario | UTM Strategy | Example |
|----------|-------------|---------|
| **Ad Campaign Attribution** | `utm_source=facebook`, `utm_campaign=summer_sale` | Measure which ad network drives the most installs and tune ad spend |
| **Cross-Promotion** | `utm_source=other_game`, `utm_medium=cross_promo` | Track installs from your other games and reward cross-install bonuses |
| **Influencer Tracking** | `utm_source=influencer_name`, `utm_medium=youtube` | Attribute installs to specific creators and calculate ROI per influencer |
| **A/B Store Listing** | `utm_campaign=listing_v2`, `utm_content=new_icon` | Compare conversion rates between different Play Store listing variants |
| **Pre-Registration** | `utm_campaign=preregister` | Identify pre-registered users and grant exclusive launch rewards |
| **Seasonal Events** | `utm_campaign=halloween_2026` | Identify users who installed during a promotion — unlock rewards server-side |

> **⚠️ Security Note:** Never encode reward types, amounts, or bonus identifiers in UTM parameters. Referral links are user-visible and trivially editable. Use UTM data only for **identification** (who invited, which campaign) — all reward logic must live server-side or in game config.

## Requirements

- Unity 6000.0 or later
- Android target platform
- **[EDM4U](https://github.com/googlesamples/unity-jar-resolver) (External Dependency Manager for Unity)** — auto-resolved via OpenUPM scoped registry (see Installation)
- Google Play Install Referrer library 2.2 (resolved automatically via `Editor/Dependencies.xml`)

## Google Play Data Safety

### Data Collected

This package collects the following data via the [Google Play Install Referrer API](https://developer.android.com/google/play/installreferrer):

| Data | Purpose | Example |
|------|---------|---------|
| Referrer URL | Install attribution | `utm_source=google&utm_medium=cpc` |
| UTM parameters | Campaign tracking | `utm_source`, `utm_medium`, `utm_campaign`, `utm_content`, `utm_term` |
| Install timestamps | Attribution timing | Client-side and server-side click/install times |
| Install version | Version tracking | `1.0.84` |

### Data NOT Collected or Shared

- **No personal data** (name, email, phone) is collected by this package
- **No data is shared** with third parties — all data stays on-device in local cache
- **No network calls** are made by this package (the referrer API is a local IPC call to Google Play)

### Play Console Data Safety Form

When filling out the [Data Safety form](https://support.google.com/googleplay/android-developer/answer/10787469) in Google Play Console:

1. **Data types**: Select "Other app info and performance" → "Other diagnostic data"
2. **Collection purpose**: Analytics / App functionality
3. **Shared with third parties**: No
4. **Encrypted**: Yes (if `_useEncryptedCache` is enabled)
5. **Users can request deletion**: Yes (via `ClearCachedData()` / `SetConsentGranted(false)`)

> **GDPR right to erasure:** Call `ClearCachedData()` to delete all cached referrer data. Call `SetConsentGranted(false)` to revoke consent — this also clears cached data and blocks future fetches. Use `LogReferrerFetchedMinimal()` on the analytics adapter to log only `utm_source`, `utm_medium`, `utm_campaign`, and `IsOrganic`, excluding raw URLs, timestamps, and granular UTM fields.

## License

This package's C# and Java source code is licensed under the [MIT License](LICENSE.md) — Copyright (c) 2026 BizSim Game Studios.

## Third-Party Licenses

This package does **not** bundle any Google SDK binaries. The native Android dependency is resolved at build time by [EDM4U](https://github.com/googlesamples/unity-jar-resolver) from the Google Maven repository (`maven.google.com`):

| Dependency | Version | License |
|-----------|---------|---------|
| `com.android.installreferrer:installreferrer` | 2.2 | [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0) |

For full third-party license details, see [NOTICES.md](NOTICES.md).
