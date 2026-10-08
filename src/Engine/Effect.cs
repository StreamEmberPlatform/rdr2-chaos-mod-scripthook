// Effect model: a definition (id, texts, category, duration) plus a factory that creates a fresh instance on every
// activation, so effect state (timers, spawned entities, cameras) never leaks between two activations.
using System;
using System.Collections.Generic;

namespace StreamEmber.ChaosMod
{
    internal enum EffectCategory
    {
        Player,
        Peds,
        World,
        Vehicles,
        Meta,
    }

    internal sealed class EffectDef
    {
        /// <summary>Stable id (same as ChaosModRDR, used by the page and later by EventFabric).</summary>
        public string Id;
        public string Name;
        public string Description;
        public EffectCategory Category;
        /// <summary>MHud icon name.</summary>
        public string Icon;
        /// <summary>Seconds; 0 = instant effect.</summary>
        public int Duration;
        /// <summary>Writes version-dependent script globals; runs only when experimental effects are enabled.</summary>
        public bool Experimental;
        /// <summary>Effects of one group exclude each other (player model, script camera, screen grading, weather).</summary>
        public string Group;
        /// <summary>A fake effect shows this name until it reveals itself.</summary>
        public string DisguiseId;
        public Func<EffectInstance> Create;

        public bool IsTimed => Duration > 0;
    }

    /// <summary>One running activation of an effect. All methods run on the script's Tick fiber.</summary>
    internal abstract class EffectInstance
    {
        public EffectDef Def;

        /// <summary>Name shown in the HUD while running (fake effects show a disguise first).</summary>
        public virtual string DisplayName => Def.Name;

        public virtual void Start()
        {
        }

        /// <param name="dtMs">Real milliseconds since the previous tick.</param>
        public virtual void Tick(int dtMs)
        {
        }

        public virtual void Stop()
        {
        }
    }

    /// <summary>Effect written as lambdas (most effects). Use <see cref="Interval"/> fields for periodic work.</summary>
    internal sealed class LambdaEffect : EffectInstance
    {
        private readonly Action _start;
        private readonly Action<int> _tick;
        private readonly Action _stop;

        public LambdaEffect(Action start, Action<int> tick = null, Action stop = null)
        {
            _start = start;
            _tick = tick;
            _stop = stop;
        }

        public override void Start() => _start?.Invoke();
        public override void Tick(int dtMs) => _tick?.Invoke(dtMs);
        public override void Stop() => _stop?.Invoke();
    }

    /// <summary>ChaosModRDR's TimerTick: accumulates time, true once per period.</summary>
    internal sealed class Interval
    {
        private readonly int _periodMs;
        private int _elapsed;

        public Interval(int periodMs, bool fireFirst = false)
        {
            _periodMs = Math.Max(1, periodMs);
            _elapsed = fireFirst ? _periodMs : 0;
        }

        public bool Tick(int dtMs)
        {
            _elapsed += dtMs;
            if (_elapsed < _periodMs) return false;
            _elapsed %= _periodMs;
            return true;
        }
    }

    /// <summary>Collects the effect definitions of all categories.</summary>
    internal sealed class EffectRegistry
    {
        public readonly List<EffectDef> All = new List<EffectDef>();
        private readonly Dictionary<string, EffectDef> _byId = new Dictionary<string, EffectDef>(StringComparer.OrdinalIgnoreCase);

        public EffectDef Get(string id) => id != null && _byId.TryGetValue(id, out EffectDef def) ? def : null;

        public EffectDef Add(EffectDef def)
        {
            if (_byId.ContainsKey(def.Id)) throw new InvalidOperationException("Duplicate effect id " + def.Id);
            _byId[def.Id] = def;
            All.Add(def);
            return def;
        }

        /// <summary>Instant effect: one action.</summary>
        public EffectDef Instant(EffectCategory category, string id, string name, string description, string icon, Action run)
            => Add(new EffectDef
            {
                Id = id, Name = name, Description = description, Category = category, Icon = icon,
                Create = () => new LambdaEffect(run),
            });

        /// <summary>Timed effect; <paramref name="create"/> builds a fresh instance (closures hold the state).</summary>
        public EffectDef Timed(EffectCategory category, string id, string name, string description, string icon, int seconds,
            Func<EffectInstance> create, string group = null)
            => Add(new EffectDef
            {
                Id = id, Name = name, Description = description, Category = category, Icon = icon,
                Duration = seconds, Create = create, Group = group,
            });

        /// <summary>Timed effect that only does something every frame (no state).</summary>
        public EffectDef EveryFrame(EffectCategory category, string id, string name, string description, string icon, int seconds,
            Action tick, Action stop = null)
            => Timed(category, id, name, description, icon, seconds, () => new LambdaEffect(null, _ => tick(), stop));
    }
}
