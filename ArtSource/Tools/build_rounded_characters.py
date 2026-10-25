"""Build rigged, rounded Fire Rescue toy characters from the approved brief.

The characters face Blender -Y (Unity +Z after FBX export), stand on Z=0,
and deliberately use broad curved forms rather than the former boxy figures.
Run with Blender 5.1:
  blender --background --python ArtSource/Tools/build_rounded_characters.py
"""
from pathlib import Path
import json
import math
import sys
from mathutils import Matrix

sys.path.insert(0, str(Path(__file__).resolve().parent))
from model_primitives import *

OUT = ROOT / 'Docs' / 'Art' / 'Models' / 'v0.3'
OUT.mkdir(parents=True, exist_ok=True)
PREVIEW_WORK = ROOT / 'Logs' / 'CharacterFrames' / 'v0.3'
PREVIEW_WORK.mkdir(parents=True, exist_ok=True)


def flat_bone(name, head, tail, parent=None):
    bone = bpy.context.object.data.edit_bones.new(name)
    bone.head, bone.tail = head, tail
    if parent:
        bone.parent = bpy.context.object.data.edit_bones[parent]
        bone.use_connect = False
    return bone


def make_rig(asset):
    bpy.ops.object.armature_add(enter_editmode=True, location=(0, 0, 0))
    rig = bpy.context.object
    rig.name = asset + '_Rig'
    rig.data.name = asset + '_Armature'
    for default_bone in list(rig.data.edit_bones):
        rig.data.edit_bones.remove(default_bone)
    # A compact 18-bone humanoid.  The individual toy segments use rigid
    # weights, which keeps the puffy silhouette clean while still being a
    # genuine deform rig and not a transform-only fake.
    flat_bone('Root', (0, 0, 0), (0, 0, .12))
    flat_bone('Hips', (0, 0, .34), (0, 0, .57), 'Root')
    flat_bone('Spine', (0, 0, .57), (0, 0, .84), 'Hips')
    flat_bone('Chest', (0, 0, .84), (0, 0, 1.08), 'Spine')
    flat_bone('Neck', (0, 0, 1.08), (0, 0, 1.17), 'Chest')
    flat_bone('Head', (0, 0, 1.17), (0, 0, 1.49), 'Neck')
    firefighter = asset == 'Firefighter'
    for side, x in (('L', -.285), ('R', .285)):
        if firefighter:
            shoulder, elbow, hand = (x, -.01, 1.01), (x * .9825, -.12, .92), (x * .456, -.24, .88)
        else:
            shoulder, elbow, hand = (x, -.005, 1.0), (x * 1.18, -.02, .77), (x * 1.16, -.05, .60)
        flat_bone('UpperArm.' + side, shoulder, elbow, 'Chest')
        flat_bone('LowerArm.' + side, elbow, hand, 'UpperArm.' + side)
        flat_bone('Hand.' + side, hand, (hand[0], hand[1] - .07, hand[2] - .02), 'LowerArm.' + side)
        flat_bone('UpperLeg.' + side, (x * .55, 0, .56), (x * .72, 0, .29), 'Hips')
        flat_bone('LowerLeg.' + side, (x * .72, 0, .29), (x * .72, -.01, .105), 'UpperLeg.' + side)
        flat_bone('Foot.' + side, (x * .72, -.01, .105), (x * .72, -.17, .07), 'LowerLeg.' + side)
    bpy.ops.object.mode_set(mode='OBJECT')
    return rig


def bind(obj, rig, bone):
    group = obj.vertex_groups.new(name=bone)
    group.add(list(range(len(obj.data.vertices))), 1.0, 'REPLACE')
    modifier = obj.modifiers.new('Toy Armature', 'ARMATURE')
    modifier.object = rig
    obj.parent = rig


def bind_blended_limb(obj, rig, upper_bone, lower_bone, knee_z, blend_half=.065):
    """Continuous toy limb with a small linear upper/lower knee weight blend."""
    upper = obj.vertex_groups.new(name=upper_bone)
    lower = obj.vertex_groups.new(name=lower_bone)
    for vertex in obj.data.vertices:
        z = (obj.matrix_world @ vertex.co).z
        upper_weight = max(0.0, min(1.0, (z - (knee_z - blend_half)) / (blend_half * 2.0)))
        upper.add([vertex.index], upper_weight, 'REPLACE')
        lower.add([vertex.index], 1.0 - upper_weight, 'REPLACE')
    modifier = obj.modifiers.new('Toy Armature', 'ARMATURE')
    modifier.object = rig
    obj.parent = rig


