// Game helpers shared by the effects (ChaosModRDR's SpawnPedAroundPlayer, MarkPedAsEnemy, GetNearbyPeds, ... in C#).
// Handles are plain ints; everything runs on the script's Tick fiber.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace StreamEmber.ChaosMod
{
    internal static class Fx
    {
        public static readonly System.Random Rng = new System.Random();

        /// <summary>
        /// True while the script shuts down (Aborted): Script.Wait/Yield throw there, so helpers must not wait.
        /// </summary>
        public static bool NoWait;

        public static int Rand(int max) => max <= 0 ? 0 : Rng.Next(max);
        public static bool Coin() => Rng.Next(2) == 0;
        public static float Sign() => Rng.Next(2) == 0 ? -1f : 1f;
        public static T Pick<T>(IList<T> items) => items[Rng.Next(items.Count)];

        private static readonly Dictionary<string, uint> HashCache = new Dictionary<string, uint>(StringComparer.Ordinal);

        /// <summary>Joaat hash (cached), e.g. H("WEAPON_LASSO").</summary>
        public static uint H(string name)
        {
            if (!HashCache.TryGetValue(name, out uint hash))
            {
                hash = MISC.GET_HASH_KEY(name);
                HashCache[name] = hash;
            }
            return hash;
        }

        #region Player & entities

        public static int PlayerPed => PLAYER.PLAYER_PED_ID();
        public static int PlayerId => PLAYER.PLAYER_ID();

        public static bool Exists(int entity) => entity != 0 && ENTITY.DOES_ENTITY_EXIST(entity);
        public static Vector3 Pos(int entity) => ENTITY.GET_ENTITY_COORDS(entity, true, false);
        public static void SetPos(int entity, Vector3 p) => ENTITY.SET_ENTITY_COORDS(entity, p.X, p.Y, p.Z, false, false, false, false);
        public static Vector3 Velocity(int entity) => ENTITY.GET_ENTITY_VELOCITY(entity, 0);
        public static void SetVelocity(int entity, Vector3 v) => ENTITY.SET_ENTITY_VELOCITY(entity, v.X, v.Y, v.Z);
        public static float Heading(int entity) => ENTITY.GET_ENTITY_HEADING(entity);
        public static void SetHeading(int entity, float heading) => ENTITY.SET_ENTITY_HEADING(entity, heading);

        public static bool InVehicle(int ped) => PED.IS_PED_IN_ANY_VEHICLE(ped, true);
        public static int VehicleOf(int ped) => PED.GET_VEHICLE_PED_IS_IN(ped, false);
        public static bool OnMount(int ped) => PED.IS_PED_ON_MOUNT(ped);
        public static int MountOf(int ped) => PED.GET_MOUNT(ped);

        /// <summary>The vehicle or mount the ped uses, otherwise the ped itself.</summary>
        public static int Transport(int ped)
        {
            if (InVehicle(ped)) return VehicleOf(ped);
            if (OnMount(ped)) return MountOf(ped);
            return ped;
        }

        public static bool IsHuman(int ped) => PED.IS_PED_HUMAN(ped);
        public static bool IsHorse(int ped) => PED._IS_THIS_MODEL_A_HORSE(ENTITY.GET_ENTITY_MODEL(ped));
        public static bool IsMission(int entity) => ENTITY.IS_ENTITY_A_MISSION_ENTITY(entity);
        public static bool IsDead(int entity) => ENTITY.IS_ENTITY_DEAD(entity);
        public static bool IsPed(int entity) => ENTITY.IS_ENTITY_A_PED(entity);

        /// <summary>0 companion .. 5 hate (GET_RELATIONSHIP_BETWEEN_PEDS).</summary>
        public static int Relationship(int a, int b) => PED.GET_RELATIONSHIP_BETWEEN_PEDS(a, b);

        /// <summary>SET_PED_TO_RAGDOLL(ped, ms, ms, 0, true, true, false).</summary>
        public static void Ragdoll(int ped, int ms)
        {
            Function.Call(0xAE99FB955581844A, ped, ms, ms, 0, true, true, false);
        }

        /// <summary>Re-sets the position (ChaosModRDR: lets ragdoll work on peds held by an anim scene).</summary>
        public static void FixInCutscene(int entity) => SetPos(entity, Pos(entity));

        public static void SetScale(int ped, float scale) => PED._SET_PED_SCALE(ped, scale);
        public static void OutfitPreset(int ped, int preset) => PED._EQUIP_META_PED_OUTFIT_PRESET(ped, preset, false);

        public static void RandomOutfitPreset(int ped)
        {
            int count = PED.GET_NUM_META_PED_OUTFITS(ped);
            if (count > 0) OutfitPreset(ped, Rand(count));
        }

        public static void SetHealth(int entity, int health)
        {
            ENTITY.SET_ENTITY_MAX_HEALTH(entity, health);
            ENTITY.SET_ENTITY_HEALTH(entity, health, 0);
        }

        /// <summary>PCF_NoCriticalHits.</summary>
        public static void NoCriticalHits(int ped) => PED.SET_PED_CONFIG_FLAG(ped, 263, true);

        public static void SetOnMount(int ped, int mount, int seat) => PED.SET_PED_ONTO_MOUNT(ped, mount, seat, true);
        public static void Dismount(int ped) => PED._REMOVE_PED_FROM_MOUNT(ped, false, false);
        public static bool IsMountSeatFree(int mount, int seat) => PED._IS_MOUNT_SEAT_FREE(mount, seat);
        public static void SetIntoVehicle(int ped, int vehicle, int seat) => PED.SET_PED_INTO_VEHICLE(ped, vehicle, seat);
        public static int SeatCount(int vehicle) => VEHICLE.GET_VEHICLE_MODEL_NUMBER_OF_SEATS(ENTITY.GET_ENTITY_MODEL(vehicle));

        /// <summary>Gets the ped off its vehicle (2 m above it) or mount.</summary>
        public static void RemoveFromTransport(int ped)
        {
            if (InVehicle(ped))
            {
                Vector3 p = Pos(VehicleOf(ped));
                p.Z += 2f;
                SetPos(ped, p);
            }
            else if (OnMount(ped))
            {
                Dismount(ped);
            }
        }

        public static void DisableControl(string input) => PAD.DISABLE_CONTROL_ACTION(0, H(input), true);

        /// <summary>Plays a screen effect (ANIMPOSTFX_PLAY, as ChaosModRDR).</summary>
        public static void PostFx(string name)
        {
            if (!GRAPHICS._ANIMPOSTFX_HAS_LOADED(name)) GRAPHICS._ANIMPOSTFX_PRELOAD_POSTFX(name);
            GRAPHICS.ANIMPOSTFX_PLAY(name);
        }

        public static void StopPostFx(string name) => GRAPHICS.ANIMPOSTFX_STOP(name);

        private static readonly Dictionary<string, int> HudContexts = new Dictionary<string, int>();

        /// <summary>Enables a HUD context; reference counted so two effects using the same one do not cancel each other.</summary>
        public static void PushHudContext(string context)
        {
            HudContexts.TryGetValue(context, out int n);
            HudContexts[context] = n + 1;
            if (n == 0) HUD._ENABLE_HUD_CONTEXT(H(context));
        }

        public static void PopHudContext(string context)
        {
            HudContexts.TryGetValue(context, out int n);
            if (n <= 0) return;
            HudContexts[context] = n - 1;
            if (n == 1) HUD._DISABLE_HUD_CONTEXT(H(context));
        }

        public static void DisableControls(string[] inputs)
        {
            foreach (string input in inputs) DisableControl(input);
        }

        #endregion

        #region Models & spawning

        /// <summary>Requests a model and waits (yielding) until it is loaded. False when it does not exist or timed out.</summary>
        public static bool LoadModel(uint hash, int timeoutMs = 4000)
        {
            if (!STREAMING.IS_MODEL_IN_CDIMAGE(hash) || !STREAMING.IS_MODEL_VALID(hash))
            {
                ChaosLog.Warn("Model not found: 0x" + hash.ToString("X8"));
                return false;
            }
            STREAMING.REQUEST_MODEL(hash, false);
            if (NoWait) return STREAMING.HAS_MODEL_LOADED(hash);
            var clock = Stopwatch.StartNew();
            while (!STREAMING.HAS_MODEL_LOADED(hash))
            {
                if (clock.ElapsedMilliseconds > timeoutMs)
                {
                    ChaosLog.Warn("Model load timed out: 0x" + hash.ToString("X8"));
                    return false;
                }
                Script.Wait(0);
            }
            return true;
        }

        public static void ReleaseModel(uint hash) => STREAMING.SET_MODEL_AS_NO_LONGER_NEEDED(hash);

        /// <summary>Entities created by effects; removed with <see cref="CleanupSpawned"/> (menu) or when an effect ends.</summary>
        public static readonly HashSet<int> SpawnedPeds = new HashSet<int>();
        public static readonly HashSet<int> SpawnedVehicles = new HashSet<int>();
        public static readonly HashSet<int> SpawnedProps = new HashSet<int>();

        public static int SpawnPed(string model, bool setInVehicle = true, bool horseForPed = false)
            => SpawnPed(H(model), setInVehicle, horseForPed);

        /// <summary>
        /// ChaosModRDR's SpawnPedAroundPlayer: creates the ped at the player; optionally puts it into the player's
        /// vehicle / behind the player on the mount, or gives it a horse when the player rides or drives.
        /// </summary>
        public static int SpawnPed(uint model, bool setInVehicle = true, bool horseForPed = false)
        {
            if (!LoadModel(model)) return 0;
            bool modelIsHorse = PED._IS_THIS_MODEL_A_HORSE(model);
            int player = PlayerPed;
            Vector3 p = Pos(player);
            bool playerInVehicle = InVehicle(player);
            if (playerInVehicle) p.Z += 2f;

            int ped = PED.CREATE_PED(model, p.X, p.Y, p.Z, 0f, true, false, false, false);
            ReleaseModel(model);
            if (!Exists(ped)) return 0;

            PED._SET_RANDOM_OUTFIT_VARIATION(ped, true);
            DECORATOR.DECOR_SET_INT(ped, "honor_override", 0);
            ENTITY.SET_ENTITY_VISIBLE(ped, true);
            PED.SET_PED_HEARING_RANGE(ped, 10000f);
            SpawnedPeds.Add(ped);

            if (setInVehicle && !modelIsHorse)
            {
                if (playerInVehicle)
                {
                    SetIntoVehicle(ped, VehicleOf(player), -2);
                }
                else if (OnMount(player))
                {
                    int mount = MountOf(player);
                    if (IsMountSeatFree(mount, 0)) SetOnMount(ped, mount, 0);
                }
            }

            if (horseForPed && !modelIsHorse && (OnMount(player) || playerInVehicle))
            {
                int mount = SpawnPed(H("A_C_Horse_Morgan_Bay"), false, false);
                if (mount != 0) SetOnMount(ped, mount, -1);
            }

            ENTITY.SET_ENTITY_AS_MISSION_ENTITY(ped, false, false);
            return ped;
        }

        /// <summary>Creates a vehicle (wagons get their draft animals).</summary>
        public static int SpawnVehicle(string model, Vector3 position, float heading, bool track = true)
        {
            uint hash = H(model);
            if (!LoadModel(hash)) return 0;
            int vehicle = VEHICLE.CREATE_VEHICLE(hash, position.X, position.Y, position.Z, heading, false, false, false, false);
            ReleaseModel(hash);
            if (!Exists(vehicle)) return 0;
            if (track) SpawnedVehicles.Add(vehicle);
            return vehicle;
        }

        /// <summary>Creates an object at the player (ChaosModRDR's SpawnObject).</summary>
        public static int SpawnObject(uint model)
        {
            if (!LoadModel(model)) return 0;
            Vector3 p = Pos(PlayerPed);
            int obj = OBJECT.CREATE_OBJECT(model, p.X, p.Y, p.Z, true, true, false, false, true);
            ReleaseModel(model);
            if (!Exists(obj)) return 0;
            ENTITY.SET_ENTITY_VISIBLE(obj, true);
            ENTITY.SET_ENTITY_ALPHA(obj, 255, true);
            ENTITY.SET_ENTITY_AS_MISSION_ENTITY(obj, false, false);
            SpawnedProps.Add(obj);
            return obj;
        }

        public static unsafe void NoLongerNeeded(int entity)
        {
            int copy = entity;
            ENTITY.SET_ENTITY_AS_NO_LONGER_NEEDED(&copy);
        }

        public static unsafe void DeletePed(int ped)
        {
            SpawnedPeds.Remove(ped);
            if (!Exists(ped) || ped == PlayerPed) return;
            ENTITY.SET_ENTITY_AS_MISSION_ENTITY(ped, true, true);
            int copy = ped;
            PED.DELETE_PED(&copy);
        }

        public static unsafe void DeleteVehicle(int vehicle)
        {
            SpawnedVehicles.Remove(vehicle);
            if (!Exists(vehicle)) return;
            ENTITY.SET_ENTITY_AS_MISSION_ENTITY(vehicle, true, true);
            int copy = vehicle;
            VEHICLE.DELETE_VEHICLE(&copy);
        }

        public static unsafe void DeleteObject(int obj)
        {
            SpawnedProps.Remove(obj);
            if (!Exists(obj)) return;
            ENTITY.SET_ENTITY_AS_MISSION_ENTITY(obj, true, true);
            int copy = obj;
            OBJECT.DELETE_OBJECT(&copy);
        }

        /// <summary>Deletes everything effects spawned, except what the player sits on.</summary>
        public static int CleanupSpawned()
        {
            int player = PlayerPed;
            int keepVehicle = InVehicle(player) ? VehicleOf(player) : 0;
            int keepMount = OnMount(player) ? MountOf(player) : 0;
            int removed = 0;
            foreach (int ped in new List<int>(SpawnedPeds))
            {
                if (ped == keepMount) { SpawnedPeds.Remove(ped); continue; }
                if (Exists(ped)) removed++;
                DeletePed(ped);
            }
            foreach (int vehicle in new List<int>(SpawnedVehicles))
            {
                if (vehicle == keepVehicle) { SpawnedVehicles.Remove(vehicle); continue; }
                if (Exists(vehicle)) removed++;
                DeleteVehicle(vehicle);
            }
            foreach (int obj in new List<int>(SpawnedProps))
            {
                if (Exists(obj)) removed++;
                DeleteObject(obj);
            }
            return removed;
        }

        #endregion

        #region Relationships

        private static readonly uint[] EnemyOf =
        {
            0, // PLAYER, filled lazily
            0x8A33CDCF, // civ male
            0x3220F762, // civ female
            0x3D714F12, 0x915095B1, 0x2A318608, 0x9BA3F8C6, 0xD23F79CC, 0xF287AFC3, 0x403647E5,
        };

        public static unsafe uint AddRelationshipGroup(string name)
        {
            uint group = 0;
            PED.ADD_RELATIONSHIP_GROUP(name, &group);
            return group;
        }

        /// <summary>ChaosModRDR's MarkPedAsCompanion: friendly group, player's ped group, always fights, friendly blip.</summary>
        public static void MarkCompanion(int ped)
        {
            if (!Exists(ped)) return;
            uint playerGroup = H("PLAYER");
            uint companions = AddRelationshipGroup("_CHAOS_COMPANION");
            PED.SET_RELATIONSHIP_BETWEEN_GROUPS(0, companions, playerGroup);
            PED.SET_RELATIONSHIP_BETWEEN_GROUPS(0, playerGroup, companions);
            PED.SET_PED_RELATIONSHIP_GROUP_HASH(ped, companions);
            PED.SET_PED_AS_GROUP_MEMBER(ped, PLAYER.GET_PLAYER_GROUP(PlayerId));
            PED.SET_PED_COMBAT_ATTRIBUTES(ped, 5, true);   // BF_CanFightArmedPedsWhenNotArmed
            PED.SET_PED_COMBAT_ATTRIBUTES(ped, 46, true);  // BF_AlwaysFight
            int blip = MAP.BLIP_ADD_FOR_ENTITY(H("BLIP_STYLE_FRIENDLY"), ped);
            MAP.BLIP_ADD_MODIFIER(blip, H("BLIP_MODIFIER_MP_COLOR_1"));
            PED.SET_PED_CONFIG_FLAG(ped, 130, false);       // allow talking
        }

        /// <summary>ChaosModRDR's MarkPedAsEnemy: hated by the player and civilians, attacks the player.</summary>
        public static void MarkEnemy(int ped)
        {
            if (!Exists(ped)) return;
            EnemyOf[0] = H("PLAYER");
            uint enemies = AddRelationshipGroup("_CHAOS_ENEMY");
            foreach (uint group in EnemyOf)
            {
                PED.SET_RELATIONSHIP_BETWEEN_GROUPS(5, enemies, group);
                PED.SET_RELATIONSHIP_BETWEEN_GROUPS(5, group, enemies);
            }
            PED.SET_PED_RELATIONSHIP_GROUP_HASH(ped, enemies);
            PED.SET_PED_COMBAT_ATTRIBUTES(ped, 5, true);   // BF_CanFightArmedPedsWhenNotArmed
            PED.SET_PED_COMBAT_ATTRIBUTES(ped, 46, true);  // BF_AlwaysFight
            PED.SET_PED_COMBAT_ATTRIBUTES(ped, 1, true);   // BF_CanUseVehicles
            PED.SET_PED_COMBAT_ATTRIBUTES(ped, 3, true);   // BF_CanLeaveVehicle
            TASK.TASK_COMBAT_PED(ped, PlayerPed, 0, 16);
        }

        public static int AddBlip(int entity, string style) => MAP.BLIP_ADD_FOR_ENTITY(H(style), entity);

        #endregion

        #region Weapons

        public const uint AddReason = 0x2CD419DC;

        public static void GiveWeapon(int ped, string weapon, int ammo, bool equip = true)
        {
            uint hash = H(weapon);
            WEAPON.GIVE_DELAYED_WEAPON_TO_PED(ped, hash, ammo, true, AddReason);
            WEAPON.SET_PED_AMMO(ped, hash, ammo);
            if (equip) WEAPON.SET_CURRENT_PED_WEAPON(ped, hash, true, 0, false, false);
        }

        public static void Equip(int ped, string weapon) => WEAPON.SET_CURRENT_PED_WEAPON(ped, H(weapon), true, 0, false, false);

        public static unsafe uint CurrentWeapon(int ped)
        {
            uint weapon = 0;
            WEAPON.GET_CURRENT_PED_WEAPON(ped, &weapon, false, 0, false);
            return weapon;
        }

        /// <summary>ChaosModRDR's weapon list (story weapons).</summary>
        public static readonly string[] Weapons =
        {
            "WEAPON_REPEATER_CARBINE", "WEAPON_REVOLVER_CATTLEMAN", "WEAPON_REVOLVER_CATTLEMAN_JOHN", "WEAPON_REVOLVER_CATTLEMAN_MEXICAN",
            "WEAPON_REVOLVER_CATTLEMAN_PIG", "WEAPON_REVOLVER_DOUBLEACTION_MICAH", "WEAPON_REVOLVER_DOUBLEACTION_GAMBLER",
            "WEAPON_REVOLVER_DOUBLEACTION_EXOTIC", "WEAPON_REVOLVER_SCHOFIELD_CALLOWAY", "WEAPON_REVOLVER_SCHOFIELD_GOLDEN",
            "WEAPON_REVOLVER_DOUBLEACTION", "WEAPON_REVOLVER_SCHOFIELD", "WEAPON_REVOLVER_LEMAT", "WEAPON_PISTOL_VOLCANIC",
            "WEAPON_PISTOL_M1899", "WEAPON_PISTOL_MAUSER", "WEAPON_PISTOL_MAUSER_DRUNK", "WEAPON_PISTOL_SEMIAUTO",
            "WEAPON_REPEATER_WINCHESTER", "WEAPON_REPEATER_HENRY", "WEAPON_RIFLE_VARMINT", "WEAPON_RIFLE_SPRINGFIELD",
            "WEAPON_RIFLE_BOLTACTION", "WEAPON_SHOTGUN_DOUBLEBARREL", "WEAPON_SHOTGUN_DOUBLEBARREL_EXOTIC", "WEAPON_SHOTGUN_SAWEDOFF",
            "WEAPON_SHOTGUN_REPEATING", "WEAPON_SHOTGUN_PUMP", "WEAPON_SHOTGUN_SEMIAUTO", "WEAPON_SNIPERRIFLE_ROLLINGBLOCK",
            "WEAPON_SNIPERRIFLE_ROLLINGBLOCK_EXOTIC", "WEAPON_SNIPERRIFLE_CARCANO", "WEAPON_MELEE_KNIFE", "WEAPON_MELEE_KNIFE_JAWBONE",
            "WEAPON_MELEE_KNIFE_JOHN", "WEAPON_MELEE_KNIFE_MICAH", "WEAPON_MELEE_KNIFE_MINER", "WEAPON_MELEE_KNIFE_VAMPIRE",
            "WEAPON_MELEE_KNIFE_CIVIL_WAR", "WEAPON_MELEE_KNIFE_BEAR", "WEAPON_MELEE_BROKEN_SWORD", "WEAPON_MELEE_CLEAVER",
            "WEAPON_MELEE_HATCHET", "WEAPON_MELEE_MACHETE", "WEAPON_MELEE_ANCIENT_HATCHET", "WEAPON_MELEE_HATCHET_VIKING",
            "WEAPON_MELEE_HATCHET_HEWING", "WEAPON_MELEE_HATCHET_HUNTER", "WEAPON_MELEE_HATCHET_HUNTER_RUSTED",
            "WEAPON_MELEE_HATCHET_DOUBLE_BIT", "WEAPON_MELEE_HATCHET_DOUBLE_BIT_RUSTED", "WEAPON_THROWN_DYNAMITE",
            "WEAPON_THROWN_MOLOTOV", "WEAPON_THROWN_THROWING_KNIVES", "WEAPON_THROWN_TOMAHAWK", "WEAPON_THROWN_TOMAHAWK_ANCIENT",
            "WEAPON_REPEATER_EVANS", "WEAPON_BOW", "WEAPON_KIT_CAMERA", "WEAPON_LASSO", "WEAPON_FISHINGROD", "WEAPON_MELEE_LANTERN_ELECTRIC",
        };

        /// <summary>Story tools that cannot be bought back: never removed or dropped.</summary>
        public static readonly HashSet<string> ProtectedWeapons = new HashSet<string>
        {
            "WEAPON_BOW", "WEAPON_KIT_CAMERA", "WEAPON_LASSO", "WEAPON_FISHINGROD", "WEAPON_MELEE_LANTERN_ELECTRIC", "WEAPON_MELEE_KNIFE",
        };

        public static bool IsProtectedWeapon(uint hash)
        {
            foreach (string name in ProtectedWeapons)
            {
                if (H(name) == hash) return true;
            }
            return false;
        }

        /// <summary>Removes every weapon except <see cref="ProtectedWeapons"/>.</summary>
        public static void RemoveAllWeapons(int ped)
        {
            Equip(ped, "WEAPON_UNARMED");
            foreach (string weapon in Weapons)
            {
                if (ProtectedWeapons.Contains(weapon)) continue;
                WEAPON.REMOVE_WEAPON_FROM_PED(ped, H(weapon), false, 0);
            }
        }

        /// <summary>Last bullet impact of the ped (changes when it shoots again).</summary>
        public static unsafe bool LastImpact(int ped, out Vector3 position)
        {
            Vector3 p = Vector3.Zero;
            bool ok = WEAPON.GET_PED_LAST_WEAPON_IMPACT_COORD(ped, &p);
            position = p;
            return ok;
        }

        /// <summary>Explosion type 27 (dynamite-like) as in ChaosModRDR.</summary>
        public static void Explosion(Vector3 p, int type = 27, float damage = 1f)
            => FIRE.ADD_EXPLOSION(p.X, p.Y, p.Z, type, damage, true, false, 1f);

        #endregion

        #region Nearby

        /// <summary>Up to <paramref name="max"/> peds around the player (player excluded), GET_PED_NEARBY_PEDS buffer.</summary>
        public static List<int> NearbyPeds(int max)
        {
            var result = new List<int>();
            int player = PlayerPed;
            foreach (Ped ped in new Ped(player).GetNearbyPeds(Math.Min(max, 100)))
            {
                if (ped.Handle != player) result.Add(ped.Handle);
            }
            return result;
        }

        /// <summary>Up to <paramref name="max"/> vehicles around the player, the player's own vehicle excluded.</summary>
        public static List<int> NearbyVehicles(int max)
        {
            var result = new List<int>();
            int player = PlayerPed;
            int own = InVehicle(player) ? VehicleOf(player) : 0;
            foreach (Vehicle vehicle in new Ped(player).GetNearbyVehicles(Math.Min(max, 100)))
            {
                if (vehicle.Handle != own) result.Add(vehicle.Handle);
            }
            return result;
        }

        /// <summary>Up to <paramref name="max"/> objects within <paramref name="radius"/> m of the player, nearest first.</summary>
        public static List<int> NearbyProps(int max, float radius = 40f)
        {
            var found = new List<KeyValuePair<float, int>>();
            Vector3 center = Pos(PlayerPed);
            float r2 = radius * radius;
            foreach (Prop prop in World.GetAllObjects())
            {
                if (prop == null) continue;
                int handle = prop.Handle;
                if (!Exists(handle)) continue;
                float d2 = Pos(handle).DistanceToSquared(center);
                if (d2 <= r2) found.Add(new KeyValuePair<float, int>(d2, handle));
            }
            found.Sort((a, b) => a.Key.CompareTo(b.Key));
            var result = new List<int>(Math.Min(max, found.Count));
            for (int i = 0; i < found.Count && i < max; i++) result.Add(found[i].Value);
            return result;
        }

        #endregion

        #region Positions & world

        public static Vector3 RandomAround(Vector3 center, float distance)
        {
            double angle = Rng.Next(360) * Math.PI / 180.0;
            center.X += distance * (float)Math.Sin(angle);
            center.Y += distance * (float)Math.Cos(angle);
            return center;
        }

        /// <summary>A point around the player's transport, ahead of it when <paramref name="useVelocity"/>.</summary>
        public static Vector3 RandomAroundPlayer(float distance, bool useVelocity = true)
        {
            int entity = Transport(PlayerPed);
            Vector3 p = Pos(entity);
            if (useVelocity)
            {
                Vector3 v = Velocity(entity);
                p.X += v.X * 2f;
                p.Y += v.Y * 2f;
                p.Z += v.Z * 2f;
            }
            return RandomAround(p, distance);
        }

        public static unsafe bool GroundZ(float x, float y, float probeZ, out float z)
        {
            float g = 0f;
            bool ok = MISC.GET_GROUND_Z_FOR_3D_COORD(x, y, probeZ, &g, false);
            z = g;
            return ok;
        }

        private static readonly float[] GroundProbeHeights =
            { 100, 150, 50, 0, 200, 250, 300, 350, 400, 450, 500, 550, 600, 650, 700, 750, 800 };

        /// <summary>Moves <paramref name="entity"/> to x/y and finds the ground there (streams the area while probing).</summary>
        public static void TeleportToGround(int entity, Vector3 target, float firstProbeZ)
        {
            if (GroundZ(target.X, target.Y, firstProbeZ, out float z))
            {
                target.Z = z;
            }
            else if (!NoWait)
            {
                foreach (float height in GroundProbeHeights)
                {
                    ENTITY.SET_ENTITY_COORDS_NO_OFFSET(entity, target.X, target.Y, height, false, false, true);
                    Script.Wait(100);
                    if (GroundZ(target.X, target.Y, height, out z))
                    {
                        target.Z = z + 3f;
                        break;
                    }
                }
            }
            SetPos(entity, target);
        }

        /// <summary>Teleports the player's transport (vehicle, mount or the player).</summary>
        public static void TeleportPlayer(Vector3 target) => SetPos(Transport(PlayerPed), target);

        public static readonly string[] Weathers =
        {
            "SUNNY", "MISTY", "FOG", "CLOUDS", "OVERCAST", "OVERCASTDARK", "DRIZZLE", "RAIN", "THUNDER", "THUNDERSTORM", "HURRICANE",
            "HIGHPRESSURE", "SHOWER", "HAIL", "SLEET", "SNOWCLEARING", "SNOWLIGHT", "SNOW", "BLIZZARD", "GROUNDBLIZZARD", "WHITEOUT", "SANDSTORM",
        };

        public static void SetWeather(string weather) => World.SetWeather(H(weather));

        public static void Lightning(Vector3 p) => World.ForceLightningFlash(p);

        public static void ApplyForceCenter(int entity, Vector3 force)
            => ENTITY.APPLY_FORCE_TO_ENTITY_CENTER_OF_MASS(entity, 0, force.X, force.Y, force.Z, false, false, true, false);

        public static void MakeDynamic(int entity)
        {
            ENTITY.SET_ENTITY_DYNAMIC(entity, true);
            ENTITY.SET_ENTITY_HAS_GRAVITY(entity, true);
        }

        public static Vector3 Normalize(Vector3 v)
        {
            float length = (float)Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
            if (length < 0.0001f) return Vector3.Zero;
            return new Vector3(v.X / length, v.Y / length, v.Z / length);
        }

        #endregion
    }

    /// <summary>A script camera that follows the gameplay camera (FOV 120, upside down, top-down).</summary>
    internal sealed class ScriptCam
    {
        private int _cam;

        public int Handle => _cam;

        public void Create()
        {
            _cam = CAM.CREATE_CAM("DEFAULT_SCRIPTED_CAMERA", true);
            CAM.RENDER_SCRIPT_CAMS(true, true, 500, true, true, 1);
        }

        public void Destroy()
        {
            if (_cam == 0) return;
            CAM.SET_CAM_ACTIVE(_cam, false);
            CAM.RENDER_SCRIPT_CAMS(false, true, 700, true, true, 1);
            CAM.DESTROY_CAM(_cam, true);
            _cam = 0;
        }

        /// <summary>Gameplay camera position/rotation with the given rotation override and FOV.</summary>
        public void Follow(Vector3 rotation, float fov)
        {
            if (_cam == 0) return;
            CAM.SET_CAM_ACTIVE(_cam, true);
            Vector3 p = CAM.GET_GAMEPLAY_CAM_COORD();
            CAM.SET_CAM_PARAMS(_cam, p.X, p.Y, p.Z, rotation.X, rotation.Y, rotation.Z, fov, 0, 0, 2, 0, 0, 0);
        }
    }
}
