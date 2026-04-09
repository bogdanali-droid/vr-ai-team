using System;
using System.Threading.Tasks;
using UnityEngine;

namespace VROffice
{
    /// <summary>
    /// Orchestreaza fluxul vocal:
    ///   Apasat trigger (Quest) sau Spatiu (PC) -> Inregistrare mic -> STT -> WebSocket -> audio+text
    /// </summary>
    [RequireComponent(typeof(WhisperSTT))]
    public class VoicePipeline : MonoBehaviour
    {
        [Header("References")]
        public WebSocketClient wsClient;
        public AnaAvatarController ana;

        [Header("Recording")]
        public int maxRecordSec = 8;
        public int sampleRate   = 16000;

        // ------------------------------------------------------------------ //

        private WhisperSTT _stt;
        private bool _isRecording;
        private AudioClip _mic;
        private string _sessionId;

        public event Action<string> OnUserTextReady;
        public event Action<string> OnAnaTextReceived;

        // ------------------------------------------------------------------ //

        private void Awake()
        {
            _stt       = GetComponent<WhisperSTT>();
            _sessionId = Guid.NewGuid().ToString();
        }

        private void OnEnable()
        {
            if (wsClient != null)
                wsClient.OnJsonReceived += HandleBackendMessage;
        }

        private void OnDisable()
        {
            if (wsClient != null)
                wsClient.OnJsonReceived -= HandleBackendMessage;
        }

        private void Update()
        {
#if OVR_INPUT
            // Quest 3 — trigger dreapta
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger)) StartRecording();
            if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger))   StopRecordingAndSend();
#else
            // PC fallback — tasta Spatiu
            if (Input.GetKeyDown(KeyCode.Space)) StartRecording();
            if (Input.GetKeyUp(KeyCode.Space))   StopRecordingAndSend();
#endif
        }

        // ------------------------------------------------------------------ //

        private void StartRecording()
        {
            if (_isRecording) return;
            _isRecording = true;
            ana?.SetListening();

            string mic = Microphone.devices.Length > 0 ? Microphone.devices[0] : null;
            _mic = Microphone.Start(mic, false, maxRecordSec, sampleRate);
            Debug.Log("[Pipeline] Inregistrare pornita (tine apasat Spatiu / Trigger)...");
        }

        private async void StopRecordingAndSend()
        {
            if (!_isRecording) return;
            _isRecording = false;

            int pos = Microphone.GetPosition(null);
            Microphone.End(null);

            if (pos < sampleRate * 0.3f)
            {
                Debug.Log("[Pipeline] Inregistrare prea scurta, ignorata.");
                ana?.SetIdle();
                return;
            }

            var trimmed = TrimClip(_mic, pos);

            Debug.Log("[Pipeline] Transcriere...");
            string text = await _stt.Transcribe(trimmed);

            if (string.IsNullOrWhiteSpace(text))
            {
                Debug.Log("[Pipeline] Transcriere goala, ignorata.");
                ana?.SetIdle();
                return;
            }

            Debug.Log($"[Pipeline] User: {text}");
            OnUserTextReady?.Invoke(text);

            await wsClient.Send(new UserMessage
            {
                agent      = "ana",
                text       = text,
                session_id = _sessionId,
            });
        }

        private void HandleBackendMessage(string json)
        {
            var response = JsonUtility.FromJson<AgentResponse>(json);
            if (response?.type != "agent_response") return;

            Debug.Log($"[Pipeline] Ana: {response.text}");
            OnAnaTextReceived?.Invoke(response.text);
            ana?.PlayResponse(response.text, response.audio_b64);
        }

        // ------------------------------------------------------------------ //

        private static AudioClip TrimClip(AudioClip clip, int samples)
        {
            var data = new float[samples * clip.channels];
            clip.GetData(data, 0);
            var trimmed = AudioClip.Create("trimmed", samples, clip.channels,
                                           clip.frequency, false);
            trimmed.SetData(data, 0);
            return trimmed;
        }
    }
}
