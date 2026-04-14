// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

namespace BizSim.Google.Play.InstallReferrer
{
    /// <summary>
    /// Pluggable cache provider for persisting install referrer data.
    /// The default implementation uses <c>PlayerPrefs</c>.
    /// Implement this interface to use a custom storage backend
    /// (e.g., SQLite, file system, encrypted storage).
    ///
    /// <b>Usage:</b>
    /// <code>
    /// InstallReferrerController.Instance.SetCacheProvider(myProvider);
    /// </code>
    /// </summary>
    public interface IInstallReferrerCacheProvider
    {
        /// <summary>
        /// Loads cached referrer data from storage.
        /// </summary>
        /// <returns>The cached data, or null if no valid cache exists.</returns>
        CachedReferrerData Load();

        /// <summary>
        /// Saves referrer data to persistent storage.
        /// </summary>
        /// <param name="data">The referrer data to cache.</param>
        void Save(CachedReferrerData data);

        /// <summary>
        /// Clears the cached referrer data from storage.
        /// </summary>
        void Clear();
    }
}
