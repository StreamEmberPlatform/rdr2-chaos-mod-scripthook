// Player effects (ChaosModRDR src/Effects/player.cpp).
using System;
using RDR2;
using RDR2.Math;
using RDR2.Native;
using RDR2.UI;
using static StreamEmber.ChaosMod.Fx;

namespace StreamEmber.ChaosMod
{
    internal static class PlayerEffects
    {
        private const EffectCategory C = EffectCategory.Player;

        private static readonly string[] LeftRight =
        {
            "INPUT_MOVE_LEFT_ONLY", "INPUT_MOVE_RIGHT_ONLY", "INPUT_MOVE_LR", "INPUT_VEH_MOVE_LR", "INPUT_MOVE_LEFT", "INPUT_MOVE_RIGHT",
            "INPUT_HORSE_MOVE_LEFT_ONLY", "INPUT_VEH_CAR_TURN_LEFT_ONLY", "INPUT_VEH_DRAFT_TURN_LEFT_ONLY", "INPUT_VEH_BOAT_TURN_LEFT_ONLY",
            "INPUT_VEH_MOVE_LEFT_ONLY", "INPUT_FRONTEND_AXIS_X", "INPUT_HORSE_MOVE_LR", "INPUT_HORSE_MOVE_RIGHT_ONLY", "INPUT_VEH_CAR_TURN_LR",
            "INPUT_VEH_DRAFT_TURN_RIGHT_ONLY", "INPUT_VEH_CAR_TURN_RIGHT_ONLY", "INPUT_VEH_MOVE_RIGHT_ONLY", "INPUT_VEH_DRAFT_TURN_LR",
            "INPUT_VEH_BOAT_TURN_LR", "INPUT_VEH_BOAT_TURN_RIGHT_ONLY", "INPUT_FRONTEND_NAV_RIGHT",
        };

        private static readonly string[] ForwardBackward =
        {
            "INPUT_MOVE_UP_ONLY", "INPUT_MOVE_DOWN_ONLY", "INPUT_VEH_ACCELERATE", "INPUT_VEH_BRAKE", "INPUT_MOVE_UP", "INPUT_MOVE_DOWN",
            "INPUT_MOVE_UD", "INPUT_FRONTEND_AXIS_Y", "INPUT_HORSE_MOVE_DOWN_ONLY", "INPUT_HORSE_MOVE_UD", "INPUT_VEH_MOVE_DOWN_ONLY",
            "INPUT_VEH_DRAFT_MOVE_UD", "INPUT_HORSE_MOVE_UP_ONLY", "INPUT_VEH_MOVE_UP_ONLY", "INPUT_VEH_MOVE_UD", "INPUT_FRONTEND_NAV_UP",
            "INPUT_HORSE_SPRINT", "INPUT_HORSE_STOP", "INPUT_VEH_HANDCART_BRAKE", "INPUT_VEH_BOAT_BRAKE", "INPUT_VEH_HANDBRAKE",
            "INPUT_VEH_CAR_BRAKE",
        };

        private static readonly string[] SprintJump = { "INPUT_SPRINT", "INPUT_JUMP", "INPUT_HORSE_SPRINT", "INPUT_VEH_ACCELERATE", "INPUT_HORSE_JUMP" };
        private static readonly string[] Attack = { "INPUT_ATTACK", "INPUT_MELEE_ATTACK", "INPUT_MELEE_GRAPPLE_ATTACK", "INPUT_HORSE_MELEE" };

        /// <summary>ChaosModRDR's DisableAllMovements (also used by Party Time).</summary>
        public static void DisableAllMovements()
        {
            DisableControls(LeftRight);
            DisableControls(ForwardBackward);
        }

        private static readonly Vector3[] RandomLocations =
        {
            new Vector3(-301.0f, 790.0f, 119.0f),    // Valentine
            new Vector3(-1303.0f, 395.0f, 96.0f),    // Wallace Station
            new Vector3(-1790.0f, -372.5f, 160.0f),  // Strawberry
            new Vector3(2432.8f, -1216.0f, 46.0f),   // Saint Denis
            new Vector3(1526.5f, 431.0f, 91.0f),     // Emerald Station
            new Vector3(1264.4f, -1311.0f, 77.0f),   // Rhodes
            new Vector3(2958.6f, 518.0f, 45.0f),     // Van Horn
            new Vector3(2927.0f, 1325.9f, 44.0f),    // Annesburg
        };

