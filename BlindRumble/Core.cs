using Il2CppPhoton.Pun;
using Il2CppRUMBLE.Managers;
using Il2CppRUMBLE.MoveSystem;
using Il2CppRUMBLE.Players;
using Il2CppRUMBLE.Players.Scaling;
using Il2CppRUMBLE.Players.Subsystems;
using Il2CppTMPro;
using MelonLoader;
using RumbleModdingAPI.RMAPI;
using System.Collections;
using UIFramework;
using Unity.Hierarchy;
using UnityEngine;
using UnityEngine.InputSystem.HID;

[assembly: MelonInfo(typeof(BlindRumble2.Core), BlindRumble2.BuildInfo.ModName, BlindRumble2.BuildInfo.ModVersion, BlindRumble2.BuildInfo.Author)]
[assembly: MelonGame("Buckethead Entertainment", "RUMBLE")]
[assembly: MelonColor(255, 140, 40, 220)]
[assembly: MelonAuthorColor(255, 140, 40, 220)]
[assembly: VerifyLoaderVersion(0, 7, 2, true)]
[assembly: MelonAdditionalDependencies("UIFramework")]


namespace BlindRumble2
{
    public class Core : MelonMod
    {
        #region Variables

        public Core Instance;
        public bool IsShaderFound = false;
        public static string CurrentSceneName = "Loader";
        public static Material sonarMaterial;
        public static bool modEnabled = true;
        public static bool EIGym = true; // EI = EnableIn
        public static bool EIPark;
        public static bool EIMatch;
        public static Color MainSonar;
        public static Color SecondarySonar;
        public static MelonLogger.Instance loggerInstance;
        public static Transform blindRumbleAssets;
        public static Transform clones;
        public static Player enemyPlayer;
        public static GameObject dummyHealthbar;
        public static GameObject playerHealth;
        public static int dummyHealth = 20;
        public static bool iHaveWayTooManyVariables = false;

        #endregion

        #region MelonLoader stuff
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            CurrentSceneName = sceneName;

            iHaveWayTooManyVariables = false;

            if (CurrentSceneName == "Loader")
            {
                GetSonarShader();
            }
            else if (modEnabled && IsShaderFound)
            {
                MelonCoroutines.Start(SonarifyScene());

                blindRumbleAssets = new GameObject("BlindRumble").transform;
                clones = new GameObject("Clones").transform;
                clones.SetParent(blindRumbleAssets);
                MelonCoroutines.Start(BewareOfThePreloadedStructures());
            }
            else
            {
                return;
            }
        }

        public override void OnInitializeMelon()
        {
            UISetup.LoadPrefs();
            UI.Register((MelonBase)this, UISetup.category1, UISetup.category2);
        }

        public override void OnLateInitializeMelon()
        {
            Instance = this;

            Actions.onPlayerHealthChanged += (player, damage) =>
            {
                if (player != Calls.Players.GetLocalPlayer())
                {
                    dummyHealth -= damage;
                    dummyHealthbar.GetComponent<PlayerHealth>().SetHealth(enemyPlayer.Data.HealthPoints, (short)dummyHealth);
                }
            };

            Actions.onMatchStarted += () =>
            {
                MelonCoroutines.Start(CreateOppHealthbar());
            };
            //RumbleEvenDarkerMode();
        }

        public override void OnLateUpdate()
        {
            if (dummyHealthbar != null)
            {
                dummyHealthbar.transform.position = playerHealth.transform.position;
                dummyHealthbar.transform.localPosition = playerHealth.transform.localPosition;
                dummyHealthbar.transform.rotation = playerHealth.transform.rotation;

                dummyHealthbar.transform.GetChild(1).position = playerHealth.transform.GetChild(1).position;
                dummyHealthbar.transform.GetChild(1).localPosition = playerHealth.transform.GetChild(1).localPosition + new Vector3(0, 0.05f, 0);
                dummyHealthbar.transform.GetChild(1).rotation = playerHealth.transform.GetChild(1).rotation;
            }
        }

        public override void OnUpdate()
        {
            float timer = 0f;
            timer += Time.deltaTime;
            if (timer >= 0.5f)
            {
                timer = 0f;
                if (CurrentSceneName == "Gym" || CurrentSceneName == "Park")
                {
                    PopIn();
                }
            }
        }

        #endregion

        #region Sonar stuff
        public void GetSonarShader()
        {
            sonarMaterial = new Material(Shader.Find("Shader Graphs/Pose Ghost Shader"))
            {
                hideFlags = HideFlags.DontUnloadUnusedAsset,
                color = new Color(0, 0, 0, 0)
            };

            IsShaderFound = true;
        }

        //public static void RumbleEvenDarkerMode()
        //{
        //    MelonMod.FindMelon("Rumble Dark Mode", "ERROR").Unregister();
        //}

