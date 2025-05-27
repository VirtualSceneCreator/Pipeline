from __future__ import annotations
from global_paths import pycharm_folder
import os
import base64
from openai import OpenAI
from langgraph.prebuilt import create_react_agent
from langchain_core.messages import HumanMessage, ToolMessage
from langchain_core.tools import tool
from langchain_openai import ChatOpenAI
from typing import Annotated, List, Optional
import Validator
import time

from pydantic import BaseModel, Field

from unity_connection import unity_connection, log_api_start, log_api_end
from Saver import model
from ExampleSelector import ExampleSelector


llm = ChatOpenAI(model="o4-mini", verbose=True)
uc = unity_connection()

received_images = []


def encode_image_to_data_uri(image_path: str) -> str:
    """Liest das Bild und gibt eine Data-URI zurück."""
    ext = os.path.splitext(image_path)[1].lstrip(".").lower()
    mime = f"image/{'jpeg' if ext in ['jpg','jpeg'] else ext}"
    with open(image_path, "rb") as f:
        b64 = base64.b64encode(f.read()).decode("utf-8")
    return f"data:{mime};base64,{b64}"


def master_planner():
    with open(fr"{pycharm_folder}\Planner_Examples\examples_v3.txt", 'r', encoding='utf-8') as file:
        text = file.read()

    client = OpenAI(
        api_key=os.environ.get("OPENAI_API_KEY"),
    )

    messages = [
        {"role": "system", "content": text}
    ]
    t0 = log_api_start("master_planner")
    while True:
        user_input = uc.get_unity_message()
        if user_input.lower() in ["exit", "quit"]:
            break

        if user_input.startswith("Image_flush:"):
            image_path = user_input.split("Image_flush:", 1)[1]
            received_images.append(image_path)
            uc.send_silent_message(f"Receive Image: {image_path}")

            data_uri = encode_image_to_data_uri(image_path)

            image_message = {
                "role": "user",
                "content": [
                    {"type": "text", "text": "I uploaded this picture for you. Dont answer, until i ask you a question about it."},
                    {"type": "image_url", "image_url": {"url": data_uri}}
                ],
            }
            messages.append(image_message)
        else:
            messages.append({"role": "user", "content": user_input})

        response = client.chat.completions.create(
            model="gpt-4.1-mini",
            messages=messages
        )

        assistant_reply = response.choices[0].message.content

        messages.append({"role": "assistant", "content": assistant_reply})

        if "Conversation finished" in assistant_reply:
            uc.send_message("Okay, lets go...")
            break
        else:
            uc.send_message(assistant_reply)
    log_api_end(response, t0, "master_planner")
    uc.send_silent_message(response.choices[0].message.content + "\n")
    return response.choices[0].message.content


def master_planner_evaluation(prompt):
    with open(fr"{pycharm_folder}\Planner_Examples\examples_v3.txt", 'r', encoding='utf-8') as file:
        text = file.read()

    client = OpenAI(
        api_key=os.environ.get("OPENAI_API_KEY"),
    )

    messages = [
        {"role": "system", "content": text},
        {"role": "user", "content": prompt}
    ]
    t0 = log_api_start("master_planner")
    response = client.chat.completions.create(
        model="gpt-4.1-mini",
        messages=messages
    )
    log_api_end(response, t0, "master_planner")
    assistant_reply = response.choices[0].message.content

    messages.append({"role": "assistant", "content": assistant_reply})

    if "Conversation finished" in assistant_reply:
        uc.send_silent_message(response.choices[0].message.content + "\n")
        return response.choices[0].message.content
    else:
        return None


class Dimensions(BaseModel):
    """
    3D dimensions (scaling factors for X, Y, and Z).
    """
    width: float = Field(..., description="Width (X-axis)")
    height: float = Field(..., description="Height (Y-axis)")
    depth: float = Field(..., description="Depth (Z-axis)")

    class Config:
        extra = "forbid"
        frozen = True


class SizeTool(BaseModel):
    action_plan: str = Field(..., description="The action plan to create the zones")


class ObjectPlacementTool(BaseModel):
    action_plan: str = Field(..., description="The action plan with all the objects to create")
    relations: List[str] = Field(..., description="Relations between the objects")
    gameobjects: List[str] = Field(..., description="GameObjects and their sizes")
    image_path: Optional[str] = Field(None, description="Path to the image")


class ValidatorTool(BaseModel):
    scene: str = Field(..., description="The current Unity scene")
    action_plan: str = Field(..., description="The action plan with all the objects to create")


class ObjectRealignTool(BaseModel):
    scene: str = Field(..., description="The current Unity scene")
    improvements: str = Field(..., description="All improvement suggestions")


