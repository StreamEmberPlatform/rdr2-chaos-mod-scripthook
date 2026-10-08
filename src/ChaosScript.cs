// StreamEmber Chaos Mod (RDR2) — StreamEmber Runtime (RDR2) script; the menu is an MHud page in the StreamEmber Overlay.
//
//   F6 (ChaosMod.ini MenuKey)  open / close the chaos menu. While it is open the mouse and keyboard go to the page:
//                              pick a category, click an effect, "Çalıştır". Esc or F6 closes it.
//
// No other hotkeys and no integrations in this version: every effect starts from the menu (or from the optional
// automatic mode / Total Chaos). EventFabric will call ChaosEngine.Run(id, "eventfabric") later.
//
// Message flow (same transport as the trainers): C# -> page { action, data } through OverlayBridge.Send,
// page -> C# { cb, data } through window.streamember.post: ready, run, stop, stopAll, random, cleanup, setting, close.
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using RDR2;
using StreamEmber.Overlay;
using StreamEmber.Trainers;

namespace StreamEmber.ChaosMod
{
    public sealed class ChaosScript : Script
    {
        public const string DefaultUiUrl = "https://streamemberplatform.github.io/rdr2-chaos-mod-scripthook/";

        private readonly ChaosConfig _config;
        private readonly ChaosPage _page;
        private readonly EffectRegistry _registry = new EffectRegistry();
        private readonly ChaosEngine _engine;
        private bool _menuOpen;
        private bool _announcedNotInstalled;
        private bool _wasDead;

        public ChaosScript()
        {
            _config = ChaosConfig.Load(DefaultUiUrl);
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
            KeyDown += OnKeyDown;
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
                Guard.Run("Messages", ReadMessages);
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
                _page.Ensure();
            }

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
                        Ui.Ready = true;
                        SendInit();
                        SendMenuState();
                        _engine.MarkDirty();
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

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            Guard.Run("KeyDown", () =>
            {
                if (e.KeyCode == _config.MenuKey) SetMenu(!_menuOpen);
            });
        }

        private void SetMenu(bool open)
        {
            if (_menuOpen == open) return;
            if (open && !Ui.Ready)
            {
                // Without the page the UI input mode would swallow every key (only the overlay's F8 gets out)
                ChaosLog.Warn("Menu requested but the chaos page is not ready (" + _page.Url + ")");
                RDR2.UI.Screen.DisplaySubtitle("Kaos Modu: menü sayfası henüz yüklenmedi (internet bağlantısını ve StreamEmber Overlay'i kontrol edin).");
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
    }
}
