# Arhitectura Sistem — VR AI Team

## Flux principal (Faza 1)

```
[User în VR]
     │
     │ vorbește (Meta Quest 3 mic)
     ▼
[Unity — STT]
  Whisper API / on-device
     │
     │ text transcris
     ▼
[Backend Python — Claude API]
  System prompt Ana
  + istoric conversatie
     │
     │ text raspuns
     ▼
[ElevenLabs TTS]
  Voice ID: Ana
  Streaming audio
     │
     │ audio stream
     ▼
[Unity — Oculus LipSync]
  Redare audio
  Animatie buze sincronizata
  Gesturi avatar
     │
     ▼
[User aude Ana vorbind]
```

## Componente

### Backend (Python)
- `claude_client.py` — wrapper Claude API cu memory conversatie
- `elevenlabs_client.py` — TTS streaming cu voice clone Ana
- `pipeline.py` — orchestrare: text in → audio out
- `server.py` — WebSocket server pentru Unity

### Unity
- **VROfficeManager** — scena principala, spawn agenti
- **AnaAvatar** — controller avatar: animatii, LipSync, IK
- **VoicePipeline** — capture mic, STT, trimite la backend, primeste audio
- **WebSocketClient** — comunicare cu backend Python

## Protocol WebSocket (Unity ↔ Backend)

### Mesaj de la Unity:
```json
{
  "type": "user_message",
  "agent": "ana",
  "text": "Buna Ana, ce ai de facut azi?",
  "session_id": "uuid"
}
```

### Raspuns de la Backend:
```json
{
  "type": "agent_response",
  "agent": "ana",
  "text": "Buna! Am pregatit...",
  "audio_url": "data:audio/mp3;base64,...",
  "emotion": "friendly"
}
```
