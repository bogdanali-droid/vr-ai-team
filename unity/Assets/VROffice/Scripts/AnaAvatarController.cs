using System.Collections;
using UnityEngine;
// OVRLipSyncContext este in namespace global (Meta XR SDK)
// Nu necesita 'using Oculus.LipSync'

namespace VROffice
{
    /// <summary>
    /// Controller avatar Ana:
    ///   - Primeste text + audio_b64 de la backend
    ///   - Decode audio -> AudioClip
    ///   - Reda audio + LipSync
    ///   - Gestioneaza animatiile (idle, talking, listening)
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AnaAvatarController : MonoBehaviour
    {
        [Header("Components")]
        public Animator animator;

#if OVR_LIPSYNC
        [Tooltip("OVRLipSyncContext pe acelasi GameObject cu AudioSource.")]
        public OVRLipSyncContext lipSyncContext;
        [Tooltip("OVRLipSyncContextMorphTarget atasat avatarului.")]
        public OVRLipSyncContextMorphTarget morphTarget;
#endif

        // Parametri Animator
        private static readonly int _IsTalking   = Animator.StringToHash("IsTalking");
        private static readonly int _IsListening = Animator.StringToHash("IsListening");

        private AudioSource _audioSource;
        private bool _isBusy;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.spatialBlend = 1f;
        }

        private void Start()
        {
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
            animator?.SetBool(_IsListening, true);
            animator?.SetBool(_IsTalking, false);
        }

        public void SetIdle()
        {
            animator?.SetBool(_IsListening, false);
            animator?.SetBool(_IsTalking, false);
        }

        private IEnumerator PlayRoutine(string text, string audio_b64)
        {
            _isBusy = true;

            AudioClip clip = AudioUtils.Base64PcmToAudioClip(audio_b64, "ana_response");

            if (clip != null)
            {
                animator?.SetBool(_IsTalking, true);
                animator?.SetBool(_IsListening, false);

                _audioSource.clip = clip;
                _audioSource.Play();

                yield return new WaitForSeconds(clip.length);
            }
            else
            {
                Debug.LogWarning("[Ana] AudioClip null — verifica audio_b64 si ElevenLabs output_format=pcm_16000");
            }

            SetIdle();
            _isBusy = false;
        }
    }
}
