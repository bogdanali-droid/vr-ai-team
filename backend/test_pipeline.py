"""
Test pipeline voce fara Unity.
Scrie un mesaj in terminal, primesti raspunsul Anei + salvezi audio.

Rulare:
    cd backend
    pip install -r requirements.txt
    cp .env.example .env   # completeaza cheile
    python test_pipeline.py
"""
import asyncio
import base64
import os
import sys
from pathlib import Path
from dotenv import load_dotenv

load_dotenv()

# Verifica cheile necesare
REQUIRED = ["ANTHROPIC_API_KEY", "ELEVENLABS_API_KEY", "ANA_VOICE_ID"]
missing = [k for k in REQUIRED if not os.getenv(k)]
if missing:
    print(f"[EROARE] Chei lipsa in .env: {', '.join(missing)}")
    sys.exit(1)

from pipeline import process_message


async def interactive_test():
    session_id = "test-session-001"
    output_dir = Path("test_output")
    output_dir.mkdir(exist_ok=True)

    print("\n=== VR AI Team — Test Pipeline Ana ===")
    print("Scrie un mesaj si apasa Enter. (Ctrl+C pentru iesire)\n")

    turn = 0
    while True:
        try:
            user_input = input("Tu: ").strip()
        except (KeyboardInterrupt, EOFError):
            print("\nTest incheiat.")
            break

        if not user_input:
            continue

        print("Ana: [procesare...]", end="\r")

        result = process_message(user_input, session_id)

        print(f"Ana: {result['text']}")

        # Salveaza audio PCM16 ca fisier .raw (redabil cu Audacity: 16kHz, mono, PCM16)
        if result.get("audio_b64"):
            audio_bytes = base64.b64decode(result["audio_b64"])
            out_path = output_dir / f"ana_turn_{turn:02d}.raw"
            out_path.write_bytes(audio_bytes)
            print(f"     [Audio salvat: {out_path} — deschide in Audacity: 16kHz, mono, PCM16 signed]")

        turn += 1
        print()


if __name__ == "__main__":
    asyncio.run(interactive_test())