        public static void Register(EffectRegistry r)
        {
            // ---- Movement & physics ------------------------------------------------------------------------------
            r.Instant(C, "launch_player_up", "Havaya fırlat", "Oyuncuyu (ya da bindiği atı / arabayı) havaya fırlatır.", "arrow-up", () =>
            {
                int ped = PlayerPed;
                int e = Transport(ped);
                if (e == ped)
                {
                    FixInCutscene(ped);
                    Script.Wait(75);
                    Ragdoll(ped, 5000);
                }
                Vector3 v = Velocity(e);
                SetVelocity(e, new Vector3(v.X, v.Y, 35f));
            });

            r.Instant(C, "to_the_stars", "Yıldızlara", "Oyuncuyu 800 m yüksekliğe ışınlar; gerisi yerçekiminin işi.", "star", () =>
            {
                int ped = PlayerPed;
                int e = Transport(ped);
                if (e == ped) Ragdoll(ped, 10000);
                Vector3 p = Pos(e);
                SetPos(e, new Vector3(p.X, p.Y, 800f));
            });

            r.Instant(C, "kickflip", "Kickflip", "Oyuncuyu bindiği şeyden fırlatıp havada takla attırır.", "refresh", () =>
            {
                int ped = PlayerPed;
                if (InVehicle(ped))
                {
                    int veh = VehicleOf(ped);
                    Vector3 p = Pos(ped);
                    p.Z += 2f;
                    SetPos(ped, p);
                    SetVelocity(ped, Velocity(veh));
                }
                FixInCutscene(ped);
                Script.Wait(75);
                Ragdoll(ped, 1000);
                ENTITY.APPLY_FORCE_TO_ENTITY(ped, 1, 0f, 0f, 10f, 2f, 0f, 0f, 0, true, true, true, false, true);
            });

            r.Instant(C, "ragdoll", "Yere yığıl", "Oyuncu birkaç saniye bez bebek gibi yere düşer.", "injured", () =>
            {
                int ped = PlayerPed;
                FixInCutscene(ped);
                Script.Wait(75);
                Ragdoll(ped, 3000);
            });

            r.Timed(C, "player_sleep", "Biraz uyumam lazım", "Oyuncu olduğu yerde 15 saniye bayılır.", "sleep", 15, () => new LambdaEffect(() =>
            {
                int ped = PlayerPed;
                if (InVehicle(ped))
                {
                    Vector3 p = Pos(ped);
                    p.Z += 2f;
                    SetPos(ped, p);
                }
                FixInCutscene(ped);
                Script.Wait(75);
                Ragdoll(ped, 18000);
            }));

            r.Instant(C, "blacking_out", "Kararma", "Ekran kararır, oyuncu bayılıp yere düşer.", "eye-off", () =>
            {
                PostFx("PlayerWakeUpInterrogation");
                int ped = PlayerPed;
                if (InVehicle(ped))
                {
                    Vector3 p = Pos(ped);
                    p.Z += 2f;
                    SetPos(ped, p);
                }
                FixInCutscene(ped);
                Script.Wait(75);
                Ragdoll(ped, 5000);
            });

            r.Instant(C, "invert_velocity", "Ters yön", "Mevcut hızı ters çevirip katlar.", "swap", () => ScaleVelocity(-3f, -5f));
            r.Instant(C, "increase_velocity", "Hızlan", "Mevcut hızı üçe katlar.", "chevs-r", () => ScaleVelocity(3f, 3f));

            r.Instant(C, "random_velocity", "Rastgele savrulma", "Oyuncuyu rastgele bir yöne savurur.", "wind", () =>
            {
                int ped = PlayerPed;
                bool inVehicle = InVehicle(ped);
                int e = Transport(ped);
                if (!inVehicle)
                {
                    FixInCutscene(e);
                    Script.Wait(75);
                    Ragdoll(e, 2000);
                }
                var v = new Vector3((Rand(15) + 5) * Sign(), (Rand(15) + 5) * Sign(), Rand(10));
                SetVelocity(e, v);
            });

            r.Timed(C, "bunnyhop", "Tavşan zıplaması", "20 saniye boyunca oyuncu durmadan zıplar.", "arrow-up", 20, () =>
            {
                var every = new Interval(500);
                return new LambdaEffect(null, dt =>
                {
                    if (!every.Tick(dt)) return;
                    int ped = PlayerPed;
                    if (InVehicle(ped))
                    {
                        Vector3 p = Pos(ped);
                        p.Z += 1.5f;
                        SetPos(ped, p);
                    }
                    else if (OnMount(ped))
                    {
                        Dismount(ped);
                    }
                    else
                    {
                        TASK.TASK_JUMP(ped, false);
                    }
                });
            });

            r.Timed(C, "player_spin", "Fırıldak", "Oyuncu (ya da bindiği şey) 20 saniye döner durur.", "repeat", 20, () =>
            {
                float heading = Heading(PlayerPed);
                return new LambdaEffect(null, dt =>
                {
                    heading = (heading + 625f * dt / 1000f) % 360f;
                    SetHeading(Transport(PlayerPed), heading);
                });
            });

            r.EveryFrame(C, "super_jump", "Süper zıplama", "30 saniye boyunca çok yükseğe zıplarsın.", "level-up", 30,
                () => MISC.SET_SUPER_JUMP_THIS_FRAME(PlayerId));

            // ---- Teleports ----------------------------------------------------------------------------------------
            r.Instant(C, "tp_to_waypoint", "İşarete ışınlan", "Haritada işaret varsa oraya ışınlar.", "pin", () =>
            {
                if (!MAP.IS_WAYPOINT_ACTIVE()) return;
                TeleportToGround(Transport(PlayerPed), MAP._GET_WAYPOINT_COORDS(), 100f);
            });

            r.Instant(C, "tp_few_meters", "Birkaç metre ışınlan", "Oyuncuyu 10 m ötede rastgele bir noktaya ışınlar.", "route", () =>
            {
                int e = Transport(PlayerPed);
                Vector3 target = RandomAround(Pos(e), 10f);
                TeleportToGround(e, target, target.Z);
            });

            EffectDef tpRandom = r.Instant(C, "tp_to_random", "Rastgele yere ışınlan", "Valentine, Saint Denis, Strawberry gibi bir kasabaya ışınlar.", "map",
                () => TeleportPlayer(Pick(RandomLocations)));

            r.Add(new EffectDef
            {
                Id = "fake_teleport", Name = "Sahte ışınlanma", Category = C, Icon = "ghost", Duration = 20, DisguiseId = tpRandom.Id,
                Description = "\"Rastgele yere ışınlan\" gibi görünür; 7 saniye sonra oyuncuyu eski yerine geri getirir.",
                Create = () => new FakeTeleport(tpRandom.Name),
            });

            // ---- Money & law -------------------------------------------------------------------------------------
            r.Instant(C, "give_player_money", "300$ ver", "Oyuncuya 300 dolar verir.", "cash",
                () => MONEY._MONEY_INCREMENT_CASH_BALANCE(30000, AddReason));
            r.Instant(C, "bankruptcy", "İflas", "Oyuncunun bütün parası silinir.", "wallet", () =>
            {
                int cash = MONEY._MONEY_GET_CASH_BALANCE();
                if (cash > 0) MONEY._MONEY_DECREMENT_CASH_BALANCE(cash);
            });
            r.Instant(C, "clear_pursuit", "Kovalamacayı bitir", "Aranma durumu ve ödül sıfırlanır.", "handcuffs", () =>
            {
                Player player = Game.Player;
                player.ClearWanted();
                player.Bounty = 0;
            });
            r.Instant(C, "increase_bounty", "Ödülü artır", "Oyuncunun başındaki ödül 50$ artar.", "sheriff",
                () => Game.Player.Bounty += 5000);
            r.Instant(C, "most_wanted", "En çok aranan", "Kanun memuruna saldırı suçu bildirilir, ödül 50$ artar.", "police", () =>
            {
                Player player = Game.Player;
                player.ReportCrime(H("CRIME_ASSAULT_LAW"));
                player.Bounty += 5000;
            });

            // ---- Honor (experimental: script globals of game builds 1311/1436) ----------------------------------
            AddHonor(r, "honor_good", "Onur: iyi", "Onuru en iyi seviyeye çeker.", "sun", 240, true);
            AddHonor(r, "honor_bad", "Onur: kötü", "Onuru en kötü seviyeye çeker.", "skull", -240, true);
            AddHonor(r, "honor_reset", "Onuru sıfırla", "Onuru başlangıç seviyesine yakın bir değere çeker.", "refresh", 40, true);
            AddHonor(r, "random_honor", "Rastgele onur", "Onura -25 ile +25 arası rastgele bir değişiklik.", "dice", 0, false);

            // ---- Weapons ------------------------------------------------------------------------------------------
            r.Instant(C, "give_rifle", "Tüfek ver", "Springfield tüfeği ve 100 mermi.", "rifle", () => GiveWeapon(PlayerPed, "WEAPON_RIFLE_SPRINGFIELD", 100));
            r.Instant(C, "give_revolver", "Tabanca ver", "Schofield revolver ve 100 mermi.", "revolver", () => GiveWeapon(PlayerPed, "WEAPON_REVOLVER_SCHOFIELD", 100));
            r.Instant(C, "give_sniper_rifle", "Keskin nişancı tüfeği ver", "Rolling Block ve 35 mermi.", "sniper", () => GiveWeapon(PlayerPed, "WEAPON_SNIPERRIFLE_ROLLINGBLOCK", 35));
            r.Instant(C, "give_dynamite", "Dinamit ver", "3 dinamit.", "dynamite", () => GiveWeapon(PlayerPed, "WEAPON_THROWN_DYNAMITE", 3));
            r.Instant(C, "give_knives", "Fırlatma bıçakları ver", "10 fırlatma bıçağı.", "knife", () => GiveWeapon(PlayerPed, "WEAPON_THROWN_THROWING_KNIVES", 10));
            r.Instant(C, "give_lasso", "Kement ver", "Kementi eline verir.", "lasso", () => GiveWeapon(PlayerPed, "WEAPON_LASSO", 100));

            r.Instant(C, "give_random_weapon", "Rastgele silah", "Listeden rastgele bir silah verir (kamera, kement, olta, fener ve bıçak da eklenir).", "dice", () =>
            {
                int ped = PlayerPed;
                foreach (string tool in new[] { "WEAPON_KIT_CAMERA", "WEAPON_LASSO", "WEAPON_FISHINGROD", "WEAPON_MELEE_LANTERN_ELECTRIC", "WEAPON_MELEE_KNIFE" })
                {
                    GiveWeapon(ped, tool, 100, false);
                }
                GiveWeapon(ped, Pick(Weapons), 100);
            });

            r.Instant(C, "no_ammo", "Mermi bitti", "Bütün silahların mermisi silinir.", "magazine", () =>
            {
                int ped = PlayerPed;
                WEAPON._REMOVE_ALL_PED_AMMO(ped);
                Equip(ped, "WEAPON_UNARMED");
            });

            r.Instant(C, "remove_all_weapons", "Silahları al", "Kamera, kement, olta, fener, bıçak ve yay dışındaki bütün silahlar alınır.", "ban",
                () => RemoveAllWeapons(PlayerPed));

            r.Instant(C, "drop_weapon", "Silahı düşür", "Elindeki silah yere düşer.", "hand", () =>
            {
                int ped = PlayerPed;
                uint weapon = CurrentWeapon(ped);
                if (weapon == 0 || weapon == H("WEAPON_UNARMED") || !WEAPON.IS_WEAPON_VALID(weapon) || IsProtectedWeapon(weapon)) return;
                WEAPON.SET_PED_DROPS_INVENTORY_WEAPON(ped, weapon, 0f, 0f, 0f, 0);
                Equip(ped, "WEAPON_UNARMED");
            });

            r.Timed(C, "lightning_weapons", "Yıldırım mermileri", "25 saniye boyunca mermilerin düştüğü yere yıldırım çarpar.", "bolt", 25,
                () => ImpactEffect(p => Lightning(p)));
            r.Timed(C, "explosive_weapons", "Patlayıcı mermiler", "25 saniye boyunca mermilerin düştüğü yer patlar.", "bomb", 25,
                () => ImpactEffect(p => Explosion(p)));
            r.Timed(C, "teleport_weapons", "Işınlanma mermileri", "25 saniye boyunca mermi nereye düşerse oyuncu oraya ışınlanır.", "spawn", 25,
                () => ImpactEffect(p => SetPos(PlayerPed, p)));

            r.Timed(C, "gravity_gun", "Yerçekimi tabancası", "30 saniye: vurduğun yerdeki NPC'ler, araçlar ve eşyalar savrulur. Elin boşsa Mauser verilir.", "grab", 30, () =>
            {
                Vector3 last = Vector3.Zero;
                return new LambdaEffect(() =>
                {
                    int ped = PlayerPed;
                    LastImpact(ped, out last);
                    if (CurrentWeapon(ped) == H("WEAPON_UNARMED")) GiveWeapon(ped, "WEAPON_PISTOL_MAUSER_DRUNK", 35);
                }, dt =>
                {
                    int ped = PlayerPed;
                    if (!LastImpact(ped, out Vector3 hit) || hit == last) return;
                    last = hit;
                    var entities = NearbyPeds(50);
                    entities.AddRange(NearbyVehicles(45));
                    foreach (int prop in NearbyProps(45))
                    {
                        MakeDynamic(prop);
                        entities.Add(prop);
                    }
                    Vector3 from = Pos(ped);
                    const float maxDist = 5f;
                    foreach (int e in entities)
                    {
                        Vector3 p = Pos(e);
                        float dist = p.DistanceTo(hit);
                        if (dist >= maxDist) continue;
                        if (IsPed(e)) Ragdoll(e, 5000);
                        float scale = 15f * (1f - dist / maxDist);
                        Vector3 dir = Normalize(p - from);
                        SetVelocity(e, dir * scale);
                    }
                });
            });

            // ---- Body & look -----------------------------------------------------------------------------------
            r.Instant(C, "heal_player", "Can doldur", "Oyuncunun canı tamamen dolar.", "heart-plus",
                () => ENTITY.SET_ENTITY_HEALTH(PlayerPed, PED.GET_PED_MAX_HEALTH(PlayerPed), 0));
            r.Instant(C, "almost_dead", "Ölümün eşiğinde", "Oyuncunun canı 1'e düşer.", "heart-broken",
                () => ENTITY.SET_ENTITY_HEALTH(PlayerPed, 1, 0));
            r.Instant(C, "restore_stamina", "Dayanıklılık doldur", "Oyuncunun dayanıklılığı dolar.", "stamina",
                () => PLAYER.RESTORE_PLAYER_STAMINA(PlayerId, 1f));
            r.Instant(C, "ignite_player", "Oyuncuyu tutuştur", "Oyuncu alev alır.", "flame", () => new Ped(PlayerPed).Ignite());

            r.Timed(C, "set_drunk", "Sarhoş", "30 saniye sarhoşluk: kamera sallanır, yürüyüş bozulur, ara ara düşersin.", "whiskey", 30, () =>
            {
                var every = new Interval(7000);
                return new LambdaEffect(() =>
                {
                    int ped = PlayerPed;
                    new Ped(ped).SetDrunk(true, 1f);
                    GameplayCamera.Shake("DRUNK_SHAKE", 1f);
                    PED.SET_PED_CONFIG_FLAG(ped, 100, true);
                    PostFx("PlayerDrunk01");
                    PED._SET_PED_DESIRED_LOCO_MOTION_TYPE(ped, "very_drunk");
                }, dt =>
                {
                    if (every.Tick(dt)) Ragdoll(PlayerPed, 1000);
                }, () =>
                {
                    int ped = PlayerPed;
                    new Ped(ped).SetDrunk(false);
                    GameplayCamera.StopShaking();
                    PED.SET_PED_CONFIG_FLAG(ped, 100, false);
                    StopPostFx("PlayerDrunk01");
                    new Ped(ped).ResetWalkStyle();
                });
            });

            r.Timed(C, "random_walk_style", "Rastgele yürüyüş", "25 saniye boyunca yaralı, sarhoş ya da korkak gibi yürürsün.", "walk", 25, () => new LambdaEffect(() =>
            {
                string[] styles = { "cower_known", "injured_left_leg", "injured_general", "injured_right_leg", "injured_left_arm", "injured_right_arm", "injured_torso", "very_drunk", "moderate_drunk" };
                new Ped(PlayerPed).SetWalkStyle("default", Pick(styles));
            }, null, () => new Ped(PlayerPed).ResetWalkStyle()));

            r.Timed(C, "player_minion", "Minik oyuncu", "30 saniye boyunca oyuncu küçülür.", "user-minus", 30, () =>
            {
                var every = new Interval(500);
                return new LambdaEffect(() => SetScale(PlayerPed, 0.4f), dt =>
                {
                    if (every.Tick(dt)) SetScale(PlayerPed, 0.4f);
                }, () => SetScale(PlayerPed, 1f));
            });

            r.Timed(C, "blood_trails", "Kan izi", "25 saniye boyunca oyuncu arkasında kan gölleri bırakır.", "blood", 25, () =>
            {
                var every = new Interval(5000);
                return new LambdaEffect(null, dt =>
                {
                    if (every.Tick(dt)) new Ped(PlayerPed).AddBloodPool(1f);
                });
            });

            r.Instant(C, "random_clothes", "Rastgele kıyafet", "Oyuncuya rastgele bir kıyafet takımı giydirir.", "tag", () =>
            {
                int id = Rand(58);
                if (id == 25 || id == 17 || id == 14 || id == 28) id = 0;
                OutfitPreset(PlayerPed, id);
            });

            r.Instant(C, "winter_outfit", "Kışlık kıyafet", "Arthur ya da John'a kışlık takım giydirir.", "snow", () =>
            {
                int ped = PlayerPed;
                uint model = ENTITY.GET_ENTITY_MODEL(ped);
                if (model == H("PLAYER_ZERO")) OutfitPreset(ped, 1);
                else if (model == H("PLAYER_THREE")) OutfitPreset(ped, 25);
            });

            r.Instant(C, "random_hat", "Rastgele şapka", "Oyuncuya rastgele bir şapka takar.", "hat", () =>
            {
                uint[] hats = { 0x2514B2B9, 0x05A94693, 0xB2A7CB98, 0x2968E73D, 0xAE8ACE4E, 0xD16013FC, 0x3D9CEC78, 0x5F74300A, 0x48760F4A };
                var ped = new Ped(PlayerPed);
                ped.EquipOutfitComponent(Pick(hats));
                ped.UpdateVariation();
            });

            r.Instant(C, "lose_weight", "Kilo ver", "Oyuncu zayıflar.", "minus", () => BodyShape(0x63F130D5, 0x86155956, 0x652668B6));
            r.Instant(C, "gain_weight", "Kilo al", "Oyuncu şişmanlar.", "plus", () => BodyShape(0x63F130D5, 0x74D74B1C, 0xBB7091D9));

            // ---- Player model (story-safe model change, see PlayerModel) ----------------------------------------
            AddSkin(r, "cow_skin", "İnek oldun", "20 saniye boyunca oyuncu bir inektir.", "A_C_COW", 20);
            AddSkin(r, "dwarf_skin", "Cüce oldun", "20 saniye boyunca oyuncu Magnifico'dur.", "CS_Magnifico", 20);
            AddSkin(r, "pig_skin", "Domuz oldun", "20 saniye boyunca oyuncu bir domuzdur.", "A_C_Pig_01", 20, ped => OutfitPreset(ped, Rand(4)));
            AddSkin(r, "rat_skin", "Fare oldun", "20 saniye boyunca oyuncu bir faredir.", "A_C_Rat_01", 20);
            AddSkin(r, "turtle_skin", "Kaplumbağa oldun", "40 saniye boyunca oyuncu bir kaplumbağadır.", "A_C_TurtleSnapping_01", 40);

            r.Timed(C, "bird_skin", "Kuş oldun", "20 saniye boyunca rastgele bir kuşsun; binek kontrolleriyle uçabilirsin.", "bird", 20, () =>
            {
                string[] birds =
                {
                    "A_C_BlueJay_01", "A_C_Cardinal_01", "A_C_CarolinaParakeet_01", "A_C_CedarWaxwing_01", "A_C_Chicken_01", "A_C_Cormorant_01",
                    "A_C_CraneWhooping_01", "A_C_Crow_01", "A_C_Duck_01", "A_C_Eagle_01", "A_C_Egret_01", "A_C_Hawk_01", "A_C_Heron_01",
                    "A_C_Loon_01", "A_C_Owl_01", "A_C_Parrot_01", "A_C_Pelican_01", "A_C_Pheasant_01", "A_C_Pigeon", "A_C_PrairieChicken_01",
                    "A_C_Quail_01", "A_C_Raven_01", "A_C_RedFootedBooby_01", "A_C_Rooster_01", "A_C_RoseateSpoonbill_01", "A_C_Seagull_01",
                    "A_C_TurkeyWild_01", "A_C_Vulture_01", "A_C_Woodpecker_01", "A_C_Woodpecker_02",
                };
                return new LambdaEffect(() =>
                {
                    if (PlayerModel.Change(H(Pick(birds)))) RandomOutfitPreset(PlayerPed);
                }, dt => Game.SetControlContext(2, H("OnMount")), PlayerModel.Restore);
            }, "model");

            r.Timed(C, "body_swap", "Beden değiştir", "30 saniye boyunca yakındaki bir NPC ile bedenini değiştirirsin.", "users", 30, () => new BodySwap(), "model");

            // ---- Input -------------------------------------------------------------------------------------------
            r.EveryFrame(C, "disable_left_right", "Sağ-sol yok", "25 saniye boyunca sağa sola gidemezsin.", "arrow-left", 25, () => DisableControls(LeftRight));
            r.EveryFrame(C, "disable_forward_backward", "İleri-geri yok", "25 saniye boyunca ileri geri gidemezsin.", "arrow-up", 25, () => DisableControls(ForwardBackward));
            r.EveryFrame(C, "disable_sprint_jump", "Koşma-zıplama yok", "25 saniye boyunca koşamaz, zıplayamazsın.", "run", 25, () => DisableControls(SprintJump));
            r.EveryFrame(C, "disable_movements", "Kıpırdayamazsın", "20 saniye boyunca hiç hareket edemezsin.", "lock", 20, DisableAllMovements);
            r.EveryFrame(C, "disable_attack", "Saldırı yok", "20 saniye boyunca ateş edemez, yumruk atamazsın.", "fist", 20, () => DisableControls(Attack));

            r.Timed(C, "disable_aiming", "Nişan alma yok", "20 saniye boyunca nişan alamazsın, nişangâh da kaybolur.", "crosshair", 20, () => new LambdaEffect(
                () => PushHudContext("HUD_CTX_IN_FAST_TRAVEL_MENU"),
                dt => DisableControl("INPUT_AIM"),
                () => PopHudContext("HUD_CTX_IN_FAST_TRAVEL_MENU")));

            r.Timed(C, "disable_dead_eye", "Dead Eye yok", "30 saniye boyunca Dead Eye kullanılamaz.", "deadeye", 30, () => new LambdaEffect(
                () => Game.Player.DeadEyeEnabled = false, null, () => Game.Player.DeadEyeEnabled = true));

            // ---- Camera --------------------------------------------------------------------------------------------
            r.Timed(C, "eye_disorder", "Göz bozukluğu", "25 saniye boyunca görüntü bulanıklaşıp kayar.", "eye-off", 25, () => new LambdaEffect(
                () => PostFx("OJDominoBlur"), null, () => StopPostFx("OJDominoBlur")));

            r.EveryFrame(C, "first_person", "Birinci şahıs", "30 saniye boyunca kamera birinci şahısta kalır.", "eye", 30,
                () => GameplayCamera.ForceFirstPersonThisFrame());

            r.Timed(C, "top_down_camera", "Kuşbakışı kamera", "30 saniye boyunca kamera oyuncunun 10 m tepesinden bakar.", "radar", 30, () =>
            {
                var cam = new ScriptCam();
                return new LambdaEffect(() =>
                {
                    cam.Create();
                    PushHudContext("HUD_CTX_IN_FAST_TRAVEL_MENU");
                }, dt =>
                {
                    if (cam.Handle == 0) return;
                    CAM.SET_CAM_ACTIVE(cam.Handle, true);
                    Vector3 p = Pos(PlayerPed);
                    CAM.SET_CAM_ROT(cam.Handle, -90f, 0f, 0f, 2);
                    CAM.SET_CAM_COORD(cam.Handle, p.X, p.Y, p.Z + 10f);
                    CAM.SET_CAM_AFFECTS_AIMING(cam.Handle, false);
                }, () =>
                {
                    if (cam.Handle != 0) CAM.SET_CAM_AFFECTS_AIMING(cam.Handle, true);
                    cam.Destroy();
                    PopHudContext("HUD_CTX_IN_FAST_TRAVEL_MENU");
                });
            }, "camera");

            r.Instant(C, "agitate_horse", "Atı ürküt", "Oyuncunun atı şahlanır ve onu üstünden atar.", "horse", () =>
            {
                int ped = PlayerPed;
                if (OnMount(ped)) PED._HORSE_AGITATE(MountOf(ped), true);
            });

            r.Instant(C, "remove_current_vehicle", "Bineği yok et", "Oyuncunun bindiği araba ya da at ortadan kaybolur.", "x", () =>
            {
                int ped = PlayerPed;
                if (InVehicle(ped))
                {
                    DeleteVehicle(VehicleOf(ped));
                }
                else if (OnMount(ped))
                {
                    int mount = MountOf(ped);
                    Dismount(ped);
                    if (!IsMission(mount)) DeletePed(mount);
                }
            });
        }

