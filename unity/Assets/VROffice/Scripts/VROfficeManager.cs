using UnityEngine;

namespace VROffice
{
    /// <summary>
    /// Managerul principal al scenei VR Office.
    /// Conecteaza toate componentele si gestioneaza starea globala.
    /// Faza 1: 1 avatar (Ana) + pipeline voce.
    /// </summary>
    public class VROfficeManager : MonoBehaviour
    {
        [Header("Scene References")]
        public WebSocketClient wsClient;
        public VoicePipeline voicePipeline;
        public AnaAvatarController ana;

        [Header("UI / Debug")]
        [SerializeField] private TMPro.TMP_Text debugText; // optional

        // ------------------------------------------------------------------ //

        private void Awake()
        {
            ValidateReferences();
        }

        private void OnEnable()
        {
            voicePipeline.OnUserTextReady   += OnUserSpoke;
            voicePipeline.OnAnaTextReceived += OnAnaReplied;
        }

        private void OnDisable()
        {
            voicePipeline.OnUserTextReady   -= OnUserSpoke;
            voicePipeline.OnAnaTextReceived -= OnAnaReplied;
        }

        // ------------------------------------------------------------------ //

        private void OnUserSpoke(string text)
        {
            Log($"Tu: {text}");
            ana.SetListening();
        }

        private void OnAnaReplied(string text)
        {
            Log($"Ana: {text}");
        }

        // ------------------------------------------------------------------ //

        private void Log(string msg)
        {
            Debug.Log($"[Office] {msg}");
            if (debugText != null)
                debugText.text = msg;
        }

        private void ValidateReferences()
        {
            if (wsClient == null)      Debug.LogError("[Office] wsClient lipsa!");
            if (voicePipeline == null) Debug.LogError("[Office] voicePipeline lipsa!");
            if (ana == null)           Debug.LogError("[Office] ana lipsa!");
        }
    }
}
