// The people of TANGHALAN VR: Bayani, the neighbors on the street, and the museum's other visitors (a class
// on a field trip, with their teacher).
//
// The children are the web app's own (web-src/museum/characters.js, makeKid). The grown-ups are new for
// TANGHALAN VR, drawn in the same style (makeAdult). Each person is exported in seven parts that Unity moves
// (Assets/TANGHALAN/Scripts/Runtime/Figure.cs): the body, the two thighs, the two shins (with the shoes) and
// the two arms. Pivots (P__) mark where they turn, at the hips, the knees and the shoulders, and where the
// hands are when the arms hang. The legs bend at the knee, so the people can crouch, sit and kneel.
// Every person is turned to face -z, so that in Unity it faces forward (+z).
import { makeKid } from '../web-src/museum/characters.js';
import { calibration, flatten } from './tag.js';

// ---- who they are ----

// The neighbors on the street (Tanghalan.Crowd and StreetLife: the same shirts, in the same order).
//   [shirt, grown-up?, what they wear and look like]
export const NEIGHBORS = [
  ['c8782a', true, { pants: 0x5a3a1a, hair: 0x2b1d16, hairStyle: 'bun', skin: 0xc68a55 }],                           // Aling Rosa, sweeping
  ['e2588a', false, { pants: 0x4a3b6b, hair: 0x3a2418, skin: 0xd69a63, girl: 'pigtails', skirt: false }],               // her daughter, on the doorstep (in shorts: she sits)
  ['2f74d0', true, { pants: 0x3d3127, hair: 0x1d1410, hairStyle: 'short', skin: 0xb87a48 }],                            // Mang Ben, Rico's father
  ['4caf50', false, { pants: 0x35506b, hair: 0x2b1d16, skin: 0xe0aa78 }],                                               // Rico, playing ball
  ['7a53c6', true, { pants: 0x2f3f4f, hair: 0xcfcac2, hairStyle: 'bun', skin: 0xc68a55 }],                           // Lola Ising
  ['f2b300', false, { pants: 0x35506b, hair: 0x1d1410, skin: 0xd69a63, girl: 'long' }],                                 // her granddaughter, playing ball
  ['2fa37a', false, { pants: 0x5a3a1a, hair: 0x2b1d16, skin: 0xb87a48 }],                                               // two children playing tag
  ['e2382b', false, { pants: 0x35506b, hair: 0x3a2418, skin: 0xe0aa78, girl: 'pigtails' }],
];

// The class on a field trip: school uniforms (white tops, navy shorts or skirts), some with school bags.
const WHITE = 0xf4f1e8, NAVY = 0x26395c;
export const VISITORS = [
  ['visitor_1', { hair: 0x1d1410, skin: 0xc68a55, bag: 0x2f74d0 }],
  ['visitor_2', { hair: 0x2b1d16, skin: 0xd69a63, bag: 0xf2b300, girl: 'long' }],
  ['visitor_3', { hair: 0x3a2418, skin: 0xb87a48 }],
  ['visitor_4', { hair: 0x1d1410, skin: 0xe0aa78, bag: 0xe2588a, girl: 'pigtails' }],
  ['visitor_5', { hair: 0x2b1d16, skin: 0xd69a63, bag: 0x4caf50 }],
  ['visitor_6', { hair: 0x4a2f20, skin: 0xc68a55, girl: 'long' }],
];
// (the grown-ups wear trousers: a stiff skirt would stand out behind them when they crouch in the drill)
export const TEACHER = { shirt: 0x2a8c8c, pants: 0x2f3540, hair: 0x2b1d16, hairStyle: 'bun', skin: 0xd69a63, lanyard: 0xc62f22 };

// ---- a grown-up, in the style of the web app's children ----

