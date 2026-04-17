// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;

namespace BizSim.Google.Play.InstallReferrer
{
    /// <summary>
    /// Singleton controller for the Google Play Install Referrer API.
    /// Attach to a persistent GameObject (e.g., one marked with <c>DontDestroyOnLoad</c>).
    ///
    /// <b>Features:</b>
    /// <list type="bullet">
    /// <item>Automatic caching with <c>appInstallTime</c> + <c>sdkVersion</c> validation</item>
    /// <item>Retry with exponential backoff (3 attempts, 2s base delay)</item>
    /// <item>Pluggable analytics via <see cref="IInstallReferrerAnalyticsAdapter"/></item>
    /// <item>Pluggable cache storage via <see cref="IInstallReferrerCacheProvider"/></item>
    /// <item>Editor mock config support via <see cref="InstallReferrerMockConfig"/></item>
    /// </list>
    ///
    /// <b>Usage:</b>
    /// <code>
    /// var data = await InstallReferrerController.Instance.FetchInstallReferrerAsync();
    /// Debug.Log($"Source: {data.UtmSource}, Campaign: {data.UtmCampaign}");
    /// </code>
    /// </summary>
    [HelpURL("https://github.com/BizSim-Game-Studios/com.bizsim.google.play.installreferrer#quick-start")]
    [AddComponentMenu("BizSim/Install Referrer Controller")]
    public class InstallReferrerController : MonoBehaviour, IInstallReferrerProvider
    {
        /// <summary>
        /// Singleton instance. If no instance exists in the scene, one is automatically
        /// created on a new GameObject marked with <c>DontDestroyOnLoad</c>.
        /// This matches the pattern used by Google Mobile Ads SDK and similar plugins.
        /// </summary>
        public static InstallReferrerController Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Search scene first (user may have placed it manually)
#if UNITY_2023_1_OR_NEWER
                    _instance = FindAnyObjectByType<InstallReferrerController>();
#else
                    _instance = FindObjectOfType<InstallReferrerController>();
#endif