def make_marker(name, location, rig, bone):
    marker = bpy.data.objects.new(name, None)
    marker.empty_display_type = 'SPHERE'
    marker.empty_display_size = .035
    bpy.context.collection.objects.link(marker)
    marker.parent = rig
    marker.parent_type = 'BONE'
    marker.parent_bone = bone
    bpy.context.view_layer.update()
    # Set the position after bone parenting, in world space, so catalog users
    # get a true head/front-eye marker rather than a local bone-origin marker.
    marker.matrix_world = Matrix.Translation(location)
    return marker


def rounded_ellipsoid(name, loc, dims, mat):
    """Primary visible forms use 24x12 smooth rings, not the kit's 16x8."""
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=12, radius=1, location=loc)
    o = bpy.context.object
    o.name = name
    o.dimensions = dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    o.data.materials.append(material(mat) if isinstance(mat, str) else mat)
    for poly in o.data.polygons:
        poly.use_smooth = True
    return o


def parts_ellipsoid(parts, name, loc, dims, mat, bone):
    o = rounded_ellipsoid(name, loc, dims, mat)
    parts.append((o, bone))
    return o


def parts_flat_hem_ellipsoid(parts, name, loc, dims, mat, bone, hem_ratio=.45, cut_ratio=.34):
    """Rounded upper volume with a broad, vertically extended flat hem.

    We flatten from the *wide* lower ring (``cut_ratio``) down to the desired
    hem floor instead of merely clipping the sphere at its narrow bottom.
    This keeps a compact shirt silhouette while still covering the leg roots.
    """
    o = rounded_ellipsoid(name, loc, dims, mat)
    hem_z = -dims[2] * hem_ratio
    cut_z = -dims[2] * cut_ratio
    for vertex in o.data.vertices:
        if vertex.co.z < cut_z:
            vertex.co.z = hem_z
    parts.append((o, bone))
    return o


def parts_box(parts, name, loc, dims, mat, bone, bevel=.06):
    o = box(name, loc, dims, mat, bevel)
    parts.append((o, bone))
    return o


def parts_asymmetric_hair_cap(parts, name, loc, dims, mat, bone):
    """Single smooth cap with its left-front edge softly dipping as a fringe."""
    o = rounded_ellipsoid(name, loc, dims, mat)
    for vertex in o.data.vertices:
        # The negative-Y surface is the face.  Only the lower left-front edge
        # dips, creating one integrated, asymmetric fringe without a sticker.
        x, y, z = vertex.co
        if y < -dims[1] * .08 and x < -dims[0] * .02 and z < -dims[2] * .04:
            front = min(1.0, max(0.0, (-y / (dims[1] * .5) - .08) / .92))
            left = min(1.0, max(0.0, (-x / (dims[0] * .5) - .02) / .98))
            low = min(1.0, max(0.0, (-z / (dims[2] * .5) - .04) / .96))
            vertex.co.z -= .065 * front * left * low
    parts.append((o, bone))
    return o


def parts_banded_ellipsoid(parts, name, loc, dims, base_mat, bone, bands):
    """One real rounded mesh with colour bands assigned to its own surface.

    ``bands`` uses pre-normalisation world-Z ranges.  Unlike a torus or a
    second shell, the reflector cannot float outside the rounded torso.
    """
    o = rounded_ellipsoid(name, loc, dims, base_mat)
    for key, low, high in bands:
        index = len(o.data.materials)
        o.data.materials.append(material(key))
        for poly in o.data.polygons:
            z = sum((o.matrix_world @ o.data.vertices[i].co).z for i in poly.vertices) / len(poly.vertices)
            if low <= z <= high:
                poly.material_index = index
    parts.append((o, bone))
    return o


def parts_between(parts, name, start, end, radius, mat, bone):
    o = between(name, start, end, radius, mat)
    parts.append((o, bone))
    return o


def parts_capsule(parts, name, start, end, radius, mat, bone):
    """A single round-ended low-poly limb, avoiding cylinder end caps."""
    a, b = Vector(start), Vector(end)
    o = rounded_ellipsoid(name, (a + b) * .5, (radius * 2, radius * 2, (b - a).length + radius * 1.55), mat)
    o.rotation_mode = 'QUATERNION'
    o.rotation_quaternion = (b - a).to_track_quat('Z', 'Y')
    parts.append((o, bone))
    return o


def parts_torus(parts, name, loc, major, minor, mat, bone, y_scale=.72):
    bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor,
                                    major_segments=24, minor_segments=8, location=loc)
    o = bpy.context.object
    o.name = name
    o.scale.y = y_scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    o.data.materials.append(material(mat) if isinstance(mat, str) else mat)
    for poly in o.data.polygons:
        poly.use_smooth = True
    parts.append((o, bone))
    return o


