using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using cHub.Core;

namespace cHub.Modules.Optimizations.Client
{
    /// <summary>
    /// c/Hub optimization for the vanilla ESC pause menu.
    ///
    /// Vanilla calls:
    ///
    /// GameManager.updatePauseState()
    ///     -> GameManager.SaveWorld()
    ///     -> World.Save()
    ///     -> ChunkCluster.Save()
    ///     -> ChunkProviderGenerateWorld.SaveAll()
    ///     -> RegionFileManager.WaitSaveDone()
    ///
    /// That synchronous save can block the frame for ~500 ms.
    ///
    /// This optimization skips ONLY the SaveWorld() call that happens
    /// from updatePauseState immediately after the local player presses ESC.
    ///
    /// Normal saves, autosaves, shutdown saves and other SaveWorld calls
    /// are NOT affected.
    /// </summary>
    internal static class PauseSaveBypassOptimization
    {
        // ============================================================
        // CONFIG
        // ============================================================

        private const float EscapeWindowSeconds = 0.35f;


        // ============================================================
        // STATE
        // ============================================================

        private static float _lastEscapeTime = -100f;

        private static int _lastEscapeFrame = -1;

        private static int _pauseStateDepth = 0;

        private static int _skippedSaveCount = 0;


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
        // HELPERS
        // ============================================================

        private static bool IsRecentEscape()
        {
            float elapsed =
                Time.realtimeSinceStartup -
                _lastEscapeTime;

            return
                elapsed >= 0f &&
                elapsed <= EscapeWindowSeconds;
        }


        private static bool ShouldBypassPauseSave()
        {
            if (_pauseStateDepth <= 0)
                return false;

            if (!IsRecentEscape())
                return false;

            // Dedicated servers do not have a local player.
            if (GameManager.Instance?.World == null)
                return false;

            var localPlayers =
                GameManager.Instance.World.GetLocalPlayers();

            if (localPlayers == null ||
                localPlayers.Count == 0)
            {
                return false;
            }

            return true;
        }


        // ============================================================
        // updatePauseState PATCH
        // ============================================================

        [HarmonyPatch]
        private static class UpdatePauseStatePatch
        {
            private static MethodBase TargetMethod()
            {
                MethodBase method =
                    AccessTools.Method(
                        typeof(GameManager),
                        "updatePauseState");

                if (method == null)
                {
                    UnityEngine.Debug.LogWarning(
                        "[cHub] [PauseSaveBypass] " +
                        "GameManager.updatePauseState was not found.");

                    return null;
                }


                UnityEngine.Debug.Log(
                    "[cHub] [PauseSaveBypass] " +
                    "Hook attached to GameManager.updatePauseState().");


                return method;
            }


            private static void Prefix()
            {
                _pauseStateDepth++;
            }


            private static void Finalizer(
                Exception __exception)
            {
                if (_pauseStateDepth > 0)
                    _pauseStateDepth--;
            }
        }


        // ============================================================
        // SaveWorld PATCH
        // ============================================================

        [HarmonyPatch]
        private static class SaveWorldPatch
        {
            private static MethodBase TargetMethod()
            {
                MethodBase method =
                    AccessTools.Method(
                        typeof(GameManager),
                        "SaveWorld");

                if (method == null)
                {
                    UnityEngine.Debug.LogWarning(
                        "[cHub] [PauseSaveBypass] " +
                        "GameManager.SaveWorld was not found.");

                    return null;
                }


                UnityEngine.Debug.Log(
                    "[cHub] [PauseSaveBypass] " +
                    "Hook attached to GameManager.SaveWorld().");


                StartupTerminal.ReportFeature(
                    "optimization.pause-save-bypass",
                    "ESC Save Stall Optimization",
                    true,
                    "Selective pause-triggered SaveWorld bypass initialized");


                return method;
            }


            private static bool Prefix()
            {
                if (!ShouldBypassPauseSave())
                    return true;


                _skippedSaveCount++;


                float elapsedMs =
                    (Time.realtimeSinceStartup -
                     _lastEscapeTime) *
                    1000f;


                int frameDifference =
                    Time.frameCount -
                    _lastEscapeFrame;


                UnityEngine.Debug.Log(
                    "[cHub] [PauseSaveBypass] " +
                    "SKIPPED pause-triggered SaveWorld // " +
                    "ESC age=" +
                    elapsedMs.ToString("0.00") +
                    " ms // frameDelta=" +
                    frameDifference +
                    " // totalSkipped=" +
                    _skippedSaveCount);


                // false = do not execute vanilla SaveWorld()
                // ONLY for this pause/ESC call.
                return false;
            }
        }
    }
}