                    if (_instance == null)
                    {
                        var go = new GameObject("[InstallReferrerController]");
                        _instance = go.AddComponent<InstallReferrerController>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        private static InstallReferrerController _instance;

        // --- Events ---

        /// <summary>Fired when referrer data is successfully fetched or loaded from cache.</summary>
        public event Action<CachedReferrerData> OnReferrerDataReady;

        /// <summary>Fired when the fetch fails after all retries.</summary>
        public event Action<InstallReferrerError> OnError;

        /// <summary>Fired when the cache is invalidated (diagnostic event).</summary>
        public event Action<CacheInvalidationReason> OnCacheInvalidated;

        // --- Public State ---

        /// <summary>Current cached referrer data, or null if not yet fetched.</summary>
        public CachedReferrerData CachedData { get; private set; }

        /// <summary>Whether a fetch is currently in progress.</summary>
        public bool IsFetching { get; private set; }

        // --- Configuration ---

        /// <summary>PlayerPrefs key used to persist referrer data between sessions.</summary>
        public const string CACHE_PREFS_KEY = "InstallReferrer_Cache";

        private const int MAX_RETRIES = 3;
        private const float RETRY_BASE_DELAY = 2f; // seconds
        /// <summary>
        /// SDK version used for cache invalidation on upgrade.
        /// Auto-synced from <c>package.json</c> by <c>PackageVersionSync</c>.
        /// </summary>
        private static string SdkVersion => PackageVersion.Current;
        private const int CACHE_MAX_AGE_HOURS = 2160; // 90 days — referrer data is immutable per install; cache is already invalidated on reinstall (AppInstallTime) or SDK upgrade (SdkVersion)

        private int _retryCount;

        // --- Privacy & Consent ---

        /// <summary>
        /// PlayerPrefs key for persisting ConsentGranted across app restarts (GDPR right-to-erasure).
        /// Default value (missing key) is <c>true</c> for backward compatibility with consumers
        /// who relied on the v1.0.2-and-earlier in-memory-only default.
        /// </summary>
        private const string ConsentGrantedPrefsKey = "BizSim.InstallReferrer.ConsentGranted";

        /// <summary>Whether the user has granted consent for referrer data collection. Persisted to PlayerPrefs since v1.0.3. Defaults to true on fresh install.</summary>
        public bool ConsentGranted { get; private set; } = true;

        // --- Pluggable services ---
        private IInstallReferrerAnalyticsAdapter _analyticsAdapter;
        private IInstallReferrerCacheProvider _cacheProvider;
        private string _userId;

        // --- Editor Mock Config ---
#if UNITY_EDITOR
        [Header("Editor Mock Config")]
        [Tooltip("Assign an InstallReferrerMockConfig asset to test different scenarios in the Editor.")]
        [SerializeField] private InstallReferrerMockConfig _mockConfig;
#endif

        // --- Test Mode (debug builds only, Android device) ---
        [Header("Test Mode (Debug Builds Only)")]
        [Tooltip("Enable to use a fake referrer string on-device. Only works in debug builds.")]
        [SerializeField] private bool _useFakeForTesting;

        [Tooltip("Fake referrer URL for on-device testing (e.g., 'utm_source=test&utm_medium=cpc').")]
        [TextArea(1, 3)]
        [SerializeField] private string _fakeReferrerUrl = "utm_source=test&utm_medium=cpc&utm_campaign=debug";

        [Header("Privacy & Encryption")]
        [Tooltip("Enable AES-256-CBC encrypted cache storage. Uses device-unique key derivation.")]
        [SerializeField] private bool _useEncryptedCache;

        [Header("Logging")]
        [Tooltip("Minimum log level. Silent suppresses all output including errors.")]
        [SerializeField] private LogLevel _logLevel = LogLevel.Verbose;

        /// <summary>
        /// Cancelled in <see cref="OnDestroy"/> to abort in-flight async operations
        /// (Task.Delay timeouts, retry waits) so they don't outlive the MonoBehaviour.
        /// </summary>
        private CancellationTokenSource _destroyCts;

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaClass _bridgeClass;
#endif

        // =================================================================
        // Lifecycle
        // =================================================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _destroyCts = new CancellationTokenSource();
            BizSimLogger.MinLevel = _logLevel;

            // Auto-set encrypted cache provider if enabled and no custom provider is set
            if (_useEncryptedCache && _cacheProvider == null)
                _cacheProvider = new EncryptedPlayerPrefsCacheProvider();

            // Read persisted ConsentGranted state (GDPR right-to-erasure: revocation survives restart).
            // Default 1 (true) on missing key — backward compat with v1.0.2-and-earlier consumers.
            ConsentGranted = PlayerPrefs.GetInt(ConsentGrantedPrefsKey, 1) == 1;

            LoadCache();
        }

        private void OnDestroy()
        {
            // Cancel all in-flight async operations (Task.Delay, linked tokens)
            _destroyCts?.Cancel();
            _destroyCts?.Dispose();
            _destroyCts = null;

            StopAllCoroutines();
            IsFetching = false;

            // Null out all event delegates to release subscriber references.
            // Prevents memory leaks when external objects subscribed via +=
            // but forgot to -= before the controller was destroyed.
            OnReferrerDataReady = null;
            OnError = null;
            OnCacheInvalidated = null;

            // Release injected adapters so GC can collect them independently.
            _analyticsAdapter = null;
            _cacheProvider = null;

            if (Instance == this) Instance = null;

#if UNITY_ANDROID && !UNITY_EDITOR
            // Clean up Java bridge state (static fields survive Domain Reload in Editor)
            try { _bridgeClass?.CallStatic("cleanup"); }
            catch { /* best-effort */ }

            _bridgeClass?.Dispose();
            _bridgeClass = null;
#endif
        }

