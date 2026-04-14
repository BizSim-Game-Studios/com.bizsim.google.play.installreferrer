// Sample: Async/Await Install Referrer Integration
// Import this sample via Package Manager → Install Referrer → Samples → Basic Integration

using System;
using UnityEngine;

namespace BizSim.Google.Play.InstallReferrer.Samples
{
    /// <summary>
    /// Async/await example for fetching install referrer data.
    /// Demonstrates structured error handling with <see cref="InstallReferrerException"/>.
    /// </summary>
    public class AsyncReferrerFetch : MonoBehaviour
    {
        private async void Start()
        {
            try
            {
                var data = await InstallReferrerController.Instance.FetchInstallReferrerAsync();

                Debug.Log($"[Referrer] Source: {data.UtmSource}, Campaign: {data.UtmCampaign}");

                if (!data.IsOrganic)
                    ProcessAttribution(data);
            }
            catch (InstallReferrerException ex)
            {
                Debug.LogWarning($"[Referrer] Failed: {ex.Error.errorMessage}");
            }
            catch (TimeoutException)
            {
                Debug.LogWarning("[Referrer] Timed out waiting for response");
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[Referrer] Cancelled (MonoBehaviour destroyed)");
            }
        }

        private void ProcessAttribution(CachedReferrerData data)
        {
            // Example: Route attribution to your analytics system
            Debug.Log($"[Referrer] Attributed install: source={data.UtmSource}, " +
                      $"medium={data.UtmMedium}, campaign={data.UtmCampaign}");
        }
    }
}
