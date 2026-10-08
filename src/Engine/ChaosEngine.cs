// Chaos engine: runs effects chosen in the menu (or by the optional automatic mode), keeps timed effects ticking,
// ends them on time and tells the page what is active.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using StreamEmber.Trainers;

namespace StreamEmber.ChaosMod
{
    internal sealed class ActiveEffect
    {
        public EffectDef Def;
        public EffectInstance Instance;
        public long StartMs;
        public long EndMs;
        /// <summary>Instant effects stay in the HUD list for a few seconds only.</summary>
        public bool Instant;
    }

    internal sealed class ChaosEngine
    {
        private const int InstantShowMs = 6000;
        private const int RecentMemory = 12;

        public readonly EffectRegistry Registry;
        private readonly List<ActiveEffect> _active = new List<ActiveEffect>();
        private readonly LinkedList<string> _recent = new LinkedList<string>();
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private long _lastTick;
        private long _nextAuto;
        private long _nextPublish;
        private bool _dirty = true;
        private bool _publishedEmpty;

        /// <summary>A random effect every <see cref="AutoInterval"/> s (Total Chaos: every 15 s).</summary>
        public bool AutoMode;
        public int AutoInterval = 45;
        public bool Experimental;

        public ChaosEngine(EffectRegistry registry)
        {
            Registry = registry;
        }

        public long Now => _clock.ElapsedMilliseconds;

        public IReadOnlyList<ActiveEffect> Active => _active;

