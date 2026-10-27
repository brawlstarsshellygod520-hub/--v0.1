import bpy
import json
import os
from mathutils import Vector

SOURCE = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', 'Source.fbx'))
OUT = os.path.dirname(os.path.abspath(__file__))
REPORT = os.path.join(OUT, "character1_inspection.json")
FRONT = os.path.join(OUT, "character1_whitebox_front.png")
THREE_QUARTER = os.path.join(OUT, "character1_whitebox_3quarter.png")

def rounded(v):
    return [round(float(x), 6) for x in v]

def bounds(points):
    if not points:
        return None
    low = [min(p[i] for p in points) for i in range(3)]
    high = [max(p[i] for p in points) for i in range(3)]
    return {
        "min": rounded(low), "max": rounded(high),
        "size": rounded([high[i] - low[i] for i in range(3)]),
        "center": rounded([(low[i] + high[i]) / 2 for i in range(3)]),
    }

def aim(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

for obj in list(bpy.data.objects):
    bpy.data.objects.remove(obj, do_unlink=True)
bpy.ops.import_scene.fbx(filepath=SOURCE, use_custom_normals=True)

all_points, meshes, armatures = [], [], []
for obj in bpy.context.scene.objects:
    if obj.type != 'MESH':
        continue
    mesh = obj.data
    points = [obj.matrix_world @ vert.co for vert in mesh.vertices]
    all_points.extend(points)
    material_slots = []
    for index, slot in enumerate(obj.material_slots):
        material_slots.append({
            "slot": index,
            "name": slot.material.name if slot.material else None,
            "polygons": sum(poly.material_index == index for poly in mesh.polygons),
        })
    meshes.append({
        "object": obj.name, "parent": obj.parent.name if obj.parent else None,
        "vertices": len(mesh.vertices), "edges": len(mesh.edges), "polygons": len(mesh.polygons),
        "bounds_world": bounds(points), "materials": material_slots,
        "uv_layers": [{"name": uv.name, "data_points": len(uv.data)} for uv in mesh.uv_layers],
        "vertex_groups": [{"name": group.name, "weights": sum(group.index in [g.group for g in v.groups] for v in mesh.vertices)} for group in obj.vertex_groups],
    })

for obj in bpy.context.scene.objects:
    if obj.type == 'ARMATURE':
        armatures.append({
            "object": obj.name,
            "bones": [{"name": bone.name, "parent": bone.parent.name if bone.parent else None,
                       "head_local": rounded(bone.head_local), "tail_local": rounded(bone.tail_local)}
                      for bone in obj.data.bones],
            "active_action": obj.animation_data.action.name if obj.animation_data and obj.animation_data.action else None,
        })

scene_bounds = bounds(all_points)
report = {
    "source": SOURCE,
    "scene_bounds_world": scene_bounds,
    "mesh_objects": meshes,
    "armatures": armatures,
    # Blender 5 action internals changed; record names only instead of querying legacy fcurves.
    "action_names": [action.name for action in bpy.data.actions],
    "animation_note": "No actions are authored or modified by this inspection.",
    "coordinate_note": "FBX-importer coordinates. Preview cameras cover +X and +X/+Y; use the visible face anatomy to confirm front.",
}
with open(REPORT, "w", encoding="utf-8") as handle:
    json.dump(report, handle, ensure_ascii=False, indent=2)

if scene_bounds:
    center = Vector(scene_bounds["center"])
    size = Vector(scene_bounds["size"])
    radius = max(size.x, size.y, size.z, 1.0)
    target = center + Vector((0, 0, size.z * 0.03))
    bpy.ops.object.camera_add(location=center + Vector((radius * 2.6, 0, 0)))
    camera = bpy.context.object
    bpy.context.scene.camera = camera
    camera.data.type = 'ORTHO'
    camera.data.ortho_scale = max(size.z * 1.12, size.x * 1.45, size.y * 1.45)
    aim(camera, target)
    bpy.context.scene.render.engine = 'BLENDER_WORKBENCH'
    bpy.context.scene.display.shading.light = 'STUDIO'
    bpy.context.scene.display.shading.studio_light = 'paint.sl'
    bpy.context.scene.display.shading.color_type = 'SINGLE'
    bpy.context.scene.display.shading.single_color = (0.72, 0.72, 0.72)
    bpy.context.scene.display.shading.show_shadows = True
    bpy.context.scene.display.shading.show_cavity = True
    bpy.context.scene.display.shading.cavity_type = 'WORLD'
    bpy.context.scene.render.resolution_x = 900
    bpy.context.scene.render.resolution_y = 900
    bpy.context.scene.render.resolution_percentage = 100
    bpy.context.scene.render.image_settings.file_format = 'PNG'
    bpy.context.scene.world.color = (0.075, 0.075, 0.075)
    bpy.context.scene.render.filepath = FRONT
    bpy.ops.render.render(write_still=True)
    camera.location = center + Vector((radius * 2.05, radius * 2.05, radius * 0.45))
    aim(camera, target)
    bpy.context.scene.render.filepath = THREE_QUARTER
    bpy.ops.render.render(write_still=True)
