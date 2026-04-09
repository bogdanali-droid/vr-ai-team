"""
Pipeline principal: text input -> raspuns agent + audio bytes.
Suporta multi-agent cu delegare automata prin Ana.
"""
import base64
from claude_client import get_response
from elevenlabs_client import text_to_speech
from delegation_engine import route_message
from agents_registry import get_agent, AGENTS


def process_message(
    text: str,
    session_id: str,
    agent_id: str = "ana",
    *,
    auto_delegate: bool = True,
) -> dict:
    """
    Preia mesajul, roteaza la agentul potrivit (daca auto_delegate=True),
    obtine raspuns Claude, genereaza audio ElevenLabs.

    Returns:
        {
            "text": str,
            "audio_b64": str,   # PCM16 base64 (16kHz, mono)
            "agent": str,       # agentul care a raspuns efectiv
            "agent_name": str,
            "animations": dict,
            "routed_from": str, # daca a fost delegat de Ana
        }
    """
    original_agent = agent_id
    active_agent   = agent_id

    # Ana roteaza automat catre agentul potrivit
    if auto_delegate and agent_id == "ana":
        routed = route_message(text)
        if routed != "ana":
            active_agent = routed

    agent_config = get_agent(active_agent)

    # Obtine raspuns text de la Claude
    reply_text = get_response(text, session_id, active_agent)

    # Genereaza audio cu vocea agentului
    voice_id    = agent_config.voice_id
    audio_bytes = text_to_speech(reply_text, voice_id=voice_id) if voice_id else b""
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
    """Lista agentilor disponibili (pentru UI Unity — agents panel)."""
    return [
        {"id": a.id, "name": a.name, "role": a.role}
        for a in AGENTS.values()
    ]
