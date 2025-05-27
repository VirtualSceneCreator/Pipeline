import os.path
import openai_client
import sys


from unity_connection import unity_connection
from Saver import save_json, save_evaluation_json

unity_folder = os.path.join(os.getenv('UNITY_PATH'), "Assets", "Json_Tests", "GPT_Tests")
pycharm_folder = os.path.dirname(os.path.abspath(__file__))


if __name__ == '__main__':
    max_iterations = 2
    extra_string = None
    uc = unity_connection()

    # Argument 1: max_iterations
    if len(sys.argv) > 1:
        try:
            max_iterations = int(sys.argv[1])
        except ValueError:
            print("Bitte eine Ganzzahl für max_iterations angeben!")
            sys.exit(1)

    # Genau 1 zusätzliches Argument → lets_go()
    if len(sys.argv) == 2:
        final_json = openai_client.lets_go(max_iterations=max_iterations)
        save_json(final_json)
        uc.send_silent_message("--------Json saved-----------")
    # Genau 2 zusätzliche Argumente → lets_go_evaluation()
    elif len(sys.argv) == 3:
        extra_string = sys.argv[2]
        final_json = openai_client.lets_go_evaluation(
            max_iterations=max_iterations,
            prompt=extra_string
        )
        save_evaluation_json(final_json)
        uc.send_silent_message("--------Json saved-----------")
    else:
        final_json = openai_client.lets_go(
            max_iterations=max_iterations
        )
        save_evaluation_json(final_json)
        uc.send_silent_message("--------Json saved-----------")