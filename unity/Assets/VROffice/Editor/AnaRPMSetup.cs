#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using VROffice;

namespace VROffice.Editor
{
    /// <summary>
    /// Menu: VROffice > Setup Ana RPM Avatar
    ///
    /// Cum se foloseste:
    /// 1. Mergi la readyplayer.me si creeaza avatarul Anei:
    ///    - Par: blond  |  Ochi: verzi  |  Outfit: office
    ///    - Format export: GLB, Pose: T-Pose, Morph targets: ARKit + Oculus Visemes
    /// 2. Importa fisierul .glb in Unity: Assets/VROffice/Avatars/Ana/
    /// 3. Drag & drop modelul in scena pe GameObject-ul "Ana"
    /// 4. Selecteaza GameObject-ul "Ana" in Hierarchy
    /// 5. Menu: VROffice > Setup Ana RPM Avatar
    ///
    /// Scriptul face automat:
    ///   - Detecteaza SkinnedMeshRenderer cu blendshapes RPM
    ///   - Adauga OVRLipSyncContext + OVRLipSyncContextMorphTarget
    ///   - Mapeaza toti cei 15 viseme RPM -> Oculus LipSync
    ///   - Configureaza AudioSource pentru LipSync
    ///   - Updateaza referintele in AnaAvatarController
    /// </summary>
    public static class AnaRPMSetup
    {
        // Mapare viseme RPM -> index Oculus LipSync (OVRLipSync.Viseme enum)
        // RPM exporta blendshape-uri cu prefix "viseme_"
        private static readonly (string blendshape, int visemeIndex)[] VisemeMap =
        {
            ("viseme_sil", 0),
            ("viseme_PP",  1),
            ("viseme_FF",  2),
            ("viseme_TH",  3),
            ("viseme_DD",  4),
            ("viseme_kk",  5),
            ("viseme_CH",  6),
            ("viseme_SS",  7),
            ("viseme_nn",  8),
            ("viseme_RR",  9),
            ("viseme_aa",  10),
            ("viseme_E",   11),
            ("viseme_I",   12),
            ("viseme_O",   13),
            ("viseme_U",   14),
        };

        // ------------------------------------------------------------------ //

        [MenuItem("VROffice/Setup Ana RPM Avatar")]
        public static void SetupAnaAvatar()
        {
            var selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("VROffice", "Selecteaza GameObject-ul 'Ana' in Hierarchy.", "OK");
                return;
            }

            // 1. Gaseste SkinnedMeshRenderer cu viseme blendshapes
            var smr = FindVisemeMesh(selected);
            if (smr == null)
            {
                EditorUtility.DisplayDialog("VROffice",
                    "Nu am gasit blendshape-uri 'viseme_' pe avatar.\n\n" +
                    "La export din Ready Player Me asigura-te ca ai selectat:\n" +
                    "Morph Targets: ARKit + Oculus Visemes", "OK");
                return;
            }

            Debug.Log($"[RPM Setup] Gasit mesh cu viseme: {smr.name}");

            // 2. AudioSource pe root
            var audioSource = EnsureComponent<AudioSource>(selected);
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode  = AudioRolloffMode.Linear;
            audioSource.maxDistance  = 10f;
            audioSource.playOnAwake  = false;

            // 3. OVRLipSyncContext pe root
            var lipSyncCtx = EnsureComponent<OVRLipSyncContext>(selected);
            lipSyncCtx.audioSource         = audioSource;
            lipSyncCtx.enableAcceleration  = true;

            // 4. OVRLipSyncContextMorphTarget pe root
            var morphTarget = EnsureComponent<OVRLipSyncContextMorphTarget>(selected);
            morphTarget.skinnedMeshRenderer = smr;

            // Mapeaza viseme-urile
            int mapped = 0;
            foreach (var (blendshape, visemeIndex) in VisemeMap)
            {
                int bsIndex = FindBlendshapeIndex(smr, blendshape);
                if (bsIndex >= 0)
                {
                    // OVRLipSyncContextMorphTarget.visemeToBlendTargets e un array de int[15]
                    // Il setam via SerializedObject pentru a suporta toate versiunile SDK
                    SetVisemeBlendshape(morphTarget, visemeIndex, bsIndex);
                    mapped++;
                }
                else
                {
                    Debug.LogWarning($"[RPM Setup] Blendshape '{blendshape}' negasit pe {smr.name}");
                }
            }

            Debug.Log($"[RPM Setup] Mapate {mapped}/15 viseme.");

            // 5. Updateaza AnaAvatarController
            var anaCtrl = selected.GetComponent<AnaAvatarController>();
            if (anaCtrl == null)
                anaCtrl = selected.AddComponent<AnaAvatarController>();

            anaCtrl.lipSyncContext = lipSyncCtx;
            anaCtrl.morphTarget    = morphTarget;
            anaCtrl.animator       = selected.GetComponent<Animator>();

            // 6. Marcheaza scena ca modificata
            EditorUtility.SetDirty(selected);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            string summary = $"Setup complet!\n\n" +
                             $"Mesh: {smr.name}\n" +
                             $"Viseme mapate: {mapped}/15\n\n" +
                             (mapped < 15
                                 ? "ATENTIE: Nu toate viseme-urile au fost gasite.\n" +
                                   "Verifica setarile de export RPM (Morph Targets: Oculus Visemes).\n\n"
                                 : "") +
                             "Urmatorii pasi:\n" +
                             "1. Asigneaza Animator Controller (AnaAnimator.controller)\n" +
                             "2. Seteaza ANA_VOICE_ID in backend/.env\n" +
                             "3. Testeaza cu backend-ul pornit";

            EditorUtility.DisplayDialog("VROffice — Ana Setup", summary, "OK");
        }

        // ------------------------------------------------------------------ //

        [MenuItem("VROffice/Setup Ana RPM Avatar", true)]
        private static bool ValidateSetup() => Selection.activeGameObject != null;

        // ------------------------------------------------------------------ //

        private static SkinnedMeshRenderer FindVisemeMesh(GameObject root)
        {
            return root.GetComponentsInChildren<SkinnedMeshRenderer>()
                       .FirstOrDefault(smr => HasVisemeBlendshapes(smr));
        }

        private static bool HasVisemeBlendshapes(SkinnedMeshRenderer smr)
        {
            for (int i = 0; i < smr.sharedMesh.blendShapeCount; i++)
            {
                if (smr.sharedMesh.GetBlendShapeName(i).StartsWith("viseme_"))
                    return true;
            }
            return false;
        }

        private static int FindBlendshapeIndex(SkinnedMeshRenderer smr, string name)
        {
            for (int i = 0; i < smr.sharedMesh.blendShapeCount; i++)
            {
                if (smr.sharedMesh.GetBlendShapeName(i) == name)
                    return i;
            }
            return -1;
        }

        private static void SetVisemeBlendshape(OVRLipSyncContextMorphTarget morphTarget,
                                                 int visemeIndex, int blendshapeIndex)
        {
            var so   = new SerializedObject(morphTarget);
            var prop = so.FindProperty("visemeToBlendTargets");
            if (prop != null && prop.isArray && visemeIndex < prop.arraySize)
            {
                prop.GetArrayElementAtIndex(visemeIndex).intValue = blendshapeIndex;
                so.ApplyModifiedProperties();
            }
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }
    }
}
#endif