class RelationTool(BaseModel):
    objects: List[str] = Field(..., description="List of the objects, that will be placed in the unity scene")
    action_plan: str = Field(..., description="The action plan with details of the relations")


class GameObject(BaseModel):
    name: str = Field(..., description="Name of the game object")
    dimension: Dimensions = Field(..., description="Dimension of the game object")


class GameObjects(BaseModel):
    gameObjects: List[GameObject] = Field(..., description="List of the game objects")


@tool(args_schema=ObjectPlacementTool)
def object_placement_module(action_plan: Annotated[str, "The action plan to create the scene"],
                            relations: Annotated[str, "Relations between the objects"],
                            gameobjects: Annotated[List[str], "GameObjects and their sizes"],
                            image_path: Optional[str] = None) -> str:
    """Returns the specific objects placed in the 3D World. Needs a list of objects that should be placed and the zones"""
    uc.send_silent_message("-------------------Start Object Placement Module-------------------")
    uc.send_silent_message("Input Action Plan: ", action_plan)
    uc.send_silent_message("Relations: ", relations)
    uc.send_silent_message("GameObjects: ", gameobjects)
    uc.send_silent_message("Image Path: ", image_path)


    with open(fr"{pycharm_folder}\Julang_Examples\julang_v2.json", 'r', encoding='utf-8') as file:
        json_schema = file.read()

    text = f"""
You are a Unity architect and object placer. 
You will be receive by the user:
- an action_plan to with the user request to create a unity 3D scene.
- a relation list, that explains the wanted relations in the 3D scene.
- a gameobjects list, that contains all the gameobjects and their sizes.

Your task is:
- to imagine the requested scene by the user in a 3D world.
- try to critically scrutinize the relations and object sizes given to you.
- try to improve the relations and sizes or change them completely, if they are wrong, but keep the intended idea behind them.
- Place all objects **aligned logically** in the 3D world with a carefully considered position, rotation and scale! 

# Guidelines to follow
- Make sure, the objects are placed right on the floor / wall / other objects / flying.
- Make sure, the scale of the objects match each other!
- The size of the objects cant be zero.
- If walls exist, all the objects should be placed inside the room, e.g. on the inside of the walls.
- Its very important to try to rotate the objects, so they look appealing.
- Its very important, that there are no unwanted collisions!
- Before finalizing the render, check and confirm that every object’s dimensions, rotation and spatial relationships are correct.
- Return the full 3D scene in the given Json Schema, see 'Json Schema'

# Json Schema
{json_schema}
"""
    examples = ExampleSelector.get_similar_examples(action_plan, 1)
    uc.send_silent_message("Examples: ", examples)

    if examples:
        prompt = f"""These are Examples from previous created 3D scenes: {examples}\n
            Create Objects with this plan:\n{action_plan}\n
            Use these relations for the objects: {relations}
            use these objects and their sizes {gameobjects}"""
    else:
        prompt = f"""Create Objects with this plan:\n{action_plan}\n
            Use these relations for the objects: {relations}\n
            use these objects and their sizes {gameobjects}"""

    client = OpenAI(
        api_key=os.environ.get("OPENAI_API_KEY"),
    )
    uc.send_silent_message("-------------obj fixed start--------------")

    messages = [
        {"role": "system", "content": text},
        {"role": "user", "content": prompt},
    ]

    if image_path:
        data_uri = encode_image_to_data_uri(image_path)
        messages.append({
            "role": "user",
            "content": [
                {"type": "text",
                 "text": "This picture should be created as a 3D Scene. Only use the action_plan and relations and objects as a reference."},
                {"type": "image_url", "image_url": {"url": data_uri}}
            ]
        })
    t0 = log_api_start("object_placement_module")
    response = client.chat.completions.create(
        model=model, #o3 gpt-4.1 o4-mini
        reasoning_effort="high",
        messages=messages
    )
    log_api_end(response, t0, "object_placement_module")
    uc.send_silent_message("-------------obj fixed done--------------")

    uc.send_silent_message("Result: ", response.choices[0].message.content)

    return response.choices[0].message.content


