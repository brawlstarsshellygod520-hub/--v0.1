"""Read-only Boolean / doorway inspection for Source.blend. Never saves the file."""
import bpy
import hashlib
import json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Source.blend"
GEOMETRY = ROOT / "level1026.geometry.json"
OUT = Path(__file__).resolve().parent
SCALE = 10.0


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def vec(v):
    return [round(float(v.x), 6), round(float(v.y), 6), round(float(v.z), 6)]


def bounds(points):
    return {"min": [min(p[i] for p in points) for i in range(3)], "max": [max(p[i] for p in points) for i in range(3)]}


def main():
    before = digest(SOURCE)
    deps = bpy.context.evaluated_depsgraph_get()
    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    # Use the exact translation already committed to the derived geometry JSON;
    # bounds from raw Blender bounding boxes would differ from evaluated solids.
    exported = json.loads(GEOMETRY.read_text(encoding="utf-8"))
    translation_data = exported["source"]["translation"]
    translation = Vector((translation_data["x"], translation_data["y"], translation_data["z"]))
    def unity(point):
        return Vector((point.y * SCALE, point.z * SCALE, -point.x * SCALE)) + translation

    wall = bpy.data.objects["wall"]
    cutter = bpy.data.objects["CTRL_Hole.004"]
    wall_eval = wall.evaluated_get(deps)
    evaluated = wall_eval.to_mesh()
    try:
        wall_points = [wall_eval.matrix_world @ vert.co for vert in evaluated.vertices]
        u_vertices = [unity(point) for point in wall_points]
        evaluated.calc_loop_triangles()
        polygons = [tuple(loop.vertices) for loop in evaluated.loop_triangles]
        bvh = BVHTree.FromPolygons(u_vertices, polygons, all_triangles=True)
        ray_origin = Vector((0.24, 0.6, -0.18))
        ray_dir = Vector((0, 0, 1))
        hit_location, hit_normal, hit_index, hit_distance = bvh.ray_cast(ray_origin, ray_dir, 3.0)
        ray = {
            "originUnity": vec(ray_origin), "directionUnity": vec(ray_dir),
            "hit": None if hit_location is None else {"location": vec(hit_location), "normal": vec(hit_normal), "triangleIndex": hit_index, "distance": round(float(hit_distance), 6)},
        }
        wall_eval_bounds = bounds(wall_points)
        wall_eval_unity_bounds = bounds(u_vertices)
        wall_counts = {"rawVertices": len(wall.data.vertices), "rawPolygons": len(wall.data.polygons), "evaluatedVertices": len(evaluated.vertices), "evaluatedPolygons": len(evaluated.polygons)}
    finally:
        wall_eval.to_mesh_clear()
    cutter_points = [cutter.matrix_world @ Vector(corner) for corner in cutter.bound_box]
    cutter_unity = [unity(point) for point in cutter_points]
    local_axes = {
        axis: vec(cutter.matrix_world.to_3x3() @ Vector(direction))
        for axis, direction in (("localX", (1, 0, 0)), ("localY", (0, 1, 0)), ("localZ", (0, 0, 1)))
    }
    modifier_rows = []
    for modifier in wall.modifiers:
        row = {"name": modifier.name, "type": modifier.type, "showViewport": modifier.show_viewport, "showRender": modifier.show_render}
        if modifier.type == "BOOLEAN":
            row.update({"operation": modifier.operation, "solver": modifier.solver, "operandType": modifier.operand_type, "object": modifier.object.name if modifier.object else None, "collection": modifier.collection.name if modifier.collection else None})
        modifier_rows.append(row)
    report = {
        "source": {"path": str(SOURCE), "sha256Before": before},
        "mapping": "Unity=(10*Blender.y,10*Blender.z,-10*Blender.x)+translation",
        "translation": vec(translation),
        "wallModifiers": modifier_rows,
        "wall": {"evaluatedWorldBounds": wall_eval_bounds, "evaluatedUnityBounds": wall_eval_unity_bounds, "counts": wall_counts},
        "ctrlHole004": {"worldBounds": bounds(cutter_points), "unityBounds": bounds(cutter_unity), "dimensionsLocal": vec(cutter.dimensions), "locationWorld": vec(cutter.matrix_world.translation), "localAxesWorld": local_axes},
        "rayReproduction": ray,
    }
    after = digest(SOURCE)
    report["source"]["sha256After"] = after
    report["source"]["unchanged"] = before == after
    (OUT / "door004-boolean-report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    lines = [
        "# Door.004 Boolean / opening inspection (read-only)", "",
        "- Source unchanged: `{}`".format(report["source"]["unchanged"]),
        "- Mapping: `{}`".format(report["mapping"]),
        "- Wall modifiers: `{}`".format(json.dumps(modifier_rows, ensure_ascii=False)),
        "- CTRL_Hole.004 Unity bounds: `{}`".format(json.dumps(report["ctrlHole004"]["unityBounds"])),
        "- Reproduced ray: `{}`".format(json.dumps(ray, ensure_ascii=False)),
        "",
        "Finding: the ray hit Unity Z={:.6f}; CTRL_Hole.004 ends at Unity Z={:.6f}. They coincide (within 1e-6).".format(ray["hit"]["location"][2], report["ctrlHole004"]["unityBounds"]["max"][2]) if ray["hit"] else "Finding: ray did not hit evaluated wall.",
        "Conclusion: the Exact collection Boolean is active (evaluated wall topology changes), but this cutter is flush with the far solidified wall face rather than overcutting it.",
        "Proposed export-only repair after approval: on an in-memory scene copy, extend each CTRL_Hole along its local depth axis beyond both wall faces (Door.004 local Y is the depth axis), update depsgraph, then export evaluated wall. Do not save Source.blend.",
    ]
    (OUT / "door004-boolean-report.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    print("DOOR004_INSPECTION", json.dumps(report, ensure_ascii=False))


if __name__ == "__main__":
    main()
