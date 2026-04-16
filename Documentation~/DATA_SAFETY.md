# Data Safety

## Play Store Data Safety Form Guidance

This document describes what data flows through `com.bizsim.google.play.installreferrer` to help consumers fill out the Google Play Store Data Safety form.

## Data Collected

| Data Type | Collected | Persisted | Transmitted | Purpose |
|-----------|-----------|-----------|-------------|---------|
| Install referrer URL | Yes | Yes (encrypted, local) | No | Install attribution |
| UTM parameters | Yes (parsed from referrer URL) | Yes (encrypted, local) | No | Campaign tracking |
| Referrer click timestamp | Yes | Yes (encrypted, local) | No | Attribution timing |
| Install begin timestamp | Yes | Yes (encrypted, local) | No | Attribution timing |
| Google Play Instant flag | Yes | Yes (encrypted, local) | No | Install type detection |

## How Data Flows

1. **Collection:** The package calls `InstallReferrerClient.getInstallReferrer()` via JNI. The Play Store returns the referrer string and timestamps. This data originates from Google Play, not from the user's device sensors.

2. **Local persistence:** Referrer data is cached locally using `EncryptedPlayerPrefsCacheProvider` (AES-encrypted PlayerPrefs). The cache has a configurable TTL (default 24 hours). Cache invalidation occurs on app reinstall, SDK version change, or manual clear.

3. **Transmission:** This package does NOT transmit referrer data to any server. If the consuming app sends referrer data to its own backend (e.g., via the `IInstallReferrerAnalyticsAdapter`), that transmission is the app's responsibility to declare.

## GDPR Compliance

The package provides `SetConsentGranted(bool)` on the controller. When consent is not granted:
- No connection to the Play Store is made
- No referrer data is read or cached
- The controller returns a `ConsentNotGranted` error immediately

## Play Store Form Entries

Based on the data above, consumers should declare:

- **Data type:** Other > Install referrer
- **Collected:** Yes
- **Shared with third parties:** No (unless your app transmits it)
- **Processing purpose:** Analytics (install attribution)
- **Is data encrypted in transit:** N/A (no network transmission by this package)
- **Can users request data deletion:** Yes (via `ClearCache()` or app uninstall)
