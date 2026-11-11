// Builds the TANGHALAN museum exactly as the web app does (web-src/museum/world.js), then gets it ready for
// VR: a doorway to the street in the Stop Hazards Early hall, the Go Bag Room's furniture as sets Unity can
// switch, and the invisible boxes and anchors Unity needs (see tag.js for the names).

import * as C from '../web-src/museum/content.js';
import { buildWorld } from '../web-src/museum/world.js';
import { markerBox, anchor, calibration, flatten, slug } from './tag.js';

// The doorway to the street, in the west wall of the Stop Hazards Early hall, beside the drawing of the canal.
export const STREET_DOOR = { a: 'mit', b: 'kalye', axis: 'z', at: -13, from: -4.0, to: -1.4 };
// The street is not a museum room (src/street.js builds it). This entry only gives the doorway its color and
// its two signs; it is a sliver so the world builder puts no walls around it.
export const KALYE = { id: 'kalye', name: 'Ang Kalye', icon: '🌧️', sub: 'The street outside', x0: -13.005, x1: -13, z0: -2.7, z1: -2.7, wall: 0xefe2c8, accent: '#1f7a8c',
  hint: 'This is the street from Rico\'s story. The canal is full of trash, and rain is coming.' };

// The Go Bag Room, from web-src (src/rooms/gobag.js in the web app): where its furniture stands.
export const GB_SPOTS = [
  { id: 't1', x: 6.9, z: -16.4, w: 1.6, d: 0.8 },
  { id: 't2', x: 11.1, z: -16.4, w: 1.6, d: 0.8 },
  { id: 't3', x: 9.0, z: -14.2, w: 1.6, d: 0.8 },
  { id: 'c4', x: 11.7, z: -18.95, w: 0.8, d: 0.5 },
];
export const GB_COUNTER = { x: 9, z: -19.32, w: 1.8, d: 0.56 };
// The sturdy tables for the earthquake drill: [room, x, z], each 1.3 by 0.8 m, in a clear spot of the room.
export const DRILL_TABLES = [['lobby', -2.3, 6.9], ['bulwagan', -2.6, -2.6], ['kuwentuhan', -3.4, -19.0], ['mit', -8.0, -8.4],
  ['prep', 8.0, -6.0], ['resp', -8.0, 2.0], ['rec', 7.2, 7.2], ['fireroom', -9.0, -16.0]];

