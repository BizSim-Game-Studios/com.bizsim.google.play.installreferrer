// Copyright (c) BizSim Game Studios. All rights reserved.
// Tests for UTM parameter parsing.

using NUnit.Framework;

namespace BizSim.Google.Play.InstallReferrer.Tests
{
    /// <summary>
    /// Unit tests for <see cref="InstallReferrerUtility.ParseUtmParameters"/>.
    /// </summary>
    [TestFixture]
    public class InstallReferrerUtmTests
    {
        [Test]
        public void NullReferrer_ReturnsEmptyStrings()
        {
            InstallReferrerUtility.ParseUtmParameters(null,
                out string src, out string med, out string cam,
                out string con, out string trm);

            Assert.AreEqual("", src);
            Assert.AreEqual("", med);
            Assert.AreEqual("", cam);
            Assert.AreEqual("", con);
            Assert.AreEqual("", trm);
        }

        [Test]
        public void EmptyReferrer_ReturnsEmptyStrings()
        {
            InstallReferrerUtility.ParseUtmParameters("",
                out string src, out string med, out string cam,
                out string con, out string trm);

            Assert.AreEqual("", src);
            Assert.AreEqual("", med);
            Assert.AreEqual("", cam);
            Assert.AreEqual("", con);
            Assert.AreEqual("", trm);
        }

        [Test]
        public void FullUtmReferrer_ParsesAllParameters()
        {
            const string referrer = "utm_source=google&utm_medium=cpc&utm_campaign=summer_sale&utm_content=banner&utm_term=mobile+games";

            InstallReferrerUtility.ParseUtmParameters(referrer,
                out string src, out string med, out string cam,
                out string con, out string trm);

            Assert.AreEqual("google", src);
            Assert.AreEqual("cpc", med);
            Assert.AreEqual("summer_sale", cam);
            Assert.AreEqual("banner", con);
            Assert.AreEqual("mobile games", trm); // '+' decoded to space
        }

        [Test]
        public void PartialUtmReferrer_ParsesAvailableParameters()
        {
            const string referrer = "utm_source=facebook&utm_medium=social";

            InstallReferrerUtility.ParseUtmParameters(referrer,
                out string src, out string med, out string cam,
                out string con, out string trm);

            Assert.AreEqual("facebook", src);
            Assert.AreEqual("social", med);
            Assert.AreEqual("", cam);
            Assert.AreEqual("", con);
            Assert.AreEqual("", trm);
        }

        [Test]
        public void UrlEncodedValues_DecodesCorrectly()
        {
            const string referrer = "utm_source=google&utm_campaign=hello%20world&utm_term=c%23+programming";

            InstallReferrerUtility.ParseUtmParameters(referrer,
                out string src, out string med, out string cam,
                out string con, out string trm);

            Assert.AreEqual("google", src);
            Assert.AreEqual("hello world", cam);
            Assert.AreEqual("c# programming", trm);
        }

        [Test]
        public void NonUtmParameters_Ignored()
        {
            const string referrer = "custom_param=value&utm_source=test&foo=bar";

            InstallReferrerUtility.ParseUtmParameters(referrer,
                out string src, out string med, out string cam,
                out string con, out string trm);

            Assert.AreEqual("test", src);
            Assert.AreEqual("", med);
        }

        [Test]
        public void LeadingQuestionMark_HandledCorrectly()
        {
            const string referrer = "?utm_source=google&utm_medium=cpc";

            var result = InstallReferrerUtility.ParseQueryString(referrer);

            Assert.AreEqual("google", result["utm_source"]);
            Assert.AreEqual("cpc", result["utm_medium"]);
        }

        [Test]
        public void CaseInsensitiveKeys_MatchCorrectly()
        {
            const string referrer = "UTM_SOURCE=Google&UTM_MEDIUM=CPC";

            var result = InstallReferrerUtility.ParseQueryString(referrer);

            Assert.IsTrue(result.ContainsKey("utm_source"));
            Assert.AreEqual("Google", result["utm_source"]);
        }

        [Test]
        public void KeyWithoutValue_ParsesAsEmpty()
        {
            const string referrer = "utm_source=google&empty_key";

            var result = InstallReferrerUtility.ParseQueryString(referrer);

            Assert.AreEqual("google", result["utm_source"]);
            Assert.AreEqual("", result["empty_key"]);
        }

        [Test]
        public void UrlDecode_HandlesInvalidSequences()
        {
            // Invalid percent encoding — should return original string
            string result = InstallReferrerUtility.UrlDecode("%ZZ");
            Assert.AreEqual("%ZZ", result);
        }
    }
}
