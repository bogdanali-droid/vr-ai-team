# Setup Avatar VRoid Studio → Unity → Quest 3

## Pas 1 — Descarcă și instalează VRoid Studio

1. Mergi la: **vroid.com/en/studio**
2. Click **Download** → alege Windows sau Mac
3. Instalează și deschide aplicația

---

## Pas 2 — Creează avatarele în VRoid Studio

### Setări generale (valabile pentru toți)
După ce deschizi VRoid Studio:
- **New** → **Create New Avatar**
- Alege **Female** sau **Male** conform tabelului de mai jos

### Per personaj:

| Agent | Sex VRoid | Păr — Culoare | Ochi | Outfit |
|---|---|---|---|---|
| **Ana** | Female | Blond `#D4A843`, lung ondulat | Verde `#4A7C59` | Office (sacou) |
| **Alina** | Female | Brunet `#2C1810`, prins sus | Căprui `#8B5E3C` | Formal (sacou navy) |
| **Cosmin** | Male | Brunet `#5C3D2E`, scurt | Albastru `#6B8CAE` | Casual (hoodie gri) |
| **Ion** | Male | Negru `#0A0A0A`, fade scurt | Căprui `#3D1C02` | Smart casual |
| **Gogu** | Male | Castaniu `#7D6147`, scurt | Verde `#6B7C3F` | Creativ (jacket colorat) |
| **Victor** | Male | Blond-cenușiu `#B8A89A`, scurt | Gri-albastru `#8FA8B8` | Tech (tricou negru) |

### Setări importante în VRoid (pentru toate):
- **Face** → ajustează forma feței
- **Hair** → culoarea și stilul de mai sus
- **Eyes** → culoarea de mai sus
- **Body** → înălțime conform fișelor

---

## Pas 3 — Export din VRoid Studio

Pentru **fiecare** avatar:
1. Click **Camera/Export** (iconița cameră sus-dreapta)
2. **Export as VRM**
3. Completează:
   ```
   Title:    Ana (sau numele agentului)
   Author:   VR AI Team
   Version:  1.0
   ```
4. **Export** → salvează ca:
   ```
   ana_avatar.vrm
   alina_avatar.vrm
   ... etc
   ```

---

## Pas 4 — Import UniVRM în Unity

UniVRM = pachetul care citește fișiere `.vrm` în Unity.

1. Mergi la: **github.com/vrm-c/UniVRM/releases**
2. Descarcă cel mai recent: `UniVRM-X.XXX.X_xxxx.unitypackage`
3. În Unity: **Assets → Import Package → Custom Package**
4. Selectează fișierul descărcat → **Import All**

> ✅ Verificare: în Project panel ar trebui să apară folderul `VRM/`

---

## Pas 5 — Import avatar VRM în Unity

1. Copiază fișierele `.vrm` în:
   ```
   Assets/VROffice/Avatars/Ana/ana_avatar.vrm
   Assets/VROffice/Avatars/Alina/alina_avatar.vrm
   ... etc
   ```
2. Unity le va procesa automat → creează un **Prefab**
3. Drag & drop prefab-ul `ana_avatar` în scenă pe GameObject-ul **Ana**
4. Poziție: `(0, 0, 1.8)`, Rotație: `(0, 180, 0)`

---

## Pas 6 — Configurare LipSync (1 click)

1. Selectează GameObject-ul `Ana` în Hierarchy
2. Adaugă manual componentele (sau folosește Inspector):
   - `OVRLipSyncContext` → `audioSource` = AudioSource de pe Ana
   - `VRMLipSyncBridge` → `blendShapeProxy` = VRMBlendShapeProxy de pe prefab
3. Sau rulează: **VROffice → Setup Ana RPM Avatar**
   *(funcționează și pentru VRM dacă are blendshapes A/I/U/E/O)*

> `VRMLipSyncBridge.cs` face bridge automat între OVRLipSync și VRM.
> Viseme-urile Oculus (15) sunt mapate la blendshapes VRM (A, I, U, E, O).

---

## Structura finală Assets

```
Assets/VROffice/Avatars/
├── Ana/
│   └── ana_avatar.vrm      (+ prefab generat automat)
├── Alina/
│   └── alina_avatar.vrm
├── Cosmin/
│   └── cosmin_avatar.vrm
├── Ion/
│   └── ion_avatar.vrm
├── Gogu/
│   └── gogu_avatar.vrm
└── Victor/
    └── victor_avatar.vrm
```

---

## Troubleshooting

| Problemă | Soluție |
|---|---|
| `.vrm` nu se importă | Verifică că UniVRM e instalat corect |
| Buzele nu mișcă | Verifică `VRMLipSyncBridge` → `blendShapeProxy` asignat |
| Avatar în T-pose | Normal la import — animațiile se aplică prin Animator |
| Avatar prea mic/mare | Scale pe GameObject: `(1, 1, 1)` sau ajustează în VRoid |
| Culori arată diferit | VRM folosește MToon shader — normal, e stilul VRoid |
