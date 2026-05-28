using Il2CppExitGames.Client.Photon;
using Il2CppPhoton.Pun;
using Il2CppPhoton.Realtime;
using Il2CppRootMotion.FinalIK;
using Il2CppRUMBLE.Managers;
using Il2CppRUMBLE.MoveSystem;
using Il2CppRUMBLE.Players;
using Il2CppRUMBLE.Players.Scaling;
using Il2CppRUMBLE.Players.Subsystems;
using Il2CppTMPro;
using MelonLoader;
using RumbleModdingAPI.RMAPI;
using System.Collections;
using System.Runtime.CompilerServices;
using UIFramework;
using Unity.Hierarchy;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using UnityEngine.InputSystem.XR;

#region MelonInfo
[assembly: MelonInfo(typeof(BlindRumble2.Core), BlindRumble2.BuildInfo.ModName, BlindRumble2.BuildInfo.ModVersion, BlindRumble2.BuildInfo.Author)]
[assembly: MelonGame("Buckethead Entertainment", "RUMBLE")]
[assembly: MelonColor(255, 140, 40, 220)]
[assembly: MelonAuthorColor(255, 140, 40, 220)]
[assembly: VerifyLoaderVersion(0, 7, 2, true)]
[assembly: MelonAdditionalDependencies("UIFramework")]
#endregion
// IMPORTANT add option to reload every pulse
namespace BlindRumble2
{
    public class Core : Utilities.RumbleMod
    {
        #region Variables

        public static Core instance;
        public Core() => instance = this;
        public static bool IsShaderFound = false;
        public static string CurrentSceneName = "Loader";
        public static Material sonarMaterial;
        public static bool modEnabled = true;
        public static bool EIGym = true; // EI = EnableIn
        public static bool EIPark;
        public static bool EIRing;
        public static bool EIPit;
        public static Color MainSonar;
        public static Color SecondarySonar;
        public static MelonLogger.Instance loggerInstance;
        public static Transform blindRumbleAssets;
        public static Transform clones;
        public static Il2CppRUMBLE.Players.Player enemyPlayer;
        public static GameObject dummyHealthbar;
        public static GameObject playerHealth;
        public static int dummyHealth = 20;
        public static float timer;
        public static List<GameObject> processedVisuals = new();
        public static bool iHaveWayTooManyVariables = false;

        #endregion

        #region MelonLoader stuff
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            CurrentSceneName = sceneName;

            iHaveWayTooManyVariables = false;
            processedVisuals.Clear();

            if (CurrentSceneName == "Loader")
            {
                GetSonarShader();
            }
            else if (modEnabled && !CurrentSceneName.Contains("Map"))
            {
                MelonCoroutines.Start(SonarifyScene());

                blindRumbleAssets = new GameObject("BlindRumble").transform;
                clones = new GameObject("Clones").transform;
                clones.SetParent(blindRumbleAssets);
                MelonCoroutines.Start(BewareOfThePreloadedStructures());
            }
            else if (modEnabled && CurrentSceneName.Contains("Map"))
            {
                SendData();
                MelonCoroutines.Start(SonarifyScene());
            }
        }

        public override void OnInitializeMelon()
        {
            loggerInstance = LoggerInstance;
            UISetup.LoadPrefs();
            UISetup.SetPrefs();
            UI.RegisterMelon((MelonBase)this, UISetup.category1, UISetup.category2).OnModSaved += UISetup.SetPrefs;
        }

        public override void OnLateInitializeMelon()
        {
            RegisterEvents();

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
            timer += Time.deltaTime;
            if (timer >= 0.5f)
            {
                timer = 0f;
                if (CurrentSceneName is "Gym" or "Park") PopIn();
            }
        }

        #endregion

        #region Sonar stuff
        public static void GetSonarShader()
        {
            Shader shader = AssetBundles.LoadAssetFromStream<Shader>(instance, "BlindRumble2.Shader.poseghostshader", "Ghost");
            sonarMaterial = new(shader)
            {
                hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset
            };

            for (int i = 0; i < shader.GetPropertyCount(); i++)
            {
                loggerInstance.Msg($"[Shader] {shader.GetPropertyType(i)} : {shader.GetPropertyName(i)}");
            }

            if (sonarMaterial == null) return;
            else IsShaderFound = true;
            loggerInstance.Msg(SecondarySonar);
        }

