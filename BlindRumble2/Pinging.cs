using HarmonyLib;
using Il2CppRUMBLE.Managers;
using Il2CppRUMBLE.MoveSystem;
using Il2CppRUMBLE.Players;
using Il2CppRUMBLE.Players.Subsystems;
using Il2CppRUMBLE.Poses;
using MelonLoader;
using RumbleModdingAPI.RMAPI;
using UnityEngine;
using static BlindRumble2.Core;
using static RumbleModdingAPI.RMAPI.GameObjects.Gym.LOGIC;

namespace BlindRumble2
{
    internal class PingingPatches
    {
        #region Pose-related stuff

        [HarmonyPatch(typeof(Structure), "OnFetchFromPool")]
        internal class StructurePinging
        {
            [HarmonyPostfix]
            public static void PostFix(Structure __instance)
            {
                if (iHaveWayTooManyVariables == false)
                {
                    return;
                }
                loggerInstance.Msg("structure");
                List<Structure> structureList = new();
                structureList.Add(__instance);
                MelonCoroutines.Start(CreateSnapshot(null, structureList));
            }
        }

        #endregion

        #region other kinds of pinging

        [HarmonyPatch(typeof(PlayerBoxInteractionSystem), "OnPlayerBoxInteraction", new Type[] { typeof(PlayerBoxInteractionTrigger), typeof(PlayerBoxInteractionTrigger) })]
        internal class FistPinging
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerBoxInteractionTrigger first, PlayerBoxInteractionTrigger second)
            {
                var localPlayer = Calls.Players.GetLocalPlayer();
                var handOne = first.parentSystem.parentController.assignedPlayer;
                var handTwo = second.parentSystem.parentController.assignedPlayer;

                if (handOne == localPlayer && handTwo == localPlayer)
                {

                }
                else if (handOne != localPlayer || handTwo != localPlayer)
                {
                    MelonCoroutines.Start(CreateSnapshot(localPlayer.Controller));
                }
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(PlayerPoseSystem), nameof(PlayerPoseSystem.OnPoseSetCompleted), new Type[] { typeof(PoseSet) })]
        private static class PosePatch
        {
            private static void Postfix(PoseSet set)
            {
                if (set.name == "PoseSetRockjump")
                {
                    SeismicSlam();
                }
            }
        }
        #endregion
    }
}
