// Copyright (c) BizSim Game Studios. All rights reserved.
// Tests for InstallReferrerData models.

using NUnit.Framework;

namespace BizSim.Google.Play.InstallReferrer.Tests
{
    /// <summary>
    /// Unit tests for <see cref="InstallReferrerResult"/> properties.
    /// </summary>
    [TestFixture]
    public class InstallReferrerResultTests
    {
        [Test]
        public void DefaultResult_HasEmptyReferrer()
        {
            var result = new InstallReferrerResult();
            Assert.AreEqual("", result.installReferrer);
            Assert.AreEqual(0, result.referrerClickTimestampSeconds);
            Assert.IsFalse(result.googlePlayInstantParam);
        }

        [Test]
        public void Result_WithReferrer_HasNonEmptyString()
        {
            var result = new InstallReferrerResult
            {
                installReferrer = "utm_source=google"
            };
            Assert.AreEqual("utm_source=google", result.installReferrer);
        }
    }

    /// <summary>
    /// Unit tests for <see cref="CachedReferrerData"/> properties and convenience methods.
    /// </summary>
    [TestFixture]
    public class CachedReferrerDataTests
    {
        [Test]
        public void EmptyReferrer_IsOrganic_ReturnsTrue()
        {
            var data = new CachedReferrerData { InstallReferrer = "" };
            Assert.IsTrue(data.IsOrganic);
            Assert.IsFalse(data.HasReferrer);
        }

        [Test]
        public void NonEmptyReferrer_HasReferrer_ReturnsTrue()
        {
            var data = new CachedReferrerData
            {
                InstallReferrer = "utm_source=google&utm_medium=cpc"
            };
            Assert.IsTrue(data.HasReferrer);
        }

        [Test]
        public void ReferrerWithUtmSource_HasUtmSource_ReturnsTrue()
        {
            var data = new CachedReferrerData
            {
                InstallReferrer = "utm_source=google",
                UtmSource = "google"
            };
            Assert.IsTrue(data.HasUtmSource);
            Assert.IsFalse(data.IsOrganic);
        }

        [Test]
        public void ReferrerWithoutUtmSource_IsOrganic_ReturnsTrue()
        {
            var data = new CachedReferrerData
            {
                InstallReferrer = "some_param=value",
                UtmSource = ""
            };
            Assert.IsTrue(data.IsOrganic);
        }

        [Test]
        public void ReferrerWithOrganicMedium_IsOrganic_ReturnsTrue()
        {
            var data = new CachedReferrerData
            {
                InstallReferrer = "utm_source=google-play&utm_medium=organic",
                UtmSource = "google-play",
                UtmMedium = "organic"
            };
            Assert.IsTrue(data.HasReferrer);
            Assert.IsTrue(data.HasUtmSource);
            Assert.IsTrue(data.IsOrganic, "utm_medium=organic should be detected as organic install");
        }

        [Test]
        public void ReferrerWithOrganicMedium_CaseInsensitive_IsOrganic_ReturnsTrue()
        {
            var data = new CachedReferrerData
            {
                InstallReferrer = "utm_source=google-play&utm_medium=Organic",
                UtmSource = "google-play",
                UtmMedium = "Organic"
            };
            Assert.IsTrue(data.IsOrganic, "Organic detection should be case-insensitive");
        }

        [Test]
        public void ReferrerWithPaidMedium_IsOrganic_ReturnsFalse()
        {
            var data = new CachedReferrerData
            {
                InstallReferrer = "utm_source=google&utm_medium=cpc",
                UtmSource = "google",
                UtmMedium = "cpc"
            };
            Assert.IsFalse(data.IsOrganic, "Paid campaign (cpc) should not be organic");
        }

        [Test]
        public void DefaultData_AllFieldsEmpty()
        {
            var data = new CachedReferrerData();
            Assert.AreEqual("", data.InstallReferrer);
            Assert.AreEqual("", data.UtmSource);
            Assert.AreEqual("", data.UtmMedium);
            Assert.AreEqual("", data.UtmCampaign);
            Assert.AreEqual("", data.UtmContent);
            Assert.AreEqual("", data.UtmTerm);
            Assert.AreEqual("", data.SdkVersion);
            Assert.AreEqual(0, data.AppInstallTimeMs);
        }

        [Test]
        public void CachedData_WithTimestamps_PreservesValues()
        {
            var data = new CachedReferrerData
            {
                ReferrerClickTimestampSeconds = 1738200000,
                InstallBeginTimestampSeconds = 1738200060,
                ReferrerClickTimestampServerSeconds = 1738200001,
                InstallBeginTimestampServerSeconds = 1738200061,
                GooglePlayInstantParam = true
            };

            Assert.AreEqual(1738200000, data.ReferrerClickTimestampSeconds);
            Assert.AreEqual(1738200060, data.InstallBeginTimestampSeconds);
            Assert.AreEqual(1738200001, data.ReferrerClickTimestampServerSeconds);
            Assert.AreEqual(1738200061, data.InstallBeginTimestampServerSeconds);
            Assert.IsTrue(data.GooglePlayInstantParam);
        }
    }

    /// <summary>
    /// Unit tests for <see cref="InstallReferrerError"/> properties.
    /// </summary>
    [TestFixture]
    public class InstallReferrerErrorTests
    {
        [TestCase(0, "OK")]
        [TestCase(1, "FEATURE_NOT_SUPPORTED")]
        [TestCase(2, "SERVICE_UNAVAILABLE")]
        [TestCase(3, "DEVELOPER_ERROR")]
        [TestCase(-1, "SERVICE_DISCONNECTED")]
        [TestCase(-100, "INTERNAL_ERROR")]
        [TestCase(-101, "ALREADY_CONNECTING")]
        [TestCase(999, "UNKNOWN_999")]
        public void ErrorCodeName_MapsCorrectly(int code, string expectedName)
        {
            var error = new InstallReferrerError { errorCode = code };
            Assert.AreEqual(expectedName, error.ErrorCodeName);
        }

        [TestCase(0, InstallReferrerErrorCode.Ok)]
        [TestCase(1, InstallReferrerErrorCode.FeatureNotSupported)]
        [TestCase(2, InstallReferrerErrorCode.ServiceUnavailable)]
        [TestCase(-1, InstallReferrerErrorCode.ServiceDisconnected)]
        [TestCase(-100, InstallReferrerErrorCode.InternalError)]
        public void ErrorCodeEnum_MapsCorrectly(int code, InstallReferrerErrorCode expected)
        {
            var error = new InstallReferrerError { errorCode = code };
            Assert.AreEqual(expected, error.ErrorCodeEnum);
        }
    }

    /// <summary>
    /// Unit tests for <see cref="InstallReferrerException"/>.
    /// </summary>
    [TestFixture]
    public class InstallReferrerExceptionTests
    {
        [Test]
        public void Exception_ContainsError()
        {
            var error = new InstallReferrerError
            {
                errorCode = 2,
                errorMessage = "Service unavailable",
                isRetryable = true
            };
            var ex = new InstallReferrerException(error);

            Assert.AreEqual(error, ex.Error);
            Assert.That(ex.Message, Does.Contain("SERVICE_UNAVAILABLE"));
        }
    }
}
