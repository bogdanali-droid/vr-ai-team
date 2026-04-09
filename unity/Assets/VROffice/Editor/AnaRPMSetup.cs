#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VROffice.Editor
{
    /// <summary>
    /// Menu: VROffice > Setup Ana RPM Avatar
    /// Configureaza LipSync pentru avatarul Ready Player Me al Anei.
    /// Necesita Meta XR SDK (OVR_LIPSYNC) pentru setup complet.
    /// </summary>
    public static class AnaRPMSetup
    {
        [MenuItem("VROffice/Setup Ana RPM Avatar")]
        public static void SetupAnaAvatar()
        {
            var selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("VROffice",
                    "Selecteaza GameObject-ul 'Ana' in Hierarchy.", "OK");
                return;
            }

            // AudioSource pe root
            var audioSource = EnsureComponent<AudioSource>(selected);
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode  = AudioRolloffMode.Linear;
            audioSource.maxDistance  = 10f;
            audioSource.playOnAwake  = false;

            // AnaAvatarController
            var anaCtrl = EnsureComponent<AnaAvatarController>(selected);
            anaCtrl.animator = selected.GetComponent<Animator>();

#if OVR_LIPSYNC
            // Setup LipSync complet cu OVR SDK
            var smr = FindVisemeMesh(selected);
            if (smr == null)
            {
                EditorUtility.DisplayDialog("VROffice",
                    "Nu am gasit blendshape-uri 'viseme_'.\n\n" +
                    "La export RPM selecteaza:\nMorph Targets: ARKit + Oculus Visemes", "OK");
                return;
            }

            var lipSyncCtx = EnsureComponent<OVRLipSyncContext>(selected);
            lipSyncCtx.audioSource        = audioSource;
            lipSyncCtx.enableAcceleration = true;

            var morphTarget = EnsureComponent<OVRLipSyncContextMorphTarget>(selected);
            morphTarget.skinnedMeshRenderer = smr;

            var visemeMap = new (string name, int idx)[]
            {
                ("viseme_sil",0),("viseme_PP",1),("viseme_FF",2),("viseme_TH",3),
                ("viseme_DD",4),("viseme_kk",5),("viseme_CH",6),("viseme_SS",7),
                ("viseme_nn",8),("viseme_RR",9),("viseme_aa",10),("viseme_E",11),
                ("viseme_I",12),("viseme_O",13),("viseme_U",14),
            };

            int mapped = 0;
            foreach (var (name, idx) in visemeMap)
            {
                int bsIdx = FindBlendshapeIndex(smr, name);
                if (bsIdx >= 0) { SetVisemeBlendshape(morphTarget, idx, bsIdx); mapped++; }
            }

            anaCtrl.lipSyncContext = lipSyncCtx;
            anaCtrl.morphTarget    = morphTarget;

            EditorUtility.SetDirty(selected);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("VROffice — Ana Setup",
                $"Setup complet!\nMesh: {smr.name}\nViseme mapate: {mapped}/15\n\n" +
                "Urmatorii pasi:\n1. Asigneaza Animator Controller\n2. Seteaza ANA_VOICE_ID in .env",
                "OK");
#else
            EditorUtility.SetDirty(selected);
            EditorUtility.DisplayDialog("VROffice — Ana Setup",
                "AudioSource si AnaAvatarController adaugate.\n\n" +
                "LipSync complet necesita Meta XR SDK.\n" +
                "Instaleaza pachetul si adauga OVR_LIPSYNC in:\n" +
                "Edit → Project Settings → Player → Scripting Define Symbols",
                "OK");
#endif
        }

        [MenuItem("VROffice/Setup Ana RPM Avatar", true)]
        private static bool ValidateSetup() => Selection.activeGameObject != null;

#if OVR_LIPSYNC
        private static SkinnedMeshRenderer FindVisemeMesh(GameObject root) =>
            root.GetComponentsInChildren<SkinnedMeshRenderer>()
                .FirstOrDefault(smr =>
                {
                    for (int i = 0; i < smr.sharedMesh.blendShapeCount; i++)
                        if (smr.sharedMesh.GetBlendShapeName(i).StartsWith("viseme_")) return true;
                    return false;
                });

        private static int FindBlendshapeIndex(SkinnedMeshRenderer smr, string name)
        {
            for (int i = 0; i < smr.sharedMesh.blendShapeCount; i++)
                if (smr.sharedMesh.GetBlendShapeName(i) == name) return i;
            return -1;
        }

        private static void SetVisemeBlendshape(OVRLipSyncContextMorphTarget mt, int visemeIdx, int bsIdx)
        {
            var so   = new SerializedObject(mt);
            var prop = so.FindProperty("visemeToBlendTargets");
            if (prop != null && prop.isArray && visemeIdx < prop.arraySize)
            {
                prop.GetArrayElementAtIndex(visemeIdx).intValue = bsIdx;
                so.ApplyModifiedProperties();
            }
        }
#endif

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }
    }
}
#endif
