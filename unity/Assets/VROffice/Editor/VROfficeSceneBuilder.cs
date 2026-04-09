#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VROffice;

namespace VROffice.Editor
{
    /// <summary>
    /// Menu: VROffice > Build Phase 1 Scene
    /// Creeaza automat:
    ///   - Scena cu OVRCameraRig + podea + lumina
    ///   - GameObject Ana cu toate componentele si referintele
    ///   - Animator Controller cu starile Idle / Talking / Listening
    ///   - GameObject Pipeline (VoicePipeline + WhisperSTT + WebSocketClient)
    ///   - VROfficeManager conectat la toate
    /// </summary>
    public static class VROfficeSceneBuilder
    {
        private const string AnimatorPath = "Assets/VROffice/Animators/AnaAnimator.controller";
        private const string ScenePath    = "Assets/VROffice/Scenes/VROffice_Phase1.unity";

        // ------------------------------------------------------------------ //

        [MenuItem("VROffice/Build Phase 1 Scene")]
        public static void BuildScene()
        {
            // 1. Animator Controller
            var animCtrl = CreateAnaAnimatorController();

            // 2. Curata scena curenta
            foreach (var go in Object.FindObjectsOfType<GameObject>())
                Object.DestroyImmediate(go);

            // 3. Lumina + podea
            CreateEnvironment();

            // 4. OVRCameraRig
            var cameraRig = CreateCameraRig();

            // 5. WebSocketClient (persistent)
            var wsGO     = new GameObject("WebSocketClient");
            var wsClient = wsGO.AddComponent<WebSocketClient>();

            // 6. Avatar Ana
            var anaGO    = CreateAnaAvatar(animCtrl);
            var anaCtrl  = anaGO.GetComponent<AnaAvatarController>();

            // 7. Pipeline
            var pipelineGO = new GameObject("VoicePipeline");
            var pipeline   = pipelineGO.AddComponent<VoicePipeline>();
            pipelineGO.AddComponent<WhisperSTT>();

            pipeline.wsClient = wsClient;
            pipeline.ana      = anaCtrl;

            // 8. Manager
            var managerGO = new GameObject("VROfficeManager");
            var manager   = managerGO.AddComponent<VROfficeManager>();
            manager.wsClient      = wsClient;
            manager.voicePipeline = pipeline;
            manager.ana           = anaCtrl;

            // 9. Salveaza scena
            EnsureDirectory("Assets/VROffice/Scenes");
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene(),
                ScenePath);

            Debug.Log("[VROffice] Scena Phase 1 creata: " + ScenePath);
            EditorUtility.DisplayDialog("VROffice", "Scena Phase 1 creata cu succes!\n\n" +
                "Urmatorii pasi:\n" +
                "1. Asigneaza avatarul Anei in Ana > AnaAvatarController\n" +
                "2. Seteaza IP-ul in WebSocketClient.serverUrl\n" +
                "3. Seteaza WhisperSTT.apiKey\n" +
                "4. Configureaza OVRLipSync pe avatarul Anei", "OK");
        }

        // ------------------------------------------------------------------ //

