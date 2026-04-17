using NUnit.Framework;
using BizSim.Google.Play.InstallReferrer;

namespace BizSim.Google.Play.InstallReferrer.Tests
{
    /// <summary>K8 PackageVersion schema drift guard (Plan G — first intro).</summary>
    public class PackageVersionSchemaTest
    {
        [Test]
        public void NativeSdkFields_ArePopulated()
        {
            Assert.IsFalse(string.IsNullOrEmpty(PackageVersion.NativeSdkVersion));
            Assert.IsFalse(string.IsNullOrEmpty(PackageVersion.NativeSdkLabel));
            Assert.IsFalse(string.IsNullOrEmpty(PackageVersion.NativeSdkArtifactCoord));
        }

        [Test]
        public void NativeSdkArtifactCoord_EndsWithVersion()
        {
            Assert.IsTrue(PackageVersion.NativeSdkArtifactCoord.EndsWith(":" + PackageVersion.NativeSdkVersion));
        }

        [Test]
        public void NativeSdkFields_MatchExpectedInstallReferrerValues()
        {
            Assert.AreEqual("2.2", PackageVersion.NativeSdkVersion);
            Assert.AreEqual("Install Referrer", PackageVersion.NativeSdkLabel);
            Assert.AreEqual("com.android.installreferrer:installreferrer:2.2", PackageVersion.NativeSdkArtifactCoord);
        }
    }
}
