# Troubleshooting

## Problem: FetchInstallReferrerAsync returns FeatureNotSupported

**Cause:** The device does not have a version of the Google Play Store that supports the Install Referrer API. This is common on emulators without Google Play Services and on very old Play Store versions.

**Fix:**
1. Update the Google Play Store app on the test device.
2. For emulators, use a system image with Google Play (e.g., "Google APIs" or "Google Play" image).
3. On devices without Play Store (Huawei AppGallery, Amazon Fire), the API is unavailable by design. Use the mock provider for testing.

---

## Problem: FetchInstallReferrerAsync returns ServiceUnavailable

**Cause:** The Play Store service is temporarily unavailable, often because the Play Store is updating itself.

**Fix:**
1. This is a transient error. The controller retries automatically (3 attempts with exponential backoff by default).
2. If retries are exhausted, wait a few seconds and try again.
3. Check that the device has a network connection -- the Play Store service requires it during initial setup.

---

## Problem: UTM parameters are all empty strings

**Cause:** The app was installed directly from the Play Store without clicking a campaign link. Organic installs have an empty referrer string.

**Fix:**
1. This is expected behavior. Check `InstallReferrerData.RawReferrerUrl` -- if it is empty, the install was organic.
2. To test UTM parameters, use the Google Play URL builder to create a campaign link: `https://play.google.com/store/apps/details?id=your.package&referrer=utm_source%3Dtest%26utm_campaign%3Ddemo`
3. In the editor, use `Samples~/MockPresets` to configure mock UTM values.

---

## Problem: InvalidOperationException -- must be called from the main thread

**Cause:** `FetchInstallReferrerAsync()` or another controller method was called from a background thread. All controller methods enforce main-thread execution.

**Fix:**
1. Move the call to the main thread. Use `UnityMainThreadDispatcher.Enqueue()` or `await` from a Unity coroutine/async method that started on the main thread.
2. If using UniTask, `await UniTask.SwitchToMainThread()` before calling the controller.

---

## Problem: AlreadyConnecting error code returned

**Cause:** A second `FetchInstallReferrerAsync()` call was made while the first is still in flight. The controller has a concurrent-call guard to prevent multiple simultaneous connections.

**Fix:**
1. Await the first call's result before starting a second.
2. If you need to check whether a fetch is in progress, guard your call site with a boolean flag.

---

## Problem: Cache is not invalidated after app update

**Cause:** The cache key includes `PackageVersion.Current`. If the package version was not bumped during the app update, the cache key remains the same and old data is served.

**Fix:**
1. Ensure `PackageVersion.Current` changes when you release a new app version that includes a package upgrade.
2. Call `InstallReferrerController.Instance.ClearCache()` explicitly if you need to force a refresh.

---

## Problem: EDM4U resolution fails with dependency conflict

**Cause:** Another plugin in the project declares a different version of the `com.android.installreferrer:installreferrer` Maven artifact.

**Fix:**
1. Open **Assets > External Dependency Manager > Android Resolver > Force Resolve**.
2. If conflicts persist, check `mainTemplate.gradle` or other `Dependencies.xml` files for version pins.
3. EDM4U generally resolves to the highest compatible version. Verify that the resolved version is >= 2.2.
