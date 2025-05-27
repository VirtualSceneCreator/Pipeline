import os

from unity_connection import unity_connection
from langsmith import utils
from dotenv import load_dotenv

load_dotenv()
openai_api_key = os.getenv('OPENAI_API_KEY')
pycharm_folder = os.path.dirname(os.path.abspath(__file__))
unity_folder = os.path.join(os.getenv('UNITY_PATH'), "Assets", "Json_Tests", "GPT_Tests")

#print(utils.tracing_is_enabled())

uc = unity_connection()
