from pydantic import BaseModel, Field
from typing import List, Dict, Optional


class XYZCoordinates(BaseModel):
    x: float
    y: float
    z: float


class Dimensions3D(BaseModel):
    width: float
    height: float
    depth: float


class LightSource(BaseModel):
    lightType: str = Field(..., description="Art der Lichtquelle")
    intensity: Optional[float] = None
    color: Optional[str] = None
    range: Optional[float] = None
    spotAngle: Optional[float] = None


class Environment(BaseModel):
    type: str = Field(..., description="Art der Umgebung")
    dimensions: Dimensions3D
    lighting: Optional[List[LightSource]] = None
    background: Optional[str] = None


class RelativePositioning(BaseModel):
    referenceObject: str
    relation: str
    distance: Optional[float] = None


class SceneObject(BaseModel):
    objectId: str
    objectType: str
    assetName: Optional[str] = None
    position: Optional[XYZCoordinates] = None
    rotation: XYZCoordinates
    dimensions: Dimensions3D
    group: Optional[str] = None
    relativePositioning: Optional[RelativePositioning] = None
    offset: Optional[XYZCoordinates] = None
    children: Optional[List['SceneObject']] = None
    anchors: Optional[Dict[str, XYZCoordinates]] = None

    class Config:
        arbitrary_types_allowed = True


class Scene(BaseModel):
    sceneName: str
    environment: Environment
    objectGroups: Optional[Dict[str, Dict[str, str]]] = None
    objects: List[SceneObject]


class RootSchema(BaseModel):
    scene: Scene
