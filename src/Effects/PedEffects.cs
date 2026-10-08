// NPC effects (ChaosModRDR src/Effects/peds.cpp).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using RDR2;
using RDR2.Math;
using RDR2.Native;
using RDR2.UI;
using static StreamEmber.ChaosMod.Fx;

namespace StreamEmber.ChaosMod
{
    internal static class PedEffects
    {
        private const EffectCategory C = EffectCategory.Peds;

        private static readonly string[] TownFolk =
        {
            "a_m_m_vallaborer_01", "a_m_m_valtownfolk_01", "a_m_m_valtownfolk_02", "a_m_m_tumtownfolk_01", "a_f_m_valtownfolk_01", "a_m_m_asbtownfolk_01",
        };

        public static void Register(EffectRegistry r)
        {
            // ---- Companions ----------------------------------------------------------------------------------------
            r.Instant(C, "spawn_soldier", "Asker yoldaş", "Keskin nişancı tüfekli bir asker yanına katılır.", "soldier", () =>
            {
                int ped = SpawnPed("s_m_m_army_01");
                if (ped == 0) return;
                MarkCompanion(ped);
                RemoveAllWeapons(ped);
                GiveWeapon(ped, "WEAPON_SNIPERRIFLE_ROLLINGBLOCK", 9999);
            });

            r.Instant(C, "spawn_lenny", "Yoldaş Lenny", "Lenny (ya da salon kızı) tüfeğiyle yanına katılır.", "friend", () => SpawnLenny(true));

            r.Instant(C, "spawn_chicken", "Tavuk yoldaş", "Bir tavuk peşine takılır.", "bird", () => Companion("A_C_Chicken_01"));
            r.Instant(C, "spawn_cat_companion", "Kedi yoldaş", "Bir kedi peşine takılır.", "cat", () => Companion("A_C_Cat_01"));
            r.Instant(C, "spawn_dog_companion", "Köpek yoldaş", "Rastgele cins bir köpek peşine takılır.", "dog", () => Companion(Pick(new[]
            {
                "A_C_DogAmericanFoxhound_01", "A_C_DogAustralianSheperd_01", "A_C_DogBluetickCoonhound_01", "A_C_DogCatahoulaCur_01",
                "A_C_DogChesBayRetriever_01", "A_C_DogCollie_01", "A_C_DogHobo_01", "A_C_DogHound_01", "A_C_DogHusky_01",
            })));

            r.Instant(C, "spawn_bear_companion", "Ayı yoldaş", "Bir kara ayı senin için savaşır.", "bat-animal", () =>
            {
                int ped = Companion("A_C_BearBlack_01");
                if (ped == 0) return;
                PED.SET_PED_COMBAT_ATTRIBUTES(ped, 17, false);
                PED.SET_PED_COMBAT_ATTRIBUTES(ped, 5, true);
                PED.SET_PED_COMBAT_ATTRIBUTES(ped, 46, true);
            });

            r.Instant(C, "parrot_companion", "Papağan yoldaş", "Bir papağan omzunun hizasında belirir.", "bird", () =>
            {
                int ped = Companion("A_C_Parrot_01");
                if (ped == 0) return;
                SetHealth(ped, 1000);
                int player = PlayerPed;
                if (!InVehicle(player) && !OnMount(player))
                {
                    Vector3 p = Pos(player);
                    p.Z += 2f;
                    SetPos(ped, p);
                }
            });

            r.Instant(C, "spawn_bertram", "Yoldaş Bertram", "Garip Bertram (1000 can) yanına katılır.", "user", () =>
            {
                int ped = SpawnPed("CS_ODDFELLOWSPINHEAD");
                if (ped == 0) return;
                SetScale(ped, 1.25f);
                MarkCompanion(ped);
                SetHealth(ped, 1000);
                WEAPON.REMOVE_ALL_PED_WEAPONS(ped, true, false);
            });

            r.Instant(C, "spawn_robot", "Robot yoldaş", "Marko'nun robotu (10000 can) yanına katılır.", "robot", () =>
            {
                int ped = SpawnPed("CS_crackpotRobot");
                if (ped == 0) return;
                SetHealth(ped, 10000);
                MarkCompanion(ped);
            });

            r.Instant(C, "spawn_twitch_viewer", "İzleyici NPC'si", "Adı başının üstünde yazan bir izleyici yoldaş belirir (entegrasyonda gerçek izleyici adı gelecek).", "viewer",
                () => SpawnViewer("İzleyici " + (Rand(9000) + 1000)));

            // ---- Mounts --------------------------------------------------------------------------------------------
            r.Instant(C, "spawn_horse", "Arap atı", "Beyaz bir Arap atı belirir ve üstüne binersin.", "horse", () => MountPlayer("A_C_Horse_Arabian_White"));
            r.Instant(C, "spawn_shire_horse", "Shire atı", "Dev bir Shire atı belirir ve üstüne binersin.", "horse", () => MountPlayer("A_C_Horse_Shire_DarkBay"));
            r.Instant(C, "spawn_mule", "Katır", "Bir katır belirir ve üstüne binersin.", "horse", () => MountPlayer("A_C_HorseMule_01"));
            r.Instant(C, "spawn_donkey", "Eşek", "Bir eşek belirir ve üstüne binersin.", "horse", () => MountPlayer("A_C_Donkey_01"));
            r.Instant(C, "spawn_mini_donkey", "Mini eşek", "Minicik bir eşek belirir ve üstüne binersin.", "horse", () => MountPlayer("A_C_Donkey_01", 0.42f));
            r.Instant(C, "spawn_giant_donkey", "Dev eşek", "Kocaman bir eşek belirir ve üstüne binersin.", "horse", () => MountPlayer("A_C_Donkey_01", 1.5f));

            // ---- Enemies -------------------------------------------------------------------------------------------
            r.Instant(C, "spawn_jon", "Sarhoş Jon", "Sarhoş ve 1000 canlı Jon sana saldırır.", "whiskey", () =>
            {
                int ped = SpawnPed("CS_GrizzledJon", false, true);
                if (ped == 0) return;
                SetHealth(ped, 1000);
                MarkEnemy(ped);
                PED._SET_PED_DRUNKNESS(ped, true, 0.75f);
                NoCriticalHits(ped);
            });

            r.Instant(C, "spawn_serial_killer", "Seri katil", "Eşek sırtında bir katil peşine düşer.", "skull", () =>
            {
                int horse = SpawnPed("A_C_Donkey_01");
                int ped = SpawnPed("G_M_M_UniDuster_03");
                if (ped == 0) return;
                if (horse != 0) SetOnMount(ped, horse, -1);
                MarkEnemy(ped);
                NoCriticalHits(ped);
            });

            r.Instant(C, "spawn_vampire", "Vampir", "Saint Denis'in vampiri (1000 can) sana saldırır.", "moon", () => Enemy("CS_Vampire", 1000));

            r.Instant(C, "spawn_giant_cop", "Dev şerif", "10 kat büyüklüğünde bir şerif seni izleyerek dolaşır.", "sheriff", () =>
            {
                int ped = SpawnPed("S_M_M_DispatchLeaderPolice_01", false, false);
                if (ped == 0) return;
                SetScale(ped, 10f);
                PED.SET_PED_RELATIONSHIP_GROUP_HASH(ped, H("COP"));
                TASK.TASK_LOOK_AT_ENTITY(ped, PlayerPed, -1, 2048, 3, 1);
                TASK.TASK_WANDER_STANDARD(ped, 10f, 10);
            });

            r.Instant(C, "spawn_skeleton", "Öfkeli iskelet", "İskelet kılığında biri (500 can, bazen kırık kılıçla) saldırır.", "skull-crossbones", () =>
            {
                string[] skins = { "U_M_M_CircusWagon_01", "A_M_M_UniCorpse_01", "A_F_M_UniCorpse_01" };
                int skin = Rand(skins.Length);
                int ped = SpawnPed(skins[skin]);
                if (ped == 0) return;
                if (skin == 1) OutfitPreset(ped, Pick(new[] { 31, 36, 46, 56, 68, 143, 144, 147 }));
                else if (skin == 2) OutfitPreset(ped, Pick(new[] { 11, 12, 27, 28 }));
                SetHealth(ped, 500);
                if (Coin()) GiveWeapon(ped, "WEAPON_MELEE_BROKEN_SWORD", 1);
                MarkEnemy(ped);
                NoCriticalHits(ped);
            });

            r.Instant(C, "spawn_dwarf", "Öfkeli cüce", "Bıçaklı Magnifico mini eşeğiyle saldırır.", "knife", () =>
            {
                int horse = SpawnPed("A_C_Donkey_01");
                if (horse != 0) SetScale(horse, 0.5f);
                int ped = SpawnPed("CS_Magnifico", false);
                if (ped == 0) return;
                GiveWeapon(ped, "WEAPON_MELEE_KNIFE", 9999);
                if (horse != 0) SetOnMount(ped, horse, -1);
                MarkEnemy(ped);
                int player = PlayerPed;
                if (!InVehicle(player) && !OnMount(player)) Dismount(ped);
            });

            r.Instant(C, "spawn_frozen_couple", "Donmuş çift", "Donarak ölmüş bir çift dirilip saldırır.", "snow", () =>
            {
                foreach (string model in new[] { "RE_FROZENTODEATH_FEMALES_01", "RE_FROZENTODEATH_MALES_01" })
                {
                    int ped = SpawnPed(model);
                    if (ped == 0) continue;
                    MarkEnemy(ped);
                    NoCriticalHits(ped);
                }
            });

            r.Instant(C, "lasso_guy", "Kementçi", "Egzotik koleksiyoncu seni kementle yakalamaya çalışır.", "lasso", () =>
            {
                int ped = SpawnPed("CS_EXOTICCOLLECTOR", false, true);
                if (ped == 0) return;
                MarkEnemy(ped);
                GiveWeapon(ped, "WEAPON_LASSO", 100);
                TASK.TASK_LASSO_PED(ped, PlayerPed);
                NoCriticalHits(ped);
            });

            r.Instant(C, "spawn_angry_corpse", "Öfkeli ceset", "Koleradan ölmüş bir ceset (300 can) saldırır.", "zombie", () =>
            {
                int ped = Enemy("A_M_M_ARMCHOLERACORPSE_01", 300);
                if (ped != 0) OutfitPreset(ped, 13);
            });

            r.Instant(C, "spawn_angry_caveman", "Öfkeli mağara adamı", "Mağara adamı (500 can) saldırır.", "fist", () =>
            {
                int ped = Enemy("A_M_M_UniCorpse_01", 500);
                if (ped == 0) return;
                OutfitPreset(ped, 44);
                SetScale(ped, 1.1f);
            });

            r.Instant(C, "spawn_angry_cowboy", "Öfkeli kovboy", "Revolverli bir kovboy (500 can) saldırır.", "cowboy", () =>
            {
                int ped = SpawnPed("S_M_M_ValCowpoke_01", false, true);
                if (ped == 0) return;
                SetHealth(ped, 500);
                OutfitPreset(ped, Rand(22));
                GiveWeapon(ped, "WEAPON_REVOLVER_SCHOFIELD", 200);
                MarkEnemy(ped);
                NoCriticalHits(ped);
            });

            r.Instant(C, "spawn_tommy", "Öfkeli Tommy", "Dev Tommy (1000 can) yumruklarıyla saldırır.", "boxing", () =>
            {
                int ped = SpawnPed("CS_mud2bigguy", false, true);
                if (ped == 0) return;
                SetHealth(ped, 1000);
                MarkEnemy(ped);
                SetScale(ped, 1.1f);
                OutfitPreset(ped, 1);
                NoCriticalHits(ped);
            });

            r.Instant(C, "spawn_angry_twin", "Öfkeli ikiz", "Oyuncunun bir kopyası aynı silahla sana saldırır.", "users", () =>
            {
                int player = PlayerPed;
                int ped = PED.CLONE_PED(player, false, false, true);
                if (!Exists(ped)) return;
                SpawnedPeds.Add(ped);
                if (PED.IS_PED_IN_ANY_VEHICLE(player, false))
                {
                    SetIntoVehicle(ped, VehicleOf(player), -2);
                }
                else if (OnMount(player))
                {
                    int mount = MountOf(player);
                    if (IsMountSeatFree(mount, 0)) SetOnMount(ped, mount, 0);
                }
                ENTITY.SET_ENTITY_INVINCIBLE(ped, false);
                ENTITY.SET_ENTITY_PROOFS(ped, 0, false);
                uint weapon = CurrentWeapon(player);
                if (weapon != 0 && weapon != H("WEAPON_UNARMED"))
                {
                    WEAPON.GIVE_DELAYED_WEAPON_TO_PED(ped, weapon, 100, true, AddReason);
                    WEAPON.SET_PED_AMMO(ped, weapon, 100);
                    WEAPON.SET_CURRENT_PED_WEAPON(ped, weapon, true, 0, false, false);
                }
                MarkEnemy(ped);
            });

            r.Instant(C, "spawn_undead_boss", "Ölümsüz patron", "Gece çöker, sis basar; dev bir ölümsüz kadim tomahawk'larla saldırır.", "boss", () =>
            {
                World.SetClockTime(22);
                SetWeather("FOG");
                int ped = SpawnPed("A_M_M_UniCorpse_01", false, false);
                if (ped == 0) return;
                SetHealth(ped, 700);
                OutfitPreset(ped, Coin() ? 68 : 45);
                SetScale(ped, 1.6f);
                GiveWeapon(ped, "WEAPON_THROWN_TOMAHAWK_ANCIENT", 100);
                WEAPON.SET_ALLOW_ANY_WEAPON_DROP(ped, false);
                PED._SET_PED_DESIRED_LOCO_MOTION_TYPE(ped, "injured_right_leg");
                NoCriticalHits(ped);
                PED.SET_PED_CONFIG_FLAG(ped, 340, true);   // PCF_DisableAllMeleeTakedowns
                PED.SET_RAGDOLL_BLOCKING_FLAGS(ped, 1 | 2 | 16 | 128 | 512 | 8192);
                MarkEnemy(ped);
                var boss = new Ped(ped);
                boss.EquipOutfitComponent(0x48760F4A);
                boss.UpdateVariation();
            });

            r.Instant(C, "undead_nightmare", "Undead Nightmare", "Gece ve sis; etrafında beş zombi belirir.", "zombie", () =>
            {
                World.SetClockTime(22);
                SetWeather("FOG");
                Vector3 center = Pos(PlayerPed);
                for (int i = 0; i < 5; i++)
                {
                    int zombie = SpawnPed(i == 0 ? "A_M_M_ARMCHOLERACORPSE_01" : "A_M_M_UniCorpse_01", false);
                    if (zombie == 0) continue;
                    if (i != 0) OutfitPreset(zombie, Rand(187));
                    double angle = i * (360.0 / 5) * Math.PI / 180.0;
                    SetPos(zombie, new Vector3(center.X + 10f * (float)Math.Sin(angle), center.Y + 10f * (float)Math.Cos(angle), center.Z));
                    PED._SET_PED_DESIRED_LOCO_MOTION_TYPE(zombie, "very_drunk");
                    MarkEnemy(zombie);
                    PED.SET_PED_COMBAT_ATTRIBUTES(zombie, 1, false);
                    AddBlip(zombie, "BLIP_STYLE_ENEMY");
                }
            });

            r.Instant(C, "spawn_greifer_micah", "Trol Micah", "Micah dinamit ya da molotofla saldırır.", "molotov", () =>
            {
                int ped = SpawnPed("CS_MicahBell", false, true);
                if (ped == 0) return;
                SetHealth(ped, 500);
                OutfitPreset(ped, 10);
                GiveWeapon(ped, Coin() ? "WEAPON_THROWN_DYNAMITE" : "WEAPON_THROWN_MOLOTOV", 20);
                NoCriticalHits(ped);
                if (OnMount(ped)) GiveWeapon(ped, "WEAPON_REVOLVER_SCHOFIELD_GOLDEN", 100);
                MarkEnemy(ped);
            });

            r.Timed(C, "spawn_evil_micah", "Aşırı trol Micah", "45 saniye: patlamaya dayanıklı Micah yay atar, okları patlar.", "bow", 45, () =>
            {
                int micah = 0;
                Vector3 last = Vector3.Zero;
                return new LambdaEffect(() =>
                {
                    micah = SpawnPed("CS_MicahBell", false, true);
                    if (micah == 0) return;
                    OutfitPreset(micah, 10);
                    ENTITY.SET_ENTITY_PROOFS(micah, 4, false);   // explosion proof
                    MarkEnemy(micah);
                    RemoveAllWeapons(micah);
                    GiveWeapon(micah, "WEAPON_BOW", 9999);
                    SetHealth(micah, 800);
                    NoCriticalHits(micah);
                    LastImpact(micah, out last);
                }, dt =>
                {
                    if (!Exists(micah) || IsDead(micah)) return;
                    if (!LastImpact(micah, out Vector3 hit) || hit == last) return;
                    last = hit;
                    Explosion(hit);
                });
            });

            r.Instant(C, "spawn_predator", "Yırtıcı", "Timsah, ayı, puma, aslan, panter ya da kurt saldırır.", "hostile", () =>
            {
                string model = Pick(new[] { "A_C_Alligator_01", "A_C_Bear_01", "A_C_Cougar_01", "A_C_LionMangy_01", "A_C_Panther_01", "A_C_Wolf" });
                int ped = SpawnPed(model, false, false);
                if (ped == 0) return;
                int count = PED.GET_NUM_META_PED_OUTFITS(ped);
                int outfit = Rand(count);
                if (model == "A_C_Bear_01" && (outfit == 9 || outfit == 10)) outfit = 0;
                OutfitPreset(ped, outfit);
                MarkEnemy(ped);
            });

            r.Instant(C, "spawn_odriscolls", "O'Driscoll çetesi", "Atlı iki O'Driscoll karabinalarla saldırır.", "outlaw", () =>
            {
                int a = SpawnPed("g_m_m_uniduster_01", false, false);
                int b = SpawnPed("g_m_m_uniduster_01", false, false);
                int mount = SpawnPed("A_C_Horse_Arabian_Black", false, false);
                if (a != 0 && mount != 0) SetOnMount(a, mount, -1);
                foreach (int ped in new[] { a, b })
                {
                    if (ped == 0) continue;
                    GiveWeapon(ped, "WEAPON_REPEATER_CARBINE", 9999);
                    OutfitPreset(ped, Rand(184));
                    SetHealth(ped, 300);
                    NoCriticalHits(ped);
                    MarkEnemy(ped);
                }
            });

            r.Instant(C, "bandito_kidnaps", "Bandito kaçırıyor", "Eşekli bir bandito seni kementle yakalamaya çalışır.", "lasso", () =>
            {
                int donkey = SpawnPed("A_C_Donkey_01", false, false);
                int ped = SpawnPed("G_M_M_UNIBANDITOS_01", false, false);
                if (ped == 0) return;
                if (donkey != 0) SetOnMount(ped, donkey, -1);
                MarkEnemy(ped);
                GiveWeapon(ped, "WEAPON_LASSO", 100);
                TASK.TASK_LASSO_PED(ped, PlayerPed);
                NoCriticalHits(ped);
                OutfitPreset(ped, 0);
            });

            // ---- Scripted scenes -----------------------------------------------------------------------------------
            r.Timed(C, "kidnapping", "Domuz çiftçileri kaçırdı", "Aberdeen kardeşler seni hapis arabasına atıp götürür; 15 saniye inemezsin.", "handcuffs", 15, () => new LambdaEffect(() =>
            {
                int farmer = SpawnPed("CS_AberdeenPigFarmer");
                int sister = SpawnPed("CS_AberdeenSister");
                uint kidnappers = AddRelationshipGroup("_CHAOS_KIDNAPPERS");
                uint playerGroup = H("PLAYER");
                PED.SET_RELATIONSHIP_BETWEEN_GROUPS(3, kidnappers, playerGroup);
                PED.SET_RELATIONSHIP_BETWEEN_GROUPS(3, playerGroup, kidnappers);
                foreach (int ped in new[] { farmer, sister })
                {
                    if (ped == 0) continue;
                    PED.SET_PED_RELATIONSHIP_GROUP_HASH(ped, kidnappers);
                    PED.SET_PED_COMBAT_ATTRIBUTES(ped, 3, false);   // BF_CanLeaveVehicle
                }
                int player = PlayerPed;
                int wagon = SpawnVehicle("WAGONPRISON01X", Pos(player), Heading(player));
                if (wagon == 0) return;
                DECORATOR.DECOR_SET_BOOL(wagon, "wagon_block_honor", true);
                if (farmer != 0) SetIntoVehicle(farmer, wagon, -1);
                if (sister != 0) SetIntoVehicle(sister, wagon, 0);
                SetIntoVehicle(player, wagon, 1);
                if (farmer != 0)
                {
                    TASK.TASK_VEHICLE_DRIVE_WANDER(farmer, wagon, 100000f, 0x400C0025);
                    PED.SET_PED_KEEP_TASK(farmer, true);
                }
            }, dt => DisableControl("INPUT_VEH_EXIT")));

            r.Timed(C, "skyrim_intro", "Sonunda uyandın", "Skyrim girişi: hapis arabasında uyanırsın; 20 saniye inemezsin, kamera birinci şahıs.", "telegram", 20, () => new LambdaEffect(() =>
            {
                PostFx("PlayerWakeUpInterrogation");
                int player = PlayerPed;
                Vector3 p = Pos(player);
                int cop = SpawnPed("U_M_O_BlWPoliceChief_01", false);
                int p1 = SpawnPed("RE_PRISONWAGON_MALES_01", false);
                int p2 = SpawnPed("RE_LONEPRISONER_MALES_01", false);
                int p3 = SpawnPed("CS_chainprisoner_01", false);
                uint group = AddRelationshipGroup("_CHAOS_WAGON");
                uint playerGroup = H("PLAYER");
                PED.SET_RELATIONSHIP_BETWEEN_GROUPS(3, group, playerGroup);
                PED.SET_RELATIONSHIP_BETWEEN_GROUPS(3, playerGroup, group);
                foreach (int ped in new[] { cop, p1, p2, p3 })
                {
                    if (ped == 0) continue;
                    PED.SET_PED_RELATIONSHIP_GROUP_HASH(ped, group);
                    PED.SET_PED_COMBAT_ATTRIBUTES(ped, 3, false);
                }
                if (cop != 0) GiveWeapon(cop, "WEAPON_REPEATER_WINCHESTER", 100, false);
                int wagon = SpawnVehicle("wagon03x", p, Heading(player));
                if (wagon == 0) return;
                if (cop != 0) SetIntoVehicle(cop, wagon, -1);
                SetIntoVehicle(player, wagon, 1);
                if (p1 != 0) SetIntoVehicle(p1, wagon, 2);
                if (p2 != 0) SetIntoVehicle(p2, wagon, 3);
                if (p3 != 0) SetIntoVehicle(p3, wagon, 4);
                VEHICLE.SET_VEHICLE_ON_GROUND_PROPERLY(wagon, false);
                if (cop != 0)
                {
                    TASK.TASK_VEHICLE_DRIVE_WANDER(cop, wagon, 10000f, 0x400C0025);
                    PED.SET_PED_KEEP_TASK(cop, true);
                }
            }, dt =>
            {
                DisableControl("INPUT_VEH_EXIT");
                GameplayCamera.ForceFirstPersonThisFrame();
            }));

            r.Timed(C, "dutch_steals_veh", "Dutch bineğini çaldı", "Dutch gelir, atını ya da arabanı alıp gider (20 saniye).", "outlaw", 20, () =>
            {
                int dutch = 0;
                return new LambdaEffect(() =>
                {
                    int player = PlayerPed;
                    bool usingVehicle = InVehicle(player);
                    bool usingHorse = OnMount(player);
                    if (!usingVehicle && !usingHorse) return;
                    dutch = SpawnPed("CS_Dutch", false, false);
                    if (dutch == 0) return;
                    MarkCompanion(dutch);
                    if (usingHorse)
                    {
                        int mount = MountOf(player);
                        if (IsMountSeatFree(mount, 0)) SetOnMount(player, mount, 0);
                        else Dismount(player);
                        SetOnMount(dutch, mount, -1);
                        TASK.TASK_WANDER_STANDARD(dutch, 100f, 10);
                    }
                    else
                    {
                        int veh = VehicleOf(player);
                        if (!VEHICLE.IS_VEHICLE_SEAT_FREE(veh, -1))
                        {
                            int driver = VEHICLE.GET_PED_IN_VEHICLE_SEAT(veh, -1);
                            int seats = SeatCount(veh) - 1;
                            bool moved = false;
                            for (int i = 0; i < seats; i++)
                            {
                                if (!VEHICLE.IS_VEHICLE_SEAT_FREE(veh, i)) continue;
                                SetIntoVehicle(driver, veh, i);
                                moved = true;
                                break;
                            }
                            if (!moved)
                            {
                                Vector3 p = Pos(veh);
                                p.Z += 1.5f;
                                SetPos(driver, p);
                            }
                        }
                        SetIntoVehicle(dutch, veh, -1);
                        TASK.TASK_VEHICLE_DRIVE_WANDER(dutch, veh, 100000f, 0x400C0025);
                    }
                    PED.SET_PED_KEEP_TASK(dutch, true);
                    OutfitPreset(dutch, 15);
                }, null, () =>
                {
                    if (Exists(dutch)) PED.REMOVE_PED_FROM_GROUP(dutch);
                });
            });

            // ---- Crowd -------------------------------------------------------------------------------------------
            r.Timed(C, "peds_bhop", "NPC'ler zıplıyor", "25 saniye boyunca yakındaki insanlar durmadan zıplar.", "arrow-up", 25, () =>
            {
                var every = new Interval(500);
                return new LambdaEffect(null, dt =>
                {
                    if (!every.Tick(dt)) return;
                    foreach (int ped in NearbyPeds(50))
                    {
                        if (!IsHuman(ped)) continue;
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
                    }
                });
            });

            r.Timed(C, "peds_spin", "Dönen NPC'ler", "30 saniye boyunca yakındaki herkes döner.", "repeat", 30, () =>
            {
                var every = new Interval(500, true);
                var peds = new List<int>();
                float heading = 0f;
                return new LambdaEffect(null, dt =>
                {
                    if (every.Tick(dt))
                    {
                        peds.Clear();
                        peds.AddRange(NearbyPeds(50));
                    }
                    heading = (heading + 625f * dt / 1000f) % 360f;
                    foreach (int ped in peds)
                    {
                        if (!Exists(ped) || InVehicle(ped) || OnMount(ped)) continue;
                        SetHeading(ped, heading);
                    }
                });
            });

            r.Timed(C, "peds_follow_player", "Peşimdeler", "30 saniye boyunca yakındaki NPC'ler seni takip eder.", "users", 30, () =>
            {
                var every = new Interval(1000, true);
                var peds = new HashSet<int>();
                return new LambdaEffect(null, dt =>
                {
                    if (!every.Tick(dt)) return;
                    int player = PlayerPed;
                    foreach (int ped in NearbyPeds(50))
                    {
                        if (peds.Contains(ped) || IsMission(ped)) continue;
                        TASK.TASK_FOLLOW_TO_OFFSET_OF_ENTITY(ped, player, 0f, 0f, 0f, 4.5f, -1, -1f, false, false, false, false, false, false);
                        peds.Add(ped);
                    }
                }, () =>
                {
                    foreach (int ped in peds)
                    {
                        if (!Exists(ped)) continue;
                        TASK.CLEAR_PED_TASKS_IMMEDIATELY(ped, true, true);
                        TASK.TASK_WANDER_STANDARD(ped, 10f, 10);
                    }
                });
            });

            r.Instant(C, "peds_fleeing", "Herkes kaçıyor", "Yakındaki NPC'ler senden kaçar.", "run", () =>
            {
                int player = PlayerPed;
                foreach (int ped in NearbyPeds(50))
                {
                    if (IsMission(ped)) continue;
                    TASK.TASK_SMART_FLEE_PED(ped, player, 70f, 10000, 0, 3f, 0);
                }
            });

            r.Instant(C, "heal_nearby_peds", "NPC'leri iyileştir", "Yakındaki herkesin canı dolar.", "heart-plus", () =>
            {
                foreach (int ped in NearbyPeds(45))
                {
                    ENTITY.SET_ENTITY_HEALTH(ped, ENTITY.GET_ENTITY_MAX_HEALTH(ped, false), 0);
                }
            });

            r.Instant(C, "revive_dead_peds", "Ölüleri dirilt", "Ölmüş NPC'ler dirilir ve sana düşman olur.", "revive", () =>
            {
                int player = PlayerPed;
                int done = 0;
                foreach (Ped p in World.GetAllPeds())
                {
                    if (p == null || done >= 150) break;
                    int ped = p.Handle;
                    if (!Exists(ped) || ped == player) continue;
                    if (PED.IS_PED_DEAD_OR_DYING(ped, true))
                    {
                        int health = ENTITY.GET_ENTITY_MAX_HEALTH(ped, false);
                        PED.RESURRECT_PED(ped);
                        SetHealth(ped, health);
                        PED.REVIVE_INJURED_PED(ped);
                        TASK.CLEAR_PED_TASKS_IMMEDIATELY(ped, false, true);
                        MarkEnemy(ped);
                        Ragdoll(ped, 500);
                        done++;
                    }
                    if (PED.IS_PED_INJURED(ped)) PED.REVIVE_INJURED_PED(ped);
                }
            });

            r.Timed(C, "party_time", "Parti zamanı", "25 saniye: herkes (sen de) cancan dansı yapar.", "music", 25, () => new PartyTime());

            r.Instant(C, "explode_nearby_peds", "NPC'leri patlat", "Yakındaki en fazla 15 kişi patlar (atlar hariç).", "bomb", () =>
            {
                int count = 0;
                foreach (int ped in NearbyPeds(50))
                {
                    if (!Exists(ped) || IsHorse(ped)) continue;
                    Explosion(Pos(ped));
                    ENTITY.SET_ENTITY_HEALTH(ped, 0, 0);
                    if (++count >= 15) break;
                }
            });

            r.Instant(C, "nearby_ped_is_companion", "Yakındaki NPC yoldaşın", "Yakındaki rastgele biri karabinayla yanına katılır.", "user-plus", () =>
            {
                var candidates = new List<int>();
                foreach (int ped in NearbyPeds(50))
                {
                    if (!IsHorse(ped) && !ENTITY._GET_IS_BIRD(ped) && !IsMission(ped)) candidates.Add(ped);
                }
                if (candidates.Count == 0) return;
                int pick = Pick(candidates);
                TASK.CLEAR_PED_TASKS_IMMEDIATELY(pick, false, true);
                MarkCompanion(pick);
                if (IsHuman(pick)) GiveWeapon(pick, "WEAPON_REPEATER_CARBINE", 9999);
            });

            r.Instant(C, "nearby_ped_is_enemy", "Yakındaki NPC düşmanın", "Yakındaki rastgele biri karabinayla sana saldırır.", "enemy", () =>
            {
                var candidates = new List<int>();
                foreach (int ped in NearbyPeds(50))
                {
                    if (IsHuman(ped)) candidates.Add(ped);
                }
                if (candidates.Count == 0) return;
                int pick = Pick(candidates);
                GiveWeapon(pick, "WEAPON_REPEATER_CARBINE", 9999);
                MarkEnemy(pick);
                AddBlip(pick, "BLIP_STYLE_ENEMY");
            });

            r.Instant(C, "clone_enemy", "Düşmanı kopyala", "Yakındaki bir düşmanın ölümsüz olmayan bir kopyası çıkar.", "users", () =>
            {
                int player = PlayerPed;
                var enemies = new List<int>();
                foreach (int ped in NearbyPeds(50))
                {
                    if (Exists(ped) && Relationship(ped, player) == 5) enemies.Add(ped);
                }
                if (enemies.Count == 0) return;
                int source = Pick(enemies);
                int clone = PED.CLONE_PED(source, false, false, true);
                if (!Exists(clone)) return;
                SpawnedPeds.Add(clone);
                if (PED.IS_PED_IN_ANY_VEHICLE(source, false))
                {
                    int veh = VehicleOf(source);
                    if (VEHICLE.ARE_ANY_VEHICLE_SEATS_FREE(veh)) SetIntoVehicle(clone, veh, -2);
                }
                if (OnMount(source))
                {
                    int mount = MountOf(source);
                    if (IsMountSeatFree(mount, 0)) SetOnMount(clone, mount, 0);
                }
                ENTITY.SET_ENTITY_INVINCIBLE(clone, false);
                MarkEnemy(clone);
            });

            r.Timed(C, "everyone_ragdolls_when_shot", "Vurulan yere yığılır", "30 saniye: ateş eden herkes (sen de) yere yığılır.", "injured", 30,
                () => ShooterEffect((ped, hit) => Ragdoll(ped, 5000)));
            r.Timed(C, "explosive_combat", "Patlayan çatışma", "30 saniye: herkesin mermisi düştüğü yerde patlar.", "bomb", 30,
                () => ShooterEffect((ped, hit) => Explosion(hit)));

            r.Instant(C, "everyone_is_lenny", "Herkes Lenny", "Yakındaki NPC'ler yerlerinde Lenny'ye dönüşür.", "friend", EveryoneIsLenny);
        }

