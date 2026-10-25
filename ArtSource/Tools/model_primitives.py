"""Small shared Blender mesh/export helpers for the approved Fire Rescue toy kit.

Run with Blender 5.1. Geometry is in meters, X right, -Y forward, Z up.
This file is authoring-only; Unity consumes FBX, not .blend auto-imports.
"""
from pathlib import Path
import math
import json
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
MODEL_DIR = ROOT / 'ArtSource' / 'Models'
FBX_DIR = ROOT / 'Assets' / 'FireRescue' / 'Art' / 'Models'
PALETTE = {
    'Skin': (.91, .65, .42), 'Uniform': (.9, .19, .08),
    'Reflector': (1.0, .78, .22), 'Helmet': (1.0, .68, .08),
    'Charcoal': (.09, .105, .12), 'Hair': (.17, .075, .035),
    'Eye': (.045, .025, .02), 'CivilianShirt': (.11, .49, .46),
    'CivilianPants': (.70, .59, .41), 'White': (.91, .91, .86),
    'Water': (.10, .59, .88), 'Oil': (1.0, .64, .08),
    'Label': (.20, .45, .12), 'Plastic': (1.0, .88, .50),
    'Metal': (.43, .48, .51), 'Wood': (.67, .40, .19),
    'Sofa': (.86, .32, .20), 'Blanket': (.36, .64, .57),
    'Pillow': (.95, .90, .77), 'Cream': (.81, .77, .65),
}

def reset_scene():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    bpy.context.scene.unit_settings.system = 'METRIC'
    bpy.context.scene.unit_settings.scale_length = 1.0

def material(key):
    name = 'FR_' + key
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name)
        rgb = PALETTE[key]
        mat.diffuse_color = (*rgb, 1)
        mat.use_nodes = True
        nodes = mat.node_tree.nodes
        bsdf = next((n for n in nodes if n.type == 'BSDF_PRINCIPLED'), None)
        if bsdf is None:
            bsdf = nodes.new('ShaderNodeBsdfPrincipled')
        output = next((n for n in nodes if n.type == 'OUTPUT_MATERIAL'), None)
        if output is None:
            output = nodes.new('ShaderNodeOutputMaterial')
        if not output.inputs['Surface'].is_linked:
            mat.node_tree.links.new(bsdf.outputs['BSDF'], output.inputs['Surface'])
        bsdf.inputs['Base Color'].default_value = (*rgb, 1)
        bsdf.inputs['Roughness'].default_value = .72
        bsdf.inputs['Metallic'].default_value = .05 if key == 'Metal' else 0
    return mat

def _finish(obj, name, mat, smooth=True):
    obj.name = name
    obj.data.materials.append(material(mat) if isinstance(mat, str) else mat)
    for poly in obj.data.polygons:
        poly.use_smooth = smooth
    return obj

def box(name, location, dimensions, material, bevel=.04):
    bpy.ops.mesh.primitive_cube_add(size=1, location=location)
    obj = bpy.context.object
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel > 0:
        mod = obj.modifiers.new('Broad bevel', 'BEVEL')
        mod.width = min(bevel, min(dimensions) * .40)
        mod.segments = 2
        bpy.ops.object.modifier_apply(modifier=mod.name)
    _finish(obj, name, material)
    normal = obj.modifiers.new('Weighted corner normals', 'WEIGHTED_NORMAL')
    normal.keep_sharp = True
    bpy.ops.object.modifier_apply(modifier=normal.name)
    return obj

def ellipsoid(name, location, dimensions, material):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, radius=1, location=location)
    obj = bpy.context.object
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return _finish(obj, name, material)

def cylinder(name, location, radius, depth, material, vertices=16):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location)
    return _finish(bpy.context.object, name, material)

def between(name, start, end, radius, material):
    a, b = Vector(start), Vector(end)
    obj = cylinder(name, (a + b) * .5, radius, (b - a).length, material)
    obj.rotation_quaternion = (b - a).to_track_quat('Z', 'Y')
    obj.rotation_mode = 'QUATERNION'
    return obj

def lathe(name, profile, location, material, segments=20):
    vertices, faces = [], []
    for radius, height in profile:
        for i in range(segments):
            angle = 2 * math.pi * i / segments
            vertices.append((location[0] + radius * math.cos(angle),
                             location[1] + radius * math.sin(angle), location[2] + height))
    for j in range(len(profile) - 1):
        for i in range(segments):
            k = (i + 1) % segments
            faces.append((j * segments + i, j * segments + k,
                          (j + 1) * segments + k, (j + 1) * segments + i))
    faces.append(tuple(reversed(range(segments))))
    faces.append(tuple((len(profile) - 1) * segments + i for i in range(segments)))
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return _finish(obj, name, material)

def export_asset(asset_name):
    MODEL_DIR.mkdir(parents=True, exist_ok=True)
    FBX_DIR.mkdir(parents=True, exist_ok=True)
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    bpy.context.view_layer.update()
    root = bpy.data.objects.new(asset_name, None)
    bpy.context.collection.objects.link(root)
    for obj in meshes:
        if obj.parent is None:
            world = obj.matrix_world.copy()
            obj.parent = root
            obj.matrix_world = world
    bpy.context.view_layer.update()
    coords = [obj.matrix_world @ Vector(p) for obj in meshes for p in obj.bound_box]
    mins = [min(v[i] for v in coords) for i in range(3)]
    maxs = [max(v[i] for v in coords) for i in range(3)]
    tris = 0
    for obj in meshes:
        obj.data.calc_loop_triangles()
        tris += len(obj.data.loop_triangles)
    report = {'asset': asset_name, 'triangles': tris, 'meshes': len(meshes),
              'dimensions_xyz': [round(maxs[i] - mins[i], 4) for i in range(3)],
              'min_xyz': [round(x, 4) for x in mins],
              'materials': sorted({mat.name for obj in meshes for mat in obj.data.materials})}
    bpy.ops.wm.save_as_mainfile(filepath=str(MODEL_DIR / (asset_name + '.blend')))
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.fbx(filepath=str(FBX_DIR / (asset_name + '.fbx')),
        use_selection=True, object_types={'MESH', 'EMPTY'}, global_scale=1,
        apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS',
        axis_forward='-Z', axis_up='Y', use_mesh_modifiers=True,
        mesh_smooth_type='FACE', bake_anim=False, add_leaf_bones=False)
    (MODEL_DIR / (asset_name + '.json')).write_text(json.dumps(report, indent=2), encoding='utf-8')
    print('FIRE_MODEL_EXPORT ' + json.dumps(report))
