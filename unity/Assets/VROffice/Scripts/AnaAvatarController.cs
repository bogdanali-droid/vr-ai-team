using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VROffice
{
    [RequireComponent(typeof(AudioSource))]
    public class AnaAvatarController : MonoBehaviour
    {
        [Header("Components")]
        public Animator animator;

#if OVR_LIPSYNC
        public OVRLipSyncContext lipSyncContext;
        public OVRLipSyncContextMorphTarget morphTarget;
#endif

        private static readonly int _IsTalking   = Animator.StringToHash("IsTalking");
        private static readonly int _IsListening = Animator.StringToHash("IsListening");

        private AudioSource _audioSource;
        private bool _isBusy;
        private HashSet<int> _animParams;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.spatialBlend = 1f;
        }

        private void Start()
        {
            // Construieste lista de parametri disponibili (evita warnings)
            _animParams = new HashSet<int>();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                foreach (var p in animator.parameters)
                    _animParams.Add(p.nameHash);
            }
            SetIdle();
        }

        public void PlayResponse(string text, string audio_b64)
        {
            if (_isBusy)
            {
                StopAllCoroutines();
                _audioSource.Stop();
            }
            StartCoroutine(PlayRoutine(text, audio_b64));
        }

        public void SetListening()
        {
            SetAnimBool(_IsListening, true);
            SetAnimBool(_IsTalking, false);
        }

        public void SetIdle()
        {
            SetAnimBool(_IsListening, false);
            SetAnimBool(_IsTalking, false);
        }

        private IEnumerator PlayRoutine(string text, string audio_b64)
        {
            _isBusy = true;

            AudioClip clip = AudioUtils.Base64PcmToAudioClip(audio_b64, "ana_response");

            if (clip != null)
            {
                SetAnimBool(_IsTalking, true);
                SetAnimBool(_IsListening, false);

                _audioSource.clip = clip;
                _audioSource.Play();

                yield return new WaitForSeconds(clip.length);
            }
            else
            {
                Debug.LogWarning("[Ana] AudioClip null — verifica audio_b64");
            }

            SetIdle();
            _isBusy = false;
        }

        private void SetAnimBool(int hash, bool value)
        {
            if (animator == null) return;
            if (_animParams != null && !_animParams.Contains(hash)) return;
            animator.SetBool(hash, value);
        }
    }
}
