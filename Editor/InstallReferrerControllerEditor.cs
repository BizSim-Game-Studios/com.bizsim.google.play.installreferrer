// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace BizSim.Google.Play.InstallReferrer.Editor
{
    /// <summary>
    /// Custom inspector for <see cref="InstallReferrerController"/> providing a card-based UI
    /// with setup, simulation, fetch, results, and debug sections.
    /// Uses a dark-theme-safe WCAG AA palette with foldable cards and status pills.
    /// </summary>
    [CustomEditor(typeof(InstallReferrerController))]
    public class InstallReferrerControllerEditor : UnityEditor.Editor
    {
        // ── Design tokens ──
        private const float CardPad = 14f;
        private const float CardSpacing = 6f;

        // ── Palette (dark-theme safe, WCAG AA contrast) ──
        private static readonly Color CardBg = new(0.22f, 0.22f, 0.22f, 0.55f);
        private static readonly Color CardBorder = new(1f, 1f, 1f, 0.06f);
        private static readonly Color Accent = new(0.35f, 0.61f, 1f);
        private static readonly Color Green = new(0.24f, 0.78f, 0.42f);
        private static readonly Color GreenDim = new(0.24f, 0.78f, 0.42f, 0.14f);
        private static readonly Color Red = new(0.92f, 0.34f, 0.34f);
        private static readonly Color Muted = new(0.6f, 0.6f, 0.6f);
        private static readonly Color SepColor = new(1f, 1f, 1f, 0.06f);

        // ── Serialized Properties ──
        private SerializedProperty _logLevel;
        private SerializedProperty _mockConfig;
        private SerializedProperty _useFakeForTesting;
        private SerializedProperty _fakeReferrerUrl;

        // ── Foldout states ──
        private static bool _foldSetup = true;
        private static bool _foldSim;
        private static bool _foldResults = true;
        private static bool _foldDebug;

        // ── Package info ──
        private static string _packageVersion;
        private static string _packageUrl;

        // ── Cached styles ──
        private GUIStyle _titleStyle;
        private GUIStyle _versionStyle;
        private GUIStyle _pillText;
        private GUIStyle _mutedMini;
        private GUIStyle _monoArea;

        private void OnEnable()
        {
            _logLevel = serializedObject.FindProperty("_logLevel");
            _mockConfig = serializedObject.FindProperty("_mockConfig");
            _useFakeForTesting = serializedObject.FindProperty("_useFakeForTesting");
            _fakeReferrerUrl = serializedObject.FindProperty("_fakeReferrerUrl");

            if (_packageVersion == null)
            {
                var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                    typeof(InstallReferrerController).Assembly);
                _packageVersion = packageInfo != null ? $"v{packageInfo.version}" : "v?";
                string repoUrl = packageInfo?.repository?.url;
                if (!string.IsNullOrEmpty(repoUrl) && repoUrl.EndsWith(".git"))
                    repoUrl = repoUrl[..^4];
                _packageUrl = repoUrl ?? "https://github.com/BizSim-Game-Studios/com.bizsim.google.play.installreferrer";
            }
        }

        /// <summary>Lazily initializes all cached <see cref="GUIStyle"/> instances.</summary>
        private void EnsureStyles()
        {
            if (_titleStyle != null) return;

            _titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(0, 0, 0, 0)
            };

            _versionStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 9,
                normal = { textColor = new Color(1f, 1f, 1f, 0.6f) },
                padding = new RectOffset(5, 5, 1, 1)
            };

            _pillText = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 9,
                padding = new RectOffset(6, 6, 1, 1)
            };

            _mutedMini = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = Muted },
                fontStyle = FontStyle.Italic
            };

            _monoArea = new GUIStyle(EditorStyles.textArea)
            {
                font = Font.CreateDynamicFontFromOSFont("Consolas", 11),
                fontSize = 11,
                wordWrap = true
            };
        }

        public override void OnInspectorGUI()
        {
            EnsureStyles();
            serializedObject.Update();

            var ctrl = (InstallReferrerController)target;
            var data = ctrl.CachedData;

            // ── Header ──
            DrawHeader(ctrl, data);

            EditorGUILayout.Space(CardSpacing);

            // ── Setup card ──
            DrawSetupCard();

            // ── Simulation card ──
            DrawSimulationCard();

            serializedObject.ApplyModifiedProperties();

            // ── Fetch button ──
            DrawFetchCard(ctrl);

            // ── Results card ──
            DrawResultsCard(ctrl, data);

            // ── Debug card ──
            DrawDebugCard(data);

            if (Application.isPlaying)
                Repaint();
        }

        // ══════════════════════════════════════════════════════════════════
        // Header
        // ══════════════════════════════════════════════════════════════════

        private void DrawHeader(InstallReferrerController ctrl, CachedReferrerData data)
        {
            EditorGUILayout.Space(4);

            var titleRect = EditorGUILayout.GetControlRect(false, 22);
            EditorGUI.LabelField(titleRect, "Install Referrer", _titleStyle);

            // Version label (right-aligned)
            float vw = 36f;
            var vRect = new Rect(titleRect.xMax - vw, titleRect.y + 2, vw, 16);
            var prevC = GUI.color;
            GUI.color = new Color(1, 1, 1, 0.3f);
            EditorGUIUtility.AddCursorRect(vRect, MouseCursor.Link);
            if (GUI.Button(vRect, _packageVersion, _versionStyle))
                Application.OpenURL(_packageUrl);
            GUI.color = prevC;

            // Status pill
            string statusLabel;
            Color statusColor;
            if (ctrl.IsFetching) { statusLabel = "Fetching"; statusColor = Accent; }
            else if (data == null) { statusLabel = "Idle"; statusColor = Muted; }
            else if (data.HasReferrer) { statusLabel = "Attributed"; statusColor = Green; }
            else { statusLabel = "Organic"; statusColor = Muted; }

            float sw = _pillText.CalcSize(new GUIContent(statusLabel)).x + 14;
            var sRect = new Rect(vRect.x - sw - 4, titleRect.y + 3, sw, 16);
            DrawPill(sRect, statusLabel, statusColor);

            // Accent line
            var lineRect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(lineRect, new Color(Accent.r, Accent.g, Accent.b, 0.4f));

            // Last fetch metadata
            if (data != null && !string.IsNullOrEmpty(data.FetchTimestamp))
            {
                string ts = data.FetchTimestamp;
                int tIdx = ts.IndexOf('T');
                if (tIdx >= 0 && ts.Length > tIdx + 9)
                    ts = ts.Substring(tIdx + 1, 8);

                var metaRect = EditorGUILayout.GetControlRect(false, 14);
                EditorGUI.LabelField(metaRect, $"Last fetch: {ts}  •  SDK: {data.SdkVersion}", _mutedMini);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // Setup Card
        // ══════════════════════════════════════════════════════════════════

        private void DrawSetupCard()
        {
            _foldSetup = BeginCard("Setup", _foldSetup);
            if (_foldSetup)
            {
                EditorGUILayout.PropertyField(_logLevel, new GUIContent("Log Level"));

                EditorGUILayout.Space(4);

                // Mock Config
                if (_mockConfig != null)
                {
                    EditorGUILayout.PropertyField(_mockConfig,
                        new GUIContent("Mock Config", "Editor-only mock responses for Play Mode testing."));
                }

                EndCardContent();
            }
            EndCard();
        }

        // ══════════════════════════════════════════════════════════════════
        // Simulation Card
        // ══════════════════════════════════════════════════════════════════

        private void DrawSimulationCard()
        {
            _foldSim = BeginCard("Simulation", _foldSim);
            if (_foldSim)
            {
                EditorGUILayout.PropertyField(_useFakeForTesting,
                    new GUIContent("Enable Simulation",
                        "Uses a fake referrer string on-device (debug builds only)."));

                if (_useFakeForTesting.boolValue)
                {
                    EditorGUILayout.Space(4);
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(_fakeReferrerUrl,
                        new GUIContent("Referrer URL", "Fake referrer URL to send through the bridge."));

                    // UTM preview
                    string url = _fakeReferrerUrl.stringValue ?? "";
                    if (!string.IsNullOrEmpty(url))
                    {
                        InstallReferrerUtility.ParseUtmParameters(url,
                            out string src, out string med, out string cam,
                            out string con, out string trm);

                        EditorGUILayout.Space(2);
                        DrawNote($"utm_source={src}  utm_medium={med}  utm_campaign={cam}");
                    }

                    EditorGUI.indentLevel--;
                }
                else
                {
                    DrawNote("Debug builds only. Enable to simulate referrer data on-device.");
                }

                EndCardContent();
            }
            EndCard();
        }

        // ══════════════════════════════════════════════════════════════════
        // Fetch Button Card
        // ══════════════════════════════════════════════════════════════════

        private void DrawFetchCard(InstallReferrerController ctrl)
        {
            EditorGUILayout.Space(CardSpacing);
            var outer = EditorGUILayout.BeginVertical();
            DrawCardBackground(outer);

            EditorGUILayout.Space(CardPad - 4);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(CardPad);

            bool canRun = Application.isPlaying && !ctrl.IsFetching;
            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = canRun ? Accent : new Color(0.35f, 0.35f, 0.35f);

            var btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12,
                fixedHeight = 28,
                padding = new RectOffset(16, 16, 4, 4),
                normal = { textColor = Color.white },
                hover = { textColor = Color.white },
                active = { textColor = Color.white }
            };

            using (new EditorGUI.DisabledGroupScope(!canRun))
            {
                if (GUILayout.Button("▶  Fetch Referrer", btnStyle, GUILayout.Width(160)))
                    ctrl.FetchInstallReferrer();
            }
            GUI.backgroundColor = prevBg;

            GUILayout.Space(8);
            if (!Application.isPlaying)
            {
                var infoRect = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
                EditorGUI.LabelField(infoRect, "Enter Play Mode to fetch.", _mutedMini);
            }
            else if (ctrl.IsFetching)
            {
                var infoRect = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
                EditorGUI.LabelField(infoRect, "Fetching…",
                    new GUIStyle(_mutedMini) { normal = { textColor = Accent } });
            }

            GUILayout.Space(CardPad);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(CardPad - 4);
            EditorGUILayout.EndVertical();
        }

        // ══════════════════════════════════════════════════════════════════
        // Results Card
        // ══════════════════════════════════════════════════════════════════

        private void DrawResultsCard(InstallReferrerController ctrl, CachedReferrerData data)
        {
            EditorGUILayout.Space(CardSpacing);
            _foldResults = BeginCard("Results", _foldResults);
            if (_foldResults)
            {
                if (data == null)
                {
                    DrawNote("No results yet. Fetch referrer data to populate.");
                    EndCardContent();
                    EndCard();
                    return;
                }

                // Referrer URL
                DrawRow("Referrer", string.IsNullOrEmpty(data.InstallReferrer) ? "(empty / organic)" : data.InstallReferrer);
                DrawRow("Organic", data.IsOrganic.ToString());

                EditorGUILayout.Space(4);
                DrawSeparator();
                EditorGUILayout.Space(4);

                // UTM parameters
                DrawSectionLabel("UTM Parameters");
                DrawRow("Source", data.UtmSource);
                DrawRow("Medium", data.UtmMedium);
                DrawRow("Campaign", data.UtmCampaign);
                DrawRow("Content", data.UtmContent);
                DrawRow("Term", data.UtmTerm);

                EditorGUILayout.Space(4);
                DrawSeparator();
                EditorGUILayout.Space(4);

                // Timestamps
                DrawSectionLabel("Timestamps");
                DrawRow("Click (client)", data.ReferrerClickTimestampSeconds.ToString());
                DrawRow("Install (client)", data.InstallBeginTimestampSeconds.ToString());
                DrawRow("Click (server)", data.ReferrerClickTimestampServerSeconds.ToString());
                DrawRow("Install (server)", data.InstallBeginTimestampServerSeconds.ToString());
                DrawRow("Instant?", data.GooglePlayInstantParam.ToString());

                EndCardContent();
            }
            EndCard();
        }

        // ══════════════════════════════════════════════════════════════════
        // Debug Card
        // ══════════════════════════════════════════════════════════════════

        private void DrawDebugCard(CachedReferrerData data)
        {
            EditorGUILayout.Space(CardSpacing);
            _foldDebug = BeginCard("Debug", _foldDebug);
            if (_foldDebug)
            {
                if (data != null)
                {
                    string json = JsonUtility.ToJson(data, true);

                    using (new EditorGUI.DisabledGroupScope(true))
                    {
                        EditorGUILayout.TextArea(json, _monoArea, GUILayout.MinHeight(100));
                    }

                    EditorGUILayout.Space(4);

                    EditorGUILayout.BeginHorizontal();

                    if (GUILayout.Button("Copy JSON", EditorStyles.miniButton, GUILayout.Width(80)))
                    {
                        EditorGUIUtility.systemCopyBuffer = json;
                        Debug.Log("[InstallReferrer] JSON copied to clipboard.");
                    }

                    if (GUILayout.Button("Save to file…", EditorStyles.miniButton, GUILayout.Width(90)))
                    {
                        string path = EditorUtility.SaveFilePanel("Save Referrer JSON", "", "install_referrer_data", "json");
                        if (!string.IsNullOrEmpty(path))
                        {
                            System.IO.File.WriteAllText(path, json);
                            Debug.Log($"[InstallReferrer] Data saved to {path}");
                        }
                    }

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Clear cache", EditorStyles.miniButton, GUILayout.Width(80)))
                    {
                        var ctrl = (InstallReferrerController)target;
                        ctrl.ClearCachedData();
                        Debug.Log("[InstallReferrer] Cache cleared.");
                    }

                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    DrawNote("No data available. Fetch referrer data first.");
                }

                EndCardContent();
            }
            EndCard();
        }

        // ══════════════════════════════════════════════════════════════════
        // Card primitives
        // ══════════════════════════════════════════════════════════════════

        private bool BeginCard(string title, bool foldout)
        {
            EditorGUILayout.Space(CardSpacing);
            var outer = EditorGUILayout.BeginVertical();
            DrawCardBackground(outer);

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(CardPad - 6);

            foldout = EditorGUILayout.Foldout(foldout, "  " + title, true, new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 11,
                fixedHeight = 22
            });

            EditorGUILayout.EndHorizontal();

            if (foldout)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(CardPad);
                EditorGUILayout.BeginVertical();
            }

            return foldout;
        }

        private static void EndCardContent()
        {
            EditorGUILayout.EndVertical();
            GUILayout.Space(CardPad);
            EditorGUILayout.EndHorizontal();
        }

        private static void EndCard()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.EndVertical();
        }

        private static void DrawCardBackground(Rect rect)
        {
            if (Event.current.type != EventType.Repaint) return;
            EditorGUI.DrawRect(rect, CardBg);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), CardBorder);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), CardBorder);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1, rect.height), CardBorder);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.y, 1, rect.height), CardBorder);
        }

        private void DrawPill(Rect rect, string text, Color color)
        {
            EditorGUI.DrawRect(rect, new Color(color.r, color.g, color.b, 0.15f));
            _pillText.normal.textColor = color;
            EditorGUI.LabelField(rect, text, _pillText);
        }

        private void DrawNote(string text)
        {
            var rect = EditorGUILayout.GetControlRect(false, 16);
            EditorGUI.LabelField(rect, text, _mutedMini);
        }

        private static void DrawSeparator()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, SepColor);
        }

        private static void DrawSectionLabel(string text)
        {
            var rect = EditorGUILayout.GetControlRect(false, 16);
            EditorGUI.LabelField(rect, text.ToUpper(), new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 9,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
            });
        }

        private static void DrawRow(string label, string value)
        {
            var rect = EditorGUILayout.GetControlRect(false, 18);
            float labelW = 120;
            EditorGUI.LabelField(new Rect(rect.x, rect.y, labelW, rect.height), label,
                new GUIStyle(EditorStyles.label) { fontSize = 11, normal = { textColor = new Color(0.6f, 0.6f, 0.6f) } });
            EditorGUI.LabelField(new Rect(rect.x + labelW, rect.y, rect.width - labelW, rect.height),
                string.IsNullOrEmpty(value) ? "—" : value,
                new GUIStyle(EditorStyles.label) { fontSize = 11 });
        }
    }
}
#endif
