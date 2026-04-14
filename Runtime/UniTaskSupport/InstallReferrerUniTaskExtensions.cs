// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

using System.Threading;
using Cysharp.Threading.Tasks;

namespace BizSim.Google.Play.InstallReferrer.UniTaskSupport
{
    /// <summary>
    /// UniTask extension methods for <see cref="InstallReferrerController"/>.
    /// This assembly only compiles when <c>com.cysharp.unitask</c> is installed
    /// (enforced via <c>defineConstraints</c> in the asmdef).
    ///
    /// <b>Usage:</b>
    /// <code>
    /// using BizSim.Google.Play.InstallReferrer.UniTaskSupport;
    ///
    /// var data = await InstallReferrerController.Instance.FetchInstallReferrerUniTask();
    /// </code>
    /// </summary>
    public static class InstallReferrerUniTaskExtensions
    {
        /// <summary>
        /// Fetches install referrer data using UniTask. Allocates no <c>Task</c> objects
        /// and integrates with Unity's PlayerLoop for zero-overhead awaiting.
        /// </summary>
        /// <param name="controller">The controller instance.</param>
        /// <param name="timeoutMs">Timeout in milliseconds (default 30000).</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The fetched or cached <see cref="CachedReferrerData"/>.</returns>
        /// <exception cref="InstallReferrerException">Fetch failed after all retries.</exception>
        /// <exception cref="System.TimeoutException">No callback within timeout.</exception>
        public static async UniTask<CachedReferrerData> FetchInstallReferrerUniTask(
            this InstallReferrerController controller,
            int timeoutMs = 30000,
            CancellationToken cancellationToken = default)
        {
            var utcs = new UniTaskCompletionSource<CachedReferrerData>();

            void OnSuccess(CachedReferrerData data)
            {
                controller.OnReferrerDataReady -= OnSuccess;
                controller.OnError -= OnError;
                utcs.TrySetResult(data);
            }

            void OnError(InstallReferrerError error)
            {
                controller.OnReferrerDataReady -= OnSuccess;
                controller.OnError -= OnError;
                utcs.TrySetException(new InstallReferrerException(error));
            }

            controller.OnReferrerDataReady += OnSuccess;
            controller.OnError += OnError;

            controller.FetchInstallReferrer();

            var (hasResult, result) = await utcs.Task
                .TimeoutWithoutException(System.TimeSpan.FromMilliseconds(timeoutMs));

            if (!hasResult)
            {
                controller.OnReferrerDataReady -= OnSuccess;
                controller.OnError -= OnError;
                throw new System.TimeoutException(
                    $"[InstallReferrer] No callback received within {timeoutMs}ms.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            return result;
        }
    }
}
