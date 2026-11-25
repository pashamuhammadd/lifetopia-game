using System;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Lifetopia.Controls
{
    /// <summary>
    /// Raises <see cref="FocusLost"/> when the app loses focus or the browser tab is hidden.
    /// In Web builds it listens to browser events (blur, visibilitychange, pagehide) through
    /// LifetopiaFocus.jslib; on every platform Unity's own focus/pause callbacks also feed it.
    /// </summary>
    public static class WebFocusBridge
    {
        public static event Action FocusLost;

        /// <summary>Called by input components from OnApplicationFocus(false) / OnApplicationPause(true).</summary>
        public static void NotifyFocusLost() => FocusLost?.Invoke();

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void LifetopiaRegisterFocusLossCallback(Action callback);

        [AOT.MonoPInvokeCallback(typeof(Action))]
        private static void OnBrowserFocusLost() => NotifyFocusLost();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Register() => LifetopiaRegisterFocusLossCallback(OnBrowserFocusLost);
#endif
    }
}
