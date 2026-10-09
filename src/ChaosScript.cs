// StreamEmber Chaos Mod (RDR2) — StreamEmber Runtime (RDR2) script; the menu is an MHud page in the StreamEmber Overlay.
//
//   F6 (ChaosMod.ini MenuKey)  open / close the chaos menu. While it is open the mouse and keyboard go to the page:
//                              pick a category, click an effect, "Çalıştır". Esc or F6 closes it.
//
// The menu key is read with GetAsyncKeyState every tick: in UI input mode the overlay keeps the key messages from
// the game, so KeyDown would never see the key that closes the menu (same as the trainers).
//
// One overlay page for all scripts: the chaos mod talks to the overlay only while its own page is shown (ChaosPage).
// With the trainer installed as well, whichever opens first keeps the overlay; F6 switches it to the chaos page.
//
// No other hotkeys and no integrations in this version: every effect starts from the menu (or from the optional
// automatic mode / Total Chaos). EventFabric will call ChaosEngine.Run(id, "eventfabric") later.
//
// Message flow (same transport as the trainers): C# -> page { action, data } through OverlayBridge.Send,
// page -> C# { cb, data } through window.streamember.post: ready, run, stop, stopAll, random, cleanup, setting, close.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using RDR2;
using StreamEmber.Overlay;
using StreamEmber.Trainers;

namespace StreamEmber.ChaosMod
{
    public sealed class ChaosScript : Script
    {
        private readonly ChaosConfig _config;
        private readonly ChaosPage _page;
        private readonly EffectRegistry _registry = new EffectRegistry();
        private readonly ChaosEngine _engine;
        private bool _menuOpen;
        private bool _announcedNotInstalled;
        private bool _wasDead;
        private bool _menuKeyDown;
        private IntPtr _gameWindow;
        // Menu requested before the page said "ready": opened when it does, or reported after the deadline
        private bool _openPending;
        private long _openDeadline;
        private bool _pageTimedOut;
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private const long OpenTimeoutMs = 10000;

        public ChaosScript()
        {
            _config = ChaosConfig.Load();
            _page = new ChaosPage(_config.UiUrl);
            PlayerEffects.Register(_registry);
            PedEffects.Register(_registry);
            WorldEffects.Register(_registry);
            VehicleEffects.Register(_registry);
            MetaEffects.Register(_registry);
            _engine = new ChaosEngine(_registry)
            {
                AutoMode = _config.AutoMode,
                AutoInterval = _config.AutoInterval,
                Experimental = _config.Experimental,
            };
            ChaosLog.Info("StreamEmber Chaos Mod (RDR2) " + ChaosPage.ProductVersion + ", " + _registry.All.Count + " effects, page " + _page.Url);

            Tick += OnTick;
            Aborted += (s, e) => Guard.Run("Shutdown", Shutdown);
        }

        private void OnTick(object sender, EventArgs e)
        {
            try
            {
                TickGuarded();
            }
            catch (Exception ex)
            {
                // Never let an exception out of Tick: the runtime would stop the script
                ChaosLog.Error("Tick", ex);
            }
        }

        private void TickGuarded()
        {
            OverlayState state = OverlayBridge.State;
            if (state == OverlayState.NotInstalled && !_announcedNotInstalled)
            {
                _announcedNotInstalled = true;
                ChaosLog.Warn("StreamEmber Overlay not installed or not compatible (needs overlay API " + OverlayBridge.ApiVersion + "). The menu is unavailable.");
                Guard.Run("Notify", () => RDR2.UI.Screen.DisplaySubtitle("StreamEmber Chaos Mod: StreamEmber Overlay kurulu değil ya da sürümü uyumsuz."));
            }

            if (state == OverlayState.Ready)
            {
                if (_page.IsCurrent)
                {
                    Guard.Run("Messages", ReadMessages);
                }
                else if (Ui.Ready || _menuOpen)
                {
                    // Another script (the trainer) loaded its page: it owns the overlay and its input mode now
                    ChaosLog.Info("The overlay switched to " + OverlayBridge.Url + "; chaos menu detached");
                    Ui.Ready = false;
                    _menuOpen = false;
                }
            }

            int ped = Fx.PlayerPed;
            bool busy = !Fx.Exists(ped) || Game.IsLoading;
            bool dead = Fx.Exists(ped) && Fx.IsDead(ped);
            if (dead && !_wasDead)
            {
                // Death: end everything (script cameras, screen grading, player model) before the game respawns
                ChaosLog.Info("Player died: stopping all effects");
                Guard.Run("StopAll", _engine.StopAll);
            }
            _wasDead = dead;

            // The page is opened once the player is in the world (see the trainers: nothing heavy while loading)
            if (state == OverlayState.Ready && !busy)
            {
                _page.EnsureStartup();
            }

            Guard.Run("MenuKey", PollMenuKey);
            if (_openPending) Guard.Run("PendingMenu", CheckPendingOpen);

            if (_menuOpen && OverlayBridge.InputMode == OverlayInputMode.Ui)
            {
                Game.DisableAllControlsThisFrame();
            }

            if (!busy && !dead)
            {
                Guard.Run("Engine", _engine.Tick);
            }
        }

