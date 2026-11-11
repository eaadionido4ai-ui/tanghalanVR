// The street outside the Stop Hazards Early hall: "Ang Kanal sa Aming Kalye". New for TANGHALAN VR, drawn
// in the museum's low-poly style with the same helpers. It lies west of the museum, in the same coordinates
// (metres; north is -z), and on a one-metre grid so that the flood model's cells line up with what you see:
//
//   x -27..-23  house yards       ground at -0.40     (cells i 0..3)
//   x -23..-21  west sidewalk     ground at -0.45     (cells i 4..5)
//   x -21..-20  the canal         bed at -1.35 (south) down to -1.55 (north)   (cell i 6)
//   x -20..-15  the road          ground at -0.60     (cells i 7..11)
//   x -15..-13  the museum's terrace, at the museum's floor level, 0.00       (cells i 12..13)
//   z -17..8    north to south    (cells j 0..24); the covered court, at 0.00, is at z 3..8 on the west side
//
// These numbers are also in Unity (Assets/TANGHALAN/Scripts/Core/StreetPlan.cs) and in the GAMA model
// (GAMA/models/TANGHALAN Flood.gaml). Change them in all three places.

import { markerBox, anchor, calibration, flatten } from './tag.js';
import { STREET_DOOR } from './museum.js';

export const PLAN = {
  x0: -27, z0: -17, nx: 14, nz: 25, cell: 1,
  yard: -0.40, wside: -0.45, road: -0.60, terrace: 0, court: 0,
  canalI: 6, canalSouth: -1.35, canalNorth: -1.55,
  courtJ: 20, // cells j >= 20 (z >= 3) on i 0..5 are the court
  houseDoor: { i: 3, j: 10 }, // the cell in front of Rico's door, where the gauge stands
};
// where the trash lies in the canal: [canal cell j, how many pieces]
export const WASTE_CLUSTERS = [[3, 2], [6, 3], [9, 2], [12, 3], [16, 2], [17, 2]];
const BINS = [['bio', -5.0, 0x2e9c4f, 'NABUBULOK', 'Biodegradable', '🍌'], ['rec', -5.8, 0x2f74d0, 'NARE-RECYCLE', 'Recyclable', '♻️'], ['res', -6.6, 0x3b4046, 'DI-NABUBULOK', 'Residual', '🗑️']];

