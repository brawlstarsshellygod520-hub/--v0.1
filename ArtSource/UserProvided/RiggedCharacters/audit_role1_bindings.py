"""Read-only audit: inspect Role1 vertex group allocation and action channels."""
import bpy, os, json
from mathutils import Vector
HERE=os.path.dirname(os.path.abspath(__file__))
BLEND=os.path.join(HERE,'ResidentRole1.blend')
bpy.ops.wm.open_mainfile(filepath=BLEND)
arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
rows=[]
for obj in sorted((o for o in bpy.context.scene.objects if o.type=='MESH'), key=lambda o:o.name):
    data={}
    for v in obj.data.vertices:
        p=obj.matrix_world @ v.co
        for g in v.groups:
            name=obj.vertex_groups[g.group].name
            entry=data.setdefault(name, {'count':0,'min':[999,999,999],'max':[-999,-999,-999]})
            entry['count']+=1
            for i in range(3):
                entry['min'][i]=min(entry['min'][i],round(float(p[i]),5)); entry['max'][i]=max(entry['max'][i],round(float(p[i]),5))
    rows.append({'object':obj.name,'vertices':len(obj.data.vertices),'groups':data})
actions=[]
for action in bpy.data.actions:
    arm.animation_data.action=action
    bpy.context.scene.frame_set(1)
    actions.append({'name':action.name,'frame_1_rotations':{
        name:[round(float(value),6) for value in arm.pose.bones[name].rotation_euler]
        for name in ('Chest','Head','L_UpperArm','R_UpperArm','L_Thigh','R_Thigh','L_Shin','R_Shin')}})
report={'armature':arm.name,'bones':[b.name for b in arm.data.bones],'mesh_weights':rows,'actions':actions}
with open(os.path.join(HERE,'role1_binding_audit.json'),'w',encoding='utf-8') as f: json.dump(report,f,ensure_ascii=False,indent=2)