def parts_lathe(parts, name, profile, loc, mat, bone, segments=24):
    o = lathe(name, profile, loc, mat, segments)
    parts.append((o, bone))
    return o


def firefighter_geometry():
    parts = []
    # Deliberately overlap pelvis, torso and upper legs.  At a toy scale a
    # clean continuous silhouette is more important than anatomical seams:
    # this avoids the former floating-egg legs and pinched belly.
    # One continuous torso owns the pelvis and leg roots.  Keeping this as a
    # single skinned volume prevents an overlapping red pelvis from hiding the
    # yellow safety stripe at game distance.
    body = parts_flat_hem_ellipsoid(parts, 'Firefighter_Body', (0, .018, .84), (.63, .46, .80), 'Uniform', 'Chest')
    # Assign the broad chest reflector to the actual body surface, after the
    # hem mesh is shaped, rather than adding a floating belt around it.
    body.data.materials.append(material('Reflector'))
    for poly in body.data.polygons:
        z = sum((body.matrix_world @ body.data.vertices[i].co).z for i in poly.vertices) / len(poly.vertices)
        if .64 <= z <= .79:
            poly.material_index = 1
    for side, x in (('L', -.18), ('R', .18)):
        # The upper part is intentionally buried in the wide pelvis through
        # the full stride.  A single smoothly weighted volume bends at knee.
        leg = parts_banded_ellipsoid(parts, 'Firefighter_Leg_' + side, (x, .025, .345), (.31, .35, .51),
                                     'Uniform', ('UpperLeg.' + side, 'LowerLeg.' + side, .29),
                                     [('Reflector', .245, .315)])
        parts_box(parts, 'Firefighter_Boot_' + side, (x, -.115, .095), (.35, .47, .19), 'Charcoal', 'Foot.' + side, .075)
    # Hands land at the requested Blender coordinates (Unity x+/-0.13,y.88,z.24).
    hand_data = (
        ('L', (-.285, -.01, 1.01), (-.28, -.12, .92), (-.13, -.24, .88)),
        ('R', (.285, -.01, 1.01), (.28, -.12, .92), (.13, -.24, .88)),
    )
    for side, shoulder, elbow, hand in hand_data:
        parts_capsule(parts, 'Firefighter_UpperArm_' + side, shoulder, elbow, .155, 'Uniform', 'UpperArm.' + side)
        parts_capsule(parts, 'Firefighter_Forearm_' + side, elbow, hand, .122, 'Uniform', 'LowerArm.' + side)
        parts_ellipsoid(parts, 'Firefighter_Glove_' + side, hand, (.285, .245, .24), 'Charcoal', 'Hand.' + side)
    # Head deliberately reads much wider than the compact body at party-game
    # distance; it is the central silhouette cue from the supplied reference.
    parts_ellipsoid(parts, 'Firefighter_HeadMesh', (0, -.035, 1.285), (.90, .78, .72), 'Skin', 'Head')
    parts_ellipsoid(parts, 'Firefighter_EyeMesh_L', (-.165, -.43, 1.305), (.075, .032, .108), 'Eye', 'Head')
    parts_ellipsoid(parts, 'Firefighter_EyeMesh_R', (.165, -.43, 1.305), (.075, .032, .108), 'Eye', 'Head')
    parts_ellipsoid(parts, 'Firefighter_Nose', (0, -.44, 1.235), (.075, .038, .06), 'Skin', 'Head')
    # A domed crown and a deliberately thin rounded rim create a firefighter
    # helmet silhouette rather than the old shallow flying-saucer disk.
    # The crown is a closed, bottom-flat dome written bottom-to-top.  This
    # avoids both a full exposed ellipsoid (UFO look) and inward-facing lathe
    # normals in Unity.
    parts_lathe(parts, 'Firefighter_HelmetCrown',
                [(.4350, .0000), (.4313, .0418), (.4202, .0828), (.4019, .1225),
                 (.3767, .1600), (.3450, .1950), (.3076, .2263), (.2650, .2540),
                 (.2175, .2770), (.1665, .2960), (.1126, .3090), (.0568, .3173),
                 (.0001, .3200)],
                (0, -.005, 1.465), 'Helmet', 'Head', 28)
    parts_lathe(parts, 'Firefighter_HelmetBrim',
                [(.0, -.020), (.40, -.020), (.468, -.006), (.472, .008), (.43, .024), (.0, .024)],
                (0, -.055, 1.455), 'Helmet', 'Head', 28)
    return parts


