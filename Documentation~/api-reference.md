# API Reference

Namespace: `BizSim.Google.Play.InstallReferrer`

Assembly: `BizSim.Google.Play.InstallReferrer`

---

## InstallReferrerController

`public class InstallReferrerController : MonoBehaviour, IInstallReferrerProvider`

Singleton controller for the Google Play Install Referrer API. Creates a `DontDestroyOnLoad` GameObject on first access.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Instance` | `InstallReferrerController` | Lazy singleton; auto-creates if not found in scene |

### Methods

| Method | Return | Description |
|--------|--------|-------------|
| `FetchInstallReferrerAsync()` | `Task<InstallReferrerResult>` | Connects to Play Store, reads referrer data, disconnects. Returns cached data if valid. |
| `ClearCache()` | `void` | Invalidates the local referrer cache |
| `SetConsentGranted(bool granted)` | `void` | Enables or disables GDPR consent for referrer collection |

### Events

| Event | Type | Description |
|-------|------|-------------|
| `OnCacheInvalidated` | `Action<CacheInvalidationReason>` | Fired when the local cache is cleared |
| `OnFetchCompleted` | `Action<InstallReferrerResult>` | Fired after each fetch attempt (success or failure) |

### SerializeField Overrides

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `_mockConfig` | `InstallReferrerMockConfig` | `null` | ScriptableObject controlling mock behavior in Editor |
| `_useMockInDevelopmentBuild` | `bool` | `false` | Use mock provider in Development Builds |

---

## InstallReferrerResult

`public readonly struct InstallReferrerResult`

| Property | Type | Description |
|----------|------|-------------|
| `IsSuccess` | `bool` | `true` when referrer data was retrieved |
| `ErrorCode` | `InstallReferrerErrorCode` | Error code (Ok=0 on success) |
| `Data` | `InstallReferrerData` | The referrer payload (valid only when IsSuccess) |

---

## InstallReferrerData

`public readonly struct InstallReferrerData`

| Property | Type | Description |
|----------|------|-------------|
| `RawReferrerUrl` | `string` | Full `utm_*` query string from the Play Store |
| `UtmSource` | `string` | Parsed `utm_source` parameter |
| `UtmMedium` | `string` | Parsed `utm_medium` parameter |
| `UtmCampaign` | `string` | Parsed `utm_campaign` parameter |
| `UtmTerm` | `string` | Parsed `utm_term` parameter |
| `UtmContent` | `string` | Parsed `utm_content` parameter |
| `ReferrerClickTimestamp` | `long` | Epoch seconds when the referrer link was clicked |
| `InstallBeginTimestamp` | `long` | Epoch seconds when the install began |
| `GooglePlayInstant` | `bool` | Whether the app was installed via Google Play Instant |

---

## InstallReferrerErrorCode (enum)

| Value | Int | Description |
|-------|-----|-------------|
| `Ok` | 0 | Success |
| `FeatureNotSupported` | 1 | API not supported on this device |
| `ServiceUnavailable` | 2 | Play Store service unavailable (retryable) |
| `DeveloperError` | 3 | Invalid request |
| `ServiceDisconnected` | -1 | Disconnected before response (retryable) |
| `InternalError` | -100 | JNI bridge failure |
| `AlreadyConnecting` | -101 | Concurrent fetch guard triggered |
| `ConsentNotGranted` | -102 | GDPR consent not granted |

---

## CacheInvalidationReason (enum)

| Value | Description |
|-------|-------------|
| `AppReinstalled` | Different install time detected |
| `SdkVersionChanged` | Package version changed |
| `DataCorrupted` | Cache data unparseable |
| `ManualClear` | `ClearCache()` was called |
| `CacheExpired` | TTL exceeded |

---

## IInstallReferrerProvider (interface)

`public interface IInstallReferrerProvider`

Implemented by `InstallReferrerController`. Allows DI-based testing.

---

## IInstallReferrerAnalyticsAdapter (interface)

`public interface IInstallReferrerAnalyticsAdapter`

Optional telemetry contract. Implement to receive fetch lifecycle events for your analytics pipeline.

---

## IInstallReferrerCacheProvider (interface)

`public interface IInstallReferrerCacheProvider`

Pluggable cache storage. Default implementation uses `EncryptedPlayerPrefsCacheProvider`.
