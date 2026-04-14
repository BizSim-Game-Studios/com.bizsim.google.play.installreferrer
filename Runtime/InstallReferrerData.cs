// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

using System;
using System.Collections.Generic;
using UnityEngine;

namespace BizSim.Google.Play.InstallReferrer
{
    /// <summary>
    /// Reason why the cached referrer data was invalidated.
    /// Fired via <see cref="InstallReferrerController.OnCacheInvalidated"/>.
    /// </summary>
    public enum CacheInvalidationReason
    {
        /// <summary>App was reinstalled (different install time).</summary>
        AppReinstalled,

        /// <summary>Package SDK version changed (upgrade/downgrade).</summary>
        SdkVersionChanged,

        /// <summary>Cache data was corrupted or unparseable.</summary>
        DataCorrupted,

        /// <summary>Cache was manually cleared by the user or developer.</summary>
        ManualClear,

        /// <summary>Cache exceeded the maximum age (TTL) and must be refreshed.</summary>
        CacheExpired
    }

    /// <summary>
    /// Error codes returned by the Install Referrer API.
    /// Maps to <c>InstallReferrerClient.InstallReferrerResponse</c> constants.
    /// </summary>
    public enum InstallReferrerErrorCode
    {
        /// <summary>Success (0) — not an error, included for completeness.</summary>
        Ok = 0,

        /// <summary>Install Referrer API not supported on this device (1).</summary>
        FeatureNotSupported = 1,

        /// <summary>Service unavailable — Play Store may be updating (2). Retryable.</summary>
        ServiceUnavailable = 2,

        /// <summary>Developer error — invalid request (3).</summary>
        DeveloperError = 3,

        /// <summary>Service disconnected before response (-1). Retryable.</summary>
        ServiceDisconnected = -1,

        /// <summary>Internal bridge error — JNI call failed, serialization error, etc.</summary>
        InternalError = -100,

        /// <summary>Concurrent fetch guard — already connecting.</summary>
        AlreadyConnecting = -101,

        /// <summary>Fetch blocked because user consent has not been granted.</summary>
        ConsentNotGranted = -102
    }

    /// <summary>
    /// Ephemeral result from the Install Referrer API.
    /// Contains raw referrer data — kept in memory only during processing.
    /// Use <see cref="CachedReferrerData"/> for persistent storage.
    /// </summary>
    [Serializable]
    public class InstallReferrerResult
    {
        /// <summary>Raw referrer URL string (e.g., "utm_source=google&amp;utm_medium=cpc").</summary>
        public string installReferrer = "";

        /// <summary>Client-side timestamp (seconds since epoch) of the referrer click.</summary>
        public long referrerClickTimestampSeconds;

        /// <summary>Client-side timestamp (seconds since epoch) of install begin.</summary>
        public long installBeginTimestampSeconds;

        /// <summary>Server-side timestamp (seconds since epoch) of the referrer click.</summary>
        public long referrerClickTimestampServerSeconds;

        /// <summary>Server-side timestamp (seconds since epoch) of install begin.</summary>
        public long installBeginTimestampServerSeconds;

        /// <summary>Version of the app that was installed.</summary>
        public string installVersion = "";

        /// <summary>Whether the app was installed via Google Play Instant.</summary>
        public bool googlePlayInstantParam;
    }

    /// <summary>
    /// Persistent referrer data with metadata for cache validation.
    /// Safe to persist to <c>PlayerPrefs</c> or custom storage.
    ///
    /// <b>Cache Validation:</b> The cache is invalidated when:
    /// <list type="bullet">
    /// <item><see cref="AppInstallTimeMs"/> doesn't match the current app install time (reinstall detected)</item>
    /// <item><see cref="SdkVersion"/> doesn't match the current package version (upgrade detected)</item>
    /// </list>
    /// </summary>
    [Serializable]
    public class CachedReferrerData
    {
        // PascalCase fields are intentional — this class is only serialized/deserialized
        // via C# JsonUtility (PlayerPrefs round-trip). It never crosses the JNI boundary.
        // In contrast, InstallReferrerResult and InstallReferrerError use camelCase
        // to match the JSON keys produced by InstallReferrerBridge.java.

        // --- Raw referrer data ---

        /// <summary>Raw referrer URL string from the API.</summary>
        public string InstallReferrer = "";

        /// <summary>Client-side referrer click timestamp (seconds since epoch).</summary>
        public long ReferrerClickTimestampSeconds;