        private static void ScaleVelocity(float onFoot, float inVehicle)
        {
            int ped = PlayerPed;
            bool usingVehicle = InVehicle(ped);
            int e = Transport(ped);
            float multiplier = onFoot;
            if (!usingVehicle)
            {
                FixInCutscene(e);
                Script.Wait(75);
                Ragdoll(e, 1000);
            }
            else
            {
                multiplier = inVehicle;
            }
            SetVelocity(e, Velocity(e) * multiplier);
        }

        private static void BodyShape(params uint[] components)
        {
            var ped = new Ped(PlayerPed);
            foreach (uint component in components) ped.EquipOutfitComponent(component);
            ped.UpdateVariation();
        }

        /// <summary>Timed effect that reacts to every new bullet impact of the player.</summary>
        private static EffectInstance ImpactEffect(Action<Vector3> onImpact)
        {
            Vector3 last = Vector3.Zero;
            return new LambdaEffect(() => LastImpact(PlayerPed, out last), dt =>
            {
                if (!LastImpact(PlayerPed, out Vector3 hit) || hit == last) return;
                last = hit;
                onImpact(hit);
            });
        }

        private static void AddSkin(EffectRegistry r, string id, string name, string description, string model, int seconds, Action<int> after = null)
        {
            r.Timed(C, id, name, description, "user", seconds, () => new LambdaEffect(() =>
            {
                if (PlayerModel.Change(H(model))) after?.Invoke(PlayerPed);
            }, null, PlayerModel.Restore), "model");
        }

