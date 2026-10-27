"""Inspect and export the supplied fire axe without modifying its source blend.

Run with Blender in background mode and --disable-autoexec.  The script never
executes Text datablocks from the input file.  Use --inspect first; --export
requires an explicit orientation mapping supplied after visual review.
"""

import argparse
import json
import math
import os
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Vector


ROOT = Path(__file__).resolve().parent.parent
OUTPUT = ROOT / "UserProvided" / "FireAxe"
INSPECTION = OUTPUT / "inspection.json"
FBX = OUTPUT / "fire_axe.fbx"
PREVIEW = OUTPUT / "preview.png"


def blender_args():
    args = sys.argv
    return args[args.index("--") + 1 :] if "--" in args else []


def all_mesh_objects():
    return [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]


def renderable_mesh_objects():
    return [obj for obj in all_mesh_objects() if not obj.hide_render and obj.visible_get()]


def evaluated_world_points(objects):
    depsgraph = bpy.context.evaluated_depsgraph_get()
    points = []
    for obj in objects:
        evaluated = obj.evaluated_get(depsgraph)
        mesh = evaluated.to_mesh()
        try:
            points.extend(evaluated.matrix_world @ vertex.co for vertex in mesh.vertices)
        finally:
            evaluated.to_mesh_clear()
    return points


def bbox_world(objects):
    corners = evaluated_world_points(objects)
    lo = Vector((min(v.x for v in corners), min(v.y for v in corners), min(v.z for v in corners)))
    hi = Vector((max(v.x for v in corners), max(v.y for v in corners), max(v.z for v in corners)))
    return lo, hi


def principal_axis(objects):
    # Covariance/PCA is sufficient here to describe the model; it is not used
    # to change orientation until a human has reviewed the preview.
    points = evaluated_world_points(objects)
    if not points:
        return Vector((0, 0, 0)), Vector((0, 0, 1))
    centroid = sum(points, Vector()) / len(points)
    cov = Matrix(((0.0, 0.0, 0.0), (0.0, 0.0, 0.0), (0.0, 0.0, 0.0)))
    for p in points:
        d = p - centroid
        cov[0][0] += d.x * d.x; cov[0][1] += d.x * d.y; cov[0][2] += d.x * d.z
        cov[1][0] += d.y * d.x; cov[1][1] += d.y * d.y; cov[1][2] += d.y * d.z
        cov[2][0] += d.z * d.x; cov[2][1] += d.z * d.y; cov[2][2] += d.z * d.z
    # Simple power iteration avoids numpy dependency.
    axis = Vector((1.0, 0.6, 0.2)).normalized()
    for _ in range(32):
        axis = (cov @ axis).normalized()
    return centroid, axis


def number(v):
    return round(float(v), 6)


def vec(v):
    return [number(c) for c in v]


def source_export_transform(objects):
    """Map source X/Y/Z to Unity Z/X/Y and place grip 0.12 m above pivot."""
    lo, hi = bbox_world(objects)
    source_length = hi.z - lo.z
    if source_length <= 0:
        raise RuntimeError("Source axe has no positive Z length.")
    scale = 0.72 / source_length
    # The bottom of the rendered handle is the first grip reference.  Its
    # x/y centre is estimated from the bottom 4% of evaluated surface points.
    bottom_band = [point for point in evaluated_world_points(objects) if point.z <= lo.z + source_length * 0.04]
    if not bottom_band:
        raise RuntimeError("Unable to estimate handle base from evaluated mesh.")
    grip_x = sum(point.x for point in bottom_band) / len(bottom_band)
    grip_y = sum(point.y for point in bottom_band) / len(bottom_band)
    pivot = Vector((grip_x, grip_y, lo.z + 0.12 / scale))
    # Keep Blender Z-up. Unity's imported FBX maps this Blender +Y to Unity +Z;
    # the broad cutting edge is source +X (objects head.001/.002), not the spike.
    # Apply Z-up -> Y-up once through FBX, and verify blade material vertices in Unity.
    axis_map = Matrix(((0.0, -1.0, 0.0, 0.0),
                       (1.0, 0.0, 0.0, 0.0),
                       (0.0, 0.0, 1.0, 0.0),
                       (0.0, 0.0, 0.0, 1.0)))
    transform = Matrix.Scale(scale, 4) @ axis_map @ Matrix.Translation(-pivot)
    return transform, {
        "source_length_z": number(source_length),
        "unity_length_y_target": 0.72,
        "uniform_scale": number(scale),
        "source_grip_pivot": vec(pivot),
        "unity_grip_pivot": [0.0, 0.0, 0.0],
        "axis_mapping": {"source_x": "unity_z (blade forward)", "source_y": "negative unity_x", "source_z": "unity_y (handle up)"},
        "expected_y_bounds": [-0.12, 0.6],
    }