export async function buildMuseum(T) {
  if (!C.ROOMS.find((r) => r.id === 'kalye')) { C.ROOMS.push(KALYE); C.DOORS.push(STREET_DOOR); }

  let loaded;
  const whenLoaded = new Promise((res) => { loaded = res; });
  const world = buildWorld(T, { capabilities: { getMaxAnisotropy: () => 1 } }, () => {}, () => loaded(), {});
  await whenLoaded;
  const { scene, colliders, items } = world;
  const W = world.kit;
  const extra = []; // markers and anchors
  scene.updateMatrixWorld(true);

  // ---- what the VR museum leaves out ----
  const drop = new Set();
  world.ring.traverse((o) => drop.add(o));
  scene.traverse((o) => {
    if (o.isSprite) drop.add(o);
    if (o.isMesh && o.material && o.material.blending === T.AdditiveBlending) drop.add(o); // the soft glows behind the frames
  });
  // the bench that stood where the street doorway now is
  scene.children.forEach((g) => { if (g.isGroup && Math.abs(g.position.x + 12.3) < 0.01 && Math.abs(g.position.z + 2.2) < 0.01) g.traverse((o) => drop.add(o)); });
  const benchAt = colliders.findIndex((c) => Math.abs(c.x0 + 12.6) < 0.01 && Math.abs(c.z0 + 3.25) < 0.01);
  if (benchAt >= 0) colliders.splice(benchAt, 1);

  // ---- the welcome board, reworded for VR ----
  const boards = {};
  items.forEach((it) => { if (it.type === 'intro' || it.type === 'map') boards[it.type] = it.targets[0]; });
  if (boards.intro) {
    const cv = W.canvas(1024, 768, (c, w, h) => {
      c.fillStyle = '#16303f'; W.roundRect(c, 0, 0, w, h, 48); c.fill();
      c.fillStyle = '#ffc53d'; c.font = '900 40px Nunito'; c.fillText('MALIGAYANG PAGDATING!', 70, 104);
      c.fillStyle = '#fff5df'; c.font = '800 96px "Baloo 2"'; c.fillText('TANGHALAN VR', 66, 200);
      c.font = '900 25px Nunito'; c.fillStyle = '#cfe0e8';
      c.fillText('Three-dimensional Animated Gallery Highlighting Youth-Generated', 70, 250);
      c.fillText('Artworks, Learnings and Disaster Narratives', 70, 284);
      c.font = '800 32px Nunito';
      ['👉  Point at a picture or a story and press the trigger', '🚶  Push the stick to walk, or point at the floor to jump there', '🧹  Clear the canal on the street before the rain', '🎒  Pack a go bag, then go to the evacuation center'].forEach((t, i) => c.fillText(t, 70, 352 + i * 76));
      c.fillStyle = '#ffc53d'; c.font = '900 27px Nunito'; c.fillText('The works shown are samples of how', 70, 676); c.fillText('the virtual museum is envisioned.', 70, 712);
    });
    boards.intro.material.map = W.tex(cv);
    boards.intro.userData.texName = 'board_welcome';
  }
  if (boards.map) boards.map.userData.texName = 'board_map';

  // ---- the Go Bag Room's furniture ----
  const gb = buildGoBagRoom(T, W, scene, extra);

  // ---- a sturdy table in each hall, to duck under in the earthquake drill ----
  DRILL_TABLES.forEach(([id, x, z]) => {
    const g = new T.Group(); g.position.set(x, 0, z); g.userData.group = 'drill'; scene.add(g);
    W.box(1.3, 0.06, 0.8, 0x8a5a33, 0, 0.75, 0, g);
    W.box(1.18, 0.1, 0.68, 0x6e4826, 0, 0.67, 0, g);
    [[-1, -1], [1, -1], [-1, 1], [1, 1]].forEach(([a, b]) => W.box(0.09, 0.72, 0.09, 0x5a3a1a, a * 0.58, 0.36, b * 0.33, g));
    extra.push(markerBox('COL', 'drill_' + id, x - 0.65, x + 0.65, 0, 0.78, z - 0.4, z + 0.4));
    extra.push(markerBox('HOT', 'drill_' + id, x - 0.7, x + 0.7, 0, 0.84, z - 0.45, z + 0.45));
    const r = C.ROOMS.find((q) => q.id === id);
    const cx = (r.x0 + r.x1) / 2 - x, cz = (r.z0 + r.z1) / 2 - z;
    extra.push(...anchor('drill_' + id, x, 0, z, cx, 0, cz)); // faces the middle of the room, the way out from under it
  });
  // in the Go Bag Room, the three tables of the Easy round are the sturdy tables
  GB_SPOTS.slice(0, 3).forEach((s) => {
    extra.push(markerBox('HOT', 'drill_gb_' + s.id, s.x - s.w / 2 - 0.05, s.x + s.w / 2 + 0.05, 0, 0.84, s.z - s.d / 2 - 0.05, s.z + s.d / 2 + 0.05, 'gb_tables'));
    extra.push(...anchor('drill_gb_' + s.id, s.x, 0, s.z, 9 - s.x, 0, -15.5 - s.z));
  });

  // ---- flatten: every piece in world space, named for the pipeline ----
  const textures = new Map(); // name -> canvas or image
  const byImage = new Map();
  let cvN = 0;
  const texName = (tex, mesh) => {
    const img = tex.image;
    let name = mesh.userData.texName || byImage.get(img);
    if (!name) {
      if (img.tagName === 'IMG') name = 'work_' + slug(decodeURIComponent(new URL(img.src, location.href).pathname.split('/').pop().replace(/\.[a-z]+$/i, '')).toLowerCase());
      else if (img.width === 256 && img.height === 8) name = 'stripe';
      else if (img.width === 512 && img.height === 512 && tex.wrapS === T.RepeatWrapping) name = 'floor_wood';
      else name = 'cv_' + String(++cvN).padStart(3, '0');
    }
    byImage.set(img, name);
    textures.set(name, img);
    return name;
  };
  const plain = new Map();
  const plainTexture = (tex) => { // the same picture without the web app's repeat, so the file carries plain UVs
    let p = plain.get(tex.image);
    if (!p) { p = new T.Texture(tex.image); p.wrapS = p.wrapT = T.RepeatWrapping; p.needsUpdate = true; plain.set(tex.image, p); }
    return p;
  };
  const mats = new Map();
  const material = (col) => { let m = mats.get(col); if (!m) { m = new T.MeshStandardMaterial({ color: col, roughness: 1, metalness: 0 }); mats.set(col, m); } return m; };
  const group = (o, center) => {
    for (let p = o; p; p = p.parent) { if (p.userData.set) return 'SET:' + p.userData.set; if (p.userData.group) return p.userData.group; }
    const r = C.ROOMS.filter((x) => x.id !== 'kalye').find((x) => center.x >= x.x0 - 0.2 && center.x <= x.x1 + 0.2 && center.z >= x.z0 - 0.2 && center.z <= x.z1 + 0.2);
    if (center.x < -13.001) return 'facade';
    return r ? r.id : 'misc';
  };
  // the floor and the ceiling are single slabs under and over every room
  scene.children.forEach((o) => { if (o.isMesh && o.geometry.parameters && o.geometry.parameters.width === 26 && o.geometry.parameters.height === 29) o.userData.group = 'shell'; });
  const meshes = flatten(scene, { skip: (o) => drop.has(o), texName, plainTexture, material, group });

  // ---- walls and furniture you cannot walk through, as high as what stands there ----
  const boxes = meshes.filter((m) => m.name.startsWith('S__')).map((m) => m.geometry.boundingBox);
  const heightOf = (c) => {
    let top = 0.3;
    boxes.forEach((b) => {
      const cx = (b.min.x + b.max.x) / 2, cz = (b.min.z + b.max.z) / 2;
      if (cx > c.x0 - 0.03 && cx < c.x1 + 0.03 && cz > c.z0 - 0.03 && cz < c.z1 + 0.03 && b.min.y < 2.5 && b.max.y - b.min.y < 4.3) top = Math.max(top, b.max.y);
    });
    return Math.min(C.CEIL, top);
  };
  // a story stand is a thing you point at, so its box is a hot spot instead
  const stands = items.filter((it) => it.type === 'work' && it.work.stand);
  let colN = 0;
  colliders.forEach((c) => {
    const st = stands.find((it) => Math.abs(it.work.stand[0] - (c.x0 + c.x1) / 2) < 0.02 && Math.abs(it.work.stand[1] - (c.z0 + c.z1) / 2) < 0.02);
    if (st) extra.push(markerBox('HOT', 'work_' + slug(st.id), c.x0 - 0.06, c.x1 + 0.06, 0, 1.42, c.z0 - 0.06, c.z1 + 0.06));
    else extra.push(markerBox('COL', 'm' + String(++colN).padStart(3, '0'), c.x0, c.x1, 0, heightOf(c), c.z0, c.z1));
  });
  extra.push(markerBox('COL', 'ceiling', -13, 13, C.CEIL, C.CEIL + 0.1, -20, 9));
  extra.push(markerBox('FLOOR', 'museum', -13, 13, -0.2, 0, -20, 9));
  // the doorway's threshold, through the wall to the terrace outside
  extra.push(markerBox('FLOOR', 'street_door', -13.2, -12.9, -0.2, 0, STREET_DOOR.from, STREET_DOOR.to));

  // ---- the works on the walls, and the boards in the lobby: boxes to point at ----
  const b3 = new T.Box3();
  const hotFor = (id, targets, pad) => {
    b3.makeEmpty(); targets.forEach((m) => { m.updateWorldMatrix(true, false); b3.expandByObject(m); });
    const p = pad == null ? 0.06 : pad;
    extra.push(markerBox('HOT', id, b3.min.x - p, b3.max.x + p, b3.min.y - p, b3.max.y + p, b3.min.z - p, b3.max.z + p));
  };
  items.forEach((it) => {
    if (it.type === 'work' && !it.work.stand) hotFor('work_' + slug(it.id), it.targets);
    else if (it.type === 'intro') hotFor('board_welcome', it.targets, 0.04);
    else if (it.type === 'map') hotFor('board_map', it.targets, 0.04);
  });

  // ---- anchors: where Unity puts its panels and where Bayani waits ----
  const A = (...a) => extra.push(...anchor(...a));
  A('spawn', C.SPAWN.x, 0, C.SPAWN.z, 0, 0, -1);
  A('lobby_start', 0, 1.35, 2.7, 0, 0, 1);            // the welcome panel, floating in front of the visitor
  A('lobby_link', 4.86, 1.5, 2.0, -1, 0, 0);          // the simulation link board, on the lobby's east wall
  A('lobby_tasks', -4.86, 1.5, 2.0, 1, 0, 0);         // the visit's task list, on the lobby's west wall
  const stations = { lobby: [1.6, 2.6], bulwagan: [1.9, -1.6], kuwentuhan: [2.2, -13.3], mit: [-7.0, -3.2], prep: [6.6, -3.4], resp: [-6.6, 2.4], rec: [6.6, 2.4], fireroom: [-6.4, -15.6], gobagroom: [6.2, -12.9] };
  Object.keys(stations).forEach((id) => A('bayani_' + id, stations[id][0], 0, stations[id][1]));
  A('fire_sign', -9, 1.6, -18.2, 0, 0, 1);            // the Fire Safety Room's "coming soon" panel
  gb.anchors.forEach((a) => A(...a));

  calibration().forEach((o) => extra.push(o));

  // what Unity needs to know about each work, beyond its text
  const layout = {
    rooms: C.ROOMS.filter((r) => r.id !== 'kalye').map((r) => ({ id: r.id, name: r.name, sub: r.sub || '', x0: r.x0, x1: r.x1, z0: r.z0, z1: r.z1, hint: r.hint, pillar: r.pillar || '', game: r.game || '' })),
    works: C.WORKS.map((w) => ({ id: w.id, hot: 'work_' + slug(w.id), picture: 'work_' + slug((w.stand ? w.illus : w.id).toLowerCase()), stand: !!w.stand })),
    gobag: gb.data,
  };
  return { meshes, extra, textures, layout, W };
}

