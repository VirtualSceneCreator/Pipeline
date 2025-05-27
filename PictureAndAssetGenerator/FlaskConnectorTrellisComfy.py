# ─────────────────────────── app.py ───────────────────────────
#  v3.5 – Verbose‑Debug + per‑Request Denoise‑Stärke
#  22 Apr 2025

from __future__ import annotations
import base64, io, json, logging, os, random, re, socket, subprocess, time, uuid, datetime, pathlib, urllib.parse, urllib.request
from typing import Dict, Optional

import openai
import requests
import websocket
from flask import Flask, jsonify, request, send_file

# ───────── Konfiguration ──────────────────────────────────────
WORKFLOW_SERVER = "127.0.0.1:8188"
MODEL_GEN_API   = "http://127.0.0.1:7960"
openai.api_key  = "yourKeyHere"
client_id       = str(uuid.uuid4())

_SAVE_DIR = pathlib.Path("saved_images")
_SAVE_DIR.mkdir(exist_ok=True)

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s | %(levelname)-8s | %(message)s",
    datefmt="%H:%M:%S",
    handlers=[logging.StreamHandler()]
)
log = logging.getLogger("APP")

app = Flask(__name__)

# ───────── Gemeinsame Helfer ──────────────────────────────────
def _first_existing(wf: Dict, *ids):
    for i in ids:
        if i in wf:
            return i
    return None

def _find_node_id(wf: Dict, class_type: str, *fallback_ids) -> str:
    cand = _first_existing(wf, *fallback_ids)
    if cand:
        return cand
    for k, v in wf.items():
        if v.get("class_type") == class_type:
            return k
    raise RuntimeError(f"No '{class_type}' node found in workflow")

def queue_prompt(prompt: Dict) -> Dict:
    log.info("▶ Comfy‑Prompt an Queue senden …")
    data = json.dumps({"prompt": prompt, "client_id": client_id}, ensure_ascii=False).encode()
    req  = urllib.request.Request(f"http://{WORKFLOW_SERVER}/prompt", data=data)
    rsp  = json.loads(urllib.request.urlopen(req).read())
    log.info("✓ Prompt‑ID %s erhalten", rsp["prompt_id"])
    return rsp

def get_image(filename: str, subfolder: str, folder_type: str) -> bytes:
    log.info("⬇ Bild herunterladen … (%s)", filename)
    params = urllib.parse.urlencode({"filename": filename, "subfolder": subfolder, "type": folder_type})
    with urllib.request.urlopen(f"http://{WORKFLOW_SERVER}/view?{params}") as r:
        return r.read()

def get_history(prompt_id: str) -> Dict:
    with urllib.request.urlopen(f"http://{WORKFLOW_SERVER}/history/{prompt_id}") as r:
        return json.loads(r.read())

def _poll_websocket(prompt_id: str) -> None:
    log.info("⏳ Warte auf Prompt‑Execution %s …", prompt_id)
    ws = websocket.WebSocket()
    ws.connect(f"ws://{WORKFLOW_SERVER}/ws?clientId={client_id}")
    while True:
        out = ws.recv()
        if isinstance(out, str):
            m = json.loads(out)
            if m["type"] == "executing":
                d = m["data"]
                if d["node"] is None and d["prompt_id"] == prompt_id:
                    log.info("✓ Prompt %s fertig", prompt_id)
                    break
    ws.close()

# ───────── Debug‑Speicher ────────────────────────────────────
def _save_image(img_bytes: bytes, prompt: str) -> pathlib.Path:
    ts   = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
    snip = re.sub(r"[^A-Za-z0-9]+", "_", prompt)[:30] or "no_prompt"
    fp   = _SAVE_DIR / f"{ts}_{snip}.png"
    with open(fp, "wb") as f:
        f.write(img_bytes)
    log.info("💾 Bild gespeichert unter %s", fp)
    return fp

