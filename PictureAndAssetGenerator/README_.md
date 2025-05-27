
# Main Program: `app.py`

`app.py` is the central control script of this project.  
It exposes a **REST interface** that lets you feed one positive and one negative prompt and automatically returns both an **image** and a **3‑D model in `.glb` format**.

---

## How It Works

1. The prompts are inserted into a workflow compatible with ComfyUI.  
2. A WebSocket connection fetches the generated image from the local ComfyUI server.  
3. The image is sent to a local model‑generation API.  
4. The resulting `.glb` file is downloaded and returned to the client.

---

## REST Endpoint: `/generate`

| Key           | Value                                                            |
|---------------|------------------------------------------------------------------|
| **URL**       | `http://127.0.0.1:5000/generate`                                 |
| **Method**    | `POST`                                                           |
| **Content‑Type** | `application/json`                                            |
| **Description** | Accepts positive and negative prompts, produces an image and a GLB‑format 3‑D model. |

### Example Request

```json
{
  "positive_prompt": "a futuristic city skyline at sunset",
  "negative_prompt": "low resolution, blurry, distorted"
}
```

### Example Response

* **Success:** the generated `.glb` file (MIME type: `model/gltf-binary`)  
* **Failure:** a JSON object containing an error message

---

## Example Call with `curl`

```bash
curl -X POST http://127.0.0.1:5000/generate \
  -H "Content-Type: application/json" \
  -d '{ 
        "positive_prompt": "a spaceship flying over Mars",
        "negative_prompt": "bad anatomy, ugly, deformed"
      }' --output model.glb
```

The resulting model is saved locally as `model.glb`.

---

# Trellis Stable Projectorz – Windows Installer

Visit https://github.com/IgorAherne/trellis-stable-projectorz/releases/tag/latest to install Trellis

---

# StableFast3D Windows Portable

Visit https://github.com/YanWenKun/StableFast3D-WinPortable to install StableFast3D

---

## Model Installation

Download https://civitai.com/models/139562/realvisxl-v50  
Save the file under `ComfyUI/models/checkpoints`.

---

## File Locations

| Purpose         | Path                |
|-----------------|---------------------|
| Outputs         | `ComfyUI/output`    |
| Sample inputs   | `ComfyUI/input`     |
