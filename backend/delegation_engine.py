"""
Motor de delegare — Ana decide catre ce agent se trimite cererea.
Foloseste Claude sa clasifice intentul si sa ruteze corect.
"""
from __future__ import annotations
import os
import re
from anthropic import Anthropic

_client = Anthropic(api_key=os.environ["ANTHROPIC_API_KEY"])

# Keywords rapide pentru routing fara API call (performanta)
_FAST_ROUTING: dict[str, list[str]] = {
    "victor": [
        "unity", "vr", "quest", "avatar", "lipsync", "lip sync", "oculus",
        "shader", "fps", "frame", "xr", "animati", "prefab", "scene", "scena",
        "meta sdk", "build", "apk", "sidequest",
    ],
    "cosmin": [
        "backend", "server", "python", "api", "websocket", "cloudflare",
        "deploy", "docker", "database", "eroare server", "endpoint",
        "elevenlabs", "whisper", "claude api", "requirements",
    ],
    "alina": [
        "plan", "planning", "task", "milestone", "sprint", "deadline",
        "termen", "timeline", "scrum", "agile", "backlog", "priorit",
        "blocat", "progres", "raport saptamanal",
    ],
    "ion": [
        "post", "instagram", "facebook", "tiktok", "linkedin", "content",
        "copywriting", "caption", "hashtag", "reel", "social media",
        "campanie", "engagement",
    ],
    "gogu": [
        "ads", "google ads", "meta ads", "seo", "marketing", "brand",
        "campanie", "roi", "kpi", "reach", "conversii", "funnel",
        "strategie marketing", "promovare",
    ],
}

_ANA_ROUTING_PROMPT = """Ești Ana, Managerul de Proiecte. Primești un mesaj de la Bogdan.
Decide DOAR dacă îl gestionezi tu sau îl delegi unui coleg.

Colegii disponibili:
- victor: VR/Unity/avatare/LipSync/Meta XR
- cosmin: backend Python/WebSocket/API/deployment
- alina: planificare/task-uri/timelines/blocaje
- ion: social media/content/copywriting/Instagram/TikTok
- gogu: marketing digital/ads/SEO/strategie brand
- ana: coordonare generală, salut, întrebări despre echipă, altele

Răspunde cu UN SINGUR cuvânt: numele agentului (ana/victor/cosmin/alina/ion/gogu).
Mesaj: {message}"""


def route_message(text: str) -> str:
    """
    Determina agentul caruia i se trimite mesajul.
    Returneaza agent_id (str).

    1. Incearca routing rapid prin keywords
    2. Daca nu e clar, intreaba Claude (Ana decide)
    """
    text_lower = text.lower()

    # 1. Verificare mentiune directa (@agent)
    for agent_id in ["victor", "cosmin", "alina", "ion", "gogu", "ana"]:
        if f"@{agent_id}" in text_lower:
            return agent_id

    # 2. Keyword routing rapid
    scores: dict[str, int] = {k: 0 for k in _FAST_ROUTING}
    for agent_id, keywords in _FAST_ROUTING.items():
        for kw in keywords:
            if kw in text_lower:
                scores[agent_id] += 1

    best = max(scores, key=scores.get)  # type: ignore
    if scores[best] >= 2:
        return best
    if scores[best] == 1:
        # Un singur keyword — suficient pentru rutare
        return best

    # 3. Fallback: Ana decide via Claude (input scurt, fara memorie)
    try:
        response = _client.messages.create(
            model="claude-haiku-4-5-20251001",  # cel mai rapid model
            max_tokens=10,
            messages=[{
                "role": "user",
                "content": _ANA_ROUTING_PROMPT.format(message=text[:300]),
            }],
        )
        agent_id = response.content[0].text.strip().lower()
        # Valideaza raspunsul
        if agent_id in _FAST_ROUTING or agent_id == "ana":
            return agent_id
    except Exception:
        pass

    return "ana"  # default
