// World effects (ChaosModRDR src/Effects/misc.cpp, without the meta effects).
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using RDR2;
using RDR2.Math;
using RDR2.Native;
using RDR2.UI;
using static StreamEmber.ChaosMod.Fx;

namespace StreamEmber.ChaosMod
{
    internal static class WorldEffects
    {
        private const EffectCategory C = EffectCategory.World;

        // Weather, time-of-day grading and other "sky" effects exclude each other: stopping one restores the sky
        private const string Sky = "sky";
        private const string Camera = "camera";
        private const string Speed = "speed";

        public static void Register(EffectRegistry r)
        {
            // ---- Weather & time ------------------------------------------------------------------------------------
            r.Instant(C, "set_time_morning", "Sabah oldu", "Saat 07:00 olur.", "sunset", () => World.SetClockTime(7));
            r.Instant(C, "set_time_night", "Gece oldu", "Saat 22:00 olur.", "moon", () => World.SetClockTime(22));
            r.Instant(C, "set_random_time", "Rastgele saat", "Saat rastgele bir değere atlar.", "clock", () => World.SetClockTime(Rand(24)));
            r.Instant(C, "set_random_weather", "Rastgele hava", "Hava rastgele değişir.", "cloud", () => SetWeather(Pick(Weathers)));
            r.Instant(C, "set_sunny_weather", "Güneşli hava", "Hava güneşli olur.", "sun", () => SetWeather("SUNNY"));
            r.Instant(C, "set_rainy_weather", "Yağmurlu hava", "Yağmur başlar.", "rain", () => SetWeather("RAIN"));
            r.Instant(C, "set_foggy_weather", "Sisli hava", "Her yeri sis kaplar.", "fog", () => SetWeather("FOG"));

            r.Timed(C, "snowstorm", "Kar fırtınası", "45 saniye boyunca tipi: kar, sert rüzgâr, beyaz örtü.", "snow", 45, () => new LambdaEffect(() =>
            {
                SetWeather("WHITEOUT");
                World.WindSpeed = 50f;
                World.SnowLevel = -1f;
                World.SetSnowCoverageType((eSnowCoverageType)2);
            }, null, () =>
            {
                SetWeather("SUNNY");
                World.WindSpeed = 0f;
                World.SnowLevel = 1f;
                World.SetSnowCoverageType((eSnowCoverageType)0);
            }), Sky);

            r.Timed(C, "thunderstorm", "Gök gürültülü fırtına", "45 saniye boyunca fırtına ve sert rüzgâr.", "storm", 45, () => new LambdaEffect(() =>
            {
                SetWeather("THUNDERSTORM");
                World.WindSpeed = 50f;
            }, null, ClearSky), Sky);

            r.Timed(C, "rapid_weather", "Deli hava", "30 saniye boyunca hava yarım saniyede bir değişir.", "wind", 30, () =>
            {
                var every = new Interval(500);
                return new LambdaEffect(null, dt =>
                {
                    if (every.Tick(dt)) SetWeather(Pick(Weathers));
                }, () => SetWeather("SUNNY"));
            }, Sky);

            r.Timed(C, "timelapse", "Hızlandırılmış zaman", "30 saniye boyunca gün saniyeler içinde akıp geçer.", "hourglass", 30, () =>
            {
                double seconds = 0;
                return new LambdaEffect(() =>
                {
                    SetWeather("SUNNY");
                    seconds = CLOCK.GET_CLOCK_HOURS() * 3600 + CLOCK.GET_CLOCK_MINUTES() * 60 + CLOCK.GET_CLOCK_SECONDS();
                }, dt =>
                {
                    seconds = (seconds + dt / 1000.0 * 10000.0) % (24 * 3600);
                    int s = (int)seconds;
                    World.SetClockTime(s / 3600, s % 3600 / 60, s % 60);
                    World.SetTimecycleModifier("SkyTimelapses01");
                }, World.ClearTimecycleModifier);
            }, Sky);

            r.Timed(C, "rainbow", "Gökkuşağı", "30 saniye boyunca gökyüzünde gökkuşağı.", "sparkles", 30, () => new LambdaEffect(() =>
            {
                SetWeather("FOG");
                World.SetClockTime(8);
                World.SetTimecycleModifier("rainBowMod");
            }, null, World.ClearTimecycleModifier), Sky);

            r.EveryFrame(C, "shades_of_gray", "Griler", "20 saniye boyunca dünya renksizleşir.", "layers", 20,
                () => World.SetTimecycleModifier("PauseMenuDark"), World.ClearTimecycleModifier).Group = Sky;

            r.Timed(C, "doomsday", "Kıyamet", "20 saniye: fırtına, yıldırımlar ve herkesi savuran rüzgâr.", "radiation", 20, () =>
            {
                var every = new Interval(500, true);
                var entities = new HashSet<int>();
                return new LambdaEffect(() =>
                {
                    SetWeather("THUNDERSTORM");
                    World.WindSpeed = 350f;
                    World.SetTimecycleModifier("EagleEyeTest");
                }, dt =>
                {
                    if (every.Tick(dt))
                    {
                        entities.Clear();
                        CollectChaosTargets(entities, 45, 45, 20, 1000);
                        Lightning(RandomAroundPlayer(Rand(100)));
                    }
                    foreach (int e in entities)
                    {
                        if (!Exists(e)) continue;
                        SetVelocity(e, new Vector3((Rand(70) + 5) * Sign(), (Rand(70) + 5) * Sign(), Rand(10)));
                    }
                }, () =>
                {
                    ClearSky();
                    World.ClearTimecycleModifier();
                });
            }, Sky);

            r.Timed(C, "darthquake", "Deprem", "25 saniye boyunca yer sarsılır, her şey savrulur.", "alert", 25, () =>
            {
                var every = new Interval(500, true);
                var entities = new HashSet<int>();
                return new LambdaEffect(null, dt =>
                {
                    if (every.Tick(dt))
                    {
                        entities.Clear();
                        CollectChaosTargets(entities, 45, 45, 20, 1000);
                    }
                    foreach (int e in entities)
                    {
                        if (!Exists(e)) continue;
                        var v = new Vector3((Rand(5) + 1) * Sign(), (Rand(5) + 1) * Sign(), (Rand(7) + 7) * (Coin() ? -1f : 0.5f));
                        SetVelocity(e, v);
                    }
                });
            });

            r.Instant(C, "lightning_once", "Yıldırım düştü", "Birkaç metre ötene yıldırım düşer.", "bolt",
                () => Lightning(RandomAroundPlayer(Rand(5) + 2, false)));

            r.Instant(C, "lightning_enemy", "Düşmana yıldırım", "Yakındaki bir düşmanı yıldırım çarpar.", "bolt", () =>
            {
                int player = PlayerPed;
                foreach (int ped in NearbyPeds(45))
                {
                    if (!Exists(ped)) continue;
                    int rel = Relationship(ped, player);
                    if (rel != 5 && rel != 4) continue;
                    Vector3 p = Pos(ped);
                    ENTITY.SET_ENTITY_HEALTH(ped, 1, 0);
                    Lightning(p);
                    return;
                }
            });

            // ---- Gravity & physics --------------------------------------------------------------------------------
            r.Instant(C, "ragdoll_everyone", "Herkes yere", "Yakındaki herkes (sen de) yere yığılır.", "injured", () =>
            {
                var peds = NearbyPeds(100);
                peds.Add(PlayerPed);
                foreach (int ped in peds) if (Exists(ped)) FixInCutscene(ped);
                Script.Wait(75);
                foreach (int ped in peds) if (Exists(ped)) Ragdoll(ped, 3000);
            });

            r.Instant(C, "launch_peds_up", "NPC'leri fırlat", "Yakındaki herkes havaya fırlar.", "arrow-up", () =>
            {
                int player = PlayerPed;
                int mount = OnMount(player) ? MountOf(player) : 0;
                var peds = NearbyPeds(100);
                foreach (int ped in peds) if (Exists(ped) && ped != mount) FixInCutscene(ped);
                Script.Wait(75);
                foreach (int ped in peds)
                {
                    if (!Exists(ped) || ped == mount) continue;
                    Ragdoll(ped, 5000);
                    Vector3 v = Velocity(ped);
                    SetVelocity(ped, new Vector3(v.X, v.Y, 35f));
                }
            });

            r.Timed(C, "inverted_gravity", "Ters yerçekimi", "10 saniye boyunca her şey yukarı düşer.", "arrow-up", 10, () =>
            {
                var every = new Interval(1000, true);
                var entities = new HashSet<int>();
                return new LambdaEffect(() =>
                {
                    var peds = NearbyPeds(100);
                    peds.Add(PlayerPed);
                    foreach (int ped in peds)
                    {
                        if (!Exists(ped)) continue;
                        FixInCutscene(ped);
                        Ragdoll(ped, 10000);
                    }
                }, dt =>
                {
                    if (every.Tick(dt))
                    {
                        entities.Clear();
                        foreach (int ped in NearbyPeds(45)) entities.Add(ped);
                        foreach (int veh in NearbyVehicles(45)) entities.Add(veh);
                        foreach (int prop in NearbyProps(45))
                        {
                            MakeDynamic(prop);
                            entities.Add(prop);
                        }
                        entities.Add(PlayerPed);
                    }
                    foreach (int e in entities)
                    {
                        if (!Exists(e)) continue;
                        if (IsPed(e)) Ragdoll(e, 3000);
                        ApplyForceCenter(e, new Vector3(0f, 0f, 25f));
                    }
                });
            });

            r.Timed(C, "insane_gravity", "Aşırı yerçekimi", "20 saniye boyunca her şey yere yapışır.", "arrow-down", 20, () =>
            {
                var every = new Interval(1000, true);
                var entities = new HashSet<int>();
                return new LambdaEffect(null, dt =>
                {
                    if (every.Tick(dt))
                    {
                        entities.Clear();
                        int player = PlayerPed;
                        var peds = NearbyPeds(50);
                        peds.Add(player);
                        var vehs = NearbyVehicles(20);
                        if (PED.IS_PED_IN_ANY_VEHICLE(player, false)) vehs.Add(VehicleOf(player));
                        foreach (int ped in peds)
                        {
                            if (!PED.IS_PED_RAGDOLL(ped)) FixInCutscene(ped);
                            PED.SET_PED_GRAVITY(ped, true);
                            Ragdoll(ped, 5000);
                            entities.Add(ped);
                        }
                        foreach (int veh in vehs) entities.Add(veh);
                        foreach (int prop in NearbyProps(20)) entities.Add(prop);
                    }
                    foreach (int e in entities)
                    {
                        if (!Exists(e)) continue;
                        MakeDynamic(e);
                        ApplyForceCenter(e, new Vector3(0f, 0f, -200f));
                    }
                });
            });

            r.Timed(C, "gravity_field", "Çekim alanı", "20 saniye boyunca etraftaki her şey sana doğru çekilir.", "grab", 20, () =>
            {
                var every = new Interval(1000, true);
                var entities = new HashSet<int>();
                return new LambdaEffect(null, dt =>
                {
                    if (every.Tick(dt))
                    {
                        entities.Clear();
                        foreach (int ped in NearbyPeds(30))
                        {
                            if (!PED.IS_PED_RAGDOLL(ped)) FixInCutscene(ped);
                            Ragdoll(ped, 2000);
                            entities.Add(ped);
                        }
                        foreach (int veh in NearbyVehicles(30)) entities.Add(veh);
                        foreach (int prop in NearbyProps(20))
                        {
                            MakeDynamic(prop);
                            entities.Add(prop);
                        }
                    }
                    Vector3 center = Pos(PlayerPed);
                    center.Z += 1f;
                    foreach (int e in entities)
                    {
                        if (!Exists(e)) continue;
                        ApplyForceCenter(e, Normalize(center - Pos(e)) * 70f);
                    }
                });
            });

            r.Timed(C, "teleport_everyithing", "Her şey bana gelsin", "25 saniye: yakındaki NPC'ler, arabalar ve eşyalar üstüne ışınlanır; eşyalar sonra yerine döner.", "spawn", 25, () =>
            {
                var props = new List<KeyValuePair<int, Vector3>>();
                return new LambdaEffect(() =>
                {
                    Vector3 target = Pos(PlayerPed);
                    var entities = NearbyPeds(45);
                    entities.AddRange(NearbyVehicles(10));
                    foreach (int e in entities)
                    {
                        if (!Exists(e)) continue;
                        SetPos(e, target);
                        if (IsPed(e)) Ragdoll(e, 3000);
                    }
                    foreach (int prop in NearbyProps(10))
                    {
                        props.Add(new KeyValuePair<int, Vector3>(prop, Pos(prop)));
                        MakeDynamic(prop);
                        SetPos(prop, target);
                    }
                }, null, () =>
                {
                    foreach (KeyValuePair<int, Vector3> prop in props)
                    {
                        if (!Exists(prop.Key)) continue;
                        SetPos(prop.Key, prop.Value);
                        ENTITY.SET_ENTITY_DYNAMIC(prop.Key, false);
                        ENTITY.SET_ENTITY_HAS_GRAVITY(prop.Key, true);
                    }
                });
            });

            // ---- Peds --------------------------------------------------------------------------------------------
            r.Timed(C, "giant_peds", "Devler ülkesi", "45 saniye boyunca yakındaki herkes beş kat büyür.", "user-plus", 45, () =>
            {
                var every = new Interval(1000);
                var giants = new HashSet<int>();
                void Grow()
                {
                    foreach (int ped in NearbyPeds(45))
                    {
                        if (!Exists(ped)) continue;
                        SetScale(ped, 5f);
                        giants.Add(ped);
                    }
                }
                return new LambdaEffect(Grow, dt =>
                {
                    if (every.Tick(dt)) Grow();
                }, () =>
                {
                    foreach (int ped in giants) if (Exists(ped)) SetScale(ped, 1f);
                });
            });

            r.Timed(C, "peds_wanna_kill_player", "Herkes seni öldürmek istiyor", "25 saniye boyunca yakındaki herkes revolverle sana saldırır.", "enemy", 25, () =>
            {
                var every = new Interval(1000);
                uint enemies = 0;
                void Turn(bool arm)
                {
                    int player = PlayerPed;
                    foreach (int ped in NearbyPeds(45))
                    {
                        if (!Exists(ped)) continue;
                        if (arm) GiveWeapon(ped, "WEAPON_REVOLVER_SCHOFIELD", 100);
                        PED.SET_PED_RELATIONSHIP_GROUP_HASH(ped, enemies);
                        PED.SET_PED_COMBAT_ATTRIBUTES(ped, 5, true);
                        PED.SET_PED_COMBAT_ATTRIBUTES(ped, 46, true);
                        PED.SET_PED_FLEE_ATTRIBUTES(ped, 2, true);
                        TASK.TASK_COMBAT_PED(ped, player, 0, 16);
                    }
                }
                return new LambdaEffect(() =>
                {
                    enemies = AddRelationshipGroup("_CHAOS_ENEMY_PEDS");
                    uint playerGroup = H("PLAYER");
                    PED.SET_RELATIONSHIP_BETWEEN_GROUPS(5, enemies, playerGroup);
                    PED.SET_RELATIONSHIP_BETWEEN_GROUPS(5, playerGroup, enemies);
                    Turn(true);
                }, dt =>
                {
                    if (every.Tick(dt)) Turn(false);
                }, () =>
                {
                    uint playerGroup = H("PLAYER");
                    PED.SET_RELATIONSHIP_BETWEEN_GROUPS(3, enemies, playerGroup);
                    PED.SET_RELATIONSHIP_BETWEEN_GROUPS(3, playerGroup, enemies);
                });
            });

            r.Timed(C, "invincible_everyone", "Herkes ölümsüz", "30 saniye boyunca kimse ölmez (sen de).", "shield", 30, () =>
            {
                var every = new Interval(1000, true);
                var peds = new HashSet<int>();
                return new LambdaEffect(null, dt =>
                {
                    if (!every.Tick(dt)) return;
                    var nearby = NearbyPeds(45);
                    nearby.Add(PlayerPed);
                    foreach (int ped in nearby)
                    {
                        // Only peds that could be damaged: their invincibility is ours to take back
                        if (peds.Contains(ped) || !ENTITY._GET_ENTITY_CAN_BE_DAMAGED(ped)) continue;
                        ENTITY.SET_ENTITY_INVINCIBLE(ped, true);
                        peds.Add(ped);
                    }
                }, () =>
                {
                    foreach (int ped in peds) if (Exists(ped)) ENTITY.SET_ENTITY_INVINCIBLE(ped, false);
                });
            });

            r.Timed(C, "one_hit_ko", "Tek vuruş", "30 saniye boyunca herkesin canı 1 (sen de).", "heart-broken", 30, () =>
            {
                var every = new Interval(500, true);
                var peds = new HashSet<int>();
                return new LambdaEffect(null, dt =>
                {
                    if (!every.Tick(dt)) return;
                    var nearby = NearbyPeds(50);
                    nearby.Add(PlayerPed);
                    foreach (int ped in nearby)
                    {
                        peds.Add(ped);
                        if (!PED.IS_PED_DEAD_OR_DYING(ped, true)) ENTITY.SET_ENTITY_HEALTH(ped, 1, 0);
                    }
                }, () =>
                {
                    foreach (int ped in peds)
                    {
                        if (Exists(ped) && !IsDead(ped)) ENTITY.SET_ENTITY_HEALTH(ped, ENTITY.GET_ENTITY_MAX_HEALTH(ped, true), 0);
                    }
                    ATTRIBUTE._SET_ATTRIBUTE_CORE_VALUE(PlayerPed, 0, 100);
                });
            });

            r.Timed(C, "ghost_town", "Hayalet kasaba", "20 saniye boyunca etraftaki insanlar ve arabalar görünmez olur.", "ghost", 20, () =>
            {
                var every = new Interval(1000, true);
                var hidden = new HashSet<int>();
                return new LambdaEffect(null, dt =>
                {
                    if (!every.Tick(dt)) return;
                    var entities = NearbyVehicles(40);
                    entities.AddRange(NearbyPeds(40));
                    foreach (int e in entities)
                    {
                        if (!Exists(e)) continue;
                        hidden.Add(e);
                        ENTITY.SET_ENTITY_VISIBLE(e, false);
                    }
                }, () =>
                {
                    hidden.Add(PlayerPed);
                    foreach (int e in hidden) if (Exists(e)) ENTITY.SET_ENTITY_VISIBLE(e, true);
                });
            });

            r.Timed(C, "potato_mode", "Patates modu", "25 saniye boyunca karakterler en düşük detayda görünür.", "gamepad", 25, () =>
            {
                var every = new Interval(500, true);
                var peds = new HashSet<int>();
                return new LambdaEffect(null, dt =>
                {
                    if (every.Tick(dt))
                    {
                        new Ped(PlayerPed).LodMultiplier = 0.06f;
                        foreach (int ped in NearbyPeds(75)) peds.Add(ped);
                    }
                    foreach (int ped in peds) if (Exists(ped)) PED.SET_PED_LOD_MULTIPLIER(ped, 0.07f);
                }, () =>
                {
                    foreach (int ped in peds) if (Exists(ped)) PED.SET_PED_LOD_MULTIPLIER(ped, 1f);
                    PED.SET_PED_LOD_MULTIPLIER(PlayerPed, 1f);
                });
            });

            r.Instant(C, "ignite_nearby_peds", "NPC'leri tutuştur", "Yakındaki insanlar alev alır (atlar hariç).", "flame", () =>
            {
                foreach (int ped in NearbyPeds(45))
                {
                    if (!IsHorse(ped)) new Ped(ped).Ignite();
                }
            });

            r.Instant(C, "remove_weapons_everyone", "Herkes silahsız", "Yakındaki herkesin (senin de) silahları alınır.", "ban", () =>
            {
                var peds = NearbyPeds(45);
                peds.Add(PlayerPed);
                foreach (int ped in peds)
                {
                    if (Exists(ped) && !IsHorse(ped)) RemoveAllWeapons(ped);
                }
            });

            r.Instant(C, "give_everyone_rifle", "Herkese tüfek", "Yakındaki herkese (sana da) karabina verilir.", "rifle", () =>
            {
                var peds = NearbyPeds(50);
                peds.Add(PlayerPed);
                foreach (int ped in peds)
                {
                    if (IsHuman(ped)) GiveWeapon(ped, "WEAPON_REPEATER_CARBINE", 100);
                }
            });

            r.Timed(C, "pig_weapons", "Domuz mermisi", "30 saniye boyunca silahlar domuz fırlatır.", "meat", 30, () => new PigWeapons());

            r.Timed(C, "raining_pigs", "Gökten domuz yağıyor", "25 saniye boyunca gökten domuz yağar.", "meat", 25, () =>
            {
                var every = new Interval(1000);
                var pigs = new List<int>();
                return new LambdaEffect(null, dt =>
                {
                    if (!every.Tick(dt)) return;
                    Vector3 p = RandomAroundPlayer(Rand(20));
                    int pig = SpawnPed("A_C_Pig_01", false, false);
                    if (pig == 0) return;
                    SetPos(pig, new Vector3(p.X, p.Y, p.Z + 35f));
                    ENTITY.SET_ENTITY_INVINCIBLE(pig, true);
                    SetScale(pig, Rand(5) + 1);
                    OutfitPreset(pig, Rand(4));
                    Ragdoll(pig, 10000);
                    PED.SET_RAGDOLL_BLOCKING_FLAGS(pig, 512);
                    SetVelocity(pig, new Vector3(0f, 0f, -50f));
                    TASK.TASK_SMART_FLEE_PED(pig, PlayerPed, 5000f, -1, 0, 3f, 0);
                    pigs.Add(pig);
                }, () =>
                {
                    foreach (int pig in pigs) DeletePed(pig);
                });
            });

            // ---- Spawns ------------------------------------------------------------------------------------------
            r.Instant(C, "spawn_hotchkiss_cannon", "Hotchkiss topu", "Yanında bir Hotchkiss topu belirir (yayaysan başına geçersin).", "target", () =>
            {
                int player = PlayerPed;
                int cannon = SpawnVehicle("hotchkiss_cannon", Pos(player), Heading(player));
                if (cannon == 0) return;
                if (!InVehicle(player) && !OnMount(player)) SetIntoVehicle(player, cannon, -1);
            });

            r.Timed(C, "spawn_ufo", "UFO", "25 saniye: gece, sis ve insanları çeken bir UFO; sonunda patlar.", "alien", 25, () => new Ufo(), Sky);

            // ---- Screen, camera, speed ---------------------------------------------------------------------------
            r.Timed(C, "play_intro", "Oyun girişi", "25 saniye boyunca oyunun açılış efekti oynar.", "play", 25, () => new LambdaEffect(
                () => PostFx("Title_GameIntro"), null, () =>
                {
                    StopPostFx("Title_GameIntro");
                    PLAYER.SET_PLAYER_CONTROL(PlayerId, true, 0, false);
                    HUD.DISPLAY_HUD(true);
                    MAP.DISPLAY_RADAR(true);
                }));

            r.EveryFrame(C, "no_hud", "HUD yok", "30 saniye boyunca oyunun HUD'u ve radarı gizlenir.", "eye-off", 30, Hud.HideThisFrame);

            r.Timed(C, "fov_120", "FOV 120", "15 saniye boyunca görüş açısı 120 derece.", "focus", 15, () =>
            {
                var cam = new ScriptCam();
                return new LambdaEffect(cam.Create, dt => cam.Follow(CAM.GET_GAMEPLAY_CAM_ROT(2), 120f), cam.Destroy);
            }, Camera);

            r.Timed(C, "upside_down_camera", "Ters kamera", "30 saniye boyunca dünya baş aşağı.", "refresh", 30, () =>
            {
                var cam = new ScriptCam();
                return new LambdaEffect(cam.Create, dt =>
                {
                    Vector3 rot = CAM.GET_GAMEPLAY_CAM_ROT(2);
                    cam.Follow(new Vector3(0f, 180f, rot.Z), CAM.GET_GAMEPLAY_CAM_FOV());
                }, cam.Destroy);
            }, Camera);

            r.Timed(C, "gamespeed_02", "Ağır çekim x0.2", "25 saniye boyunca oyun beş kat yavaş.", "timer", 25, () => GameSpeed(0.2f), Speed);
            r.Timed(C, "gamespeed_05", "Ağır çekim x0.5", "25 saniye boyunca oyun yarı hızda.", "timer", 25, () => GameSpeed(0.5f), Speed);

            r.Instant(C, "alt_tab", "Alt + Tab", "Windows'a Alt+Tab gönderir: oyun bir anlığına arka plana düşer.", "keyboard", AltTab);
        }