def resident_geometry():
    parts = []
    # The shirt deliberately overlaps the trousers at the hips: it reads as
    # one compact torso, not as a bright underwear-like floating belt.
    parts_flat_hem_ellipsoid(parts, 'Resident_Body', (0, .018, .84), (.62, .46, .80), 'CivilianShirt', 'Chest')
    for side, x in (('L', -.18), ('R', .18)):
        # The resident has short, continuous trouser legs; all-pants material
        # keeps the silhouette like the supplied tiny green-shirt resident.
        leg = rounded_ellipsoid('Resident_PantsLeg_' + side, (x, .02, .345), (.30, .34, .50), 'CivilianPants')
        parts.append((leg, ('UpperLeg.' + side, 'LowerLeg.' + side, .29)))
        parts_box(parts, 'Resident_Shoe_' + side, (x, -.115, .095), (.34, .46, .19), 'Charcoal', 'Foot.' + side, .075)
    for side, x in (('L', -.285), ('R', .285)):
        shoulder, elbow, hand = (x, -.005, 1.0), (x * 1.18, -.02, .77), (x * 1.16, -.05, .60)
        parts_capsule(parts, 'Resident_UpperArm_' + side, shoulder, elbow, .13, 'CivilianShirt', 'UpperArm.' + side)
        parts_capsule(parts, 'Resident_Forearm_' + side, elbow, hand, .105, 'Skin', 'LowerArm.' + side)
        parts_ellipsoid(parts, 'Resident_Hand_' + side, hand, (.19, .17, .20), 'Skin', 'Hand.' + side)
    parts_ellipsoid(parts, 'Resident_HeadMesh', (0, -.035, 1.30), (.89, .78, .72), 'Skin', 'Head')
    # A lathed dome hugs the head.  The one small offset fringe makes an
    # asymmetric hairline without the former two loose pancake pieces.
    parts_asymmetric_hair_cap(parts, 'Resident_HairCap', (0, .01, 1.55), (.91, .80, .40), 'Hair', 'Head')
    for side, x in (('L', -.405), ('R', .405)):
        parts_ellipsoid(parts, 'Resident_Ear_' + side, (x, -.025, 1.255), (.10, .075, .14), 'Skin', 'Head')
    parts_ellipsoid(parts, 'Resident_EyeMesh_L', (-.165, -.43, 1.315), (.072, .032, .102), 'Eye', 'Head')
    parts_ellipsoid(parts, 'Resident_EyeMesh_R', (.165, -.43, 1.315), (.072, .032, .102), 'Eye', 'Head')
    parts_ellipsoid(parts, 'Resident_Nose', (0, -.442, 1.245), (.07, .037, .054), 'Skin', 'Head')
    return parts


def key_rot(pose, name, frame, rot=(0, 0, 0), loc=None, scale=None):
    b = pose.bones[name]
    b.rotation_mode = 'XYZ'
    b.rotation_euler = rot
    b.keyframe_insert('rotation_euler', frame=frame)
    if loc is not None:
        b.location = loc
        b.keyframe_insert('location', frame=frame)
    if scale is not None:
        b.scale = scale
        b.keyframe_insert('scale', frame=frame)


def key_neutral_pose(pose, frame):
    """Explicitly key every channel so actions cannot inherit another clip."""
    for bone in pose.bones:
        bone.rotation_mode = 'XYZ'
        bone.location = (0, 0, 0)
        bone.rotation_euler = (0, 0, 0)
        bone.scale = (1, 1, 1)
        bone.keyframe_insert('location', frame=frame)
        bone.keyframe_insert('rotation_euler', frame=frame)
        bone.keyframe_insert('scale', frame=frame)


def finish_action(action):
    action.frame_start, action.frame_end = (0, 60) if action.name == 'Idle' else (0, 24)
    # Blender 5.1's layered Action API no longer exposes ``action.fcurves``
    # directly.  Keyframes inserted above are stored in its slot/layer curves;
    # their default Bezier interpolation is the desired soft toy motion.
    return action


