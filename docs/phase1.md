# Faza 1 — Prototip: Ana + Pipeline Voce Complet

## Obiectiv

Prima versiune funcțională: utilizatorul poate vorbi cu **Ana** în VR, iar Ana răspunde cu voce și animație sincronizată pe buze.

## Scope Faza 1

### ✅ Inclus
- [ ] 1 avatar Ana (model de baza, rigged)
- [ ] Captura microfon Meta Quest 3
- [ ] STT → text (Whisper)
- [ ] Text → Claude API cu system prompt Ana
- [ ] Text răspuns → ElevenLabs TTS (vocea Anei)
- [ ] Audio → Oculus LipSync (animatie buze)
- [ ] WebSocket backend ↔ Unity
- [ ] Mediu minimal: o incapere simpla, 1 masa, 1 scaun

### ❌ Exclus din Faza 1
- Ceilalti agenti (@alina, @cosmin, @ion, @gogu)
- Navigare birou VR
- Multi-agent conversations
- Expresii faciale avansate
- Full body IK

## Criteriu de succes

> Utilizatorul pune ochelarii, vede pe Ana la birou, îi spune ceva,
> și Ana răspunde în max 3 secunde cu vocea ei și mișcarea buzelor sincronizată.

## Task-uri tehnice

### Backend
1. Setup server WebSocket Python
2. Integrare Claude API cu system prompt Ana
3. Integrare ElevenLabs TTS streaming
4. Pipeline: text in → audio base64 out

### Unity
1. Import Meta XR SDK + setup scena
2. Import avatar Ana + rig
3. Integrare Oculus LipSync SDK
4. Implementare VoicePipeline (mic → STT → WebSocket)
5. Implementare AudioPlayer cu LipSync trigger

## Latenta tinta

| Pas | Target |
|---|---|
| STT (Whisper) | < 800ms |
| Claude API | < 1000ms |
| ElevenLabs TTS | < 700ms |
| **Total** | **< 2.5s** |