        // =================================================================
        // Public Configuration API
        // =================================================================

        /// <summary>Sets the minimum log level at runtime. Overrides the Inspector value.</summary>
        public void SetLogLevel(LogLevel level)
        {
            _logLevel = level;
            BizSimLogger.MinLevel = level;
        }

        /// <summary>Sets the user ID for analytics attribution.</summary>
        public void SetUserId(string userId)
        {
            _userId = userId;
        }

        /// <summary>Sets a custom analytics adapter for logging referrer events.</summary>
        public void SetAnalyticsAdapter(IInstallReferrerAnalyticsAdapter adapter)
        {
            _analyticsAdapter = adapter;
        }

        /// <summary>Sets a custom cache provider for persisting referrer data.</summary>
        public void SetCacheProvider(IInstallReferrerCacheProvider provider)
        {
            _cacheProvider = provider;
            LoadCache(); // Reload from new provider
        }

        /// <summary>
        /// Sets user consent for referrer data collection.
        /// When consent is revoked, cached data is also cleared (GDPR right-to-erasure).
        /// </summary>
        public void SetConsentGranted(bool granted)
        {
            ConsentGranted = granted;
            // Persist across app restarts (GDPR right-to-erasure — v1.0.3+).
            PlayerPrefs.SetInt(ConsentGrantedPrefsKey, granted ? 1 : 0);
            PlayerPrefs.Save();
            if (!granted)
                ClearCachedData();
        }

        // =================================================================
        // Public API
        // =================================================================

        /// <summary>
        /// Initiates a fetch of install referrer data.
        /// Returns cached data if valid; otherwise queries the API.
        /// </summary>
        public void FetchInstallReferrer()
        {
            if (IsFetching) return;

            if (!ConsentGranted)
            {
                OnError?.Invoke(new InstallReferrerError
                {
                    errorCode = -102,
                    errorMessage = "User consent not granted for referrer data collection",
                    isRetryable = false
                });
                return;
            }

            // Check cache first
            if (CachedData != null && IsCacheValid(CachedData))
            {
                BizSimLogger.Info("Returning valid cached referrer data");
                _analyticsAdapter?.LogReferrerFetched(CachedData, true);
                _analyticsAdapter?.LogReferrerFetchedMinimal(new ReferrerAnalyticsEvent(CachedData, true));
                OnReferrerDataReady?.Invoke(CachedData);
                return;
            }

            _retryCount = 0;
            ExecuteFetch();
        }

        /// <summary>
        /// Async/await version of <see cref="FetchInstallReferrer"/>.
        /// </summary>
        public async Task<CachedReferrerData> FetchInstallReferrerAsync(float timeoutSeconds = 30f)
        {
            // Fast path: valid cache
            if (CachedData != null && IsCacheValid(CachedData))
            {
                BizSimLogger.Info("Returning valid cached referrer data (async fast path)");
                _analyticsAdapter?.LogReferrerFetched(CachedData, true);
                _analyticsAdapter?.LogReferrerFetchedMinimal(new ReferrerAnalyticsEvent(CachedData, true));
                return CachedData;
            }

            var tcs = new TaskCompletionSource<CachedReferrerData>();

            Action<CachedReferrerData> onSuccess = null;
            Action<InstallReferrerError> onError = null;

            void Cleanup()
            {
                OnReferrerDataReady -= onSuccess;
                OnError -= onError;
            }

            onSuccess = data =>
            {
                Cleanup();
                tcs.TrySetResult(data);
            };

            onError = error =>
            {
                Cleanup();
                tcs.TrySetException(new InstallReferrerException(error));
            };

            OnReferrerDataReady += onSuccess;
            OnError += onError;

            FetchInstallReferrer();

            // Use _destroyCts so Task.Delay is cancelled if the MonoBehaviour is destroyed.
            var token = _destroyCts?.Token ?? CancellationToken.None;

            using var registration = token.Register(() =>
            {
                Cleanup();
                tcs.TrySetCanceled(token);
            });

            var completedTask = await Task.WhenAny(
                tcs.Task,
                Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), token)
            );

