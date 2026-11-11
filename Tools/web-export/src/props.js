// The things that move: the 24 go bag items (web-src/rooms/items3d.js), the go bag you carry, Bayani
// (web-src/museum/characters.js), and the pieces of trash in the canal (new for TANGHALAN VR).
// Each is exported on its own, standing on y = 0 at the origin.

import { makeItem } from '../web-src/rooms/items3d.js';
import { makeKid } from '../web-src/museum/characters.js';
import { ITEMS } from '../web-src/rooms/logic.js';
import { calibration, flatten } from './tag.js';

// The trash: [model, sort, name, Filipino name, emoji for its icon]
// sort: bio = nabubulok (biodegradable), rec = nare-recycle (recyclable), res = di-nabubulok (residual)
export const WASTE_MODELS = [
  ['banana', 'bio', '🍌'], ['leaves', 'bio', '🍂'], ['coconut', 'bio', '🥥'], ['foodscraps', 'bio', '🍚'],
  ['bottle', 'rec', '🧴'], ['can', 'rec', '🥫'], ['glassbottle', 'rec', '🍾'], ['cardboard', 'rec', '📦'],
  ['sachet', 'res', '🍬'], ['styro', 'res', '🥡'], ['plasticbag', 'res', '🛍️'], ['slipper', 'res', '🩴'],
];

