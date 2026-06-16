using Harmony;
using Il2CppExitGames.Client.Photon;
using Il2CppMS.Internal.Xml.XPath;
using Il2CppPhoton.Pun;
using Il2CppPhoton.Realtime;
using Il2CppPOpusCodec.Enums;
using Il2CppRootMotion.FinalIK;
using Il2CppRUMBLE.Audio;
using Il2CppRUMBLE.Interactions.InteractionBase;
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
using UnityEngine.Playables;


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
        public static MelonLogger.Instance loggerInstance;
        public static Transform blindRumbleAssets;
        public static Transform clones;
        public static Il2CppRUMBLE.Players.Player enemyPlayer;
        public static GameObject dummyHealthbar;
        public static GameObject playerHealth;
        public static int dummyHealth = 20;
        public static float timer;
        public static AudioCall seismicSlam;
        public static bool matchFound;
        public static bool reqFufilled = false;
        public static bool iHaveWayTooManyVariables = false;

        public static List<GameObject> processedVisuals = new();
        public static Dictionary<GameObject, Structure> ObjsWithExplode = new();
        public static Dictionary<GameObject, List<GameObject>> cloneGroups = new();

        public static bool modEnabled;
        public static bool EIGym;
        public static bool EIPark;
        public static bool EIRing;
        public static bool EIPit;
        public static Color MainSonar;
        public static Color SecondarySonar;

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
                seismicSlam = RumbleModdingAPI.RMAPI.AudioManager.CreateAudioCall("UserData/BlindRumble2/seismic_slam_buildup.wav", 1);
            }
            else if (modEnabled && !CurrentSceneName.Contains("Map"))
            {
                MelonCoroutines.Start(SonarifyScene());

                blindRumbleAssets = new GameObject("BlindRumble").transform;
                clones = new GameObject("Clones").transform;
                clones.SetParent(blindRumbleAssets);
                MelonCoroutines.Start(BewareOfThePreloadedStructures());
            }
            else if (modEnabled && CurrentSceneName is "Map0" or "Map1")
            {
                Criterion();
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
            foreach(var list in ObjsWithExplode)
            {
                Structure clone = list.Value;
                GameObject oruginal = list.Key;


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
            yield return new WaitForSeconds(1);

            if (CurrentSceneName == "Gym" && EIGym)
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
            if (CurrentSceneName == "Park" && EIPark)
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
            if (CurrentSceneName.Contains("Map") && reqFufilled)
            {
                if (CurrentSceneName == "Map0" && EIRing)
                {
                    foreach (Renderer rend in GameObjects.Map0.Scene.Map0.GetGameObject().GetComponentsInChildren<Renderer>(true))
                    {
                        rend.material = sonarMaterial;
                    }
                }
                else if (CurrentSceneName == "Map1" && EIPit)
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

        public static IEnumerator CreateSnapshot(PlayerController player = null, List<Structure> structures = null, bool addToCloneList = true)
        {
            if (sonarMaterial == null) yield break;

            if (player == Calls.Players.GetLocalPlayerController()) yield break;

            if (player != null)
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

                float timeToDestroy;

                if (addToCloneList)
                {
                    if (!cloneGroups.ContainsKey(player.gameObject))
                    {
                        cloneGroups[player.gameObject] = new List<GameObject>();
                    }

                    //if (collidedWithMap == true)
                    //{
                    //    cooldownTime = 0.1f;
                    //}

                    cloneGroups[player.gameObject].Add(clone.gameObject);
                    //cloneCooldowns[player.gameObject] = cooldownTime;

                    timeToDestroy = 1f;
                }
                else
                {
                    if (!cloneGroups.ContainsKey(player.gameObject))
                    {
                        cloneGroups[player.gameObject] = new();
                    }

                    timeToDestroy = 1f + (cloneGroups[player.gameObject].Count - 1) * 0.1f;
                }

                clone.GetChild(4).gameObject.active = false;
                clone.GetChild(5).gameObject.active = false;
                clone.GetChild(7).gameObject.active = false;

                MelonCoroutines.Start(DestroyCloneAfterDelay(clone.gameObject, timeToDestroy, player.gameObject, cloneVisuals.gameObject));
            }
            else if (structures != null)
            {
                foreach (Structure struc in structures)
                {
                    if (ObjsWithExplode.ContainsKey(struc.gameObject))
                    {

                    }
                    Transform Structure = GameObject.Instantiate(struc.gameObject, clones).transform;
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
                   
                    CheckForExplode(struc);

                    if (ObjsWithExplode.ContainsKey(struc.gameObject))
                    {
                        if (cloneGroups.ContainsKey(struc.gameObject))
                        {
                            continue;
                        }
                        else
                        {
                            cloneGroups[struc.gameObject] = new();
                            cloneGroups[struc.gameObject].Add(struc.gameObject);
                        }
                    }
                }
            }
        }

        public static void UpdExplodedObj(Structure original, Structure clone)
        {
            clone.gameObject.Destroy();

            MelonCoroutines.Start(CloneWithDelay(0.2f));
        }

        public static void CheckForExplode(Structure struc)
        {
            List<Structure> strucs = new();

            if (struc.transform.childCount >= 1)
            {
                // check explode and add to ObjsWithExplode if not inside
            }

            if (ObjsWithExplode[struc.gameObject])
            {
                Collider[] maybeStrucs = Physics.OverlapSphere(struc.transform.position, 2f);

 
                foreach (Collider col in maybeStrucs)
                {
                    if (col.TryGetComponent<Structure>(out var structure))
                    {
                        strucs.Add(structure);
                    } 
                }
            }

            MelonCoroutines.Start(CloneWithDelay(0.3f, null, strucs));
        }

        public static IEnumerator CloneWithDelay(float delay, PlayerController player = null, List<Structure> structures = null)
        {
            yield return new WaitForSeconds(delay);

            MelonCoroutines.Start(CreateSnapshot(player, structures));
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

        private static IEnumerator DestroyCloneAfterDelay(GameObject clone, float delay, GameObject original, GameObject cloneVisuals = null)
        {
            float elapsedTime = 0f;

            while (clone != null)
            {
                if (cloneVisuals == null)
                {
                    var holdVFX = clone.transform.Find("Hold_VFX");
                    var flickVFX = clone.transform.Find("Flick_VFX");

                    if (holdVFX != null || flickVFX != null)
                    {
                        yield return MelonCoroutines.Start(ScaleClone(clone.transform, UnityEngine.Vector3.zero, 0.05f));
                        break;
                    }
                }
                else
                {
                    yield return new WaitForSeconds(delay);

                    yield return MelonCoroutines.Start(ScaleClone(cloneVisuals.transform, UnityEngine.Vector3.zero, 0.05f));
                    break;
                }


                elapsedTime += Time.deltaTime;
                if (elapsedTime >= delay)
                {
                    yield return MelonCoroutines.Start(ScaleClone(clone.transform, UnityEngine.Vector3.zero, 0.05f));
                    break;
                }

                yield return null;
            }


            if (clone != null)
            {


                if (cloneGroups.ContainsKey(original))
                {
                    cloneGroups[original].Remove(clone);
                    if (cloneGroups[original].Count == 0)
                    {
                        cloneGroups.Remove(original);
                    }
                }

                if (clone.transform.Find("Visuals") && clone.transform.Find("Visuals").Find("Renderer"))
                {
                    GameObject.Destroy(clone.transform.GetChild(1).GetChild(0).gameObject);

                    GameObject.Destroy(clone.transform.GetChild(1).gameObject);
                }


                GameObject.Destroy(clone);
            }
        }

        public static IEnumerator ScaleClone(Transform clone, Vector3 newScale, float length, bool startFrom0 = false)
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

            GameObject matchConsole = GameObjects.Gym.INTERACTABLES.MatchConsole.GetGameObject();
            if (matchConsole != null) loggerInstance.Msg($"[MatchConsole] dist: {Vector3.Distance(matchConsole.transform.position, pos)}");

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

                foreach (MeshRenderer mesh in inter.GetComponentsInChildren<MeshRenderer>())
                {
                    if (Vector3.Distance(mesh.transform.position, pos) >= 4) continue;
                }

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

                if (inter.name.Contains("Fruit"))
                {
                    GameObject fruit = GameObjects.DDOL.GameInstance.PreInitializable.PoolManager.PoolFruitRUMBLEEnvironmentFruit.GetGameObject();
                    GameObject flong = GameObjects.DDOL.GameInstance.PreInitializable.PoolManager.PoolFruitLongRUMBLEEnvironmentFruit.GetGameObject();
                    GameObject ffat = GameObjects.DDOL.GameInstance.PreInitializable.PoolManager.PoolFruitFatRUMBLEEnvironmentFruit.GetGameObject();

                    foreach (GameObject pool in new[] { fruit, flong, ffat })
                    {
                        for (int i = 0; i < pool.transform.childCount; i++)
                        {
                            var child = pool.transform.GetChild(i);

                            if (Vector3.Distance(child.position, pos) <= 4)
                            {

                            }
                        }
                    }
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

        public static IEnumerator SeismicSlam()
        {
            Il2CppRUMBLE.Players.Player slammer = null;
            List<Structure> structures = new();

            for (int i = 0; i < PlayerManager.instance.AllPlayers.Count; i++)
            {
                if (Calls.Players.GetPlayerByActorNo(i).Controller.PlayerMovement.WasGrounded == false)
                {
                    slammer = Calls.Players.GetPlayerByActorNo(i);
                    break;
                }
            }
            while (!slammer.Controller.PlayerMovement.WasGrounded) yield return null;
            RumbleModdingAPI.RMAPI.AudioManager.PlaySound(seismicSlam, slammer.Controller.transform.localPosition);

            Collider[] colliders = Physics.OverlapSphere(slammer.Controller.transform.localPosition, 50f);
            foreach (Collider col in colliders)
            {
                if (col.GetComponent<Structure>() != null) structures.Add(col.GetComponent<Structure>());
            }
            loggerInstance.Msg(structures.Count);
            MelonCoroutines.Start(CreateSnapshot(null, structures);
        }

        public static void Criterion()
        {
            if (matchFound)
            {
                int stepIndex = GameObjects.Gym.INTERACTABLES.MatchConsole.MatchmakingSettings.InteractionSliderHorizontalGrip.Sliderhandle.GetGameObject().GetComponent<InteractionSlider>().stepCount;
                if (stepIndex == 3) reqFufilled = true;
                else reqFufilled = false;

                if (Calls.Mods.doesOpponentHaveMod(BuildInfo.ModName, BuildInfo.ModVersion, false)) reqFufilled = true;
                else reqFufilled = false;
            }
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
            if (CurrentSceneName is not "Map0" or "Map1") return;
            EIRing = data[0].ToString() == "True";
            EIPit = data[1].ToString() == "True";
        }
        #endregion
    }
}
