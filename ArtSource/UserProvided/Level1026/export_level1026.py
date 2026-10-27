"""Export the authored Level1026 Blend to derived Unity-native JSON.

Read-only with respect to the source .blend: run Blender with --disable-autoexec
and do not save.  The output keeps every source mesh at its authored full height.
"""
import bpy
import hashlib
import json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

SOURCE = Path(__file__).resolve().parent / "Source.blend"
OUT = Path(__file__).resolve().parent / "level1026.geometry.json"
SCALE = 10.0
RAW_OVER_CUT_PER_SIDE = 0.02


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def raw_bounds(points):
    return {
        "min": [min(p[i] for p in points) for i in range(3)],
        "max": [max(p[i] for p in points) for i in range(3)],
    }


def as_dict(v):
    return {"x": round(float(v.x), 6), "y": round(float(v.y), 6), "z": round(float(v.z), 6)}


def classify(name):
    if name == "wall":
        return "wall"
    if name == "Door" or (name.startswith("Door.") and name[5:].isdigit()):
        return "door"
    if name == "DoorFrame" or (name.startswith("DoorFrame.") and name[10:].isdigit()):
        return "frame"
    if name == "Handle_Front" or name == "Handle_Back" or name.startswith("Handle_Front.") or name.startswith("Handle_Back."):
        return "handle"
    return "other"


def suffix(name, prefix):
    return name[len(prefix):] if name.startswith(prefix) else ""


def extend_door_cutters_in_memory(objects):
    """Overcut only the cutters' local thickness axis; never save Source.blend."""
    repairs = []
    for cutter in (obj for obj in objects if obj.name == "CTRL_Hole" or obj.name.startswith("CTRL_Hole.")):
        original_depth = float(cutter.dimensions.y)
        if original_depth <= 0:
            raise RuntimeError("Invalid CTRL_Hole local-Y depth: " + cutter.name)
        factor = (original_depth + RAW_OVER_CUT_PER_SIDE * 2.0) / original_depth
        cutter.scale.y *= factor
        repairs.append({
            "object": cutter.name,
            "localDepthAxis": "Y",
            "rawOvercutPerSide": RAW_OVER_CUT_PER_SIDE,
            "originalLocalDepth": round(original_depth, 8),
            "scaleMultiplier": round(factor, 8),
        })
    if not repairs:
        raise RuntimeError("No CTRL_Hole cutters found for derived aperture-depth repair.")
    bpy.context.view_layer.update()
    return repairs


def validate_openings_against_wall(doors, unity, depsgraph):
    """Validate that each opening is clear but nearby wall remains solid."""
    wall = bpy.data.objects.get("wall")
    if wall is None:
        raise RuntimeError("Missing wall mesh for opening validation.")
    evaluated = wall.evaluated_get(depsgraph)
    mesh = evaluated.to_mesh()
    try:
        vertices = [unity(evaluated.matrix_world @ vertex.co) for vertex in mesh.vertices]
        mesh.calc_loop_triangles()
        faces = [tuple(triangle.vertices) for triangle in mesh.loop_triangles]
        bvh = BVHTree.FromPolygons(vertices, faces, all_triangles=True)
        checks = []
        for door in doors:
            anchor = door["openingAnchor"]
            p = anchor["position"]
            center = Vector((p["x"], 0.6, p["z"]))
            normal = Vector((0, 0, 1)) if anchor["alongUnityX"] else Vector((1, 0, 0))
            tangent = Vector((1, 0, 0)) if anchor["alongUnityX"] else Vector((0, 0, 1))
            through = []
            for sign in (-1, 1):
                start = center + normal * (0.7 * sign)
                direction = -normal * sign
                hit = bvh.ray_cast(start, direction, 1.4)
                through.append({"side": sign, "wallHit": hit[0] is not None, "hitDistance": None if hit[3] is None else round(float(hit[3]), 6)})
            side_checks = []
            # At 0.7m sideways from the door centre (about 0.23m beyond its edge),
            # nearby wall must remain solid. Both sides are recorded for corners.
            lateral_distance = .7
            for sign in (-1, 1):
                side_center = center + tangent * (lateral_distance * sign)
                hit = bvh.ray_cast(side_center - normal * .7, normal, 1.4)
                side_checks.append({"side": sign, "wallHit": hit[0] is not None, "hitDistance": None if hit[3] is None else round(float(hit[3]), 6)})
            checks.append({
                "door": door["name"], "heightUnity": 0.6,
                "throughBothSidesClear": not any(row["wallHit"] for row in through),
                "throughRays": through,
                "sideWallStillBlocks": any(row["wallHit"] for row in side_checks),
                "sideWallRays": side_checks,
            })
        return {"allDoorwaysClear": all(row["throughBothSidesClear"] for row in checks), "allDoorwaysRetainNearbyWall": all(row["sideWallStillBlocks"] for row in checks), "doors": checks}
    finally:
        evaluated.to_mesh_clear()