export function makeAdult(T, o) {
  const g = new T.Group();
  const body = new T.Group();
  g.add(body);
  const skin = o.skin || 0xc68a55;
  const mat = (color, extra) => new T.MeshLambertMaterial(Object.assign({ color }, extra || {}));
  const add = (parent, geo, color, x, y, z, extra) => { const m = new T.Mesh(geo, mat(color, extra)); m.position.set(x || 0, y || 0, z || 0); parent.add(m); return m; };
  const HIP = 0.76, SH = 1.28;

  const knees = [];
  const legs = [-1, 1].map((s) => {
    const hip = new T.Group(); hip.position.set(0.095 * s, HIP, 0); body.add(hip);
    // under a skirt the leg shows; otherwise the trouser leg goes down to the shoe
    const leg = o.skirt ? skin : o.pants || 0x2f3f4f, r = o.skirt ? 0.06 : 0.075;
    add(hip, new T.CylinderGeometry(r, r * 0.92, 0.36, 10), leg, 0, -0.18, 0);
    const knee = new T.Group(); knee.position.set(0, -0.36, 0); hip.add(knee);
    add(knee, new T.CylinderGeometry(r * 0.92, r * 0.84, 0.32, 10), leg, 0, -0.16, 0);
    add(knee, new T.BoxGeometry(0.13, 0.08, 0.24), 0x2b2b2b, 0, -0.36, 0.04);
    knees.push(knee);
    return hip;
  });
  add(body, new T.CylinderGeometry(0.18, 0.19, 0.2, 16), o.pants || 0x2f3f4f, 0, HIP + 0.05, 0);
  if (o.skirt) add(body, new T.CylinderGeometry(0.19, 0.27, 0.42, 16), o.skirt, 0, HIP - 0.12, 0);
  add(body, new T.CylinderGeometry(0.165, 0.19, 0.48, 16), o.shirt, 0, HIP + 0.34, 0);
  const arms = [-1, 1].map((s) => {
    const shoulder = new T.Group(); shoulder.position.set(0.225 * s, SH, 0); body.add(shoulder);
    const arm = new T.Group(); arm.rotation.z = 0.08 * s; arm.position.x = 0.015 * s; shoulder.add(arm);
    add(arm, new T.CylinderGeometry(0.062, 0.058, 0.22, 10), o.shirt, 0, -0.1, 0);          // the sleeve
    add(arm, new T.CylinderGeometry(0.05, 0.045, 0.32, 10), skin, 0, -0.36, 0);             // the forearm
    add(arm, new T.SphereGeometry(0.052, 10, 8), skin, 0, -0.54, 0);                        // the hand
    return shoulder;
  });
  add(body, new T.CylinderGeometry(0.055, 0.06, 0.1, 10), skin, 0, SH + 0.07, 0);           // the neck
  const HEAD = SH + 0.22;
  add(body, new T.SphereGeometry(0.155, 20, 16), skin, 0, HEAD, 0);
  // the face, as the children's
  add(body, new T.SphereGeometry(0.02, 8, 6), 0x2a2320, -0.052, HEAD + 0.02, 0.143);
  add(body, new T.SphereGeometry(0.02, 8, 6), 0x2a2320, 0.052, HEAD + 0.02, 0.143);
  add(body, new T.SphereGeometry(0.024, 8, 6), 0xf29a7a, -0.09, HEAD - 0.035, 0.122);
  add(body, new T.SphereGeometry(0.024, 8, 6), 0xf29a7a, 0.09, HEAD - 0.035, 0.122);
  const smile = add(body, new T.TorusGeometry(0.04, 0.01, 6, 12, Math.PI), 0x2a2320, 0, HEAD - 0.03, 0.148); smile.rotation.z = Math.PI;
  // the hair: a cap, and a bun at the back
  const hair = add(body, new T.SphereGeometry(0.165, 20, 14, 0, Math.PI * 2, 0, Math.PI * (o.hairStyle === 'short' ? 0.5 : 0.6)), o.hair || 0x2b1d16, 0, HEAD + 0.015, -0.012);
  hair.rotation.x = -0.35;
  if (o.hairStyle === 'bun') add(body, new T.SphereGeometry(0.075, 12, 10), o.hair || 0x2b1d16, 0, HEAD + 0.04, -0.16);
  // a teacher's ID on a lanyard
  if (o.lanyard) {
    const strap = (s) => { const m = add(body, new T.BoxGeometry(0.025, 0.3, 0.01), o.lanyard, 0.06 * s, SH - 0.06, 0.168); m.rotation.z = 0.22 * s; };
    strap(-1); strap(1);
    add(body, new T.BoxGeometry(0.085, 0.11, 0.012), 0xffffff, 0, SH - 0.24, 0.182);
    add(body, new T.BoxGeometry(0.085, 0.025, 0.014), o.lanyard, 0, SH - 0.2, 0.183);
  }
  return { group: g, body, legs, knees, arms };
}

