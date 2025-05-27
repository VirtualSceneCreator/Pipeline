from __future__ import annotations
import json
import os
from openai import OpenAI
from langgraph.graph import StateGraph, START, END
from langgraph.graph.message import add_messages
from langchain_core.messages import HumanMessage
from typing import Annotated, TypedDict, List, Optional, Any

from pydantic import BaseModel, Field, ValidationError
from unity_connection import unity_connection
from unity_connection import log_api_start, log_api_end

uc = unity_connection()


class ValidatorTool(BaseModel):
    scene: str = Field(..., description="The current Unity scene")
    action_plan: str = Field(...,
                             description="The action plan with all the objects to create")


class IssueReport(BaseModel):
    objectName: str = Field(..., description="Name of the object")
    path: str = Field(..., description="Hierarchy path")
    issue: str = Field(..., description="Issue Description")
    suggestedFix: str = Field(..., description="Suggested Fix")


class ValidatorResponse(BaseModel):
    issueReports: Optional[List[IssueReport]] = Field(...,
                                                      description="The Issue Reports")
    noImprovementSuggestions: Optional[str] = Field(...,
                                                    description="No suggestions for improvement")


class GraphState(TypedDict):
    messages: Annotated[list, add_messages]
    scene: Annotated[str, Field(..., description="The full unity scene")]
    issueReports: Annotated[
        Optional[List[IssueReport]],
        Field(..., description="List of issue reports")
    ]
    action_plan: Annotated[str, Field(..., description="Action plan to create the scene")]
    current_iteration: Annotated[int, Field(..., description="Current iteration")]
    max_iterations: Annotated[int, Field(..., description="Maximum number of iterations")]


def wall_validator_module(scene: Annotated[str, "The existing unity scene"],
                          action_plan: Annotated[str, "The action plan by which the scene was created"]) -> dict[
                                                                                                                str, Any] | str | Any:
    """Validates the untiy scene and responds with Improvement suggestions, needs the complete Unity scene JSON provided"""
    uc.send_silent_message("-------------------Start Wall Validator Module-------------------")
    uc.send_silent_message("Input Wall Action Plan: ", action_plan)
    uc.send_silent_message("Input Wall scene: ", scene)

    json_schema = ValidatorResponse.model_json_schema()
    functions = [
        {
            "name": "validate_scene",
            "description": "Validates the unity scene.",
            "parameters": json_schema
        }
    ]

    text = f"""
    You are a **Unity Scene Validator**.  
You will receive two inputs:  
  1. **existingScene** (JSON) – the full Unity 3D scene description (Hierarchy, Transforms, Colliders, Materials, etc.)  
  2. **action_plan** (action_plan) – the scene and objects asked for by the user   

**Focus**:
1. Only focus on the walls and the floor
2. Ignore all objects, except the walls and the floor

**Your Task**:  
Try to imagine the walls in a 3D World. If they are not okay, write a Fix Suggestions.

**Automated Fix Suggestions**  
- For every issue found, generate an **Issue Report** containing:  
- **objectName** (name of the object)  
- **path** Hierarchy path
- **issue** Issue Description 
- **suggestedFix** Suggested Fix: new Position, Rotation and/or Scale (absolute values or relative adjustments).  
- You are allowed to propose modifications to an object’s Position, Rotation, and Scale to correct the scene.

Here are some guidelines for you:
1. I will use your guideline to validate the objects *iteratively*, so please start with the first wall in your thought process.

Only Respond with the Automated Fix Suggestions, do NOT try to recreate the Unity scene by yourself, thats not your job!
Write your improvement Suggestions in the json field 'issueReports'.
If you have no improvement Suggestions, write that in the field 'noImprovementSuggestions'.
    """

    prompt = (
        f"Validate this existing scene:\n{scene}\n"
        f"This was the action plan:\n{action_plan}"
    )

    client = OpenAI(
        api_key=os.environ.get("OPENAI_API_KEY"),
    )
    uc.send_silent_message("-------------Wallvalidator start--------------")

    t0 = log_api_start("wall_validator_module")

    response = client.chat.completions.create(
        model="o4-mini",
        functions=functions,
        reasoning_effort="high",
        messages=[
            {"role": "system", "content": text},
            {"role": "user", "content": prompt}
        ],
        function_call="auto"
    )
    log_api_end(response, t0, "wall_validator_module")

    uc.send_silent_message("-------------Wallvalidator done--------------")

    message = response.choices[0].message
    if hasattr(message, "function_call") and message.function_call is not None:

        arguments = json.loads(message.function_call.arguments)
        uc.send_silent_message("-----------Wall Empfangene Daten:", json.dumps(arguments, indent=2))

        try:
            uc.send_silent_message("start validating WallValidatorResponse")
            module_instance = ValidatorResponse.model_validate(arguments)
            uc.send_silent_message("end validating WallValidatorResponse")

            uc.send_silent_message("-------------WallValidatorResponse:-----------")
            uc.send_silent_message(module_instance.model_dump())
            uc.send_silent_message("-------------WallValidatorResponse:-----------")
            uc.send_silent_message(json.dumps(module_instance.model_dump(), indent=2))
            return module_instance.model_dump()
        except ValidationError as e:
            uc.send_silent_message("Fehlerdetails:", e.errors())
            uc.send_silent_message("Fehlerdetails:", e.json())
            uc.send_silent_message("ValidationError:", e)
            uc.send_silent_message("----------------------Failed WallValidation Validator :/")
            return arguments
    uc.send_silent_message("----------------------Validator Wallnot successfull :/")
    return "WallValidator not successfull :/"


