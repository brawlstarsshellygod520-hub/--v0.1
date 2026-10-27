import bpy, json, math, os, sys
from mathutils import Vector

FBX = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', 'Source.fbx'))
OUT = os.path.dirname(os.path.abspath(__file__))
REPORT = os.path.join(OUT, "characters2_inspection.json")
PREVIEW = os.path.join(OUT, "characters2_overview.png")
FRONT_PREVIEW = os.path.join(OUT, "characters2_front_plus_x.png")
PARTS_PREVIEW = os.path.join(OUT, "characters2_parts_plus_x.png")

def vec(v): return [round(float(x), 6) for x in v]
def bounds(points):
    if not points: return None
    lo = [min(p[i] for p in points) for i in range(3)]
    hi = [max(p[i] for p in points) for i in range(3)]
    return {"min": vec(lo), "max": vec(hi), "size": vec([hi[i]-lo[i] for i in range(3)]),
            "center": vec([(lo[i]+hi[i])/2 for i in range(3)])}

def mesh_islands(mesh, matrix):
    # Vertex connectivity identifies independently recolorable/replaceable geometric regions.
    adjacent = [[] for _ in mesh.vertices]
    for e in mesh.edges:
        a,b = e.vertices[:]
        adjacent[a].append(b); adjacent[b].append(a)
    seen, groups = set(), []
    for start in range(len(mesh.vertices)):
        if start in seen: continue
        stack, group = [start], []
        seen.add(start)
        while stack:
            n = stack.pop(); group.append(n)
            for other in adjacent[n]:
                if other not in seen: seen.add(other); stack.append(other)
        pts = [matrix @ mesh.vertices[i].co for i in group]
        groups.append({"vertices": len(group), "bounds_world": bounds(pts)})
    return sorted(groups, key=lambda x: x["vertices"], reverse=True)

for obj in list(bpy.data.objects): bpy.data.objects.remove(obj, do_unlink=True)
bpy.ops.import_scene.fbx(filepath=FBX, use_custom_normals=True)

meshes = []
all_points = []
for obj in bpy.context.scene.objects:
    if obj.type != 'MESH': continue
    me = obj.data
    pts = [obj.matrix_world @ v.co for v in me.vertices]
    all_points += pts
    attrs = []
    for a in me.attributes:
        attrs.append({"name": a.name, "domain": a.domain, "type": a.data_type})
    mats = []
    for i, slot in enumerate(obj.material_slots):
        used = sum(1 for p in me.polygons if p.material_index == i)
        mats.append({"slot": i, "name": slot.material.name if slot.material else None, "polygons": used})
    meshes.append({
        "object": obj.name, "parent": obj.parent.name if obj.parent else None,
        "vertices": len(me.vertices), "edges": len(me.edges), "polygons": len(me.polygons),
        "bounds_world": bounds(pts), "materials": mats,
        "uv_layers": [{"name": uv.name, "data_points": len(uv.data)} for uv in me.uv_layers],
        "has_custom_normals": bool(me.has_custom_normals), "auto_smooth": getattr(me, 'use_auto_smooth', None),
        "attributes": attrs, "islands": mesh_islands(me, obj.matrix_world)
    })

armatures = []
for obj in bpy.context.scene.objects:
    if obj.type == 'ARMATURE':
        armatures.append({"object": obj.name, "bones": [{"name": b.name, "parent": b.parent.name if b.parent else None,
                         "head_local": vec(b.head_local), "tail_local": vec(b.tail_local)} for b in obj.data.bones],
                         "actions": [a.name for a in bpy.data.actions if any(fc.data_path.startswith('pose.bones') for fc in a.fcurves)]})

scene_bounds = bounds(all_points)
report = {"source": FBX, "scene_bounds_world": scene_bounds, "mesh_objects": meshes, "armatures": armatures,
          "all_actions": [{"name": a.name, "frame_range": [float(a.frame_range[0]), float(a.frame_range[1])], "fcurves": len(a.fcurves)} for a in bpy.data.actions],
          "axis_note": "FBX importer coordinates; dimensions alone cannot authoritatively identify facing direction. Preview camera is from world Y-negative toward origin."}
with open(REPORT, 'w', encoding='utf-8') as f: json.dump(report, f, ensure_ascii=False, indent=2)

# Neutral inspection render: preserves imported material colours, uses one diagonal camera.
if scene_bounds:
    c = Vector(scene_bounds['center']); s = Vector(scene_bounds['size']); radius = max(s.x, s.y, s.z, 1.0)
    bpy.ops.object.camera_add(location=c + Vector((radius*1.7, -radius*2.1, radius*1.25)))
    cam = bpy.context.object; bpy.context.scene.camera = cam
    def aim(obj, target): obj.rotation_euler = (Vector(target)-obj.location).to_track_quat('-Z', 'Y').to_euler()
    aim(cam, c + Vector((0,0,s.z*0.06)))
    bpy.ops.object.light_add(type='AREA', location=c + Vector((radius, -radius, radius*1.7)))
    bpy.context.object.data.energy = 1100; bpy.context.object.data.shape = 'DISK'; bpy.context.object.data.size = radius*1.8
    aim(bpy.context.object, c)
    bpy.ops.object.light_add(type='AREA', location=c + Vector((-radius, -radius*0.3, radius*.7)))
    bpy.context.object.data.energy = 550; bpy.context.object.data.size = radius
    aim(bpy.context.object, c)
    bpy.context.scene.render.engine = 'BLENDER_WORKBENCH'
    bpy.context.scene.display.shading.light = 'STUDIO'
    bpy.context.scene.display.shading.studio_light = 'paint.sl'
    bpy.context.scene.display.shading.color_type = 'MATERIAL'
    bpy.context.scene.render.resolution_x = 1000; bpy.context.scene.render.resolution_y = 700; bpy.context.scene.render.resolution_percentage = 100
    bpy.context.scene.render.image_settings.file_format = 'PNG'; bpy.context.scene.render.filepath = PREVIEW
    bpy.context.scene.render.film_transparent = False
    bpy.context.scene.world.color = (0.12, 0.12, 0.12)
    bpy.ops.render.render(write_still=True)
    # +X is the evidence-backed face direction: both eye islands project at the largest X extent.
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = radius * 1.34
    cam.location = c + Vector((radius * 2.5, 0, 0)); aim(cam, c + Vector((0, 0, s.z * 0.03)))
    bpy.context.scene.render.filepath = FRONT_PREVIEW
    bpy.ops.render.render(write_still=True)
    # Temporary object-ID colours make the otherwise all-white part boundaries inspectable.
    part_colours = [(0.1, 0.1, 0.1, 1), (0.82, 0.20, 0.20, 1), (0.16, 0.38, 0.85, 1),
                    (0.22, 0.72, 0.30, 1), (0.96, 0.72, 0.12, 1), (0.9, 0.35, 0.08, 1)]
    for i, obj in enumerate([o for o in bpy.context.scene.objects if o.type == 'MESH']): obj.color = part_colours[i]
    bpy.context.scene.display.shading.color_type = 'OBJECT'
    bpy.context.scene.render.filepath = PARTS_PREVIEW
    bpy.ops.render.render(write_still=True)