def create_actions(rig, firefighter):
    rig.animation_data_create()
    pose = rig.pose
    idle = bpy.data.actions.new('Idle')
    idle.use_fake_user = True
    rig.animation_data.action = idle
    for frame, lift in ((0, 0), (30, .009), (60, 0)):
        key_neutral_pose(pose, frame)
        # Bone local Y follows the vertical rest-bone axis, so this is a true
        # small upwards breathing bob in Blender's Z-up world.
        key_rot(pose, 'Chest', frame, rot=(0, 0, 0), loc=(0, lift, 0), scale=(1, 1, 1 + lift * .55))
        key_rot(pose, 'Head', frame, rot=(.018 if frame == 30 else 0, 0, 0))
        for side in ('L', 'R'):
            key_rot(pose, 'UpperArm.' + side, frame, rot=(.04 if firefighter else .015, 0, 0))
            key_rot(pose, 'LowerArm.' + side, frame, rot=(-.05 if firefighter else 0, 0, 0))
            key_rot(pose, 'UpperLeg.' + side, frame)
            key_rot(pose, 'LowerLeg.' + side, frame)
    finish_action(idle)
    run = bpy.data.actions.new('Run')
    run.use_fake_user = True
    rig.animation_data.action = run
    # Full contact -> passing (lift) -> opposite contact -> passing -> same
    # contact.  The lower leg and foot compensate the upper leg so a planted
    # boot reads as planted at frames 0/12, while the other boot visibly lifts.
    # This is deliberately in-place: Root remains completely unanimated.
    gait = {
        0:  { 'L': (.74, -.61, -.13), 'R': (-.48, 1.02, -.54), 'lift': 0.000 },
        6:  { 'L': (.18, .92, -.98),  'R': (-.20, .28, -.10), 'lift': 0.020 },
        12: { 'L': (-.48, 1.02, -.54), 'R': (.74, -.61, -.13), 'lift': 0.000 },
        18: { 'L': (-.20, .28, -.10), 'R': (.18, .92, -.98),  'lift': 0.020 },
        24: { 'L': (.74, -.61, -.13), 'R': (-.48, 1.02, -.54), 'lift': 0.000 },
    }
    for frame in (0, 6, 12, 18, 24):
        key_neutral_pose(pose, frame)
        bob = gait[frame]['lift']
        key_rot(pose, 'Hips', frame, loc=(0, bob, 0))
        # Clear forward run intent while the head counters just enough to keep
        # the friendly toy face readable; firefighter hands stay braced.
        key_rot(pose, 'Chest', frame, rot=(.10, 0, 0))
        key_rot(pose, 'Head', frame, rot=(-.075, 0, 0))
        for side, sign in (('L', 1), ('R', -1)):
            upper, lower, foot = gait[frame][side]
            key_rot(pose, 'UpperLeg.' + side, frame, rot=(upper, 0, 0))
            key_rot(pose, 'LowerLeg.' + side, frame, rot=(lower, 0, 0))
            key_rot(pose, 'Foot.' + side, frame, rot=(foot, 0, 0))
            if firefighter:
                # Both hands remain on the nozzle: only a tiny bracing sway.
                arm_sway = (upper * .11) if frame not in (6, 18) else (upper * .05)
                key_rot(pose, 'UpperArm.' + side, frame, rot=(.08 + arm_sway, 0, 0))
                key_rot(pose, 'LowerArm.' + side, frame, rot=(-.11, 0, 0))
            else:
                key_rot(pose, 'UpperArm.' + side, frame, rot=(-upper * .72, 0, 0))
                key_rot(pose, 'LowerArm.' + side, frame, rot=(.13, 0, 0))
    finish_action(run)
    rig.animation_data.action = idle
    return idle, run


def asset_bounds(meshes, evaluated=False):
    """Bounds from actual vertices, optionally after Armature deformation.

    AABB corners ignore skinned motion and caused v0.3 to report 1.60 m while
    Unity's baked Idle mesh measured 1.56 m.  Authoring normalisation must use
    the same evaluated surface the game sees.
    """
    if evaluated:
        depsgraph = bpy.context.evaluated_depsgraph_get()
        coords = []
        for o in meshes:
            evaluated_object = o.evaluated_get(depsgraph)
            evaluated_mesh = evaluated_object.to_mesh()
            coords.extend(evaluated_object.matrix_world @ vertex.co for vertex in evaluated_mesh.vertices)
            evaluated_object.to_mesh_clear()
    else:
        coords = [o.matrix_world @ Vector(v) for o in meshes for v in o.bound_box]
    return [min(v[i] for v in coords) for i in range(3)], [max(v[i] for v in coords) for i in range(3)]


def normalize_character(rig, meshes, target_height=1.60):
    """Uniformly scale the character + skeleton to 1.60 m with feet at Z=0."""
    # Explicitly evaluate the authored Idle first; assigning an Action does
    # not itself reset Blender's current pose/frame after Run key creation.
    rig.animation_data.action = next(action for action in bpy.data.actions if action.name == 'Idle')
    bpy.context.scene.frame_set(0)
    bpy.context.view_layer.update()
    lo, hi = asset_bounds(meshes, evaluated=True)
    scale = target_height / (hi[2] - lo[2])
    rig.scale = (scale, scale, scale)
    bpy.context.view_layer.update()
    lo, _ = asset_bounds(meshes, evaluated=True)
    rig.location.z -= lo[2]
    bpy.context.view_layer.update()


