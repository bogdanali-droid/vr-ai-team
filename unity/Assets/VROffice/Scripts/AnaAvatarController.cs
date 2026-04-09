using System.Collections;
using UnityEngine;
// Oculus LipSync SDK — import manual
// https://developer.oculus.com/downloads/package/oculus-lipsync-unity/
using Oculus.LipSync;

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
        public OVRLipSyncContext lipSyncContext;  // pe acelasi GameObject cu AudioSource

        [Header("LipSync")]
        [Tooltip("OVRLipSyncContextMorphTarget atasat avatarului.")]
        public OVRLipSyncContextMorphTarget morphTarget;

        // Parametri Animator (trebuie definiti in Unity Animator Controller)
        private static readonly int _IsTalking   = Animator.StringToHash("IsTalking");
        private static readonly int _IsListening = Animator.StringToHash("IsListening");
        private static readonly int _Greet       = Animator.StringToHash("Greet");

        private AudioSource _audioSource;
        private bool _isBusy;

        // ------------------------------------------------------------------ //

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.spatialBlend = 1f; // 3D sound in VR
        }

        private void Start()
        {
            SetIdle();
        }

        // ------------------------------------------------------------------ //
        // API public apelat din VoicePipeline

        /// <summary>
        /// Primeste raspunsul de la backend si il reda.
        /// </summary>
        public void PlayResponse(string text, string audio_b64)
        {
            if (_isBusy)
            {
                // Opreste ce redai si treci la noul raspuns
                StopAllCoroutines();
                _audioSource.Stop();
            }
            StartCoroutine(PlayRoutine(text, audio_b64));
        }

        /// <summary>Apelat cand utilizatorul tine butonul apasat (inregistreaza).</summary>
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

        // ------------------------------------------------------------------ //

        private IEnumerator PlayRoutine(string text, string audio_b64)
        {
            _isBusy = true;

            // ElevenLabs output_format=pcm_16000 -> raw PCM16, decode direct cu AudioUtils
            AudioClip clip = AudioUtils.Base64PcmToAudioClip(audio_b64, "ana_response");

            if (clip != null)
            {
                animator?.SetBool(_IsTalking, true);
                animator?.SetBool(_IsListening, false);

                // OVRLipSyncContext trebuie sa aiba audioSource = _audioSource
                // si processAudioSamples = true
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