// ---------------------------------------------------------------------------------------------------------
// The Go Bag Room: the packing table, the giant bag, the three tables of the Easy level, and the cabinet,
// drawers, crate and cooler of the Difficult level, each of those both closed and open.
function buildGoBagRoom(T, W, scene, extra) {
  const box = W.box, lam = W.lam;
  const r = C.room('gobagroom');
  const root = new T.Group(); scene.add(root);
  const set = (name) => { const g = new T.Group(); g.userData.set = name; root.add(g); return g; };
  const anchors = [];
  const data = { easy: [], hard: [] };

  // the packing table at the north wall, with empty go bags on it
  const counter = new T.Group(); counter.position.set(GB_COUNTER.x, 0, GB_COUNTER.z); root.add(counter);
  box(GB_COUNTER.w, 0.8, GB_COUNTER.d, 0x128253, 0, 0.4, 0, counter);
  box(GB_COUNTER.w + 0.08, 0.06, GB_COUNTER.d + 0.08, 0xb07a3c, 0, 0.83, 0, counter);
  [-0.5, 0, 0.5].forEach((x) => {
    const b = new T.Group(); b.position.set(x, 0.86, 0); counter.add(b);
    box(0.34, 0.4, 0.17, 0xe2382b, 0, 0.2, 0, b); box(0.3, 0.14, 0.06, 0xb8261b, 0, 0.11, 0.1, b); box(0.34, 0.06, 0.18, 0xb8261b, 0, 0.42, 0, b);
  });
  extra.push(markerBox('HOT', 'gb_counter', GB_COUNTER.x - GB_COUNTER.w / 2 - 0.06, GB_COUNTER.x + GB_COUNTER.w / 2 + 0.06, 0, 1.36, GB_COUNTER.z - GB_COUNTER.d / 2, GB_COUNTER.z + GB_COUNTER.d / 2 + 0.06));
  const sign = W.plane(2.6, 0.65, W.basic(W.tex(W.signCanvas('Go Bag Challenge', '#0c7a4a', '#ffffff', 'Take a bag · Pack · Zip it up'))));
  sign.userData.texName = 'sign_gobag';
  W.hang(r, 'N', 9, 2.75, sign);

  // the giant go bag in the corner
  const giant = new T.Group(); giant.position.set(6.6, 0, -19.0); giant.rotation.y = 0.4; root.add(giant);
  W.add(new T.Mesh(new T.CylinderGeometry(0.75, 0.75, 0.12, 32), lam(0xffffff)), 0, 0.06, 0, giant);
  box(0.95, 1.15, 0.55, 0xe2382b, 0, 0.7, 0, giant); box(0.7, 0.45, 0.12, 0xb8261b, 0, 0.55, 0.32, giant); box(0.95, 0.12, 0.6, 0xb8261b, 0, 1.3, 0, giant);
  box(0.12, 1.0, 0.06, 0x7a1a12, -0.28, 0.75, -0.3, giant); box(0.12, 1.0, 0.06, 0x7a1a12, 0.28, 0.75, -0.3, giant);
  W.add(new T.Mesh(new T.TorusGeometry(0.14, 0.03, 8, 16, Math.PI), lam(0x7a1a12)), 0, 1.36, 0, giant);
  extra.push(markerBox('COL', 'gb_giant', 5.95, 7.25, 0, 1.5, -19.65, -18.35));

  // Easy: three tables, with five places on each
  const tables = set('gb_tables');
  GB_SPOTS.slice(0, 3).forEach((s, si) => {
    const g = new T.Group(); g.position.set(s.x, 0, s.z); tables.add(g);
    box(s.w, 0.08, s.d, 0xb07a3c, 0, 0.75, 0, g);
    [[-1, -1], [1, -1], [-1, 1], [1, 1]].forEach(([a, b]) => box(0.07, 0.75, 0.07, 0x6e4826, a * (s.w / 2 - 0.1), 0.375, b * (s.d / 2 - 0.08), g));
    extra.push(markerBox('COL', 'gb_' + s.id, s.x - s.w / 2, s.x + s.w / 2, 0, 0.79, s.z - s.d / 2, s.z + s.d / 2, 'gb_tables'));
    [[-0.52, -0.18], [0, -0.2], [0.52, -0.18], [-0.27, 0.2], [0.27, 0.2]].slice(0, [5, 5, 4][si]).forEach(([dx, dz]) => {
      anchors.push(['gbslot_easy_' + data.easy.length, s.x + dx, 0.79, s.z + dz]);
      data.easy.push({ station: s.id });
    });
  });

  // Difficult: four containers, each closed and open
  const slot = (kind, x, y, z) => { anchors.push(['gbslot_hard_' + data.hard.length, x, y, z]); data.hard.push({ station: kind }); };
  const closedBox = (kind, s, w, h, d) => extra.push(markerBox('HOT', 'gb_open_' + kind, s.x - w / 2 - 0.04, s.x + w / 2 + 0.04, 0, h + 0.04, s.z - d / 2 - 0.04, s.z + d / 2 + 0.09, 'gb_' + kind + '_closed'));
  const openBase = (kind, s, w, d) => extra.push(markerBox('COL', 'gb_' + kind + '_base', s.x - w / 2, s.x + w / 2, 0, 0.02, s.z - d / 2, s.z + d / 2, 'gb_' + kind + '_open'));

  { // the cabinet, doors facing south
    const s = GB_SPOTS[0], cw = 1.3, ch = 1.05, cd = 0.55;
    [false, true].forEach((open) => {
      const g = new T.Group(); g.position.set(s.x, 0, s.z); set('gb_cabinet_' + (open ? 'open' : 'closed')).add(g);
      box(cw, 0.04, cd, 0xc9ad84, 0, 0.02, 0, g); box(cw, 0.04, cd, 0xc9ad84, 0, ch - 0.02, 0, g);
      box(0.04, ch, cd, 0xc9ad84, -cw / 2 + 0.02, ch / 2, 0, g); box(0.04, ch, cd, 0xc9ad84, cw / 2 - 0.02, ch / 2, 0, g);
      box(cw, ch, 0.03, 0xb39670, 0, ch / 2, -cd / 2 + 0.015, g); box(cw - 0.08, 0.03, cd - 0.04, 0xc9ad84, 0, 0.52, 0, g);
      [-1, 1].forEach((sd) => {
        const p = new T.Group(); p.position.set(sd * cw / 2, 0, cd / 2); g.add(p);
        box(cw / 2 - 0.01, ch - 0.04, 0.035, 0xe0c9a2, -sd * (cw / 4), ch / 2, 0.018, p);
        box(0.03, 0.14, 0.04, 0x6e4826, -sd * (cw / 2 - 0.08), ch / 2, 0.05, p);
        if (open) p.rotation.y = -sd * 1.75;
      });
    });
    closedBox('cabinet', s, cw, ch, cd); openBase('cabinet', s, cw, cd);
    [[-0.32, 0.555], [0.32, 0.555], [-0.32, 0.045], [0.32, 0.045]].forEach(([dx, y]) => slot('cabinet', s.x + dx, y, s.z + 0.02));
  }
  { // the chest of drawers
    const s = GB_SPOTS[1], cw = 1.2, ch = 0.82, cd = 0.55;
    const pull = [0.27, 0.47], y0s = [0.43, 0.06];
    [false, true].forEach((open) => {
      const g = new T.Group(); g.position.set(s.x, 0, s.z); set('gb_drawers_' + (open ? 'open' : 'closed')).add(g);
      box(cw, 0.04, cd, 0x8a5a33, 0, ch - 0.02, 0, g); box(0.04, ch, cd, 0x8a5a33, -cw / 2 + 0.02, ch / 2, 0, g); box(0.04, ch, cd, 0x8a5a33, cw / 2 - 0.02, ch / 2, 0, g);
      box(cw, ch, 0.03, 0x744a2a, 0, ch / 2, -cd / 2 + 0.015, g); box(cw, 0.04, cd, 0x8a5a33, 0, 0.02, 0, g);
      y0s.forEach((y0, i) => {
        const p = new T.Group(); g.add(p);
        box(cw - 0.1, 0.03, cd - 0.06, 0xa87346, 0, y0 + 0.015, 0, p);
        box(cw - 0.1, 0.32, 0.03, 0xb98257, 0, y0 + 0.16, cd / 2 - 0.02, p);
        box(cw - 0.1, 0.2, 0.025, 0xa87346, 0, y0 + 0.1, -cd / 2 + 0.05, p);
        box(0.025, 0.2, cd - 0.06, 0xa87346, -(cw - 0.1) / 2, y0 + 0.1, 0, p); box(0.025, 0.2, cd - 0.06, 0xa87346, (cw - 0.1) / 2, y0 + 0.1, 0, p);
        box(0.2, 0.035, 0.04, 0x3d2817, 0, y0 + 0.17, cd / 2 + 0.01, p);
        if (open) p.position.z = pull[i];
      });
    });
    closedBox('drawers', s, cw, ch, cd); openBase('drawers', s, cw, cd + 0.5);
    y0s.forEach((y0, i) => [-0.27, 0.27].forEach((dx) => slot('drawers', s.x + dx, y0 + 0.03, s.z + pull[i])));
  }
  { // the crate, lid hinged at the back
    const s = GB_SPOTS[2], cw = 1.0, ch = 0.5, cd = 0.7;
    [false, true].forEach((open) => {
      const g = new T.Group(); g.position.set(s.x, 0, s.z); set('gb_crate_' + (open ? 'open' : 'closed')).add(g);
      for (let i = 0; i < 4; i++) {
        box(cw, 0.1, 0.03, 0xc79a5b, 0, 0.07 + i * 0.12, cd / 2 - 0.015, g); box(cw, 0.1, 0.03, 0xc79a5b, 0, 0.07 + i * 0.12, -cd / 2 + 0.015, g);
        box(0.03, 0.1, cd, 0xc79a5b, -cw / 2 + 0.015, 0.07 + i * 0.12, 0, g); box(0.03, 0.1, cd, 0xc79a5b, cw / 2 - 0.015, 0.07 + i * 0.12, 0, g);
      }
      box(cw - 0.04, 0.03, cd - 0.04, 0xa87d45, 0, 0.2, 0, g);
      const hinge = new T.Group(); hinge.position.set(0, ch, -cd / 2); g.add(hinge);
      box(cw + 0.04, 0.05, cd + 0.04, 0xb88a4e, 0, 0.025, cd / 2, hinge);
      box(cw + 0.04, 0.02, 0.08, 0x8a6236, 0, 0.06, cd / 2, hinge);
      if (open) hinge.rotation.x = -1.95;
    });
    closedBox('crate', s, cw, ch + 0.07, cd); openBase('crate', s, cw, cd);
    [[-0.3, 0.215, 0], [0.05, 0.215, 0.05], [0.33, 0.215, -0.05]].forEach(([dx, y, dz]) => slot('crate', s.x + dx, y, s.z + dz));
  }
  { // the cooler, lid hinged at the back
    const s = GB_SPOTS[3], cw = 0.8, ch = 0.45, cd = 0.5;
    [false, true].forEach((open) => {
      const g = new T.Group(); g.position.set(s.x, 0, s.z); set('gb_cooler_' + (open ? 'open' : 'closed')).add(g);
      box(cw, 0.04, cd, 0x2f74d0, 0, 0.02, 0, g);
      box(cw, ch, 0.04, 0x2f74d0, 0, ch / 2, cd / 2 - 0.02, g); box(cw, ch, 0.04, 0x2f74d0, 0, ch / 2, -cd / 2 + 0.02, g);
      box(0.04, ch, cd, 0x2f74d0, -cw / 2 + 0.02, ch / 2, 0, g); box(0.04, ch, cd, 0x2f74d0, cw / 2 - 0.02, ch / 2, 0, g);
      box(cw - 0.08, 0.02, cd - 0.08, 0xdfeef8, 0, 0.12, 0, g);
      const hinge = new T.Group(); hinge.position.set(0, ch, -cd / 2); g.add(hinge);
      box(cw + 0.02, 0.08, cd + 0.02, 0xf4f7f9, 0, 0.04, cd / 2, hinge);
      box(0.36, 0.04, 0.05, 0x1f5f95, 0, 0.1, cd / 2, hinge);
      if (open) hinge.rotation.x = -1.7;
    });
    closedBox('cooler', s, cw, ch + 0.12, cd); openBase('cooler', s, cw, cd);
    [[-0.2, 0.13, 0], [0.18, 0.13, -0.05], [0.0, 0.13, 0.08]].forEach(([dx, y, dz]) => slot('cooler', s.x + dx, y, s.z + dz));
  }

  // panels: the level choice and the result above the packing table, your best beside the sign,
  // and the street's water level on the south wall
  anchors.push(['gb_menu', GB_COUNTER.x, 1.75, GB_COUNTER.z + 0.75, 0, 0, 1]);
  anchors.push(['gb_best', 11.75, 1.95, r.z0 + 0.12, 0, 0, 1]);
  anchors.push(['gb_monitor', 7.4, 1.6, r.z1 - 0.12, 0, 0, -1]);
  return { anchors, data };
}
