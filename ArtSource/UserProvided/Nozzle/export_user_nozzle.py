"""Inspect, render, and export the user-supplied handheld nozzle without saving it.

Run from the project root with Blender background mode and --disable-autoexec:
  D:/Blender/blender.exe --background --disable-autoexec --python ArtSource/UserProvided/Nozzle/export_user_nozzle.py -- --inspect --source-preview

The orientation constants are deliberately not populated until the source preview
has been reviewed.  The script only writes the copied source's inspection, a
render, and the final FBX; it never saves the opened .blend.
"""

import argparse
import hashlib
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Vector


HERE = Path(__file__).resolve().parent
SOURCE = HERE / "source.blend"
INSPECTION = HERE / "inspection.json"
METADATA = HERE / "nozzle-metadata.json"
SOURCE_PREVIEW = HERE / "source_preview.png"
EXPORT_PREVIEW = HERE / "export_preview.png"
FBX_REIMPORT_PREVIEW = HERE / "fbx_reimport_preview.png"
PROJECT = next(parent for parent in HERE.parents if (parent / "ProjectSettings").is_dir())
FBX = PROJECT / "Assets" / "FireRescue" / "Art" / "UserProvided" / "Tools" / "Nozzle" / "nozzle.fbx"
TARGET_LENGTH_METRES = 0.55

# Established from source_preview.png: the visible open muzzle is the source
# world -Y endpoint, and the pistol grip descends along source -Z.  The pivot
# is the visual centre of that grip, so held transforms originate at the hand.
SOURCE_NOZZLE_DIRECTION = (0.0, -1.0, 0.0)
SOURCE_UP_DIRECTION = (0.0, 0.0, 1.0)
SOURCE_GRIP_POINT = (0.0, -0.20, 0.48)


def cli_args():
    return sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []


def number(value):
    return round(float(value), 6)


def vector(value):
    return [number(component) for component in value]


