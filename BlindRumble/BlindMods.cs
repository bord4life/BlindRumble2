using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static BlindRumble2.Core;


namespace BlindRumble2
{
    internal class BlindMods
    {
        public static void ClientHostFix()
        {
            if (GameObject.Find("MatchInfoMod") && CurrentSceneName.Contains("Map"))
            {
                 
            }
        }

    }
}