        #region Helpers

        private static int Companion(string model)
        {
            int ped = SpawnPed(model);
            if (ped != 0) MarkCompanion(ped);
            return ped;
        }

        private static int Enemy(string model, int health)
        {
            int ped = SpawnPed(model);
            if (ped == 0) return 0;
            SetHealth(ped, health);
            MarkEnemy(ped);
            NoCriticalHits(ped);
            return ped;
        }

        private static void MountPlayer(string model, float scale = 1f)
        {
            int horse = SpawnPed(model);
            if (horse == 0) return;
            if (Math.Abs(scale - 1f) > 0.01f) SetScale(horse, scale);
            SetOnMount(PlayerPed, horse, -1);
        }

        private static readonly string[] LennySkins = { "CS_lenny", "MSP_SALOON1_MALES_01", "MSP_SALOON1_FEMALES_01" };

        private static int LennyOutfit(int skin)
        {
            switch (skin)
            {
                case 0: return Rand(13);
                case 1: return Pick(new[] { 0, 2, 5, 7, 9, 11, 13, 15, 17, 21, 23, 25, 27, 29, 31, 33, 35, 37, 39, 41, 43 });
                default: return Pick(new[] { 2, 4, 6, 8, 10, 12, 14, 16, 18, 20 });
            }
        }

