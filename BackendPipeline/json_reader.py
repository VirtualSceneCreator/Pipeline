import json
import os

class JsonReader:
    """Eine Klasse zum Einlesen einer JSON-Datei als String oder Dictionary"""

    def __init__(self, file_path):
        """Initialisiert den Reader mit dem Dateipfad."""
        self.file_path = file_path

    def read_json(self, as_string=True):
        """
        Liest eine JSON-Datei und gibt sie entweder als String oder Dictionary zurück.

        :param as_string: True gibt JSON als String zurück, False als Dictionary.
        :return: JSON-Inhalt oder eine Fehlermeldung.
        """
        if not os.path.exists(self.file_path):
            return f"Fehler: Datei '{self.file_path}' nicht gefunden."

        try:
            with open(self.file_path, "r", encoding="utf-8") as file:
                json_data = json.load(file)  # JSON-Datei parsen
                print("🔻 Json correctly read 🔻")
                if as_string:
                    return json.dumps(json_data, indent=4, ensure_ascii=False)  # Schöner formatierter String
                return json_data  # JSON als Dictionary

        except json.JSONDecodeError as e:
            return f"Fehler: Ungültiges JSON-Format in '{self.file_path}'.\nDetails: {str(e)}"
        except Exception as e:
            return f"Unerwarteter Fehler: {str(e)}"
