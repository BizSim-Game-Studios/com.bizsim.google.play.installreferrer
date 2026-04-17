using NUnit.Framework;
using UnityEngine;
using BizSim.Google.Play.InstallReferrer;

namespace BizSim.Google.Play.InstallReferrer.Tests
{
    /// <summary>
    /// Drift guard for GDPR right-to-erasure (C2.2 compliance) per
    /// development-plans/plans/2026-04-17-installreferrer-consent-persist/README.md
    /// (Wave 0 hotfix, v1.0.3 PATCH).
    ///
    /// Asserts that <see cref="InstallReferrerController.SetConsentGranted"/>
    /// persists the consent flag to PlayerPrefs (key
    /// <c>BizSim.InstallReferrer.ConsentGranted</c>) so revocation survives
    /// app restart, domain reload, and device reboot.
    ///
    /// Prior to v1.0.3, the flag was in-memory only and silently reset to
    /// <c>true</c> on every boot — a GDPR compliance gap that this test
    /// prevents from regressing.
    /// </summary>
    public class ConsentPersistenceTest
    {
        private const string PrefsKey = "BizSim.InstallReferrer.ConsentGranted";

        [SetUp]
        public void Setup()
        {
            PlayerPrefs.DeleteKey(PrefsKey);
            PlayerPrefs.Save();
        }

        [TearDown]
        public void Cleanup()
        {
            PlayerPrefs.DeleteKey(PrefsKey);
            PlayerPrefs.Save();
        }

        [Test]
        public void SetConsentGranted_False_WritesPlayerPrefs()
        {
            // Directly verify PlayerPrefs write by invoking SetConsentGranted on
            // the singleton. The controller's Instance is auto-created on first
            // access (MonoBehaviour singleton pattern).
            var controller = InstallReferrerController.Instance;
            controller.SetConsentGranted(false);

            Assert.AreEqual(0, PlayerPrefs.GetInt(PrefsKey, -1),
                "SetConsentGranted(false) must persist '0' to PlayerPrefs key '" + PrefsKey + "'.");
        }

        [Test]
        public void SetConsentGranted_True_WritesPlayerPrefs()
        {
            var controller = InstallReferrerController.Instance;
            controller.SetConsentGranted(true);

            Assert.AreEqual(1, PlayerPrefs.GetInt(PrefsKey, -1),
                "SetConsentGranted(true) must persist '1' to PlayerPrefs key '" + PrefsKey + "'.");
        }

        [Test]
        public void DefaultValue_WhenNoPrefExists_IsTrue()
        {
            // Fresh controller with no pref → ConsentGranted should default true
            // (backward compat with v1.0.2-and-earlier consumers).
            Assert.IsFalse(PlayerPrefs.HasKey(PrefsKey));
            var controller = InstallReferrerController.Instance;
            Assert.IsTrue(controller.ConsentGranted,
                "Fresh install without pref must default to ConsentGranted=true (backward compat).");
        }

        [Test]
        public void PrefsKey_IsNamespaced()
        {
            // Ensure the key does not collide with consumer PlayerPrefs.
            StringAssert.StartsWith("BizSim.InstallReferrer.", PrefsKey,
                "PlayerPrefs key must use BizSim.InstallReferrer. namespace prefix.");
        }
    }
}