def ground_run_contacts(rig, meshes):
    """Bake a physically readable in-place run against the floor.

    The Root remains still.  We key only the Hips local vertical channel after
    measuring the evaluated boot/shoe surface on every Run frame.  Therefore
    each pose has a real supporting foot rather than an animation that merely
    *looks* like it might touch due to a static bounding box.
    """
    run = next(action for action in bpy.data.actions if action.name == 'Run')
    rig.animation_data.action = run
    hips = rig.pose.bones['Hips']
    for frame in range(int(run.frame_start), int(run.frame_end) + 1):
        bpy.context.scene.frame_set(frame)
        bpy.context.view_layer.update()
        floor_min = asset_bounds(meshes, evaluated=True)[0][2]
        vertical = ((rig.matrix_world @ hips.matrix).to_3x3() @ Vector((0, 1, 0))).z
        if abs(vertical) < .00001:
            raise RuntimeError('Hips local Y is not a usable vertical grounding axis')
        hips.location.y += (.003 - floor_min) / vertical
        hips.keyframe_insert('location', frame=frame)
    # The authored cycle already has symmetric end poses.  Re-evaluate both
    # ends after ground correction and make endpoint equality explicit.
    bpy.context.scene.frame_set(int(run.frame_start))
    start_y = hips.location.y
    bpy.context.scene.frame_set(int(run.frame_end))
    hips.location.y = start_y
    hips.keyframe_insert('location', frame=int(run.frame_end))
    bpy.context.scene.frame_set(0)
    bpy.context.view_layer.update()


def validate(asset, rig, meshes, markers, actions):
    # Asset height and ground origin are the neutral/Idle contract.  Run has
    # its own sampled support checks below and must not overwrite this measure.
    rig.animation_data.action = next(action for action in actions if action.name == 'Idle')
    bpy.context.scene.frame_set(0)
    bpy.context.view_layer.update()
    lo, hi = asset_bounds(meshes, evaluated=True)
    tris = 0
    invalid = []
    for o in meshes:
        o.data.calc_loop_triangles()
        tris += len(o.data.loop_triangles)
        if not o.vertex_groups or not any(m.type == 'ARMATURE' and m.object == rig for m in o.modifiers):
            invalid.append(o.name)
        elif any(not v.groups for v in o.data.vertices):
            invalid.append(o.name + ':unweighted')
    action_info = {}
    stable_root = True
    loop_endpoints = True
    run_geometry_checks = []
    for a in actions:
        rig.animation_data.action = a
        bpy.context.scene.frame_set(int(a.frame_start))
        bpy.context.view_layer.update()
        start_pose = {b.name: (tuple(round(v, 6) for v in b.location),
                               tuple(round(v, 6) for v in b.rotation_euler),
                               tuple(round(v, 6) for v in b.scale)) for b in rig.pose.bones}
        samples = []
        for frame in range(int(a.frame_start), int(a.frame_end) + 1):
            bpy.context.scene.frame_set(frame)
            root = rig.pose.bones['Root']
            samples.append((tuple(round(v, 6) for v in root.location),
                            tuple(round(v, 6) for v in root.rotation_euler)))
        bpy.context.scene.frame_set(int(a.frame_end))
        end_pose = {b.name: (tuple(round(v, 6) for v in b.location),
                             tuple(round(v, 6) for v in b.rotation_euler),
                             tuple(round(v, 6) for v in b.scale)) for b in rig.pose.bones}
        is_loop = start_pose == end_pose
        is_stable = all(s == samples[0] for s in samples)
        loop_endpoints &= is_loop
        stable_root &= is_stable
        action_info[a.name] = {'range': [a.frame_start, a.frame_end], 'slots': len(a.slots),
                               'has_animation_data': len(a.slots) > 0,
                               'loop_endpoints_match': is_loop, 'root_stable': is_stable}
        if a.name == 'Run':
            linked_pairs = [('UpperArm.L', 'LowerArm.L'), ('LowerArm.L', 'Hand.L'),
                            ('UpperArm.R', 'LowerArm.R'), ('LowerArm.R', 'Hand.R'),
                            ('UpperLeg.L', 'LowerLeg.L'), ('LowerLeg.L', 'Foot.L'),
                            ('UpperLeg.R', 'LowerLeg.R'), ('LowerLeg.R', 'Foot.R')]
            for frame in (6, 12):
                bpy.context.scene.frame_set(frame)
                bpy.context.view_layer.update()
                joint_gap = max((rig.matrix_world @ rig.pose.bones[child].head -
                                 rig.matrix_world @ rig.pose.bones[parent].tail).length
                                for parent, child in linked_pairs)
                mesh_min_z = asset_bounds(meshes, evaluated=True)[0][2]
                run_geometry_checks.append({'frame': frame, 'max_joint_gap_m': round(joint_gap, 6),
                                            'min_mesh_z_m': round(mesh_min_z, 6)})
    marker_locations = {m.name: [round(v, 4) for v in m.matrix_world.translation] for m in markers}
    eye_is_front = marker_locations[asset + '_Eye_L'][1] < marker_locations[asset + '_Head'][1]
    return {'asset': asset, 'triangles': tris, 'mesh_count': len(meshes), 'bone_count': len(rig.data.bones),
            'height_m': round(hi[2] - lo[2], 4), 'min_z': round(lo[2], 4),
            'valid_weighted_meshes': len(invalid) == 0, 'invalid_meshes': invalid,
            'actions': action_info, 'root_stable': stable_root,
            'loop_endpoints_match': loop_endpoints,
            'marker_world_locations': marker_locations,
            'eye_marker_is_in_front_of_head': eye_is_front,
            'run_geometry_checks': run_geometry_checks}


