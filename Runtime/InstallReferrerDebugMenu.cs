// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

using System;
using UnityEngine;

namespace BizSim.Google.Play.InstallReferrer
{
    /// <summary>
    /// Runtime debug menu for testing Install Referrer behavior on-device.
    /// Only active in <b>debug builds</b> (<c>Debug.isDebugBuild == true</c>).
    ///
    /// <b>Usage:</b> Add this component to any persistent GameObject.
    /// Open the menu at runtime by tapping the top-left corner 5 times
    /// or pressing <c>F9</c> on keyboard.
    ///
    /// <b>Input System support:</b> This component uses legacy Input by default.
    /// If <c>com.unity.inputsystem</c> is installed, the optional
    /// <c>BizSim.Google.Play.InstallReferrer.InputSystem</c> assembly auto-registers
    /// New Input System handlers via <see cref="KeyToggleCheck"/> and
    /// <see cref="TouchBeganCheck"/> callbacks.
    /// </summary>
    [HelpURL("https://github.com/BizSim-Game-Studios/com.bizsim.google.play.installreferrer#debug-menu")]
    [AddComponentMenu("BizSim/Install Referrer Debug Menu")]
    public class InstallReferrerDebugMenu : MonoBehaviour
    {
        // --- Optional Input System hooks (registered by InputSystemSupport assembly) ---

        /// <summary>
        /// Returns true when the debug menu toggle key is pressed (e.g., F9).
        /// Set by the optional InputSystem support assembly. When null, legacy
        /// <c>Input.GetKeyDown(KeyCode.F9)</c> is used as fallback.
        /// </summary>
        internal static Func<bool> KeyToggleCheck;

        /// <summary>
        /// Returns the screen position of a touch that just began, or null if none.
        /// Set by the optional InputSystem support assembly. When null, legacy
        /// <c>Input.GetTouch(0)</c> is used as fallback.
        /// </summary>
        internal static Func<Vector2?> TouchBeganCheck;

        private bool _showMenu;
        private int _tapCount;
        private float _lastTapTime;

        // Mock referrer input
        private string _mockReferrerInput = "utm_source=debug&utm_medium=test&utm_campaign=manual";

        private Vector2 _scrollPos;

