using NUnit.Framework;
using UnityEngine;
using BizSim.Google.Play.InstallReferrer;

namespace BizSim.Google.Play.InstallReferrer.Tests
{
    /// <summary>
    /// Wave 2 Forget API drift guard. Verifies that
    /// <c>EncryptedPlayerPrefsCacheProvider.EraseAll()</c> — the engine behind
    /// <c>InstallReferrerController.ForgetAll()</c> — wipes both the encrypted
    /// payload AND the per-install encryption key identifier, restoring the
    /// package to a fresh-install state for GDPR Article 17 compliance.
    /// </summary>
    /// <remarks>
    /// Tests the provider directly because <c>InstallReferrerController.ForgetAll</c>
    /// requires a live <c>MonoBehaviour</c> instance. Provider-level tests
    /// exercise the actual erasure logic and run on every CI pass.
    /// </remarks>
    [TestFixture]
    public class ForgetApiTest
    {
        // Must match private const in EncryptedPlayerPrefsCacheProvider.
        private const string PayloadKey = "InstallReferrer_Cache_Enc";
        private const string KeyIdKey   = "InstallReferrer_KeyId";
        // Match public const in InstallReferrerController + private const in Controller.
        private const string LegacyCacheKey = "InstallReferrer_Cache";
        private const string ConsentKey     = "BizSim.InstallReferrer.ConsentGranted";

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(PayloadKey);
            PlayerPrefs.DeleteKey(KeyIdKey);
            PlayerPrefs.DeleteKey(LegacyCacheKey);
            PlayerPrefs.DeleteKey(ConsentKey);
            PlayerPrefs.Save();
        }

        [Test]
        public void EraseAll_DeletesBothPayloadAndKeyId()
        {
            PlayerPrefs.SetString(PayloadKey, "encrypted-blob");
            PlayerPrefs.SetString(KeyIdKey, "per-install-guid");
            PlayerPrefs.Save();

            var provider = new EncryptedPlayerPrefsCacheProvider();
            provider.EraseAll();

            Assert.IsFalse(PlayerPrefs.HasKey(PayloadKey),
                "EraseAll must delete the encrypted payload.");
            Assert.IsFalse(PlayerPrefs.HasKey(KeyIdKey),
                "EraseAll must also delete the per-install key identifier.");
        }

        [Test]
        public void Clear_PreservesKeyIdButDeletesPayload()
        {
            PlayerPrefs.SetString(PayloadKey, "encrypted-blob");
            PlayerPrefs.SetString(KeyIdKey, "per-install-guid");
            PlayerPrefs.Save();

            var provider = new EncryptedPlayerPrefsCacheProvider();
            provider.Clear();

            Assert.IsFalse(PlayerPrefs.HasKey(PayloadKey),
                "Clear must delete the encrypted payload.");
            Assert.IsTrue(PlayerPrefs.HasKey(KeyIdKey),
                "Clear must preserve the per-install key identifier " +
                "(distinct semantics from EraseAll — see ForgetAll vs ClearCachedData).");
        }

        [Test]
        public void EraseAll_IsIdempotent_NoExceptionOnRepeat()
        {
            var provider = new EncryptedPlayerPrefsCacheProvider();
            Assert.DoesNotThrow(() => provider.EraseAll(), "First EraseAll on empty prefs must not throw.");
            Assert.DoesNotThrow(() => provider.EraseAll(), "Second EraseAll (idempotent) must not throw.");
        }

        [Test]
        public void EraseAll_AfterSaveLoad_RoundtripGeneratesFreshKey()
        {
            var provider = new EncryptedPlayerPrefsCacheProvider();
            // Content of the cached data is irrelevant to this test — we just need
            // any Save() call to trigger GetOrCreateKeyId() once.
            var data = new CachedReferrerData
            {
                InstallReferrer = "utm_source=test",
                SdkVersion = PackageVersion.Current,
            };
            provider.Save(data);
            string firstKeyId = PlayerPrefs.GetString(KeyIdKey, "");
            Assert.IsNotEmpty(firstKeyId, "Save must create a key id on first run.");

            provider.EraseAll();
            var freshProvider = new EncryptedPlayerPrefsCacheProvider();
            freshProvider.Save(data);

            string secondKeyId = PlayerPrefs.GetString(KeyIdKey, "");
            Assert.IsNotEmpty(secondKeyId, "Save after EraseAll must generate a fresh key id.");
            Assert.AreNotEqual(firstKeyId, secondKeyId,
                "The key id after EraseAll must differ from the previous one — otherwise " +
                "GDPR-erased data would remain decryptable by the old key.");
        }
    }
}
