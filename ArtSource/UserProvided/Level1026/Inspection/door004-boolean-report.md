# Door.004 Boolean / opening inspection (read-only)

- Source unchanged: `True`
- Mapping: `Unity=(10*Blender.y,10*Blender.z,-10*Blender.x)+translation`
- Wall modifiers: `[{"name": "實體化", "type": "SOLIDIFY", "showViewport": true, "showRender": true}, {"name": "布林", "type": "BOOLEAN", "showViewport": true, "showRender": true, "operation": "DIFFERENCE", "solver": "EXACT", "operandType": "COLLECTION", "object": null, "collection": "牆上"}]`
- CTRL_Hole.004 Unity bounds: `{"min": [-0.2283649444580078, 0.09620016813278198, 0.6983366012573242], "max": [0.7156662940979004, 2.078667163848877, 0.9249043464660645]}`
- Reproduced ray: `{"originUnity": [0.24, 0.6, -0.18], "directionUnity": [0.0, 0.0, 1.0], "hit": {"location": [0.24, 0.6, 0.924904], "normal": [0.0, -0.0, 1.0], "triangleIndex": 272, "distance": 1.104904}}`

Finding: the ray hit Unity Z=0.924904; CTRL_Hole.004 ends at Unity Z=0.924904. They coincide (within 1e-6).
Conclusion: the Exact collection Boolean is active (evaluated wall topology changes), but this cutter is flush with the far solidified wall face rather than overcutting it.
Proposed export-only repair after approval: on an in-memory scene copy, extend each CTRL_Hole along its local depth axis beyond both wall faces (Door.004 local Y is the depth axis), update depsgraph, then export evaluated wall. Do not save Source.blend.
