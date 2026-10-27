import bpy, os, math, json, hashlib
from pathlib import Path
from mathutils import Matrix, Vector

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = next(parent for parent in Path(HERE).parents if (parent / 'ProjectSettings').is_dir())
SOURCE = os.path.join(HERE, 'Source.fbx')
BLEND = os.path.join(HERE, 'Resident2.blend')
EXPORT = str(PROJECT / 'Assets' / 'FireRescue' / 'Art' / 'UserProvided' / 'Characters' / 'Character2' / 'Resident2.fbx')
META = os.path.join(HERE, 'metadata.json')

def v3(x): return [round(float(a), 6) for a in x]
def mesh_bounds(meshes):
    p = [o.matrix_world @ v.co for o in meshes for v in o.data.vertices]
    lo = Vector((min(x[i] for x in p) for i in range(3))); hi = Vector((max(x[i] for x in p) for i in range(3)))
    return lo, hi
def material(name, color):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=(*color,1); bs.inputs['Roughness'].default_value=.78
    return m
def set_slots(obj, mats):
    obj.data.materials.clear()
    for m in mats: obj.data.materials.append(m)
def set_face_material(obj, name, predicate):
    idx=[m.name for m in obj.data.materials].index(name)
    for f in obj.data.polygons:
        if predicate(obj.matrix_world @ f.center): f.material_index=idx
def add_bone(eb, name, head, tail, parent=None):
    b=eb.new(name); b.head=head; b.tail=tail; b.parent=parent; return b
def key(action, bone, frame, rot):
    pb=arm.pose.bones[bone]; pb.rotation_mode='XYZ'; pb.rotation_euler=rot; pb.keyframe_insert('rotation_euler', frame=frame)
def cycle(action):
    # Blender 5's layered Action API no longer exposes fcurves directly.  Both clips
    # have matching first/last poses, so they loop cleanly at the exported clip seam.
    return action

bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=SOURCE, use_custom_normals=True)
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
if len(meshes)!=6: raise RuntimeError('Expected six original mesh objects, got %d' % len(meshes))
before={o.name:len(o.data.vertices) for o in meshes}

# Original: +X front, Z up.  Resident source is centred and normalized to 1.5m without editing topology.
lo,hi=mesh_bounds(meshes); center=(lo+hi)*.5; height=hi.z-lo.z; scale=1.5/height
transform=Matrix.Translation((0,0,-lo.z)) @ Matrix.Rotation(math.radians(-90),4,'Z') @ Matrix.Translation((-center.x,-center.y,0))
for o in meshes: o.matrix_world=Matrix.Scale(scale,4) @ transform @ o.matrix_world

skin=material('U2_Skin',(0.94,0.70,0.49)); hair=material('U2_Hair',(0.24,0.115,0.065))
shirt=material('U2_Shirt',(0.16,0.56,0.49)); trim=material('U2_Trim',(0.94,0.85,0.61))
pants=material('U2_Pants',(0.14,0.20,0.28)); dark=material('U2_Dark',(0.085,0.10,0.12)); eye=material('U2_Eye',(0.045,0.028,0.022))
byname={o.name:o for o in meshes}
# Exact source parts observed in the inspection: circle=eyes, cube=hair, cube.001=head/hands/legs,
# cube.002=shirt, cube.003/.004=the existing two chest trims.
eyes=byname.get('圓形'); hair_obj=byname.get('立方體'); body=byname.get('立方體.001'); shirt_obj=byname.get('立方體.002')
set_slots(eyes,[eye]); set_slots(hair_obj,[hair]); set_slots(shirt_obj,[shirt])
set_slots(byname.get('立方體.003'),[trim]); set_slots(byname.get('立方體.004'),[trim])
set_slots(body,[skin,pants,dark,shirt])
for f in body.data.polygons:
    p=body.matrix_world @ f.center; z=p.z
    # The source's single connected body also includes a small under-shirt section;
    # give it the shirt colour so it does not read as exposed skin through the hem.
    f.material_index=2 if z < .12 else (1 if z < .30 else (0 if z > .69 or (abs(p.x) > .30 and .40 < z < .69) else 3))

