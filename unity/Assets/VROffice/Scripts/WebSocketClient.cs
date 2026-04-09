using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
// NativeWebSocket — adauga in Package Manager:
// + Add package from git URL: https://github.com/endel/NativeWebSocket.git#upm
// Dupa instalare, defineste scriptul NATIVE_WEBSOCKET in:
// Edit > Project Settings > Player > Other Settings > Scripting Define Symbols

#if NATIVE_WEBSOCKET
using NativeWebSocket;
#endif

namespace VROffice
{
    /// <summary>
    /// WebSocket client Unity <-> Backend Python.
    /// Necesita pachetul NativeWebSocket (vezi instructiunile de mai sus).
    /// Pana la instalare, scriptul compileaza dar conexiunea e inactiva.
    /// </summary>
    public class WebSocketClient : MonoBehaviour
    {
        [Header("Connection")]
        [Tooltip("ws://[IP-PC]:8765")]
        public string serverUrl = "ws://192.168.1.100:8765";
        [SerializeField] private float reconnectDelaySec = 3f;

        private readonly Queue<string> _incomingQueue = new();

        public event Action<string> OnJsonReceived;

#if NATIVE_WEBSOCKET
        private WebSocket _ws;
        private bool _reconnecting;
        public bool IsConnected => _ws?.State == WebSocketState.Open;

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

        public async Task Send(object data)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("[WS] Nu esti conectat. Mesajul a fost ignorat.");
                return;
            }
            await _ws.SendText(JsonUtility.ToJson(data));
        }

        private async void OnApplicationQuit()
        {
            if (_ws != null) await _ws.Close();
        }

#else
        // Stub — compileaza fara NativeWebSocket
        public bool IsConnected => false;

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
            Debug.LogWarning("[WS] NativeWebSocket nu e instalat. " +
                "Adauga pachetul din: https://github.com/endel/NativeWebSocket.git#upm");
        }

        private void Update()
        {
            lock (_incomingQueue)
            {
                while (_incomingQueue.Count > 0)
                    OnJsonReceived?.Invoke(_incomingQueue.Dequeue());
            }
        }

        public Task Connect() { return Task.CompletedTask; }

        public Task Send(object data)
        {
            Debug.LogWarning("[WS] Nu e conectat — instaleaza NativeWebSocket.");
            return Task.CompletedTask;
        }
#endif
    }

    // ---- Modele de date ---- //

    [Serializable]
    public class UserMessage
    {
        public string type       = "user_message";
        public string agent      = "ana";
        public string text;
        public string session_id;
    }

    [Serializable]
    public class AgentResponse
    {
        public string type;
        public string agent;
        public string text;
        public string audio_b64;
    }
}
