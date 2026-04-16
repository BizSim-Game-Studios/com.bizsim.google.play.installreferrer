# Configuration

## InstallReferrerSettings Asset

The package uses a `InstallReferrerSettings` ScriptableObject for project-wide defaults. The asset is located at:

```
Assets/Resources/BizSim/GooglePlay/InstallReferrerSettings.asset
```

If the asset does not exist, it is auto-created the first time you open the Configuration window.

### Settings Fields

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `LogsEnabled` | `bool` | `true` | Master switch for all log output |
| `LogLevel` | `LogLevel` | `Info` | Minimum log level: Verbose, Info, Warning, Error, Silent |
| `UseMockInDevelopmentBuild` | `bool` | `false` | Use mock provider in Development Builds instead of the real API |
| `EnableAnalyticsByDefault` | `bool` | `true` | Whether the analytics adapter fires events by default |
| `CacheTtlSeconds` | `int` | `86400` | Time-to-live for cached referrer data (default: 24 hours) |
| `MaxRetryAttempts` | `int` | `3` | Number of retry attempts on transient failures |
| `RetryBaseDelayMs` | `int` | `2000` | Base delay in milliseconds for exponential backoff |

## Configuration Editor Window

Open via **BizSim > Google Play > Install Referrer > Configuration**.

### Settings Panel

The window draws each field from the `InstallReferrerSettings` asset using `SerializedObject` and `EditorGUILayout.PropertyField`. Three buttons are available:

- **Apply** -- saves changes to the asset and calls `BizSimLogger.InvalidateCache()` so log-level changes take effect immediately without domain reload
- **Revert** -- discards unsaved changes and reloads from disk
- **Reset to defaults** -- restores all fields to their default values

### Per-Instance Overrides

`InstallReferrerController` has `[SerializeField]` fields that mirror the Settings asset. When you place the controller on a GameObject manually, per-instance values override the asset defaults. This allows different scenes to use different configurations (e.g., a debug scene with verbose logging).

## GDPR Consent

Call `InstallReferrerController.Instance.SetConsentGranted(false)` to disable referrer data collection. When consent is not granted:

- `FetchInstallReferrerAsync()` returns `ConsentNotGranted` error code immediately
- No network call to the Play Store is made
- No data is cached

Call `SetConsentGranted(true)` to re-enable collection after obtaining consent.
