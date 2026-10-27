"""Create the two non-destructive role-swap character assets for Unity.

Input meshes are the preserved project-local user source copies. This creates only derived .blend,
.fbx, metadata and offline workbench previews under ArtSource/UserProvided/RiggedCharacters and
Assets/FireRescue/Art/UserProvided/Characters/Rigged/Models.
"""
import bpy
import os
import math
import json
import hashlib
from pathlib import Path
from mathutils import Matrix, Vector

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = str(next(parent for parent in Path(HERE).parents if (parent / 'ProjectSettings').is_dir()))
OUT_ROOT = os.path.join(PROJECT, 'Assets', 'FireRescue', 'Art', 'UserProvided', 'Characters', 'Rigged', 'Models')

SPECS = [
    {
        'key': 'ResidentRole1',
        'output': 'Role1',
        'source': os.path.join(PROJECT, 'ArtSource', 'UserProvided', 'Character1', 'Source.fbx'),
        'height': 1.50,
        'palette': 'resident_white',
        'role': 'NPC resident',
        'has_tail': True,
    },
    {
        'key': 'FirefighterRole2',
        'output': 'Role2',
        'source': os.path.join(PROJECT, 'ArtSource', 'UserProvided', 'Character2', 'Source.fbx'),
        'height': 1.60,
        'palette': 'firefighter_u2',
        'role': 'Player firefighter',
        'has_tail': False,
    },
]


def vv(v):
    return [round(float(x), 6) for x in v]


def mesh_bounds(meshes):
    points = [obj.matrix_world @ vert.co for obj in meshes for vert in obj.data.vertices]
    lo = Vector(tuple(min(point[index] for point in points) for index in range(3)))
    hi = Vector(tuple(max(point[index] for point in points) for index in range(3)))
    return lo, hi


def material(name, color):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1.0)
    mat.use_nodes = True
    bsdf = next((node for node in mat.node_tree.nodes if node.type == 'BSDF_PRINCIPLED'), None)
    if bsdf:
        bsdf.inputs['Base Color'].default_value = (*color, 1.0)
        bsdf.inputs['Roughness'].default_value = .78
    return mat


def set_slots(obj, mats):
    obj.data.materials.clear()
    for mat in mats:
        obj.data.materials.append(mat)


def add_bone(edit_bones, name, head, tail, parent=None):
    bone = edit_bones.new(name)
    bone.head = head
    bone.tail = tail
    bone.parent = parent
    return bone


def key_rotation(armature, bone_name, frame, rotation):
    pose_bone = armature.pose.bones[bone_name]
    pose_bone.rotation_mode = 'XYZ'
    pose_bone.rotation_euler = rotation
    pose_bone.keyframe_insert('rotation_euler', frame=frame)


def normalized_meshes(source):
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=source, use_custom_normals=True)
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
    if not meshes:
        raise RuntimeError('No meshes imported from ' + source)
    source_counts = {obj.name: len(obj.data.vertices) for obj in meshes}
    lo, hi = mesh_bounds(meshes)
    center = (lo + hi) * .5
    source_height = hi.z - lo.z
    if source_height <= 0:
        raise RuntimeError('Invalid source height')
    return meshes, source_counts, lo, hi, center, source_height


def assign_materials(meshes, key, palette):
    # Materials are intentionally simple.  User will replace them with coloured
    # production assets later; this gives the role swap reliable visible defaults.
    if palette == 'resident_white':
        white = material('R1_White', (.82, .84, .86))
        lighter = material('R1_Light', (.94, .95, .96))
        dark = material('R1_Dark', (.07, .075, .09))
        for obj in meshes:
            set_slots(obj, [white, lighter, dark])
            name = obj.name.lower()
            for polygon in obj.data.polygons:
                polygon.material_index = 2 if ('圓形' in obj.name or 'circle' in name) else (1 if '立方體' not in obj.name else 0)
        return [white, lighter, dark]

    skin = material('U2_Skin', (.94, .70, .49))
    hair = material('U2_Hair', (.24, .115, .065))
    shirt = material('U2_Shirt', (.16, .56, .49))
    trim = material('U2_Trim', (.94, .85, .61))
    pants = material('U2_Pants', (.14, .20, .28))
    dark = material('U2_Dark', (.085, .10, .12))
    eye = material('U2_Eye', (.045, .028, .022))
    by_name = {obj.name: obj for obj in meshes}
    # Exact known Role2 source pieces.  Fall back to coordinate assignment below.
    for obj in meshes:
        set_slots(obj, [skin, pants, dark, shirt])
    if '圓形' in by_name:
        set_slots(by_name['圓形'], [eye])
    if '立方體' in by_name:
        set_slots(by_name['立方體'], [hair])
    if '立方體.002' in by_name:
        set_slots(by_name['立方體.002'], [shirt])
    for name in ('立方體.003', '立方體.004'):
        if name in by_name:
            set_slots(by_name[name], [trim])
    body = by_name.get('立方體.001')
    if body:
        set_slots(body, [skin, pants, dark, shirt])
        for polygon in body.data.polygons:
            point = body.matrix_world @ polygon.center
            z = point.z
            polygon.material_index = 2 if z < .128 else (1 if z < .32 else (0 if z > .735 or (abs(point.x) > .32 and .42 < z < .735) else 3))
    return [skin, hair, shirt, trim, pants, dark, eye]


