# Architecture

## Overview

The Install Referrer package follows a single-API-call wrapper pattern: connect to the Play Store service, read the referrer data, disconnect. The entire flow completes in a single `FetchInstallReferrerAsync()` call.

## Component Diagram

```
┌──────────────────────────────────────────────┐
│              Unity C# (Main Thread)          │
│                                              │
│  InstallReferrerController (MonoBehaviour)   │
│       ├── IInstallReferrerProvider           │
│       ├── IInstallReferrerCacheProvider      │
│       └── IInstallReferrerAnalyticsAdapter   │
│                                              │
│  Compile-time provider selection:            │
│  #if UNITY_ANDROID && !UNITY_EDITOR          │
│    → AndroidInstallReferrerProvider          │
│  #else                                       │
│    → MockInstallReferrerProvider             │
│  #endif                                      │
└──────────────┬───────────────────────────────┘
               │ JNI (AndroidJavaProxy)
┌──────────────▼───────────────────────────────┐
│           Android Java (Any Thread)          │
│                                              │
│  InstallReferrerBridge.java                  │
│    └── InstallReferrerClient (Google lib)    │
│         connect → getInstallReferrer → end   │
└──────────────────────────────────────────────┘
```

## Thread Model

1. All public `InstallReferrerController` methods enforce main-thread execution via `EnsureMainThread()`. Calling from a background thread throws `InvalidOperationException`.
2. The Android provider creates an `InstallReferrerCallbackProxy` (extends `AndroidJavaProxy`) that receives callbacks on the Java side.
3. Java callbacks arrive on an arbitrary thread. `UnityMainThreadDispatcher.Enqueue()` marshals them back to the Unity main thread before invoking C# handlers.
4. The mock provider executes entirely on the main thread with configurable simulated delay.

## State Machine

```
[Idle] ──FetchInstallReferrerAsync()──> [Connecting]
[Connecting] ──onConnected──> [Reading]
[Reading] ──onReferrerReceived──> [Disconnecting]
[Disconnecting] ──done──> [Idle] (result returned)

[Connecting] ──onError──> [Idle] (error returned)
[Connecting] ──duplicate call──> AlreadyConnecting error
```

The `AlreadyConnecting` guard prevents concurrent fetch calls from creating multiple connections.

## Cache Layer

`InstallReferrerCacheLogic` manages a local cache of referrer data:

- Cache key includes `appInstallTime` and `sdkVersion` for automatic invalidation on reinstall or SDK upgrade
- Default storage via `EncryptedPlayerPrefsCacheProvider` (AES-encrypted PlayerPrefs)
- Pluggable via `IInstallReferrerCacheProvider` for custom storage backends
- TTL-based expiration (default 24 hours, configurable)

## Provider Selection

| Platform | Condition | Provider |
|----------|-----------|----------|
| Android device | `UNITY_ANDROID && !UNITY_EDITOR` | `AndroidInstallReferrerProvider` |
| Unity Editor | Always | `MockInstallReferrerProvider` |
| Development Build | `_useMockInDevelopmentBuild` flag | `MockInstallReferrerProvider` |
| Non-Android platform | Always | `MockInstallReferrerProvider` |

## ProGuard / R8

The `.androidlib` subproject ships `proguard-rules.pro` and `consumer-rules.pro` with keep rules for:
- `com.android.installreferrer.**` (Google library)
- `com.bizsim.google.play.installreferrer.**` (bridge classes)
- `com.google.android.gms.tasks.**` (GMS Tasks, used by the referrer client)

These are injected automatically at build time. Consumers do not need to edit ProGuard manually.
