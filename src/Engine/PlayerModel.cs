// Player model changes (cow, rat, bird, body swap ...): ChaosModRDR's SavePlayerAttributes / SetPlayerModel /
// ResetPlayerSkin on top of the runtime's story-safe Player.ChangeModelPersistent / RestoreStoryModel.
using System.Collections.Generic;
using RDR2;
using RDR2.Native;

namespace StreamEmber.ChaosMod
{
    internal static class PlayerModel
    {
        private static bool s_saved;
        private static readonly int[] s_ranks = new int[3];
        private static int s_deadEyeLevel;
        private static uint[] s_clothes;

        /// <summary>True while the player has a model set by an effect.</summary>
        public static bool IsChanged => !Game.Player.IsStoryCharacterModel;

        /// <summary>Changes the player model; saves Arthur/John's attributes and clothes the first time.</summary>
        public static bool Change(uint model)
        {
            Player player = Game.Player;
            int ped = Fx.PlayerPed;
            if (player.IsStoryCharacterModel)
            {
                Save(player, ped);
            }

            // Off the vehicle / mount first (the new ped would be left inside a seat it cannot use)
            if (Fx.InVehicle(ped))
            {
                var p = Fx.Pos(ped);
                p.Z += 2f;
                Fx.SetPos(ped, p);
            }
            else if (Fx.OnMount(ped))
            {
                ENTITY.SET_ENTITY_AS_MISSION_ENTITY(Fx.MountOf(ped), true, true);
                PED._REMOVE_PED_FROM_MOUNT(ped, true, false);
            }

            bool changed = player.ChangeModelPersistent(new Model(model));
            if (changed)
            {
                // Runtime 1.1.0 does not outfit the new MetaPed yet (invisible otherwise); effects may override it
                int newPed = Fx.PlayerPed;
                PED._SET_RANDOM_OUTFIT_VARIATION(newPed, true);
                PED._UPDATE_PED_VARIATION(newPed, false, true, true, true, false);
            }
            if (!changed)
            {
                ChaosLog.Warn("Player model change failed: 0x" + model.ToString("X8"));
            }
            else if (!Player.LastModelChangeSyncedGlobals)
            {
                ChaosLog.Warn("Player model changed without the story globals (game build differs); the game may switch it back.");
            }
            return changed;
        }

        /// <summary>Back to Arthur/John with the saved attributes and clothes.</summary>
        public static void Restore()
        {
            int ped = Fx.PlayerPed;
            if (Fx.OnMount(ped))
            {
                Fx.Dismount(ped);
            }
            Player player = Game.Player;
            if (player.IsStoryCharacterModel)
            {
                s_saved = false;
                return;
            }
            if (Fx.NoWait && s_storyModel != 0 && !STREAMING.HAS_MODEL_LOADED(s_storyModel))
            {
                // Shutting down: no waiting allowed and the model is not streamed (kept requested since the change)
                ChaosLog.Warn("Story model not loaded during shutdown; the player keeps the effect model.");
                return;
            }
            if (!player.RestoreStoryModel())
            {
                ChaosLog.Warn("Restoring the story model failed.");
                return;
            }
            RestoreSaved(player, Fx.PlayerPed);
        }

        private static uint s_storyModel;

        private static void Save(Player player, int ped)
        {
            // Keep Arthur/John's model requested so it can be restored without waiting (also from Aborted)
            s_storyModel = ENTITY.GET_ENTITY_MODEL(ped);
            STREAMING.REQUEST_MODEL(s_storyModel, false);
            for (int i = 0; i < 3; i++)
            {
                s_ranks[i] = ATTRIBUTE.GET_ATTRIBUTE_BASE_RANK(ped, i);
            }
            s_deadEyeLevel = player.DeadEyeLevel;
            s_clothes = new Ped(ped).GetShopItemComponents();
            s_saved = true;
        }

        private static void RestoreSaved(Player player, int ped)
        {
            PED._EQUIP_META_PED_OUTFIT_PRESET(ped, 0, false);
            PED._UPDATE_PED_VARIATION(ped, false, true, true, true, false);
            if (s_storyModel != 0)
            {
                STREAMING.SET_MODEL_AS_NO_LONGER_NEEDED(s_storyModel);
                s_storyModel = 0;
            }
            if (!s_saved) return;
            s_saved = false;
            for (int i = 0; i < 3; i++)
            {
                ATTRIBUTE.SET_ATTRIBUTE_BASE_RANK(ped, i, s_ranks[i]);
                ATTRIBUTE._SET_ATTRIBUTE_CORE_VALUE(ped, i, 100);
            }
            ENTITY.SET_ENTITY_HEALTH(ped, ENTITY.GET_ENTITY_MAX_HEALTH(ped, false), 0);
            player.RestoreStamina(1f);
            PLAYER._SPECIAL_ABILITY_START_RESTORE(player.Handle, -1, true);
            player.SetSpecialAbilitiesEnabled(true);
            player.DeadEyeEnabled = true;
            player.EagleEyeEnabled = true;
            player.DeadEyeLevel = s_deadEyeLevel;

            // Clothes need the new ped's streaming to finish
            for (int i = 0; i < 50 && !Fx.NoWait && !PED.IS_PED_READY_TO_RENDER(ped); i++)
            {
                Script.Wait(100);
            }
            if (s_clothes != null && s_clothes.Length > 0)
            {
                new Ped(ped).ApplyShopItemComponents(new List<uint>(s_clothes));
            }
            s_clothes = null;
        }
    }
}
