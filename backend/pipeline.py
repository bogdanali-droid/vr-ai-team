"""Pipeline principal: text input -> raspuns text + audio bytes."""
import base64
from claude_client import get_response
from elevenlabs_client import text_to_speech


def process_message(text: str, session_id: str, agent: str = "ana") -> dict:
    """
    Preia mesajul utilizatorului, obtine raspuns Claude,
    genereaza audio ElevenLabs.

    Returns:
        {
            "text": str,
            "audio_b64": str,  # mp3 encodat base64
            "agent": str,
        }
    """
    reply_text = get_response(text, session_id, agent)
    audio_bytes = text_to_speech(reply_text)
    audio_b64 = base64.b64encode(audio_bytes).decode("utf-8")

    return {
        "text": reply_text,
        "audio_b64": audio_b64,
        "agent": agent,
    }
