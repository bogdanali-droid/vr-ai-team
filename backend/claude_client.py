"""
Client Ollama — inlocuitor local gratuit pentru Anthropic API.
Foloseste llama3.2 rulat local cu Ollama.

Trecere la Anthropic: schimba LLM_PROVIDER=anthropic in .env
"""
import os
import requests
from agents_registry import get_agent

# sessions: { session_id -> { agent_id -> [mesaje] } }
_sessions: dict[str, dict[str, list[dict]]] = {}

LLM_PROVIDER = os.getenv("LLM_PROVIDER", "ollama")  # "ollama" sau "anthropic"
OLLAMA_URL   = os.getenv("OLLAMA_URL", "http://localhost:11434")
OLLAMA_MODEL = os.getenv("OLLAMA_MODEL", "llama3.2")


def get_response(text: str, session_id: str, agent_id: str = "ana") -> str:
    if LLM_PROVIDER == "anthropic":
        return _anthropic_response(text, session_id, agent_id)
    return _ollama_response(text, session_id, agent_id)


def _ollama_response(text: str, session_id: str, agent_id: str) -> str:
    agent = get_agent(agent_id)

    if session_id not in _sessions:
        _sessions[session_id] = {}
    if agent_id not in _sessions[session_id]:
        _sessions[session_id][agent_id] = []

    history = _sessions[session_id][agent_id]
    history.append({"role": "user", "content": text})

    # Ollama chat API
    messages = [{"role": "system", "content": agent.system_prompt}] + history

    try:
        resp = requests.post(
            f"{OLLAMA_URL}/api/chat",
            json={"model": OLLAMA_MODEL, "messages": messages, "stream": False},
            timeout=60,
        )
        resp.raise_for_status()
        reply = resp.json()["message"]["content"]
    except Exception as e:
        reply = f"[Eroare Ollama: {e}]"

    history.append({"role": "assistant", "content": reply})
    if len(history) > 20:
        _sessions[session_id][agent_id] = history[-20:]

    return reply


def _anthropic_response(text: str, session_id: str, agent_id: str) -> str:
    from anthropic import Anthropic
    client = Anthropic(api_key=os.environ["ANTHROPIC_API_KEY"])
    agent  = get_agent(agent_id)

    if session_id not in _sessions:
        _sessions[session_id] = {}
    if agent_id not in _sessions[session_id]:
        _sessions[session_id][agent_id] = []

    history = _sessions[session_id][agent_id]
    history.append({"role": "user", "content": text})

    response = client.messages.create(
        model=agent.model,
        max_tokens=256,
        system=agent.system_prompt,
        messages=history,
    )
    reply = response.content[0].text
    history.append({"role": "assistant", "content": reply})

    if len(history) > 20:
        _sessions[session_id][agent_id] = history[-20:]

    return reply


def clear_session(session_id: str) -> None:
    _sessions.pop(session_id, None)
