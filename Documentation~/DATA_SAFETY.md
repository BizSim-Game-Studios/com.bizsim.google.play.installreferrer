# Data Safety Declaration

This document describes the data practices of the **Google Play Install Referrer Bridge** package
(`com.bizsim.google.play.installreferrer`) for compliance with Google Play Data Safety requirements.

## Data Collected

| Data Type | Collected? | Shared? | Purpose | Retention |
|-----------|-----------|---------|---------|-----------|
| Referrer URL | Yes (from Google Play Install Referrer API) | No | Install attribution | Cached in `PlayerPrefs` (90-day TTL) |
| UTM parameters | Derived locally from referrer URL | No | Campaign tracking | Cached in `PlayerPrefs` (90-day TTL) |
| Install timestamps | Yes (from Google Play Install Referrer API) | No | Attribution timing | Cached in `PlayerPrefs` (90-day TTL) |
| Install version | Yes (from Google Play Install Referrer API) | No | Version tracking | Cached in `PlayerPrefs` (90-day TTL) |
| App install time | Yes (from `PackageManager`) | No | Cache invalidation on reinstall | Cached in `PlayerPrefs` |
| API call success/error | Yes (if analytics adapter is injected) | Per adapter implementation | Technical monitoring | Per adapter retention policy |

## Data NOT Collected

- ❌ No personal information (name, email, phone)
- ❌ No device identifiers (IMEI, advertising ID)
- ❌ No location data
- ❌ No financial information
- ❌ No health or fitness data
- ❌ No browsing history or search queries

## Key Privacy Principles

### 1. Local IPC Only — No External Network Requests
The Google Play Install Referrer API is a **local IPC call** to the Google Play Store app
on the device. This package does not make any HTTP requests or connect to any external servers.
All data retrieval happens on-device via Android's `AIDL` binding mechanism.

### 2. Immutable Install-Time Data
Referrer data is set **once at install time** and never changes. It captures the attribution
link that led to the install (e.g., ad campaign UTM parameters). This data cannot be
updated or modified after the initial install.

### 3. Cache Invalidation
Cached data is automatically invalidated when:
- The cache exceeds the **90-day TTL**
- The app is **reinstalled** (detected via `PackageManager.firstInstallTime`)
- The **SDK version changes** (detected via `PackageVersion.Current`)

### 4. Consent Management
The controller supports explicit consent management via `SetConsentGranted()`:
- `SetConsentGranted(false)` — clears all cached data and blocks future API calls
- `ClearCachedData()` — deletes cached referrer data without affecting consent state

### 5. Data Minimization
The `IInstallReferrerAnalyticsAdapter` interface provides two logging methods:
- `LogReferrerFetched()` — full data logging (for internal analytics)
- `LogReferrerFetchedMinimal()` — logs only `utm_source`, `utm_medium`, `utm_campaign`, and `IsOrganic`

## Google Play Data Safety Form

When filling out the [Data Safety form](https://support.google.com/googleplay/android-developer/answer/10787469) in Google Play Console:

| Question | Answer |
|----------|--------|
| Does your app collect or share user data? | Yes |
| Data type | Other app info and performance → Other diagnostic data |
| Is the data collected, shared, or both? | Collected only |
| Is data processing ephemeral? | No — cached locally for 90 days |
| Is data collection required or optional? | Optional (can be disabled via `SetConsentGranted(false)`) |
| Purpose | Analytics — install attribution and campaign tracking |
| Encrypted in transit? | N/A — local IPC only, no network transit |
| Encrypted at rest? | Yes (if `_useEncryptedCache` is enabled) |
| Users can request deletion? | Yes (via `ClearCachedData()` or `SetConsentGranted(false)`) |

## Contact

For privacy-related questions about this package:
- **Author:** Aşkın Ceyhan (https://github.com/AskinCeyhan)
- **Company:** BizSim Game Studios (https://www.bizsim.com)
