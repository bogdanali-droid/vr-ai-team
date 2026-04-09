"""
Test pipeline voce fara Unity.
Anthropix API + ElevenLabs TTS (cu fallback gTTS gratuit)

Rulare:
    cd backend
    pip install -r requirements.txt
    python test_pipeline.py

Audio output:
  - ElevenLabs reusit  -> test_output/ana_turn_XX.raw  (Audacity: 16kHz, mono, PCM16 signed)
  - Fallback gTTS      -> test_output/ana_turn_XX.mp3  (orice player)
"""
import asyncio
import os
import sys
from pathlib import Path
from dotenv import load_dotenv

load_dotenv()

# Verifica doar cheia Anthropic (ElevenLabs e optionala)
missing = [k for k in ["ANTHROPIC_API_KEY"] if not os.getenv(k)]
if missing:
    print(f"[EROARE] Chei lipsa in .env: {', '.join(missing)}")
    sys.exit(1)

from claude_client import get_response


def _tts_elevenlabs(text: str, voice_id: str) -> bytes | None:
    """Incearca ElevenLabs. Returneaza bytes PCM16 sau None la eroare."""
    try:
        from elevenlabs_client import text_to_speech
        return text_to_speech(text, voice_id=voice_id)
    except Exception as e:
        print(f"     [ElevenLabs eroare: {e}]")
        return None


def _tts_gtts(text: str, out_path: Path) -> bool:
    """Fallback gratuit cu Google TTS. Salveaza MP3. Returneaza True la succes."""
    try:
        from gtts import gTTS
        tts = gTTS(text=text, lang="ro", slow=False)
        mp3_path = out_path.with_suffix(".mp3")
        tts.save(str(mp3_path))
        print(f"     [Audio gTTS: {mp3_path} — deschide cu orice player]")
        return True
    except ImportError:
        print("     [gTTS nu e instalat: pip install gtts]")
        return False
    except Exception as e:
        print(f"     [gTTS eroare: {e}]")
        return False


async def interactive_test():
    session_id = "test-session-001"
    output_dir = Path("test_output")
    output_dir.mkdir(exist_ok=True)

    voice_id = os.getenv("ANA_VOICE_ID", "")
    use_elevenlabs = bool(os.getenv("ELEVENLABS_API_KEY") and voice_id)

    tts_mode = "ElevenLabs + gTTS fallback" if use_elevenlabs else "gTTS (gratuit)"
    print(f"\n=== VR AI Team — Test Ana [Anthropic + {tts_mode}] ===")
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

        # Audio
        raw_path = output_dir / f"ana_turn_{turn:02d}.raw"
        saved = False

        if use_elevenlabs:
            audio_bytes = _tts_elevenlabs(reply, voice_id)
            if audio_bytes:
                raw_path.write_bytes(audio_bytes)
                print(f"     [Audio ElevenLabs: {raw_path} — Audacity: 16kHz, mono, PCM16 signed]")
                saved = True

        if not saved:
            _tts_gtts(reply, raw_path)

        turn += 1
        print()


if __name__ == "__main__":
    asyncio.run(interactive_test())
