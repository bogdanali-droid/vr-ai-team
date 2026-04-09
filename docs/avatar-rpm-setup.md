# Setup Avatar Ana — Ready Player Me + Oculus LipSync

## Pas 1 — Creeaza avatarul pe readyplayer.me

1. Mergi la **readyplayer.me**
2. Alege **Full Body avatar**
3. Personalizeaza dupa fisa Anei:
   - Par: **blond lung**
   - Ochi: **verzi**
   - Outfit: **office** (sacou, bluza)
   - Inaltime: **medie-inalta**
4. Click **Export** si alege setarile:

```
Format:          GLB
Pose:            T-Pose        ← IMPORTANT pentru animatii
Texture Atlas:   512 (Quest optimizat)
Morph Targets:   ARKit + Oculus Visemes   ← IMPORTANT pentru LipSync
```

5. Descarca fisierul `ana_avatar.glb`

---

## Pas 2 — Import in Unity

1. Copiaza `ana_avatar.glb` in:
   ```
   Assets/VROffice/Avatars/Ana/ana_avatar.glb
   ```
2. In **Import Settings** (Inspector):
   - Animation Type: **Humanoid**
   - Avatar Definition: **Create From This Model**
   - Click **Configure...** si verifica ca oasele sunt mapate corect
   - **Apply**

---

## Pas 3 — Plaseaza in scena

1. Drag & drop `ana_avatar` din Project in scena pe GameObject-ul **Ana**
   (sau inlocuieste Capsule placeholder-ul)
2. Pozitioneaza: `(0, 0, 1.8)`, rotatie `(0, 180, 0)` — fata spre utilizator

---

## Pas 4 — Auto-Setup LipSync (1 click)

1. **Selecteaza** GameObject-ul `Ana` in Hierarchy
2. Menu Unity: **VROffice > Setup Ana RPM Avatar**

Scriptul face automat:
- Detecteaza mesh-ul cu blendshapes viseme
- Adauga `OVRLipSyncContext` + `OVRLipSyncContextMorphTarget`
- Mapeaza toti cei **15 viseme** RPM → Oculus LipSync
- Configureaza `AudioSource` pentru 3D spatial
- Leaga referintele in `AnaAvatarController`

> Daca apar avertismente despre viseme lipsa → verifica la export RPM ca ai bifat **Oculus Visemes**

---

## Pas 5 — Animator Controller

1. Menu: **VROffice > Create Ana Animator Controller**
2. In `AnaAvatarController` (Inspector) → asigneaza `AnaAnimator.controller`
3. Adauga animatii pe fiecare stare:
   - **Idle** → animatie idle birou (Mixamo: `Idle`)
   - **Talking** → animatie vorbire cu maini (Mixamo: `Talking`)
   - **Listening** → animatie ascultare (Mixamo: `Listening`)

> Animatii gratuite Mixamo: **mixamo.com** → cauta "Idle", "Talking", "Listening"
> Retarget pe avatarul Ana in Unity (Humanoid rig)

---

## Pas 6 — Test final

1. Porneste backend-ul:
   ```bash
   cd backend && python server.py
   ```
2. In Unity: seteaza `WebSocketClient.serverUrl = "ws://[IP-PC]:8765"`
3. **Play** in Editor (sau builda pe Quest)
4. Apasa trigger dreapta → vorbeste → Ana trebuie sa raspunda cu voce + LipSync

---

## Troubleshooting

| Problema | Solutie |
|---|---|
| Buzele nu misca | Verifica OVRLipSync SDK instalat + viseme mapate |
| Audio fara voce | Verifica `ANA_VOICE_ID` in `.env` |
| Avatar T-pose | Verifica Animation Type = Humanoid in import settings |
| WebSocket eroare | Verifica IP-ul PC-ului si ca backend ruleaza |
| Latenta mare (>3s) | Verifica conexiunea WiFi Quest — acelasi router cu PC-ul |
