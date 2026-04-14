// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

#if UNITY_EDITOR
using UnityEngine;

namespace BizSim.Google.Play.InstallReferrer
{
    /// <summary>
    /// ScriptableObject for configuring mock Install Referrer responses in the Unity Editor.
    /// Create via <b>Assets → Create → BizSim → Install Referrer Mock Config</b>.
    ///
    /// <b>Presets:</b>
    /// <list type="bullet">
    /// <item><b>Organic</b> — Empty referrer (direct install from Play Store)</item>
    /// <item><b>Google Ads Campaign</b> — utm_source=google, utm_medium=cpc</item>
    /// <item><b>Facebook Campaign</b> — utm_source=facebook, utm_medium=social</item>
    /// <item><b>Deep Link</b> — Custom referrer with app-specific parameters</item>
    /// </list>
    /// </summary>
    [CreateAssetMenu(menuName = "BizSim/Install Referrer Mock Config")]
    public class InstallReferrerMockConfig : ScriptableObject
    {
        [Header("Mock Referrer Data")]
        [Tooltip("The raw referrer URL string to simulate. Use standard query-string format (e.g., 'utm_source=google&utm_medium=cpc&utm_campaign=summer_sale').")]
        [TextArea(2, 4)]
        public string MockReferrerUrl = "";

        [Tooltip("Simulated referrer click timestamp (seconds since epoch). 0 = not available.")]
        public long MockReferrerClickTimestamp;

        [Tooltip("Simulated install begin timestamp (seconds since epoch). 0 = not available.")]
        public long MockInstallBeginTimestamp;

        [Tooltip("Whether to simulate a Google Play Instant install.")]
        public bool MockGooglePlayInstant;

        [Header("Latency Simulation")]
        [Tooltip("Simulated network latency in seconds before returning the mock result.")]
        [Range(0f, 5f)]
        public float SimulatedLatencySeconds;

        [Header("Offline Simulation")]
        [Tooltip("When enabled, simulates the device being offline (returns SERVICE_UNAVAILABLE error).")]
        public bool SimulateOffline;

        [Header("Error Simulation")]
        [Tooltip("When enabled, simulates an API error instead of a successful response.")]
        public bool SimulateError;

        [Tooltip("Error code to simulate (1=FEATURE_NOT_SUPPORTED, 2=SERVICE_UNAVAILABLE, 3=DEVELOPER_ERROR, -1=SERVICE_DISCONNECTED).")]
        public int SimulatedErrorCode = 2;
    }
}
#endif