        public IEnumerator SonarifyScene()
        {
            // Makes everything have sonar shader
            if (CurrentSceneName == "Gym") // sonars gym
            {
                while (!IsShaderFound)
                {
                    yield return null;
                }
                foreach (Renderer rend in GameObjects.Gym.SCENE.GYM.GetGameObject().GetComponentsInChildren<Renderer>(true)) // remeber to change these to supercopia's thing
                {
                    rend.material = sonarMaterial;
                    rend.material.color = SecondarySonar;
                }
                GameObjects.Gym.SCENE.GYMVista.GetGameObject().active = false;
                GameObjects.Gym.SCENE.GYMWater.GetGameObject().GetComponent<MeshRenderer>().material = sonarMaterial;
                GameObjects.Gym.SCENE.GYMWater.GetGameObject().GetComponent<MeshRenderer>().material.color = new(0, 255, 239, 1);

                GameObjects.Gym.INTERACTABLES.Bag.GetGameObject().active = false;
                GameObjects.Gym.INTERACTABLES.BeltRack.GetGameObject().active = false;
                GameObjects.Gym.INTERACTABLES.CommunitySlab.GetGameObject().active = false;
                GameObjects.Gym.INTERACTABLES.DressingRoom.GetGameObject().active = false;
                GameObjects.Gym.INTERACTABLES.Fruit.GetGameObject().active = false;
                GameObjects.Gym.INTERACTABLES.Gearmarket.GetGameObject().active = false;
                GameObjects.Gym.INTERACTABLES.Gondola.GetGameObject().active = false;
                GameObjects.Gym.INTERACTABLES.Howard.GetGameObject().active = false;
                GameObjects.Gym.INTERACTABLES.Leaderboard.GetGameObject().active = false;
                GameObjects.Gym.INTERACTABLES.MatchConsole.GetGameObject().active = false;
                GameObjects.Gym.INTERACTABLES.MenuSlab.GetGameObject().active = false;
                GameObjects.Gym.INTERACTABLES.Notifications.GetGameObject().active = false;
                GameObjects.Gym.INTERACTABLES.Parkboard.GetGameObject().active = false;
                GameObjects.Gym.INTERACTABLES.PoseGhost.GetGameObject().active = false;
                GameObjects.Gym.INTERACTABLES.ProgressTracker.GetGameObject().active = false;
                GameObjects.Gym.INTERACTABLES.RegionSelector.GetGameObject().active = false;
                GameObjects.Gym.INTERACTABLES.RockCamStand.GetGameObject().active = false;
                GameObjects.Gym.INTERACTABLES.Shiftstones.GetGameObject().active = false;
                GameObjects.Gym.INTERACTABLES.Telephone20REDUXspecialedition.GetGameObject().active = false;
                GameObjects.Gym.INTERACTABLES.Toys.GetGameObject().active = false;
            }
            if (CurrentSceneName == "Park") // sonars park
            {
                while (!IsShaderFound)
                {
                    yield return null;
                }
                foreach (Renderer rend in GameObjects.Park.SCENE.PARK.GetGameObject().GetComponentsInChildren<Renderer>(true))
                {
                    rend.material = sonarMaterial;
                    rend.material.color = SecondarySonar;
                }


            }
            if (CurrentSceneName.Contains("Map"))
            {
                while (!IsShaderFound)
                {
                    yield return null;
                }
                if (CurrentSceneName == "Map0") // sonars ring
                {
                    foreach (Renderer rend in GameObjects.Map0.Scene.Map0.GetGameObject().GetComponentsInChildren<Renderer>(true))
                    {
                        rend.material = sonarMaterial;
                        rend.material.color = SecondarySonar;
                    }
                }
                else if (CurrentSceneName == "Map1") // sonars pit
                {
                    foreach (Renderer rend in GameObjects.Map1.Scene.MAP1.GetGameObject().GetComponentsInChildren<Renderer>(true))
                    {
                        rend.material = sonarMaterial;
                        rend.material.color = SecondarySonar;
                    }
                }

            }
        }

        public static void ConstantSnapshot()
        {
            if (modEnabled == false)
            {
                return;
            }

        }

