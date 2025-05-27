import os

if os.name == 'nt':
    import ctypes

    ctypes.windll.kernel32.AllocConsole()

from tkinter.scrolledtext import ScrolledText
import time, inspect
import os, sys, time, json, shutil, datetime, threading, logging, tkinter as tk
from pathlib import Path


LOG_DIR = Path(__file__).with_name("logs")
LOG_DIR.mkdir(exist_ok=True)

LOGFILE = LOG_DIR / f"app_{os.getpid()}.log"


class ExplicitFilter(logging.Filter):
    def filter(self, record):
        return getattr(record, 'explicit', False)


class TextHandler(logging.Handler):
    """Schreibt Log-Records in ein ScrolledText-Widget."""
    def __init__(self, text_widget: tk.Text):
        super().__init__()
        self.text_widget = text_widget

    def emit(self, record):
        msg = self.format(record)
        tag = "explicit_tag" if getattr(record, "explicit", False) else None

        def _append():
            self.text_widget.configure(state="normal")
            self.text_widget.insert(tk.END, msg + "\n", tag)
            self.text_widget.configure(state="disabled")
            self.text_widget.yview(tk.END)

        self.text_widget.after(0, _append)


def _create_logging_window():
    root = tk.Tk()
    root.title(f"Logging – PID {os.getpid()}")
    st = ScrolledText(root, state="disabled", width=90, height=22)
    st.pack(expand=True, fill="both")
    st.tag_config("explicit_tag", foreground="blue")

    # ­­­Formatter
    formatter = logging.Formatter("[%(asctime)s] %(levelname)s: %(message)s")

    # ­­­Text-Handler
    text_handler = TextHandler(st)
    text_handler.setFormatter(formatter)

    # ­­­File-Handler
    file_handler = logging.FileHandler(LOGFILE, encoding="utf-8", mode="w")
    file_handler.setFormatter(formatter)

    # ­­­Root-Logger
    root_logger = logging.getLogger()
    root_logger.setLevel(logging.DEBUG)
    root_logger.addHandler(text_handler)
    root_logger.addHandler(file_handler)

    # Fenster-Close → Logging sauber herunterfahren
    def _on_close():
        logging.info("Logging window closed by user")
        logging.shutdown()
        root.destroy()

    root.protocol("WM_DELETE_WINDOW", _on_close)
    return root


def start_logging_window():
    root = _create_logging_window()
    root.mainloop()


threading.Thread(target=start_logging_window, daemon=True).start()


class unity_connection:
    def __init__(self):
        pass

    @staticmethod
    def send_message(*args, **kwargs):
        """
        Sendet Nachrichten, die von Unity erfasst werden
        """
        message = " ".join(str(arg) for arg in args)
        formatted_message = message
        sys.stdout.write(formatted_message + "\n")
        sys.stdout.flush()

    @staticmethod
    def send_silent_message(*args, **kwargs):
        """
        Schreibt Nachrichten ausschließlich in das separate Logging-Fenster.
        """
        message = " ".join(str(arg) for arg in args)
        logging.info(message, extra={'explicit': True})
        #logging.info(message)

    @staticmethod
    def get_unity_message():
        """
        Liest eine Zeile von sys.stdin (anstelle von input("User: ")).
        """
        sys.stdout.flush()
        message = sys.stdin.readline().strip()
        return message


def log_api_start(label: str | None = None):
    #label = label or _caller_name()
    logging.info(f"{label} | Request started …", extra={'explicit': True})
    return time.perf_counter()


def log_api_end(response, start_time: float, label: str | None = None):
    #label = label or _caller_name()
    elapsed = time.perf_counter() - start_time

    # Token-Infos holen (falls vorhanden)
    if getattr(response, "usage", None):
        u = response.usage
        token_info = (f"Tokens prompt={u.prompt_tokens}, "
                      f"completion={u.completion_tokens}, "
                      f"total={u.total_tokens}")
    else:
        token_info = "Tokens unknown (no usage field)"

    logging.info(f"{label} | Request finished in {elapsed:.3f} s | {token_info}", extra={'explicit': True})
