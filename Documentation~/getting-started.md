# Getting Started

## Prerequisites

- Unity 6000.0 (Unity 6.0 LTS) or later
- Android build target configured in Unity
- External Dependency Manager for Unity (EDM4U) installed

## Installation

### Step 1 -- Add the OpenUPM scoped registry

Open `Packages/manifest.json` in your Unity project and add the scoped registry for EDM4U:

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

### Step 2 -- Add the package via Git URL

Add to the `dependencies` section of `manifest.json`:

```json
{
  "dependencies": {
    "com.bizsim.google.play.installreferrer": "https://github.com/BizSim-Game-Studios/com.bizsim.google.play.installreferrer.git#v1.0.2"
  }
}
```

### Step 3 -- Resolve Android dependencies

After Unity reimports, run **Assets > External Dependency Manager > Android Resolver > Force Resolve** to download the Google Play Install Referrer Maven artifact.

### Step 4 -- Verify setup

1. Open **BizSim > Google Play > Install Referrer > Configuration**.
2. The configuration window should load without errors.
3. Build to an Android device and call `FetchInstallReferrerAsync()`.

## First API Call

```csharp
using BizSim.Google.Play.InstallReferrer;
using UnityEngine;

public class ReferrerExample : MonoBehaviour
{
    async void Start()
    {
        var controller = InstallReferrerController.Instance;
        var result = await controller.FetchInstallReferrerAsync();

        if (result.IsSuccess)
        {
            Debug.Log($"UTM Source: {result.Data.UtmSource}");
            Debug.Log($"UTM Campaign: {result.Data.UtmCampaign}");
            Debug.Log($"Install Time: {result.Data.InstallBeginTimestamp}");
        }
        else
        {
            Debug.LogError($"Referrer fetch failed: {result.ErrorCode}");
        }
    }
}
```

## Next Steps

- Read the [API Reference](api-reference.md) for all available types and methods
- Configure caching and logging in the [Configuration](configuration.md) guide
- Set up [mock presets](../README.md) for editor testing
