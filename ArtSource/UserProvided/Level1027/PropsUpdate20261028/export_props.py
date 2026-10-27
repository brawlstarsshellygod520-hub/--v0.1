"""Read-only snapshot export. Coordinates match the existing Level1027 wall origin."""
import bpy
import hashlib
import importlib.util
import json
from pathlib import Path
from mathutils import Vector

HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location("level_export", HERE.parent.parent / "Level1026/export_level1026.py")
helper = importlib.util.module_from_spec(spec)
spec.loader.exec_module(helper)
objects = list(bpy.context.scene.objects)
helper.extend_door_cutters_in_memory(objects)
depsgraph = bpy.context.evaluated_depsgraph_get()
wall = bpy.data.objects["wall"]
points = [wall.matrix_world @ Vector(v) for v in wall.bound_box]
lo = Vector(tuple(min(v[i] for v in points) for i in range(3)))
hi = Vector(tuple(max(v[i] for v in points) for i in range(3)))
offset = Vector((-(lo.y + hi.y) * 5, -lo.z * 10 + .1, (lo.x + hi.x) * 5))

def convert(v):
    return Vector((v.y * 10, v.z * 10, -v.x * 10)) + offset

rows = []
for obj in sorted(objects, key=lambda o: o.name):
    if obj.type != "MESH":
        continue
    evaluated = obj.evaluated_get(depsgraph)
    mesh = evaluated.to_mesh()
    mesh.calc_loop_triangles()
    vertices = [convert(evaluated.matrix_world @ v.co) for v in mesh.vertices]
    # Split corners retain Blender flat/smooth/custom normals instead of smoothing cube edges.
    normal_matrix = evaluated.matrix_world.to_3x3().inverted().transposed()
    render_vertices, render_normals = [], []
    for triangle in mesh.loop_triangles:
        for index in (triangle.loops[0], triangle.loops[2], triangle.loops[1]):
            render_vertices.append(helper.as_dict(vertices[mesh.loops[index].vertex_index]))
            n = normal_matrix @ mesh.corner_normals[index].vector
            render_normals.append(helper.as_dict(Vector((n.y, n.z, -n.x)).normalized()))
    kind = "wall" if obj.name == "wall" else "door" if obj.name.startswith("door.") else "handle" if obj.name.startswith("door handle") else "other"
    rows.append(dict(
        name=obj.name, kind=kind, controlGeometry=obj.name.startswith("CTRL_") or obj.name == "door handle_1",
        vertices=[helper.as_dict(v) for v in vertices],
        triangles=[i for t in mesh.loop_triangles for i in (t.vertices[0], t.vertices[2], t.vertices[1])],
        triangleMaterials=[t.material_index for t in mesh.loop_triangles],
        renderVertices=render_vertices, renderNormals=render_normals,
        materials=[dict(name=m.name, color=list(m.diffuse_color)) if m else dict(name="Unassigned", color=[.7,.7,.7,1]) for m in mesh.materials],
        bounds=helper.raw_bounds(vertices), parent=obj.parent.name if obj.parent else None,
    ))
    evaluated.to_mesh_clear()
source_hash = hashlib.sha256((HERE / "Source.blend").read_bytes()).hexdigest()
data = dict(sourceSha256=source_hash, translation=helper.as_dict(offset), meshes=rows)
(HERE / "props.geometry.json").write_text(json.dumps(data, ensure_ascii=False), encoding="utf-8")
for r in rows:
    print(json.dumps({k: r[k] for k in ("name", "parent", "bounds", "materials")}, ensure_ascii=False))
print("USER_PROPS_EXPORTED", len(rows), source_hash)