        private static EffectInstance GameSpeed(float scale)
            => new LambdaEffect(() => MISC.SET_TIME_SCALE(scale), null, () => MISC.SET_TIME_SCALE(1f));

        private static void ClearSky()
        {
            SetWeather("SUNNY");
            World.WindSpeed = 0f;
        }

        /// <summary>Peds (+player, ragdolled), vehicles and dynamic props near the player.</summary>
        private static void CollectChaosTargets(HashSet<int> into, int peds, int vehicles, int props, int ragdollMs)
        {
            var list = NearbyPeds(peds);
            list.Add(PlayerPed);
            foreach (int ped in list)
            {
                if (!Exists(ped)) continue;
                if (!PED.IS_PED_RAGDOLL(ped)) FixInCutscene(ped);
                Ragdoll(ped, ragdollMs);
                into.Add(ped);
            }
            foreach (int veh in NearbyVehicles(vehicles)) into.Add(veh);
            foreach (int prop in NearbyProps(props))
            {
                MakeDynamic(prop);
                into.Add(prop);
            }
        }

        #region Alt+Tab

        // INPUT on x64: type at 0, the KEYBDINPUT/MOUSEINPUT union at 8, 40 bytes in total
        [StructLayout(LayoutKind.Explicit, Size = 40)]
        private struct KeyboardInput
        {
            [FieldOffset(0)] public uint Type;
            [FieldOffset(8)] public ushort Vk;
            [FieldOffset(10)] public ushort Scan;
            [FieldOffset(12)] public uint Flags;
            [FieldOffset(16)] public uint Time;
            [FieldOffset(24)] public IntPtr ExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint count, KeyboardInput[] inputs, int size);

