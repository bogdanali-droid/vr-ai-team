"""
Test pipeline voce fara Unity.
Anthropic API + ElevenLabs TTS — salveaza audio .raw

Rulare:
    cd backend
    python test_pipeline.py
"""
import asyncio
import base64
import os
import sys
from pathlib import Path
from dotenv import load_dotenv

load_dotenv()

# Verifica cheile
missing = [k for k in ["ANTHROPIC_API_KEY", "ELEVENLABS_API_KEY", "ANA_VOICE_ID"]
           if not os.getenv(k)]
if missing:
    print(f"[EROARE] Chei lipsa in .env: {', '.join(missing)}")
    sys.exit(1)

from claude_client import get_response
from elevenlabs_client import text_to_speech


async def interactive_test():
    session_id = "test-session-001"
    output_dir = Path("test_output")
    output_dir.mkdir(exist_ok=True)

    print("\n=== VR AI Team — Test Ana [Anthropic + ElevenLabs] ===")
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

        # Text de la Claude
        reply = get_response(user_input, session_id, "ana")
        print(f"Ana: {reply}")

        # Audio de la ElevenLabs
        voice_id = os.getenv("ANA_VOICE_ID", "")
        if voice_id:
            try:
                audio_bytes = text_to_speech(reply, voice_id=voice_id)
                out_path = output_dir / f"ana_turn_{turn:02d}.raw"
                out_path.write_bytes(audio_bytes)
                print(f"     [Audio: {out_path} — Audacity: 16kHz, mono, PCM16 signed]")
            except Exception as e:
                print(f"     [Audio eroare: {e}]")

        turn += 1
        print()


if __name__ == "__main__":
    asyncio.run(interactive_test())
