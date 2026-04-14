// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

package com.bizsim.google.play.installreferrer;

import android.app.Activity;
import android.content.Context;
import android.content.pm.PackageInfo;
import android.content.pm.PackageManager;
import android.os.Build;
import android.os.RemoteException;
import android.util.Log;

import com.android.installreferrer.api.InstallReferrerClient;
import com.android.installreferrer.api.InstallReferrerStateListener;
import com.android.installreferrer.api.ReferrerDetails;

import com.unity3d.player.UnityPlayer;

import org.json.JSONException;
import org.json.JSONObject;

/**
 * Java bridge for the Google Play Install Referrer API.
 * Called from Unity C# via {@code AndroidJavaClass} JNI calls.
 * Results are returned asynchronously through {@code UnityPlayer.UnitySendMessage}.
 *
 * <p><b>State Machine:</b> IDLE → CONNECTING → CONNECTED / DISCONNECTED → IDLE</p>
 *
 * <p><b>Lifecycle:</b> The bridge connects to the Install Referrer service,
 * reads referrer details, sends the result to Unity, and disconnects.
 * The {@code cleanup()} method is always called in a finally block.</p>
 */
public class InstallReferrerBridge {

    private static final String TAG = "InstallReferrerBridge";

    // State machine
    private static final int STATE_IDLE = 0;
    private static final int STATE_CONNECTING = 1;
    private static final int STATE_CONNECTED = 2;
    private static final int STATE_DISCONNECTED = 3;

    private static int sState = STATE_IDLE;
    private static InstallReferrerClient sClient;
    private static boolean sCallbackSent;

    // =================================================================
    // PRODUCTION — Real API call
    // =================================================================

    /**
     * Fetches install referrer data from the Google Play Install Referrer API.
     *
     * @param gameObjectName Unity GameObject name that will receive callbacks
     * @param successMethod  C# method name for the success callback
     * @param errorMethod    C# method name for the error callback
     */
    public static void checkInstallReferrer(
            final String gameObjectName,
            final String successMethod,
            final String errorMethod) {

        checkInstallReferrerInternal(gameObjectName, successMethod, errorMethod,
                false, null);
    }

    // =================================================================
    // TESTING — Fake referrer string for controlled responses
    // =================================================================

    /**
     * Returns a fake referrer response for testing the full Java-to-C# bridge path.
     *
     * @param gameObjectName  Unity GameObject name that will receive callbacks
     * @param successMethod   C# method name for the success callback
     * @param errorMethod     C# method name for the error callback
     * @param useFake         true to use a fake referrer string
     * @param fakeReferrerUrl Fake referrer URL string (e.g., "utm_source=test&utm_medium=cpc")
     */
    public static void checkInstallReferrerWithFake(
            final String gameObjectName,
            final String successMethod,
            final String errorMethod,
            final boolean useFake,
            final String fakeReferrerUrl) {

        checkInstallReferrerInternal(gameObjectName, successMethod, errorMethod,
                useFake, fakeReferrerUrl);
    }

    /**
     * Cleans up the Install Referrer client connection.
     * Safe to call multiple times. Always called in a finally block.
     */
    public static synchronized void cleanup() {
        try {
            if (sClient != null) {
                sClient.endConnection();
                Log.d(TAG, "Client connection ended");
            }
        } catch (Exception e) {
            Log.w(TAG, "cleanup() exception (non-fatal): " + e.getMessage());
        } finally {
            sClient = null;
            sState = STATE_IDLE;
        }
    }

