"""Renders the people's poses, as Unity shows them, to check them by eye without Unity.

    python Tools/blender/render_poses.py <out.png> [stand | low]      (with the bpy module)

The people move with no skeleton (Unity/Assets/TANGHALAN/Scripts/Runtime/Figure.cs): their parts turn about
pivots at the hips, the knees and the shoulders (P__ points in each model). This script turns the same parts
about the same pivots, by the same angles, in a child (visitor_2) and a grown-up (the teacher):
  stand  standing poses, from the front: Look, Point, Talk, Listen, Wave, Walk, Throw, Catch, Sweep
  low    low poses, from the side: Sit (on a step), Duck, and Cover (under a sturdy table 72 cm high)
Keep the angles below the same as Figure.cs's.

Unity's figure space is x right, y up, z forward (left-handed); Blender's is x right, y forward, z up. A turn
by an angle about Unity's x, y or z is a turn by minus that angle about Blender's x, z or y.
"""
import bpy, sys, os, math
from mathutils import Matrix, Vector

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else sys.argv[1:]
out = os.path.abspath(argv[0])
mode = argv[1] if len(argv) > 1 else 'stand'
R = math.radians


def Rx(a): return Matrix.Rotation(R(a), 4, 'X')
def Ry(a): return Matrix.Rotation(R(a), 4, 'Y')
def Rz(a): return Matrix.Rotation(R(a), 4, 'Z')
def T(v): return Matrix.Translation(Vector(v))


# the poses, as in Figure.cs: drop is how far the hips come down (m), the angles are in degrees (Unity's)
def pose(name, adult, hipY, thigh, shin):
    p = dict(drop=0, lean=0, twist=0, legR=0, legL=0, bendR=0, bendL=0, armRx=0, armRz=0, armLx=0, armLz=0)
    crouch_y = thigh * math.cos(R(-85)) + shin * math.cos(R(-85 + 165)) + (0.07 if adult else 0.06)
    if name == 'Look':
        p.update(lean=-3, **(dict(armRx=-28, armLx=-28, armRz=-14, armLz=14) if adult else dict(armRx=16, armLx=16, armRz=-8, armLz=8)))
    elif name == 'Point': p.update(lean=-4, armRx=-112, armRz=6, armLx=10, armLz=6)
    elif name == 'Talk': p.update(twist=5, lean=1, armRx=-55, armRz=12, armLx=-30, armLz=-8)
    elif name == 'Listen': p.update(lean=6, armRx=-42, armLx=-42, armRz=-38, armLz=38)
    elif name == 'Wave': p.update(armRz=150, armRx=-12)
    elif name == 'Duck': p.update(drop=hipY - crouch_y, legR=-85, legL=-85, bendR=165, bendL=165, lean=58 if adult else 55, armRx=-168, armLx=-168, armRz=-20, armLz=20)
    elif name == 'Cover': p.update(drop=hipY - crouch_y, legR=-85, legL=-85, bendR=165, bendL=165, lean=72 if adult else 70, armRx=-78, armRz=30, armLx=-168, armLz=20)
    elif name == 'Sit': p.update(drop=hipY - 0.08, legR=-105, legL=-105, bendR=95, bendL=95, lean=6, armRx=-55, armLx=-55, armRz=-8, armLz=8)
    elif name == 'Sweep': p.update(lean=24, twist=15, armRx=-52, armRz=-14, armLx=-40, armLz=12, legR=-10, legL=8, bendR=14, bendL=10)
    elif name == 'Throw': p.update(lean=6, armRx=-150, armRz=8, armLx=-30)
    elif name == 'Catch': p.update(armRx=-96, armLx=-96, armRz=-10, armLz=10)
    elif name == 'Walk': p.update(legR=27.5, legL=-27.5, bendR=29.6, bendL=6, armRx=-25, armLx=25)  # mid-stride
    return p