export function makeWaste(T, id) {
  const g = new T.Group();
  const M = (c) => new T.MeshLambertMaterial({ color: c });
  const box = (w, h, d, m) => new T.Mesh(new T.BoxGeometry(w, h, d), m);
  const cyl = (a, b, h, m, s) => new T.Mesh(new T.CylinderGeometry(a, b, h, s || 14), m);
  const sph = (r, m) => new T.Mesh(new T.SphereGeometry(r, 14, 10), m);
  const tor = (r, t, m, arc) => new T.Mesh(new T.TorusGeometry(r, t, 8, 16, arc || Math.PI * 2), m);
  const add = (mesh, x, y, z, rx, ry, rz) => { mesh.position.set(x || 0, y || 0, z || 0); mesh.rotation.set(rx || 0, ry || 0, rz || 0); g.add(mesh); return mesh; };
  const PI = Math.PI;
  switch (id) {
    case 'banana': // a peel, opened into three flaps
      for (let i = 0; i < 3; i++) { const f = add(tor(0.07, 0.022, M(0xf2c53d), PI * 0.75), 0, 0.025, 0, PI / 2, 0, i * (PI * 2 / 3)); f.scale.set(1, 1, 0.5); }
      add(cyl(0.012, 0.016, 0.03, M(0x6b4a22)), 0, 0.04, 0); break;
    case 'leaves':
      [[0, 0, 0, 0x8a6a2f], [0.06, 0.05, 0.6, 0xb5843a], [-0.05, 0.04, 1.4, 0x6f8f3a], [0.02, -0.06, 2.2, 0x9a5a2a], [-0.04, -0.03, 2.9, 0x7a9a44]].forEach(([x, z, r, c], i) => { const l = add(sph(0.06, M(c)), x, 0.012 + i * 0.006, z, 0, r, 0); l.scale.set(1, 0.12, 0.55); });
      add(cyl(0.004, 0.004, 0.16, M(0x5a3a1a)), 0.03, 0.035, 0.02, 0, 0.5, PI / 2); break;
    case 'coconut':
      add(sph(0.085, M(0x7a5a3a)), 0, 0.075).scale.set(1, 0.85, 1);
      add(sph(0.06, M(0xf3ead9)), 0.01, 0.1, 0.02).scale.set(1, 0.5, 1);
      for (let i = 0; i < 5; i++) add(cyl(0.003, 0.003, 0.06, M(0x9a7a4a)), Math.cos(i * 1.26) * 0.07, 0.07, Math.sin(i * 1.26) * 0.07, 0.6, i * 1.26, 0); break;
    case 'foodscraps':
      add(sph(0.075, M(0xf5f0df)), 0, 0.03).scale.set(1.1, 0.4, 0.9);
      add(sph(0.03, M(0xd96a2b)), 0.03, 0.055, 0.01); add(sph(0.026, M(0x5a9a3a)), -0.035, 0.05, 0.02); add(box(0.05, 0.012, 0.02, M(0x8a5a2b)), 0, 0.062, -0.03, 0, 0.5, 0);
      add(cyl(0.004, 0.004, 0.09, M(0xe8e3d6)), -0.02, 0.066, 0, 0, 0.3, PI / 2); break;
    case 'bottle': // a squashed plastic bottle
      add(cyl(0.035, 0.038, 0.16, M(0xa8ddf5), 12), 0, 0.036, 0, 0, 0, PI / 2).scale.set(1, 1, 0.7);
      add(cyl(0.018, 0.034, 0.035, M(0xa8ddf5), 12), 0.095, 0.036, 0, 0, 0, -PI / 2);
      add(cyl(0.017, 0.017, 0.022, M(0x2a6fb0), 12), 0.122, 0.036, 0, 0, 0, PI / 2);
      add(cyl(0.039, 0.039, 0.05, M(0xf4f7f9), 12), -0.01, 0.036, 0, 0, 0, PI / 2).scale.set(1, 1, 0.72); break;
    case 'can':
      add(cyl(0.04, 0.04, 0.11, M(0xb8bcc0)), 0, 0.04, 0, 0, 0, PI / 2);
      add(cyl(0.041, 0.041, 0.07, M(0xcf3b3b)), 0, 0.04, 0, 0, 0, PI / 2);
      add(cyl(0.036, 0.036, 0.004, M(0x8f9499)), 0.056, 0.04, 0, 0, 0, PI / 2); break;
    case 'glassbottle':
      add(cyl(0.034, 0.034, 0.15, M(0x3f8f5a), 12), -0.02, 0.034, 0, 0, 0, PI / 2);
      add(cyl(0.014, 0.03, 0.05, M(0x3f8f5a), 12), 0.08, 0.034, 0, 0, 0, -PI / 2);
      add(cyl(0.014, 0.014, 0.05, M(0x3f8f5a), 12), 0.128, 0.034, 0, 0, 0, PI / 2);
      add(cyl(0.035, 0.035, 0.06, M(0xf3e9c8), 12), -0.03, 0.034, 0, 0, 0, PI / 2); break;
    case 'cardboard':
      add(box(0.22, 0.012, 0.16, M(0xc79a5b)), 0, 0.012, 0, 0, 0.2, 0);
      add(box(0.2, 0.012, 0.07, M(0xb88a4e)), 0.01, 0.026, 0.05, 0.12, 0.2, 0);
      add(box(0.09, 0.003, 0.03, M(0xf3ead9)), -0.03, 0.02, -0.03, 0, 0.2, 0); break;
    case 'sachet':
      add(box(0.09, 0.008, 0.12, M(0xe2382b)), 0, 0.01, 0, 0, 0.3, 0.05);
      add(box(0.06, 0.004, 0.05, M(0xffd23d)), 0, 0.016, 0.01, 0, 0.3, 0.05);
      add(box(0.08, 0.008, 0.11, M(0x2f74d0)), 0.07, 0.014, 0.05, 0, -0.5, -0.06);
      add(box(0.05, 0.004, 0.04, M(0xffffff)), 0.07, 0.02, 0.05, 0, -0.5, -0.06); break;
    case 'styro':
      add(cyl(0.05, 0.036, 0.1, M(0xf8f8f4), 14), 0, 0.045, 0, 0, 0, 1.25);
      add(cyl(0.052, 0.052, 0.008, M(0xe6e6e0), 14), 0.047, 0.06, 0, 0, 0, 1.25);
      add(box(0.11, 0.03, 0.11, M(0xf8f8f4)), -0.1, 0.018, 0.04, 0, 0.3, 0); break;
    case 'plasticbag':
      add(sph(0.085, M(0xfbe3ea)), 0, 0.05).scale.set(1.15, 0.6, 0.9);
      add(sph(0.05, M(0xf7cfdc)), 0.06, 0.07, 0.03).scale.set(1, 0.7, 0.8);
      add(tor(0.035, 0.008, M(0xf7cfdc), PI), -0.05, 0.095, 0, 0, 0.4, 0.3);
      add(tor(0.035, 0.008, M(0xf7cfdc), PI), 0.03, 0.105, -0.02, 0, -0.5, -0.2); break;
    case 'slipper':
      add(box(0.24, 0.022, 0.09, M(0x2aa7d6)), 0, 0.014, 0).scale.set(1, 1, 1);
      add(sph(0.045, M(0x2aa7d6)), 0.12, 0.014, 0).scale.set(1, 0.24, 1); add(sph(0.045, M(0x2aa7d6)), -0.12, 0.014, 0).scale.set(1, 0.24, 1);
      add(tor(0.04, 0.007, M(0xf2760c), PI), 0.05, 0.028, 0, 0, PI / 2, 0); break;
    default:
      add(box(0.1, 0.1, 0.1, M(0x9aa7b0)), 0, 0.05, 0);
  }
  return g;
}

