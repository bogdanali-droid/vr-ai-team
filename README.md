# VR AI Team — Birou Virtual Meta Quest 3

Birou virtual imersiv pe Meta Quest 3 cu agenți AI ca avatare 3D.

## Stack

| Componenta | Tehnologie |
|---|---|
| VR Runtime | Unity + Meta XR SDK |
| Avatare | Ready Player Me / custom rig |
| LipSync | Oculus LipSync SDK |
| AI Brain | Claude API (Anthropic) |
| Voce TTS | ElevenLabs |
| STT | Whisper (on-device / API) |

## Agenți VR (v1)

| Agent | Rol |
|---|---|
| @ana | Manager Proiecte — lider echipă, interfața principală cu utilizatorul |
| @alina | TBD |
| @cosmin | TBD |
| @ion | TBD |
| @gogu | TBD |

> Fișe complete de caracter: [bogdanali-droid/ai-team/projects/vr-office/characters](https://github.com/bogdanali-droid/ai-team/tree/main/projects/vr-office/characters)

## Faze

- **Faza 1** ✅ In lucru — 1 avatar (Ana) + pipeline voce complet
- **Faza 2** — Toți agenții + navigare birou VR
- **Faza 3** — Colaborare multi-agent în timp real

## Structura repo

```
vr-ai-team/
├── docs/           # Arhitectura, planuri de faze
├── backend/        # Serviciu Python: Claude API + ElevenLabs
├── unity/          # Ghid setup Unity + Meta XR
└── agents/         # Configuratii agenti (system prompt, voce, animatii)
```

## Setup rapid

```bash
cd backend
pip install -r requirements.txt
cp .env.example .env   # adauga cheile API
python main.py
```