        public static IEnumerator SonarifyScene()
        {
            if (sonarMaterial == null)
            {
                loggerInstance.Error("[SonarifyScene] sonarMaterial is null, aborting.");
                yield break;
            }

            if (CurrentSceneName == "Gym")
            {
                foreach (Renderer rend in GameObjects.Gym.SCENE.GYM.GetGameObject().GetComponentsInChildren<Renderer>(true))
                {
                    rend.material = sonarMaterial;
                    SetShaderColor(rend.material, SecondarySonar);
                }
                GameObjects.Gym.SCENE.GYMVista.GetGameObject().active = false;
                GameObjects.Gym.SCENE.GYMWater.GetGameObject().GetComponent<MeshRenderer>().material = sonarMaterial;
                SetShaderColor(GameObjects.Gym.SCENE.GYMWater.GetGameObject().GetComponent<MeshRenderer>().material, new Color32(0, 255, 239, 1));
                GameObjects.Gym.SCENE.GYMMoss.GetGameObject().GetComponent<MeshRenderer>().material = sonarMaterial;
                SetShaderColor(GameObjects.Gym.SCENE.GYMMoss.GetGameObject().GetComponent<MeshRenderer>().material, new Color32(138, 154, 91, 255));

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

                GameObjects.DDOL.GameInstance.PreInitializable.PoolManager.PoolFruitFatRUMBLEEnvironmentFruit.GetGameObject().active = false;
                GameObjects.DDOL.GameInstance.PreInitializable.PoolManager.PoolFruitLongRUMBLEEnvironmentFruit.GetGameObject().active = false;
                GameObjects.DDOL.GameInstance.PreInitializable.PoolManager.PoolFruitRUMBLEEnvironmentFruit.GetGameObject().active = false;
            }
            if (CurrentSceneName == "Park")
            {
                foreach (Renderer rend in GameObjects.Park.SCENE.PARK.GetGameObject().GetComponentsInChildren<Renderer>(true))
                {
                    rend.material = sonarMaterial;
                    SetShaderColor(rend.material, SecondarySonar);
                }

                GameObjects.Park.INTERACTABLES.Fruit.GetGameObject().active = false;
                GameObjects.Park.INTERACTABLES.Gondola.GetGameObject().active = false;
                GameObjects.Park.INTERACTABLES.Notifications.GetGameObject().active = false;
                GameObjects.Park.INTERACTABLES.ParkboardPark.GetGameObject().active = false;
                GameObjects.Park.INTERACTABLES.Shiftstones.GetGameObject().active = false;
                GameObjects.Park.INTERACTABLES.Telephone20REDUXspecialedition.GetGameObject().active = false;
                GameObjects.Park.INTERACTABLES.Toys.GetGameObject().active = false;

                GameObjects.DDOL.GameInstance.PreInitializable.PoolManager.PoolFruitFatRUMBLEEnvironmentFruit.GetGameObject().active = false;
                GameObjects.DDOL.GameInstance.PreInitializable.PoolManager.PoolFruitLongRUMBLEEnvironmentFruit.GetGameObject().active = false;
                GameObjects.DDOL.GameInstance.PreInitializable.PoolManager.PoolFruitRUMBLEEnvironmentFruit.GetGameObject().active = false;
            }
            if (CurrentSceneName.Contains("Map"))
            {
                if (CurrentSceneName == "Map0")
                {
                    foreach (Renderer rend in GameObjects.Map0.Scene.Map0.GetGameObject().GetComponentsInChildren<Renderer>(true))
                    {
                        rend.material = sonarMaterial;
                    }
                }
                else if (CurrentSceneName == "Map1")
                {
                    foreach (Renderer rend in GameObjects.Map1.Scene.MAP1.GetGameObject().GetComponentsInChildren<Renderer>(true))
                    {
                        rend.material = sonarMaterial;
                    }
                }
            }
        }

