using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
// Pachet: https://github.com/endel/NativeWebSocket
// Add via Package Manager: https://github.com/endel/NativeWebSocket.git
using NativeWebSocket;

namespace VROffice
{
    /// <summary>
    /// WebSocket client persistent — Unity <-> Backend Python.
    /// Adauga acest component pe un GameObject cu DontDestroyOnLoad.
    /// </summary>
    public class WebSocketClient : MonoBehaviour
    {
        [Header("Connection")]
        [Tooltip("ws://[IP-PC]:8765")]
        public string serverUrl = "ws://192.168.1.100:8765";
        [SerializeField] private float reconnectDelaySec = 3f;

        private WebSocket _ws;
        private readonly Queue<string> _incomingQueue = new();
        private bool _reconnecting;

        /// <summary>Mesaj primit de la backend (thread-safe, pe main thread).</summary>
        public event Action<string> OnJsonReceived;
        public bool IsConnected => _ws?.State == WebSocketState.Open;

        // ------------------------------------------------------------------ //

        private async void Start()
        {
            DontDestroyOnLoad(gameObject);
            await Connect();
        }

        private void Update()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            _ws?.DispatchMessageQueue();
#endif
            lock (_incomingQueue)
            {
                while (_incomingQueue.Count > 0)
                    OnJsonReceived?.Invoke(_incomingQueue.Dequeue());
            }
        }

        // ------------------------------------------------------------------ //

        public async Task Connect()
        {
            _ws = new WebSocket(serverUrl);

            _ws.OnOpen    += () => Debug.Log("[WS] Conectat la backend");
            _ws.OnError   += err => Debug.LogError($"[WS] Eroare: {err}");
            _ws.OnClose   += code =>
            {
                Debug.LogWarning($"[WS] Deconectat ({code}). Reconectare in {reconnectDelaySec}s...");
                if (!_reconnecting) Reconnect();
            };
            _ws.OnMessage += bytes =>
            {
                var json = System.Text.Encoding.UTF8.GetString(bytes);
                lock (_incomingQueue) _incomingQueue.Enqueue(json);
            };

            await _ws.Connect();
        }

        private async void Reconnect()
        {
            _reconnecting = true;
            await Task.Delay((int)(reconnectDelaySec * 1000));
            _reconnecting = false;
            await Connect();
        }

        /// <summary>Trimite un obiect serializat JSON la backend.</summary>
        public async Task Send(object data)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("[WS] Nu esti conectat. Mesajul a fost ignorat.");
                return;
            }
            var json = JsonUtility.ToJson(data);
            await _ws.SendText(json);
        }

        private async void OnApplicationQuit()
        {
            if (_ws != null)
                await _ws.Close();
        }
    }

    // ---- Modele de date trimise / primite ---- //

    [Serializable]
    public class UserMessage
    {
        public string type    = "user_message";
        public string agent   = "ana";
        public string text;
        public string session_id;
    }

    [Serializable]
    public class AgentResponse
    {
        public string type;       // "agent_response"
        public string agent;
        public string text;
        public string audio_b64;  // mp3 base64
    }
}
