// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

using System;
using System.Threading;
using System.Threading.Tasks;

namespace BizSim.Google.Play.InstallReferrer
{
    /// <summary>
    /// Abstraction for the Install Referrer service.
    ///
    /// Use this interface to decouple your game logic from the concrete
    /// <see cref="InstallReferrerController"/> singleton. This enables:
    /// <list type="bullet">
    /// <item>Unit testing with custom mock implementations</item>
    /// <item>Dependency injection via Zenject, VContainer, or any DI framework</item>
    /// <item>Swapping the real provider with a stub in automated test suites</item>
    /// </list>
    ///
    /// <b>Singleton usage (no DI):</b>
    /// <code>
    /// IInstallReferrerProvider provider = InstallReferrerController.Instance;
    /// var data = await provider.FetchInstallReferrerAsync();
    /// </code>
    /// </summary>
    public interface IInstallReferrerProvider
    {
        /// <summary>Fired when referrer data is successfully fetched or loaded from cache.</summary>
        event Action<CachedReferrerData> OnReferrerDataReady;

        /// <summary>Fired when the fetch fails after all retries.</summary>
        event Action<InstallReferrerError> OnError;

        /// <summary>Fired when the cache is invalidated (diagnostic event).</summary>
        event Action<CacheInvalidationReason> OnCacheInvalidated;

        /// <summary>Current cached referrer data, or null if not yet fetched.</summary>
        CachedReferrerData CachedData { get; }

        /// <summary>Whether a fetch is currently in progress.</summary>
        bool IsFetching { get; }

        /// <summary>
        /// Initiates a fetch of install referrer data.
        /// Returns cached data if valid; otherwise queries the API.
        /// Results are delivered via <see cref="OnReferrerDataReady"/> and <see cref="OnError"/> events.
        /// </summary>
        void FetchInstallReferrer();

        /// <summary>
        /// Async version of <see cref="FetchInstallReferrer"/>.
        /// Returns the cached or freshly fetched <see cref="CachedReferrerData"/>.
        /// </summary>
        /// <param name="timeoutSeconds">Maximum seconds to wait (default 30).</param>
        /// <exception cref="InstallReferrerException">Fetch failed after all retries.</exception>
        /// <exception cref="TimeoutException">No callback within timeout.</exception>
        Task<CachedReferrerData> FetchInstallReferrerAsync(float timeoutSeconds = 30f);

        /// <summary>
        /// Cancellable version of <see cref="FetchInstallReferrerAsync(float)"/>.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <param name="timeoutSeconds">Maximum seconds to wait (default 30).</param>
        Task<CachedReferrerData> FetchInstallReferrerAsync(CancellationToken cancellationToken, float timeoutSeconds = 30f);
    }
}