        /// <summary>Client-side install begin timestamp (seconds since epoch).</summary>
        public long InstallBeginTimestampSeconds;

        /// <summary>Server-side referrer click timestamp (seconds since epoch).</summary>
        public long ReferrerClickTimestampServerSeconds;

        /// <summary>Server-side install begin timestamp (seconds since epoch).</summary>
        public long InstallBeginTimestampServerSeconds;

        /// <summary>Version of the app that was installed.</summary>
        public string InstallVersion = "";

        /// <summary>Whether the app was installed via Google Play Instant.</summary>
        public bool GooglePlayInstantParam;

        // --- Parsed UTM parameters ---

        /// <summary>Parsed utm_source parameter (e.g., "google").</summary>
        public string UtmSource = "";

        /// <summary>Parsed utm_medium parameter (e.g., "cpc").</summary>
        public string UtmMedium = "";

        /// <summary>Parsed utm_campaign parameter (e.g., "summer_sale").</summary>
        public string UtmCampaign = "";

        /// <summary>Parsed utm_content parameter (e.g., "banner_ad").</summary>
        public string UtmContent = "";

        /// <summary>Parsed utm_term parameter (e.g., "mobile+games").</summary>
        public string UtmTerm = "";

        // --- Cache metadata ---

        /// <summary>App install time (ms since epoch) at the time of caching. Used for cache validation.</summary>
        public long AppInstallTimeMs;

        /// <summary>Package SDK version at the time of caching. Used for cache validation on upgrade.</summary>
        public string SdkVersion = "";

        /// <summary>ISO 8601 timestamp of when this data was fetched and cached.</summary>
        public string FetchTimestamp = "";

        // --- Convenience properties ---

        /// <summary>Whether a referrer URL was returned (non-empty).</summary>
        public bool HasReferrer => !string.IsNullOrEmpty(InstallReferrer);

        /// <summary>Whether UTM source is present (organic installs often have empty source).</summary>
        public bool HasUtmSource => !string.IsNullOrEmpty(UtmSource);

