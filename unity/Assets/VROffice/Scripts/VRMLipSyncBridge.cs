using System.Collections.Generic;
using UnityEngine;
// UniVRM — import via Package Manager sau .unitypackage de pe:
// https://github.com/vrm-c/UniVRM/releases  (ex: UniVRM-0.127.0_xxxx.unitypackage)
using VRM;

namespace VROffice
{
    /// <summary>
    /// Bridge OVRLipSync → VRM BlendShapeProxy.
    ///
    /// VRoid Studio exporta blendshapes ca: A, I, U, E, O
    /// OVRLipSync lucreaza cu 15 viseme (sil, PP, FF, TH, DD, kk, CH, SS, nn, RR, aa, E, I, O, U)
    ///
    /// Aceasta clasa mapeaza viseme-urile Oculus la blendshape-urile VRM
    /// si le aplica in timp real pe VRMBlendShapeProxy.
    ///
    /// Adauga pe acelasi GameObject cu OVRLipSyncContext.
    /// </summary>
    [RequireComponent(typeof(OVRLipSyncContext))]
    public class VRMLipSyncBridge : MonoBehaviour
    {
        [Header("VRM")]
        [Tooltip("VRMBlendShapeProxy al avatarului. Se gaseste pe root-ul VRM.")]
        public VRMBlendShapeProxy blendShapeProxy;

        [Header("Sensitivitate")]
        [Range(0f, 2f)]
        [Tooltip("Amplificare blendshape (1 = normal, >1 = mai expresiv)")]
        public float amplification = 1.2f;

        [Range(0.01f, 0.3f)]
        [Tooltip("Smoothing — valori mici = mai rapid, mari = mai fluid")]
        public float smoothing = 0.08f;

        // ── Mapare Oculus Viseme Index → BlendShapeKey VRM ─────────────── //
        // VRM standard: A, I, U, E, O (din BlendShapePreset)
        // Aproximare pentru viseme-urile fara echivalent direct:
        //   PP/FF/TH → inchidere gura (SIL) + usor A
        //   DD/kk/nn/RR → A moderat
        //   CH/SS → I + E
        private static readonly (int visemeIdx, BlendShapePreset preset, float weight)[] VisemeToVRM =
        {
            (0,  BlendShapePreset.Neutral, 1.0f),  // sil  → Neutral
            (1,  BlendShapePreset.A,       0.2f),  // PP   → A slab (buze lipite)
            (2,  BlendShapePreset.A,       0.3f),  // FF   → A slab
            (3,  BlendShapePreset.A,       0.25f), // TH   → A slab
            (4,  BlendShapePreset.A,       0.6f),  // DD   → A moderat
            (5,  BlendShapePreset.A,       0.5f),  // kk   → A moderat
            (6,  BlendShapePreset.I,       0.7f),  // CH   → I
            (7,  BlendShapePreset.I,       0.6f),  // SS   → I
            (8,  BlendShapePreset.A,       0.5f),  // nn   → A
            (9,  BlendShapePreset.U,       0.6f),  // RR   → U
            (10, BlendShapePreset.A,       1.0f),  // aa   → A complet
            (11, BlendShapePreset.E,       1.0f),  // E    → E complet
            (12, BlendShapePreset.I,       1.0f),  // I    → I complet
            (13, BlendShapePreset.O,       1.0f),  // O    → O complet
            (14, BlendShapePreset.U,       1.0f),  // U    → U complet
        };

        // ── Valori curente (smooth) ──────────────────────────────────────── //
        private readonly Dictionary<BlendShapePreset, float> _current = new()
        {
            { BlendShapePreset.Neutral, 0f },
            { BlendShapePreset.A,       0f },
            { BlendShapePreset.I,       0f },
            { BlendShapePreset.U,       0f },
            { BlendShapePreset.E,       0f },
            { BlendShapePreset.O,       0f },
        };

        private OVRLipSyncContext _ctx;

        // ------------------------------------------------------------------ //

        private void Awake()
        {
            _ctx = GetComponent<OVRLipSyncContext>();

            if (blendShapeProxy == null)
                blendShapeProxy = GetComponentInChildren<VRMBlendShapeProxy>();

            if (blendShapeProxy == null)
                Debug.LogError("[VRMLipSync] VRMBlendShapeProxy negasit! Asigneaza manual in Inspector.");
        }

        private void Update()
        {
            if (_ctx == null || blendShapeProxy == null) return;

            var frame = _ctx.GetCurrentPhonemeFrame();
            if (frame == null) return;

            // Calculeaza target per preset VRM
            var targets = new Dictionary<BlendShapePreset, float>
            {
                { BlendShapePreset.Neutral, 0f },
                { BlendShapePreset.A,       0f },
                { BlendShapePreset.I,       0f },
                { BlendShapePreset.U,       0f },
                { BlendShapePreset.E,       0f },
                { BlendShapePreset.O,       0f },
            };

            foreach (var (visemeIdx, preset, weight) in VisemeToVRM)
            {
                if (visemeIdx < frame.Visemes.Length)
                {
                    float v = frame.Visemes[visemeIdx] * weight * amplification;
                    targets[preset] = Mathf.Max(targets[preset], v);
                }
            }

            // Smooth + aplica
            foreach (var preset in new[]
            {
                BlendShapePreset.Neutral,
                BlendShapePreset.A, BlendShapePreset.I,
                BlendShapePreset.U, BlendShapePreset.E,
                BlendShapePreset.O
            })
            {
                _current[preset] = Mathf.Lerp(
                    _current[preset],
                    Mathf.Clamp01(targets[preset]),
                    smoothing
                );
                blendShapeProxy.ImmediatelySetValue(
                    BlendShapeKey.CreateFromPreset(preset),
                    _current[preset]
                );
            }

            blendShapeProxy.Apply();
        }
    }
}
