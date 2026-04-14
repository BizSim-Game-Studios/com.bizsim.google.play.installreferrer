# Basic Integration Sample

This sample demonstrates a minimal Install Referrer integration for a Unity game.

## Contents

| File | Description |
|------|-------------|
| `BasicReferrerFetch.cs` | MonoBehaviour that fetches install referrer data on launch and logs UTM parameters |
| `AsyncReferrerFetch.cs` | Async/await variant using `Task<CachedReferrerData>` for modern codebases |

## Setup

1. Import this sample via **Package Manager → Google Play Install Referrer Bridge → Samples → Import**
2. Create a new scene or open your startup scene
3. Add a GameObject with `InstallReferrerController` component
4. Add another GameObject with `BasicReferrerFetch` component
5. Enter Play Mode — the controller returns mock data in the Editor

## What This Sample Shows

- Subscribing to `OnReferrerDataReady` and `OnError` events
- Reading UTM parameters (`UtmSource`, `UtmMedium`, `UtmCampaign`)
- Detecting organic vs paid installs with `IsOrganic`
- Identifying friend invitations via referral deep links
- Graceful error handling with automatic retries
