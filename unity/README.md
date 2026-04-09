# Unity — Setup VR Office

## Versiune recomandata
- **Unity 2022.3 LTS** (sau 2023.2+)
- **Meta XR SDK** — via Unity Package Manager
- **Oculus LipSync SDK** — import manual din [Oculus Developer](https://developer.oculus.com/downloads/package/oculus-lipsync-unity/)

## Package Manager — dependinte

```json
{
  "com.meta.xr.sdk.all": "60.0.0",
  "com.unity.textmeshpro": "3.0.6",
  "com.unity.nuget.newtonsoft-json": "3.2.1"
}
```

## Structura Assets recomandata

```
Assets/
└── VROffice/
    ├── Scenes/
    │   └── VROffice_Phase1.unity
    ├── Scripts/
    │   ├── VROfficeManager.cs
    │   ├── AnaAvatarController.cs
    │   ├── VoicePipeline.cs
    │   └── WebSocketClient.cs
    ├── Avatars/
    │   └── Ana/
    │       ├── Ana_Model.fbx
    │       └── Ana_Animations/
    ├── Audio/
    └── Materials/
```

## Setup scena (Faza 1)

1. Importa **Meta XR All-in-One SDK** din Package Manager
2. Creeaza scena noua → adauga **OVRCameraRig**
3. Importa avatarul Anei (Ready Player Me sau custom)
4. Adauga **OVRLipSync** component pe avatarul Anei
5. Adauga script **VoicePipeline** pe un GameObject gol
6. Configureaza WebSocket URL: `ws://[IP-PC]:8765`

## WebSocket Client (Unity → Backend)

Unity trimite:
```json
{"type": "user_message", "agent": "ana", "text": "Buna Ana!"}
```

Unity primeste:
```json
{"type": "agent_response", "agent": "ana", "text": "...", "audio_b64": "..."}
```

Audio-ul se decode din base64 → `AudioClip` → redat cu `AudioSource` + LipSync.
