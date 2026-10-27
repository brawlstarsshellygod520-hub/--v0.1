"""Read-only derived-FBX validation for role swap assets."""
import bpy
import os
import json
from pathlib import Path
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = str(next(parent for parent in Path(HERE).parents if (parent / 'ProjectSettings').is_dir()))
MODELS = os.path.join(PROJECT, 'Assets', 'FireRescue', 'Art', 'UserProvided', 'Characters', 'Rigged', 'Models')
CASES = [('Role1', 10, 1.50, ['Tail_1', 'Tail_2']), ('Role2', 6, 1.60, [])]

def clear():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.actions, bpy.data.armatures, bpy.data.meshes, bpy.data.materials):
        for block in list(datablocks):
            if block.users == 0:
                datablocks.remove(block)

def bounds(meshes):
    points = [obj.matrix_world @ vertex.co for obj in meshes for vertex in obj.data.vertices]
    lo = Vector(tuple(min(point[i] for point in points) for i in range(3)))
    hi = Vector(tuple(max(point[i] for point in points) for i in range(3)))
    return lo, hi

results = []
for key, expected_meshes, expected_height, extras in CASES:
    clear()
    filepath = os.path.join(MODELS, key + '.fbx')
    bpy.ops.import_scene.fbx(filepath=filepath, use_custom_normals=True)
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
    armatures = [obj for obj in bpy.context.scene.objects if obj.type == 'ARMATURE']
    empties = [obj.name for obj in bpy.context.scene.objects if obj.type == 'EMPTY']
    if len(meshes) != expected_meshes:
        raise RuntimeError('%s mesh count: %d' % (key, len(meshes)))
    if len(armatures) != 1:
        raise RuntimeError('%s armature count: %d' % (key, len(armatures)))
    armature = armatures[0]
    required = {'Root','Hips','Spine','Chest','Neck','Head','L_UpperArm','L_LowerArm','L_Hand','R_UpperArm','R_LowerArm','R_Hand','L_Thigh','L_Shin','L_Foot','R_Thigh','R_Shin','R_Foot'} | set(extras)
    missing = sorted(required - {bone.name for bone in armature.data.bones})
    if missing:
        raise RuntimeError('%s missing bones: %s' % (key, missing))
    if not {'FaceForward','HeadCenter'}.issubset(set(empties)):
        raise RuntimeError('%s lacks facing markers: %s' % (key, empties))
    for obj in meshes:
        if not any(mod.type == 'ARMATURE' and mod.object == armature for mod in obj.modifiers):
            raise RuntimeError('%s mesh %s has no armature modifier' % (key, obj.name))
        if not obj.vertex_groups:
            raise RuntimeError('%s mesh %s has no vertex weights' % (key, obj.name))
    lo, hi = bounds(meshes)
    height = hi.z - lo.z
    if abs(height - expected_height) > .025:
        raise RuntimeError('%s height %.4f != %.4f' % (key, height, expected_height))
    names = [action.name for action in bpy.data.actions]
    expected_actions = [name for name in names if key in name and ('Idle' in name or 'Walk' in name)]
    if len(expected_actions) < 2:
        raise RuntimeError('%s actions not imported: %s' % (key, names))
    results.append({'asset':key, 'meshes':len(meshes), 'bones':len(armature.data.bones), 'height':round(float(height),5), 'actions':expected_actions, 'markers':sorted(set(empties) & {'FaceForward','HeadCenter'})})

with open(os.path.join(HERE, 'validation.json'), 'w', encoding='utf-8') as file:
    json.dump({'status':'PASS','results':results}, file, ensure_ascii=False, indent=2)