        private static void AltTab()
        {
            const uint InputKeyboard = 1, KeyUp = 2;
            const ushort Alt = 0x12, Tab = 0x09;
            var inputs = new[]
            {
                new KeyboardInput { Type = InputKeyboard, Vk = Alt },
                new KeyboardInput { Type = InputKeyboard, Vk = Tab },
                new KeyboardInput { Type = InputKeyboard, Vk = Tab, Flags = KeyUp },
                new KeyboardInput { Type = InputKeyboard, Vk = Alt, Flags = KeyUp },
            };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(KeyboardInput)));
        }

        #endregion

        /// <summary>Every shot fires a small invincible pig in the shot's direction.</summary>
        private sealed class PigWeapons : EffectInstance
        {
            private readonly Interval _refresh = new Interval(2000, true);
            private readonly List<int> _shooters = new List<int>();
            private readonly HashSet<int> _pigs = new HashSet<int>();
            private readonly List<(int pig, Vector3 velocity, long until)> _flying = new List<(int, Vector3, long)>();
            private readonly Dictionary<int, Vector3> _lastImpact = new Dictionary<int, Vector3>();
            private readonly System.Diagnostics.Stopwatch _clock = System.Diagnostics.Stopwatch.StartNew();

            public override void Tick(int dtMs)
            {
                if (_refresh.Tick(dtMs))
                {
                    _shooters.Clear();
                    _shooters.AddRange(NearbyPeds(20));
                    _shooters.Add(PlayerPed);
                }

                long now = _clock.ElapsedMilliseconds;
                for (int i = _flying.Count - 1; i >= 0; i--)
                {
                    var f = _flying[i];
                    if (now > f.until || !Exists(f.pig))
                    {
                        _flying.RemoveAt(i);
                        continue;
                    }
                    Ragdoll(f.pig, 1000);
                    SetVelocity(f.pig, f.velocity);
                }

                uint unarmed = H("WEAPON_UNARMED");
                uint pigModel = H("A_C_Pig_01");
                bool loaded = false;
                foreach (int ped in _shooters)
                {
                    if (!Exists(ped) || CurrentWeapon(ped) == unarmed) continue;
                    if (!LastImpact(ped, out Vector3 hit) || !PED.IS_PED_SHOOTING(ped)) continue;
                    if (_lastImpact.TryGetValue(ped, out Vector3 prev) && prev == hit) continue;
                    _lastImpact[ped] = hit;

                    Vector3 from = Pos(ped);
                    from.Z += 0.25f;
                    Vector3 forward = ENTITY.GET_ENTITY_FORWARD_VECTOR(ped);
                    from.X += forward.X * 0.5f;
                    from.Y += forward.Y * 0.5f;
                    Vector3 velocity = Normalize(hit - from) * 50f;

                    if (!loaded)
                    {
                        if (!LoadModel(pigModel)) return;
                        loaded = true;
                    }
                    int pig = PED.CREATE_PED(pigModel, from.X, from.Y, from.Z, Heading(ped), true, false, false, false);
                    if (!Exists(pig)) continue;
                    ENTITY.SET_ENTITY_VISIBLE(pig, true);
                    ENTITY.SET_ENTITY_INVINCIBLE(pig, true);
                    PED.SET_PED_CAN_RAGDOLL(pig, true);
                    OutfitPreset(pig, Rand(4));
                    SetScale(pig, 0.4f);
                    _pigs.Add(pig);
                    SpawnedPeds.Add(pig);
                    _flying.Add((pig, velocity, now + 500));
                }
                if (loaded) ReleaseModel(pigModel);
            }

            public override void Stop()
            {
                foreach (int pig in _pigs) DeletePed(pig);
                _pigs.Clear();
                _flying.Clear();
            }
        }

        /// <summary>A spinning UFO that dashes around and pulls towards random people, then explodes.</summary>
        private sealed class Ufo : EffectInstance
        {
            private readonly Interval _move = new Interval(500);
            private int _ufo;
            private float _heading;
            private int _moves;

            public override void Start()
            {
                World.SetClockTime(2);
                SetWeather("FOG");
                _ufo = SpawnObject(0xC92962E3);
                if (_ufo == 0) return;
                MakeDynamic(_ufo);
                Vector3 p = ENTITY.GET_ENTITY_COORDS(_ufo, true, true);
                p.Z += 2f;
                SetPos(_ufo, p);
                AddBlip(_ufo, "BLIP_STYLE_FRIENDLY");
                World.SetTimecycleModifier("PLayerSpottedDark");
                AUDIO.PLAY_SOUND_FROM_ENTITY("Loop_A", _ufo, "Ufos_Sounds", false, 0, 0);
            }

            public override void Tick(int dtMs)
            {
                if (!Exists(_ufo)) return;
                ENTITY.SET_ENTITY_ROTATION(_ufo, 0f, 0f, 0f, 2, false);
                _heading = (_heading + dtMs / 1000f * 2000f) % 360f;
                SetHeading(_ufo, _heading);
                if (!_move.Tick(dtMs)) return;

                var velocity = new Vector3(175f * Sign(), 175f * Sign(), 5f * Sign());
                if (_moves % 2 == 1)
                {
                    var candidates = new List<int>();
                    var peds = NearbyPeds(50);
                    peds.Add(PlayerPed);
                    foreach (int ped in peds)
                    {
                        if (Exists(ped) && IsHuman(ped)) candidates.Add(ped);
                    }
                    if (candidates.Count > 0)
                    {
                        int target = Pick(candidates);
                        velocity = Normalize(ENTITY.GET_ENTITY_COORDS(target, true, true) - ENTITY.GET_ENTITY_COORDS(_ufo, true, true)) * 100f;
                    }
                }
                _moves++;
                SetVelocity(_ufo, velocity);
            }

            public override void Stop()
            {
                if (Exists(_ufo))
                {
                    Explosion(Pos(_ufo));
                    DeleteObject(_ufo);
                }
                _ufo = 0;
                World.ClearTimecycleModifier();
            }
        }
    }
}
