// Copyright (c) BizSim Game Studios. All rights reserved.
// Integration tests simulating JSON payloads from the Java bridge.

using NUnit.Framework;
using UnityEngine;

namespace BizSim.Google.Play.InstallReferrer.Tests
{
    /// <summary>
    /// Integration tests that verify JSON deserialization of payloads
    /// matching the exact format sent by <c>InstallReferrerBridge.java</c>.
    /// These tests catch case-sensitivity issues with <see cref="JsonUtility"/>.
    /// </summary>
    [TestFixture]
    public class InstallReferrerJsonTests
    {
        // =================================================================
        // Success JSON (from Java handleSuccess)
        // =================================================================

        [Test]
        public void SuccessJson_FullReferrer_DeserializesCorrectly()
        {
            const string json = @"{
                ""installReferrer"": ""utm_source=google&utm_medium=cpc&utm_campaign=summer"",
                ""referrerClickTimestampSeconds"": 1738200000,
                ""installBeginTimestampSeconds"": 1738200060,
                ""referrerClickTimestampServerSeconds"": 1738200001,
                ""installBeginTimestampServerSeconds"": 1738200061,
                ""installVersion"": ""1.0.84"",
                ""googlePlayInstantParam"": false
            }";

            var parsed = JsonUtility.FromJson<InstallReferrerResult>(json);

            Assert.AreEqual("utm_source=google&utm_medium=cpc&utm_campaign=summer", parsed.installReferrer);
            Assert.AreEqual(1738200000, parsed.referrerClickTimestampSeconds);
            Assert.AreEqual(1738200060, parsed.installBeginTimestampSeconds);
            Assert.AreEqual(1738200001, parsed.referrerClickTimestampServerSeconds);
            Assert.AreEqual(1738200061, parsed.installBeginTimestampServerSeconds);
            Assert.AreEqual("1.0.84", parsed.installVersion);
            Assert.IsFalse(parsed.googlePlayInstantParam);
        }

        [Test]
        public void SuccessJson_EmptyReferrer_OrganicInstall()
        {
            const string json = @"{
                ""installReferrer"": """",
                ""referrerClickTimestampSeconds"": 0,
                ""installBeginTimestampSeconds"": 0,
                ""referrerClickTimestampServerSeconds"": 0,
                ""installBeginTimestampServerSeconds"": 0,
                ""installVersion"": """",
                ""googlePlayInstantParam"": false
            }";