        // Script globals found in ChaosModRDR (game builds 1311/1436): Global_11170 is cleared, Global_1347477.f_156 is the
        // honor change the story scripts apply on the next frame. Unverified on other builds -> experimental effects.
        private static void AddHonor(EffectRegistry r, string id, string name, string description, string icon, int delta, bool reset)
        {
            EffectDef def = r.Instant(C, id, name, description + " (Deneysel: oyun sürümüne bağlı global yazar.)", icon, () =>
            {
                if (reset) Game.GetGlobalPtr(0x2BA2).Set(0);
                Game.GetGlobalPtr(1347477 + 155 + 1).Set(delta != 0 ? delta : Rand(51) - 25);
            });
            def.Experimental = true;
        }

        /// <summary>Teleports like "tp_to_random", comes back after 7 s and only then shows its real name.</summary>
        private sealed class FakeTeleport : EffectInstance
        {
            private readonly string _disguise;
            private readonly Interval _back = new Interval(7000);
            private Vector3 _old;
            private bool _returned;

            public FakeTeleport(string disguise)
            {
                _disguise = disguise;
            }

            public override string DisplayName => _returned ? Def.Name : _disguise;

            public override void Start()
            {
                _old = Pos(PlayerPed);
                TeleportPlayer(Pick(RandomLocations));
            }

