using System;
using UnityEngine;

namespace VROffice
{
    /// <summary>
    /// Utilitare audio: WAV bytes <-> AudioClip, base64 decode.
    /// ElevenLabs recomandat: output_format = pcm_16000 (WAV raw, mono, 16kHz)
    /// Seteaza in elevenlabs_client.py: output_format="pcm_16000"
    /// </summary>
    public static class AudioUtils
    {
        /// <summary>
        /// Decode base64 WAV (pcm_16000) -> AudioClip.
        /// Compatibil cu output_format="pcm_16000" de la ElevenLabs.
        /// </summary>
        public static AudioClip Base64PcmToAudioClip(string base64, string clipName = "tts")
        {
            if (string.IsNullOrEmpty(base64)) return null;

            byte[] pcmBytes;
            try { pcmBytes = Convert.FromBase64String(base64); }
            catch (Exception e)
            {
                Debug.LogError($"[AudioUtils] Base64 decode fail: {e.Message}");
                return null;
            }

            // ElevenLabs pcm_16000: raw PCM16 LE, mono, 16000 Hz (FARA header WAV)
            const int sampleRate = 16000;
            const int channels   = 1;
            int numSamples = pcmBytes.Length / 2;

            if (numSamples == 0) return null;

            var samples = new float[numSamples];
            for (int i = 0; i < numSamples; i++)
            {
                short s = BitConverter.ToInt16(pcmBytes, i * 2);
                samples[i] = s / 32768f;
            }

            var clip = AudioClip.Create(clipName, numSamples, channels, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Decode base64 WAV cu header (output_format="pcm_44100" sau wav standard).
        /// </summary>
        public static AudioClip Base64WavToAudioClip(string base64, string clipName = "tts")
        {
            if (string.IsNullOrEmpty(base64)) return null;

            byte[] wavBytes;
            try { wavBytes = Convert.FromBase64String(base64); }
            catch (Exception e)
            {
                Debug.LogError($"[AudioUtils] Base64 decode fail: {e.Message}");
                return null;
            }

            return WavBytesToClip(wavBytes, clipName);
        }

        private static AudioClip WavBytesToClip(byte[] wav, string clipName)
        {
            const int headerSize = 44;
            if (wav.Length <= headerSize) return null;

            int channels   = BitConverter.ToInt16(wav, 22);
            int sampleRate = BitConverter.ToInt32(wav, 24);
            int numSamples = (wav.Length - headerSize) / 2;

            var samples = new float[numSamples];
            for (int i = 0; i < numSamples; i++)
            {
                short s = BitConverter.ToInt16(wav, headerSize + i * 2);
                samples[i] = s / 32768f;
            }

            var clip = AudioClip.Create(clipName, numSamples / channels, channels, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