        private static void SpawnLenny(bool companion)
        {
            int skin = Rand(LennySkins.Length);
            int ped = SpawnPed(LennySkins[skin]);
            if (ped == 0) return;
            if (companion)
            {
                MarkCompanion(ped);
                RemoveAllWeapons(ped);
                GiveWeapon(ped, "WEAPON_RIFLE_SPRINGFIELD", 9999);
            }
            OutfitPreset(ped, LennyOutfit(skin));
        }

        /// <summary>A named companion (ChaosModRDR's "Spawn Twitch Viewer"); EventFabric will pass the real viewer name.</summary>
        public static int SpawnViewer(string name)
        {
            int ped = SpawnPed(Pick(TownFolk), false, true);
            if (ped == 0) return 0;
            RandomOutfitPreset(ped);
            ushort[] faces =
            {
                0x84D6, 0x3303, 0x2FF9, 0x4AD1, 0xC04F, 0xB6CE, 0x2844, 0xED30, 0x6A0B, 0xABCF, 0x358D, 0x8D0A, 0xEBAE, 0x1DF6, 0x3C0F,
                0xC3B2, 0xE323, 0x8B2B, 0x1B6B, 0x6E7F, 0x3471, 0x03F5, 0x34B1, 0xF156, 0x561E, 0xF065, 0xAA69, 0x7AC3, 0x410D, 0x1A00,
                0x91C1, 0xC375, 0xBB4D, 0xB0B0, 0x5D16,
            };
            var p = new Ped(ped);
            foreach (ushort face in faces)
            {
                p.SetFacialExpression(face, Rand(101) / 100f * 10f - 5f);
            }
            p.SetFacialExpression(0xEE44, Rand(101) / 100f * 2f + 1f);
            p.UpdateVariation();
            GiveWeapon(ped, "WEAPON_REVOLVER_SCHOFIELD", 200);
            p.SetPedPromptName(name);
            SetScale(ped, 0.9f + Rand(11) / 100f);
            SetHealth(ped, 200);
            NoCriticalHits(ped);
            MarkCompanion(ped);
            ChaosLog.Info("Spawned viewer NPC '" + name + "'");
            return ped;
        }

