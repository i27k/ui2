using System;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace cHub.Modules.Optimizations.Client
{
    /// <summary>
    /// Diagnostic pentru save-ul vanilla declanșat la ESC.
    ///
    /// Urmărește mesajele:
    /// "Saving X of chunks took Yms"
    ///
    /// și afișează call stack-ul pentru a identifica metoda vanilla
    /// exactă care blochează frame-ul când este deschis ingameMenu.
    ///
    /// IMPORTANT:
    /// această versiune NU oprește salvarea.
    /// </summary>
    [HarmonyPatch]
    internal static class EscSaveOptimization
    {
        private static float _lastEscapeTime = -100f;
        private static int _lastEscapeFrame = -1;

        private const float EscDetectionWindow = 1.25f;


        // ============================================================
        // ESC TRACKING
        // ============================================================

        internal static void ReportEscapePressed()
        {
            _lastEscapeTime =
                Time.realtimeSinceStartup;

            _lastEscapeFrame =
                Time.frameCount;
        }


        // ============================================================
        // HARMONY TARGET
        // ============================================================

        private static MethodBase TargetMethod()
        {
            Type logType =
                AccessTools.TypeByName("Log");

            if (logType == null)
            {
                UnityEngine.Debug.LogWarning(
                    "[cHub] [EscSave] Could not find vanilla Log type.");

                return null;
            }


            MethodInfo method =
                AccessTools.Method(
                    logType,
                    "Out",
                    new Type[]
                    {
                        typeof(string)
                    });


            if (method == null)
            {
                UnityEngine.Debug.LogWarning(
                    "[cHub] [EscSave] Could not find Log.Out(string).");

                return null;
            }


            UnityEngine.Debug.Log(
                "[cHub] [EscSave] Diagnostic hook attached to Log.Out(string).");


            return method;
        }


        // ============================================================
        // LOG INTERCEPT
        // ============================================================

        private static void Prefix(string __0)
        {
            try
            {
                string text =
                    __0;


                if (string.IsNullOrEmpty(text))
                    return;


                // We only care about vanilla chunk save completion logs.
                if (!text.Contains("Saving ") ||
                    !text.Contains(" of chunks took "))
                {
                    return;
                }


                float sinceEscape =
                    Time.realtimeSinceStartup -
                    _lastEscapeTime;


                // Ignore normal autosaves that have nothing to do with ESC.
                if (sinceEscape < 0f ||
                    sinceEscape > EscDetectionWindow)
                {
                    return;
                }


                int frameDifference =
                    Time.frameCount -
                    _lastEscapeFrame;


                StackTrace stackTrace =
                    new StackTrace(
                        1,
                        true);


                string stack =
                    stackTrace.ToString();


                UnityEngine.Debug.Log(
                    "\n" +
                    "============================================================\n" +
                    "[cHub] [EscSave] ESC SAVE DETECTED\n" +
                    "------------------------------------------------------------\n" +
                    "Vanilla log:\n" +
                    text +
                    "\n\n" +
                    "Time since ESC: " +
                    (sinceEscape * 1000f).ToString("0.00") +
                    " ms\n" +
                    "Frame difference: " +
                    frameDifference +
                    "\n\n" +
                    "CALL STACK:\n" +
                    stack +
                    "\n" +
                    "============================================================");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning(
                    "[cHub] [EscSave] Diagnostic exception: " +
                    ex.GetType().Name +
                    " // " +
                    ex.Message);
            }
        }
    }
}