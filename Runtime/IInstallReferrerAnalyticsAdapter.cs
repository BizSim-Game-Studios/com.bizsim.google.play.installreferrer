// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

namespace BizSim.Google.Play.InstallReferrer
{
    /// <summary>
    /// Pluggable analytics adapter for logging install referrer events.
    /// Implement this interface to integrate with your analytics provider
    /// (Firebase, Unity Analytics, Amplitude, etc.).
    ///
    /// <b>Usage:</b>
    /// <code>
    /// InstallReferrerController.Instance.SetAnalyticsAdapter(myAdapter);
    /// </code>
    /// </summary>
    public interface IInstallReferrerAnalyticsAdapter
    {
        /// <summary>
        /// Called when install referrer data is successfully fetched.
        /// </summary>
        /// <param name="data">The fetched referrer data with parsed UTM parameters.</param>
        /// <param name="fromCache">True if the data was loaded from cache, false if freshly fetched.</param>
        void LogReferrerFetched(CachedReferrerData data, bool fromCache);

        /// <summary>
        /// Called when the install referrer fetch fails after all retries.
        /// </summary>
        /// <param name="error">The error that caused the failure.</param>
        void LogReferrerError(InstallReferrerError error);

        /// <summary>
        /// Called with a minimal, privacy-safe analytics event that excludes raw URLs and timestamps.
        /// New adapters should prefer implementing this overload over <see cref="LogReferrerFetched"/>.
        /// Default implementation is a no-op for backward compatibility.
        /// </summary>
        void LogReferrerFetchedMinimal(ReferrerAnalyticsEvent evt) { }
    }
}
