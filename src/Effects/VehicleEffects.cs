// Vehicle effects (ChaosModRDR src/Effects/vehs.cpp) and the two meta effects (misc.cpp).
using System.Collections.Generic;
using RDR2;
using RDR2.Math;
using RDR2.Native;
using static StreamEmber.ChaosMod.Fx;

namespace StreamEmber.ChaosMod
{
    internal static class VehicleEffects
    {
        private const EffectCategory C = EffectCategory.Vehicles;

        private static readonly string[] RandomVehicles =
        {
            "CART01", "CART02", "CART03", "CART04", "CART05", "CART06", "CART07", "CART08", "ARMYSUPPLYWAGON", "BUGGY01", "BUGGY02",
            "BUGGY03", "CHUCKWAGON000X", "CHUCKWAGON002X", "COACH2", "COACH3", "COACH4", "COACH5", "COACH6", "coal_wagon", "OILWAGON01X",
            "POLICEWAGON01X", "WAGON02X", "WAGON04X", "LOGWAGON", "WAGON03X", "WAGON05X", "WAGON06X", "WAGONPRISON01X", "STAGECOACH001X",
            "STAGECOACH002X", "UTILLIWAG", "GATCHUCK", "GATCHUCK_2", "wagonCircus01x", "wagonDairy01x", "wagonWork01x", "wagonTraveller01x",
            "KEELBOAT", "CANOE", "CANOETREETRUNK", "SKIFF", "BREACH_CANNON", "trolley01x",
        };