        public static IEnumerator CreateSnapshot(bool isItStructure, bool poseTrigger, PlayerController player = null, Structure structure = null)
        {
            if (player == Calls.Players.GetLocalPlayerController())
            {
                yield break;
            }
            if (isItStructure == false)
            {
                //Creates a temporary image of where the player used to be when a sound happened nearby.
                GameObject cloneVisuals = GameObject.Instantiate(player.PlayerVisuals.gameObject, clones);
                cloneVisuals.GetComponent<Animator>().enabled = false;
                cloneVisuals.GetComponent<PlayerAnimator>().enabled = false;
                cloneVisuals.GetComponent<RigDefinition>().enabled = false;
                cloneVisuals.GetComponent<PlayerVisuals>().enabled = false;
                cloneVisuals.GetComponent<PlayerAudioPresence>().enabled = false;
                cloneVisuals.GetComponent<PlayerHandPresence>().enabled = false;
                cloneVisuals.GetComponent<PlayerScaling>().enabled = false;
                cloneVisuals.GetComponent<PhotonAnimatorView>().enabled = false;


                foreach (var renderer in cloneVisuals.GetComponentsInChildren<Renderer>())
                {
                    renderer.material = sonarMaterial;
                    renderer.material.color = MainSonar;
                }

                if (!poseTrigger)
                {
                    yield return new WaitForSeconds(1.45f);

                    MelonCoroutines.Start(ScaleClone(player.transform, Vector3.zero, 0.05f));
                }
                else
                {

                }

                GameObject.Destroy(cloneVisuals);
            }
            else if (isItStructure == true)
            {
                Transform Structure = GameObject.Instantiate(structure.gameObject, clones).transform;
                MeshRenderer structureVisuals = null;
                if (Structure.GetName() == "LargeRock" || Structure.GetName() == "SmallRock")
                {
                    structureVisuals = Structure.GetComponent<MeshRenderer>();
                }
                else if (Structure.GetName() != "LargeRock" && Structure.GetName() != "SmallRock")
                {
                    structureVisuals = Structure.GetChild(0).GetComponent<MeshRenderer>();
                }

                Structure.GetComponent<Structure>().enabled = false;
                Structure.GetComponent<Rigidbody>().isKinematic = true;
                Structure.GetComponent<Collider>().enabled = false;

                structureVisuals.material = sonarMaterial;
                structureVisuals.material.color = MainSonar;
            }
        }

        public static IEnumerator BewareOfThePreloadedStructures()
        {
            float length = 3.14159269535897932384626433f;
            float elapsed = 0f;
            while (elapsed < length)
            {
                elapsed += Time.deltaTime;
                iHaveWayTooManyVariables = false;
                yield return null;
            }
            iHaveWayTooManyVariables = true;
        }

        public static IEnumerator ScaleClone(Transform clone, Vector3 newScale, float length)
        {
            float elapsed = 0f;
            Vector3 originalScale = clone.localScale;

            while (elapsed < length)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / length;
                clone.localScale = Vector3.Lerp(originalScale, newScale, t);
                yield return null;
            }
        }

        public static IEnumerator CreateOppHealthbar()
        {
            if (CurrentSceneName == "Gym")
            {
                yield break;
            }
            while (Calls.Players.GetEnemyPlayers().Count <= 0)
            {
                yield return null;
            }
            if (Calls.Players.GetEnemyPlayers().Count == 1 && dummyHealthbar == null && CurrentSceneName != "Park")
            {
                for (int index = 0; index < PlayerManager.instance.AllPlayers.Count; index++)
                {
                    var player = PlayerManager.instance.AllPlayers[index];
                    if (player != null && player.Controller != null && player.Controller.gameObject != PlayerManager.instance.localPlayer.Controller.gameObject)
                    {
                        enemyPlayer = player;
                        break;
                    }
                }

                enemyPlayer.Controller.transform.GetChild(5).gameObject.SetActive(false);

                dummyHealthbar = GameObject.Instantiate(Calls.Players.GetLocalPlayerController().PlayerUI.localUIBar.gameObject, blindRumbleAssets);
                dummyHealthbar.name = "EnemyHealthbar";

                playerHealth = Calls.Players.GetLocalPlayerController().PlayerUI.localUIBar.gameObject;

            }
        }

        public static void PopIn()
        {
            Vector3 pos =  PlayerManager.instance.localPlayer.Controller.transform.position;

            Collider[] possibleInteractibles = Physics.OverlapSphere(pos, 6);
            List<GameObject> Interactibles = new();

            foreach (Collider col in possibleInteractibles)
            {
                GameObject current = col.gameObject;
                while (current != null)
                {
                    if (current == GameObjects.Gym.INTERACTABLES.GetGameObject() || current == GameObjects.Park.INTERACTABLES.GetGameObject())
                    {
                        Interactibles.Add(col.gameObject);
                        break;
                    }
                    current = current.transform.parent.gameObject;
                }
            }

            foreach (GameObject inter in Interactibles)
            {
                foreach (MeshRenderer mesh in inter.GetComponentsInChildren<MeshRenderer>())
                {
                    mesh.material = sonarMaterial;
                    mesh.material.color = new Color32(51, 204, 255, 255);
                }
                foreach (LineRenderer line in inter.GetComponentsInChildren<LineRenderer>())
                {
                    line.material = sonarMaterial;
                    line.material.color = new Color32(51, 204, 255, 255);
                }
                foreach (TextMeshPro tmp in inter.GetComponentsInChildren<TextMeshPro>())
                {
                    tmp.material = sonarMaterial;
                    tmp.material.color = new Color32(51, 204, 255, 255);
                }

                inter.active = true;
            }
        }

        #endregion
    }
}
