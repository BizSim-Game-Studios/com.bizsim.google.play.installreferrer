// Sample: Basic Install Referrer Integration
// Import this sample via Package Manager → Install Referrer → Samples → Basic Integration

using UnityEngine;

namespace BizSim.Google.Play.InstallReferrer.Samples
{
    /// <summary>
    /// Minimal example of fetching install referrer data on app launch.
    /// Attach this to a persistent GameObject (e.g., GameManager).
    /// </summary>
    public class BasicReferrerFetch : MonoBehaviour
    {
        private void Start()
        {
            // Subscribe to events
            InstallReferrerController.Instance.OnReferrerDataReady += OnReferrerReady;
            InstallReferrerController.Instance.OnError += OnError;

            // Fetch referrer data (uses cache if valid)
            InstallReferrerController.Instance.FetchInstallReferrer();
        }

        private void OnReferrerReady(CachedReferrerData data)
        {
            Debug.Log($"[Referrer] Source: {data.UtmSource}");
            Debug.Log($"[Referrer] Medium: {data.UtmMedium}");
            Debug.Log($"[Referrer] Campaign: {data.UtmCampaign}");
            Debug.Log($"[Referrer] Is Organic: {data.IsOrganic}");

            if (!data.IsOrganic && data.UtmCampaign == "invite")
            {
                string inviterUserId = data.UtmSource;
                Debug.Log($"[Referrer] Invited by: {inviterUserId}");
                // TODO: Grant referral reward and register inviter
            }
        }

        private void OnError(InstallReferrerError error)
        {
            Debug.LogWarning($"[Referrer] Error: {error.errorMessage} (retryable: {error.isRetryable})");
            // The controller retries automatically for transient errors.
            // Handle permanent errors here (e.g., show default UI).
        }

        private void OnDestroy()
        {
            if (InstallReferrerController.Instance != null)
            {
                InstallReferrerController.Instance.OnReferrerDataReady -= OnReferrerReady;
                InstallReferrerController.Instance.OnError -= OnError;
            }
        }
    }
}
