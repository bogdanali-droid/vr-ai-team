using System;
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

            AudioClip clip = null;

            if (!string.IsNullOrEmpty(audio_b64))
            {
                byte[] mp3Bytes = Convert.FromBase64String(audio_b64);
                clip = Mp3ToAudioClip(mp3Bytes);
            }

            if (clip != null)
            {
                // Porneste animatie talking
                animator?.SetBool(_IsTalking, true);
                animator?.SetBool(_IsListening, false);

                _audioSource.clip = clip;
                _audioSource.Play();

                // LipSync proceseaza AudioSource-ul automat daca e configurat corect
                // (OVRLipSyncContext.audioSource = this._audioSource)

                yield return new WaitForSeconds(clip.length);
            }

            SetIdle();
            _isBusy = false;
        }

        // ------------------------------------------------------------------ //
        // MP3 bytes -> AudioClip
        // Unity nu suporta MP3 nativ pe Quest in runtime.
        // Optiune 1: ElevenLabs trimite PCM16/WAV (configureaza output_format=pcm_16000)
        // Optiune 2: Foloseste un plugin (NAudio, MiniMP3, etc.)
        // Deocamdata: placeholder — schimba cu solutia aleasa.

        private static AudioClip Mp3ToAudioClip(byte[] mp3Bytes)
        {
            // TODO: integreaza MiniMP3 sau solicita WAV de la ElevenLabs
            // Placeholder: returneaza null (va fi inlocuit)
            Debug.LogWarning("[Ana] Mp3ToAudioClip: nevoie de decoder MP3. " +
                             "Seteaza ElevenLabs output_format=pcm_16000 si treci la WAV.");
            return null;
        }

        // Alternativa recomandata: solicita WAV de la backend si decode direct
        private static AudioClip WavBytesToAudioClip(byte[] wavBytes, string clipName = "ana")
        {
            // Sari header-ul WAV (44 bytes standard)
            const int headerSize = 44;
            if (wavBytes.Length <= headerSize) return null;

            int sampleRate    = BitConverter.ToInt32(wavBytes, 24);
            int channels      = BitConverter.ToInt16(wavBytes, 22);
            int numSamples    = (wavBytes.Length - headerSize) / 2;

            var samples = new float[numSamples];
            for (int i = 0; i < numSamples; i++)
            {
                short s = BitConverter.ToInt16(wavBytes, headerSize + i * 2);
                samples[i] = s / 32768f;
            }

            var clip = AudioClip.Create(clipName, numSamples / channels,
                                        channels, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
