using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem; // Meta XR InputSystem (sau OVRInput)

namespace VROffice
{
    /// <summary>
    /// Orchestreaza fluxul vocal:
    ///   Apasat trigger -> Inregistrare mic -> Whisper STT -> WebSocket -> primeste audio+text
    /// </summary>
    [RequireComponent(typeof(WhisperSTT))]
    public class VoicePipeline : MonoBehaviour
    {
        [Header("References")]
        public WebSocketClient wsClient;
        public AnaAvatarController ana;

        [Header("Recording")]
        [Tooltip("Durata maxima inregistrare in secunde.")]
        public int maxRecordSec = 8;
        [Tooltip("Frecventa Hz pentru microfon.")]
        public int sampleRate = 16000;

        [Header("Input — Meta Quest 3")]
        [Tooltip("Tine apasat trigger dreapta pentru a vorbi.")]
        public OVRInput.Button pushToTalkButton = OVRInput.Button.PrimaryIndexTrigger;

        // ------------------------------------------------------------------ //

        private WhisperSTT _stt;
        private bool _isRecording;
        private AudioClip _mic;
        private string _sessionId;

        public event Action<string> OnUserTextReady;   // text transcris
        public event Action<string> OnAnaTextReceived; // raspuns text de la Ana

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
            if (OVRInput.GetDown(pushToTalkButton)) StartRecording();
            if (OVRInput.GetUp(pushToTalkButton))   StopRecordingAndSend();
        }

        // ------------------------------------------------------------------ //

        private void StartRecording()
        {
            if (_isRecording) return;
            _isRecording = true;

            string mic = Microphone.devices.Length > 0 ? Microphone.devices[0] : null;
            _mic = Microphone.Start(mic, false, maxRecordSec, sampleRate);
            Debug.Log("[Pipeline] Inregistrare pornita...");
        }

        private async void StopRecordingAndSend()
        {
            if (!_isRecording) return;
            _isRecording = false;

            int pos = Microphone.GetPosition(null);
            Microphone.End(null);

            if (pos < sampleRate * 0.3f) // mai putin de 300ms -> ignora
            {
                Debug.Log("[Pipeline] Inregistrare prea scurta, ignorata.");
                return;
            }

            // Taie AudioClip la lungimea reala
            var trimmed = TrimClip(_mic, pos);

            // STT
            Debug.Log("[Pipeline] Transcriere...");
            string text = await _stt.Transcribe(trimmed);

            if (string.IsNullOrWhiteSpace(text))
            {
                Debug.Log("[Pipeline] Transcriere goala, ignorata.");
                return;
            }

            Debug.Log($"[Pipeline] User: {text}");
            OnUserTextReady?.Invoke(text);

            // Trimite la backend
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