    /**
     * Returns the app's first install time in milliseconds since epoch.
     * Used by C# for cache validation.
     *
     * @return install time in ms, or -1 if unavailable
     */
    public static long getAppInstallTimeMs() {
        try {
            Activity activity = UnityPlayer.currentActivity;
            if (activity == null) return -1;

            PackageManager pm = activity.getPackageManager();
            PackageInfo pi;
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
                pi = pm.getPackageInfo(activity.getPackageName(), PackageManager.PackageInfoFlags.of(0));
            } else {
                pi = pm.getPackageInfo(activity.getPackageName(), 0);
            }
            return pi.firstInstallTime;
        } catch (Exception e) {
            Log.w(TAG, "getAppInstallTime() failed: " + e.getMessage());
            return -1;
        }
    }

    // =================================================================
    // INTERNAL IMPLEMENTATION
    // =================================================================

    private static synchronized void checkInstallReferrerInternal(
            final String gameObjectName,
            final String successMethod,
            final String errorMethod,
            final boolean useFake,
            final String fakeReferrerUrl) {

        final Activity activity = UnityPlayer.currentActivity;
        if (activity == null) {
            sendError(gameObjectName, errorMethod, -100, "Unity activity is null");
            return;
        }

        // Guard against stale state from Unity Domain Reload (Editor Play/Stop cycle).
        // Java statics survive C# domain reloads — if sState is not IDLE, a previous
        // session ended without cleanup. Auto-recover by forcing cleanup instead of
        // returning ALREADY_CONNECTING — this is more user-friendly in the Editor
        // where rapid Play/Stop cycles are common.
        if (sState != STATE_IDLE) {
            Log.w(TAG, "Stale state detected (state=" + sState
                    + ", client=" + (sClient != null ? "alive" : "null")
                    + "). Auto-recovering via cleanup().");
            cleanup();
        }

        if (useFake) {
            // Fake mode: build JSON directly without connecting to the service
            Log.d(TAG, "Using fake referrer: " + fakeReferrerUrl);
            try {
                JSONObject json = new JSONObject();
                json.put("installReferrer", fakeReferrerUrl != null ? fakeReferrerUrl : "");
                json.put("referrerClickTimestampSeconds", 0);
                json.put("installBeginTimestampSeconds", 0);
                json.put("referrerClickTimestampServerSeconds", 0);
                json.put("installBeginTimestampServerSeconds", 0);
                json.put("installVersion", "");
                json.put("googlePlayInstantParam", false);

                Log.d(TAG, "Fake referrer result: " + json.toString());
                UnityPlayer.UnitySendMessage(gameObjectName, successMethod, json.toString());
            } catch (JSONException e) {
                Log.e(TAG, "Fake referrer JSON error", e);
                sendError(gameObjectName, errorMethod, -100,
                        "Fake referrer JSON error: " + e.getMessage());
            }
            return;
        }

        // Real API path
        sState = STATE_CONNECTING;
        sCallbackSent = false;

        try {
            sClient = InstallReferrerClient.newBuilder(activity).build();

            sClient.startConnection(new InstallReferrerStateListener() {
                @Override
                public void onInstallReferrerSetupFinished(int responseCode) {
                    if (sCallbackSent) return;
                    sCallbackSent = true;
                    try {
                        if (responseCode == InstallReferrerClient.InstallReferrerResponse.OK) {
                            sState = STATE_CONNECTED;
                            handleSuccess(gameObjectName, successMethod, errorMethod);
                        } else {
                            sState = STATE_DISCONNECTED;
                            String msg = mapResponseCode(responseCode);
                            Log.e(TAG, "Setup failed: code=" + responseCode + " msg=" + msg);
                            sendError(gameObjectName, errorMethod, responseCode, msg);
                        }
                    } finally {
                        cleanup();
                    }
                }

                @Override
                public void onInstallReferrerServiceDisconnected() {
                    sState = STATE_DISCONNECTED;
                    Log.w(TAG, "Service disconnected unexpectedly");
                    if (!sCallbackSent) {
                        sCallbackSent = true;
                        sendError(gameObjectName, errorMethod, -1,
                                "Service disconnected before response");
                    }
                    cleanup();
                }
            });
        } catch (Throwable e) {
            // Catches both Exception and Error (e.g., NoClassDefFoundError
            // when the Google Play SDK is not bundled via EDM4U).
            Log.e(TAG, "Failed to start connection", e);
            sendError(gameObjectName, errorMethod, -100,
                    "Connection start failed: " + e.getMessage());
            cleanup();
        }
    }

    private static void handleSuccess(
            final String gameObjectName,
            final String successMethod,
            final String errorMethod) {

        try {
            ReferrerDetails details = sClient.getInstallReferrer();

            // ⚠ IMPORTANT: All JSON key names below MUST be camelCase and match
            // the field names in C# InstallReferrerResult exactly.
            // JsonUtility.FromJson is case-sensitive — mismatched keys silently
            // fail to deserialize. See InstallReferrerJsonTests.cs for validation.

            // Read all fields upfront with null guards.
            // ReferrerDetails getters throw RemoteException on IPC failure —
            // caught by the outer catch block which sends a proper error to C#.
            String referrer = details.getInstallReferrer();
            String version = details.getInstallVersion();

            JSONObject json = new JSONObject();
            json.put("installReferrer", referrer != null ? referrer : "");
            json.put("referrerClickTimestampSeconds", details.getReferrerClickTimestampSeconds());
            json.put("installBeginTimestampSeconds", details.getInstallBeginTimestampSeconds());
            json.put("referrerClickTimestampServerSeconds", details.getReferrerClickTimestampServerSeconds());
            json.put("installBeginTimestampServerSeconds", details.getInstallBeginTimestampServerSeconds());
            json.put("installVersion", version != null ? version : "");
            json.put("googlePlayInstantParam", details.getGooglePlayInstantParam());

            Log.d(TAG, "Install referrer result: " + json.toString());
            UnityPlayer.UnitySendMessage(gameObjectName, successMethod, json.toString());

        } catch (RemoteException e) {
            Log.e(TAG, "RemoteException reading referrer details", e);
            sendError(gameObjectName, errorMethod, -100,
                    "RemoteException: " + e.getMessage());
        } catch (JSONException e) {
            Log.e(TAG, "JSON serialization failed", e);
            sendError(gameObjectName, errorMethod, -100,
                    "Serialization error: " + e.getMessage());
        } catch (Exception e) {
            Log.e(TAG, "Unexpected error reading referrer", e);
            sendError(gameObjectName, errorMethod, -100,
                    "Unexpected error: " + e.getMessage());
        }
    }

    // =================================================================
    // HELPER METHODS
    // =================================================================

    /**
     * Maps the Install Referrer response code to a human-readable message.
     */
    private static String mapResponseCode(int code) {
        switch (code) {
            case InstallReferrerClient.InstallReferrerResponse.OK:
                return "OK";
            case InstallReferrerClient.InstallReferrerResponse.FEATURE_NOT_SUPPORTED:
                return "Install Referrer API not supported on this device";
            case InstallReferrerClient.InstallReferrerResponse.SERVICE_UNAVAILABLE:
                return "Install Referrer service unavailable (Play Store may be updating)";
            case InstallReferrerClient.InstallReferrerResponse.DEVELOPER_ERROR:
                return "Developer error (invalid request)";
            case InstallReferrerClient.InstallReferrerResponse.SERVICE_DISCONNECTED:
                return "Service disconnected before response";
            default:
                return "Unknown response code: " + code;
        }
    }

    /**
     * Sends an error response back to Unity via {@code UnitySendMessage}.
     * The JSON payload contains {@code errorCode}, {@code errorMessage}, and {@code isRetryable}.
     */
    private static void sendError(String gameObjectName, String errorMethod,
                                   int errorCode, String errorMessage) {
        try {
            JSONObject json = new JSONObject();
            json.put("errorCode", errorCode);
            json.put("errorMessage", errorMessage != null ? errorMessage : "Unknown error");
            json.put("isRetryable", isRetryable(errorCode));
            UnityPlayer.UnitySendMessage(gameObjectName, errorMethod, json.toString());
        } catch (Exception e) {
            Log.e(TAG, "Failed to send error to Unity", e);
        }
    }

    /**
     * Determines if an error code represents a transient failure that can be retried.
     * SERVICE_UNAVAILABLE (1) and SERVICE_DISCONNECTED (5) are transient.
     * FEATURE_NOT_SUPPORTED (2), DEVELOPER_ERROR (3), and internal errors are permanent.
     */
    private static boolean isRetryable(int errorCode) {
        return errorCode == InstallReferrerClient.InstallReferrerResponse.SERVICE_UNAVAILABLE
            || errorCode == InstallReferrerClient.InstallReferrerResponse.SERVICE_DISCONNECTED;
    }
}