        private void ReadMessages()
        {
            while (OverlayBridge.TryReceive(out string json))
            {
                if (!(MiniJson.Parse(json) is IDictionary<string, object> msg)) continue;
                IDictionary<string, object> data = msg.Obj("data");
                string cb = msg.Str("cb");
                switch (cb)
                {
                    case "ready":
                        // Only the chaos page (a late message of the previous page must not count)
                        if (data?.Str("app") != "chaos") break;
                        Ui.Ready = true;
                        _pageTimedOut = false;
                        ChaosLog.Info("Chaos page ready");
                        SendInit();
                        SendMenuState();
                        _engine.MarkDirty();
                        if (_openPending)
                        {
                            _openPending = false;
                            SetMenu(true);
                        }
                        break;
                    case "run":
                        if (CanRunEffects()) _engine.Run(data?.Str("id"), "menu");
                        break;
                    case "stop":
                        _engine.Stop(data?.Str("id"));
                        break;
                    case "stopAll":
                        _engine.StopAll();
                        ChaosEngine.Toast("info", "Bütün efektler durduruldu");
                        break;
                    case "random":
                        if (CanRunEffects() && _engine.RunRandom("menu") == null) ChaosEngine.Toast("warn", "Çalıştırılacak efekt kalmadı");
                        break;
                    case "cleanup":
                        int removed = Fx.CleanupSpawned();
                        ChaosEngine.Toast("info", "Temizlendi", removed + " varlık silindi");
                        break;
                    case "setting":
                        ApplySetting(data);
                        break;
                    case "close":
                        SetMenu(false);
                        break;
                }
            }
        }

        /// <summary>No effects while the player is dead, missing or the game is loading.</summary>
        private static bool CanRunEffects()
        {
            int ped = Fx.PlayerPed;
            if (Fx.Exists(ped) && !Fx.IsDead(ped) && !Game.IsLoading) return true;
            ChaosEngine.Toast("warn", "Şu an efekt çalıştırılamaz", "Oyuncu ölü ya da oyun yükleniyor.");
            return false;
        }

        private void ApplySetting(IDictionary<string, object> data)
        {
            if (data == null) return;
            string key = data.Str("key");
            data.TryGetValue("value", out object value);
            switch (key)
            {
                case "auto":
                    _engine.AutoMode = value is bool on && on;
                    break;
                case "interval":
                    if (value is double seconds) _engine.AutoInterval = Math.Max(10, Math.Min(600, (int)seconds));
                    break;
                case "experimental":
                    _engine.Experimental = value is bool exp && exp;
                    break;
            }
            ChaosLog.Info("Setting " + key + " = " + value);
            _engine.MarkDirty();
            SendSettings();
        }

        /// <summary>Menu key edge, read from the keyboard state (works in both input modes), only while the game
        /// window has the focus.</summary>
        private void PollMenuKey()
        {
            bool down = (GetAsyncKeyState((int)_config.MenuKey) & 0x8000) != 0;
            if (down && !_menuKeyDown && IsGameFocused()) ToggleMenu();
            _menuKeyDown = down;
        }

        private void ToggleMenu()
        {
            if (_menuOpen)
            {
                SetMenu(false);
                return;
            }
            if (_openPending)
            {
                // Second press while waiting: give up
                _openPending = false;
                ChaosLog.Info("Menu opening cancelled");
                if (!Ui.Ready) _page.Release();
                RDR2.UI.Screen.DisplaySubtitle("Kaos Modu: menü açma iptal edildi.");
                return;
            }
            if (OverlayBridge.State != OverlayState.Ready)
            {
                ChaosLog.Warn("Menu key pressed, overlay state " + OverlayBridge.State);
                RDR2.UI.Screen.DisplaySubtitle("Kaos Modu: StreamEmber Overlay hazır değil (" + OverlayBridge.State + ").");
                return;
            }
            if (Ui.Ready && _page.IsCurrent)
            {
                SetMenu(true);
                return;
            }
            if (!_page.Available)
            {
                ChaosLog.Warn("Menu key pressed, chaos page missing: " + _page.Url);
                RDR2.UI.Screen.DisplaySubtitle("Kaos Modu: menü dosyası bulunamadı (StreamEmber\\UI\\ChaosMod). Modu yeniden kurun.");
                return;
            }
            // Page not shown (another script's page or still blank) or it never answered: open it, then the menu
            _page.Claim(_page.IsCurrent && _pageTimedOut);
            _openPending = true;
            _openDeadline = _clock.ElapsedMilliseconds + OpenTimeoutMs;
            RDR2.UI.Screen.DisplaySubtitle("Kaos Modu: menü yükleniyor...");
        }