@tool(args_schema=RelationTool)
def relation_module(objects: Annotated[str, "List of the objects, that will be placed"],
                    action_plan: Annotated[str, "The action plan with details of the relations"]) -> str:
    """Returns a plan for the specific zones in the fair booth."""
    uc.send_silent_message("-------------------Start Relation Module-------------------")
    uc.send_silent_message("Input Objects: ", objects)
    uc.send_silent_message("Input Action_plan: ", action_plan)

    text = f"""
    You are an experienced room designer. You will receive as input:
     - objects: list of the objects, that will be placed in the unity scene.
     - action_plan: the action plan with details of the relations.

Please help me arrange objects in the room by assigning
constraints to each object. Here are the constraints and their definitions:
1. global constraint:
    1.1) edge: at the edge of the room, close to the wall.
    1.2) middle: not at the edge of the room.
    1.3) front: at the front of the room.
    1.4) back: at the back of the room
    1.5) side, left: at the left side of the room.
    1.6) side, right: at the right side of the room.
2. distance constraint:
    2.1) near, object: near to the other object, but with some distanbce, 50cm < distance < 150cm.
    2.2) far, object: far away from the other object, distance >= 150cm.
3. position constraint:
    3.1) in front of, object: in front of another object.
    3.2) side of, object: on the side (left or right) of another object.
    3.3) on top of, object: directly on top of an object (touching edges)
    3.4) above, object: not touching, but directy above an object.
    3.5) flying mid air: not touching any objects and flying mid in the room.
    3.6) flying top air: not touching any objects and flying high in the room.
4. alignment constraint: 
    4.1) center aligned, object: align the center of the object with the center of another object.
5. Rotation constraint: 
    5.1) face to, object: face to the center of another object.
    5.2) rotated: little bit rotated, to look better
6. Height constraint:
    6.1) ground: on the ground
    6.2) top: touching the ceiling
    6.3) air: between the ceiling and the ground

For each object, you must have one global constraint and you can select various numbers of constraints and any
combinations of them and the output format must be: object | global constraint | constraint 1 | constraint 2 | ...
For example: sofa-0 | edge
coffee table-0 | middle | near, sofa-0 | in front of, sofa-0 | center aligned, sofa-0 | face to, sofa-0
tv stand-0 | edge | far, coffee table-0 | in front of, coffee table-0 | center aligned, coffee table-0 | face to,
coffee table-0
flying car-0 | middle| far, coffee table-0 | flying top air

Here are some guidelines for you:
1. I will use your guideline to arrange the objects *iteratively*, so please start with an anchor object which doesn’t
depend on the other objects (with only one global constraint).
2. Place the larger objects first.
3. The latter objects could only depend on the former objects.
4. The objects of the *same type* are usually *aligned*.
5. There are usually no objects on top of the walls. With "one a wall" is usually meant -> on the side of a wall.
6. Chairs must be placed near to the table/desk and face to the table/desk.

Please keep going until the users query is completely resolved, before ending your turn.
    """

    prompt = f"""
Here are the objects that I want to place: {objects}
Here are the relations that I want to create: {action_plan}
Please first use natural language to explain your high-level design strategy, and then follow the desired format
*strictly* (do not add any additional text at the beginning or end) to provide the constraints for each object."""
    client = OpenAI(
        api_key=os.environ.get("OPENAI_API_KEY"),
    )
    uc.send_silent_message("-------------Relation start--------------")
    t0 = log_api_start("relation_module")
    response = client.chat.completions.create(
        model="o4-mini",
        reasoning_effort="high",
        messages=[
            {"role": "system", "content": text},
            {"role": "user", "content": prompt}
        ]
    )
    log_api_end(response, t0, "relation_module")
    uc.send_silent_message("-------------Relation done--------------")
    uc.send_silent_message("Result: ", response.choices[0].message.content)
    return response.choices[0].message.content


@tool(args_schema=SizeTool)
def game_objects_size_module(action_plan: Annotated[str, "Action_plan to create the unity scene"]) -> str:
    """Returns a plan for the specific zones in the fair booth."""
    uc.send_silent_message("-------------------Start Size Module-------------------")
    uc.send_silent_message("Input: ", action_plan)

    text = """
    You are an experienced room designer. You get an action plan as input to define game Objects for the Unity scene.
    Your Task is to create sizes for all the GameObjects asked for.

Here are some guidelines for you:
1. I will use your guideline to create the sizes of the objects *iteratively*, so please start with an anchor object which doesn’t
depend on the other objects.
2. Create the larger objects first.
3. The objects of the *same type* have usually the same size.
4. The sizes of the objects must match so that one object is not unintentionally much larger than another.
5. Its very important that you create the walls and floor at the end with a size, that alle object will fit into the scene and that there is enough space between the objects.

Please keep going until the users query is completely resolved, before ending your turn.
You MUST plan extensively before calculating the size of the floor at the end.

Create for each GameObject a JSON Element like this:
{"name": "coffe_table1", "size": { "width": 10, "height": 3, "depth": 1 }}
    """

    prompt = f"""
Here are the objects that I want to place in the scene: {action_plan}
Please first use natural language to explain your high-level size strategy, and then follow the desired format
*strictly* (do not add any additional text at the beginning or end) to provide the constraints for each object."""
    client = OpenAI(
        api_key=os.environ.get("OPENAI_API_KEY"),
    )
    uc.send_silent_message("-------------Size start--------------")

    t0 = log_api_start("game_objects_size_module")
    response = client.chat.completions.create(
        model="o4-mini",
        reasoning_effort="high",
        messages=[
            {"role": "system", "content": text},
            {"role": "user", "content": prompt}
        ]
    )
    log_api_end(response, t0, "game_objects_size_module")
    uc.send_silent_message("-------------Size done--------------")
    uc.send_silent_message("Result: ", response.choices[0].message.content)
    return response.choices[0].message.content