        [MenuItem("VROffice/Create Ana Animator Controller")]
        public static AnimatorController CreateAnaAnimatorController()
        {
            EnsureDirectory("Assets/VROffice/Animators");

            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(AnimatorPath);

            // Parametri
            ctrl.AddParameter("IsTalking",   AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("IsListening", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Greet",       AnimatorControllerParameterType.Trigger);

            var rootSM = ctrl.layers[0].stateMachine;

            // Stari
            var idleState      = rootSM.AddState("Idle");
            var talkingState   = rootSM.AddState("Talking");
            var listeningState = rootSM.AddState("Listening");
            var greetState     = rootSM.AddState("Greet");

            rootSM.defaultState = idleState;

            // Tranzitii Idle -> Talking
            var t = idleState.AddTransition(talkingState);
            t.AddCondition(AnimatorConditionMode.If, 0, "IsTalking");
            t.hasExitTime = false; t.duration = 0.15f;

            // Talking -> Idle
            t = talkingState.AddTransition(idleState);
            t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsTalking");
            t.hasExitTime = false; t.duration = 0.2f;

            // Idle -> Listening
            t = idleState.AddTransition(listeningState);
            t.AddCondition(AnimatorConditionMode.If, 0, "IsListening");
            t.hasExitTime = false; t.duration = 0.1f;

            // Listening -> Idle
            t = listeningState.AddTransition(idleState);
            t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsListening");
            t.hasExitTime = false; t.duration = 0.2f;

            // Any -> Greet (trigger)
            var anyT = rootSM.AddAnyStateTransition(greetState);
            anyT.AddCondition(AnimatorConditionMode.If, 0, "Greet");
            anyT.hasExitTime = false; anyT.duration = 0.1f;

            // Greet -> Idle (exit time)
            t = greetState.AddTransition(idleState);
            t.hasExitTime = true; t.exitTime = 1f; t.duration = 0.2f;

            AssetDatabase.SaveAssets();
            Debug.Log("[VROffice] Animator Controller creat: " + AnimatorPath);
            return ctrl;
        }

        // ------------------------------------------------------------------ //

        private static void CreateEnvironment()
        {
            // Lumina directionala
            var lightGO = new GameObject("Directional Light");
            var light   = lightGO.AddComponent<Light>();
            light.type      = LightType.Directional;
            light.intensity = 1.2f;
            lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Podea
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(2f, 1f, 2f);

            // Masa (placeholder simplu)
            var desk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            desk.name = "Desk";
            desk.transform.position   = new Vector3(0f, 0.4f, 1.5f);
            desk.transform.localScale = new Vector3(1.2f, 0.05f, 0.6f);
        }

        private static GameObject CreateCameraRig()
        {
            // Creeaza un placeholder OVRCameraRig (sau gaseste prefab-ul Meta XR daca e instalat)
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Packages/com.meta.xr.sdk.core/Prefabs/OVRCameraRig.prefab");

            GameObject rig;
            if (prefab != null)
            {
                rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                rig.name = "OVRCameraRig";
            }
            else
            {
                rig = new GameObject("OVRCameraRig [placeholder — import Meta XR SDK]");
                var cam = new GameObject("CenterEyeAnchor");
                cam.transform.SetParent(rig.transform);
                cam.AddComponent<Camera>();
                Debug.LogWarning("[VROffice] Meta XR SDK nu e instalat. OVRCameraRig e un placeholder.");
            }

            rig.transform.position = Vector3.zero;
            return rig;
        }

        private static GameObject CreateAnaAvatar(AnimatorController animCtrl)
        {
            var anaGO = new GameObject("Ana");
            anaGO.transform.position = new Vector3(0f, 0f, 1.8f);
            anaGO.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // fata spre utilizator

            // Animator
            var anim = anaGO.AddComponent<Animator>();
            anim.runtimeAnimatorController = animCtrl;

            // AudioSource pentru voce
            var audioSrc = anaGO.AddComponent<AudioSource>();
            audioSrc.spatialBlend    = 1f;
            audioSrc.rolloffMode     = AudioRolloffMode.Linear;
            audioSrc.maxDistance     = 10f;
            audioSrc.playOnAwake     = false;

            // AnaAvatarController
            var anaCtrl = anaGO.AddComponent<AnaAvatarController>();
            anaCtrl.animator = anim;
            // lipSyncContext si morphTarget se asigneaza manual dupa import Oculus LipSync

            // Sub-obiect placeholder corp (vizibil in scena pana la import avatar real)
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body_Placeholder";
            body.transform.SetParent(anaGO.transform);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            body.transform.localScale    = new Vector3(0.4f, 0.9f, 0.4f);

            return anaGO;
        }

        private static void EnsureDirectory(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parts  = path.Split('/');
                var parent = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    var full = parent + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(full))
                        AssetDatabase.CreateFolder(parent, parts[i]);
                    parent = full;
                }
            }
        }
    }
}
#endif
