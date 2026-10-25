"""Read-only comparison of the imported snapshot and subsequently saved source."""
import bpy
import hashlib
import json


def fingerprint(path):
    bpy.ops.wm.open_mainfile(filepath=path, load_ui=False, use_scripts=False)
    graph = bpy.context.evaluated_depsgraph_get()
    objects = []
    for obj in sorted(bpy.context.scene.objects, key=lambda item: item.name):
        if obj.type != 'MESH' or not obj.visible_get():
            continue
        mesh = bpy.data.meshes.new_from_object(obj.evaluated_get(graph), depsgraph=graph)
        materials = []
        for slot in obj.material_slots:
            mat = slot.material
            values = {"name": mat.name, "diffuse": list(mat.diffuse_color)} if mat else None
            if mat and mat.use_nodes:
                values["shaders"] = []
                for node in mat.node_tree.nodes:
                    if node.type == 'BSDF_PRINCIPLED':
                        values["shaders"].append({name: list(node.inputs[name].default_value) if name == 'Base Color'
                            else node.inputs[name].default_value for name in ('Base Color', 'Metallic', 'Roughness')})
            materials.append(values)
        objects.append({"name": obj.name, "matrix": [list(row) for row in obj.matrix_world],
            "vertices": [list(vertex.co) for vertex in mesh.vertices],
            "polygons": [(list(poly.vertices), poly.material_index, poly.use_smooth) for poly in mesh.polygons],
            "materials": materials})
        bpy.data.meshes.remove(mesh)
    payload = {"objects": objects, "scale": bpy.context.scene.unit_settings.scale_length}
    return hashlib.sha256(json.dumps(payload, sort_keys=True).encode()).hexdigest(), len(objects)


snapshot = fingerprint('D:/Documents/畢專/ArtSource/UserNozzle/source.blend')
current = fingerprint('D:/Downloads/消防管.blend')
print('NOZZLE_SOURCE_COMPARE=' + json.dumps({"snapshot": snapshot, "current": current, "equivalent": snapshot == current}))