def agent_start(action_plan):
    if received_images:
        prompt = f"""
You are a Unity Scene Generator Agent. Create the Scene with the action plan given to you.
Please keep going until the users query is completely resolved, before ending your turn.
Use your tools, dont hallucinate.
You MUST plan extensively before each tool call and reflect extensively on the outcomes after.
Best Practice is to first use your tool 'game_objects_size_module', to get a list of all the game objects and their sizes.
Then call the 'relation_module', to get the relations between the objects.
Then call the 'object_placement_module', to create the objects in your Unity scene.
Always forward the complete action plan, do not change the action plan!
These are all the images to get a sense of what the scene should look like: {received_images}.
Pass the full path of the images to the object_placement_module."""
    else:
        prompt = f"""
You are a Unity Scene Generator Agent. Create the Scene with the action plan given to you.
Please keep going until the users query is completely resolved, before ending your turn.
Use your tools, dont hallucinate.
You MUST plan extensively before each tool call and reflect extensively on the outcomes after.
Best Practice is to first use your tool 'game_objects_size_module', to get a list of all the game objects and their sizes.
Then call the 'relation_module', to get the relations between the objects.
Then call the 'object_placement_module', to create the objects in your Unity scene.
Always forward the complete action plan, do not change the action plan!"""


    agent = create_react_agent(model=llm,
                               tools=[game_objects_size_module, relation_module, object_placement_module],
                               prompt=prompt)
    t0 = log_api_start("agent_start")
    result = agent.invoke({"messages": [HumanMessage(content=action_plan)]})
    log_api_end(result, t0, "agent_start")
    tool_msgs = [m for m in result["messages"] if isinstance(m, ToolMessage)]
    return tool_msgs[-1]


def json_converter(prompt, master_action_plan):
    uc.send_silent_message("-------------------Start Json Converter-------------------")
    with open(fr"{pycharm_folder}\Julang_Examples\examples_v2.txt", 'r', encoding='utf-8') as file:
        text = file.read()

    client = OpenAI(
        api_key=os.environ.get("OPENAI_API_KEY"),
    )

    uc.send_silent_message("Message sent")

    t0 = log_api_start("json_converter")

    response = client.chat.completions.create(
        model="o4-mini",
        messages=[
            {"role": "system", "content": text},
            {"role": "user", "content": f"Use this action plan for the style: {master_action_plan}\n"
                                        f"Put this scene into the json format: {prompt}"}
        ]
    )

    log_api_end(response, t0, "json_converter")

    if hasattr(response, "usage") and response.usage:
        usage = response.usage
        token_info = (
            f"Tokens prompt={usage.prompt_tokens}, "
            f"completion={usage.completion_tokens}, "
            f"total={usage.total_tokens}"
        )
    else:
        token_info = "Tokens unknown (request failed or usage missing)"

    uc.send_silent_message(response.choices[0].message.content + "\n")
    uc.send_silent_message("-------------------Finished-------------------")
    uc.send_silent_message("-------------------Finished-------------------")
    uc.send_silent_message("-------------------Finished-------------------")
    return response.choices[0].message.content


def lets_go(max_iterations):
    master_action_plan = master_planner()
    created_scene = agent_start(master_action_plan)
    validated_scene = Validator.start_validate(created_scene, master_action_plan, max_iterations)
    final_json = json_converter(validated_scene, master_action_plan)

    uc.send_silent_message("Finished")

    return final_json


def lets_go_evaluation(max_iterations, prompt):
    master_action_plan = master_planner_evaluation(prompt)
    created_scene = agent_start(master_action_plan)
    validated_scene = Validator.start_validate(created_scene, master_action_plan, max_iterations)
    final_json = json_converter(validated_scene, master_action_plan)

    uc.send_silent_message("Finished")

    return final_json