def save_export(asset, rig, parts, markers, actions):
    meshes = [o for o, _ in parts]
    for obj, bone in parts:
        if isinstance(bone, tuple):
            bind_blended_limb(obj, rig, bone[0], bone[1], bone[2])
        else:
            bind(obj, rig, bone)
    normalize_character(rig, meshes)
    ground_run_contacts(rig, meshes)
    report = validate(asset, rig, meshes, markers, actions)
    run_bad = any(check['max_joint_gap_m'] > .001 or check['min_mesh_z_m'] < -.001
                  for check in report['run_geometry_checks'])
    if (report['triangles'] > 12000 or abs(report['height_m'] - 1.60) > .002 or abs(report['min_z']) > .002
            or not report['valid_weighted_meshes'] or not report['root_stable']
            or not report['loop_endpoints_match'] or not report['eye_marker_is_in_front_of_head'] or run_bad):
        raise RuntimeError('Character validation failed: ' + json.dumps(report))
    # .blend is an asset source with no camera, floor, or studio lights.
    rig.animation_data.action = actions[0]
    bpy.context.scene.frame_set(0)
    bpy.ops.wm.save_as_mainfile(filepath=str(MODEL_DIR / (asset + '.blend')))
    bpy.ops.object.select_all(action='DESELECT')
    for obj in meshes + [rig] + markers:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.export_scene.fbx(filepath=str(FBX_DIR / (asset + '.fbx')), use_selection=True,
                             object_types={'MESH', 'ARMATURE', 'EMPTY'}, global_scale=1,
                             apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS',
                             axis_forward='-Z', axis_up='Y', use_mesh_modifiers=True,
                             mesh_smooth_type='FACE', bake_anim=True,
                             bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
                             bake_anim_step=1, add_leaf_bones=False)
    report['fbx_reimport'] = validate_exported_fbx(asset)
    return report


def validate_exported_fbx(asset):
    """Read the generated FBX back in and measure its real visible vertices."""
    reset_scene()
    bpy.ops.import_scene.fbx(filepath=str(FBX_DIR / (asset + '.fbx')))
    bpy.context.scene.frame_set(0)
    bpy.context.view_layer.update()
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    lo, hi = asset_bounds(meshes, evaluated=True)
    result = {'height_m': round(hi[2] - lo[2], 4), 'min_z': round(lo[2], 4),
              'mesh_count': len(meshes)}
    if abs(result['height_m'] - 1.60) > .002 or abs(result['min_z']) > .002:
        raise RuntimeError('FBX reimport validation failed: ' + json.dumps(result))
    return result


def build_asset(asset):
    reset_scene()
    # Blender keeps Action datablocks after object deletion.  Remove them so
    # each authored file contains exactly this character's Idle and Run clips.
    for action in list(bpy.data.actions):
        bpy.data.actions.remove(action)
    rig = make_rig(asset)
    is_firefighter = asset == 'Firefighter'
    parts = firefighter_geometry() if is_firefighter else resident_geometry()
    markers = [make_marker(asset + '_Head', (0, -.02, 1.31), rig, 'Head'),
               make_marker(asset + '_Eye_L', (-.14, -.34, 1.29), rig, 'Head')]
    actions = create_actions(rig, is_firefighter)
    report = save_export(asset, rig, parts, markers, actions)
    return report


def append_asset(filepath, x_offset):
    with bpy.data.libraries.load(str(filepath), link=False) as (src, dst):
        dst.objects = src.objects
        dst.actions = src.actions
    added = [o for o in dst.objects if o]
    for o in added:
        if o.name not in bpy.context.scene.objects:
            bpy.context.collection.objects.link(o)
    roots = [o for o in added if o.type == 'ARMATURE']
    for rig in roots:
        rig.location.x += x_offset
    return roots[0]


