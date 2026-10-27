"""Offline ray checks on exported collision triangles, not on the original file."""
import json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
ROOT = next(parent for parent in Path(__file__).resolve().parents if (parent / 'ProjectSettings').is_dir())
data = json.loads((ROOT / 'ArtSource/UserProvided/LevelOne/level-one.geometry.json').read_text(encoding='utf-8'))
trees = [(p['name'], BVHTree.FromPolygons([Vector((v['x'], v['y'], v['z'])) for v in p['vertices']],
          [tuple(p['triangles'][i:i+3]) for i in range(0, len(p['triangles']), 3)], all_triangles=True)) for p in data['meshes']]
for door in data['doors']:
    for height in (.001, .35):
        c = door['center']
        center = Vector((c['x'], height, c['z']))
        normal = Vector((0, 0, 1)) if door['alongX'] else Vector((1, 0, 0))
        hits = []
        for name, tree in trees:
            hit = tree.ray_cast(center + normal * 1.5, -normal, 3)
            if hit[0] is not None:
                hits.append((name, tuple(hit[0]), tuple(hit[1])))
        print(door['name'], 'CENTRE_RAY', height, hits)
        if hits:
            raise RuntimeError('Exported doorway is obstructed: ' + door['name'])