            if (completedTask != tcs.Task)
            {
                Cleanup();
                IsFetching = false;

                if (token.IsCancellationRequested)
                {
                    BizSimLogger.Info("Fetch cancelled — MonoBehaviour destroyed");
                    throw new OperationCanceledException(token);
                }

                BizSimLogger.Error($"Timeout: no callback received within {timeoutSeconds}s");
                tcs.TrySetException(new TimeoutException(
                    $"[InstallReferrer] No callback received within {timeoutSeconds}s."));
            }

            return await tcs.Task;
        }

        /// <inheritdoc/>
        public async Task<CachedReferrerData> FetchInstallReferrerAsync(
            CancellationToken cancellationToken, float timeoutSeconds = 30f)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Fast path: valid cache
            if (CachedData != null && IsCacheValid(CachedData))
            {
                BizSimLogger.Info("Returning valid cached referrer data (async fast path)");
                _analyticsAdapter?.LogReferrerFetched(CachedData, true);
                _analyticsAdapter?.LogReferrerFetchedMinimal(new ReferrerAnalyticsEvent(CachedData, true));
                return CachedData;
            }

            // Link caller token + destroy token so either can cancel.
            using var linkedCts = _destroyCts != null
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _destroyCts.Token)
                : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var linkedToken = linkedCts.Token;

            var tcs = new TaskCompletionSource<CachedReferrerData>();

            Action<CachedReferrerData> onSuccess = null;
            Action<InstallReferrerError> onError = null;

            void Cleanup()
            {
                OnReferrerDataReady -= onSuccess;
                OnError -= onError;
            }

            onSuccess = data =>
            {
                Cleanup();
                tcs.TrySetResult(data);
            };

            onError = error =>
            {
                Cleanup();
                tcs.TrySetException(new InstallReferrerException(error));
            };

            using var registration = linkedToken.Register(() =>
            {
                Cleanup();
                tcs.TrySetCanceled(linkedToken);
            });

            OnReferrerDataReady += onSuccess;
            OnError += onError;

            FetchInstallReferrer();

            var completedTask = await Task.WhenAny(
                tcs.Task,
                Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), linkedToken)
            );

            if (completedTask != tcs.Task)
            {
                Cleanup();
                IsFetching = false;

                if (linkedToken.IsCancellationRequested)
                {
                    BizSimLogger.Info("Fetch cancelled via CancellationToken or MonoBehaviour destroyed");
                    throw new OperationCanceledException(cancellationToken.IsCancellationRequested
                        ? cancellationToken
                        : linkedToken);
                }

                BizSimLogger.Error($"Timeout: no callback received within {timeoutSeconds}s");
                tcs.TrySetException(new TimeoutException(
                    $"[InstallReferrer] No callback received within {timeoutSeconds}s."));
            }

            return await tcs.Task;
        }

        // =================================================================
        // Bridge Execution
        // =================================================================

        private void ExecuteFetch()
        {
            IsFetching = true;

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (_bridgeClass == null)
                    _bridgeClass = new AndroidJavaClass(
                        "com.bizsim.google.play.installreferrer.InstallReferrerBridge");

                bool useFake = Debug.isDebugBuild && _useFakeForTesting;

                if (useFake)
                {
                    _bridgeClass.CallStatic("checkInstallReferrerWithFake",
                        gameObject.name,
                        nameof(OnInstallReferrerResult),
                        nameof(OnInstallReferrerError),
                        true,
                        _fakeReferrerUrl);
                }
                else
                {
                    _bridgeClass.CallStatic("checkInstallReferrer",
                        gameObject.name,
                        nameof(OnInstallReferrerResult),
                        nameof(OnInstallReferrerError));
                }
            }
            catch (Exception e)
            {
                BizSimLogger.Error($"Java bridge call failed: {e.Message}");
                IsFetching = false;
                OnError?.Invoke(new InstallReferrerError
                {
                    errorCode = -100,
                    errorMessage = $"JNI call failed: {e.Message}",
                    isRetryable = false
                });
            }
