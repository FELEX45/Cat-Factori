"""Rewrite MainScene Factori: remove primitive box, keep Mirror, add FactoryHall."""
from pathlib import Path
import re

path = Path(r"C:\Users\Professional\Cat Factori\Assets\Scenes\MainScene.unity")
text = path.read_text(encoding="utf-8")

# Remove GameObjects and their components by anchoring fileIDs belonging to shell pieces
remove_ids = {
    # Wall (3)
    248455334, 248455335, 248455336, 248455337, 248455338,
    # Roof
    430811313, 430811314, 430811315, 430811316, 430811317,
    # Floor
    1079697926, 1079697927, 1079697928, 1079697929, 1079697930,
    # Wall (2)
    1318268684, 1318268685, 1318268686, 1318268687, 1318268688,
    # Wall
    1846570949, 1846570950, 1846570951, 1846570952, 1846570953,
    # Wall (1)
    2136127481, 2136127482, 2136127483, 2136127484, 2136127485,
}

# Split into YAML documents (Unity uses --- !u!TYPE &ID)
parts = re.split(r"(?=^--- !u!)", text, flags=re.M)
header = parts[0]
docs = parts[1:]

kept = []
for doc in docs:
    m = re.match(r"--- !u!\d+ &(-?\d+)", doc)
    if not m:
        kept.append(doc)
        continue
    fid = int(m.group(1))
    if fid in remove_ids:
        continue
    kept.append(doc)

text2 = header + "".join(kept)

# Update Factori GameObject components + children
factory_go = """--- !u!1 &1031115986
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 1031115987}
  - component: {fileID: 1031115988}
  - component: {fileID: 1031115989}
  - component: {fileID: 1031115990}
  - component: {fileID: 1031115991}
  m_Layer: 0
  m_Name: Factori
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
"""

factory_tf = """--- !u!4 &1031115987
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1031115986}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: -0.66884, y: 10.25, z: -5.13149}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {fileID: 901001002}
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
"""

factory_extra = """--- !u!33 &1031115988
MeshFilter:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1031115986}
  m_Mesh: {fileID: 0}
--- !u!23 &1031115989
MeshRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1031115986}
  m_Enabled: 1
  m_CastShadows: 1
  m_ReceiveShadows: 1
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RayTracingMode: 2
  m_RayTraceProcedural: 0
  m_RayTracingAccelStructBuildFlagsOverride: 0
  m_RayTracingAccelStructBuildFlags: 1
  m_SmallMeshCulling: 1
  m_ForceMeshLod: -1
  m_MeshLodSelectionBias: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {fileID: 0}
  - {fileID: 0}
  - {fileID: 0}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {fileID: 0}
  m_ProbeAnchor: {fileID: 0}
  m_LightProbeVolumeOverride: {fileID: 0}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 3
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {fileID: 0}
  m_GlobalIlluminationMeshLod: 0
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 0
  m_MaskInteraction: 0
  m_AdditionalVertexStreams: {fileID: 0}
--- !u!64 &1031115990
MeshCollider:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1031115986}
  m_Material: {fileID: 0}
  m_IncludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ExcludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_LayerOverridePriority: 0
  m_IsTrigger: 0
  m_ProvidesContacts: 0
  m_Enabled: 1
  serializedVersion: 5
  m_Convex: 0
  m_CookingOptions: 30
  m_Mesh: {fileID: 0}
--- !u!114 &1031115991
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1031115986}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: f55a4c52000000000000000000000001, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  width: 100
  height: 50
  depth: 100
  floorTileMeters: 4
  wallTileMeters: 4
  ceilingTileMeters: 8
  floorMaterial: {fileID: 2100000, guid: c44f3b41000000000000000000000001, type: 2}
  wallMaterial: {fileID: 2100000, guid: c44f3b41000000000000000000000002, type: 2}
  ceilingMaterial: {fileID: 2100000, guid: c44f3b41000000000000000000000003, type: 2}
  ensureDirectionalLight: 1
  lightColor: {r: 1, g: 0.96, b: 0.88, a: 1}
  lightIntensity: 1.35
  lightEuler: {x: 50, y: -30, z: 0}
"""

# Replace Factori GameObject + Transform blocks
text2 = re.sub(
    r"--- !u!1 &1031115986\nGameObject:.*?m_IsActive: 1\n",
    factory_go,
    text2,
    count=1,
    flags=re.S,
)
text2 = re.sub(
    r"--- !u!4 &1031115987\nTransform:.*?m_LocalEulerAnglesHint: \{x: 0, y: 0, z: 0\}\n",
    factory_tf,
    text2,
    count=1,
    flags=re.S,
)

# Insert FactoryHall components before SceneRoots
insert_at = text2.find("--- !u!1660057539 &9223372036854775807")
if insert_at < 0:
    raise SystemExit("SceneRoots not found")
text2 = text2[:insert_at] + factory_extra + text2[insert_at:]

path.write_text(text2, encoding="utf-8")
print("MainScene updated, size", len(text2))
# sanity
for name in ("Floor", "Roof", "Wall (1)", "Wall (2)", "Wall (3)", "m_Name: Wall\n"):
    if name == "m_Name: Wall\n":
        if re.search(r"m_Name: Wall\n", text2):
            print("WARNING still has Wall")
    elif name in text2:
        print("WARNING still has", name)
if "FactoryHall" in text2 or "f55a4c52" in text2:
    print("FactoryHall script ref OK")
if "m_Name: Mirror" in text2:
    print("Mirror kept")
if "m_Name: Factori" in text2:
    print("Factori kept")
