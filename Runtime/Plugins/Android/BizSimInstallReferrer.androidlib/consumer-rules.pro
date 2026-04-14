# Consumer ProGuard/R8 rules for com.bizsim.google.play.installreferrer
# These rules are automatically applied to the consuming app's minification pass.

# Google Play Install Referrer API (Google's SDK)
-keep class com.android.installreferrer.** { *; }

# BizSim Install Referrer Bridge (JNI bridge class invoked via UnitySendMessage)
# Note: checkInstallReferrerWithFake() is intentionally NOT kept here.
# It is only called from C# in debug builds (Debug.isDebugBuild), so R8 can
# safely tree-shake it and its FakeManager dependencies from release APKs.
-keep class com.bizsim.google.play.installreferrer.InstallReferrerBridge {
    public static void checkInstallReferrer(...);
    public static void cleanup();
    public static long getAppInstallTimeMs();
}
