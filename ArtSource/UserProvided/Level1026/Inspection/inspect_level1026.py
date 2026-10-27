"""Read-only inspection helper for 第一關1026 (1).blend.

Run with Blender in background mode.  It never saves the opened blend file;
all artifacts are written next to this script.
"""
import bpy
import csv
import hashlib
import json
import math
from pathlib import Path
from mathutils import Vector

OUT = Path(__file__).resolve().parent
SOURCE = OUT.parent / "Source.blend"
OLD_GEOMETRY = Path(r"D:/Documents/畢專/ArtSource/UserProvided/LevelOne/level-one.geometry.json")


def v3(v):
    return [round(float(v.x), 5), round(float(v.y), 5), round(float(v.z), 5)]


def bounds_for(obj, depsgraph):
    evaluated = obj.evaluated_get(depsgraph)
    mesh = evaluated.to_mesh()
    try:
        if not mesh.vertices:
            return None
        points = [evaluated.matrix_world @ vertex.co for vertex in mesh.vertices]
        lo = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
        hi = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
        return {"min": v3(lo), "max": v3(hi), "size": v3(hi - lo)}
    finally:
        evaluated.to_mesh_clear()


def classify(name):
    low = name.lower()
    tests = [
        ("door", ("door", "門")),
        ("window", ("window", "窗")),
        ("wall", ("wall", "牆")),
        ("floor", ("floor", "ground", "地板", "地面")),
        ("toilet", ("toilet", "馬桶")),
        ("room", ("room", "bed", "kitchen", "bath", "bedroom", "臥", "廚", "浴")),
    ]
    return [label for label, tokens in tests if any(token in low for token in tokens)] or ["unclassified"]


def old_summary():
    if not OLD_GEOMETRY.exists():
        return {"available": False, "path": str(OLD_GEOMETRY)}
    try:
        data = json.loads(OLD_GEOMETRY.read_text(encoding="utf-8"))
        parts = data.get("meshes", data.get("parts", data.get("meshParts", [])))
        doors = data.get("doors", [])
        old_points = []
        for part in parts:
            for point in part.get("vertices", []):
                if isinstance(point, dict) and all(k in point for k in ("x", "y", "z")):
                    old_points.append((point["x"], point["y"], point["z"]))
        old_bounds = None
        if old_points:
            old_bounds = {
                "min": [min(p[i] for p in old_points) for i in range(3)],
                "max": [max(p[i] for p in old_points) for i in range(3)],
            }
        return {
            "available": True,
            "path": str(OLD_GEOMETRY),
            "keys": sorted(data.keys()),
            "partCount": len(parts),
            "doorCount": len(doors),
            "scale": data.get("scale"),
            "cutHeight": data.get("cutHeight"),
            "partNames": [p.get("name", "") for p in parts][:50],
            "bounds": old_bounds,
        }
    except Exception as exc:
        return {"available": False, "path": str(OLD_GEOMETRY), "error": repr(exc)}


def look_at(obj, point):
    obj.rotation_euler = (Vector(point) - obj.location).to_track_quat("-Z", "Y").to_euler()


def make_preview(scene, overall, filename, angle):
    lo = Vector(overall["min"])
    hi = Vector(overall["max"])
    center = (lo + hi) * 0.5
    span_x, span_y, span_z = hi.x - lo.x, hi.y - lo.y, hi.z - lo.z
    radius = max(span_x, span_y, 1.0)
    camera_data = bpy.data.cameras.new("InspectionCamera")
    camera_data.type = "ORTHO"
    camera_data.ortho_scale = radius * (1.15 if angle == "top" else 1.35)
    camera = bpy.data.objects.new("InspectionCamera", camera_data)
    scene.collection.objects.link(camera)
    if angle == "top":
        camera.location = center + Vector((0, 0, radius * 2.0 + span_z + 1.0))
    else:
        camera.location = center + Vector((radius * 0.78, -radius * 0.92, radius * 0.92 + span_z))
    look_at(camera, center)
    scene.camera = camera
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.studio_light = "paint.sl"
    scene.display.shading.color_type = "MATERIAL"
    scene.display.shading.show_shadows = True
    scene.display.shading.show_cavity = True
    scene.display.shading.cavity_type = "WORLD"
    scene.display.shading.curvature_ridge_factor = 1.4
    scene.display.shading.curvature_valley_factor = 1.1
    scene.render.resolution_x = 1200
    scene.render.resolution_y = 1200
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = str(OUT / filename)
    scene.render.film_transparent = False
    scene.world.color = (0.075, 0.075, 0.075)
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(camera, do_unlink=True)
    bpy.data.cameras.remove(camera_data, do_unlink=True)