def inspection():
    objects = all_mesh_objects()
    if not objects:
        raise RuntimeError("No visible mesh objects found in source blend.")
    lo, hi = bbox_world(objects)
    centre, axis = principal_axis(objects)
    dimensions = hi - lo
    object_data = []
    materials = set()
    depsgraph = bpy.context.evaluated_depsgraph_get()
    for obj in objects:
        mesh = obj.data
        evaluated = obj.evaluated_get(depsgraph)
        evaluated_mesh = evaluated.to_mesh()
        try:
            evaluated_triangles = sum(len(poly.vertices) - 2 for poly in evaluated_mesh.polygons)
            evaluated_vertices = len(evaluated_mesh.vertices)
        finally:
            evaluated.to_mesh_clear()
        names = [slot.material.name if slot.material else None for slot in obj.material_slots]
        materials.update(n for n in names if n)
        object_data.append({
            "name": obj.name,
            "mesh": mesh.name,
            "vertices": len(mesh.vertices),
            "edges": len(mesh.edges),
            "triangles": sum(len(poly.vertices) - 2 for poly in mesh.polygons),
            "evaluated_vertices": evaluated_vertices,
            "evaluated_triangles": evaluated_triangles,
            "materials": names,
            "location": vec(obj.location),
            "rotation_euler_degrees": [number(math.degrees(a)) for a in obj.rotation_euler],
            "scale": vec(obj.scale),
            "local_dimensions": vec(obj.dimensions),
            "hide_viewport": bool(obj.hide_viewport),
            "hide_render": bool(obj.hide_render),
            "visible_get": bool(obj.visible_get()),
            "selected": bool(obj.select_get()),
            "parent": obj.parent.name if obj.parent else None,
            "collections": sorted(collection.name for collection in obj.users_collection),
            "modifiers": [{"name": mod.name, "type": mod.type, "show_render": bool(mod.show_render)} for mod in obj.modifiers],
        })
    data = {
        "source": bpy.data.filepath,
        "scene": bpy.context.scene.name,
        "unit_system": bpy.context.scene.unit_settings.system,
        "unit_scale": number(bpy.context.scene.unit_settings.scale_length),
        "mesh_count": len(objects),
        "objects": object_data,
        "active_object": bpy.context.view_layer.objects.active.name if bpy.context.view_layer.objects.active else None,
        "materials": sorted(materials),
        "world_bounds_min": vec(lo),
        "world_bounds_max": vec(hi),
        "world_dimensions": vec(dimensions),
        "world_centroid": vec(centre),
        "principal_axis_unsigned": vec(axis),
        "orientation_note": "PCA direction is unsigned; endpoint/forward direction requires visual review before export transform.",
        "planned_unity_export": source_export_transform(objects)[1],
    }
    OUTPUT.mkdir(parents=True, exist_ok=True)
    INSPECTION.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print("INSPECTION_WRITTEN", INSPECTION)
    print(json.dumps(data, ensure_ascii=False))
    return data