            var parsed = JsonUtility.FromJson<InstallReferrerResult>(json);
            Assert.AreEqual("", parsed.installReferrer);
            Assert.AreEqual(0, parsed.referrerClickTimestampSeconds);
        }

        [Test]
        public void SuccessJson_GooglePlayInstant_True()
        {
            const string json = @"{
                ""installReferrer"": ""utm_source=google_play_instant"",
                ""referrerClickTimestampSeconds"": 0,
                ""installBeginTimestampSeconds"": 0,
                ""referrerClickTimestampServerSeconds"": 0,
                ""installBeginTimestampServerSeconds"": 0,
                ""installVersion"": """",
                ""googlePlayInstantParam"": true
            }";

            var parsed = JsonUtility.FromJson<InstallReferrerResult>(json);
            Assert.IsTrue(parsed.googlePlayInstantParam);
        }

        // =================================================================
        // Error JSON (from Java sendError)
        // =================================================================

        [Test]
        public void ErrorJson_ServiceUnavailable_DeserializesCorrectly()
        {
            const string json = @"{
                ""errorCode"": 2,
                ""errorMessage"": ""Install Referrer service unavailable"",
                ""isRetryable"": true
            }";

            var error = JsonUtility.FromJson<InstallReferrerError>(json);

            Assert.AreEqual(2, error.errorCode);
            Assert.AreEqual("Install Referrer service unavailable", error.errorMessage);
            Assert.IsTrue(error.isRetryable);
            Assert.AreEqual("SERVICE_UNAVAILABLE", error.ErrorCodeName);
        }

        [Test]
        public void ErrorJson_FeatureNotSupported_NotRetryable()
        {
            const string json = @"{
                ""errorCode"": 1,
                ""errorMessage"": ""Not supported"",
                ""isRetryable"": false
            }";

            var error = JsonUtility.FromJson<InstallReferrerError>(json);

            Assert.AreEqual(1, error.errorCode);
            Assert.IsFalse(error.isRetryable);
            Assert.AreEqual("FEATURE_NOT_SUPPORTED", error.ErrorCodeName);
        }

        [Test]
        public void ErrorJson_InternalError_FieldsCaseSensitive()
        {
            const string json = @"{
                ""errorCode"": -100,
                ""errorMessage"": ""Connection start failed"",
                ""isRetryable"": false
            }";

            var error = JsonUtility.FromJson<InstallReferrerError>(json);

            Assert.AreEqual(-100, error.errorCode,
                "errorCode must be camelCase — JsonUtility is case-sensitive");
            Assert.AreEqual("Connection start failed", error.errorMessage,
                "errorMessage must be camelCase — JsonUtility is case-sensitive");
            Assert.IsFalse(error.isRetryable,
                "isRetryable must be camelCase — JsonUtility is case-sensitive");
        }

        [TestCase(2, true)]   // SERVICE_UNAVAILABLE — retryable
        [TestCase(-1, true)]  // SERVICE_DISCONNECTED — retryable
        [TestCase(1, false)]  // FEATURE_NOT_SUPPORTED — not retryable
        [TestCase(3, false)]  // DEVELOPER_ERROR — not retryable
        [TestCase(-100, false)] // INTERNAL_ERROR — not retryable
        public void ErrorJson_RetryableRange_MatchesJavaBridge(int code, bool expectedRetryable)
        {
            string json = $@"{{
                ""errorCode"": {code},
                ""errorMessage"": ""test"",
                ""isRetryable"": {expectedRetryable.ToString().ToLower()}
            }}";

            var error = JsonUtility.FromJson<InstallReferrerError>(json);
            Assert.AreEqual(expectedRetryable, error.isRetryable);
        }

        // =================================================================
        // CachedReferrerData Round-trip
        // =================================================================

        [Test]
        public void CachedData_SerializeDeserialize_RoundTrip()
        {
            var original = new CachedReferrerData
            {
                InstallReferrer = "utm_source=google&utm_medium=cpc",
                ReferrerClickTimestampSeconds = 1738200000,
                InstallBeginTimestampSeconds = 1738200060,
                ReferrerClickTimestampServerSeconds = 1738200001,
                InstallBeginTimestampServerSeconds = 1738200061,
                InstallVersion = "1.0.84",
                GooglePlayInstantParam = false,
                UtmSource = "google",
                UtmMedium = "cpc",
                UtmCampaign = "",
                UtmContent = "",
                UtmTerm = "",
                AppInstallTimeMs = 1738000000000,
                SdkVersion = "0.1.0",
                FetchTimestamp = "2026-01-30T12:00:00.0000000Z"
            };

            string json = JsonUtility.ToJson(original);
            var deserialized = JsonUtility.FromJson<CachedReferrerData>(json);

            Assert.AreEqual(original.InstallReferrer, deserialized.InstallReferrer);
            Assert.AreEqual(original.ReferrerClickTimestampSeconds, deserialized.ReferrerClickTimestampSeconds);
            Assert.AreEqual(original.InstallBeginTimestampSeconds, deserialized.InstallBeginTimestampSeconds);
            Assert.AreEqual(original.UtmSource, deserialized.UtmSource);
            Assert.AreEqual(original.UtmMedium, deserialized.UtmMedium);
            Assert.AreEqual(original.AppInstallTimeMs, deserialized.AppInstallTimeMs);
            Assert.AreEqual(original.SdkVersion, deserialized.SdkVersion);
            Assert.AreEqual(original.FetchTimestamp, deserialized.FetchTimestamp);
        }

        [Test]
        public void CachedData_EmptyJson_DeserializesGracefully()
        {
            const string json = @"{
                ""InstallReferrer"": """",
                ""UtmSource"": """",
                ""SdkVersion"": ""0.1.0"",
                ""FetchTimestamp"": ""2026-01-30T12:00:00Z""
            }";

            var deserialized = JsonUtility.FromJson<CachedReferrerData>(json);

            Assert.AreEqual("", deserialized.InstallReferrer);
            Assert.IsTrue(deserialized.IsOrganic);
        }
    }
}