// The go bag you carry: the web app's red bag, with its top open.
export function makeGoBag(T) {
  const g = new T.Group();
  const M = (c) => new T.MeshLambertMaterial({ color: c });
  const box = (w, h, d, c, x, y, z, rx) => { const m = new T.Mesh(new T.BoxGeometry(w, h, d), M(c)); m.position.set(x, y, z); if (rx) m.rotation.x = rx; g.add(m); return m; };
  box(0.34, 0.03, 0.2, 0xb8261b, 0, 0.015, 0);                 // the bottom
  box(0.34, 0.36, 0.025, 0xe2382b, 0, 0.19, 0.0875);           // front
  box(0.34, 0.36, 0.025, 0xe2382b, 0, 0.19, -0.0875);          // back
  box(0.025, 0.36, 0.2, 0xe2382b, -0.1575, 0.19, 0);           // sides
  box(0.025, 0.36, 0.2, 0xe2382b, 0.1575, 0.19, 0);
  box(0.3, 0.15, 0.05, 0xb8261b, 0, 0.12, 0.12);               // the front pocket
  box(0.3, 0.012, 0.012, 0xffd24d, 0, 0.2, 0.146);             // its zip
  box(0.34, 0.03, 0.21, 0xb8261b, 0, 0.42, -0.17, -1.05);      // the flap, open at the back
  box(0.05, 0.3, 0.02, 0x7a1a12, -0.1, 0.2, -0.108); box(0.05, 0.3, 0.02, 0x7a1a12, 0.1, 0.2, -0.108); // straps
  const h = new T.Mesh(new T.TorusGeometry(0.06, 0.012, 8, 14, Math.PI), M(0x7a1a12)); h.position.set(0, 0.37, -0.1); g.add(h);
  box(0.29, 0.02, 0.15, 0x5a120c, 0, 0.05, 0);                 // the dark inside
  return g;
}

export function buildProps(T) {
  const mats = new Map();
  const material = (col) => { let m = mats.get(col); if (!m) { m = new T.MeshStandardMaterial({ color: col, roughness: 1, metalness: 0 }); mats.set(col, m); } return m; };
  const out = {};
  const one = (name, obj, skip) => {
    const meshes = flatten(obj, { material, group: () => name, texName: () => null, skip });
    out[name] = { meshes, extra: calibration() };
  };
  ITEMS.forEach((it) => one('item_' + it.id, makeItem(T, it.id)));
  WASTE_MODELS.forEach(([id]) => one('waste_' + id, makeWaste(T, id)));
  one('gobag', makeGoBag(T));
  // Bayani, waving, turned to face -z so that in Unity he faces forward
  const b = makeKid(T, { shirt: 0x1f6f8b, hat: true, face: true, cape: true, star: true, wave: true, skin: 0xe8ab70 });
  b.pose(0, 0, 0.26, 0.016);
  const turn = new T.Group(); turn.rotation.y = Math.PI; turn.add(b.group);
  one('bayani', turn, (o) => o.material.transparent && o.material.opacity < 0.5); // not the soft shadow
  return out;
}

// Small pictures for the panels: each item's emoji, the stars, a tick and a cross.
export function buildIcons(W) {
  const out = new Map();
  const emoji = (ch) => W.canvas(160, 160, (c) => { c.textAlign = 'center'; c.textBaseline = 'middle'; c.font = '124px "Noto Color Emoji","Apple Color Emoji","Segoe UI Emoji",sans-serif'; c.fillText(ch, 80, 88); });
  ITEMS.forEach((it) => out.set('icon_item_' + it.id, emoji(it.icon)));
  WASTE_MODELS.forEach(([id, , ch]) => out.set('icon_waste_' + id, emoji(ch)));
  const star = (fill, stroke) => W.canvas(160, 160, (c) => {
    c.beginPath();
    for (let i = 0; i < 10; i++) { const a = -Math.PI / 2 + i * Math.PI / 5, r = i % 2 ? 30 : 70; c[i ? 'lineTo' : 'moveTo'](80 + Math.cos(a) * r, 84 + Math.sin(a) * r); }
    c.closePath(); c.fillStyle = fill; c.fill(); c.lineWidth = 8; c.lineJoin = 'round'; c.strokeStyle = stroke; c.stroke();
  });
  out.set('star_on', star('#ffc53d', '#eaa100')); out.set('star_off', star('#e6e1d6', '#cfc8b8'));
  out.set('tick', W.canvas(160, 160, (c) => { c.fillStyle = '#128253'; c.beginPath(); c.arc(80, 80, 72, 0, Math.PI * 2); c.fill(); c.strokeStyle = '#fff'; c.lineWidth = 18; c.lineCap = 'round'; c.lineJoin = 'round'; c.beginPath(); c.moveTo(44, 84); c.lineTo(70, 110); c.lineTo(118, 54); c.stroke(); }));
  out.set('cross', W.canvas(160, 160, (c) => { c.fillStyle = '#c62f22'; c.beginPath(); c.arc(80, 80, 72, 0, Math.PI * 2); c.fill(); c.strokeStyle = '#fff'; c.lineWidth = 18; c.lineCap = 'round'; c.beginPath(); c.moveTo(50, 50); c.lineTo(110, 110); c.moveTo(110, 50); c.lineTo(50, 110); c.stroke(); }));
  return out;
}