#else
            // --- Editor Mock Path ---
            // IsFetching remains true until ProcessResult or error path completes,
            // so latency simulation via DelayedMockResult is correctly guarded.
            BuildEditorMockResult();
#endif
        }

#if !UNITY_ANDROID || UNITY_EDITOR
        private void BuildEditorMockResult()
        {
#if UNITY_EDITOR
            // Priority 1: Test Mode fields
            if (_useFakeForTesting)
            {
                BizSimLogger.Info($"Editor test mode — referrer: {_fakeReferrerUrl}");
                var fakeResult = new InstallReferrerResult
                {
                    installReferrer = _fakeReferrerUrl ?? ""
                };
                ProcessResult(fakeResult);
                return;
            }

            // Priority 2: Mock Config ScriptableObject
            if (_mockConfig != null)
            {
                if (_mockConfig.SimulateOffline || _mockConfig.SimulateError)
                {
                    int errorCode = _mockConfig.SimulateOffline ? 2 : _mockConfig.SimulatedErrorCode;
                    string errorMsg = _mockConfig.SimulateOffline
                        ? "Simulated offline (mock config)"
                        : $"Simulated error (mock config)";
                    BizSimLogger.Info($"Editor mock — simulating error code {errorCode}");

                    IsFetching = false;
                    var error = new InstallReferrerError
                    {
                        errorCode = errorCode,
                        errorMessage = errorMsg,
                        isRetryable = errorCode == 2 || errorCode == -1
                    };
                    LogFetchResult(false);
                    _analyticsAdapter?.LogReferrerError(error);
                    OnError?.Invoke(error);
                    return;
                }

                if (_mockConfig.SimulatedLatencySeconds > 0)
                {
                    StartCoroutine(DelayedMockResult(_mockConfig));
                    return;
                }

                BizSimLogger.Info($"Editor mock — referrer: {_mockConfig.MockReferrerUrl}");
                var mockResult = new InstallReferrerResult
                {
                    installReferrer = _mockConfig.MockReferrerUrl ?? "",
                    referrerClickTimestampSeconds = _mockConfig.MockReferrerClickTimestamp,
                    installBeginTimestampSeconds = _mockConfig.MockInstallBeginTimestamp,
                    googlePlayInstantParam = _mockConfig.MockGooglePlayInstant
                };
                ProcessResult(mockResult);
                return;
            }
#endif
            // Default fallback: empty referrer (organic install)
            BizSimLogger.Info("Editor mode — no mock config assigned, returning empty referrer");
            ProcessResult(new InstallReferrerResult());
        }

#if UNITY_EDITOR
        private IEnumerator DelayedMockResult(InstallReferrerMockConfig config)
        {
            BizSimLogger.Verbose($"Editor mock — simulating {config.SimulatedLatencySeconds}s latency");
            yield return new WaitForSeconds(config.SimulatedLatencySeconds);

            var mockResult = new InstallReferrerResult
            {
                installReferrer = config.MockReferrerUrl ?? "",
                referrerClickTimestampSeconds = config.MockReferrerClickTimestamp,
                installBeginTimestampSeconds = config.MockInstallBeginTimestamp,
                googlePlayInstantParam = config.MockGooglePlayInstant
            };
            ProcessResult(mockResult);
        }