# A compact Generic-friendly skeleton.  Meshes keep their original vertices; weights are procedural, not geometry edits.
bpy.ops.object.armature_add(enter_editmode=True, location=(0,0,0)); arm=bpy.context.object; arm.name='U2_Root'; arm.data.name='U2_Rig'
eb=arm.data.edit_bones; eb.remove(eb[0])
root=add_bone(eb,'Root',(0,0,0),(0,0,.18)); hips=add_bone(eb,'Hips',(0,0,.30),(0,0,.48),root)
spine=add_bone(eb,'Spine',(0,0,.48),(0,0,.61),hips); chest=add_bone(eb,'Chest',(0,0,.61),(0,0,.72),spine)
neck=add_bone(eb,'Neck',(0,0,.72),(0,0,.80),chest); add_bone(eb,'Head',(0,0,.80),(0,0,1.30),neck)
for side,x in [('L',-.25),('R',.25)]:
    thigh=add_bone(eb,side+'_Thigh',(x*.62,0,.34),(x*.82,0,.18),hips); shin=add_bone(eb,side+'_Shin',(x*.82,0,.18),(x*.90,0,.07),thigh); add_bone(eb,side+'_Foot',(x*.90,0,.07),(x*.90,-.12,.04),shin)
    upper=add_bone(eb,side+'_UpperArm',(x,0,.67),(x*1.36,0,.57),chest); lower=add_bone(eb,side+'_LowerArm',(x*1.36,0,.57),(x*1.68,0,.51),upper); add_bone(eb,side+'_Hand',(x*1.68,0,.51),(x*1.78,-.03,.47),lower)
bpy.ops.object.mode_set(mode='POSE'); bpy.ops.pose.armature_apply(selected=False); bpy.ops.object.mode_set(mode='OBJECT')

def group(obj, bone, ids, weight=1.0):
    g=obj.vertex_groups.get(bone) or obj.vertex_groups.new(name=bone); g.add(ids,weight,'REPLACE')
for obj in meshes:
    for vert in obj.data.vertices:
        p=obj.matrix_world @ vert.co; x,z=p.x,p.z
        if obj is hair_obj or obj is eyes: target='Head'
        elif obj is body and z > .69: target='Head'
        elif obj is body and abs(x) > .30 and .40 < z < .69: target=('L_Hand' if x < 0 else 'R_Hand')
        elif obj is body and z < .12: target=('L_Foot' if x < 0 else 'R_Foot')
        elif obj is body and z < .30: target=('L_Shin' if x < 0 else 'R_Shin')
        elif obj is body and z < .45: target=('L_Thigh' if x < 0 else 'R_Thigh')
        elif obj is shirt_obj and abs(x) > .24 and z > .48: target=('L_UpperArm' if x < 0 else 'R_UpperArm')
        elif obj is shirt_obj: target='Chest'
        elif z > .58: target='Chest'
        elif z > .45: target='Spine'
        else: target='Hips'
        group(obj,target,[vert.index])
    obj.parent=arm; mod=obj.modifiers.new('U2_Skin','ARMATURE'); mod.object=arm

# Named, embedded markers for Unity-side facing and head-center corrections.
for name,loc in [('FaceForward',(0,-.42,.82)),('HeadCenter',(0,0,1.28))]:
    e=bpy.data.objects.new(name,None); bpy.context.collection.objects.link(e); e.empty_display_type='ARROWS'; e.empty_display_size=.09; e.location=loc; e.rotation_euler=(math.radians(90),0,0); e.parent=arm

def make_idle():
    a=bpy.data.actions.new('U2_Idle'); a.use_fake_user=True; arm.animation_data_create(); arm.animation_data.action=a
    for f,tilt in [(1,0),(31,math.radians(1.4)),(61,0)]:
        key(a,'Chest',f,(tilt,0,0)); key(a,'Head',f,(-tilt*.45,0,0)); key(a,'L_UpperArm',f,(0,0,math.radians(tilt*12))); key(a,'R_UpperArm',f,(0,0,math.radians(-tilt*12)))
    cycle(a); return a