def collision_validator_module(scene: Annotated[str, "The existing unity scene"],
                               action_plan: Annotated[str, "The action plan by which the scene was created"]) -> dict[
                                                                                                                     str, Any] | str | Any:
    """Validates the untiy scene and responds with Improvement suggestions, needs the complete Unity scene JSON provided"""
    uc.send_silent_message("-------------------Start Validator Module-------------------")
    uc.send_silent_message("Input Action Plan: ", action_plan)
    uc.send_silent_message("Input scene: ", scene)

    json_schema = ValidatorResponse.model_json_schema()
    functions = [
        {
            "name": "validate_scene",
            "description": "Validates the unity scene.",
            "parameters": json_schema
        }
    ]

    text = f"""
    You are a **Unity Scene Validator**.  
You will receive two inputs:  
  1. **existingScene** (JSON) – the full Unity 3D scene description (Hierarchy, Transforms, Colliders, Materials, etc.)  
  2. **action_plan** (action_plan) – the scene and objects asked for by the user   


**Your Task**:  
Try to image the all the objects in a 3D world. Are there any collision? Are there objects in
the wrong place? Write a Fix Suggestion for everything that is wrong or could be improved. There should be no objects in other objects!
You can move the floor or any objects if needed.

**Automated Fix Suggestions**  
   - For every issue found, generate an **Issue Report** containing:  
     - **Object** (name and Hierarchy path)  
     - **Issue Description**  
     - **Suggested Fix**: new Position, Rotation and/or Scale (absolute values or relative adjustments).  
   - You are allowed to propose modifications to an object’s Position, Rotation, and Scale to correct the scene.

Only Respond with the Automated Fix Suggestions, do NOT try to recreate the Unity scene by yourself, thats not your job!
    """

    prompt = (
        f"Validate this existing scene:\n{scene}\n"
        f"This was the action plan:\n{action_plan}"
    )

    client = OpenAI(
        api_key=os.environ.get("OPENAI_API_KEY"),
    )
    uc.send_silent_message("-------------Collision validator start--------------")

    t0 = log_api_start("collision_validator_module")
    response = client.chat.completions.create(
        model="o4-mini",
        functions=functions,
        reasoning_effort="high",
        messages=[
            {"role": "system", "content": text},
            {"role": "user", "content": prompt}
        ],
        function_call="auto"
    )
    log_api_end(response, t0, "collision_validator_module")

    uc.send_silent_message("-------------Collision validator done--------------")

    message = response.choices[0].message
    if hasattr(message, "function_call") and message.function_call is not None:

        arguments = json.loads(message.function_call.arguments)
        uc.send_silent_message("-----------Collision Empfangene Daten:", json.dumps(arguments, indent=2))

        try:
            uc.send_silent_message("start validating CollisionValidatorResponse")
            module_instance = ValidatorResponse.model_validate(arguments)
            uc.send_silent_message("end validating CollisionValidatorResponse")

            uc.send_silent_message("-------------CollisionValidatorResponse:-----------")
            uc.send_silent_message(module_instance.model_dump())
            uc.send_silent_message("-------------CollisionValidatorResponse:-----------")
            uc.send_silent_message(json.dumps(module_instance.model_dump(), indent=2))
            return module_instance.model_dump()
        except ValidationError as e:
            uc.send_silent_message("Fehlerdetails:", e.errors())
            uc.send_silent_message("Fehlerdetails:", e.json())
            uc.send_silent_message("ValidationError:", e)
            uc.send_silent_message("----------------------Failed CollisionValidation Validator :/")
            return arguments
    uc.send_silent_message("----------------------Validator Collision  not successfull :/")
    return "CollisionValidator not successfull :/"


