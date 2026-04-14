// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace BizSim.Google.Play.InstallReferrer.Editor
{
    [CustomEditor(typeof(InstallReferrerMockConfig))]
    public class InstallReferrerMockConfigEditor : UnityEditor.Editor
    {
        // ── Palette ──
        private static readonly Color Accent = new(0.35f, 0.61f, 1f);
        private static readonly Color AccentDim = new(0.35f, 0.61f, 1f, 0.08f);
        private static readonly Color Green = new(0.24f, 0.78f, 0.42f);
        private static readonly Color Red = new(0.92f, 0.34f, 0.34f);
        private static readonly Color Warn = new(1f, 0.82f, 0.22f);
        private static readonly Color Muted = new(0.6f, 0.6f, 0.6f);
        private static readonly Color CardBg = new(0.22f, 0.22f, 0.22f, 0.55f);
        private static readonly Color SepColor = new(1f, 1f, 1f, 0.06f);

        // ── Cached Styles ──
        private GUIStyle _codeStyle;

        // ── Serialized Properties ──
        private SerializedProperty _mockReferrerUrl;
        private SerializedProperty _mockReferrerClickTimestamp;
        private SerializedProperty _mockInstallBeginTimestamp;
        private SerializedProperty _mockGooglePlayInstant;
        private SerializedProperty _simulatedLatencySeconds;
        private SerializedProperty _simulateOffline;
        private SerializedProperty _simulateError;
        private SerializedProperty _simulatedErrorCode;

        private void OnEnable()
        {
            _mockReferrerUrl = serializedObject.FindProperty("MockReferrerUrl");
            _mockReferrerClickTimestamp = serializedObject.FindProperty("MockReferrerClickTimestamp");
            _mockInstallBeginTimestamp = serializedObject.FindProperty("MockInstallBeginTimestamp");
            _mockGooglePlayInstant = serializedObject.FindProperty("MockGooglePlayInstant");
            _simulatedLatencySeconds = serializedObject.FindProperty("SimulatedLatencySeconds");
            _simulateOffline = serializedObject.FindProperty("SimulateOffline");
            _simulateError = serializedObject.FindProperty("SimulateError");
            _simulatedErrorCode = serializedObject.FindProperty("SimulatedErrorCode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var config = (InstallReferrerMockConfig)target;

            // ── Presets ──
            DrawPresetsCard();

            EditorGUILayout.Space(6);

            // ── Mock Referrer Data ──
            DrawMockDataCard();

            EditorGUILayout.Space(6);

            // ── Simulation Settings ──
            DrawSimulationCard();

            EditorGUILayout.Space(6);

            // ── UTM Preview ──
            DrawUtmPreviewCard(config);

            EditorGUILayout.Space(6);

            // ── JSON Preview ──
            DrawJsonPreviewCard(config);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8);

            // ── What is this? ──
            DrawInfoCard();
        }

        // ─────────────────────────────────────────────
        // Presets Card
        // ─────────────────────────────────────────────

        private void DrawPresetsCard()
        {
            var outer = EditorGUILayout.BeginVertical();
            DrawCardBg(outer);

            GUILayout.Space(10);
            BeginPadded();

            EditorGUILayout.LabelField("Quick Presets", EditorStyles.boldLabel);
            DrawNote("Click a preset to populate the mock data fields.");
            GUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Organic", EditorStyles.miniButton))
            {
                _mockReferrerUrl.stringValue = "";
                _mockReferrerClickTimestamp.longValue = 0;
                _mockInstallBeginTimestamp.longValue = 0;
            }

            if (GUILayout.Button("Google Ads", EditorStyles.miniButton))
            {
                _mockReferrerUrl.stringValue = "utm_source=google&utm_medium=cpc&utm_campaign=summer_sale_2026&utm_content=banner_300x250";
                _mockReferrerClickTimestamp.longValue = 1738200000;
                _mockInstallBeginTimestamp.longValue = 1738200060;
            }

            if (GUILayout.Button("Facebook", EditorStyles.miniButton))
            {
                _mockReferrerUrl.stringValue = "utm_source=facebook&utm_medium=social&utm_campaign=retargeting_q1&utm_content=video_feed";
                _mockReferrerClickTimestamp.longValue = 1738200000;
                _mockInstallBeginTimestamp.longValue = 1738200120;
            }

            if (GUILayout.Button("Deep Link", EditorStyles.miniButton))
            {
                _mockReferrerUrl.stringValue = "utm_source=app&utm_medium=referral&utm_campaign=invite_friend&referrer_id=user_12345";
                _mockReferrerClickTimestamp.longValue = 1738200000;
                _mockInstallBeginTimestamp.longValue = 1738200030;
            }

            EditorGUILayout.EndHorizontal();

            EndPadded();
            GUILayout.Space(10);
            EditorGUILayout.EndVertical();
        }

        // ─────────────────────────────────────────────
        // Mock Data Card
        // ─────────────────────────────────────────────

        private void DrawMockDataCard()
        {
            var outer = EditorGUILayout.BeginVertical();
            DrawCardBg(outer);

            GUILayout.Space(10);
            BeginPadded();

            EditorGUILayout.LabelField("Mock Referrer Data", EditorStyles.boldLabel);
            GUILayout.Space(4);

            EditorGUILayout.PropertyField(_mockReferrerUrl, new GUIContent("Referrer URL", "Raw referrer string in query-string format."));
            EditorGUILayout.PropertyField(_mockReferrerClickTimestamp, new GUIContent("Click Timestamp", "Seconds since epoch when the referrer was clicked."));
            EditorGUILayout.PropertyField(_mockInstallBeginTimestamp, new GUIContent("Install Timestamp", "Seconds since epoch when install began."));
            EditorGUILayout.PropertyField(_mockGooglePlayInstant, new GUIContent("Play Instant", "Whether to simulate a Google Play Instant install."));

            EndPadded();
            GUILayout.Space(10);
            EditorGUILayout.EndVertical();
        }

        // ─────────────────────────────────────────────
        // Simulation Settings Card
        // ─────────────────────────────────────────────

        private void DrawSimulationCard()
        {
            var outer = EditorGUILayout.BeginVertical();
            DrawCardBg(outer);

            GUILayout.Space(10);
            BeginPadded();

            EditorGUILayout.LabelField("Simulation Settings", EditorStyles.boldLabel);
            GUILayout.Space(4);

            EditorGUILayout.PropertyField(_simulatedLatencySeconds, new GUIContent("Latency (sec)", "Simulated network latency."));
            EditorGUILayout.PropertyField(_simulateOffline, new GUIContent("Simulate Offline", "Returns SERVICE_UNAVAILABLE error."));
            EditorGUILayout.PropertyField(_simulateError, new GUIContent("Simulate Error", "Returns a specific error code."));

            if (_simulateError.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_simulatedErrorCode, new GUIContent("Error Code"));

                int code = _simulatedErrorCode.intValue;
                string codeName = code switch
                {
                    1 => "FEATURE_NOT_SUPPORTED",
                    2 => "SERVICE_UNAVAILABLE",
                    3 => "DEVELOPER_ERROR",
                    -1 => "SERVICE_DISCONNECTED",
                    _ => $"UNKNOWN ({code})"
                };
                bool retryable = code == 2 || code == -1;
                Color infoColor = retryable ? Warn : Red;

                var infoRect = EditorGUILayout.GetControlRect(false, 20);
                EditorGUI.DrawRect(infoRect, new Color(infoColor.r, infoColor.g, infoColor.b, 0.08f));
                EditorGUI.DrawRect(new Rect(infoRect.x, infoRect.y, 3, infoRect.height), infoColor);
                EditorGUI.LabelField(
                    new Rect(infoRect.x + 10, infoRect.y, infoRect.width - 12, infoRect.height),
                    $"{codeName}  •  {(retryable ? "retryable" : "not retryable")}",
                    new GUIStyle(EditorStyles.label) { fontSize = 11, normal = { textColor = infoColor } });

                EditorGUI.indentLevel--;
            }

            EndPadded();
            GUILayout.Space(10);
            EditorGUILayout.EndVertical();
        }

        // ─────────────────────────────────────────────
        // UTM Preview Card
        // ─────────────────────────────────────────────

        private void DrawUtmPreviewCard(InstallReferrerMockConfig config)
        {
            var outer = EditorGUILayout.BeginVertical();
            DrawCardBg(outer);

            GUILayout.Space(10);
            BeginPadded();

            EditorGUILayout.LabelField("UTM Preview", EditorStyles.boldLabel);
            GUILayout.Space(4);

            string url = config.MockReferrerUrl ?? "";
            if (string.IsNullOrEmpty(url))
            {
                DrawNote("No referrer URL — this simulates an organic install.");
            }
            else
            {
                InstallReferrerUtility.ParseUtmParameters(url,
                    out string src, out string med, out string cam,
                    out string con, out string trm);

                DrawUtmRow("Source", src);
                DrawUtmRow("Medium", med);
                DrawUtmRow("Campaign", cam);
                DrawUtmRow("Content", con);
                DrawUtmRow("Term", trm);
            }

            EndPadded();
            GUILayout.Space(10);
            EditorGUILayout.EndVertical();
        }

        // ─────────────────────────────────────────────
        // JSON Preview Card
        // ─────────────────────────────────────────────

        private void DrawJsonPreviewCard(InstallReferrerMockConfig config)
        {
            var outer = EditorGUILayout.BeginVertical();
            DrawCardBg(outer);

            GUILayout.Space(10);
            BeginPadded();

            EditorGUILayout.LabelField("JSON Preview", EditorStyles.boldLabel);
            DrawNote("This is what the controller will receive in Play Mode.");
            GUILayout.Space(4);

            if (_codeStyle == null)
            {
                _codeStyle = new GUIStyle(EditorStyles.label)
                {
                    font = Font.CreateDynamicFontFromOSFont("Consolas", 11),
                    fontSize = 11,
                    richText = true,
                    wordWrap = true,
                    normal = { textColor = new Color(0.78f, 0.86f, 0.68f) },
                    padding = new RectOffset(8, 8, 6, 6)
                };
            }
            var codeBg = new Color(0.12f, 0.12f, 0.12f, 0.8f);

            string json;
            if (config.SimulateOffline || config.SimulateError)
            {
                int code = config.SimulateOffline ? 2 : config.SimulatedErrorCode;
                string codeName = code switch
                {
                    1 => "FEATURE_NOT_SUPPORTED",
                    2 => "SERVICE_UNAVAILABLE",
                    3 => "DEVELOPER_ERROR",
                    -1 => "SERVICE_DISCONNECTED",
                    _ => $"UNKNOWN_{code}"
                };
                bool retryable = code == 2 || code == -1;
                json = $"<color=#569cd6>Error Response</color>\n" +
                       $"  errorCode: <color=#b5cea8>{code}</color>\n" +
                       $"  errorMessage: <color=#ce9178>\"Simulated error\"</color>\n" +
                       $"  isRetryable: <color=#569cd6>{retryable.ToString().ToLower()}</color>\n" +
                       $"  codeName: <color=#ce9178>\"{codeName}\"</color>";
            }
            else
            {
                string referrer = config.MockReferrerUrl ?? "";
                json = $"<color=#569cd6>Success Response</color>\n" +
                       $"  installReferrer: <color=#ce9178>\"{referrer}\"</color>\n" +
                       $"  referrerClickTimestampSeconds: <color=#b5cea8>{config.MockReferrerClickTimestamp}</color>\n" +
                       $"  installBeginTimestampSeconds: <color=#b5cea8>{config.MockInstallBeginTimestamp}</color>\n" +
                       $"  googlePlayInstantParam: <color=#569cd6>{config.MockGooglePlayInstant.ToString().ToLower()}</color>";
            }

            var codeRect = EditorGUILayout.GetControlRect(false, 90);
            EditorGUI.DrawRect(codeRect, codeBg);
            EditorGUI.LabelField(codeRect, json, _codeStyle);

            EndPadded();
            GUILayout.Space(10);
            EditorGUILayout.EndVertical();
        }

        // ─────────────────────────────────────────────
        // Info Card
        // ─────────────────────────────────────────────

        private static void DrawInfoCard()
        {
            var outer = EditorGUILayout.BeginVertical();

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(outer, AccentDim);
                EditorGUI.DrawRect(new Rect(outer.x, outer.y, 3, outer.height), Accent);
            }

            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(14);
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField("What is this?", EditorStyles.boldLabel);
            GUILayout.Space(2);

            EditorGUILayout.LabelField(
                "This asset lets you test Install Referrer without deploying to a device.\n\n" +
                "It simulates what the Google Play Install Referrer API would return — the referrer URL\n" +
                "with UTM parameters, timestamps, and install metadata. The controller uses this mock\n" +
                "data in Play Mode instead of calling the real API.\n\n" +
                "This is Editor-only. It has no effect in actual builds.",
                new GUIStyle(EditorStyles.wordWrappedLabel)
                {
                    fontSize = 11,
                    normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
                });

            GUILayout.Space(10);

            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        // ─────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────

        private static void DrawCardBg(Rect rect)
        {
            if (Event.current.type != EventType.Repaint) return;
            EditorGUI.DrawRect(rect, CardBg);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), SepColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), SepColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1, rect.height), SepColor);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.y, 1, rect.height), SepColor);
        }

        private static void BeginPadded()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(14);
            EditorGUILayout.BeginVertical();
        }

        private static void EndPadded()
        {
            EditorGUILayout.EndVertical();
            GUILayout.Space(14);
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawNote(string text)
        {
            EditorGUILayout.LabelField(text, new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) },
                fontStyle = FontStyle.Italic
            });
        }

        private static void DrawUtmRow(string label, string value)
        {
            var rect = EditorGUILayout.GetControlRect(false, 18);
            float labelW = 80;
            EditorGUI.LabelField(new Rect(rect.x, rect.y, labelW, rect.height), label,
                new GUIStyle(EditorStyles.label) { fontSize = 11, normal = { textColor = new Color(0.6f, 0.6f, 0.6f) } });
            EditorGUI.LabelField(new Rect(rect.x + labelW, rect.y, rect.width - labelW, rect.height),
                string.IsNullOrEmpty(value) ? "—" : value,
                new GUIStyle(EditorStyles.label) { fontSize = 11 });
        }
    }
}
#endif
