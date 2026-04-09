"""Claude API client cu memorie conversatie per sesiune."""
import os
from pathlib import Path
from anthropic import Anthropic

client = Anthropic(api_key=os.environ["ANTHROPIC_API_KEY"])

_ANA_SYSTEM_PROMPT = (
    Path(__file__).parent.parent / "agents" / "ana" / "system_prompt.md"
).read_text(encoding="utf-8")

# session_id -> lista de mesaje
_sessions: dict[str, list[dict]] = {}


def get_response(text: str, session_id: str, agent: str = "ana") -> str:
    """Trimite mesaj la Claude si returneaza raspunsul text."""
    if session_id not in _sessions:
        _sessions[session_id] = []

    _sessions[session_id].append({"role": "user", "content": text})

    response = client.messages.create(
        model="claude-sonnet-4-6",
        max_tokens=256,
        system=_ANA_SYSTEM_PROMPT,
        messages=_sessions[session_id],
    )

    reply = response.content[0].text
    _sessions[session_id].append({"role": "assistant", "content": reply})

    # pastreaza max 20 mesaje per sesiune
    if len(_sessions[session_id]) > 20:
        _sessions[session_id] = _sessions[session_id][-20:]

    return reply


def clear_session(session_id: str) -> None:
    _sessions.pop(session_id, None)
