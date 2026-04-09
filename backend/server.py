"""WebSocket + HTTP server (FastAPI).

  ws://HOST:PORT/        — Unity WebSocket
  POST /transcribe       — Whisper STT local (audio → text)
"""
import json
import os
import tempfile
import uuid
from dotenv import load_dotenv
load_dotenv()  # TREBUIE inainte de orice import local

from fastapi import FastAPI, WebSocket, WebSocketDisconnect, UploadFile, File, Form
from fastapi.responses import JSONResponse
import uvicorn

from pipeline import process_message, get_agents_list

app = FastAPI()

# ── Whisper local ──────────────────────────────────────────── #
_whisper_model = None

def _get_whisper():
    global _whisper_model
    if _whisper_model is None:
        print("[Whisper] Se incarca modelul 'base' (prima data ~150MB)...")
        import whisper
        _whisper_model = whisper.load_model("base")
        print("[Whisper] Model incarcat.")
    return _whisper_model


@app.post("/transcribe")
async def transcribe_audio(
    file: UploadFile = File(...),
    language: str = Form(default="ro"),
    model: str = Form(default="whisper-1"),
):
    audio_bytes = await file.read()
    suffix = ".wav"
    with tempfile.NamedTemporaryFile(suffix=suffix, delete=False) as tmp:
        tmp.write(audio_bytes)
        tmp_path = tmp.name
    try:
        result = _get_whisper().transcribe(tmp_path, language=language)
        text = result.get("text", "").strip()
        print(f"[Whisper STT] '{text}'")
    except Exception as e:
        print(f"[Whisper] Eroare: {e}")
        text = ""
    finally:
        os.unlink(tmp_path)
    return JSONResponse({"text": text})


# ── WebSocket ─────────────────────────────────────────────── #
@app.websocket("/")
async def websocket_endpoint(websocket: WebSocket):
    await websocket.accept()
    session_id = str(uuid.uuid4())
    print(f"[+] Client conectat — sesiune {session_id}")
    try:
        while True:
            raw = await websocket.receive_text()
            msg = json.loads(raw)
            msg_type = msg.get("type")

            if msg_type == "get_agents":
                await websocket.send_text(json.dumps({
                    "type": "agents_list",
                    "agents": get_agents_list(),
                }))

            elif msg_type == "user_message":
                text  = msg.get("text", "").strip()
                agent = msg.get("agent", "ana")
                if not text:
                    continue
                print(f"[{session_id}] User → {agent}: {text}")
                result = process_message(text, session_id, agent_id=agent)
                if result["routed_from"]:
                    print(f"[{session_id}] Ana → {result['agent']}: delegat")
                print(f"[{session_id}] {result['agent_name']}: {result['text']}")
                await websocket.send_text(json.dumps({
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

    except WebSocketDisconnect:
        pass
    finally:
        print(f"[-] Client deconectat — sesiune {session_id}")


if __name__ == "__main__":
    host = os.getenv("HOST", "0.0.0.0")
    port = int(os.getenv("PORT", 8765))
    print(f"VR AI Team Backend")
    print(f"  WebSocket : ws://{host}:{port}/")
    print(f"  Whisper   : http://{host}:{port}/transcribe")
    uvicorn.run(app, host=host, port=port, log_level="warning")
