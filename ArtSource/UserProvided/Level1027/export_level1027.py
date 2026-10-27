"""Read-only derived export. Keep both the downloaded and archived Blend untouched."""
import importlib.util
import bpy
import json
from mathutils import Vector
from pathlib import Path

HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location("level_export", HERE.parent / "Level1026/export_level1026.py")
exporter = importlib.util.module_from_spec(spec)
spec.loader.exec_module(exporter)
exporter.SOURCE = HERE / "Source.blend"
exporter.OUT = HERE / "level1027.geometry.json"
sources = list(bpy.context.scene.objects)
exporter.extend_door_cutters_in_memory(sources)
dg = bpy.context.evaluated_depsgraph_get()
wall = bpy.data.objects['wall']
points = [wall.matrix_world @ Vector(v) for v in wall.bound_box]
lo = Vector(tuple(min(v[i] for v in points) for i in range(3)))
hi = Vector(tuple(max(v[i] for v in points) for i in range(3)))
offset = Vector((-(lo.y+hi.y)*5, -lo.z*10+.1, (lo.x+hi.x)*5))
def convert(v): return Vector((v.y*10,v.z*10,-v.x*10))+offset
rows=[]
for obj in sorted(sources, key=lambda o:o.name):
    if obj.type!='MESH': continue
    ev=obj.evaluated_get(dg); mesh=ev.to_mesh(); mesh.calc_loop_triangles()
    verts=[convert(ev.matrix_world@v.co) for v in mesh.vertices]
    kind='wall' if obj.name=='wall' else 'door' if obj.name.startswith('door.') else 'handle' if obj.name.startswith('door handle') else 'other'
    control=obj.name.startswith('CTRL_') or obj.name=='door handle_1'
    rows.append(dict(name=obj.name,kind=kind,controlGeometry=control,vertices=[exporter.as_dict(v) for v in verts],triangles=[i for t in mesh.loop_triangles for i in (t.vertices[0],t.vertices[2],t.vertices[1])],bounds=exporter.raw_bounds(verts)))
    ev.to_mesh_clear()
data=dict(sourceSha256=exporter.sha256(exporter.SOURCE),translation=exporter.as_dict(offset),meshes=rows)
exporter.OUT.write_text(json.dumps(data,ensure_ascii=False),encoding='utf-8')
# Review image uses only user geometry. Wall cutaway is a presentation derivative.
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
palette={'wall':(.78,.73,.64,1),'door':(.46,.26,.12,1),'handle':(.22,.25,.28,1),'other':(.65,.7,.72,1)}
for row in rows:
    if row['controlGeometry']: continue
    me=bpy.data.meshes.new(row['name']); vv=[(v['x'],v['z'],min(v['y'],.85) if row['kind']=='wall' else v['y']) for v in row['vertices']]
    me.from_pydata(vv,[],[row['triangles'][i:i+3][::-1] for i in range(0,len(row['triangles']),3)])
    ob=bpy.data.objects.new(row['name'],me);bpy.context.scene.collection.objects.link(ob)
    mat=bpy.data.materials.new(row['name']);mat.diffuse_color=palette[row['kind']];me.materials.append(mat)
camdata=bpy.data.cameras.new('Review');cam=bpy.data.objects.new('Review',camdata);bpy.context.scene.collection.objects.link(cam)
cam.location=(0,-12,20);cam.rotation_euler=(Vector((0,0,0))-cam.location).to_track_quat('-Z','Y').to_euler();camdata.type='ORTHO';camdata.ortho_scale=18
s=bpy.context.scene;s.camera=cam;s.render.engine='BLENDER_WORKBENCH';s.display.shading.light='STUDIO';s.display.shading.color_type='MATERIAL';s.display.shading.show_shadows=True;s.display.shading.show_cavity=True
s.render.resolution_x=1000;s.render.resolution_y=850;s.render.resolution_percentage=100;s.render.filepath=str(HERE/'source-review.png');bpy.ops.render.render(write_still=True)
print('LEVEL1027_EXPORTED',len(rows),offset)