        public static void SetShaderColor(Material material, Color color)
        {
            if (material == null) return;

            Shader shader = material.shader;
            material.color = color;

            for (int i = 0; i < shader.GetPropertyCount(); i++)
            {
                if (shader.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Color)
                {
                    material.SetColor(i, color);
                }
            }
        }

        public static IEnumerator CreateSnapshot(bool isItStructure, PlayerController player = null, Structure structure = null, bool addToCloneList = true)
        {
            if (sonarMaterial == null) yield break;

            if (player == Calls.Players.GetLocalPlayerController()) yield break;

            if (isItStructure == false)
            {
                Transform clone = GameObject.Instantiate(player.gameObject, clones).transform;
                Transform cloneVisuals = clone.GetChild(1).transform;

                Transform VR = clone.GetChild(2).transform;
                Transform leftController = VR.GetChild(1).transform;
                Transform rightController = VR.GetChild(2).transform;
                Transform pillBody = clone.GetChild(3).GetChild(0).transform;
                Transform headset = VR.GetChild(0).GetChild(0).transform;

                Transform VROriginal = player.transform.GetChild(2).transform;
                Transform leftControllerOriginal = VROriginal.GetChild(1).transform;
                Transform rightControllerOriginal = VROriginal.GetChild(2).transform;
                Transform pillBodyOriginal = player.transform.GetChild(3).GetChild(0).transform;
                Transform headsetOriginal = VROriginal.GetChild(0).GetChild(0).transform;

                Vector3 oldSize;

                clone.GetComponent<PlayerController>().assignedPlayer = null;
                leftController.gameObject.GetComponent<TrackedPoseDriver>().enabled = false;
                rightController.gameObject.GetComponent<TrackedPoseDriver>().enabled = false;

                cloneVisuals.GetComponent<VRIK>().enabled = true;

                pillBody.position = pillBodyOriginal.position;
                headset.position = headsetOriginal.position;
                leftController.position = leftControllerOriginal.position;
                rightController.position = rightControllerOriginal.position;

                pillBody.rotation = pillBodyOriginal.rotation;
                headset.rotation = headsetOriginal.rotation;
                leftController.rotation = leftControllerOriginal.rotation;
                rightController.rotation = rightControllerOriginal.rotation;

                oldSize = cloneVisuals.localScale;

                foreach (Behaviour component in clone.GetComponents<Behaviour>())
                {
                    component.enabled = false;
                }

                foreach (Behaviour component in cloneVisuals.GetComponents<Behaviour>())
                {
                    component.enabled = false;
                }

                Rigidbody[] rigidbodies = clone.GetComponentsInChildren<Rigidbody>();
                foreach (Rigidbody rb in rigidbodies)
                {
                    rb.isKinematic = true;
                }

                Renderer[] cloneRenderers = clone.GetComponentsInChildren<Renderer>();
                foreach (var rend in cloneRenderers)
                {
                    rend.enabled = true;
                }

                Collider cloneCollider = clone.GetComponent<Collider>();
                MeshCollider meshCloneCollider = clone.GetComponent<MeshCollider>();

                Collider[] cloneColliders = clone.GetComponentsInChildren<Collider>();
                MeshCollider[] meshCloneColliders = clone.GetComponentsInChildren<MeshCollider>();

                foreach (var rend in cloneColliders)
                {
                    rend.enabled = false;
                }

                foreach (var rend in meshCloneColliders)
                {
                    rend.enabled = false;
                }

                if (cloneCollider != null)
                {
                    cloneCollider.enabled = false;
                }
                if (meshCloneCollider != null)
                {
                    meshCloneCollider.enabled = false;
                }

                if (addToCloneList)
                {

                }
                else
                {

                }
                
                yield return new WaitForSeconds(1.45f);
                MelonCoroutines.Start(ScaleClone(player.transform, Vector3.zero, 0.05f, false));
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

                structureVisuals.material = new Material(sonarMaterial);
                SetShaderColor(structureVisuals.material, MainSonar);
            }
        }