def place(scene, model, name, at):
    with bpy.data.libraries.load(os.path.join(ROOT, 'Blender', 'props', model + '.blend'), link=False) as (src, dst):
        dst.objects = list(src.objects)
    objs = [o for o in dst.objects if o is not None]
    # (objects read from a file are not placed yet: their location is where they are)
    piv = {o.name.split('__')[1].split('.')[0]: o.location.copy() for o in objs if o.name.startswith('P__')}
    hip_y = (piv['hipR'].z + piv['hipL'].z) / 2
    adult = hip_y > 0.62
    thigh = hip_y - (piv['kneeR'].z + piv['kneeL'].z) / 2
    p = pose(name, adult, hip_y, thigh, hip_y - thigh)
    up = Vector((0, 0, hip_y))
    pelvis = T((0, 0, hip_y - p['drop']))
    torso = Rz(-p['twist']) @ Rx(-p['lean'])
    for o in objs:
        if o.type != 'MESH':
            continue
        part = o.name.split('-')[-1].split('.')[0]
        side = part[-1]
        if part == 'body':
            m = pelvis @ torso @ T(-up)
        elif part.startswith('leg'):
            h = piv['hip' + side]
            m = pelvis @ T(h - up) @ Rx(-p['leg' + side]) @ T(-h)
        elif part.startswith('shin'):
            h, k = piv['hip' + side], piv['knee' + side]
            m = pelvis @ T(h - up) @ Rx(-p['leg' + side]) @ T(k - h) @ Rx(-p['bend' + side]) @ T(-k)
        else:
            s = piv['shoulder' + side]
            m = pelvis @ torso @ T(s - up) @ Rx(-p['arm' + side + 'x']) @ Ry(-p['arm' + side + 'z']) @ T(-s)
        scene.collection.objects.link(o)
        o.matrix_world = T(at) @ m @ Matrix.Translation(o.location)


def box(x0, x1, y0, y1, z0, z1, color):
    bpy.ops.mesh.primitive_cube_add(size=1, location=((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2))
    c = bpy.context.object
    c.scale = (x1 - x0, y1 - y0, z1 - z0)
    m = bpy.data.materials.new('m'); m.use_nodes = True
    m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = color
    c.data.materials.append(m)


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    if mode == 'stand':
        for k, name in enumerate(['Look', 'Point', 'Talk', 'Listen', 'Wave', 'Walk', 'Throw', 'Catch', 'Sweep']):
            x = k * 1.25 - 5.0
            place(scene, 'visitor_2', name, (x - 0.3, 0, 0))
            place(scene, 'teacher', name, (x + 0.3, 0, 0))
    else:
        for k, name in enumerate(['Sit', 'Duck', 'Cover']):
            y = k * 2.2 - 2.2
            for dy, model in ((0, 'visitor_2'), (-0.9, 'teacher')):
                place(scene, model, name, (0, y + dy, 0))
                if name == 'Cover':   # a sturdy table over each: its top from 0.72 to 0.78 m
                    box(-0.65, 0.65, y + dy - 0.4, y + dy + 0.4, 0.72, 0.78, (0.55, 0.36, 0.2, 1))
                if name == 'Sit':     # a doorstep 10 cm high: the ground in front is lower
                    box(-0.4, 0.4, y + dy - 0.35, y + dy + 0.05, -0.1, 0.0, (0.78, 0.76, 0.7, 1))
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = 20
    world = bpy.data.worlds.new('W'); scene.world = world; world.use_nodes = True
    world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.92, 0.9, 0.86, 1)
    sun = bpy.data.objects.new('Sun', bpy.data.lights.new('Sun', 'SUN')); sun.data.energy = 2.5; sun.rotation_euler = (0.6, 0.2, 2.2)
    scene.collection.objects.link(sun)
    bpy.ops.mesh.primitive_plane_add(size=30, location=(0, 0, 0 if mode == 'stand' else -0.1))
    cam = bpy.data.objects.new('Cam', bpy.data.cameras.new('Cam')); scene.collection.objects.link(cam); scene.camera = cam
    cam.data.type = 'ORTHO'
    if mode == 'stand':   # from the front
        cam.location = (0, 10, 0.95); cam.rotation_euler = (R(90), 0, R(180)); cam.data.ortho_scale = 12.5
    else:                 # from the side: they face right
        cam.location = (10, -0.4, 0.7); cam.rotation_euler = (R(90), 0, R(90)); cam.data.ortho_scale = 8.2
    scene.render.resolution_x, scene.render.resolution_y = 1800, 1000
    scene.render.filepath = out
    bpy.ops.render.render(write_still=True)
    print('rendered', out)


if __name__ == '__main__':
    main()