def make_unity_north_preview(scene):
    """Render derived JSON in Unity's orientation: +Z screen-up, +X screen-right."""
    geometry_path = OUT.parent / "level1026.geometry.json"
    if not geometry_path.exists():
        return
    data = json.loads(geometry_path.read_text(encoding="utf-8"))
    # Unity x/y/z becomes Blender x/z/y, so Blender top camera has Unity +Z up.
    vertices, faces = [], []
    for part in data["meshes"]:
        offset = len(vertices)
        vertices.extend((v["x"], v["z"], v["y"]) for v in part["vertices"])
        indices = part["triangles"]
        faces.extend((offset + indices[i], offset + indices[i + 1], offset + indices[i + 2]) for i in range(0, len(indices), 3))
    mesh = bpy.data.meshes.new("UnityCoordinatePreviewMesh")
    mesh.from_pydata(vertices, [], faces)
    preview = bpy.data.objects.new("UnityCoordinatePreviewMesh", mesh)
    scene.collection.objects.link(preview)
    material = bpy.data.materials.new("UnityPreviewMaterial")
    material.diffuse_color = (0.78, 0.78, 0.78, 1.0)
    preview.data.materials.append(material)
    label_objects = []
    label_mat = bpy.data.materials.new("UnityDoorLabelMaterial")
    label_mat.diffuse_color = (1.0, 0.55, 0.06, 1.0)
    for number, door in enumerate(data["doors"], 1):
        point = door["openingAnchor"]["position"]
        curve = bpy.data.curves.new("UnityDoorLabel", "FONT")
        curve.body = "D{}  ({:.1f}, {:.1f})".format(number, point["x"], point["z"])
        curve.align_x = "CENTER"
        curve.align_y = "CENTER"
        curve.size = 0.33
        text = bpy.data.objects.new("UnityDoorLabel", curve)
        # The z offset prevents z-fighting with the top edge of the frame.
        text.location = (point["x"], point["z"], 2.32)
        curve.materials.append(label_mat)
        scene.collection.objects.link(text)
        label_objects.append(text)
    bounds = data["bounds"]
    min_x, max_x = bounds["min"]["x"], bounds["max"]["x"]
    min_z, max_z = bounds["min"]["z"], bounds["max"]["z"]
    span = max(max_x - min_x, max_z - min_z)
    camera_data = bpy.data.cameras.new("UnityNorthCamera")
    camera_data.type = "ORTHO"
    camera_data.ortho_scale = span * 1.27
    camera = bpy.data.objects.new("UnityNorthCamera", camera_data)
    scene.collection.objects.link(camera)
    camera.location = ((min_x + max_x) * .5, (min_z + max_z) * .5, 30.0)
    # Camera local up is Blender +Y => screen-up is Unity +Z after the remap.
    look_at(camera, Vector(((min_x + max_x) * .5, (min_z + max_z) * .5, 0.0)))
    scene.camera = camera
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "MATERIAL"
    scene.display.shading.show_shadows = True
    scene.display.shading.show_cavity = True
    scene.render.resolution_x = 1400
    scene.render.resolution_y = 1400
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = str(OUT / "level1026-unity-north-top.png")
    scene.world.color = (0.07, 0.07, 0.07)
    bpy.ops.render.render(write_still=True)
    for text in label_objects:
        bpy.data.objects.remove(text, do_unlink=True)
    bpy.data.objects.remove(preview, do_unlink=True)
    bpy.data.meshes.remove(mesh, do_unlink=True)
    bpy.data.materials.remove(material, do_unlink=True)
    bpy.data.materials.remove(label_mat, do_unlink=True)
    bpy.data.cameras.remove(camera_data, do_unlink=True)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    depsgraph = bpy.context.evaluated_depsgraph_get()
    scene = bpy.context.scene
    mesh_rows = []
    all_lo, all_hi = [], []
    for obj in sorted((o for o in scene.objects if o.type == "MESH"), key=lambda o: o.name.lower()):
        bounds = bounds_for(obj, depsgraph)
        if bounds:
            all_lo.append(Vector(bounds["min"]))
            all_hi.append(Vector(bounds["max"]))
        mesh_rows.append({
            "name": obj.name,
            "type": obj.type,
            "classification": classify(obj.name),
            "parent": obj.parent.name if obj.parent else None,
            "collections": sorted(c.name for c in obj.users_collection),
            "boundsWorld": bounds,
            "location": v3(obj.matrix_world.translation),
            "rotationEulerDegrees": [round(math.degrees(x), 3) for x in obj.matrix_world.to_euler()],
            "materials": [slot.material.name if slot.material else None for slot in obj.material_slots],
            "meshStats": {
                "vertices": len(obj.data.vertices), "edges": len(obj.data.edges),
                "polygons": len(obj.data.polygons),
            },
        })
    if not all_lo:
        raise RuntimeError("No mesh objects found in opened blend.")
    low = Vector((min(v.x for v in all_lo), min(v.y for v in all_lo), min(v.z for v in all_lo)))
    high = Vector((max(v.x for v in all_hi), max(v.y for v in all_hi), max(v.z for v in all_hi)))
    overall = {"min": v3(low), "max": v3(high), "size": v3(high - low)}
    cameras = [{
        "name": o.name, "location": v3(o.matrix_world.translation),
        "rotationEulerDegrees": [round(math.degrees(x), 3) for x in o.matrix_world.to_euler()],
        "lens": o.data.lens, "type": o.data.type,
    } for o in scene.objects if o.type == "CAMERA"]
    source_hash = hashlib.sha256(SOURCE.read_bytes()).hexdigest()
    report = {
        "source": {"path": str(SOURCE), "sha256": source_hash, "bytes": SOURCE.stat().st_size},
        "scene": {
            "name": scene.name, "objectCount": len(scene.objects), "meshCount": len(mesh_rows),
            "units": {"system": scene.unit_settings.system, "scaleLength": scene.unit_settings.scale_length},
            "coordinateConvention": "Blender native: X/Y horizontal plan; Z vertical/up.",
            "cameras": cameras,
        },
        "overallMeshBoundsWorld": overall,
        "meshes": mesh_rows,
        "namedGroups": {
            "doors": [row["name"] for row in mesh_rows if "door" in row["classification"]],
            "windows": [row["name"] for row in mesh_rows if "window" in row["classification"]],
            "walls": [row["name"] for row in mesh_rows if "wall" in row["classification"]],
            "toilets": [row["name"] for row in mesh_rows if "toilet" in row["classification"]],
        },
        "oldLevelOneComparison": old_summary(),
    }
    (OUT / "level1026.inspection.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    with (OUT / "mesh-inventory.csv").open("w", newline="", encoding="utf-8-sig") as f:
        writer = csv.writer(f)
        writer.writerow(["name", "classification", "bounds min", "bounds max", "size", "materials", "vertices", "polygons"])
        for row in mesh_rows:
            b = row["boundsWorld"] or {}
            writer.writerow([row["name"], ";".join(row["classification"]), b.get("min"), b.get("max"), b.get("size"), ";".join(str(x) for x in row["materials"]), row["meshStats"]["vertices"], row["meshStats"]["polygons"]])
    (OUT / "source-sha256.txt").write_text(source_hash + "  " + str(SOURCE) + "\n", encoding="utf-8")
    make_preview(scene, overall, "level1026-top.png", "top")
    make_preview(scene, overall, "level1026-angled.png", "angled")
    make_unity_north_preview(scene)
    print("INSPECTION_OK", OUT)


if __name__ == "__main__":
    main()
