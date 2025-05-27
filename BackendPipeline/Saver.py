import openai_client
import json
import datetime
import shutil, logging
from global_paths import pycharm_folder, unity_folder
from pathlib import Path


def save_json(json_answer):
    # Falls json_answer ein String ist, Markdown-Codeblöcke entfernen
    if isinstance(json_answer, str):
        json_answer = json_answer.strip()
        if json_answer.startswith("```json"):
            json_answer = json_answer[len("```json"):].strip()
        if json_answer.endswith("```"):
            json_answer = json_answer[:-3].strip()

    # Falls der bereinigte String ein JSON-Format darstellt, in ein Python-Objekt umwandeln
    try:
        json_data = json.loads(json_answer)
    except json.JSONDecodeError:
        # Sollte json_answer bereits ein Dictionary sein oder kein gültiges JSON darstellen,
        # kann alternativ einfach json_answer verwendet werden
        json_data = json_answer

    # Erzeuge den Dateinamen mit Zeitstempel
    filename = fr'{pycharm_folder}\Output\output_{datetime.datetime.now().strftime("%Y-%m-%d_%H-%M-%S")}.json'
    with open(filename, 'w') as file:
        # Mit separators werden unnötige Leerzeichen entfernt
        json.dump(json_data, file, separators=(',', ':'))

    filename_unity = fr"{unity_folder}\output_{datetime.datetime.now().strftime("%Y-%m-%d_%H-%M-%S")}.json"
    with open(filename_unity, 'w') as file:
        # Mit separators werden unnötige Leerzeichen entfernt
        json.dump(json_data, file, separators=(',', ':'))

model = "o4-mini" # o4-mini o3

def save_evaluation_json(json_answer):
    category = "fairBooth"  # classroom conferenceRoom fairBooth
    prompt_category = "medium"  # easy medium hard
    validator = "withoutValidator"  # withoutValidator withValidator
    date = datetime.datetime.now().strftime("%Y-%m-%d_%H-%M-%S")
    # model = "o4-mini"  # o4-mini o3

    if isinstance(json_answer, str):
        json_answer = json_answer.strip()
        if json_answer.startswith("```json"):
            json_answer = json_answer[len("```json"):].strip()
        if json_answer.endswith("```"):
            json_answer = json_answer[:-3].strip()

    # Falls der bereinigte String ein JSON-Format darstellt, in ein Python-Objekt umwandeln
    try:
        json_data = json.loads(json_answer)
    except json.JSONDecodeError:
        # Sollte json_answer bereits ein Dictionary sein oder kein gültiges JSON darstellen,
        # kann alternativ einfach json_answer verwendet werden
        json_data = json_answer

    base = (
            Path(r"D:\JKoll\Documents\TH\Master\Masterarbeit\Evaluation")
            / f"output_{category}_{prompt_category}_{validator}_{model}_{date}"
    )
    # Erzeuge den Dateinamen mit Zeitstempel
    json_file = base.with_suffix(".json")
    txt_file  = base.with_suffix(".txt")
    with open(json_file, 'w') as file:
        # Mit separators werden unnötige Leerzeichen entfernt
        json.dump(json_data, file, separators=(',', ':'))

    # ------ Save Logging Window -------

    log_path = None
    for h in logging.getLogger().handlers:
        if isinstance(h, logging.FileHandler):
            h.flush()
            log_path = Path(h.baseFilename)
            break
    if not log_path or not log_path.exists():
        # falls kein Handler vorhanden oder Datei fehlt
        with txt_file.open("w", encoding="utf-8") as f:
            f.write("<< Kein Logfile gefunden >>")
    else:
        shutil.copy(log_path, txt_file)

    logging.info(
        f"Ergebnis gespeichert: {json_file.name}  +  {txt_file.name}",
        extra={"explicit": True},
    )