def make_armature(key, height, add_tail):
    bpy.ops.object.armature_add(enter_editmode=True, location=(0, 0, 0))
    armature = bpy.context.object
    armature.name = key + '_Root'
    armature.data.name = key + '_Rig'
    bones = armature.data.edit_bones
    bones.remove(bones[0])

    root = add_bone(bones, 'Root', (0, 0, 0), (0, 0, height * .12))
    hips = add_bone(bones, 'Hips', (0, 0, height * .21), (0, 0, height * .34), root)
    spine = add_bone(bones, 'Spine', (0, 0, height * .34), (0, 0, height * .43), hips)
    chest = add_bone(bones, 'Chest', (0, 0, height * .43), (0, 0, height * .50), spine)
    neck = add_bone(bones, 'Neck', (0, 0, height * .50), (0, 0, height * .56), chest)
    add_bone(bones, 'Head', (0, 0, height * .56), (0, 0, height * .91), neck)
    for side, sign in [('L', -1), ('R', 1)]:
        x = sign * height * .11
        thigh = add_bone(bones, side + '_Thigh', (x, 0, height * .235), (sign * height * .145, 0, height * .12), hips)
        shin = add_bone(bones, side + '_Shin', (sign * height * .145, 0, height * .12), (sign * height * .16, 0, height * .042), thigh)
        add_bone(bones, side + '_Foot', (sign * height * .16, 0, height * .042), (sign * height * .16, -height * .08, height * .02), shin)
        upper = add_bone(bones, side + '_UpperArm', (sign * height * .165, 0, height * .46), (sign * height * .225, 0, height * .39), chest)
        lower = add_bone(bones, side + '_LowerArm', (sign * height * .225, 0, height * .39), (sign * height * .28, 0, height * .34), upper)
        add_bone(bones, side + '_Hand', (sign * height * .28, 0, height * .34), (sign * height * .30, -height * .015, height * .31), lower)
    if add_tail:
        tail_1 = add_bone(bones, 'Tail_1', (0, height * .07, height * .30), (0, height * .19, height * .28), hips)
        add_bone(bones, 'Tail_2', (0, height * .19, height * .28), (0, height * .29, height * .20), tail_1)
    bpy.ops.object.mode_set(mode='POSE')
    bpy.ops.pose.armature_apply(selected=False)
    bpy.ops.object.mode_set(mode='OBJECT')
    return armature


def nearest_bone(obj, point, height, has_tail):
    x, y, z = point.x, point.y, point.z
    # The imported source faces +X.  After normalisation its face points -Y and
    # the tail is behind +Y.  Large head meshes must remain with Head, never arms.
    if has_tail and y > height * .105 and .10 * height < z < .48 * height:
        return 'Tail_2' if y > height * .20 else 'Tail_1'
    if z > height * .55:
        return 'Head'
    if abs(x) > height * .22 and height * .28 < z < height * .49:
        return 'L_Hand' if x < 0 else 'R_Hand'
    if z < height * .075:
        return 'L_Foot' if x < 0 else 'R_Foot'
    if z < height * .165:
        return 'L_Shin' if x < 0 else 'R_Shin'
    if z < height * .30:
        return 'L_Thigh' if x < 0 else 'R_Thigh'
    if abs(x) > height * .17 and z > height * .40:
        return 'L_UpperArm' if x < 0 else 'R_UpperArm'
    if z > height * .44:
        return 'Chest'
    if z > height * .34:
        return 'Spine'
    return 'Hips'