def look_at(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat("-Z", "Y").to_euler()


def add_area(scene, location, energy, size):
    data = bpy.data.lights.new("Preview Area", "AREA")
    data.energy = energy
    data.shape = "DISK"
    data.size = size
    light = bpy.data.objects.new("Preview Area", data)
    scene.collection.objects.link(light)
    light.location = location
    look_at(light, (0, 0, 1.35))


def render_preview():
    originals = renderable_mesh_objects()
    if not originals:
        raise RuntimeError("No visible mesh objects found for preview.")
    lo, hi = bbox_world(originals)
    centre = (lo + hi) * 0.5
    scene = bpy.data.scenes.new("FireAxePreview")
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.studio_light = "paint.sl"
    scene.display.shading.color_type = "MATERIAL"
    scene.display.shading.show_shadows = True
    scene.display.shading.show_cavity = True
    scene.display.shading.cavity_type = "WORLD"
    scene.world = bpy.data.worlds.new("Preview World")
    scene.world.color = (0.045, 0.045, 0.055)
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 720
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = str(PREVIEW)

    transforms = (
        (Vector((-1.45, 0, 0)), 0.0),
        (Vector((1.45, 0, 0)), math.radians(38.0)),
    )
    for slot, (offset, yaw) in enumerate(transforms):
        transform = Matrix.Translation(offset) @ Matrix.Rotation(yaw, 4, "Z") @ Matrix.Translation(-centre)
        for original in originals:
            duplicate = original.copy()
            duplicate.data = original.data.copy()
            duplicate.name = "Front" if slot == 0 else "Oblique"
            duplicate.matrix_world = transform @ original.matrix_world
            scene.collection.objects.link(duplicate)

    camera_data = bpy.data.cameras.new("Preview Camera")
    camera_data.type = "ORTHO"
    # Deliberately generous framing: this is an inspection sheet, not an
    # asset-scale preview.  It must show the full handle and head.
    camera_data.ortho_scale = 7.0
    camera = bpy.data.objects.new("Preview Camera", camera_data)
    scene.collection.objects.link(camera)
    camera.location = (0, -8.0, 0)
    look_at(camera, (0, 0, 0))
    scene.camera = camera
    add_area(scene, (-3.0, -4.0, 5.0), 1000, 5.0)
    add_area(scene, (3.0, -2.0, 2.5), 450, 3.0)

    bpy.context.window.scene = scene
    bpy.ops.render.render(write_still=True)
    bpy.data.scenes.remove(scene, do_unlink=True)
    print("PREVIEW_WRITTEN", PREVIEW)


def build_export_asset():
    originals = renderable_mesh_objects()
    if not originals:
        raise RuntimeError("No render-visible mesh objects found to export.")
    transform, export_info = source_export_transform(originals)
    # Evaluate Skin/Subdivision while the originals still belong to this depsgraph.
    depsgraph = bpy.context.evaluated_depsgraph_get()
    meshes = []
    for original in originals:
        evaluated = original.evaluated_get(depsgraph)
        mesh = bpy.data.meshes.new_from_object(evaluated, depsgraph=depsgraph)
        meshes.append((original.name, mesh, transform @ original.matrix_world))
    scene = bpy.data.scenes.new("FireAxeExport")
    bpy.context.window.scene = scene
    baked = []
    for name, mesh, matrix in meshes:
        obj = bpy.data.objects.new(name, mesh)
        scene.collection.objects.link(obj)
        obj.matrix_world = matrix
        baked.append(obj)

    view_layer = bpy.context.view_layer
    bpy.ops.object.select_all(action="DESELECT")
    for obj in baked:
        obj.select_set(True)
        view_layer.objects.active = obj
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    view_layer.objects.active = baked[0]
    bpy.ops.object.join()
    root = view_layer.objects.active
    root.name = "FireAxe"
    root.data.name = "FireAxeMesh"
    root.location = (0, 0, 0)
    root.rotation_euler = (0, 0, 0)
    root.scale = (1, 1, 1)
    return scene, root, export_info


def render_export_preview(scene, root):
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.studio_light = "paint.sl"
    scene.display.shading.color_type = "MATERIAL"
    scene.display.shading.show_shadows = True
    scene.display.shading.show_cavity = True
    scene.display.shading.cavity_type = "WORLD"
    scene.world = bpy.data.worlds.new("Preview World")
    scene.world.color = (0.045, 0.045, 0.055)
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 900
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = str(PREVIEW)
    root.hide_render = False
    # Workbench uses viewport colors, not Principled node colors. Match them in
    # memory for this preview only; the user's .blend is never saved.
    for material in root.data.materials:
        if material and material.use_nodes:
            shader = next((node for node in material.node_tree.nodes if node.type == "BSDF_PRINCIPLED"), None)
            if shader: material.diffuse_color = shader.inputs["Base Color"].default_value
    camera_data = bpy.data.cameras.new("Preview Camera")
    camera_data.type = "ORTHO"
    camera_data.ortho_scale = 1.3
    camera = bpy.data.objects.new("Preview Camera", camera_data)
    scene.collection.objects.link(camera)
    camera.location = (2, -1.3, .8)
    look_at(camera, (0, 0, 0.24))
    scene.camera = camera
    bpy.context.window.scene = scene
    bpy.ops.render.render(write_still=True)
    print("PREVIEW_WRITTEN", PREVIEW)


def export_asset():
    scene, root, export_info = build_export_asset()
    bpy.context.window.scene = scene
    bpy.ops.object.select_all(action="DESELECT")
    root.select_set(True)
    bpy.context.view_layer.objects.active = root
    if not hasattr(bpy.ops.export_scene, "fbx"):
        raise RuntimeError("This Blender build has no export_scene.fbx operator.")
    bpy.ops.export_scene.fbx(
        filepath=str(FBX),
        use_selection=True,
        use_active_collection=False,
        object_types={"MESH"},
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        bake_anim=False,
        axis_forward="-Z",
        axis_up="Y",
    )
    export_info["fbx"] = str(FBX)
    export_info["exported_mesh_objects"] = 1
    export_info["exported_vertices"] = len(root.data.vertices)
    export_info["exported_triangles"] = sum(len(poly.vertices) - 2 for poly in root.data.polygons)
    export_info["material_colors"] = {}
    for material in root.data.materials:
        if material and material.node_tree:
            node = next((node for node in material.node_tree.nodes if node.type == "BSDF_PRINCIPLED"), None)
            if node: export_info["material_colors"][material.name] = list(node.inputs["Base Color"].default_value)
    lo, hi = bbox_world([root])
    export_info["export_bounds_min"] = vec(lo)
    export_info["export_bounds_max"] = vec(hi)
    export_info["export_dimensions"] = vec(hi - lo)
    export_info["export_coordinate_system"] = "Blender Z-up; FBX axis_up=Y converts once"
    inspection_data = json.loads(INSPECTION.read_text(encoding="utf-8")) if INSPECTION.exists() else inspection()
    inspection_data["export"] = export_info
    INSPECTION.write_text(json.dumps(inspection_data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    render_export_preview(scene, root)
    bpy.data.scenes.remove(scene, do_unlink=True)
    print("FBX_WRITTEN", FBX)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--inspect", action="store_true")
    parser.add_argument("--preview", action="store_true")
    parser.add_argument("--export", action="store_true")
    args = parser.parse_args(blender_args())
    if not args.inspect and not args.preview:
        parser.error("Use --inspect or --preview. Export is gated on visual axis review.")
    if args.inspect:
        inspection()
    if args.preview:
        render_preview()
    if args.export:
        if not INSPECTION.exists():
            inspection()
        export_asset()


if __name__ == "__main__":
    main()
