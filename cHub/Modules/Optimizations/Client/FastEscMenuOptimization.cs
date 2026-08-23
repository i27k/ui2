using System;
using System.Diagnostics;
using System.Linq;
using cHub.Core;
using UnityEngine;

namespace cHub.Modules.Optimizations.Client
{
    /// <summary>
    /// Diagnostic / optimization component for the vanilla ESC menu.
    ///
    /// Measures:
    /// ESC key press -> vanilla ingameMenu becoming open.
    ///
    /// This version intentionally does NOT force-open or prewarm the menu.
    /// First we measure the real vanilla delay so the actual bottleneck
    /// can be patched instead of guessing.
    /// </summary>
    internal sealed class FastEscMenuOptimization : MonoBehaviour
    {
        // ============================================================
        // CONFIG
        // ============================================================

        private const string IngameMenuWindow = "ingameMenu";

        // If vanilla still has not opened the menu after this long,
        // the diagnostic attempt is considered timed out.
        private const float EscOpenTimeoutSeconds = 2.0f;


        // ============================================================
        // INSTANCE
        // ============================================================

        private static FastEscMenuOptimization _instance;


        // ============================================================
        // UI STATE
        // ============================================================

        private object _currentWorld;

        private LocalPlayerUI _playerUi;

        private bool _uiReady;

        private bool _lastMenuOpen;


        // ============================================================
        // ESC DIAGNOSTIC STATE
        // ============================================================

        private bool _waitingForMenuOpen;

        private long _escapeTimestamp;

        private int _escapeFrame;

        private float _escapeRealtime;

        private int _measurementNumber;


        // ============================================================
        // CREATE
        // ============================================================

        internal static void Create()
        {
            if (_instance != null)
                return;

            GameObject host =
                new GameObject(
                    "cHub.FastEscMenuOptimization");

            DontDestroyOnLoad(host);

            _instance =
                host.AddComponent<FastEscMenuOptimization>();
        }


        // ============================================================
        // UNITY
        // ============================================================

        private void Awake()
        {
            StartupTerminal.ReportFeature(
                "optimization.fast-esc.diagnostic",
                "Fast ESC Menu Diagnostics",
                true,
                "ESC to ingameMenu timing diagnostics loaded");

            UnityEngine.Debug.Log(
                "[cHub] [FastESC] Diagnostics loaded.");
        }


