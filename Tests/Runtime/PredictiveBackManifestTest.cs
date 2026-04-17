using System.IO;
using NUnit.Framework;

namespace BizSim.Google.Play.InstallReferrer.Tests
{
    /// <summary>
    /// C5.2 drift guard (Plan E). For installreferrer, expected value is
    /// "false" — this package has no UI surface (pure API). Opt-out declared
    /// explicitly.
    /// </summary>
    public class PredictiveBackManifestTest
    {
        private const string ManifestPath =
            "Packages/com.bizsim.google.play.installreferrer/Runtime/Plugins/Android/BizSimInstallReferrer.androidlib/AndroidManifest.xml";

        private const string FallbackPath =
            "Runtime/Plugins/Android/BizSimInstallReferrer.androidlib/AndroidManifest.xml";

        private static string ReadManifest()
        {
            if (File.Exists(ManifestPath)) return File.ReadAllText(ManifestPath);
            if (File.Exists(FallbackPath)) return File.ReadAllText(FallbackPath);
            Assert.Inconclusive("Manifest not found at " + ManifestPath + " or " + FallbackPath);
            return null;
        }

        [Test]
        public void Manifest_DeclaresPredictiveBackCallback_False()
        {
            var xml = ReadManifest();
            Assert.IsTrue(xml.Contains("enableOnBackInvokedCallback=\"false\""),
                "Per C5.2, installreferrer .androidlib manifest must declare " +
                "android:enableOnBackInvokedCallback=\"false\" (opt-out, no UI).");
        }
    }
}
