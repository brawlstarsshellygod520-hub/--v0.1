"""Convert the user's actual evaluated meshes to Unity-native mesh data, without saving the .blend.

Run Blender with --disable-autoexec. No embedded text, driver script or addon is executed.
The original plan is preserved; the playable copy has open doors and .8m cutaway walls.
"""
import bpy
import bmesh
import json
from pathlib import Path
from mathutils import Vector, Matrix
from math import pi

ROOT = next(parent for parent in Path(__file__).resolve().parents if (parent / 'ProjectSettings').is_dir())
SCALE = 14.0
CUT_HEIGHT = .8
wall = bpy.data.objects['wall']
# This one saved cutter only reaches partway through the wall after Solidify.
# Deepen its local normal, not its doorway width, in this disposable export process.
# The source .blend remains untouched; the intended bathroom doorway becomes traversable.
bpy.data.objects['CTRL_Hole.004'].scale.y *= 3
bpy.context.view_layer.update()
# Saved cutters start ~3.8mm above the floor, leaving a solid strip across each
# doorway. Players step over it, but the existing floor-level NPC clearance query
# correctly treats it as blocked. Extend the holes below ground in the playable copy.
for cutter in bpy.data.objects:
    if cutter.name.startswith('CTRL_Hole'):
        matrix = cutter.matrix_world.copy()
        matrix.translation.z -= .01
        cutter.matrix_world = matrix
bpy.context.view_layer.update()
deps = bpy.context.evaluated_depsgraph_get()
wall_world = [wall.matrix_world @ v.co for v in wall.evaluated_get(deps).data.vertices]
center_x = (min(v.x for v in wall_world) + max(v.x for v in wall_world)) / 2
front_y = bpy.data.objects['Door_Group'].matrix_world.translation.y

def unity(v):
    return {'x': float(v.x), 'y': float(v.z), 'z': float(-v.y - 6)}

def normalized(v):
    return Vector(((v.x - center_x) * SCALE, (v.y - front_y) * SCALE, v.z * SCALE))

result = {'source': bpy.data.filepath, 'scale': SCALE, 'cutHeight': CUT_HEIGHT,
          'meshes': [], 'doors': []}
for source in list(bpy.context.scene.objects):
    if source.type != 'MESH':
        continue
    if source.name.startswith('CTRL_Hole'):
        corners = [source.matrix_world @ Vector(v) for v in source.bound_box]
        lo = Vector(tuple(min(v[i] for v in corners) for i in range(3)))
        hi = Vector(tuple(max(v[i] for v in corners) for i in range(3)))
        middle = normalized((lo + hi) / 2)
        middle.z = 0
        result['doors'].append({'name': source.name, 'center': unity(middle),
                                'alongX': hi.x - lo.x > hi.y - lo.y,
                                'width': max(hi.x - lo.x, hi.y - lo.y) * SCALE})
        continue
    if source.name != 'wall' and not (source.name == 'Door' or source.name.startswith('Door.') or source.name.startswith('DoorFrame')):
        continue
    obj = source.evaluated_get(deps)
    evaluated = obj.to_mesh()
    bm = bmesh.new()
    bm.from_mesh(evaluated)
    for v in bm.verts:
        v.co = source.matrix_world @ v.co
    leaf = source.name == 'Door' or source.name.startswith('Door.')
    if leaf:
        lo = Vector(tuple(min(v.co[i] for v in bm.verts) for i in range(3)))
        hi = Vector(tuple(max(v.co[i] for v in bm.verts) for i in range(3)))
        pivot = Vector((lo.x, (lo.y + hi.y) / 2, 0)) if hi.x-lo.x > hi.y-lo.y else Vector(((lo.x+hi.x)/2, lo.y, 0))
        rotation = Matrix.Rotation(pi / 2, 3, 'Z')
        for v in bm.verts:
            v.co = pivot + rotation @ (v.co - pivot)
    for v in bm.verts:
        v.co = normalized(v.co)
    if source.name == 'wall':
        # The source wall is already shorter than its door openings. Preserve its
        # authored top-face topology: filling each cut loop would seal room interiors.
        for v in bm.verts:
            v.co.z = min(v.co.z, CUT_HEIGHT)
    else:
        # Cut frames rather than flattening their overhead lintels into the doorway.
        bmesh.ops.bisect_plane(bm, geom=list(bm.verts)+list(bm.edges)+list(bm.faces),
                               dist=.00001, plane_co=(0, 0, CUT_HEIGHT), plane_no=(0, 0, 1),
                               clear_outer=True, clear_inner=False)
        cut_edges = [e for e in bm.edges if e.is_boundary and all(abs(v.co.z-CUT_HEIGHT)<.0001 for v in e.verts)]
        if cut_edges:
            bmesh.ops.holes_fill(bm, edges=cut_edges, sides=0)
    bmesh.ops.triangulate(bm, faces=list(bm.faces))
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    # Separate triangle vertices preserve the source's hard architectural edges.
    vertices = [unity(v.co) for face in bm.faces for v in face.verts]
    result['meshes'].append({'name': source.name, 'kind': 'door' if leaf else 'wall',
                              'vertices': vertices, 'triangles': list(range(len(vertices)))})
    bm.free()
    obj.to_mesh_clear()

output = ROOT / 'ArtSource/UserProvided/LevelOne/level-one.geometry.json'
output.parent.mkdir(parents=True, exist_ok=True)
output.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
print('LEVEL_ONE_EXPORT', json.dumps({'meshes': len(result['meshes']), 'doors': result['doors'],
                                     'triangles': sum(len(m['triangles'])//3 for m in result['meshes'])}))