def bind_meshes(meshes, armature, height, has_tail, palette):
    for obj in meshes:
        # Independent visible parts that clearly belong to the head retain one clean
        # transform.  For Role2 this handles eyes + hair correctly.
        name = obj.name
        # Role1 cube.001 is the separate long side-hair mesh.  Its low tips sit
        # beside the shoulders, so geometric fallbacks would incorrectly bind a
        # few tips to hands.  Keep the entire hair/face pieces on Head.
        object_head = (
            palette == 'firefighter_u2' and name in ('圓形', '立方體')
        ) or (
            palette == 'resident_white' and name in ('圓形', '圓形.001', '立方體.001', '立方體.005', '立方體.006')
        )
        for vertex in obj.data.vertices:
            point = obj.matrix_world @ vertex.co
            target = 'Head' if object_head else nearest_bone(obj, point, height, has_tail)
            group = obj.vertex_groups.get(target) or obj.vertex_groups.new(name=target)
            group.add([vertex.index], 1.0, 'REPLACE')
        obj.parent = armature
        modifier = obj.modifiers.new(name='RoleSwapSkin', type='ARMATURE')
        modifier.object = armature


def add_markers(armature, height):
    for name, location in [('FaceForward', (0, -height * .29, height * .55)), ('HeadCenter', (0, 0, height * .86))]:
        empty = bpy.data.objects.new(name, None)
        bpy.context.collection.objects.link(empty)
        empty.empty_display_type = 'ARROWS'
        empty.empty_display_size = .09
        empty.location = location
        empty.rotation_euler = (math.radians(90), 0, 0)
        empty.parent = armature


def make_clips(armature, key):
    armature.animation_data_create()
    def clear_pose():
        for pose_bone in armature.pose.bones:
            pose_bone.rotation_mode = 'XYZ'
            pose_bone.rotation_euler = (0, 0, 0)
            pose_bone.location = (0, 0, 0)
            pose_bone.scale = (1, 1, 1)

    clear_pose()
    idle = bpy.data.actions.new(key + '_Idle')
    idle.use_fake_user = True
    armature.animation_data.action = idle
    for frame, tilt in [(1, 0), (31, math.radians(1.35)), (61, 0)]:
        key_rotation(armature, 'Chest', frame, (tilt, 0, 0))
        key_rotation(armature, 'Head', frame, (-tilt * .42, 0, 0))
        key_rotation(armature, 'L_UpperArm', frame, (0, 0, tilt * 11))
        key_rotation(armature, 'R_UpperArm', frame, (0, 0, -tilt * 11))

    clear_pose()
    walk = bpy.data.actions.new(key + '_Walk')
    walk.use_fake_user = True
    armature.animation_data.action = walk
    for frame, swing in [(1, 0), (8, 1), (16, 0), (24, -1), (31, 0)]:
        leg = math.radians(17 * swing)
        arm_swing = math.radians(12 * swing)
        key_rotation(armature, 'L_Thigh', frame, (leg, 0, 0))
        key_rotation(armature, 'R_Thigh', frame, (-leg, 0, 0))
        key_rotation(armature, 'L_Shin', frame, (math.radians(-10 * max(swing, 0)), 0, 0))
        key_rotation(armature, 'R_Shin', frame, (math.radians(-10 * max(-swing, 0)), 0, 0))
        key_rotation(armature, 'L_UpperArm', frame, (-arm_swing, 0, 0))
        key_rotation(armature, 'R_UpperArm', frame, (arm_swing, 0, 0))
        key_rotation(armature, 'Chest', frame, (math.radians(1.0), 0, 0))
        key_rotation(armature, 'Head', frame, (math.radians(-.65), 0, 0))
    armature.animation_data.action = idle
    bpy.context.scene.render.fps = 24
    return idle, walk


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()


def preview(key, armature, action, frame, filename, location, height):
    armature.animation_data.action = action
    bpy.context.scene.frame_set(frame)
    bpy.context.scene.render.engine = 'BLENDER_WORKBENCH'
    bpy.context.scene.display.shading.light = 'STUDIO'
    bpy.context.scene.display.shading.color_type = 'MATERIAL'
    bpy.context.scene.world.color = (.12, .12, .12)
    camera = bpy.data.objects.get('PreviewCamera')
    if not camera:
        bpy.ops.object.camera_add()
        camera = bpy.context.object
        camera.name = 'PreviewCamera'
        bpy.context.scene.camera = camera
    camera.data.type = 'ORTHO'
    camera.data.ortho_scale = height * 1.22
    camera.location = location
    look_at(camera, (0, 0, height * .50))
    render = bpy.context.scene.render
    render.resolution_x = 700
    render.resolution_y = 700
    render.resolution_percentage = 100
    render.image_settings.file_format = 'PNG'
    render.filepath = filename
    bpy.ops.render.render(write_still=True)


