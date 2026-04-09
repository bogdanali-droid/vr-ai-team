"""
Test pipeline voce fara Unity.
Ollama (local, gratuit) sau Anthropic (online, $5 credit).

Rulare:
    cd backend
    python test_pipeline.py
"""
import asyncio
import os
import sys
from dotenv import load_dotenv

load_dotenv()

# Cu Ollama nu ai nevoie de chei API
LLM_PROVIDER = os.getenv("LLM_PROVIDER", "ollama")

if LLM_PROVIDER == "anthropic":
    missing = [k for k in ["ANTHROPIC_API_KEY"] if not os.getenv(k)]
    if missing:
        print(f"[EROARE] Chei lipsa in .env: {', '.join(missing)}")
        sys.exit(1)

from claude_client import get_response


async def interactive_test():
    session_id = "test-session-001"

    provider_info = "Ollama (local)" if LLM_PROVIDER == "ollama" else "Anthropic API"
    print(f"\n=== VR AI Team — Test Ana [{provider_info}] ===")
    print("Scrie un mesaj si apasa Enter. (Ctrl+C pentru iesire)\n")
    print("Ana se incarca... (prima rulare poate dura 10-20 sec)\n")

    while True:
        try:
            user_input = input("Tu: ").strip()
        except (KeyboardInterrupt, EOFError):
            print("\nTest incheiat.")
            break

        if not user_input:
            continue

        print("Ana: [procesare...]", end="\r")
        reply = get_response(user_input, session_id, "ana")
        print(f"Ana: {reply}\n")


if __name__ == "__main__":
    asyncio.run(interactive_test())
