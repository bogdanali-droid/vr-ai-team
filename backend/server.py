"""WebSocket server — Unity trimite text, primeste raspuns + audio. Multi-agent."""
import asyncio
import json
import uuid
import websockets
from dotenv import load_dotenv
from pipeline import process_message, get_agents_list

load_dotenv()


async def handle_connection(websocket):
    session_id = str(uuid.uuid4())
    print(f"[+] Client conectat — sesiune {session_id}")

    try:
        async for raw in websocket:
            msg = json.loads(raw)
            msg_type = msg.get("type")

            # ── Lista agenti (Unity o cere la start) ─────────────────────── #
            if msg_type == "get_agents":
                await websocket.send(json.dumps({
                    "type": "agents_list",
                    "agents": get_agents_list(),
                }))

            # ── Mesaj utilizator ──────────────────────────────────────────── #
            elif msg_type == "user_message":
                text  = msg.get("text", "").strip()
                agent = msg.get("agent", "ana")

                if not text:
                    continue

                print(f"[{session_id}] User → {agent}: {text}")
                result = process_message(text, session_id, agent_id=agent)

                # Daca Ana a delegat, afiseaza in log
                if result["routed_from"]:
                    print(f"[{session_id}] Ana → {result['agent']}: delegat")

                print(f"[{session_id}] {result['agent_name']}: {result['text']}")

                await websocket.send(json.dumps({
                    "type":        "agent_response",
                    "agent":       result["agent"],
                    "agent_name":  result["agent_name"],
                    "text":        result["text"],
                    "audio_b64":   result["audio_b64"],
                    "animations":  result["animations"],
                    "routed_from": result["routed_from"],
                }))

            elif msg_type == "end_session":
                break

    except websockets.exceptions.ConnectionClosed:
        pass
    finally:
        print(f"[-] Client deconectat — sesiune {session_id}")


async def main():
    import os
    host = os.getenv("HOST", "0.0.0.0")
    port = int(os.getenv("PORT", 8765))
    print(f"VR AI Team Backend — ws://{host}:{port}")
    async with websockets.serve(handle_connection, host, port):
        await asyncio.Future()


if __name__ == "__main__":
    asyncio.run(main())