def source_sha256():
    digest = hashlib.sha256()
    with SOURCE.open("rb") as source_file:
        for block in iter(lambda: source_file.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest().upper()


def source_meshes():
    return [obj for obj in bpy.context.scene.objects if obj.type == "MESH" and obj.visible_get()]


def evaluated_mesh(obj, depsgraph):
    return bpy.data.meshes.new_from_object(obj.evaluated_get(depsgraph), depsgraph=depsgraph)


def world_points(objects):
    depsgraph = bpy.context.evaluated_depsgraph_get()
    points = []
    for obj in objects:
        mesh = evaluated_mesh(obj, depsgraph)
        try:
            points.extend(obj.matrix_world @ vertex.co for vertex in mesh.vertices)
        finally:
            bpy.data.meshes.remove(mesh)
    if not points:
        raise RuntimeError("The source blend has no visible mesh vertices.")
    return points


def bounds(points):
    low = Vector((min(point.x for point in points), min(point.y for point in points), min(point.z for point in points)))
    high = Vector((max(point.x for point in points), max(point.y for point in points), max(point.z for point in points)))
    return low, high


def tri_count(mesh):
    return sum(max(0, len(poly.vertices) - 2) for poly in mesh.polygons)


def inspect():
    objects = source_meshes()
    if not objects:
        raise RuntimeError("No visible mesh objects found in source blend.")
    depsgraph = bpy.context.evaluated_depsgraph_get()
    report_objects = []
    materials = set()
    for obj in objects:
        baked = evaluated_mesh(obj, depsgraph)
        try:
            evaluated_vertices = len(baked.vertices)
            evaluated_triangles = tri_count(baked)
        finally:
            bpy.data.meshes.remove(baked)
        material_names = [slot.material.name if slot.material else None for slot in obj.material_slots]
        materials.update(name for name in material_names if name)
        report_objects.append({
            "name": obj.name,
            "mesh": obj.data.name,
            "vertices": len(obj.data.vertices),
            "triangles": tri_count(obj.data),
            "evaluated_vertices": evaluated_vertices,
            "evaluated_triangles": evaluated_triangles,
            "materials": material_names,
            "world_location": vector(obj.matrix_world.translation),
            "local_dimensions": vector(obj.dimensions),
            "modifiers": [{"name": mod.name, "type": mod.type, "show_render": bool(mod.show_render)} for mod in obj.modifiers],
        })
    points = world_points(objects)
    low, high = bounds(points)
    dimensions = high - low
    data = {
        "source": str(SOURCE),
        "source_sha256": source_sha256(),
        "scene": bpy.context.scene.name,
        "unit_system": bpy.context.scene.unit_settings.system,
        "unit_scale": number(bpy.context.scene.unit_settings.scale_length),
        "mesh_count": len(objects),
        "objects": report_objects,
        "materials": sorted(materials),
        "world_bounds_min": vector(low),
        "world_bounds_max": vector(high),
        "world_dimensions": vector(dimensions),
        "world_triangles_evaluated": sum(item["evaluated_triangles"] for item in report_objects),
        "orientation_status": "pending source preview review; export is intentionally gated",
    }
    INSPECTION.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print("INSPECTION_WRITTEN", INSPECTION)
    print(json.dumps(data, ensure_ascii=False))
    return data


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def material_viewport_colors(objects):
    for obj in objects:
        for material in obj.data.materials:
            if material and material.use_nodes:
                shader = next((node for node in material.node_tree.nodes if node.type == "BSDF_PRINCIPLED"), None)
                if shader:
                    material.diffuse_color = shader.inputs["Base Color"].default_value


def prepare_preview_scene(name, output, objects, camera_y_sign=-1.0):
    points = world_points(objects)
    low, high = bounds(points)
    centre = (low + high) * 0.5
    diagonal = max((high - low).length, 0.001)
    scene = bpy.data.scenes.new(name)
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.studio_light = "paint.sl"
    scene.display.shading.color_type = "MATERIAL"
    scene.display.shading.show_shadows = True
    scene.display.shading.show_cavity = True
    scene.display.shading.cavity_type = "WORLD"
    scene.world = bpy.data.worlds.new(name + " World")
    scene.world.color = (0.16, 0.14, 0.11)
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 900
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = str(output)
    camera_data = bpy.data.cameras.new(name + " Camera")
    camera_data.type = "ORTHO"
    camera_data.ortho_scale = diagonal * 1.38
    camera = bpy.data.objects.new(name + " Camera", camera_data)
    scene.collection.objects.link(camera)
    camera.location = centre + Vector((diagonal * 1.15, diagonal * 1.45 * camera_y_sign, diagonal * 0.9))
    look_at(camera, centre)
    scene.camera = camera
    return scene, centre, diagonal


def render_source_preview():
    objects = source_meshes()
    material_viewport_colors(objects)
    scene, _, _ = prepare_preview_scene("UserNozzleSourcePreview", SOURCE_PREVIEW, objects)
    for obj in objects:
        duplicate = obj.copy()
        duplicate.data = obj.data.copy()
        scene.collection.objects.link(duplicate)
    bpy.context.window.scene = scene
    bpy.ops.render.render(write_still=True)
    bpy.data.scenes.remove(scene, do_unlink=True)
    print("SOURCE_PREVIEW_WRITTEN", SOURCE_PREVIEW)


def export_transform():
    if None in (SOURCE_NOZZLE_DIRECTION, SOURCE_UP_DIRECTION, SOURCE_GRIP_POINT):
        raise RuntimeError("Export is blocked until source_preview.png has been reviewed and orientation constants are set.")
    forward = Vector(SOURCE_NOZZLE_DIRECTION).normalized()
    up = Vector(SOURCE_UP_DIRECTION).normalized()
    right = forward.cross(up).normalized()
    up = right.cross(forward).normalized()
    # Imported Unity axes: Blender +Y -> Unity +Z and Blender +Z -> Unity +Y.
    # This matrix sends the visually confirmed source nozzle axis to Blender +Y
    # and source up to Blender +Z before the FBX coordinate conversion.
    rotation = Matrix(((right.x, right.y, right.z, 0), (forward.x, forward.y, forward.z, 0), (up.x, up.y, up.z, 0), (0, 0, 0, 1)))
    points = world_points(source_meshes())
    projected = [(point - Vector(SOURCE_GRIP_POINT)).dot(forward) for point in points]
    length = max(projected) - min(projected)
    scale = TARGET_LENGTH_METRES / length
    return Matrix.Scale(scale, 4) @ rotation @ Matrix.Translation(-Vector(SOURCE_GRIP_POINT)), length, scale


def export_asset():
    transform, source_length, scale = export_transform()
    objects = source_meshes()
    depsgraph = bpy.context.evaluated_depsgraph_get()
    scene = bpy.data.scenes.new("UserNozzleExport")
    bpy.context.window.scene = scene
    baked = []
    for original in objects:
        mesh = evaluated_mesh(original, depsgraph)
        obj = bpy.data.objects.new(original.name, mesh)
        scene.collection.objects.link(obj)
        obj.matrix_world = transform @ original.matrix_world
        baked.append(obj)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in baked:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = baked[0]
    bpy.ops.object.join()
    root = bpy.context.view_layer.objects.active
    root.name = "UserNozzle"
    root.data.name = "UserNozzleMesh"
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    root.location = (0, 0, 0)
    root.rotation_euler = (0, 0, 0)
    root.scale = (1, 1, 1)
    FBX.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=str(FBX), use_selection=True, use_active_collection=False, object_types={"MESH"}, use_mesh_modifiers=True, add_leaf_bones=False, bake_anim=False, axis_forward="-Z", axis_up="Y")
    points = world_points([root])
    low, high = bounds(points)
    # With the confirmed transform, Blender +Y is the nozzle direction.  Use a
    # narrow end band to find the physical centres of the opening and inlet,
    # rather than substituting the model bounds corner for either anchor.
    root_points = [root.matrix_world @ vertex.co for vertex in root.data.vertices]
    root_length = high.y - low.y
    band_width = max(root_length * 0.025, 0.001)
    muzzle_band = [point for point in root_points if point.y >= high.y - band_width]
    inlet_band = [point for point in root_points if point.y <= low.y + band_width]
    if not muzzle_band or not inlet_band:
        raise RuntimeError("Could not derive muzzle and hose-inlet anchor bands.")
    muzzle_blender = sum(muzzle_band, Vector()) / len(muzzle_band)
    inlet_blender = sum(inlet_band, Vector()) / len(inlet_band)
    # FBX conversion used below maps Blender (X, Y forward, Z up) to Unity
    # (X, Y up, Z forward).  These values are directly usable by the runtime.
    unity_point = lambda point: [number(point.x), number(point.z), number(point.y)]
    metadata = {
        "coordinateSystem": "Unity local coordinates: Y-up, +Z-forward",
        "muzzle": unity_point(muzzle_blender),
        "hoseInlet": unity_point(inlet_blender),
        "grip": [0.0, 0.0, 0.0],
        "lengthMeters": number(root_length),
        "triangleCount": tri_count(root.data),
        "sourceSha256": source_sha256(),
    }
    METADATA.write_text(json.dumps(metadata, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    data = json.loads(INSPECTION.read_text(encoding="utf-8"))
    data["orientation_status"] = "reviewed source preview and exported"
    data["source_orientation"] = {"nozzle_direction": vector(SOURCE_NOZZLE_DIRECTION), "up_direction": vector(SOURCE_UP_DIRECTION), "grip_point": vector(SOURCE_GRIP_POINT)}
    data["export"] = {"fbx": str(FBX), "metadata": str(METADATA), "target_length_metres": TARGET_LENGTH_METRES, "source_axis_length": number(source_length), "uniform_scale": number(scale), "unity_axes": {"nozzle": "+Z", "up": "+Y"}, "export_bounds_blender": {"min": vector(low), "max": vector(high), "dimensions": vector(high - low)}, "muzzle_unity_local": metadata["muzzle"], "hose_inlet_unity_local": metadata["hoseInlet"], "vertices": len(root.data.vertices), "triangles": tri_count(root.data)}
    INSPECTION.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    material_viewport_colors([root])
    # The exported Blender +Y is the Unity +Z muzzle direction; review it from
    # that direction so the open nozzle is directly visible in the evidence.
    preview, _, _ = prepare_preview_scene("UserNozzleExportPreview", EXPORT_PREVIEW, [root], camera_y_sign=1.0)
    duplicate = root.copy()
    duplicate.data = root.data.copy()
    preview.collection.objects.link(duplicate)
    bpy.context.window.scene = preview
    bpy.ops.render.render(write_still=True)
    bpy.data.scenes.remove(preview, do_unlink=True)
    print("FBX_WRITTEN", FBX)
    print("EXPORT_PREVIEW_WRITTEN", EXPORT_PREVIEW)


def validate_fbx():
    if not FBX.exists():
        raise RuntimeError(f"FBX does not exist: {FBX}")
    # This command is run from a fresh background Blender process.  Importing
    # the emitted FBX verifies its on-disk axes and geometry, and does not
    # touch either source blend file.
    for obj in list(bpy.context.scene.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    bpy.ops.import_scene.fbx(filepath=str(FBX))
    imported = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if not imported:
        raise RuntimeError("FBX re-import produced no mesh objects.")
    points = world_points(imported)
    low, high = bounds(points)
    triangle_total = sum(tri_count(obj.data) for obj in imported)
    material_viewport_colors(imported)
    preview, _, _ = prepare_preview_scene("UserNozzleFbxReimportPreview", FBX_REIMPORT_PREVIEW, imported, camera_y_sign=1.0)
    for obj in imported:
        duplicate = obj.copy()
        duplicate.data = obj.data.copy()
        preview.collection.objects.link(duplicate)
    bpy.context.window.scene = preview
    bpy.ops.render.render(write_still=True)
    bpy.data.scenes.remove(preview, do_unlink=True)
    data = json.loads(INSPECTION.read_text(encoding="utf-8"))
    data["fbx_reimport_validation"] = {
        "mesh_count": len(imported),
        "triangles": triangle_total,
        "bounds_blender": {"min": vector(low), "max": vector(high), "dimensions": vector(high - low)},
        "preview": str(FBX_REIMPORT_PREVIEW),
    }
    # `triangleCount` is the final FBX's observed import count, rather than
    # the pre-export evaluated-mesh count (which can include degenerate faces
    # discarded by FBX triangulation).
    metadata = json.loads(METADATA.read_text(encoding="utf-8"))
    metadata["triangleCount"] = triangle_total
    METADATA.write_text(json.dumps(metadata, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    INSPECTION.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print("FBX_REIMPORT_VALIDATED", json.dumps(data["fbx_reimport_validation"], ensure_ascii=False))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--inspect", action="store_true")
    parser.add_argument("--source-preview", action="store_true")
    parser.add_argument("--export", action="store_true")
    parser.add_argument("--validate-fbx", action="store_true")
    args = parser.parse_args(cli_args())
    if not (args.inspect or args.source_preview or args.export or args.validate_fbx):
        parser.error("Use --inspect, --source-preview, --export, and/or --validate-fbx.")
    if args.inspect:
        inspect()
    if args.source_preview:
        render_source_preview()
    if args.export:
        if not INSPECTION.exists():
            inspect()
        export_asset()
    if args.validate_fbx:
        validate_fbx()


if __name__ == "__main__":
    main()
