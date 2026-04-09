using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using UnityEngine;

namespace VROffice
{
    /// <summary>
    /// Speech-to-Text via Whisper API (OpenAI).
    /// Primeste un AudioClip inregistrat, returneaza textul transcris.
    /// </summary>
    public class WhisperSTT : MonoBehaviour
    {
        [Header("Whisper API")]
        [Tooltip("Lasa gol pentru OpenAI. Sau un endpoint compatibil (ex: local Whisper).")]
        public string apiUrl = "https://api.openai.com/v1/audio/transcriptions";
        public string apiKey;  // seteaza din Inspector sau citeste din env
        public string language = "ro";

        private static readonly HttpClient _http = new();

        // ------------------------------------------------------------------ //

        /// <summary>
        /// Transcrie un AudioClip la text.
        /// Apeleaza await pe main thread sau dintr-un coroutine cu Task.
        /// </summary>
        public async Task<string> Transcribe(AudioClip clip)
        {
            if (clip == null || clip.length < 0.3f)
                return string.Empty;

            byte[] wavBytes = AudioClipToWav(clip);

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent("whisper-1"), "model");
            content.Add(new StringContent(language), "language");

            var fileContent = new ByteArrayContent(wavBytes);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav");
            content.Add(fileContent, "file", "audio.wav");

            var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
            {
                Content = content
            };
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            try
            {
                var response = await _http.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonUtility.FromJson<WhisperResponse>(json);
                return result?.text?.Trim() ?? string.Empty;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Whisper] Eroare: {ex.Message}");
                return string.Empty;
            }
        }

        // ------------------------------------------------------------------ //
        // AudioClip -> WAV bytes (PCM16, mono)

        private static byte[] AudioClipToWav(AudioClip clip)
        {
            var samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            // Downsample la mono daca e stereo
            float[] mono = clip.channels == 1 ? samples : ToMono(samples, clip.channels);

            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            int sampleRate = clip.frequency;
            int numSamples = mono.Length;
            int dataBytes   = numSamples * 2; // PCM16

            // WAV header
            writer.Write(new char[] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + dataBytes);
            writer.Write(new char[] { 'W', 'A', 'V', 'E' });
            writer.Write(new char[] { 'f', 'm', 't', ' ' });
            writer.Write(16);          // chunk size
            writer.Write((short)1);   // PCM
            writer.Write((short)1);   // mono
            writer.Write(sampleRate);
            writer.Write(sampleRate * 2); // byte rate
            writer.Write((short)2);   // block align
            writer.Write((short)16);  // bits per sample
            writer.Write(new char[] { 'd', 'a', 't', 'a' });
            writer.Write(dataBytes);

            foreach (var s in mono)
            {
                short pcm = (short)Mathf.Clamp(s * 32767f, -32768f, 32767f);
                writer.Write(pcm);
            }

            return ms.ToArray();
        }

        private static float[] ToMono(float[] stereo, int channels)
        {
            var mono = new float[stereo.Length / channels];
            for (int i = 0; i < mono.Length; i++)
            {
                float sum = 0f;
                for (int c = 0; c < channels; c++)
                    sum += stereo[i * channels + c];
                mono[i] = sum / channels;
            }
            return mono;
        }

        [Serializable]
        private class WhisperResponse { public string text; }
    }
}