# ───────── Upload Basisbild ──────────────────────────────────
def _upload_image_to_comfy(img_bytes: bytes) -> str:
    log.info("⇡ Lade Basisbild zu Comfy …")
    files = {"image": ("init.png", img_bytes, "image/png")}
    resp  = requests.post(f"http://{WORKFLOW_SERVER}/upload/image", files=files, timeout=30)
    resp.raise_for_status()
    fn = resp.json()["name"]
    log.info("✓ Basisbild %s hochgeladen", fn)
    return fn

# ───────── Bild‑Generierung ──────────────────────────────────
def generate_image_data(
    positive: str,
    negative: str,
    backend: str = "local",
    base_image_b64: Optional[str] = None,
    denoise: Optional[float] = None
) -> bytes:

    log.info("=== Bild‑Generierung | Backend=%s | Denoise=%s ===", backend, denoise)
    if backend == "dalle3" and denoise is not None:
        log.warning("Denoise wird bei DALL·E‑Backend ignoriert.")

    # —— OpenAI DALL·E‑3 ——————————————————————————————
    if backend == "dalle3":
        # Jetzt das image-1 Modell verwenden und Base64 zurückbekommen
        rsp = openai.images.generate(
            model="gpt-image-1",  # statt "dall-e-3"
            prompt=positive,
            quality="low",
            size="1024x1024",
            n=1,
        )
        # b64_json enthält das Bild als Base64-String
        base64_img = rsp.data[0].b64_json
        img = base64.b64decode(base64_img)

        log.info("✓ image-1 Bild erstellt (%d Bytes)", len(img))
        return img

    # —— Lokaler Comfy‑Workflow ————————————————————————
    if backend == "local":
        wf_path = "TextToImageWithBaseImage.json" if base_image_b64 else "TextToOneImage.json"
        log.info("Workflow: %s", wf_path)
        with open(wf_path, encoding="utf‑8") as f:
            wf = json.load(f)

        # Seed
        seed_node = "12" if "12" in wf and "seed" in wf["12"]["inputs"] else next(
            k for k, v in wf.items() if v["class_type"] == "KSampler"
        )
        rnd_seed = random.randint(0, 2**32 - 1)
        wf[seed_node]["inputs"]["seed"] = rnd_seed
        log.info("Seed‑Node %s = %d", seed_node, rnd_seed)

        # Prompts
        pos_node = _find_node_id(wf, "CLIPTextEncode", "29", "9")
        neg_node = _find_node_id(wf, "CLIPTextEncode", "30", "10")
        wf[pos_node]["inputs"]["text"] = positive
        wf[neg_node]["inputs"]["text"] = negative

        # Denoise
        if denoise is not None:
            if "denoise" in wf[seed_node]["inputs"]:
                wf[seed_node]["inputs"]["denoise"] = float(denoise)
                log.info("Denoise auf %s gesetzt", denoise)
            else:
                log.warning("KSampler hat kein 'denoise'‑Feld!")

        # Basisbild
        if base_image_b64:
            img_bytes = base64.b64decode(base_image_b64.split(",")[-1])
            filename  = _upload_image_to_comfy(img_bytes)
            load_node = _find_node_id(wf, "LoadImage", "15")
            wf[load_node]["inputs"]["image"] = filename

        pid  = queue_prompt(wf)["prompt_id"]
        _poll_websocket(pid)
        hist = get_history(pid)[pid]["outputs"]
        node = next(iter(hist))
        img  = hist[node]["images"][0]
        buf  = get_image(img["filename"], img["subfolder"], img["type"])
        log.info("Bild aus Node %s geladen (%d Bytes)", node, len(buf))
        return buf

    raise ValueError(f"Unknown backend '{backend}'")

