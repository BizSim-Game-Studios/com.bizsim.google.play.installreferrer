// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

#if UNITY_EDITOR && UNITY_ANDROID
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BizSim.Google.Play.InstallReferrer.Editor
{
    /// <summary>
    /// Pre-build validator that checks for duplicate Install Referrer AAR files.
    /// Runs before every Android build to prevent "duplicate class" errors at runtime.
    ///
    /// <b>What it checks:</b>
    /// <list type="bullet">
    /// <item>Multiple AAR files containing <c>com.android.installreferrer</c> classes</item>
    /// <item>Conflicting versions of the Install Referrer library</item>
    /// </list>
    /// </summary>
    public class InstallReferrerBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android)
                return;

            CheckEdm4uPresence();
            CheckDuplicateAars();
        }

        private static void CheckEdm4uPresence()
        {
            // EDM4U's PlayServicesResolver lives in Google.JarResolver.dll.
            // Try both possible assembly names for compatibility across EDM4U versions.
            var edm4uType = System.Type.GetType(
                "GooglePlayServices.PlayServicesResolver, Google.JarResolver", false);

            if (edm4uType == null)
            {
                const string message =
                    "EDM4U (External Dependency Manager for Unity) is not installed.\n\n" +
                    "This package uses Editor/Dependencies.xml to resolve " +
                    "'com.android.installreferrer:installreferrer:2.2'.\n\n" +
                    "Without EDM4U, the native library will be missing and " +
                    "you'll get ClassNotFoundException at runtime.";

                Debug.LogWarning("[InstallReferrer Build Validator] " + message);

                bool openPage = EditorUtility.DisplayDialog(
                    "Install Referrer — Missing EDM4U",
                    message,
                    "Download EDM4U",
                    "Ignore");

                if (openPage)
                {
                    Application.OpenURL("https://github.com/googlesamples/unity-jar-resolver");
                }
            }
        }

        private static void CheckDuplicateAars()
        {
            // Search for any AAR files that might contain the Install Referrer classes
            var allAars = Directory.GetFiles(Application.dataPath, "*.aar", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(Path.Combine(Application.dataPath, "..", "Packages"), "*.aar", SearchOption.AllDirectories))
                .Where(path => path.Replace('\\', '/').ToLower().Contains("installreferrer"))
                .ToList();

            if (allAars.Count > 1)
            {
                string files = string.Join("\n  • ", allAars.Select(p => p.Replace('\\', '/')));
                Debug.LogWarning(
                    $"[InstallReferrer Build Validator] Found {allAars.Count} potential Install Referrer AAR files:\n  • {files}\n" +
                    "This may cause 'duplicate class' errors at build time. " +
                    "If you're using EDM4U (Dependencies.xml), remove any manually added AAR files.");
            }

            // Also check for the deprecated com.android.installreferrer JAR
            var jars = Directory.GetFiles(Application.dataPath, "*.jar", SearchOption.AllDirectories)
                .Where(path => path.Replace('\\', '/').ToLower().Contains("installreferrer"))
                .ToList();

            if (jars.Count > 0)
            {
                string files = string.Join("\n  • ", jars.Select(p => p.Replace('\\', '/')));
                Debug.LogWarning(
                    $"[InstallReferrer Build Validator] Found deprecated Install Referrer JAR files:\n  • {files}\n" +
                    "The Install Referrer library is now resolved via EDM4U (Dependencies.xml). " +
                    "Remove these JAR files to avoid conflicts.");
            }
        }
    }
}
#endif