        private void Update()
        {
            if (!Debug.isDebugBuild) return;

            // Keyboard toggle (F9)
            bool keyPressed = KeyToggleCheck?.Invoke() ?? Input.GetKeyDown(KeyCode.F9);
            if (keyPressed)
                _showMenu = !_showMenu;

            // 5-tap toggle (top-left corner)
            Vector2? touchPos = TouchBeganCheck?.Invoke() ?? GetLegacyTouchBegan();
            if (touchPos.HasValue)
            {
                var pos = touchPos.Value;
                if (pos.x < 100 && pos.y > Screen.height - 100)
                {
                    if (Time.unscaledTime - _lastTapTime > 2f)
                        _tapCount = 0;

                    _tapCount++;
                    _lastTapTime = Time.unscaledTime;

                    if (_tapCount >= 5)
                    {
                        _showMenu = !_showMenu;
                        _tapCount = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the screen position of a legacy input touch that just began,
        /// or <c>null</c> if no touch is active. Used as fallback when the New Input System
        /// bridge is not registered.
        /// </summary>
        private static Vector2? GetLegacyTouchBegan()
        {
#if !ENABLE_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began)
                return Input.GetTouch(0).position;
#endif
            return null;
        }

        private void OnGUI()
        {
            if (!Debug.isDebugBuild || !_showMenu) return;

            float scale = Screen.dpi > 0 ? Screen.dpi / 160f : 1f;
            int padding = Mathf.RoundToInt(10 * scale);
            int width = Mathf.Min(Mathf.RoundToInt(400 * scale), Screen.width - padding * 2);
            int height = Mathf.Min(Mathf.RoundToInt(580 * scale), Screen.height - padding * 2);

            GUILayout.BeginArea(new Rect(padding, padding, width, height),
                GUI.skin.box);

            _scrollPos = GUILayout.BeginScrollView(_scrollPos);

            GUILayout.Label("<b>Install Referrer Debug Menu</b>",
                new GUIStyle(GUI.skin.label) { richText = true, fontSize = Mathf.RoundToInt(16 * scale) });

            GUILayout.Space(8);

            // --- Current State ---
            var ctrl = InstallReferrerController.Instance;
            if (ctrl == null)
            {
                GUILayout.Label("⚠ InstallReferrerController not found");
                GUILayout.EndScrollView();
                GUILayout.EndArea();
                return;
            }

            GUILayout.Label($"IsFetching: {ctrl.IsFetching}");

            var data = ctrl.CachedData;
            if (data != null)
            {
                GUILayout.Label("<b>Cached Data:</b>",
                    new GUIStyle(GUI.skin.label) { richText = true });
                GUILayout.Label($"  Referrer: {(string.IsNullOrEmpty(data.InstallReferrer) ? "(empty)" : data.InstallReferrer)}");
                GUILayout.Label($"  Organic: {data.IsOrganic}");

                GUILayout.Space(4);
                GUILayout.Label("<b>UTM Parameters:</b>",
                    new GUIStyle(GUI.skin.label) { richText = true });
                GUILayout.Label($"  Source: {data.UtmSource}");
                GUILayout.Label($"  Medium: {data.UtmMedium}");
                GUILayout.Label($"  Campaign: {data.UtmCampaign}");
                GUILayout.Label($"  Content: {data.UtmContent}");
                GUILayout.Label($"  Term: {data.UtmTerm}");

                GUILayout.Space(4);
                GUILayout.Label($"  Install Time: {data.AppInstallTimeMs}");
                GUILayout.Label($"  SDK Version: {data.SdkVersion}");
                GUILayout.Label($"  Fetched: {data.FetchTimestamp}");
            }
            else
            {
                GUILayout.Label("Data: null (not fetched)");
            }

            GUILayout.Space(12);

            // --- Mock Referrer Injection ---
            GUILayout.Label("<b>Inject Mock Referrer:</b>",
                new GUIStyle(GUI.skin.label) { richText = true });

            _mockReferrerInput = GUILayout.TextField(_mockReferrerInput);

            if (GUILayout.Button("▶ Inject Mock Referrer"))
            {
                InjectMockReferrer(_mockReferrerInput);
            }

            GUILayout.Space(8);

            if (GUILayout.Button("🔄 Re-fetch Install Referrer"))
            {
                ctrl.ClearCachedData();
                ctrl.FetchInstallReferrer();
            }

            if (GUILayout.Button("🗑 Clear Cached Data"))
            {
                ctrl.ClearCachedData();
                Debug.Log("[InstallReferrer Debug] Cache cleared");
            }

            if (data != null && GUILayout.Button("📋 Copy JSON"))
            {
                string json = JsonUtility.ToJson(data, true);
                GUIUtility.systemCopyBuffer = json;
                Debug.Log("[InstallReferrer Debug] JSON copied to clipboard");
            }

            GUILayout.Space(8);

            if (GUILayout.Button("✕ Close"))
                _showMenu = false;

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        /// <summary>
        /// Injects a mock <see cref="InstallReferrerResult"/> into the controller's
        /// processing pipeline via <c>SendMessage</c>, bypassing the Java bridge.
        /// </summary>
        /// <param name="referrerUrl">Raw referrer URL string (e.g., "utm_source=debug&amp;utm_medium=test").</param>
        private void InjectMockReferrer(string referrerUrl)
        {
            var ctrl = InstallReferrerController.Instance;
            if (ctrl == null)
            {
                Debug.LogWarning("[InstallReferrer Debug] Controller not found");
                return;
            }

            if (ctrl.IsFetching)
            {
                Debug.LogWarning("[InstallReferrer Debug] Cannot inject — a fetch is already in progress");
                return;
            }

            string json = $@"{{
                ""installReferrer"": ""{EscapeJson(referrerUrl)}"",
                ""referrerClickTimestampSeconds"": 0,
                ""installBeginTimestampSeconds"": 0,
                ""referrerClickTimestampServerSeconds"": 0,
                ""installBeginTimestampServerSeconds"": 0,
                ""installVersion"": """",
                ""googlePlayInstantParam"": false
            }}";

            Debug.Log($"[InstallReferrer Debug] Injecting mock referrer: {referrerUrl}");
            ctrl.SendMessage("OnInstallReferrerResult", json, SendMessageOptions.RequireReceiver);
        }

        /// <summary>
        /// Escapes backslashes and double-quotes for safe JSON string embedding.
        /// </summary>
        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