def make_run():
    a=bpy.data.actions.new('U2_Run'); a.use_fake_user=True; arm.animation_data.action=a
    for f,s in [(1,0),(8,1),(16,0),(24,-1),(31,0)]:
        leg=math.radians(17*s); arm_s=math.radians(12*s)
        key(a,'L_Thigh',f,(leg,0,0)); key(a,'R_Thigh',f,(-leg,0,0)); key(a,'L_Shin',f,(math.radians(-10*max(s,0)),0,0)); key(a,'R_Shin',f,(math.radians(-10*max(-s,0)),0,0))
        key(a,'L_UpperArm',f,(-arm_s,0,0)); key(a,'R_UpperArm',f,(arm_s,0,0)); key(a,'Chest',f,(math.radians(1.2),0,0)); key(a,'Head',f,(math.radians(-.8),0,0))
    cycle(a); return a
idle=make_idle(); run=make_run(); arm.animation_data.action=idle
bpy.context.scene.render.fps=24

# Save editable derived asset before export.
bpy.ops.wm.save_as_mainfile(filepath=BLEND)
os.makedirs(os.path.dirname(EXPORT),exist_ok=True)
bpy.ops.object.select_all(action='DESELECT')
for o in meshes+[arm]+[o for o in bpy.context.scene.objects if o.name in {'FaceForward','HeadCenter'}]: o.select_set(True)
bpy.context.view_layer.objects.active=arm
bpy.ops.export_scene.fbx(filepath=EXPORT, use_selection=True, object_types={'ARMATURE','MESH','EMPTY'}, add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=True, bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0, axis_forward='-Z', axis_up='Y')

# Offline Workbench evidence, no source modification.
def aim(o,target): o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
def render(path, loc, action, frame):
    arm.animation_data.action=action; bpy.context.scene.frame_set(frame)
    bpy.context.scene.render.engine='BLENDER_WORKBENCH'; bpy.context.scene.display.shading.light='STUDIO'; bpy.context.scene.display.shading.color_type='MATERIAL'; bpy.context.scene.world.color=(.12,.12,.12)
    if not bpy.data.objects.get('PreviewCamera'):
        bpy.ops.object.camera_add(); bpy.context.object.name='PreviewCamera'; bpy.context.scene.camera=bpy.context.object
    cam=bpy.data.objects['PreviewCamera']; cam.data.type='ORTHO'; cam.data.ortho_scale=1.82; cam.location=loc; aim(cam,(0,0,.77))
    bpy.context.scene.render.resolution_x=700; bpy.context.scene.render.resolution_y=700; bpy.context.scene.render.resolution_percentage=100; bpy.context.scene.render.image_settings.file_format='PNG'; bpy.context.scene.render.filepath=path
    bpy.ops.render.render(write_still=True)
render(os.path.join(HERE,'Resident2_front_idle.png'),(0,-4,.82),idle,31)
render(os.path.join(HERE,'Resident2_3q_run.png'),(2.7,-3.4,2.05),run,8)

with open(SOURCE,'rb') as f: sha=hashlib.sha256(f.read()).hexdigest()
meta={'source':'Source.fbx','source_sha256':sha,'source_mesh_vertex_counts':before,'export':'Assets/FireRescue/Art/UserProvided/Characters/Character2/Resident2.fbx','coordinates':{'source_front':'+X','source_up':'Z','derived_front':'-Y','feet_z':0,'height_m':1.5},'materials':[m.name for m in [skin,hair,shirt,trim,pants,dark,eye]],'bones':len(arm.data.bones),'clips':[{'name':'U2_Idle','frames':[1,61],'fps':24,'seconds':2.5},{'name':'U2_Run','frames':[1,31],'fps':24,'seconds':1.25}],'markers':['FaceForward','HeadCenter']}
with open(META,'w',encoding='utf-8') as f: json.dump(meta,f,ensure_ascii=False,indent=2)