def realign_module(scene: Annotated[str, "The existing unity scene"],
                   action_plan: Annotated[str, "The action plan by which the scene was created"],
                   issue_reports: Annotated[List[IssueReport], "The issue reports"]) -> str:
    """Realigns the untiy scene according to the issue reports"""
    uc.send_silent_message("-------------------Start realign Module-------------------")
    uc.send_silent_message("Input Action Plan: ", action_plan)
    uc.send_silent_message("Issue Reports: ", issue_reports)
    uc.send_silent_message("Input scene: ", scene)

    text = f"""
You are a **Unity object replacer**. 
You will receive three inputs:  
- an action plan with the idea of the scene
- an existing unity scene
- Issue reports with improvement suggestions to change the existing scene

**Your Task**: 
- Change the existing scene according to the issue reports and their suggested fix.

# Guidelines to follow
- You must pay attention to the suggested fixes and create right size and coordinates for all the objects asked for.
- Keep the other objects not mentioned in the issue reports as they are.

"""

    prompt = (
        f"This is the existing scene:\n{scene}\n"
        f"This was the action plan:\n{action_plan}"
        f"These are the issue reports:\n{issue_reports}"
    )

    client = OpenAI(
        api_key=os.environ.get("OPENAI_API_KEY"),
    )
    uc.send_silent_message("-------------realign start--------------")

    t0 = log_api_start("realign_module")
    response = client.chat.completions.create(
        model="o4-mini",
        reasoning_effort="high",
        messages=[
            {"role": "system", "content": text},
            {"role": "user", "content": prompt}
        ]
    )
    log_api_end(response, t0, "realign_module")

    uc.send_silent_message("-------------realign done--------------")

    uc.send_silent_message(response.choices[0].message.content)
    return response.choices[0].message.content


def validator_sup(state: GraphState):
    if state["current_iteration"] >= state["max_iterations"]:
        return (
            {
                "messages": [
                    HumanMessage(content="Max iterations reached", name="validator_sup")
                ],
                "next": END
            }
        )
    state["issueReports"] = []

    wall_result = wall_validator_module(state["scene"], state["action_plan"])
    wall_issues = wall_result if isinstance(wall_result, list) else [wall_result]
    collision_result = collision_validator_module(state["scene"], state["action_plan"])
    collision_issues = collision_result if isinstance(collision_result, list) else [collision_result]

    issue_reports = wall_issues + collision_issues

    if not issue_reports or all(ir is None for ir in issue_reports) or issue_reports == [None]:
        return (
        {
            "messages": [
                HumanMessage(content="Finished Validation", name="validator_sup")
            ],
            "issueReports": issue_reports,
            "current_iteration": state["current_iteration"] + 1,
            "next": END
        }
    )

    return (
        {
            "messages": [
                HumanMessage(content="Finished Validation", name="validator_sup")
            ],
            "issueReports": issue_reports,
            "current_iteration": state["current_iteration"] + 1,
            "next": "realign_node"
        }
    )


def wall_validator_node(state: GraphState):
    result = wall_validator_module(state["scene"], state["action_plan"])
    new_issues = result if isinstance(result, list) else [result]
    existing_issues = state["issueReports"] or []
    return (
        {
            "messages": [
                HumanMessage(content="Finished Wall Validator", name="wall_validator")
            ],
            "issueReports": existing_issues + new_issues
        }
    )


def collision_validator_node(state: GraphState):
    result = collision_validator_module(state["scene"], state["action_plan"])
    new_issues = result if isinstance(result, list) else [result]
    existing_issues = state["issueReports"] or []
    return (
        {
            "messages": [
                HumanMessage(content="Finished Collision Validator", name="collision_validator")
            ],
            "issueReports": existing_issues + new_issues
        }
    )


def realign_node(state: GraphState):
    filtered_issues = []
    for entry in state.get("issueReports", []):
        reports = entry.get("issueReports")
        if reports:  # None und leere Listen werden ignoriert
            filtered_issues.extend(reports)

    result = realign_module(state["scene"], state["action_plan"], filtered_issues)

    return (
        {
            "messages": [
                HumanMessage(content="Finished Realign", name="realign")
            ],
            "scene": result
        }
    )


def start_validate(scene, action_plan, max_iterations):
    builder = StateGraph(GraphState)
    builder.add_node("validator_sup", validator_sup)
    builder.add_node("realign_node", realign_node)

    builder.add_edge(START, "validator_sup")
    builder.add_conditional_edges(
        "validator_sup",
        lambda validator_result: validator_result.get("next", END)
    )
    builder.add_edge("realign_node", "validator_sup")
    graph = builder.compile()

    input_data = {
        "messages": [HumanMessage(content="Validate this json", name="user")],
        "scene": scene,
        "issueReports": None,
        "action_plan": action_plan,
        "current_iteration": 0,
        "max_iterations": max_iterations
    }

    result = graph.invoke(input_data)

    return result["scene"]