        /// <summary>Every 500 ms refreshes the nearby peds + player; calls <paramref name="onShooter"/> with every new bullet impact.</summary>
        private static EffectInstance ShooterEffect(Action<int, Vector3> onShooter)
        {
            var every = new Interval(500, true);
            var peds = new List<int>();
            var last = new Dictionary<int, Vector3>();
            return new LambdaEffect(null, dt =>
            {
                if (every.Tick(dt))
                {
                    peds.Clear();
                    peds.AddRange(NearbyPeds(50));
                    peds.Add(PlayerPed);
                }
                foreach (int ped in peds)
                {
                    if (!Exists(ped) || IsDead(ped)) continue;
                    if (!LastImpact(ped, out Vector3 hit)) continue;
                    if (last.TryGetValue(ped, out Vector3 prev) && prev == hit) continue;
                    bool first = !last.ContainsKey(ped);
                    last[ped] = hit;
                    if (!first) onShooter(ped, hit);
                }
            });
        }

        private static void EveryoneIsLenny()
        {
            int player = PlayerPed;
            foreach (int ped in NearbyPeds(70))
            {
                if (!Exists(ped) || IsHorse(ped)) continue;
                int relationship = Relationship(ped, player);
                if (IsMission(ped) && !SpawnedPeds.Contains(ped) && relationship < 4) continue;

                Vector3 p = Pos(ped);
                float heading = Heading(ped);
                int veh = 0, mount = 0, seat = -2;
                if (PED.IS_PED_IN_ANY_VEHICLE(ped, false))
                {
                    veh = VehicleOf(ped);
                    int seats = SeatCount(veh) - 1;
                    for (int i = -1; i < seats; i++)
                    {
                        if (VEHICLE.GET_PED_IN_VEHICLE_SEAT(veh, i) == ped)
                        {
                            seat = i;
                            break;
                        }
                    }
                }
                else if (OnMount(ped))
                {
                    mount = MountOf(ped);
                    seat = PED._GET_RIDER_OF_MOUNT(mount, false) == ped ? -1 : 0;
                }

                DeletePed(ped);

                int skin = Rand(LennySkins.Length);
                int lenny = SpawnPed(LennySkins[skin]);
                if (lenny == 0) continue;
                OutfitPreset(lenny, LennyOutfit(skin));
                SetPos(lenny, p);
                SetHeading(lenny, heading);
                if (seat != -2 && veh != 0)
                {
                    SetIntoVehicle(lenny, veh, seat);
                }
                else if (seat == -1 && mount != 0)
                {
                    SetOnMount(lenny, mount, -1);
                    TASK.TASK_WANDER_STANDARD(lenny, 10f, 10);
                }
                else
                {
                    TASK.TASK_WANDER_STANDARD(lenny, 10f, 10);
                    TASK.TASK_LOOK_AT_ENTITY(lenny, player, -1, 2048, 3, 1);
                }
                if (relationship == 5) MarkEnemy(lenny);
                // They replace ambient peds: the game may clean them up like any other
                SpawnedPeds.Remove(lenny);
                NoLongerNeeded(lenny);
            }
        }

