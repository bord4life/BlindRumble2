using HarmonyLib;
using Il2CppRUMBLE.Managers;
using Il2CppRUMBLE.MoveSystem;
using Il2CppRUMBLE.Players;
using Il2CppRUMBLE.Players.Subsystems;
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
                MelonCoroutines.Start(CreateSnapshot(true, false, null, __instance));
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
					MelonCoroutines.Start(CreateSnapshot(false, false, localPlayer.Controller));
				}
			}
		}

        #endregion
    }
}
