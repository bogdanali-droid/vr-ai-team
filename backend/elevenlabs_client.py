"""ElevenLabs TTS — returneaza PCM16 raw bytes (fara header WAV).

Initializare lazy: clientul se creeaza la primul apel, nu la import.
Daca ELEVENLABS_API_KEY lipseste, returneaza bytes gol si logheaza warning.
"""
import os

_client = None

ANA_VOICE_ID = ""

_VOICE_SETTINGS = {
    "stability": 0.6,
    "similarity_boost": 0.85,
    "style": 0.4,
    "use_speaker_boost": True,
}


def _get_client():
    global _client
    if _client is None:
        api_key = os.environ.get("ELEVENLABS_API_KEY", "")
        if not api_key:
            raise RuntimeError("ELEVENLABS_API_KEY lipsa din .env")
        from elevenlabs.client import ElevenLabs
        _client = ElevenLabs(api_key=api_key)
    return _client


def text_to_speech(text: str, voice_id: str = "") -> bytes:
    """Converts text to PCM16 audio bytes (16kHz, mono, no WAV header).
    Returneaza bytes gol daca ElevenLabs nu e disponibil.
    """
    if not voice_id:
        voice_id = os.environ.get("ANA_VOICE_ID", "")
    if not voice_id:
        return b""

    try:
        client = _get_client()
        audio_generator = client.text_to_speech.convert(
            voice_id=voice_id,
            text=text,
            model_id="eleven_multilingual_v2",
            output_format="pcm_16000",
            voice_settings=_VOICE_SETTINGS,
        )
        return b"".join(audio_generator)
    except Exception as e:
        print(f"[ElevenLabs warning] {e}")
        return b""
