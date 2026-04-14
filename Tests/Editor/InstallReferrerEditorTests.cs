// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)

using NUnit.Framework;

namespace BizSim.Google.Play.InstallReferrer.EditorTests
{
    /// <summary>
    /// Editor-mode tests for Install Referrer package.
    /// </summary>
    public class InstallReferrerEditorTests
    {
        [Test]
        public void InstallReferrerErrorCode_FeatureNotSupported_HasCorrectValue()
        {
            Assert.AreEqual(1, (int)InstallReferrerErrorCode.FeatureNotSupported);
        }

        [Test]
        public void InstallReferrerErrorCode_ServiceUnavailable_HasCorrectValue()
        {
            Assert.AreEqual(2, (int)InstallReferrerErrorCode.ServiceUnavailable);
        }

        [Test]
        public void InstallReferrerErrorCode_ServiceDisconnected_HasCorrectValue()
        {
            Assert.AreEqual(-1, (int)InstallReferrerErrorCode.ServiceDisconnected);
        }

        [Test]
        public void CacheInvalidationReason_AllValuesExist()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(CacheInvalidationReason), CacheInvalidationReason.AppReinstalled));
            Assert.IsTrue(System.Enum.IsDefined(typeof(CacheInvalidationReason), CacheInvalidationReason.SdkVersionChanged));
            Assert.IsTrue(System.Enum.IsDefined(typeof(CacheInvalidationReason), CacheInvalidationReason.DataCorrupted));
            Assert.IsTrue(System.Enum.IsDefined(typeof(CacheInvalidationReason), CacheInvalidationReason.ManualClear));
        }

        [Test]
        public void CachedReferrerData_HasReferrer_ReturnsFalse_WhenEmpty()
        {
            var data = new CachedReferrerData { InstallReferrer = "" };
            Assert.IsFalse(data.HasReferrer);
        }

        [Test]
        public void CachedReferrerData_HasReferrer_ReturnsTrue_WhenPopulated()
        {
            var data = new CachedReferrerData { InstallReferrer = "utm_source=test" };
            Assert.IsTrue(data.HasReferrer);
        }

        [Test]
        public void UtmParser_ParseQueryString_ReturnsEmpty_ForNull()
        {
            var result = InstallReferrerUtility.ParseQueryString(null);
            Assert.AreEqual(0, result.Count);
        }
    }
}