# ───────── Modell‑Generierung (wie zuvor) ─────────────────────
def generate_model_from_base64(image_b64: str) -> str:
    log.info("=== 3D‑Modell‑Generierung ===")
    params = dict(
        image_base64         = image_b64,
        seed                 = 42,
        ss_guidance_strength = 7.5,
        ss_sampling_steps    = 30,
        slat_guidance_strength = 7.5,
        slat_sampling_steps  = 30,
        mesh_simplify_ratio  = 0.95,
        texture_size         = 1024,
        output_format        = "glb",
    )
    requests.post(f"{MODEL_GEN_API}/generate_no_preview", data=params).raise_for_status()
    while True:
        st = requests.get(f"{MODEL_GEN_API}/status").json()
        if st["status"] == "COMPLETE":
            log.info("✓ Modell fertig")
            break
        if st["status"] == "FAILED":
            raise RuntimeError(st["message"])
        time.sleep(1)
    glb = requests.get(f"{MODEL_GEN_API}/download/model")
    glb.raise_for_status()
    path = "generated_model.glb"
    with open(path, "wb") as f:
        f.write(glb.content)
    log.info("💾 Modell gespeichert unter %s", path)
    return path

# ───────── REST‑Endpunkte ─────────────────────────────────────
@app.post("/generate_image")
def api_generate_image():
    log.info("=== /generate_image ===")
    try:
        d = request.json or {}
        if "positive_prompt" not in d:
            return jsonify(error="Missing 'positive_prompt'"), 400
        img = generate_image_data(
            positive = d["positive_prompt"],
            negative = d.get("negative_prompt", ""),
            backend  = d.get("image_backend", "local").lower(),
            base_image_b64 = d.get("base_image_base64") or None,
            denoise = d.get("denoise")
        )
        _save_image(img, d["positive_prompt"])
        return send_file(io.BytesIO(img), mimetype="image/png", download_name="preview.png")
    except Exception as ex:
        log.exception("Fehler in /generate_image")
        return jsonify(error=repr(ex)), 500

@app.post("/generate_model")
def api_generate_model():
    log.info("=== /generate_model ===")
    d = request.json or {}
    b64 = d.get("image_base64")
    if not b64:
        return jsonify(error="Missing 'image_base64'"), 400
    try:
        path = generate_model_from_base64(b64)
        return send_file(path, mimetype="model/gltf-binary", as_attachment=True, download_name="model.glb")
    except Exception as ex:
        log.exception("Fehler in /generate_model")
        return jsonify(error=repr(ex)), 500

@app.post("/generate")
def legacy_generate():
    log.info("=== /generate (legacy) ===")
    try:
        data = request.json or {}
        img  = generate_image_data(
            positive = data.get("positive_prompt", ""),
            negative = data.get("negative_prompt", ""),
            backend  = data.get("image_backend", "local").lower(),
            base_image_b64 = data.get("base_image_base64") or None,
            denoise = data.get("denoise")
        )
        _save_image(img, data.get("positive_prompt", "legacy"))
        glb = generate_model_from_base64(base64.b64encode(img).decode())
        return send_file(glb, mimetype="model/gltf-binary", as_attachment=True, download_name="model.glb")
    except Exception as ex:
        log.exception("Fehler in /generate")
        return jsonify(error=repr(ex)), 500

# ───────── Start lokaler Server (unchanged) ──────────────────
def _is_open(h: str, p: int) -> bool:
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        s.settimeout(1)
        return s.connect_ex((h, p)) == 0

def _wait(h: str, p: int, name: str):
    while not _is_open(h, p):
        log.info("[%s] warte …", name)
        time.sleep(2)

def _start_if_needed(h: str, p: int, bat: str, name: str):
    if not _is_open(h, p):
        log.info("[%s] Starte Batch %s", name, bat)
        subprocess.Popen(f'start "" "{os.path.abspath(bat)}"', shell=True, cwd=os.path.dirname(bat))
    _wait(h, p, name)

if os.environ.get("WERKZEUG_RUN_MAIN") == "true" or os.environ.get("FLASK_RUN_FROM_CLI") == "true":
    _start_if_needed("127.0.0.1", 8188, r"SF3D\run.bat", "Workflow‑Server")
    _start_if_needed("127.0.0.1", 7960, r"trellis-spz\run-fp16.bat", "Model‑Generator")

if __name__ == "__main__":
    app.run(port=5000)
