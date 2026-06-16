using HarmonyLib;
using Il2CppRUMBLE.Managers;
using Il2CppRUMBLE.MoveSystem;
using Il2CppRUMBLE.Players.Subsystems;
using Il2CppRUMBLE.Poses;
using MelonLoader;
using RumbleModdingAPI.RMAPI;
using static BlindRumble2.Core;

namespace BlindRumble2
{
    internal class Patches
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

        #region other kinds of patches

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

        [HarmonyPatch(typeof(PlayerPoseSystem), nameof(PlayerPoseSystem.OnPoseSetCompleted), typeof(PoseSet))]
        internal class PosePatch
        {
            private static void Postfix(PoseSet set)
            {
                if (set.name == "PoseSetRockjump")
                {
                    SeismicSlam();
                }
            }
        }

        [HarmonyPatch(typeof(Il2CppRUMBLE.Environment.Matchmaking.MatchmakeConsole), "MatchmakeStatusUpdated", typeof(MatchmakingHandler.MatchmakeStatus))]
        internal class MatchMade
        {
            private static void PreFix(MatchmakingHandler.MatchmakeStatus status)
            {
                if (status == MatchmakingHandler.MatchmakeStatus.Success)
                {
                    loggerInstance.Msg("---- Match found ----");
                    matchFound = true;
                }
            }
        }
        #endregion
    }
}