export function buildStreet(T, W, facadeColor) {
  const root = new T.Group(); root.userData.group = 'street';
  const extra = [];
  const textures = new Map();
  const lam = W.lam;
  // a box from corner to corner
  const B = (x0, x1, y0, y1, z0, z1, color, parent) => W.box(Math.abs(x1 - x0), Math.abs(y1 - y0), Math.abs(z1 - z0), color, (x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2, parent || root);
  const COL = (id, x0, x1, y0, y1, z0, z1) => extra.push(markerBox('COL', 'st_' + id, x0, x1, y0, y1, z0, z1));
  const FLOOR = (id, x0, x1, top, z0, z1) => extra.push(markerBox('FLOOR', 'st_' + id, x0, x1, top - 0.2, top, z0, z1));
  const A = (...a) => extra.push(...anchor(...a));
  const picture = (name, w, h, cv, x, y, z, yaw) => { // a flat picture, facing the way yaw says (0 faces +z)
    const m = W.plane(w, h, W.basic(W.tex(cv))); m.userData.texName = name; m.position.set(x, y, z); m.rotation.y = yaw; root.add(m); textures.set(name, cv); return m;
  };
  const P = PLAN, ZN = P.z0, ZS = P.z0 + P.nz; // north and south ends
  const DA = STREET_DOOR.from, DB = STREET_DOOR.to; // the museum's doorway
  const SA = DA - 0.2, SB = DB + 0.2;               // the steps in front of it
  const BA = (DA + DB) / 2 - 0.6, BB = (DA + DB) / 2 + 0.6; // the bridge straight ahead

  // ---- the ground ----
  const CONC = 0xd9d4c7, ASPHALT = 0x5b5e63, LINER = 0x9a968c, MUD = 0x6b5e4b, GRASS = 0x8fae6a;
  B(-15, -13.15, -0.75, 0, ZN, ZS, CONC);                                   // the terrace
  B(-15.02, -14.86, 0, 0.42, ZN, SA - 0.2, 0xb9b2a2); B(-15.02, -14.86, 0, 0.42, SB + 0.2, ZS, 0xb9b2a2); // its low wall
  COL('terrace_wall_n', -15.02, -14.86, 0, 1.1, ZN, SA - 0.2); COL('terrace_wall_s', -15.02, -14.86, 0, 1.1, SB + 0.2, ZS);
  FLOOR('terrace', -15, -13.15, 0, ZN, ZS);
  for (let k = 1; k <= 3; k++) {                                            // the steps down to the road
    B(-15 - 0.3 * k, -15 - 0.3 * (k - 1), -0.75, -0.15 * k, SA, SB, 0xcfc9bb);
    FLOOR('stair_' + k, -15 - 0.3 * k, -15 - 0.3 * (k - 1), -0.15 * k, SA, SB);
  }
  B(-15.9, -15, -0.75, 0.02, SA - 0.12, SA, 0xb9b2a2); B(-15.9, -15, -0.75, 0.02, SB, SB + 0.12, 0xb9b2a2);  // cheeks beside the steps
  B(-20, -15, -0.9, P.road, ZN, ZS, ASPHALT);                               // the road
  for (let z = ZN + 1.2; z < ZS - 1; z += 2.4) B(-17.56, -17.44, P.road, P.road + 0.006, z, z + 1.2, 0xe8c84a);
  FLOOR('road', -20, -15, P.road, ZN, ZS);
  B(-23, -21, -0.75, P.wside, ZN, 3, CONC);                                 // the west sidewalk
  FLOOR('wside', -23, -21, P.wside, ZN, 3);
  B(-27, -23, -0.7, P.yard, ZN, 3, GRASS);                                  // the yards
  // the canal: a sloping bed between two concrete sides, a culvert at each end
  const bed = W.box(1, 0.1, P.nz + 0.02, MUD, -20.5, (P.canalSouth + P.canalNorth) / 2 - 0.05, (ZN + ZS) / 2, root);
  bed.rotation.x = -Math.atan((P.canalSouth - P.canalNorth) / P.nz);
  B(-20.06, -20, -1.7, P.road, ZN, ZS, LINER); B(-21, -20.94, -1.7, P.wside, ZN, 3, LINER); B(-21, -20.94, -1.7, 0, 3, ZS, LINER);
  B(-21, -20, -1.7, P.wside, ZN - 0.1, ZN, 0x24262a); B(-21, -20, -1.7, 0, ZS, ZS + 0.1, 0x24262a);
  B(-20.02, -19.9, P.road, P.road + 0.1, ZN, BA, 0xc7c1b3); B(-20.02, -19.9, P.road, P.road + 0.1, BB, 1.2, 0xc7c1b3); B(-20.02, -19.9, P.road, P.road + 0.1, 2.4, ZS, 0xc7c1b3); // a low kerb
  COL('canal_e1', -20.06, -19.96, P.road, P.road + 0.34, ZN, BA); COL('canal_e2', -20.06, -19.96, P.road, P.road + 0.34, BB, 1.2); COL('canal_e3', -20.06, -19.96, P.road, P.road + 0.34, 2.4, ZS);
  COL('canal_w1', -21.04, -20.94, P.wside, P.wside + 0.34, ZN, BA); COL('canal_w2', -21.04, -20.94, P.wside, P.wside + 0.34, BB, 1.2); COL('canal_w3', -21.04, -20.94, P.wside, P.wside + 0.34, 2.4, 3);
  // two slab bridges with rails
  [[BA, BB, 'bridge1'], [1.2, 2.4, 'bridge2']].forEach(([za, zb, id]) => {
    B(-21.02, -19.98, P.road - 0.08, P.road, za, zb, 0xbdb7a8);
    FLOOR(id, -21, -20, P.road, za, zb);
    [za, zb - 0.06].forEach((z, n) => {
      B(-21, -20, P.road + 0.78, P.road + 0.84, z, z + 0.06, 0xf2760c);
      [-20.97, -20.5, -20.03].forEach((x) => B(x - 0.03, x + 0.03, P.road, P.road + 0.8, z, z + 0.06, 0xf2760c));
      COL(id + '_rail' + n, -21, -20, P.road, P.road + 1.0, z, z + 0.06);
    });
  });

  // ---- the trash heaps in the canal: what is left when the pieces are gone ----
  let wn = 0;
  WASTE_CLUSTERS.forEach(([j, count]) => {
    const zc = ZN + j + 0.5, bedY = P.canalSouth + (P.canalNorth - P.canalSouth) * (ZS - zc) / P.nz;
    const heap = new T.Mesh(new T.SphereGeometry(0.46, 14, 8), lam(0x5a4a3a)); heap.scale.set(1, 0.42, 1.05); heap.position.set(-20.5, bedY + 0.06, zc); root.add(heap);
    for (let k = 0; k < count; k++) {
      const dx = [-0.2, 0.18, 0.0][k % 3], dz = [-0.22, 0.2, 0.02][k % 3];
      A('waste_' + String(wn++).padStart(2, '0'), -20.5 + dx, bedY + 0.3, zc + dz);
    }
  });

  // ---- the bins ----
  BINS.forEach(([kind, z, color, fil, en, emoji]) => {
    const x = -19.4;
    B(x - 0.27, x + 0.27, P.road, P.road + 0.72, z - 0.27, z + 0.27, color);
    B(x - 0.3, x + 0.3, P.road + 0.72, P.road + 0.78, z - 0.3, z + 0.3, 0x24262a);
    B(x - 0.2, x + 0.2, P.road + 0.74, P.road + 0.785, z - 0.2, z + 0.2, 0x0c0d0f);
    const cv = W.canvas(512, 512, (c, w, h) => {
      c.fillStyle = '#ffffff'; W.roundRect(c, 0, 0, w, h, 60); c.fill();
      c.textAlign = 'center'; c.textBaseline = 'middle';
      c.font = '190px "Noto Color Emoji","Apple Color Emoji","Segoe UI Emoji",sans-serif'; c.fillText(emoji, w / 2, 170);
      c.fillStyle = '#' + color.toString(16).padStart(6, '0'); W.fitText(c, fil, w - 50, 86, 800, '"Baloo 2"'); c.fillText(fil, w / 2, 340);
      c.fillStyle = '#16303f'; W.fitText(c, en, w - 70, 60, 900, 'Nunito'); c.fillText(en, w / 2, 430);
    });
    picture('bin_' + kind, 0.46, 0.46, cv, x + 0.275, P.road + 0.38, z, Math.PI / 2);
    extra.push(markerBox('ZONE', 'bin_' + kind, x - 0.36, x + 0.36, P.road, P.road + 1.35, z - 0.36, z + 0.36));
  });

  // ---- the houses ----
  const house = (zc, wall, roofColor, trim) => {
    B(-26.8, -23.5, P.yard, 2.15, zc - 2.1, zc + 2.1, wall);
    B(-26.9, -23.4, P.yard, P.yard + 0.45, zc - 2.2, zc + 2.2, trim);
    const roofGeo = new T.ConeGeometry(Math.SQRT2, 1.25, 4); roofGeo.rotateY(Math.PI / 4); // a pyramid with a 2 x 2 base, square to the walls
    const roof = new T.Mesh(roofGeo, lam(roofColor)); roof.scale.set(1.95, 1, 2.4); roof.position.set(-25.15, 2.15 + 0.62, zc); root.add(roof);
    B(-23.5, -23.44, P.yard, 1.5, zc - 0.45, zc + 0.45, 0x8a5a2b); B(-23.46, -23.41, 0.42, 0.56, zc + 0.24, zc + 0.3, 0xe2c27a); // the door
    B(-23.5, -23.3, P.yard - 0.02, P.yard + 0.1, zc - 0.7, zc + 0.7, 0xc7c1b3);                                             // the doorstep
    [-1.35, 1.35].forEach((dz) => { B(-23.5, -23.45, 0.55, 1.45, zc + dz - 0.4, zc + dz + 0.4, 0xbfe3f2); B(-23.5, -23.43, 0.98, 1.02, zc + dz - 0.4, zc + dz + 0.4, 0xffffff); B(-23.5, -23.43, 0.55, 1.45, zc + dz - 0.02, zc + dz + 0.02, 0xffffff); });
    COL('house_' + Math.round(zc + 20), -26.9, -23.4, P.yard, 3.2, zc - 2.2, zc + 2.2);
  };
  house(-12.6, 0xf3e3c3, 0xb5533c, 0xd9c7a0);
  house(-6.6, 0xcfe6ef, 0x2a6b8f, 0xa9cbd8);   // Rico's house, with the water gauge by the door
  house(-0.6, 0xf6d98a, 0x7a5c3a, 0xd9bb6a);
  // the fence along the yards, with a gate in front of each house's door
  const gates = [-12.6, -6.6, -0.6].map((zc) => [zc - 0.7, zc + 0.7]);
  let fz = ZN;
  gates.concat([[3, 3]]).forEach(([ga, gb], n) => {
    B(-23.06, -23, P.wside, P.wside + 0.62, fz, ga, 0xf4efe4);
    COL('fence_' + n, -23.08, -22.98, P.wside, P.wside + 1.2, fz, ga);
    if (gb < 3) { B(-23.12, -22.94, P.wside, P.wside + 0.8, ga - 0.06, ga, 0x2a6b8f); B(-23.12, -22.94, P.wside, P.wside + 0.8, gb, gb + 0.06, 0x2a6b8f); }
    fz = gb;
  });
  // the water gauge: a child drawn on a board, with marks at the ankle, the knee and the waist
  const gauge = W.canvas(512, 1024, (c, w, h) => {
    c.fillStyle = '#ffffff'; c.fillRect(0, 0, w, h);
    const m = h / 1.2; // pixels per metre; the board is 1.2 m tall and stands on the ground
    [[0.8, 1.2, '#f7c9c4'], [0.4, 0.8, '#fde3b0'], [0.1, 0.4, '#fff5c2'], [0, 0.1, '#dff3e8']].forEach(([a, b, col]) => { c.fillStyle = col; c.fillRect(0, h - b * m, w, (b - a) * m); });
    c.strokeStyle = '#16303f'; c.fillStyle = '#16303f'; c.lineWidth = 4; c.font = '800 30px Nunito'; c.textBaseline = 'middle'; c.textAlign = 'left';
    for (let cm = 10; cm < 120; cm += 10) { const y = h - (cm / 100) * m; c.beginPath(); c.moveTo(0, y); c.lineTo(cm % 50 ? 24 : 44, y); c.stroke(); if (cm % 20 === 0) c.fillText(cm + '', 50, y); }
    // the child, 1.3 m tall, cut off at the top of the board
    c.fillStyle = '#2a6b8f'; c.beginPath(); c.arc(170, h - 1.14 * m, 0.1 * m, 0, Math.PI * 2); c.fill();
    W.roundRect(c, 122, h - 1.02 * m, 96, 0.5 * m, 30); c.fill();
    c.fillRect(130, h - 0.56 * m, 34, 0.56 * m); c.fillRect(176, h - 0.56 * m, 34, 0.56 * m);
    [[0.8, 'baywang', 'waist'], [0.4, 'tuhod', 'knee'], [0.1, 'bukung-bukong', 'ankle']].forEach(([d, fil, en]) => {
      const y = h - d * m;
      c.fillStyle = '#c62f22'; c.fillRect(0, y - 3, w, 6);
      c.fillStyle = '#ffffff'; W.roundRect(c, 236, y - 78, 266, 70, 14); c.fill();
      c.fillStyle = '#16303f'; W.fitText(c, fil, 246, 38, 900, 'Nunito'); c.fillText(fil, 246, y - 56);
      c.fillStyle = '#4f6572'; c.font = '800 26px Nunito'; c.fillText(en, 246, y - 26);
    });
  });
  picture('gauge', 0.6, 1.2, gauge, -23.38, P.yard + 0.6, -5.5, Math.PI / 2);
  B(-23.5, -23.4, P.yard, P.yard + 1.24, -5.83, -5.17, 0x3d3127);
  A('gauge', -23.3, P.yard + 1.45, -5.55, 1, 0, 0);

  // ---- the covered court, the evacuation center ----
  B(-27, -21, -0.7, 0, 3, ZS, CONC); B(-26.4, -21.6, 0, 0.006, 3.5, ZS - 0.5, 0x4f9a6a); B(-26.3, -21.7, 0.006, 0.01, 3.6, ZS - 0.6, 0x5fae7b);
  FLOOR('court', -27, -21, 0, 3, ZS);
  [[2.4, 2.7, -0.30, 'a'], [2.7, 3.0, -0.15, 'b']].forEach(([za, zb, top, id]) => { B(-23, -21, -0.75, top, za, zb, 0xcfc9bb); FLOOR('court_step_' + id, -23, -21, top, za, zb); });
  [[-26.8, 3.2], [-21.2, 3.2], [-26.8, 7.8], [-21.2, 7.8]].forEach(([x, z], n) => { B(x - 0.1, x + 0.1, 0, 3.5, z - 0.1, z + 0.1, 0x2a6b8f); COL('court_post' + n, x - 0.12, x + 0.12, 0, 3.5, z - 0.12, z + 0.12); });
  B(-27.25, -20.75, 3.5, 3.68, 2.85, ZS + 0.15, 0x2a6b8f); B(-27.1, -20.9, 3.68, 3.74, 3.0, ZS, 0x3f86ad);
  const banner = W.canvas(1600, 360, (c, w, h) => {
    c.fillStyle = '#128253'; W.roundRect(c, 0, 0, w, h, 50); c.fill();
    c.fillStyle = '#ffffff'; c.textAlign = 'center'; c.textBaseline = 'middle';
    c.font = '800 150px "Baloo 2"'; c.fillText('EVACUATION CENTER', w / 2, 140);
    c.fillStyle = '#ffe58a'; c.font = '900 72px Nunito'; c.fillText('Covered Court ng Barangay · Ligtas na lugar', w / 2, 280);
  });
  picture('evac_banner', 4.6, 1.03, banner, -24, 2.95, 2.84, Math.PI);
  A('evac_board', -24, 1.5, 7.55, 0, 0, -1);
  COL('court_back', -27, -21, 0, 3, ZS, ZS + 0.2); COL('court_west', -27.2, -27, 0, 3, 3, ZS); COL('court_north', -27, -23, -0.45, 1.0, 2.9, 3.0);
  B(-27, -23, 0, 0.5, 2.94, 3.0, 0xf4efe4);

  // ---- the ends of the street, and what stands behind the houses ----
  B(-27.4, -13.15, -0.75, 2.6, ZN - 0.3, ZN, 0xbfb8a8); COL('end_north', -27.4, -13.15, -1, 3, ZN - 0.3, ZN + 0.02);
  B(-21, -13.15, -0.75, 2.6, ZS, ZS + 0.3, 0xbfb8a8); COL('end_south', -21, -13.15, -1, 3, ZS - 0.02, ZS + 0.3);
  B(-27.9, -27.3, -0.7, 3.4, ZN - 0.3, ZS + 0.3, 0x3f7a45); B(-28.3, -27.6, 3.2, 4.4, ZN - 0.3, ZS + 0.3, 0x4d9153);
  const tree = (x, y, z, s) => {
    const tr = new T.Mesh(new T.CylinderGeometry(0.1 * s, 0.14 * s, 1.5 * s, 8), lam(0x7a5218)); tr.position.set(x, y + 0.75 * s, z); root.add(tr);
    [[0, 1.9, 0, 0.8, 0x3f9a4a], [0.35, 2.3, 0.2, 0.55, 0x55b85e], [-0.3, 2.35, -0.15, 0.5, 0x4aa655]].forEach(([dx, dy, dz, r, col]) => { const m = new T.Mesh(new T.SphereGeometry(r * s, 12, 9), lam(col)); m.position.set(x + dx * s, y + dy * s, z + dz * s); root.add(m); });
    COL('tree_' + Math.round(x * 10) + '_' + Math.round(z * 10), x - 0.2, x + 0.2, y, y + 2, z - 0.2, z + 0.2);
  };
  tree(-14.2, 0, -15.6, 1.2); tree(-14.2, 0, 6.6, 1.1); tree(-25.6, P.yard, -9.6, 1.0); tree(-25.8, P.yard, -3.4, 0.9);
  // a lamp post on the terrace
  B(-14.75, -14.63, 0, 3.2, -9.06, -8.94, 0x3d4a52); B(-15.3, -14.63, 3.14, 3.22, -9.05, -8.95, 0x3d4a52); B(-15.36, -15.12, 3.04, 3.14, -9.1, -8.9, 0xfff3c4);
  COL('lamp', -14.8, -14.58, 0, 3.2, -9.1, -8.9);

  // ---- the museum's west face, with its name ----
  const face = facadeColor;
  B(-13.1, -13, 0, 4.7, ZN, DA, face); B(-13.1, -13, 0, 4.7, DB, ZS, face); B(-13.1, -13, 4.2, 4.7, DA, DB, face);
  B(-13.16, -13.1, 0, 0.55, ZN, DA - 0.14, 0xc9b99a); B(-13.16, -13.1, 0, 0.55, DB + 0.14, ZS, 0xc9b99a);
  B(-13.2, -12.95, 4.7, 4.82, ZN, ZS, 0x16303f);
  const name = W.canvas(1600, 400, (c, w, h) => {
    c.fillStyle = '#16303f'; W.roundRect(c, 0, 0, w, h, 60); c.fill();
    c.fillStyle = '#fff5df'; c.textAlign = 'center'; c.textBaseline = 'middle'; c.font = '800 230px "Baloo 2"'; c.fillText('TANGHALAN', w / 2, 170);
    c.fillStyle = '#ffc53d'; c.font = '900 66px Nunito'; c.fillText('Museo ng mga likha at kuwento ng kabataan', w / 2, 330);
  });
  picture('facade_name', 4.4, 1.1, name, -13.17, 3.75, -9.6, -Math.PI / 2);
  COL('facade_n', -13.2, -13, 0, 4.7, ZN, DA); COL('facade_s', -13.2, -13, 0, 4.7, DB, ZS);

  // ---- anchors ----
  A('street_board', -14.72, 1.5, SA - 1.15, 1, 0, 0);      // the rain warning and the countdown, beside the steps
  A('bayani_street', -14.1, 0, DB + 0.8);
  A('bayani_road', -16.9, P.road, SB + 0.7);
  A('bayani_court', -22.2, 0, 4.3);
  A('street_arrive', -14.2, 0, (DA + DB) / 2, -1, 0, 0);        // where you stand when you step out of the door
  A('rain_center', -18, 6, -4.5);
  A('grid_o', P.x0, 0, P.z0); A('grid_x', P.x0 + P.nx, 0, P.z0); A('grid_z', P.x0, 0, P.z0 + P.nz);
  calibration().forEach((o) => extra.push(o));

  const mats = new Map();
  const material = (col) => { let m = mats.get(col); if (!m) { m = new T.MeshStandardMaterial({ color: col, roughness: 1, metalness: 0 }); mats.set(col, m); } return m; };
  const meshes = flatten(root, { texName: (tex, mesh) => mesh.userData.texName, material, group: () => 'street' });
  return { meshes, extra, textures };
}