def main():
    before = sha256(SOURCE)
    sources = sorted((obj for obj in bpy.context.scene.objects if obj.type == "MESH"), key=lambda item: item.name.lower())
    if not sources:
        raise RuntimeError("No mesh objects in source Blend.")
    # Keep the level origin stable: control cutters must never move the playable
    # coordinate frame merely because a derived repair makes them deeper.
    base_depsgraph = bpy.context.evaluated_depsgraph_get()
    source_points = []
    for source in sources:
        evaluated = source.evaluated_get(base_depsgraph)
        mesh = evaluated.to_mesh()
        try:
            source_points.extend(evaluated.matrix_world @ vertex.co for vertex in mesh.vertices)
        finally:
            evaluated.to_mesh_clear()
    derived_repairs = extend_door_cutters_in_memory(sources)
    depsgraph = bpy.context.evaluated_depsgraph_get()

    # This X/Z exchange aligns the authored plan with the user-provided north-up
    # reference: Unity X=Blender Y, Y=Blender Z, Z=-Blender X.
    source_bounds = raw_bounds(source_points)
    low = Vector(source_bounds["min"])
    high = Vector(source_bounds["max"])
    pre_low = Vector((low.y * SCALE, low.z * SCALE, -high.x * SCALE))
    pre_high = Vector((high.y * SCALE, high.z * SCALE, -low.x * SCALE))
    translation = Vector((-(pre_low.x + pre_high.x) * 0.5, -pre_low.y, -(pre_low.z + pre_high.z) * 0.5))

    def unity(point):
        return Vector((point.y * SCALE, point.z * SCALE, -point.x * SCALE)) + translation

    result = {
        "schema": "fire-rescue-level-geometry/v1",
        "source": {
            "path": str(SOURCE),
            "sha256": before,
            "blendUnits": {"system": bpy.context.scene.unit_settings.system, "scaleLength": bpy.context.scene.unit_settings.scale_length},
            "coordinateMapping": "Unity=(10*Blender.y, 10*Blender.z, -10*Blender.x)+translation",
            "translation": as_dict(translation),
            "orientation": "north=+UnityZ, east=+UnityX, up=+UnityY; mapping determinant is negative, so emitted triangles reverse winding to preserve authored outward normals.",
        },
        "sourceWorldBounds": {"min": as_dict(low), "max": as_dict(high)},
        "bounds": None,
        "meshes": [],
        "doors": [],
        "notes": [
            "Full-height source geometry: no wall/door cutting, opening width/height edits or furniture additions were applied.",
            "Derived aperture-depth repair: CTRL_Hole cutters are extended only in Blender memory by 0.02 raw units on each depth side before evaluating the authored Exact Boolean; Source.blend is never saved.",
            "CTRL_Hole meshes are retained as authored control geometry; gameplay should not create physical colliders for kind=other controlGeometry=true.",
            "No authored floor mesh was identified by name. Main scene should supply a simple physical ground plane at Unity Y=0.",
        ],
        "derivedRepairs": derived_repairs,
        "suggestedGameplayAnchors": {
            "status": "Provisional design anchors from the supplied floor-plan reading, not semantic mesh names. Confirm against the imported visual before shipping.",
            "rooms": [
                {"name": "Kitchen (southwest)", "position": {"x": -3.8, "y": 0.0, "z": -3.8}},
                {"name": "Living room", "position": {"x": -1.6, "y": 0.0, "z": -1.3}},
                {"name": "Bathroom (confirmed gameplay anchor)", "position": {"x": 0.24, "y": 0.0, "z": 1.8}},
                {"name": "Bedroom / first rescue", "position": {"x": 2.4, "y": 0.0, "z": 3.2}},
                {"name": "Storage candidate", "position": {"x": -5.3, "y": 0.0, "z": 3.7}},
            ],
            "firstRescue": {
                "toiletWaterSource": {"x": 0.24, "y": 0.0, "z": 1.8},
                "bedroomNpc": {"x": 2.4, "y": 0.0, "z": 3.2},
                "bedroomFire": {"x": 2.55, "y": 0.0, "z": 2.4},
                "outsideSpawn": {"x": 6.26, "y": 0.0, "z": -4.0},
            },
        },
    }
    frame_bounds = {}
    all_unity_points = []
    physical_unity_points = []
    for source in sources:
        evaluated = source.evaluated_get(depsgraph)
        mesh = evaluated.to_mesh()
        try:
            world_vertices = [evaluated.matrix_world @ vertex.co for vertex in mesh.vertices]
            converted = [unity(point) for point in world_vertices]
            mesh.calc_loop_triangles()
            vertices = [as_dict(point) for point in converted]
            # X/Z exchange reverses handedness; flip every winding for outward normals.
            triangles = [index for triangle in mesh.loop_triangles for index in (triangle.vertices[0], triangle.vertices[2], triangle.vertices[1])]
            bounds = raw_bounds(converted)
            bounds = {"min": as_dict(Vector(bounds["min"])), "max": as_dict(Vector(bounds["max"]))}
            kind = classify(source.name)
            control = source.name.startswith("CTRL_")
            row = {
                "name": source.name,
                "kind": kind,
                "controlGeometry": control,
                "vertices": vertices,
                "triangles": triangles,
                "bounds": bounds,
                "materialNames": [slot.material.name if slot.material else None for slot in source.material_slots],
            }
            result["meshes"].append(row)
            all_unity_points.extend(converted)
            if not control:
                physical_unity_points.extend(converted)
            if kind == "frame":
                frame_bounds[source.name] = bounds
        finally:
            evaluated.to_mesh_clear()

    for leaf in (item for item in result["meshes"] if item["kind"] == "door"):
        frame_name = "DoorFrame" + suffix(leaf["name"], "Door")
        frame = frame_bounds.get(frame_name)
        if frame is None:
            continue
        lo = Vector((frame["min"]["x"], frame["min"]["y"], frame["min"]["z"]))
        hi = Vector((frame["max"]["x"], frame["max"]["y"], frame["max"]["z"]))
        horizontal_x = (hi.x - lo.x) >= (hi.z - lo.z)
        result["doors"].append({
            "name": leaf["name"],
            "frameName": frame_name,
            "leafBounds": leaf["bounds"],
            "frameBounds": frame,
            "openingAnchor": {
                "position": as_dict(Vector(((lo.x + hi.x) * .5, 0.0, (lo.z + hi.z) * .5))),
                "alongUnityX": horizontal_x,
                "approximateWidth": round(float((hi.x - lo.x) if horizontal_x else (hi.z - lo.z)), 6),
                "approximateDepth": round(float((hi.z - lo.z) if horizontal_x else (hi.x - lo.x)), 6),
                "approximateClearanceHeight": round(float(hi.y), 6),
            },
        })
    exported_bounds = raw_bounds(physical_unity_points)
    control_bounds = raw_bounds(all_unity_points)
    result["bounds"] = {"min": as_dict(Vector(exported_bounds["min"])), "max": as_dict(Vector(exported_bounds["max"]))}
    result["controlInclusiveBounds"] = {"min": as_dict(Vector(control_bounds["min"])), "max": as_dict(Vector(control_bounds["max"]))}
    result["apertureValidation"] = validate_openings_against_wall(result["doors"], unity, depsgraph)
    print("APERTURE_VALIDATION", json.dumps(result["apertureValidation"], ensure_ascii=False))
    if not result["apertureValidation"]["allDoorwaysClear"]:
        raise RuntimeError("Derived aperture repair did not clear every doorway at Unity Y=0.6.")
    if not result["apertureValidation"]["allDoorwaysRetainNearbyWall"]:
        raise RuntimeError("Derived aperture repair removed nearby wall around at least one doorway.")
    after = sha256(SOURCE)
    result["source"]["sha256AfterExport"] = after
    result["source"]["unchangedDuringExport"] = before == after
    if before != after:
        raise RuntimeError("Source Blend hash changed during derived export; refusing output.")
    OUT.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
    print("LEVEL1026_EXPORT", json.dumps({"output": str(OUT), "meshes": len(result["meshes"]), "doors": len(result["doors"]), "bounds": result["bounds"]}, ensure_ascii=False))


if __name__ == "__main__":
    main()