        private void Update()
        {
            try
            {
                DetectWorld();

                if (!_uiReady)
                    return;


                bool menuOpen =
                    IsIngameMenuOpen();


                // ====================================================
                // ESC PRESSED
                // ====================================================

                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    // We only measure opening.
                    // If the menu is already open, this ESC is being
                    // used by vanilla to close it.
                    if (!menuOpen)
                    {
                        BeginEscMeasurement();
                    }
                }


                // ====================================================
                // MENU OPENED
                // ====================================================

                if (menuOpen &&
                    !_lastMenuOpen)
                {
                    OnMenuOpened();
                }


                // ====================================================
                // TIMEOUT
                // ====================================================

                if (_waitingForMenuOpen &&
                    Time.realtimeSinceStartup -
                    _escapeRealtime >
                    EscOpenTimeoutSeconds)
                {
                    _waitingForMenuOpen = false;

                    UnityEngine.Debug.LogWarning(
                        "[cHub] [FastESC] ESC #" +
                        _measurementNumber +
                        " timed out after " +
                        EscOpenTimeoutSeconds.ToString("0.0") +
                        " seconds without detecting ingameMenu.");
                }


                _lastMenuOpen = menuOpen;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning(
                    "[cHub] [FastESC] Diagnostic error: " +
                    ex.GetType().Name +
                    " // " +
                    ex.Message);
            }
        }


        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }


        // ============================================================
        // WORLD / PLAYER UI
        // ============================================================

        private void DetectWorld()
        {
            object world =
                GameManager.Instance?.World;


            // ========================================================
            // MAIN MENU
            // ========================================================

            if (world == null)
            {
                if (_currentWorld != null)
                {
                    _currentWorld = null;
                    ResetUiState();
                }

                return;
            }


            // ========================================================
            // NEW WORLD / SERVER
            // ========================================================

            if (!ReferenceEquals(
                world,
                _currentWorld))
            {
                _currentWorld = world;

                ResetUiState();

                UnityEngine.Debug.Log(
                    "[cHub] [FastESC] World detected. " +
                    "Waiting for LocalPlayerUI.");
            }


            if (_uiReady)
                return;


            ResolveLocalPlayerUi();
        }


        private void ResolveLocalPlayerUi()
        {
            if (GameManager.Instance?.World == null)
                return;


            EntityPlayerLocal player =
                GameManager.Instance.World
                    .GetLocalPlayers()
                    .FirstOrDefault();


            if (player == null)
                return;


            LocalPlayerUI ui =
                LocalPlayerUI.GetUIForPlayer(player);


            if (ui == null ||
                ui.windowManager == null)
            {
                return;
            }


            _playerUi = ui;


            // Verify that the vanilla window actually exists.
            var window =
                _playerUi.windowManager.GetWindow(
                    IngameMenuWindow);


            if (window == null)
                return;


            _uiReady = true;

            _lastMenuOpen =
                IsIngameMenuOpen();


            UnityEngine.Debug.Log(
                "[cHub] [FastESC] LocalPlayerUI ready. " +
                "ingameMenu diagnostics active.");


            StartupTerminal.ReportFeature(
                "optimization.fast-esc.ui",
                "Fast ESC Menu UI Hook",
                true,
                "LocalPlayerUI and vanilla ingameMenu detected");
        }


        private void ResetUiState()
        {
            _playerUi = null;

            _uiReady = false;

            _lastMenuOpen = false;

            _waitingForMenuOpen = false;

            _escapeTimestamp = 0;

            _escapeFrame = -1;

            _escapeRealtime = 0f;

            _measurementNumber = 0;
        }


        // ============================================================
        // MEASUREMENT
        // ============================================================

        private void BeginEscMeasurement()
        {
            //====== Modificari i27k ========

            EscSaveOptimization.ReportEscapePressed();

            PauseSaveBypassOptimization.ReportEscapePressed();

            //===============================

            _measurementNumber++;

            _waitingForMenuOpen = true;

            _escapeTimestamp =
                Stopwatch.GetTimestamp();

            _escapeFrame =
                Time.frameCount;

            _escapeRealtime =
                Time.realtimeSinceStartup;

            UnityEngine.Debug.Log(
                "[cHub] [FastESC] ESC #" +
                _measurementNumber +
                " PRESSED // frame=" +
                _escapeFrame);
        }


        private void OnMenuOpened()
        {
            if (!_waitingForMenuOpen)
            {
                UnityEngine.Debug.Log(
                    "[cHub] [FastESC] ingameMenu opened " +
                    "without an active ESC measurement.");

                return;
            }


            _waitingForMenuOpen = false;


            long now =
                Stopwatch.GetTimestamp();


            double elapsedMilliseconds =
                (now - _escapeTimestamp) *
                1000.0 /
                Stopwatch.Frequency;


            int frames =
                Time.frameCount -
                _escapeFrame;


            string result =
                "[cHub] [FastESC] ESC #" +
                _measurementNumber +
                " -> ingameMenu OPEN // " +
                elapsedMilliseconds.ToString("0.00") +
                " ms // " +
                frames +
                " frame(s)";


            UnityEngine.Debug.Log(result);


            // Separate ID intentionally.
            // ReportFeature deduplicates identical READY states.
            StartupTerminal.ReportFeature(
                "optimization.fast-esc.measurement",
                "Fast ESC Menu Measurement",
                true,
                elapsedMilliseconds.ToString("0.00") +
                " ms / " +
                frames +
                " frame(s)");
        }


        // ============================================================
        // VANILLA MENU STATE
        // ============================================================

        private bool IsIngameMenuOpen()
        {
            if (_playerUi?.windowManager == null)
                return false;


            return
                _playerUi.windowManager.IsWindowOpen(
                    IngameMenuWindow);
        }
    }
}