def point(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()


def setup_studio(output, frame=24):
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_EEVEE'
    scene.render.resolution_x, scene.render.resolution_y, scene.render.resolution_percentage = 920, 640, 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.filepath = str(output)
    scene.render.film_transparent = False
    # The reference uses clear toy red/yellow/teal blocks.  Standard view
    # transform keeps those authored palette colours from being AgX-washed.
    scene.view_settings.view_transform = 'Standard'
    scene.view_settings.exposure = -.75
    scene.world.color = (.30, .25, .20)
    world = scene.world
    world.use_nodes = True
    world.node_tree.nodes['Background'].inputs['Color'].default_value = (.32, .265, .205, 1)
    world.node_tree.nodes['Background'].inputs['Strength'].default_value = .28
    floor = box('StudioFloor', (0, 0, -.075), (8, 8, .10), 'Cream', .02)
    floor_mat = floor.data.materials[0]
    floor_bsdf = next(node for node in floor_mat.node_tree.nodes if node.type == 'BSDF_PRINCIPLED')
    floor_bsdf.inputs['Base Color'].default_value = (.49, .385, .275, 1)
    cam_data = bpy.data.cameras.new('StudioCamera')
    cam = bpy.data.objects.new('StudioCamera', cam_data)
    bpy.context.collection.objects.link(cam)
    cam.location = (3.4, -7.2, 3.15)
    point(cam, (0, 0, .82))
    cam.data.lens = 56
    scene.camera = cam
    for loc, energy, size in (((-3.5, -4.2, 5), 480, 4.0), ((3.5, -1.4, 3.5), 260, 3.0)):
        data = bpy.data.lights.new('StudioSoftbox', 'AREA')
        data.energy, data.shape, data.size = energy, 'DISK', size
        light = bpy.data.objects.new('StudioSoftbox', data)
        bpy.context.collection.objects.link(light)
        light.location = loc
        point(light, (0, 0, .8))
    # Each appended blend already stores its own Idle action as active.  Do not
    # assign by a global action name here: Blender Action slots belong to their
    # original armature owner and an accidental cross-rig assignment is invalid.
    scene.frame_set(frame)
    bpy.ops.render.render(write_still=True)
    # Deliberately remove temporary file at the end of the host process by not
    # exporting it; it is ignored/overwritten by the next authoring run.


def render_previews():
    reset_scene()
    # Switch away from Resident.blend before appending it.  This disposable
    # working file stays under Logs, never beside the deliverable source assets.
    bpy.ops.wm.save_as_mainfile(filepath=str(PREVIEW_WORK / 'rounded-preview-work.blend'))
    append_asset(MODEL_DIR / 'Firefighter.blend', -1.05)
    append_asset(MODEL_DIR / 'Resident.blend', 1.05)
    setup_studio(OUT / 'blender-characters.png', frame=24)


def render_run_previews():
    """Render four real poses from the authored Run actions for art review."""
    reset_scene()
    bpy.ops.wm.save_as_mainfile(filepath=str(PREVIEW_WORK / 'rounded-run-preview-work.blend'))
    firefighter = append_asset(MODEL_DIR / 'Firefighter.blend', -1.05)
    resident = append_asset(MODEL_DIR / 'Resident.blend', 1.05)
    for rig in (firefighter, resident):
        action = next((candidate for candidate in bpy.data.actions
                       if candidate.name.split('.')[0] == 'Run'
                       and any(slot.identifier == 'OB' + rig.name for slot in candidate.slots)), None)
        if action is None:
            raise RuntimeError('Could not find appended Run action for ' + rig.name + ': ' +
                               json.dumps([(candidate.name, [slot.identifier for slot in candidate.slots])
                                           for candidate in bpy.data.actions]))
        rig.animation_data.action = action
    # Build the studio once; repeating it per frame would stack lights and
    # make later poses falsely brighter than the early ones.
    setup_studio(OUT / 'blender-run-00.png', frame=0)
    for frame in (0, 6, 12, 18):
        bpy.context.scene.frame_set(frame)
        bpy.context.scene.render.filepath = str(OUT / ('blender-run-%02d.png' % frame))
        bpy.ops.render.render(write_still=True)


if __name__ == '__main__':
    reports = [build_asset('Firefighter'), build_asset('Resident')]
    render_previews()
    render_run_previews()
    final = {'version': 'v0.3', 'reference': 'user-supplied rounded fire-rescue plan image',
             'orientation': '-Y forward, Z up', 'characters': reports,
             'previews': ['blender-characters.png', 'blender-run-00.png', 'blender-run-06.png',
                          'blender-run-12.png', 'blender-run-18.png']}
    (OUT / 'character-v3-report.json').write_text(json.dumps(final, indent=2), encoding='utf-8')
    print('ROUNDED_CHARACTER_REPORT ' + json.dumps(final))
