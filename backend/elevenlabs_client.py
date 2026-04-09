"""ElevenLabs TTS — returneaza PCM16 raw bytes (fara header WAV).

Configurat cu output_format='pcm_16000' pentru compatibilitate directa
cu AudioUtils.Base64PcmToAudioClip() din Unity (fara decoder MP3 necesar).
"""
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
    """Converts text to PCM16 audio bytes (16kHz, mono, no WAV header)."""
    audio_generator = _client.text_to_speech.convert(
        voice_id=voice_id,
        text=text,
        model_id="eleven_multilingual_v2",
        output_format="pcm_16000",   # PCM raw — compatibil direct cu Unity
        voice_settings=_VOICE_SETTINGS,
    )
    return b"".join(audio_generator)
