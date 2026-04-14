// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

using System;

namespace BizSim.Google.Play.InstallReferrer
{
    /// <summary>
    /// Pure C# static utility class containing all cache validation and data creation logic.
    /// Extracted from <see cref="InstallReferrerController"/> to enable unit testing
    /// without Play Mode or MonoBehaviour dependencies.
    ///
    /// <b>DI / Testing:</b> All methods are static and take explicit parameters —
    /// no hidden state, no Unity API calls, no singletons. This makes them trivially
    /// testable with NUnit in Edit Mode.
    ///
    /// <b>Why not a full service class?</b> The controller requires <c>MonoBehaviour</c>
    /// for <c>UnitySendMessage</c> (JNI callbacks) and <c>StartCoroutine</c> (retry logic).
    /// Extracting those would require a callback bridge that adds complexity without
    /// meaningful benefit. Instead, we extract only the pure, deterministic logic.
    /// </summary>
    internal static class InstallReferrerCacheLogic
    {
        /// <summary>
        /// Determines whether cached referrer data is still valid.
        /// Checks SDK version, app install time, and TTL expiration.
        /// </summary>
        /// <param name="data">The cached data to validate.</param>
        /// <param name="sdkVersion">Current SDK version string.</param>
        /// <param name="currentInstallTimeMs">Current app install time in ms, or -1 if unavailable.</param>
        /// <param name="maxAgeHours">Maximum cache age in hours before expiration.</param>
        /// <returns>True if the cache is valid and can be used.</returns>
        public static bool IsCacheValid(
            CachedReferrerData data,
            string sdkVersion,
            long currentInstallTimeMs,
            int maxAgeHours)
        {
            if (data == null) return false;
            if (string.IsNullOrEmpty(data.SdkVersion)) return false;
            if (data.SdkVersion != sdkVersion) return false;

            if (currentInstallTimeMs > 0 && data.AppInstallTimeMs > 0 &&
                data.AppInstallTimeMs != currentInstallTimeMs)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(data.FetchTimestamp) &&
                DateTime.TryParse(data.FetchTimestamp, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var fetchTime) &&
                (DateTime.UtcNow - fetchTime).TotalHours > maxAgeHours)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines the reason why cached data failed validation.
        /// Call this only when <see cref="IsCacheValid"/> returns false.
        /// </summary>
        public static CacheInvalidationReason GetInvalidationReason(
            CachedReferrerData data,
            string sdkVersion,
            long currentInstallTimeMs,
            int maxAgeHours)
        {
            if (data.SdkVersion != sdkVersion)
                return CacheInvalidationReason.SdkVersionChanged;

            if (currentInstallTimeMs > 0 && data.AppInstallTimeMs > 0 &&
                data.AppInstallTimeMs != currentInstallTimeMs)
            {
                return CacheInvalidationReason.AppReinstalled;
            }

            if (!string.IsNullOrEmpty(data.FetchTimestamp) &&
                DateTime.TryParse(data.FetchTimestamp, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var fetchTime) &&
                (DateTime.UtcNow - fetchTime).TotalHours > maxAgeHours)
            {
                return CacheInvalidationReason.CacheExpired;
            }

            return CacheInvalidationReason.DataCorrupted;
        }

        /// <summary>
        /// Creates a <see cref="CachedReferrerData"/> from an API result.
        /// Pure function — no side effects, no Unity API calls.
        /// </summary>
        /// <param name="result">Raw API result.</param>
        /// <param name="installTimeMs">App install time in milliseconds, or -1.</param>
        /// <param name="sdkVersion">Current SDK version string.</param>
        /// <returns>A fully populated cache entry ready to persist.</returns>
        public static CachedReferrerData CreateCachedData(
            InstallReferrerResult result,
            long installTimeMs,
            string sdkVersion)
        {
            InstallReferrerUtility.ParseUtmParameters(
                result.installReferrer,
                out string source, out string medium,
                out string campaign, out string content, out string term);

            return new CachedReferrerData
            {
                InstallReferrer = result.installReferrer ?? "",
                ReferrerClickTimestampSeconds = result.referrerClickTimestampSeconds,
                InstallBeginTimestampSeconds = result.installBeginTimestampSeconds,
                ReferrerClickTimestampServerSeconds = result.referrerClickTimestampServerSeconds,
                InstallBeginTimestampServerSeconds = result.installBeginTimestampServerSeconds,
                InstallVersion = result.installVersion ?? "",
                GooglePlayInstantParam = result.googlePlayInstantParam,
                UtmSource = source,
                UtmMedium = medium,
                UtmCampaign = campaign,
                UtmContent = content,
                UtmTerm = term,
                AppInstallTimeMs = installTimeMs,
                SdkVersion = sdkVersion,
                FetchTimestamp = DateTime.UtcNow.ToString("o")
            };
        }
    }
}
