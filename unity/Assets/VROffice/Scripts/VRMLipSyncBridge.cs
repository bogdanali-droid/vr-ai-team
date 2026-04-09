using System.Collections.Generic;
using UnityEngine;

// Acest script necesita doua SDK-uri optionale:
// 1. UniVRM       — https://github.com/vrm-c/UniVRM/releases
// 2. OVRLipSync   — inclus in Meta XR SDK (Package Manager)
//
// Fara ele, scriptul se compileaza fara erori dar LipSync e dezactivat.

#if VRM_IMPORTED
using VRM;
#endif

namespace VROffice
{
    /// <summary>
    /// Bridge OVRLipSync → VRM BlendShapeProxy.
    /// Activ doar daca sunt importate atat UniVRM cat si OVRLipSync.
    /// </summary>
#if VRM_IMPORTED && OVR_LIPSYNC
    [RequireComponent(typeof(OVRLipSyncContext))]
#endif
    public class VRMLipSyncBridge : MonoBehaviour
    {
#if VRM_IMPORTED && OVR_LIPSYNC
        [Header("VRM")]
        [Tooltip("VRMBlendShapeProxy al avatarului.")]
        public VRMBlendShapeProxy blendShapeProxy;

        [Header("Sensitivitate")]
        [Range(0f, 2f)]
        public float amplification = 1.2f;

        [Range(0.01f, 0.3f)]
        public float smoothing = 0.08f;

        private static readonly (int visemeIdx, BlendShapePreset preset, float weight)[] VisemeToVRM =
        {
            (0,  BlendShapePreset.Neutral, 1.0f),
            (1,  BlendShapePreset.A,       0.2f),
            (2,  BlendShapePreset.A,       0.3f),
            (3,  BlendShapePreset.A,       0.25f),
            (4,  BlendShapePreset.A,       0.6f),
            (5,  BlendShapePreset.A,       0.5f),
            (6,  BlendShapePreset.I,       0.7f),
            (7,  BlendShapePreset.I,       0.6f),
            (8,  BlendShapePreset.A,       0.5f),
            (9,  BlendShapePreset.U,       0.6f),
            (10, BlendShapePreset.A,       1.0f),
            (11, BlendShapePreset.E,       1.0f),
            (12, BlendShapePreset.I,       1.0f),
            (13, BlendShapePreset.O,       1.0f),
            (14, BlendShapePreset.U,       1.0f),
        };

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

        private void Awake()
        {
            _ctx = GetComponent<OVRLipSyncContext>();

            if (blendShapeProxy == null)
                blendShapeProxy = GetComponentInChildren<VRMBlendShapeProxy>();

            if (blendShapeProxy == null)
                Debug.LogError("[VRMLipSync] VRMBlendShapeProxy negasit!");
        }

        private void Update()
        {
            if (_ctx == null || blendShapeProxy == null) return;

            var frame = _ctx.GetCurrentPhonemeFrame();
            if (frame == null) return;

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
#else
        private void Awake()
        {
            Debug.Log("[VRMLipSyncBridge] Inactiv — importa UniVRM si OVRLipSync pentru LipSync complet.");
        }
#endif
    }
}
