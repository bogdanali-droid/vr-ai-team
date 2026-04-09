"""Claude API client cu memorie conversatie per sesiune si suport multi-agent."""
import os
from anthropic import Anthropic
from agents_registry import get_agent

_client = Anthropic(api_key=os.environ["ANTHROPIC_API_KEY"])

# session_id -> { agent_id -> [mesaje] }
# Fiecare agent are memoria lui separata per sesiune
_sessions: dict[str, dict[str, list[dict]]] = {}


def get_response(text: str, session_id: str, agent_id: str = "ana") -> str:
    """
    Trimite mesaj la Claude cu system prompt-ul agentului corect.
    Memoria e separata per (session_id, agent_id).
    """
    agent = get_agent(agent_id)

    # Initializeaza memoria sesiunii
    if session_id not in _sessions:
        _sessions[session_id] = {}
    if agent_id not in _sessions[session_id]:
        _sessions[session_id][agent_id] = []

    history = _sessions[session_id][agent_id]
    history.append({"role": "user", "content": text})

    response = _client.messages.create(
        model=agent.model,
        max_tokens=256,
        system=agent.system_prompt,
        messages=history,
    )

    reply = response.content[0].text
    history.append({"role": "assistant", "content": reply})

    # Pastreaza max 20 mesaje per (sesiune, agent)
    if len(history) > 20:
        _sessions[session_id][agent_id] = history[-20:]

    return reply


def clear_session(session_id: str) -> None:
    _sessions.pop(session_id, None)
