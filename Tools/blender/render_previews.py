"""Renders pictures of the museum and the street from the Blender files, to check the models by eye.

    python Tools/blender/render_previews.py <output folder>

Top views (with and without the invisible boxes) and a few views from where a visitor would stand.
"""
import bpy, os, sys, math
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
OUT = sys.argv[-1]
os.makedirs(OUT, exist_ok=True)

bpy.ops.wm.open_mainfile(filepath=os.path.join(ROOT, 'Blender', 'museum.blend'))
with bpy.data.libraries.load(os.path.join(ROOT, 'Blender', 'street.blend'), link=False) as (src, dst):
    dst.objects = [n for n in src.objects if not n.startswith('CAL__')]
for o in dst.objects:
    if o is not None:
        bpy.context.scene.collection.objects.link(o)
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.samples = 12
scene.cycles.use_denoising = False
scene.cycles.max_bounces = 2
scene.render.film_transparent = False
world = bpy.data.worlds.new('W'); scene.world = world; world.use_nodes = True
world.node_tree.nodes['Background'].inputs[0].default_value = (0.95, 0.97, 1.0, 1)
world.node_tree.nodes['Background'].inputs[1].default_value = 1.1
sun = bpy.data.objects.new('Sun', bpy.data.lights.new('Sun', 'SUN')); sun.data.energy = 2.0; sun.rotation_euler = (0.5, 0.2, 0.6); scene.collection.objects.link(sun)

def emit(name, color, alpha):
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    out = nt.nodes.new('ShaderNodeOutputMaterial'); mix = nt.nodes.new('ShaderNodeMixShader')
    tr = nt.nodes.new('ShaderNodeBsdfTransparent'); em = nt.nodes.new('ShaderNodeEmission')
    em.inputs[0].default_value = (*color, 1); mix.inputs[0].default_value = alpha
    nt.links.new(tr.outputs[0], mix.inputs[1]); nt.links.new(em.outputs[0], mix.inputs[2]); nt.links.new(mix.outputs[0], out.inputs[0])
    return m
BOX = {'COL': emit('col', (1, 0.1, 0.2), 0.55), 'FLOOR': emit('floor', (0.1, 0.8, 0.3), 0.25), 'HOT': emit('hot', (1, 0.8, 0), 0.7), 'ZONE': emit('zone', (0.1, 0.5, 1), 0.6)}

def boxes(show, kinds=('COL', 'FLOOR', 'HOT', 'ZONE')):
    for o in scene.objects:
        k = o.name.split('__')[0]
        if k in BOX:
            o.hide_render = not (show and k in kinds) or o.name == 'COL__ceiling'
            o.display_type = 'TEXTURED'
            if not o.data.materials: o.data.materials.append(BOX[k])

def sets(on):
    for o in scene.objects:
        if o.name.startswith('SW__'):
            o.hide_render = o.name[4:] not in on

def hide(names, flag=True):
    for o in scene.objects:
        if o.name in names: o.hide_render = flag

cam = bpy.data.objects.new('Cam', bpy.data.cameras.new('Cam')); scene.collection.objects.link(cam); scene.camera = cam
# positions below are in the web app's coordinates (x, y up, z); Blender has (x, -z, y)
B = lambda x, y, z: Vector((x, -z, y))

def look(eye, target, lens=18):
    cam.data.type = 'PERSP'; cam.data.lens = lens; cam.data.clip_start = 0.05
    cam.location = B(*eye)
    d = B(*target) - cam.location
    cam.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()

def top(cx, cz, width, res):
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = width
    cam.location = B(cx, 40, cz); cam.rotation_euler = (0, 0, 0)
    scene.render.resolution_x, scene.render.resolution_y = res

def shot(name):
    scene.render.filepath = os.path.join(OUT, name + '.png')
    bpy.ops.render.render(write_still=True)
    print('rendered', name)

hide({'M__shell', 'COL__ceiling'})
sets({'gb_tables'})
boxes(False)
top(-7, -5.5, 44, (1760, 1240)); shot('top_all')
boxes(True); shot('top_boxes')
boxes(False)
scene.render.resolution_x, scene.render.resolution_y = 1280, 800
look((0, 1.4, 5.5), (0, 1.3, -3)); shot('eye_lobby')
look((-6.2, 1.4, -8.5), (-13, 1.3, -3.4)); shot('eye_mit_to_street_door')
look((-14.0, 1.4, -3.1), (-22, 0.2, -4.5), 16); shot('eye_street_from_door')
look((-17.5, 0.8, 5.5), (-20.5, -0.6, -6), 16); shot('eye_street_north')
look((-17.5, 0.8, -14.5), (-22, 0.4, 5), 16); shot('eye_street_south')
look((-19.2, 0.9, -8.2), (-20.5, -1.2, -10.5), 20); shot('eye_canal_trash')
look((6.3, 1.4, -13.2), (10.5, 0.8, -18.5), 16); shot('eye_gobag_easy')
sets({'gb_cabinet_closed', 'gb_drawers_closed', 'gb_crate_closed', 'gb_cooler_closed'})
shot('eye_gobag_hard_closed')
sets({'gb_cabinet_open', 'gb_drawers_open', 'gb_crate_open', 'gb_cooler_open'})
shot('eye_gobag_hard_open')