        public static IEnumerator BewareOfThePreloadedStructures()
        {
            float length = (float)Math.PI;
            float elapsed = 0f;
            while (elapsed < length)
            {
                elapsed += Time.deltaTime;
                iHaveWayTooManyVariables = false;
                yield return null;
            }
            iHaveWayTooManyVariables = true;
        }

        public static IEnumerator ScaleClone(Transform clone, Vector3 newScale, float length, bool startFrom0)
        {
            float elapsed = 0f;
            Vector3 originalScale = clone.localScale;
            if (startFrom0) originalScale = Vector3.zero;

            while (elapsed < length)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / length;
                clone.localScale = Vector3.Lerp(originalScale, newScale, t);
                yield return null;
            }

            if (clone.localScale == Vector3.zero) clone.gameObject.active = false;
        }

        public static IEnumerator CreateOppHealthbar()
        {
            if (CurrentSceneName == "Gym") yield break;

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
            PopOut();

            Vector3 pos = PlayerManager.instance.localPlayer.Controller.transform.position;

            List<GameObject> Interactibles = new();

            foreach (GameObject root in new[] { GameObjects.Gym.INTERACTABLES.GetGameObject(), GameObjects.Park.INTERACTABLES.GetGameObject() })
            {
                if (root == null) continue;
                for (int i = 0; i < root.transform.childCount; i++)
                {
                    Transform child = root.transform.GetChild(i);
                    if (Vector3.Distance(child.position, pos) <= 4f)
                    {
                        Interactibles.Add(child.gameObject);
                    }
                }
            }

            if (Interactibles.Count == 0) return;
            loggerInstance.Msg($"[PopIn] {Interactibles.Count} objects found");

            foreach (GameObject inter in Interactibles)
            {
                if (processedVisuals.Contains(inter)) continue;

                foreach (MeshRenderer mesh in inter.GetComponentsInChildren<MeshRenderer>(true))
                {
                    mesh.material = new Material(sonarMaterial);
                    SetShaderColor(mesh.material, new Color32(51, 204, 255, 255));
                }
                foreach (SkinnedMeshRenderer skin in inter.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    skin.material = new Material(sonarMaterial);
                    SetShaderColor(skin.material, new Color32(51, 204, 255, 255));
                }
                foreach (LineRenderer line in inter.GetComponentsInChildren<LineRenderer>(true))
                {
                    line.material = new Material(sonarMaterial);
                    SetShaderColor(line.material, new Color32(51, 204, 255, 255));
                }
                foreach (TextMeshPro tmp in inter.GetComponentsInChildren<TextMeshPro>(true))
                {
                    tmp.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                }
                foreach (TextMeshProUGUI tmp in inter.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    tmp.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                }
                processedVisuals.Add(inter);
                inter.active = true;
                MelonCoroutines.Start(ScaleClone(inter.transform, inter.transform.localScale, 0.05f, true));
            }
        }

        public static void PopOut()
        {
            Vector3 pos = PlayerManager.instance.localPlayer.Controller.transform.position;

            processedVisuals.RemoveAll(inter =>
            {
                if (inter == null) return true;
                if (Vector3.Distance(inter.transform.position, pos) > 4f)
                {
                    MelonCoroutines.Start(ScaleClone(inter.transform, Vector3.zero, 0.05f, false));
                    return true;
                }
                return false;
            });
        }

        #endregion

        #region Networking stuff
        public void SendData()
        { 
            List<Il2CppSystem.Object> data = new();
            
            if (EIRing) data.Add(true); 
            else data.Add(false);

            if (EIPit) data.Add(true);
            else data.Add(false);

            RaiseEventOptions options = new()
            {
                Receivers = ReceiverGroup.Others,
                CachingOption = EventCaching.AddToRoomCache   
            };

            RaiseEvent(data, options, SendOptions.SendReliable);
        }

        public override void OnEvent(List<Il2CppSystem.Object> data)
        {
            if (CurrentSceneName == "Park") return;
            EIRing = data[0].ToString() == "True";
            EIPit = int.Parse(data[1].ToString()) == 1;
        }
        #endregion
    }
}