// ---- a child: the web app's makeKid, with long hair or pigtails and a skirt for the girls ----

export function makeChild(T, o) {
  const k = makeKid(T, Object.assign({ face: true }, o));
  k.pose(0, 0, 0, 0.016); // standing, arms down
  const body = k.group.children[0];
  // makeKid hangs the legs from groups at hip height (y 0.48) and the arms from groups at shoulder height (y 0.98)
  const legs = body.children.filter((c) => !c.isMesh && Math.abs(c.position.y - 0.48) < 0.01).sort((a, b) => a.position.x - b.position.x);
  const arms = body.children.filter((c) => !c.isMesh && Math.abs(c.position.y - 0.98) < 0.01).sort((a, b) => a.position.x - b.position.x);
  if (legs.length !== 2 || arms.length !== 2) throw new Error('makeKid has changed: cannot find the legs and arms');
  const mat = (color) => new T.MeshLambertMaterial({ color });
  // the web app's leg is one piece (a 42 cm cylinder and a shoe); here the same leg, cut at the knee
  const skin = o.skin || 0xd69a63;
  const knees = legs.map((hip) => {
    hip.children.slice().forEach((c) => hip.remove(c));
    const thigh = new T.Mesh(new T.CylinderGeometry(0.065, 0.0625, 0.21, 10), mat(skin)); thigh.position.y = -0.105; hip.add(thigh);
    const knee = new T.Group(); knee.position.y = -0.21; hip.add(knee);
    const shin = new T.Mesh(new T.CylinderGeometry(0.0625, 0.06, 0.21, 10), mat(skin)); shin.position.y = -0.105; knee.add(shin);
    const shoe = new T.Mesh(new T.BoxGeometry(0.13, 0.08, 0.2), mat(0x2b2b2b)); shoe.position.set(0, -0.22, 0.03); knee.add(shoe);
    return knee;
  });
  const add = (geo, color, x, y, z) => { const m = new T.Mesh(geo, mat(color)); m.position.set(x, y, z); body.add(m); return m; };
  if (o.girl && o.skirt !== false) add(new T.CylinderGeometry(0.175, 0.25, 0.24, 16), o.pants || 0x35506b, 0, 0.47, 0); // a skirt
  if (o.girl === 'long') add(new T.BoxGeometry(0.3, 0.32, 0.07), o.hair || 0x2b1d16, 0, 1.07, -0.15);
  if (o.girl === 'pigtails') [-1, 1].forEach((s) => add(new T.SphereGeometry(0.075, 10, 8), o.hair || 0x2b1d16, 0.2 * s, 1.12, -0.07));
  return { group: k.group, body, legs, knees, arms };
}

// ---- export: five parts and the pivots ----