        /// <summary>
        /// Whether this appears to be an organic install.
        /// True when there is no referrer URL, no UTM source, or the UTM medium
        /// is explicitly "organic" (Google Play sometimes returns
        /// <c>utm_source=google-play&amp;utm_medium=organic</c> for store-browsing installs).
        /// </summary>
        public bool IsOrganic => !HasReferrer || !HasUtmSource
            || string.Equals(UtmMedium, "organic", System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Error returned by the Install Referrer API or the bridge layer.
    /// </summary>
    [Serializable]
    public class InstallReferrerError
    {
        // Field names MUST be camelCase to match the JSON keys sent by InstallReferrerBridge.java.
        // JsonUtility.FromJson is case-sensitive — PascalCase fields will silently fail to deserialize.
        public int errorCode;
        public string errorMessage;
        public bool isRetryable;

        /// <summary>Typed error code enum for switch expressions.</summary>
        public InstallReferrerErrorCode ErrorCodeEnum => errorCode switch
        {
            0 => InstallReferrerErrorCode.Ok,
            1 => InstallReferrerErrorCode.FeatureNotSupported,
            2 => InstallReferrerErrorCode.ServiceUnavailable,
            3 => InstallReferrerErrorCode.DeveloperError,
            -1 => InstallReferrerErrorCode.ServiceDisconnected,
            -100 => InstallReferrerErrorCode.InternalError,
            -101 => InstallReferrerErrorCode.AlreadyConnecting,
            -102 => InstallReferrerErrorCode.ConsentNotGranted,
            _ => InstallReferrerErrorCode.InternalError
        };

        /// <summary>Human-readable error code name for logging and debugging.</summary>
        public string ErrorCodeName => errorCode switch
        {
            0 => "OK",
            1 => "FEATURE_NOT_SUPPORTED",
            2 => "SERVICE_UNAVAILABLE",
            3 => "DEVELOPER_ERROR",
            -1 => "SERVICE_DISCONNECTED",
            -100 => "INTERNAL_ERROR",
            -101 => "ALREADY_CONNECTING",
            -102 => "CONSENT_NOT_GRANTED",
            _ => $"UNKNOWN_{errorCode}"
        };
    }

    /// <summary>
    /// Exception thrown by <see cref="IInstallReferrerProvider.FetchInstallReferrerAsync"/>
    /// when the install referrer fetch fails after all retries.
    /// Wraps the underlying <see cref="InstallReferrerError"/> for structured error handling.
    /// </summary>
    public class InstallReferrerException : Exception
    {
        /// <summary>The underlying error from the API or bridge layer.</summary>
        public InstallReferrerError Error { get; }

        public InstallReferrerException(InstallReferrerError error)
            : base($"Install Referrer fetch failed: {error.ErrorCodeName} ({error.errorCode}) — {error.errorMessage}")
        {
            Error = error;
        }
    }

    /// <summary>
    /// Minimal analytics event with only the data needed for attribution reporting.
    /// Excludes raw referrer URL, timestamps, utm_content, and utm_term to comply
    /// with data minimization principles (GDPR Art. 5(1)(c)).
    /// </summary>
    public readonly struct ReferrerAnalyticsEvent
    {
        public readonly string UtmSource;
        public readonly string UtmMedium;
        public readonly string UtmCampaign;
        public readonly bool IsOrganic;
        public readonly bool FromCache;

        public ReferrerAnalyticsEvent(CachedReferrerData data, bool fromCache)
        {
            UtmSource = data.UtmSource ?? "";
            UtmMedium = data.UtmMedium ?? "";
            UtmCampaign = data.UtmCampaign ?? "";
            IsOrganic = data.IsOrganic;
            FromCache = fromCache;
        }
    }

    /// <summary>
    /// Static utility class for parsing UTM parameters from a referrer URL string.
    /// </summary>
    internal static class InstallReferrerUtility
    {
        /// <summary>
        /// Parses UTM parameters from a referrer URL string.
        /// Handles URL-encoded values and missing parameters gracefully.
        /// </summary>
        /// <param name="referrer">Raw referrer string (e.g., "utm_source=google&amp;utm_medium=cpc")</param>
        /// <param name="source">Parsed utm_source value, or empty string if missing.</param>
        /// <param name="medium">Parsed utm_medium value, or empty string if missing.</param>
        /// <param name="campaign">Parsed utm_campaign value, or empty string if missing.</param>
        /// <param name="content">Parsed utm_content value, or empty string if missing.</param>
        /// <param name="term">Parsed utm_term value, or empty string if missing.</param>
        public static void ParseUtmParameters(
            string referrer,
            out string source,
            out string medium,
            out string campaign,
            out string content,
            out string term)
        {
            source = "";
            medium = "";
            campaign = "";
            content = "";
            term = "";

            if (string.IsNullOrEmpty(referrer))
                return;

            var parameters = ParseQueryString(referrer);

            if (parameters.TryGetValue("utm_source", out var s)) source = s;
            if (parameters.TryGetValue("utm_medium", out var m)) medium = m;
            if (parameters.TryGetValue("utm_campaign", out var c)) campaign = c;
            if (parameters.TryGetValue("utm_content", out var co)) content = co;
            if (parameters.TryGetValue("utm_term", out var t)) term = t;
        }

        /// <summary>
        /// Parses a query string into a dictionary of key-value pairs.
        /// Handles URL encoding via <see cref="UnityEngine.Networking.UnityWebRequest.UnEscapeURL"/>.
        /// </summary>
        public static Dictionary<string, string> ParseQueryString(string queryString)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(queryString))
                return result;

            // Remove leading '?' if present
            if (queryString.StartsWith("?"))
                queryString = queryString.Substring(1);

            var pairs = queryString.Split('&');
            foreach (var pair in pairs)
            {
                if (string.IsNullOrEmpty(pair)) continue;

                var idx = pair.IndexOf('=');
                if (idx < 0)
                {
                    // Key without value
                    result[UrlDecode(pair)] = "";
                    continue;
                }

                var key = UrlDecode(pair.Substring(0, idx));
                var value = UrlDecode(pair.Substring(idx + 1));
                result[key] = value;
            }

            return result;
        }

        /// <summary>
        /// Decodes a URL-encoded string. Uses <see cref="Uri.UnescapeDataString"/>
        /// with '+' to space conversion.
        /// </summary>
        public static string UrlDecode(string encoded)
        {
            if (string.IsNullOrEmpty(encoded)) return "";

            try
            {
                // Replace '+' with space before unescaping (standard form encoding)
                return Uri.UnescapeDataString(encoded.Replace('+', ' '));
            }
            catch
            {
                return encoded;
            }
        }
    }
}