#endif
#endif

        // =================================================================
        // UnitySendMessage Callbacks (invoked from Java via JNI)
        // =================================================================

        /// <summary>
        /// Called by the Java bridge when the API returns a successful result.
        /// </summary>
        [Preserve]
        private void OnInstallReferrerResult(string json)
        {
            try
            {
                var result = JsonUtility.FromJson<InstallReferrerResult>(json);
                BizSimLogger.Info($"Result: referrer=\"{result.installReferrer}\"");
                ProcessResult(result);
            }
            catch (Exception e)
            {
                IsFetching = false;
                BizSimLogger.Error($"Failed to parse result: {e.Message}\nJSON: {json}");
                OnError?.Invoke(new InstallReferrerError
                {
                    errorCode = -100,
                    errorMessage = $"Parse error: {e.Message}",
                    isRetryable = false
                });
            }
        }

        /// <summary>
        /// Called by the Java bridge when the API returns an error.
        /// </summary>
        [Preserve]
        private void OnInstallReferrerError(string json)
        {
            try
            {
                var error = JsonUtility.FromJson<InstallReferrerError>(json);

                BizSimLogger.Error($"Error: {error.ErrorCodeName} ({error.errorCode})" +
                                         $" — {error.errorMessage} — retryable={error.isRetryable}");

                // Automatic retry with exponential backoff for transient errors.
                // IsFetching stays true during retry to prevent concurrent fetches.
                if (error.isRetryable && _retryCount < MAX_RETRIES)
                {
                    _retryCount++;
                    float delay = RETRY_BASE_DELAY * Mathf.Pow(2, _retryCount - 1);
                    BizSimLogger.Info($"Retry {_retryCount}/{MAX_RETRIES} in {delay}s");
                    StartCoroutine(RetryAfterDelay(delay));
                    return;
                }

                // No more retries — mark as done
                IsFetching = false;
                LogFetchResult(false);
                _analyticsAdapter?.LogReferrerError(error);
                OnError?.Invoke(error);
            }
            catch (Exception e)
            {
                IsFetching = false;
                BizSimLogger.Error($"Failed to parse error: {e.Message}\nJSON: {json}");
                OnError?.Invoke(new InstallReferrerError
                {
                    errorCode = -100,
                    errorMessage = $"Error JSON parse failed: {e.Message}",
                    isRetryable = false
                });
            }
        }

        private IEnumerator RetryAfterDelay(float seconds)
        {
            yield return new WaitForSeconds(seconds);

            // Guard: if the MonoBehaviour was destroyed during the wait,
            // StopAllCoroutines should have killed this, but check defensively.
            if (_destroyCts == null || _destroyCts.IsCancellationRequested)
            {
                BizSimLogger.Info("Retry cancelled — MonoBehaviour destroyed");
                yield break;
            }

            ExecuteFetch();
        }

        // =================================================================
        // Result Processing
        // =================================================================

        private void ProcessResult(InstallReferrerResult result)
        {
            IsFetching = false;
            _retryCount = 0;

            var cached = InstallReferrerCacheLogic.CreateCachedData(
                result, GetAppInstallTimeMs(), SdkVersion);

            CachedData = cached;
            SaveCache(cached);
            LogFetchResult(true);
            _analyticsAdapter?.LogReferrerFetched(cached, false);
            _analyticsAdapter?.LogReferrerFetchedMinimal(new ReferrerAnalyticsEvent(cached, false));
            OnReferrerDataReady?.Invoke(cached);
        }

        // =================================================================
        // Cache Management
        // =================================================================

        private void LoadCache()
        {
            try
            {
                CachedReferrerData loaded;
                if (_cacheProvider != null)
                {
                    loaded = _cacheProvider.Load();
                }
                else
                {
                    string json = PlayerPrefs.GetString(CACHE_PREFS_KEY, "");
                    if (string.IsNullOrEmpty(json)) return;
                    loaded = JsonUtility.FromJson<CachedReferrerData>(json);
                }

                if (loaded == null) return;

                if (IsCacheValid(loaded))
                {
                    CachedData = loaded;
                    BizSimLogger.Info("Loaded valid cached referrer data");
                }
                else
                {
                    // Cache is invalid — determine reason
                    var reason = GetInvalidationReason(loaded);
                    BizSimLogger.Info($"Cache invalidated: {reason}");
                    ClearCache(reason);
                }
            }
            catch (Exception e)
            {
                BizSimLogger.Error($"Failed to load cache: {e.Message}");
                ClearCache(CacheInvalidationReason.DataCorrupted);
            }
        }

        private void SaveCache(CachedReferrerData data)
        {
            try
            {
                if (_cacheProvider != null)
                {
                    _cacheProvider.Save(data);
                }
                else
                {
                    PlayerPrefs.SetString(CACHE_PREFS_KEY, JsonUtility.ToJson(data));
                    PlayerPrefs.Save();
                }
            }
            catch (Exception e)
            {
                BizSimLogger.Error($"Failed to save cache: {e.Message}");
            }
        }

        private void ClearCache(CacheInvalidationReason reason)
        {
            CachedData = null;
            if (_cacheProvider != null)
            {
                _cacheProvider.Clear();
            }
            else
            {
                PlayerPrefs.DeleteKey(CACHE_PREFS_KEY);
                PlayerPrefs.Save();
            }
            OnCacheInvalidated?.Invoke(reason);
        }

        /// <summary>
        /// Clears the cached referrer data. Call this to force a fresh fetch on next request.
        /// </summary>
        public void ClearCachedData()
        {
            ClearCache(CacheInvalidationReason.ManualClear);
        }

        /// <summary>
        /// Delegates to <see cref="InstallReferrerCacheLogic.IsCacheValid"/> —
        /// a pure static method that can be unit tested without Play Mode.
        /// </summary>
        private bool IsCacheValid(CachedReferrerData data)
            => InstallReferrerCacheLogic.IsCacheValid(
                data, SdkVersion, GetAppInstallTimeMs(), CACHE_MAX_AGE_HOURS);

        private CacheInvalidationReason GetInvalidationReason(CachedReferrerData data)
            => InstallReferrerCacheLogic.GetInvalidationReason(
                data, SdkVersion, GetAppInstallTimeMs(), CACHE_MAX_AGE_HOURS);

        // =================================================================
        // JNI Helpers
        // =================================================================

        /// <summary>
        /// Gets the app's first install time in milliseconds since epoch via JNI.
        /// Returns -1 if unavailable (e.g., in Editor or on non-Android platforms).
        /// </summary>
        public long GetAppInstallTimeMs()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (_bridgeClass == null)
                    _bridgeClass = new AndroidJavaClass(
                        "com.bizsim.google.play.installreferrer.InstallReferrerBridge");
                return _bridgeClass.CallStatic<long>("getAppInstallTimeMs");
            }
            catch (Exception e)
            {
                BizSimLogger.Error($"GetAppInstallTimeMs failed: {e.Message}");
                return -1;
            }
#else
            return -1;
#endif
        }

        // =================================================================
        // Analytics
        // =================================================================

        private void LogFetchResult(bool success)
        {
            string result = success ? "success" : "error";

#if BIZSIM_FIREBASE && !UNITY_EDITOR
            try
            {
                Firebase.Analytics.FirebaseAnalytics.LogEvent("install_referrer_fetch",
                    new Firebase.Analytics.Parameter("result", result)
                );
            }
            catch (Exception e)
            {
                BizSimLogger.Error($"Analytics log failed: {e.Message}");
            }
#else
            BizSimLogger.Info($"Fetch result: {result}");
#endif
        }

        // =================================================================
        // Logging
        // =================================================================

        // All logging is now delegated to BizSimLogger, whose methods are
        // marked [Conditional("DEBUG")]. This eliminates string interpolation
        // and Debug.Log calls in release builds at the compiler level.
        //
        // Error-level messages use BizSimLogger.Error() which is NOT
        // conditional — errors always reach the console.
    }
}
