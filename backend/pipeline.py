"""
Pipeline principal: text input -> raspuns agent + audio bytes.
Audio: ElevenLabs (daca disponibil) -> Edge TTS neural -> gTTS fallback.
"""
import asyncio
import base64
import io
from claude_client import get_response
from elevenlabs_client import text_to_speech
from delegation_engine import route_message
from agents_registry import get_agent, AGENTS

# Voci Edge TTS per agent (ro-RO neural, gratuit)
EDGE_VOICES = {
    "ana":    "ro-RO-AlinaNeural",
    "alina":  "ro-RO-AlinaNeural",
    "cosmin": "ro-RO-EmilNeural",
    "ion":    "ro-RO-EmilNeural",
    "gogu":   "ro-RO-EmilNeural",
    "victor": "ro-RO-EmilNeural",
}


def _edge_tts_to_pcm16(text: str, voice: str) -> bytes:
    """Edge TTS (Microsoft neural) -> PCM16 raw bytes (16kHz, mono)."""
    try:
        import edge_tts
        from pydub import AudioSegment

        async def _synthesize():
            communicate = edge_tts.Communicate(text, voice)
            mp3_buf = io.BytesIO()
            async for chunk in communicate.stream():
                if chunk["type"] == "audio":
                    mp3_buf.write(chunk["data"])
            return mp3_buf.getvalue()

        mp3_bytes = asyncio.run(_synthesize())
        if not mp3_bytes:
            return b""

        audio = AudioSegment.from_mp3(io.BytesIO(mp3_bytes))
        audio = audio.set_frame_rate(16000).set_channels(1).set_sample_width(2)
        return audio.raw_data
    except Exception as e:
        print(f"[Edge TTS] Eroare: {e}")
        return b""


def _gtts_to_pcm16(text: str) -> bytes:
    """Fallback gTTS -> PCM16."""
    try:
        from gtts import gTTS
        from pydub import AudioSegment
        mp3_buf = io.BytesIO()
        gTTS(text=text, lang="ro", slow=False).write_to_fp(mp3_buf)
        mp3_buf.seek(0)
        audio = AudioSegment.from_mp3(mp3_buf)
        audio = audio.set_frame_rate(16000).set_channels(1).set_sample_width(2)
        return audio.raw_data
    except Exception as e:
        print(f"[gTTS] Eroare: {e}")
        return b""


def _get_audio(text: str, agent_id: str, voice_id: str) -> bytes:
    """ElevenLabs -> Edge TTS neural -> gTTS fallback."""
    # 1. ElevenLabs (daca e configurat)
    if voice_id:
        pcm = text_to_speech(text, voice_id=voice_id)
        if pcm:
            return pcm

    # 2. Edge TTS neural (gratuit, voce naturala)
    edge_voice = EDGE_VOICES.get(agent_id, "ro-RO-AlinaNeural")
    print(f"[Pipeline] Folosesc Edge TTS: {edge_voice}")
    pcm = _edge_tts_to_pcm16(text, edge_voice)
    if pcm:
        return pcm

    # 3. Fallback gTTS
    print("[Pipeline] Fallback gTTS...")
    return _gtts_to_pcm16(text)


def process_message(
    text: str,
    session_id: str,
    agent_id: str = "ana",
    *,
    auto_delegate: bool = True,
) -> dict:
    original_agent = agent_id
    active_agent   = agent_id

    if auto_delegate and agent_id == "ana":
        routed = route_message(text)
        if routed != "ana":
            active_agent = routed

    agent_config = get_agent(active_agent)
    reply_text   = get_response(text, session_id, active_agent)

    audio_bytes = _get_audio(reply_text, active_agent, agent_config.voice_id)
    audio_b64   = base64.b64encode(audio_bytes).decode("utf-8") if audio_bytes else ""

    return {
        "text":        reply_text,
        "audio_b64":   audio_b64,
        "agent":       active_agent,
        "agent_name":  agent_config.name,
        "animations":  agent_config.animations,
        "routed_from": original_agent if original_agent != active_agent else "",
    }


def get_agents_list() -> list[dict]:
    return [
        {"id": a.id, "name": a.name, "role": a.role}
        for a in AGENTS.values()
    ]