        /// <summary>Nearby humans (and the player) dance the cancan.</summary>
        private sealed class PartyTime : EffectInstance
        {
            private const string Dict = "script_shows@cancandance@p1";
            private static readonly string[] Anims = { "cancandance_fem0", "cancandance_fem1", "cancandance_fem2", "cancandance_fem3", "cancandance_male" };
            private readonly Interval _every = new Interval(1000, true);
            private readonly Dictionary<int, string> _dancing = new Dictionary<int, string>();

            public override void Tick(int dtMs)
            {
                PlayerEffects.DisableAllMovements();
                if (!_every.Tick(dtMs)) return;

                STREAMING.REQUEST_ANIM_DICT(Dict);
                var clock = Stopwatch.StartNew();
                while (!STREAMING.HAS_ANIM_DICT_LOADED(Dict))
                {
                    if (clock.ElapsedMilliseconds > 3000) return;
                    Script.Wait(0);
                }

                int player = PlayerPed;
                var peds = NearbyPeds(50);
                peds.Add(player);
                foreach (int ped in peds)
                {
                    if (!Exists(ped) || !IsHuman(ped)) continue;
                    if (_dancing.TryGetValue(ped, out string playing) && ENTITY.IS_ENTITY_PLAYING_ANIM(ped, Dict, playing, 1)) continue;
                    if (ped != player && IsMission(ped) && Relationship(ped, player) < 4) continue;
                    Fx.RemoveFromTransport(ped);
                    TASK.CLEAR_PED_TASKS_IMMEDIATELY(ped, false, true);
                    string anim = Pick(Anims);
                    _dancing[ped] = anim;
                    TASK.TASK_PLAY_ANIM(ped, Dict, anim, 3f, -3f, -1, 1, 0f, false, 0, false, "", false);
                }
            }

            public override void Stop()
            {
                foreach (KeyValuePair<int, string> d in _dancing)
                {
                    if (Exists(d.Key) && ENTITY.IS_ENTITY_PLAYING_ANIM(d.Key, Dict, d.Value, 1))
                    {
                        TASK.STOP_ANIM_TASK(d.Key, Dict, d.Value, 0f);
                    }
                }
                _dancing.Clear();
                STREAMING.REMOVE_ANIM_DICT(Dict);
            }
        }

        #endregion
    }
}
