# Upgrade Guide

## Upgrading from 0.1.x to 0.2.0

### Breaking Changes

1. **Encrypted cache salt changed** — The default constructor now uses
   `Application.identifier` (bundle ID) as part of the PBKDF2 salt instead of a
   hardcoded string. Existing encrypted caches will fail to decrypt and will be
   automatically re-fetched from the API. No action required — this is a one-time
   transparent cache miss on upgrade.

2. **PBKDF2 iterations reduced** — Iteration count lowered from 10,000 to 1,000
   for faster `Awake()` performance on low-end devices. Combined with the salt
   change, this also invalidates existing encrypted caches (handled gracefully).

3. **Internal API surface** — The following classes are now `internal` and no longer
   part of the public API:
   - `InstallReferrerCacheLogic`
   - `InstallReferrerUtility`
   - `PackageVersion`

   If you were referencing these directly, use the controller's public methods instead.

### New Features

- **SDK version cache invalidation** — Cache is now invalidated when the package
  version changes, ensuring bug fixes in decision logic take effect immediately.
- **Fail-safe defaults** — Default flags are restrictive until the API confirms
  attribution data.
- **`synchronized` Java bridge** — Thread-safe access to `InstallReferrerBridge`
  static state, preventing race conditions during rapid retry cycles.

## Upgrading from 0.0.x to 0.1.0

### Initial Release

No migration needed — this was the first public release.