def export_asset(spec):
    # Each exported FBX must carry only its own two clips.  The first derived
    # character's fake-user actions otherwise remain in Blender's datablock list
    # and Blender exports them again for the second model.
    for action in list(bpy.data.actions):
        bpy.data.actions.remove(action)
    meshes, source_counts, original_lo, original_hi, center, source_height = normalized_meshes(spec['source'])
    scale = spec['height'] / source_height
    # source front is +X; rotate it into project -Y while preserving upright Z.
    transform = Matrix.Translation((0, 0, -original_lo.z)) @ Matrix.Rotation(math.radians(-90), 4, 'Z') @ Matrix.Translation((-center.x, -center.y, 0))
    for obj in meshes:
        obj.matrix_world = Matrix.Scale(scale, 4) @ transform @ obj.matrix_world
    materials = assign_materials(meshes, spec['key'], spec['palette'])
    armature = make_armature(spec['key'], spec['height'], spec['has_tail'])
    bind_meshes(meshes, armature, spec['height'], spec['has_tail'], spec['palette'])
    add_markers(armature, spec['height'])
    idle, walk = make_clips(armature, spec['key'])

    blend_path = os.path.join(HERE, spec['key'] + '.blend')
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)

    os.makedirs(OUT_ROOT, exist_ok=True)
    fbx_path = os.path.join(OUT_ROOT, spec['output'] + '.fbx')
    bpy.ops.object.select_all(action='DESELECT')
    for obj in meshes + [armature] + [obj for obj in bpy.context.scene.objects if obj.name in {'FaceForward', 'HeadCenter'}]:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = armature
    bpy.ops.export_scene.fbx(
        filepath=fbx_path,
        use_selection=True,
        object_types={'ARMATURE', 'MESH', 'EMPTY'},
        add_leaf_bones=False,
        bake_anim=True,
        bake_anim_use_all_actions=True,
        bake_anim_force_startend_keying=True,
        bake_anim_simplify_factor=0.0,
        axis_forward='-Z',
        axis_up='Y')

    preview(spec['key'], armature, idle, 31, os.path.join(HERE, spec['key'] + '_idle.png'), (0, -4.0, spec['height'] * .55), spec['height'])
    preview(spec['key'], armature, walk, 8, os.path.join(HERE, spec['key'] + '_walk.png'), (2.7, -3.4, spec['height'] * 1.35), spec['height'])

    with open(spec['source'], 'rb') as source_file:
        source_hash = hashlib.sha256(source_file.read()).hexdigest()
    metadata = {
        'role': spec['role'],
        'source': spec['source'],
        'source_sha256': source_hash,
        'source_mesh_vertex_counts': source_counts,
        'source_bounds': {'min': vv(original_lo), 'max': vv(original_hi), 'height': round(float(source_height), 6)},
        'derived_fbx': os.path.relpath(fbx_path, PROJECT).replace('\\', '/'),
        'coordinates': {'source_front': '+X', 'source_up': 'Z', 'derived_local_face_marker': '-Y', 'feet_y': 0, 'height_m': spec['height'], 'root_motion': False},
        'materials': [mat.name for mat in materials],
        'bones': [bone.name for bone in armature.data.bones],
        'clips': [
            {'name': spec['key'] + '_Idle', 'frames': [1, 61], 'fps': 24, 'seconds': 2.5, 'loop': True},
            {'name': spec['key'] + '_Walk', 'frames': [1, 31], 'fps': 24, 'seconds': 1.25, 'loop': True},
        ],
        'markers': ['FaceForward', 'HeadCenter'],
        'notes': 'Topology was not recreated. Procedural weights bind original supplied meshes; gameplay colliders, tool anchors and root motion remain Unity-side.',
    }
    with open(os.path.join(HERE, spec['key'] + '_metadata.json'), 'w', encoding='utf-8') as metadata_file:
        json.dump(metadata, metadata_file, ensure_ascii=False, indent=2)
    return metadata


all_metadata = [export_asset(spec) for spec in SPECS]
with open(os.path.join(HERE, 'metadata.json'), 'w', encoding='utf-8') as output:
    json.dump({'assets': all_metadata}, output, ensure_ascii=False, indent=2)