// Exports one person as <name>-body, <name>-legR, <name>-legL (the thighs), <name>-shinR, <name>-shinL (with
// the shoes), <name>-armR, <name>-armL, with the pivots P__hipR, P__hipL, P__kneeR, P__kneeL, P__shoulderR,
// P__shoulderL, the hands P__handR, P__handL and the top of the head P__top. "R" is the person's own right.
export function exportPerson(T, name, person, extraFn) {
  const turn = new T.Group(); turn.rotation.y = Math.PI; turn.add(person.group);
  turn.updateMatrixWorld(true);
  const v = new T.Vector3();
  // after the half turn the person faces -z, and their right is +x
  const side = (o) => (o.getWorldPosition(v), v.x > 0 ? 'R' : 'L');
  const part = new Map();
  person.legs.forEach((o) => part.set(o, 'leg' + side(o)));
  person.knees.forEach((o) => part.set(o, 'shin' + side(o)));
  person.arms.forEach((o) => part.set(o, 'arm' + side(o)));
  const mats = new Map();
  const material = (col) => { let m = mats.get(col); if (!m) { m = new T.MeshStandardMaterial({ color: col, roughness: 1, metalness: 0 }); mats.set(col, m); } return m; };
  const group = (mesh) => {
    for (let p = mesh; p; p = p.parent) if (part.has(p)) return name + '-' + part.get(p);
    return name + '-body';
  };
  const skip = (o) => o.material.transparent && o.material.opacity < 0.5; // the soft shadow
  const meshes = flatten(turn, { material, group, texName: () => null, skip });
  // the pivots, and the hands: the lowest point of each arm
  const extra = calibration();
  const point = (id, p) => { const e = new T.Object3D(); e.name = 'P__' + id; e.position.copy(p); e.userData.marker = 'P'; extra.push(e); };
  person.legs.forEach((o) => point('hip' + side(o), o.getWorldPosition(new T.Vector3())));
  person.knees.forEach((o) => point('knee' + side(o), o.getWorldPosition(new T.Vector3())));
  person.arms.forEach((o) => {
    point('shoulder' + side(o), o.getWorldPosition(new T.Vector3()));
    const box = new T.Box3().setFromObject(o);
    point('hand' + side(o), new T.Vector3((box.min.x + box.max.x) / 2, box.min.y + 0.04, (box.min.z + box.max.z) / 2));
  });
  const all = new T.Box3().setFromObject(turn);
  point('top', new T.Vector3(0, all.max.y, 0));
  if (extraFn) extraFn(extra);
  return { meshes, extra };
}

// ---- things the neighbors hold ----

// a walis tingting: the broom of thin coconut-leaf ribs that sweeps yards and streets, tied near the top
export function makeBroom(T) {
  const g = new T.Group();
  const M = (c) => new T.MeshLambertMaterial({ color: c });
  const n = 18;
  for (let i = 0; i < n; i++) {
    const a = (i / n) * Math.PI * 2, r = 0.02 + (i % 3) * 0.012;
    const rib = new T.Mesh(new T.CylinderGeometry(0.004, 0.0055, 1.05, 4), M(i % 2 ? 0xc9a66b : 0xb8945a));
    // the ribs spread out towards the bottom
    rib.position.set(Math.cos(a) * r * 2.2, 0.52, Math.sin(a) * r * 2.2);
    rib.rotation.set(Math.sin(a) * r * 2.2, 0, -Math.cos(a) * r * 2.2);
    g.add(rib);
  }
  const tie = new T.Mesh(new T.CylinderGeometry(0.03, 0.03, 0.06, 10), M(0xd94a3a)); tie.position.y = 0.92; g.add(tie);
  return g;
}

export function makeBall(T) {
  const g = new T.Group();
  const M = (c) => new T.MeshLambertMaterial({ color: c });
  const b = new T.Mesh(new T.SphereGeometry(0.09, 14, 10), M(0xe2382b)); b.position.y = 0.09; g.add(b);
  const band = new T.Mesh(new T.TorusGeometry(0.091, 0.012, 6, 18), M(0xffd24d)); band.position.y = 0.09; band.rotation.x = Math.PI / 2; g.add(band);
  return g;
}

export { WHITE, NAVY };