            public override void Tick(int dtMs)
            {
                if (!_returned && _back.Tick(dtMs)) Return();
            }

            public override void Stop()
            {
                if (!_returned) Return();
            }

            private void Return()
            {
                TeleportPlayer(_old);
                _returned = true;
            }
        }

        /// <summary>The player takes a nearby ped's body and place; a clone of the player wanders off in the ped's.</summary>
        private sealed class BodySwap : EffectInstance
        {
            private uint _pedModel;
            private int _clone;

            public override void Start()
            {
                var valid = new System.Collections.Generic.List<int>();
                var mission = new System.Collections.Generic.List<int>();
                foreach (int p in NearbyPeds(50))
                {
                    if (!IsHuman(p)) continue;
                    if (!IsMission(p) || SpawnedPeds.Contains(p)) valid.Add(p);
                    else mission.Add(p);
                }
                bool useMission = valid.Count == 0 && mission.Count > 0;
                bool useRandomSkin = valid.Count == 0 && mission.Count == 0;

                int player = PlayerPed;
                int ped = 0;
                if (!useRandomSkin)
                {
                    ped = useMission ? Pick(mission) : Pick(valid);
                    if (!Exists(ped)) return;
                }

                Vector3 playerPos = Pos(player);
                Vector3 pedPos = Vector3.Zero;
                float pedHeading = 0f;
                int playerVehicle = 0, playerMount = 0, playerSeat = -2;
                int pedVehicle = 0, pedMount = 0, pedSeat = -2;

                _pedModel = useRandomSkin
                    ? H(Pick(new[] { "a_m_m_vallaborer_01", "a_m_m_valtownfolk_01", "a_m_m_valtownfolk_02", "a_m_m_tumtownfolk_01", "a_f_m_valtownfolk_01", "a_m_m_asbtownfolk_01" }))
                    : ENTITY.GET_ENTITY_MODEL(ped);

                if (!useMission && !useRandomSkin)
                {
                    pedPos = Pos(ped);
                    pedHeading = Heading(ped);
                    if (InVehicle(player))
                    {
                        playerVehicle = VehicleOf(player);
                        playerSeat = PED.GET_SEAT_PED_IS_USING(player);
                    }
                    else if (OnMount(player))
                    {
                        playerMount = MountOf(player);
                        playerSeat = PED.GET_SEAT_PED_IS_USING(player);
                        Dismount(player);
                        ENTITY.SET_ENTITY_AS_MISSION_ENTITY(playerMount, true, true);
                    }
                    if (InVehicle(ped))
                    {
                        pedVehicle = VehicleOf(ped);
                        pedSeat = PED.GET_SEAT_PED_IS_USING(ped);
                    }
                    else if (OnMount(ped))
                    {
                        pedMount = MountOf(ped);
                        pedSeat = PED.GET_SEAT_PED_IS_USING(ped);
                    }
                    DeletePed(ped);

                    // A clone of the player takes the player's place and wanders off
                    _clone = PED.CLONE_PED(player, false, true, true);
                    if (Exists(_clone))
                    {
                        SpawnedPeds.Add(_clone);
                        if (playerVehicle != 0)
                        {
                            SetIntoVehicle(_clone, playerVehicle, playerSeat);
                            TASK.TASK_VEHICLE_DRIVE_WANDER(_clone, playerVehicle, 100000f, 0x400C0025);
                        }
                        else if (playerMount != 0)
                        {
                            SetOnMount(_clone, playerMount, playerSeat);
                            TASK.TASK_WANDER_STANDARD(_clone, 100f, 10);
                        }
                        else
                        {
                            SetPos(_clone, playerPos);
                            TASK.TASK_WANDER_STANDARD(_clone, 100f, 10);
                        }
                    }
                }

                if (!PlayerModel.Change(_pedModel)) return;
                player = PlayerPed;

                if (!useMission && !useRandomSkin)
                {
                    if (pedVehicle != 0) SetIntoVehicle(player, pedVehicle, pedSeat);
                    else if (pedMount != 0) SetOnMount(player, pedMount, pedSeat);
                    else
                    {
                        SetPos(player, pedPos);
                        SetHeading(player, pedHeading);
                    }
                }
                if (useRandomSkin) RandomOutfitPreset(player);
            }

            public override void Stop()
            {
                // Player first: during shutdown nothing may wait, and the respawn below loads a model
                PlayerModel.Restore();
                if (Exists(_clone))
                {
                    // The ped comes back where the clone walked to
                    int ped = NoWait ? 0 : SpawnPed(_pedModel, false, false);
                    if (ped != 0)
                    {
                        SetPos(ped, Pos(_clone));
                        TASK.TASK_WANDER_STANDARD(ped, 100f, 10);
                    }
                    DeletePed(_clone);
                }
                _clone = 0;
            }
        }
    }
}
