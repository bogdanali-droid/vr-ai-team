"""ElevenLabs TTS — returneaza audio mp3 ca bytes."""
import os
from elevenlabs.client import ElevenLabs

_client = ElevenLabs(api_key=os.environ["ELEVENLABS_API_KEY"])

ANA_VOICE_ID = os.environ.get("ANA_VOICE_ID", "")

_VOICE_SETTINGS = {
    "stability": 0.6,
    "similarity_boost": 0.85,
    "style": 0.4,
    "use_speaker_boost": True,
}


def text_to_speech(text: str, voice_id: str = ANA_VOICE_ID) -> bytes:
    """Converts text to speech and returns raw mp3 bytes."""
    audio_generator = _client.text_to_speech.convert(
        voice_id=voice_id,
        text=text,
        model_id="eleven_multilingual_v2",
        voice_settings=_VOICE_SETTINGS,
    )
    return b"".join(audio_generator)
