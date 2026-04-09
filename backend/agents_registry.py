"""
Registry agenti VR — incarca config si system prompt per agent.
Fisele de caracter sunt in bogdanali-droid/ai-team/projects/vr-office/characters/
"""
from __future__ import annotations
import json
from pathlib import Path
from dataclasses import dataclass, field

# Calea catre fisele de caracter (cand rulezi local, cloneaza ai-team in parinte)
_CHARS_DIR = Path(__file__).parent.parent.parent / "ai-team" / "projects" / "vr-office" / "characters"

# Fallback system prompt daca fisierul nu e gasit
_FALLBACK_PROMPT = (
    "Ești un agent AI în biroul virtual VR al lui Bogdan. "
    "Răspunzi în română, scurt și clar. Ești în VR, nu în chat."
)


@dataclass
class AgentConfig:
    id: str
    name: str
    role: str
    voice_id: str = ""
    model: str = "claude-sonnet-4-6"
    system_prompt: str = ""
    animations: dict = field(default_factory=dict)


# ── Configuratii statice ────────────────────────────────────────────────── #

AGENTS: dict[str, AgentConfig] = {
    "ana": AgentConfig(
        id="ana",
        name="Ana",
        role="Manager de Proiecte",
        model="claude-sonnet-4-6",
        animations={
            "idle": "stand_desk_idle",
            "talking": "talk_gestures_natural",
            "listening": "turn_to_user_attentive",
        },
    ),
    "alina": AgentConfig(
        id="alina",
        name="Alina",
        role="Manager de Proiect & Coordonator Planificare",
        model="claude-sonnet-4-6",
        animations={
            "idle": "sit_tablet_browse",
            "talking": "talk_point_list",
            "listening": "sit_attentive",
        },
    ),
    "cosmin": AgentConfig(
        id="cosmin",
        name="Cosmin",
        role="Backend Developer & DevOps",
        model="claude-sonnet-4-6",
        animations={
            "idle": "lean_desk_screen",
            "talking": "talk_casual_shrug",
            "listening": "lean_listen",
        },
    ),
    "ion": AgentConfig(
        id="ion",
        name="Ion",
        role="Social Media & Marketing Specialist",
        model="claude-sonnet-4-6",
        animations={
            "idle": "sit_phone_scroll",
            "talking": "talk_energetic_gestures",
            "listening": "lean_forward_excited",
        },
    ),
    "gogu": AgentConfig(
        id="gogu",
        name="Gogu",
        role="Expert Marketing Digital",
        model="claude-sonnet-4-6",
        animations={
            "idle": "sit_feet_desk_graphs",
            "talking": "talk_draw_funnel",
            "listening": "sit_arms_crossed_thinking",
        },
    ),
    "victor": AgentConfig(
        id="victor",
        name="Victor",
        role="VR/XR Developer",
        model="claude-opus-4-6",  # Victor foloseste Opus — cel mai tehnic agent
        animations={
            "idle": "stand_hologram_code",
            "talking": "talk_technical_point",
            "listening": "stand_arms_crossed_calm",
        },
    ),
}


def get_agent(agent_id: str) -> AgentConfig:
    """Returneaza config-ul agentului si incarca system prompt-ul."""
    agent = AGENTS.get(agent_id.lower())
    if agent is None:
        raise ValueError(f"Agent necunoscut: {agent_id}. Disponibili: {list(AGENTS.keys())}")

    if not agent.system_prompt:
        agent.system_prompt = _load_system_prompt(agent_id)

    return agent


def _load_system_prompt(agent_id: str) -> str:
    """Incarca system prompt-ul din fisa de caracter VR."""
    char_file = _CHARS_DIR / f"{agent_id}.md"

    if char_file.exists():
        content = char_file.read_text(encoding="utf-8")
        # Extrage sectiunea "System Prompt" daca exista, altfel foloseste tot fisierul
        if "## System Prompt" in content:
            section = content.split("## System Prompt")[-1].strip()
            # Sterge subsectiuni urmatoare daca exista
            if "\n## " in section:
                section = section.split("\n## ")[0].strip()
            # Curata blocul de cod markdown daca e intr-un ```
            section = section.strip("`").strip()
            return section
        return content  # Foloseste toata fisa ca system prompt

    # Fallback inline per agent
    fallbacks = {
        "ana":    "Ești Ana, Managerul de Proiecte. Coordonezi echipa și ești interfața principală cu Bogdan. Răspunsuri scurte, în română.",
        "alina":  "Ești Alina, specialista în project management. Dai task-uri cu obiectiv, responsabil și termen. Răspunsuri scurte, în română.",
        "cosmin": "Ești Cosmin, backend developer-ul echipei. Ești relaxat și tehnic. Răspunsuri scurte, în română.",
        "ion":    "Ești Ion, specialistul de social media. Ești energic și dai idei concrete. Răspunsuri scurte, în română.",
        "gogu":   "Ești Gogu, expertul în marketing digital. Ești creativ și bazat pe date. Răspunsuri scurte, în română.",
        "victor": "Ești Victor, VR developer-ul echipei. Ești tehnic și precis. Răspunsuri scurte, în română.",
    }
    return fallbacks.get(agent_id, _FALLBACK_PROMPT)
