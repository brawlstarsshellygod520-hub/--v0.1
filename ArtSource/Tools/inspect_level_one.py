"""Read the supplied .blend without auto-execution; never save or alter the source file."""
import bpy
import json
from pathlib import Path
from mathutils import Vector

output = Path(r'D:/Documents/畢專/Docs/Validation/2026-10-19-greybox-step4')
output.mkdir(parents=True, exist_ok=True)
scene = bpy.context.scene
data = {'source': bpy.data.filepath, 'units': scene.unit_settings.system,
        'unit_scale': scene.unit_settings.scale_length, 'objects': []}
points = []
for obj in list(scene.objects):
    entry = {'name': obj.name, 'type': obj.type, 'location': list(obj.location),
             'scale': list(obj.scale), 'hidden': obj.hide_render}
    if obj.type == 'MESH':
        evaluated = obj.evaluated_get(bpy.context.evaluated_depsgraph_get())
        mesh = evaluated.to_mesh()
        verts = [list(obj.matrix_world @ v.co) for v in mesh.vertices]
        entry.update({'vertices': verts, 'polygons': [list(p.vertices) for p in mesh.polygons],
                      'bounds_min': [min(v[i] for v in verts) for i in range(3)],
                      'bounds_max': [max(v[i] for v in verts) for i in range(3)],
                      'materials': [m.name if m else None for m in obj.data.materials]})
        points.extend(Vector(v) for v in verts)
        evaluated.to_mesh_clear()
    data['objects'].append(entry)
(output / 'source-inspection.json').write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')
print('SOURCE_INSPECTION', json.dumps([{k: v for k, v in o.items() if k not in ('vertices', 'polygons')} for o in data['objects']], ensure_ascii=False))

if points:
    low = Vector(tuple(min(p[i] for p in points) for i in range(3)))
    high = Vector(tuple(max(p[i] for p in points) for i in range(3)))
    center = (low + high) / 2
    size = high - low
    for obj in list(scene.objects):
        if obj.type in ('CAMERA', 'LIGHT'):
            obj.hide_render = True
    camera_data = bpy.data.cameras.new('Inspection Camera')
    camera = bpy.data.objects.new('Inspection Camera', camera_data)
    scene.collection.objects.link(camera)
    camera_data.type = 'ORTHO'
    camera_data.ortho_scale = max(size.x, size.y) * 1.18
    camera.location = center + Vector((0, 0, max(size.x, size.y) * 2 + 10))
    camera.rotation_euler = (0, 0, 0)
    scene.camera = camera
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.display.shading.light = 'STUDIO'
    scene.display.shading.color_type = 'MATERIAL'
    scene.display.shading.show_shadows = True
    scene.display.shading.show_cavity = True
    scene.display.shading.background_type = 'WORLD'
    scene.world.color = (.18, .18, .18)
    scene.render.resolution_x = 1200
    scene.render.resolution_y = 1200
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.filepath = str(output / 'source-plan.png')
    bpy.ops.render.render(write_still=True)