        public bool IsActive(string id)
        {
            foreach (ActiveEffect a in _active)
            {
                if (!a.Instant && string.Equals(a.Def.Id, id, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private ActiveEffect FindTimed(EffectDef def)
        {
            foreach (ActiveEffect a in _active)
            {
                if (!a.Instant && a.Def == def) return a;
            }
            return null;
        }

        /// <summary>Starts an effect. A timed effect that already runs gets its full duration again.</summary>
        public bool Run(string id, string source)
        {
            EffectDef def = Registry.Get(id);
            if (def == null)
            {
                ChaosLog.Warn("Unknown effect '" + id + "' (" + source + ")");
                return false;
            }
            if (def.Experimental && !Experimental)
            {
                Toast("warn", "Deneysel efekt kapalı", def.Name + " oyun sürümüne bağlı global yazar. Ayarlar'dan açılabilir.");
                return false;
            }

            if (def.IsTimed)
            {
                ActiveEffect running = FindTimed(def);
                if (running != null)
                {
                    running.EndMs = Now + def.Duration * 1000L;
                    _dirty = true;
                    Toast("info", running.Instance.DisplayName, "Süre yenilendi (" + def.Duration + " sn)");
                    return true;
                }
                if (def.Group != null)
                {
                    foreach (ActiveEffect other in _active.ToArray())
                    {
                        if (!other.Instant && other.Def.Group == def.Group) StopEffect(other);
                    }
                }
            }

            EffectInstance instance;
            try
            {
                instance = def.Create();
                instance.Def = def;
            }
            catch (Exception ex)
            {
                ChaosLog.Error("Effect " + def.Id + " create", ex);
                return false;
            }

            ChaosLog.Info("Run " + def.Id + " (" + source + ")");
            var active = new ActiveEffect
            {
                Def = def,
                Instance = instance,
                StartMs = Now,
                Instant = !def.IsTimed,
            };
            // Effects may yield (model streaming, short waits): register first so the HUD shows it at once
            _active.Add(active);
            _dirty = true;
            bool started = Guard.Run("Effect " + def.Id + " start", instance.Start);
            active.StartMs = Now;
            active.EndMs = Now + (def.IsTimed ? def.Duration * 1000L : InstantShowMs);
            if (!started)
            {
                _active.Remove(active);
                if (def.IsTimed) Guard.Run("Effect " + def.Id + " stop", instance.Stop);
                Toast("danger", def.Name, "Efekt başlatılamadı (ayrıntı: ChaosMod.log)");
                return false;
            }

            Remember(def.Id);
            Toast(def.Category == EffectCategory.Meta ? "gold" : "accent", instance.DisplayName,
                def.IsTimed ? def.Duration + " sn" : null, def.Icon);

            // Combo Time: every effect brings two more
            if (source != "combo" && def.Category != EffectCategory.Meta && IsActive("combo_time"))
            {
                for (int i = 0; i < 2; i++) RunRandom("combo");
            }
            return true;
        }

        /// <summary>Runs a random effect that is not running and was not used recently.</summary>
        public EffectDef RunRandom(string source)
        {
            var candidates = new List<EffectDef>();
            foreach (EffectDef def in Registry.All)
            {
                if (def.Category == EffectCategory.Meta) continue;
                if (def.Experimental && !Experimental) continue;
                if (def.IsTimed && FindTimed(def) != null) continue;
                if (_recent.Contains(def.Id)) continue;
                candidates.Add(def);
            }
            if (candidates.Count == 0) return null;
            EffectDef pick = Fx.Pick(candidates);
            return Run(pick.Id, source) ? pick : null;
        }

        public void Stop(string id)
        {
            foreach (ActiveEffect a in _active.ToArray())
            {
                if (!a.Instant && string.Equals(a.Def.Id, id, StringComparison.OrdinalIgnoreCase)) StopEffect(a);
            }
        }

        public void StopAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                StopEffect(_active[i]);
            }
            _active.Clear();
            _dirty = true;
        }

        private void StopEffect(ActiveEffect a)
        {
            if (!_active.Remove(a)) return;
            _dirty = true;
            if (!a.Instant)
            {
                ChaosLog.Info("Stop " + a.Def.Id);
                Guard.Run("Effect " + a.Def.Id + " stop", a.Instance.Stop);
            }
        }

        private void Remember(string id)
        {
            _recent.Remove(id);
            _recent.AddFirst(id);
            while (_recent.Count > RecentMemory) _recent.RemoveLast();
        }

        public void Tick()
        {
            long now = Now;
            int dt = (int)Math.Max(0, Math.Min(1000, now - _lastTick));
            _lastTick = now;

            foreach (ActiveEffect a in _active.ToArray())
            {
                if (!_active.Contains(a)) continue;
                if (now >= a.EndMs)
                {
                    StopEffect(a);
                    continue;
                }
                if (!a.Instant)
                {
                    Guard.Run("Effect " + a.Def.Id + " tick", () => a.Instance.Tick(dt));
                }
            }

            bool totalChaos = IsActive("total_chaos");
            if (AutoMode || totalChaos)
            {
                int interval = totalChaos ? 15 : Math.Max(10, AutoInterval);
                if (_nextAuto == 0 || _nextAuto > now + interval * 1000L)
                {
                    _nextAuto = now + interval * 1000L;
                }
                else if (now >= _nextAuto)
                {
                    _nextAuto = now + interval * 1000L;
                    RunRandom("auto");
                }
            }
            else
            {
                _nextAuto = 0;
            }

            Publish(now);
        }

        /// <summary>Sends the active list to the page (5x a second while something runs, once when it empties).</summary>
        private void Publish(long now)
        {
            if (!Ui.Ready) return;
            bool any = _active.Count > 0 || _nextAuto != 0;
            if (!any && _publishedEmpty && !_dirty) return;
            if (!_dirty && now < _nextPublish) return;
            _nextPublish = now + 200;
            _dirty = false;
            _publishedEmpty = !any;

            var w = Ui.Begin("chaos:active").BeginObject().Name("items").BeginArray();
            foreach (ActiveEffect a in _active)
            {
                w.BeginObject()
                    .Prop("id", a.Def.Id)
                    .Prop("name", a.Instance.DisplayName)
                    .Prop("icon", a.Def.Icon ?? "sparkles")
                    .Prop("instant", a.Instant)
                    .Prop("meta", a.Def.Category == EffectCategory.Meta)
                    .Prop("left", (int)Math.Max(0, a.EndMs - now))
                    .Prop("total", (int)Math.Max(1, a.EndMs - a.StartMs))
                    .EndObject();
            }
            w.EndArray();
            w.Name("auto").BeginObject()
                .Prop("on", AutoMode)
                .Prop("forced", _nextAuto != 0 && !AutoMode)
                .Prop("interval", AutoInterval)
                .Prop("next", _nextAuto == 0 ? 0 : (int)Math.Max(0, _nextAuto - now))
                .EndObject();
            w.EndObject();
            Ui.Send();
        }

        public void MarkDirty() => _dirty = true;

        public static void Toast(string tone, string title, string text = null, string icon = null)
        {
            if (!Ui.Ready) return;
            var w = Ui.Begin("chaos:toast").BeginObject().Prop("tone", tone).Prop("title", title ?? string.Empty);
            if (text != null) w.Prop("text", text);
            if (icon != null) w.Prop("icon", icon);
            w.EndObject();
            Ui.Send();
        }
    }
}