        private void CheckPendingOpen()
        {
            if (_clock.ElapsedMilliseconds < _openDeadline) return;
            _openPending = false;
            _pageTimedOut = true;
            ChaosLog.Warn("The chaos page did not answer within " + OpenTimeoutMs / 1000 + " s: " + _page.Url +
                          " (overlay URL " + OverlayBridge.Url + "). The MHud kit comes from cdn.jsdelivr.net: is the network up?");
            // Whatever is shown instead (an error page) must not stay over the game
            _page.Release();
            RDR2.UI.Screen.DisplaySubtitle("Kaos Modu: menü yüklenemedi. MHud CDN'den gelir, internet bağlantısını kontrol edin (ayrıntı: ChaosMod.log).");
        }

        private bool IsGameFocused()
        {
            if (_gameWindow == IntPtr.Zero) _gameWindow = Process.GetCurrentProcess().MainWindowHandle;
            IntPtr foreground = GetForegroundWindow();
            if (foreground == _gameWindow) return true;
            // The main window handle can change once (splash -> game window): accept our own process' window
            GetWindowThreadProcessId(foreground, out uint pid);
            if (pid != (uint)Process.GetCurrentProcess().Id) return false;
            _gameWindow = foreground;
            return true;
        }

        private void SetMenu(bool open)
        {
            if (_menuOpen == open) return;
            if (open && !(Ui.Ready && _page.IsCurrent))
            {
                // Without the page the UI input mode would swallow every key (only the overlay's F8 gets out)
                ChaosLog.Warn("Menu requested but the chaos page is not ready (" + _page.Url + ")");
                return;
            }
            _menuOpen = open;
            if (open)
            {
                OverlayBridge.Visible = true;
                OverlayBridge.InputMode = OverlayInputMode.Ui;
            }
            else
            {
                OverlayBridge.InputMode = OverlayInputMode.Game;
            }
            ChaosLog.Info(open ? "Menu open" : "Menu closed");
            SendMenuState();
        }

        private void SendMenuState()
        {
            if (!Ui.Ready) return;
            Ui.Begin("chaos:menu").BeginObject().Prop("open", _menuOpen).EndObject();
            Ui.Send();
        }

        private void SendSettings()
        {
            if (!Ui.Ready) return;
            Ui.Begin("chaos:settings").BeginObject()
                .Prop("auto", _engine.AutoMode)
                .Prop("interval", _engine.AutoInterval)
                .Prop("experimental", _engine.Experimental)
                .EndObject();
            Ui.Send();
        }

        /// <summary>Everything the page needs to build the menu: effects, categories, settings, theme, menu key.</summary>
        private void SendInit()
        {
            var w = Ui.Begin("chaos:init").BeginObject()
                .Prop("version", ChaosPage.ProductVersion ?? "dev")
                .Prop("theme", _config.Theme)
                .Prop("menuKey", _config.MenuKey.ToString())
                .Prop("menuKeyCode", (int)_config.MenuKey)
                .Name("settings").BeginObject()
                    .Prop("auto", _engine.AutoMode)
                    .Prop("interval", _engine.AutoInterval)
                    .Prop("experimental", _engine.Experimental)
                .EndObject()
                .Name("effects").BeginArray();
            foreach (EffectDef def in _registry.All)
            {
                w.BeginObject()
                    .Prop("id", def.Id)
                    .Prop("name", def.Name)
                    .Prop("desc", def.Description ?? string.Empty)
                    .Prop("cat", CategoryId(def.Category))
                    .Prop("icon", def.Icon ?? "sparkles")
                    .Prop("dur", def.Duration);
                if (def.Experimental) w.Prop("exp", true);
                if (def.DisguiseId != null) w.Prop("fake", true);
                w.EndObject();
            }
            w.EndArray().EndObject();
            Ui.Send();
        }

        private static string CategoryId(EffectCategory category)
        {
            switch (category)
            {
                case EffectCategory.Player: return "player";
                case EffectCategory.Peds: return "peds";
                case EffectCategory.World: return "world";
                case EffectCategory.Vehicles: return "vehicles";
                default: return "meta";
            }
        }

        private void Shutdown()
        {
            // Aborted runs outside the script loop: Script.Wait/Yield would throw, effects must stop without waiting
            Fx.NoWait = true;
            Guard.Run("StopAll", _engine.StopAll);
            if (_menuOpen) OverlayBridge.InputMode = OverlayInputMode.Game;
            ChaosLog.Info("Shutdown");
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int key);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    }
}
