"""Read-only audit of Role2 body/shirt deformation assignment."""
import bpy, os, json
HERE=os.path.dirname(os.path.abspath(__file__))
bpy.ops.wm.open_mainfile(filepath=os.path.join(HERE,'FirefighterRole2.blend'))
rows=[]
for obj in sorted((o for o in bpy.context.scene.objects if o.type=='MESH'),key=lambda o:o.name):
    groups={}
    for v in obj.data.vertices:
        for g in v.groups:
            name=obj.vertex_groups[g.group].name
            groups[name]=groups.get(name,0)+1
    rows.append({'object':obj.name,'vertices':len(obj.data.vertices),'groups':groups})
with open(os.path.join(HERE,'role2_binding_audit.json'),'w',encoding='utf-8') as f:
    json.dump({'mesh_weights':rows},f,ensure_ascii=False,indent=2)
