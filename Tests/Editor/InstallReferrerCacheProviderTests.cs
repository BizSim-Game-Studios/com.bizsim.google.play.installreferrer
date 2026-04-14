// Copyright (c) BizSim Game Studios. All rights reserved.
// Integration tests for cache providers. These tests use PlayerPrefs
// and MUST clean up after themselves to avoid polluting the Editor.

using NUnit.Framework;
using UnityEngine;

namespace BizSim.Google.Play.InstallReferrer.EditorTests
{
    /// <summary>
    /// Integration tests for <see cref="EncryptedPlayerPrefsCacheProvider"/>.
    /// Exercises the full Save → Load → Clear cycle using real PlayerPrefs.
    /// <para>
    /// <b>Important:</b> All tests clean up PlayerPrefs keys in <c>[TearDown]</c>
    /// to prevent cross-test contamination and Editor state pollution.
    /// </para>
    /// </summary>
    [TestFixture]
    public class EncryptedCacheProviderTests
    {
        // Use a unique salt to isolate test keys from production keys.
        private const string TestSalt = "InstallReferrer_UnitTest_Salt";

        private EncryptedPlayerPrefsCacheProvider _provider;

        [SetUp]
        public void SetUp()
        {
            _provider = new EncryptedPlayerPrefsCacheProvider(TestSalt);
        }

        [TearDown]
        public void TearDown()
        {
            // Always clean up regardless of test outcome.
            _provider.Clear();

            // Also delete the per-install key ID that the provider creates.
            PlayerPrefs.DeleteKey("InstallReferrer_Cache_Enc");
            PlayerPrefs.DeleteKey("InstallReferrer_KeyId");
            PlayerPrefs.Save();
        }

        [Test]
        public void Load_WhenEmpty_ReturnsNull()
        {
            var result = _provider.Load();
            Assert.IsNull(result);
        }

        [Test]
        public void SaveAndLoad_RoundTrip_PreservesData()
        {
            var original = new CachedReferrerData
            {
                InstallReferrer = "utm_source=google&utm_medium=cpc",
                UtmSource = "google",
                UtmMedium = "cpc",
                UtmCampaign = "summer",
                AppInstallTimeMs = 1738000000000,
                SdkVersion = "0.2.0",
                FetchTimestamp = "2026-01-30T12:00:00Z"
            };

            _provider.Save(original);
            var loaded = _provider.Load();

            Assert.IsNotNull(loaded);
            Assert.AreEqual(original.InstallReferrer, loaded.InstallReferrer);
            Assert.AreEqual(original.UtmSource, loaded.UtmSource);
            Assert.AreEqual(original.UtmMedium, loaded.UtmMedium);
            Assert.AreEqual(original.UtmCampaign, loaded.UtmCampaign);
            Assert.AreEqual(original.AppInstallTimeMs, loaded.AppInstallTimeMs);
            Assert.AreEqual(original.SdkVersion, loaded.SdkVersion);
            Assert.AreEqual(original.FetchTimestamp, loaded.FetchTimestamp);
        }

        [Test]
        public void Clear_RemovesCachedData()
        {
            var data = new CachedReferrerData { InstallReferrer = "test" };
            _provider.Save(data);

            _provider.Clear();

            Assert.IsNull(_provider.Load());
        }

        [Test]
        public void Save_EncryptsData_NotPlaintext()
        {
            var data = new CachedReferrerData
            {
                InstallReferrer = "utm_source=secret_campaign"
            };
            _provider.Save(data);

            // Read raw PlayerPrefs — should NOT contain plaintext referrer
            string raw = PlayerPrefs.GetString("InstallReferrer_Cache_Enc", "");
            Assert.IsFalse(string.IsNullOrEmpty(raw), "Encrypted data should exist in PlayerPrefs");
            Assert.IsFalse(raw.Contains("secret_campaign"),
                "Raw PlayerPrefs value must not contain plaintext referrer data");
        }

        [Test]
        public void Load_CorruptedData_ReturnsNull()
        {
            // Write garbage to the cache key
            PlayerPrefs.SetString("InstallReferrer_Cache_Enc", "not-valid-base64!@#$");
            PlayerPrefs.Save();

            var result = _provider.Load();
            Assert.IsNull(result, "Corrupted data should return null (not throw)");
        }
    }
}