        public static void Register(EffectRegistry r)
        {
            // ---- Spawns -------------------------------------------------------------------------------------------
            r.Instant(C, "spawn_wagon", "Posta arabası", "Bir posta arabası belirir, sürücü koltuğuna geçersin.", "car", () =>
            {
                int player = PlayerPed;
                int veh = SpawnVehicle("COACH3", Pos(player), Heading(player));
                if (veh == 0) return;
                DECORATOR.DECOR_SET_BOOL(veh, "wagon_block_honor", true);
                SetIntoVehicle(player, veh, -1);
            });

            r.Instant(C, "spawn_canoe", "Kano", "Bir kano belirir ve içine oturursun.", "boat", () => SpawnAndSit("CANOE", Rand(360)));
            r.Instant(C, "spawn_random_veh", "Rastgele araç", "Rastgele bir araba, tekne ya da top belirir ve içine oturursun.", "dice",
                () => SpawnAndSit(Pick(RandomVehicles), Heading(PlayerPed)));

            r.Instant(C, "spawn_balloon", "Sıcak hava balonu", "Bir balon belirir, içine binip rüzgârla yükselirsin.", "wind", () =>
            {
                int player = PlayerPed;
                Vector3 p = Pos(player);
                p.Z += 2f;
                int veh = SpawnVehicle("hotAirBalloon01", p, Rand(360));
                if (veh == 0) return;
                SetIntoVehicle(player, veh, -1);
                Vector3 wind = World.WindDirection;
                SetVelocity(veh, new Vector3(wind.X * 25f, wind.Y * 25f, 25f));
            });

            // ---- Seats ------------------------------------------------------------------------------------------
            r.Instant(C, "everyone_exits_vehs", "Herkes insin", "Yakındaki herkes (sen de) arabadan ya da attan iner.", "door-open", () =>
            {
                var peds = NearbyPeds(45);
                peds.Add(PlayerPed);
                foreach (int ped in peds)
                {
                    if (InVehicle(ped)) TASK.TASK_LEAVE_VEHICLE(ped, VehicleOf(ped), 4160, 0);
                    else if (OnMount(ped)) Dismount(ped);
                }
            });

            r.Instant(C, "set_to_random_veh", "Rastgele araca geç", "Yakındaki rastgele bir arabaya ya da ata oturtulursun.", "swap", () =>
            {
                int player = PlayerPed;
                var entities = NearbyVehicles(45);
                foreach (int ped in NearbyPeds(45))
                {
                    if (IsHorse(ped)) entities.Add(ped);
                }
                // Shuffle
                for (int i = entities.Count - 1; i > 0; i--)
                {
                    int j = Rand(i + 1);
                    (entities[i], entities[j]) = (entities[j], entities[i]);
                }
                foreach (int e in entities)
                {
                    if (IsPed(e))
                    {
                        foreach (int seat in new[] { -1, 0 })
                        {
                            if (!IsMountSeatFree(e, seat)) continue;
                            SetOnMount(player, e, seat);
                            return;
                        }
                    }
                    else
                    {
                        if (VEHICLE.IS_VEHICLE_SEAT_FREE(e, -1))
                        {
                            SetIntoVehicle(player, e, -1);
                            return;
                        }
                        int seats = SeatCount(e) - 1;
                        for (int i = 0; i < seats; i++)
                        {
                            if (!VEHICLE.IS_VEHICLE_SEAT_FREE(e, i)) continue;
                            SetIntoVehicle(player, e, i);
                            return;
                        }
                    }
                }
            });

            r.Instant(C, "set_peds_into_player_veh", "Yolcu doldur", "Yakındaki insanlar arabana ya da atının terkisine ışınlanır.", "users", () =>
            {
                int player = PlayerPed;
                bool mount = false;
                int veh;
                int seats = 0;
                if (InVehicle(player))
                {
                    veh = VehicleOf(player);
                    seats = SeatCount(veh) - 1;
                }
                else if (OnMount(player))
                {
                    mount = true;
                    veh = MountOf(player);
                }
                else
                {
                    return;
                }
                foreach (int ped in NearbyPeds(30))
                {
                    if (!Exists(ped) || IsHorse(ped)) continue;
                    if (mount)
                    {
                        if (IsMountSeatFree(veh, 0)) SetOnMount(ped, veh, 0);
                        return;
                    }
                    bool seated = false;
                    for (int i = 0; i < seats; i++)
                    {
                        if (!VEHICLE.IS_VEHICLE_SEAT_FREE(veh, i)) continue;
                        SetIntoVehicle(ped, veh, i);
                        seated = true;
                        break;
                    }
                    if (!seated) return;
                }
            });

            // ---- Damage ---------------------------------------------------------------------------------------------
            r.Instant(C, "ignite_wagon", "Arabanı patlat", "Oyuncunun bindiği araba patlar.", "flame", () =>
            {
                int player = PlayerPed;
                if (InVehicle(player)) VEHICLE.EXPLODE_VEHICLE(VehicleOf(player), true, false, 0, 0);
            });

            r.Instant(C, "ignite_nearby_wagons", "Arabaları patlat", "Yakındaki bütün arabalar patlar.", "bomb", () =>
            {
                foreach (int veh in NearbyVehicles(45)) VEHICLE.EXPLODE_VEHICLE(veh, true, false, 0, 0);
            });

            r.Instant(C, "detach_wheels", "Tekerlekler gitti", "Bindiğin arabanın tekerlekleri kopar.", "tire", () =>
            {
                int player = PlayerPed;
                if (!InVehicle(player)) return;
                var veh = new Vehicle(VehicleOf(player));
                for (int i = 0; i < 4; i++) veh.BreakOffWheel(i);
            });

            r.Instant(C, "random_wheels_detaching", "Rastgele tekerlek", "Yakındaki arabaların tekerlekleri rastgele kopar.", "tire", () =>
            {
                int player = PlayerPed;
                var vehs = NearbyVehicles(50);
                if (InVehicle(player)) vehs.Add(VehicleOf(player));
                foreach (int handle in vehs)
                {
                    var veh = new Vehicle(handle);
                    for (int i = 0; i < 4; i++)
                    {
                        if (Coin()) veh.BreakOffWheel(i);
                    }
                }
            });

            r.Instant(C, "flip_vehs", "Arabaları ters çevir", "Yakındaki arabalar ve atlar ters döner.", "refresh", () =>
            {
                int player = PlayerPed;
                var entities = NearbyVehicles(45);
                if (InVehicle(player)) entities.Add(VehicleOf(player));
                foreach (int ped in NearbyPeds(45))
                {
                    if (!IsHorse(ped)) continue;
                    Ragdoll(ped, 5000);
                    entities.Add(ped);
                }
                foreach (int e in entities)
                {
                    if (!Exists(e)) continue;
                    Vector3 rot = ENTITY.GET_ENTITY_ROTATION(e, 2);
                    Vector3 v = Velocity(e);
                    ENTITY.SET_ENTITY_ROTATION(e, rot.X + 180f, rot.Y, rot.Z, 2, true);
                    SetVelocity(e, new Vector3(v.X, v.Y, v.Z + 5f));
                }
            });

            r.Instant(C, "horses_are_donkeys", "Bütün atlar eşek", "Yakındaki atlar (binicileriyle) eşeğe dönüşür.", "horse", () =>
            {
                foreach (int ped in NearbyPeds(100))
                {
                    if (!Exists(ped) || !IsHorse(ped)) continue;
                    Vector3 p = Pos(ped);
                    Vector3 v = ENTITY.GET_ENTITY_VELOCITY(ped, 1);
                    float heading = Heading(ped);
                    int rider = 0;
                    if (!IsMountSeatFree(ped, -1))
                    {
                        rider = PED._GET_RIDER_OF_MOUNT(ped, false);
                        if (rider != 0) Dismount(rider);
                    }
                    if (IsMission(ped) && !SpawnedPeds.Contains(ped)) continue;
                    DeletePed(ped);
                    int donkey = SpawnPed("A_C_Donkey_01", false, false);
                    if (donkey == 0) continue;
                    SetPos(donkey, p);
                    SetVelocity(donkey, v);
                    SetHeading(donkey, heading);
                    if (rider != 0) SetOnMount(rider, donkey, -1);
                }
            });

            // ---- Speed ------------------------------------------------------------------------------------------
            r.Timed(C, "full_acceleration", "Tam gaz", "30 saniye: yakındaki bütün arabalar, trenler ve atlar son hızla gider.", "gauge", 30, () =>
            {
                var every = new Interval(1000, true);
                var vehs = new List<int>();
                var horses = new HashSet<int>();
                return new LambdaEffect(null, dt =>
                {
                    if (every.Tick(dt))
                    {
                        vehs.Clear();
                        vehs.AddRange(NearbyVehicles(45));
                        int player = PlayerPed;
                        if (InVehicle(player)) vehs.Add(VehicleOf(player));
                        foreach (int ped in NearbyPeds(45))
                        {
                            if (!IsHorse(ped)) continue;
                            Vector3 forward = ENTITY.GET_ENTITY_FORWARD_VECTOR(ped);
                            ENTITY.SET_ENTITY_INVINCIBLE(ped, true);
                            horses.Add(ped);
                            Ragdoll(ped, 1000);
                            SetVelocity(ped, forward * 1000f);
                        }
                    }
                    foreach (int handle in vehs)
                    {
                        if (!Exists(handle)) continue;
                        var veh = new Vehicle(handle);
                        if (veh.IsTrain)
                        {
                            veh.SetForwardSpeed(1000f);
                            veh.SetTrainSpeed(1000f);
                        }
                        else
                        {
                            veh.SetForwardSpeed(100f);
                        }
                    }
                }, () =>
                {
                    foreach (int horse in horses) if (Exists(horse)) ENTITY.SET_ENTITY_INVINCIBLE(horse, false);
                });
            });

            r.EveryFrame(C, "fast_players_wagon", "Nitro", "30 saniye boyunca bindiğin araba (ya da tren) iki kat hızlanır.", "chevs-r", 30, () =>
            {
                int player = PlayerPed;
                if (!InVehicle(player)) return;
                var veh = new Vehicle(VehicleOf(player));
                bool train = veh.IsTrain;
                float speed = System.Math.Min(ENTITY.GET_ENTITY_SPEED(veh.Handle) * 2f, train ? 1000f : 25f);
                veh.SetForwardSpeed(speed);
                if (train) veh.SetTrainSpeed(speed);
            });

            // ---- Raining things -------------------------------------------------------------------------------------
            r.Timed(C, "minecart_rain", "Maden arabası yağmuru", "30 saniye boyunca gökten maden arabaları düşer.", "train", 30, () =>
            {
                var every = new Interval(500);
                var vehs = new List<int>();
                return new LambdaEffect(null, dt =>
                {
                    if (!every.Tick(dt)) return;
                    Vector3 p = RandomAroundPlayer(Rand(20));
                    int veh = SpawnVehicle("mineCart01x", new Vector3(p.X, p.Y, p.Z + 35f), Rand(360));
                    if (veh == 0) return;
                    SetVelocity(veh, new Vector3(0f, 0f, -150f));
                    vehs.Add(veh);
                }, () =>
                {
                    foreach (int veh in vehs) DeleteVehicle(veh);
                });
            });

            r.Timed(C, "horses_rain", "At yağmuru", "25 saniye boyunca gökten atlar yağar.", "horse", 25, () =>
            {
                string[] models =
                {
                    "A_C_Horse_Morgan_Bay", "A_C_Horse_Arabian_Black", "A_C_Horse_Arabian_Grey", "A_C_Horse_Arabian_White",
                    "A_C_Horse_Shire_DarkBay", "A_C_Horse_TennesseeWalker_DappleBay",
                };
                var every = new Interval(1000);
                var horses = new List<int>();
                return new LambdaEffect(null, dt =>
                {
                    if (!every.Tick(dt)) return;
                    int horse = SpawnPed(Pick(models), false);
                    if (horse == 0) return;
                    ENTITY.SET_ENTITY_INVINCIBLE(horse, true);
                    PED.SET_PED_CAN_RAGDOLL(horse, false);
                    Vector3 p = RandomAroundPlayer(Rand(20));
                    SetPos(horse, new Vector3(p.X, p.Y, p.Z + 35f));
                    SetVelocity(horse, new Vector3(0f, 0f, -40f));
                    horses.Add(horse);
                }, () =>
                {
                    foreach (int horse in horses) DeletePed(horse);
                });
            });

            r.Timed(C, "oilwagons_rain", "Petrol vagonu yağmuru", "30 saniye boyunca gökten düşen petrol vagonları yere çarpınca patlar.", "fuel", 30, () =>
            {
                var every = new Interval(2500);
                var vehs = new List<int>();
                var falling = new HashSet<int>();
                return new LambdaEffect(null, dt =>
                {
                    if (every.Tick(dt))
                    {
                        Vector3 p = RandomAroundPlayer(Rand(50));
                        uint model = H("OilWagon01X");
                        if (LoadModel(model))
                        {
                            int veh = VEHICLE.CREATE_VEHICLE(model, p.X, p.Y, p.Z + 175f, 0f, false, false, true, false);
                            ReleaseModel(model);
                            if (Exists(veh))
                            {
                                ENTITY.SET_ENTITY_ROTATION(veh, 0f, 180f, 0f, 2, true);
                                SetVelocity(veh, new Vector3(0f, 0f, -150f));
                                vehs.Add(veh);
                                falling.Add(veh);
                                SpawnedVehicles.Add(veh);
                            }
                        }
                    }
                    foreach (int veh in new List<int>(falling))
                    {
                        if (!Exists(veh))
                        {
                            falling.Remove(veh);
                            continue;
                        }
                        if (Velocity(veh).Z >= -0.5f)
                        {
                            VEHICLE.EXPLODE_VEHICLE(veh, true, false, 0, 0);
                            falling.Remove(veh);
                        }
                    }
                }, () =>
                {
                    foreach (int veh in vehs) DeleteVehicle(veh);
                });
            });
        }

        private static void SpawnAndSit(string model, float heading)
        {
            int player = PlayerPed;
            int veh = SpawnVehicle(model, Pos(player), heading);
            if (veh != 0) SetIntoVehicle(player, veh, -1);
        }
    }

    internal static class MetaEffects
    {
        public static void Register(EffectRegistry r)
        {
            // The engine reads these while they are active (see ChaosEngine.Tick / Run)
            r.Timed(EffectCategory.Meta, "total_chaos", "Total Chaos", "3 dakika boyunca her 15 saniyede bir rastgele efekt çalışır (otomatik mod kapalı olsa da).", "fire-circle", 180,
                () => new LambdaEffect(null));
            r.Timed(EffectCategory.Meta, "combo_time", "Combo Time", "3 dakika boyunca çalıştırdığın her efekt yanında iki rastgele efekt daha getirir.", "layers", 180,
                () => new LambdaEffect(null));
        }
    }
}
