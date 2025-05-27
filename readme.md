# Virtual Scene Creator (VSC)

This repository contains everything you need to reproduce the results of our **VSC (Virtual Scene Creator)**—an end-to-end pipeline that converts **natural-language descriptions of 3-D scenes** into fully realised virtual-reality environments.

---

## Repository Structure
```
Virtual Scene Creator
├── Evaluation 
├── BackendPipeline 
├── Visualizer
└── PictureAndAssetGenerator
```


### Directory Details

| Path | Purpose |
|------|---------|
| **Evaluation/** | <ul><li>**results/** – Excel sheets with the evaluation data and a Python script for parsing them</li><li>**Classroom_*/** – datasets generated for classroom scenes</li><li>**FairBooth_*/** – datasets generated for trade-fair booth scenes</li></ul> |
| **BackendPipeline/** | Implements the back-end steps of the project. It communicates with the **Visualizer** and handles all requests coming from the chatbot, covering the stages **Scene Planning** and **Layout Generation**. |
| **Visualizer/** | Unity project that forms the central interface of the pipeline. It launches the chatbot, enables user intervention, and triggers asset generation. |
| **PictureAndAssetGenerator/** | <ul><li>**FlaskConnectorTrellisComfy/** – Flask back-end for communicating with Trellis, ComfyUI and OpenAI</li><li>**TextTo/** – generation scripts for image creation in ComfyUI</li></ul> |

---

## How to Reproduce the Examples

1. **Set up the Visualizer**  
   Install Unity, then open the project in the **Visualizer/** folder through the Unity Editor.

2. **Set up the Backend Pipeline**  
   Create a `.env` file inside **BackendPipeline/** with the following keys: 
     OPENAI_API_KEY=<your-OpenAI-API-key>
     UNITY_PATH=<absolute-path-to-Visualizer>

3. **Set up the PictureAndAssetGenerator**  
Follow the instructions in the **PictureAndAssetGenerator** README, then start the Flask server inside that directory.

4. **Start Creating**  
* In Unity, right-click inside the *Hierarchy* panel → **Show UI** to launch the chatbot and create a scene.  
* Once the scene is finished, a new JSON file is written to `Visualizer/Json_Tests/GPT_Tests/`.  
* To visualise it, select the **Visualizer** GameObject and assign the JSON file to it, then press **Ctrl + G**.  
* If you are satisfied with the layout, you can trigger asset generation via another right-click in the *Hierarchy* → **DevTools → AssetGeneration**.

---

## Tips & Notes
* The pipeline assumes an active internet connection for model queries and asset downloads.  
* Large assets are stored via Git LFS; be sure to run `git lfs install` before cloning if you use LFS.

---

We hope you enjoy experimenting with **VSC (Virtual Scene Creator)**!  
For questions or feedback, feel free to open an issue or submit a pull request.

