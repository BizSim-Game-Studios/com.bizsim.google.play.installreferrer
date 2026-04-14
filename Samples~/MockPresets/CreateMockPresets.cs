// Sample: Mock Config Preset Creator
// Import this sample via Package Manager → Install Referrer → Samples → Mock Presets

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace BizSim.Google.Play.InstallReferrer.Samples
{
    /// <summary>
    /// Editor utility that creates pre-configured <see cref="InstallReferrerMockConfig"/>
    /// ScriptableObject assets for common testing scenarios.
    /// </summary>
    public static class CreateMockPresets
    {
        private const string OutputFolder = "Assets/InstallReferrerMockPresets";

        [MenuItem("BizSim/Google Play/Install Referrer/Create Mock Presets")]
        public static void CreateAll()
        {
            if (!AssetDatabase.IsValidFolder(OutputFolder))
            {
                AssetDatabase.CreateFolder("Assets", "InstallReferrerMockPresets");
            }

            // 1. Organic (empty referrer)
            CreatePreset("Mock_Organic", new PresetData
            {
                referrerUrl = "",
                description = "Direct install from Play Store — no referrer URL"
            });

            // 2. Google Ads Campaign
            CreatePreset("Mock_GoogleAds", new PresetData
            {
                referrerUrl = "utm_source=google&utm_medium=cpc&utm_campaign=summer_sale&utm_content=banner_v2",
                clickTimestamp = 1738000000,
                installTimestamp = 1738000060,
                description = "Paid Google Ads search campaign"
            });

            // 3. Facebook Social
            CreatePreset("Mock_Facebook", new PresetData
            {
                referrerUrl = "utm_source=facebook&utm_medium=social&utm_campaign=launch&utm_content=video_ad",
                clickTimestamp = 1738100000,
                installTimestamp = 1738100120,
                description = "Facebook social media campaign"
            });

            // 4. Friend Invitation
            CreatePreset("Mock_FriendInvite", new PresetData
            {
                referrerUrl = "utm_source=user_12345&utm_medium=invite&utm_campaign=invite",
                clickTimestamp = 1738200000,
                installTimestamp = 1738200030,
                description = "Friend invitation deep link referral"
            });

            // 5. Error: Service Unavailable (transient)
            CreatePreset("Mock_Error_ServiceUnavailable", new PresetData
            {
                simulateError = true,
                errorCode = 2, // SERVICE_UNAVAILABLE
                description = "Simulates SERVICE_UNAVAILABLE (transient, retryable)"
            });

            // 6. Error: Feature Not Supported (permanent)
            CreatePreset("Mock_Error_NotSupported", new PresetData
            {
                simulateError = true,
                errorCode = 1, // FEATURE_NOT_SUPPORTED
                description = "Simulates FEATURE_NOT_SUPPORTED (permanent, non-retryable)"
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[InstallReferrer] Created 6 mock presets in {OutputFolder}/");
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(
                $"{OutputFolder}/Mock_Organic.asset");
        }

        private struct PresetData
        {
            public string referrerUrl;
            public long clickTimestamp;
            public long installTimestamp;
            public bool simulateError;
            public int errorCode;
            public string description;
        }

        private static void CreatePreset(string name, PresetData data)
        {
            var config = ScriptableObject.CreateInstance<InstallReferrerMockConfig>();
            config.MockReferrerUrl = data.referrerUrl ?? "";
            config.MockReferrerClickTimestamp = data.clickTimestamp;
            config.MockInstallBeginTimestamp = data.installTimestamp;
            config.SimulateError = data.simulateError;
            config.SimulatedErrorCode = data.errorCode;

            string path = $"{OutputFolder}/{name}.asset";
            AssetDatabase.CreateAsset(config, path);
        }
    }
}
#endif
