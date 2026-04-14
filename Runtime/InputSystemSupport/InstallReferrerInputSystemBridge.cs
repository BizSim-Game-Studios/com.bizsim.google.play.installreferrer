// Copyright (c) BizSim Game Studios. All rights reserved.
// Author: Aşkın Ceyhan (https://github.com/AskinCeyhan)
// https://www.bizsim.com | https://www.junkyardtycoon.com

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace BizSim.Google.Play.InstallReferrer.InputSystemSupport
{
    /// <summary>
    /// Auto-registers New Input System handlers for <see cref="InstallReferrerDebugMenu"/>.
    /// This assembly only compiles when <c>com.unity.inputsystem</c> is installed
    /// (enforced via <c>defineConstraints</c> in the asmdef).
    /// </summary>
    internal static class InstallReferrerInputSystemBridge
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Register()
        {
            EnhancedTouchSupport.Enable();

            InstallReferrerDebugMenu.KeyToggleCheck = () =>
                Keyboard.current != null && Keyboard.current[Key.F9].wasPressedThisFrame;

            InstallReferrerDebugMenu.TouchBeganCheck = () =>
            {
                foreach (var touch in Touch.activeTouches)
                {
                    if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                        return touch.screenPosition;
                }
                return null;
            };
        }
    }
}
