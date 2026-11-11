// Builds the TANGHALAN museum with the app's three.js (window.THREE, r128), in the games'
// low-poly style: rooms with tinted walls and colored doorways, framed works with spotlight
// glows, story stands with open books, signs, and furniture. Returns the scene, the walls and
// furniture to collide with, and the things a player can look at.

import { CEIL, DOOR_H, ROOMS, DOORS, WORKS, NAME_PARTS, room, wallPoint, spotOf, imageOf } from './content.js';

const PANEL = 0.1; // each room has its own wall panels, so a shared wall is two panels back to back
const WAINSCOT = 0xe4d3b5;
const FRAMES = { mit: 0x2a6b8f, prep: 0x128253, resp: 0xf2760c, rec: 0x7a53c6 };
const STORY_FRAME = 0xeaa100;
const PILLAR_HEX = ['#2a6b8f', '#128253', '#f2760c', '#7a53c6'];
// which wall each room's banner hangs on
const BANNERS = { mit: ['W', -6], prep: ['E', -6], resp: ['W', 4.5], rec: ['E', 4.5], fireroom: ['N', -9], gobagroom: ['N', 9] };

// The four sides of a room: the wall line, the direction into the room, and how far it runs.
function side(r, s) {
  if (s === 'N') return { horiz: true, line: r.z0, dir: 1, a: r.x0, b: r.x1 };
  if (s === 'S') return { horiz: true, line: r.z1, dir: -1, a: r.x0, b: r.x1 };
  if (s === 'W') return { horiz: false, line: r.x0, dir: 1, a: r.z0, b: r.z1 };
  return { horiz: false, line: r.x1, dir: -1, a: r.z0, b: r.z1 };
}
// Doors in a room's side, as gaps along it, with the room on the other side.
function gaps(r, s) {
  const sd = side(r, s);
  return DOORS.filter((d) => (d.a === r.id || d.b === r.id) && (d.axis === 'x') === sd.horiz && d.at === sd.line)
    .map((d) => ({ from: d.from, to: d.to, other: room(d.a === r.id ? d.b : d.a) }))
    .sort((p, q) => p.from - q.from);
}

function roundRect(c, x, y, w, h, r) { c.beginPath(); c.moveTo(x + r, y); c.arcTo(x + w, y, x + w, y + h, r); c.arcTo(x + w, y + h, x, y + h, r); c.arcTo(x, y + h, x, y, r); c.arcTo(x, y, x + w, y, r); c.closePath(); }
function canvas(w, h, paint) { const cv = document.createElement('canvas'); cv.width = w; cv.height = h; paint(cv.getContext('2d'), w, h); return cv; }
function drawLogo(c, x, y, s) { // logo 1b, the Bayanihan house
  c.save(); c.translate(x, y); c.scale(s / 100, s / 100);
  roundRect(c, 0, 0, 100, 100, 25); c.fillStyle = '#cfe6ef'; c.fill(); c.clip();
  c.fillStyle = '#16303f'; c.strokeStyle = '#16303f'; c.lineWidth = 6; c.lineJoin = 'round';
  c.beginPath(); c.moveTo(50, 18); c.lineTo(77, 40); c.lineTo(23, 40); c.closePath(); c.fill(); c.stroke();
  c.fillStyle = '#fff5df'; c.fillRect(30, 39, 40, 18);
  c.fillStyle = '#f2760c'; roundRect(c, 45, 44, 10, 13, 2); c.fill();
  c.fillStyle = '#eaa100'; roundRect(c, 18, 56, 64, 6, 3); c.fill();
  [[27, '#2a6b8f'], [42.3, '#128253'], [57.7, '#f2760c'], [73, '#7a53c6']].forEach(([px, col]) => {
    c.fillStyle = col; c.beginPath(); c.arc(px, 68.5, 4.6, 0, Math.PI * 2); c.fill(); roundRect(c, px - 5, 74.5, 10, 14, 5); c.fill();
  });
  c.restore();
}
function fitText(c, text, max, size, weight, family) {
  let s = size;
  do { c.font = weight + ' ' + s + 'px ' + family; s -= 4; } while (c.measureText(text).width > max && s > 12);
}
function floorCanvas() {
  let seed = 9; const r = () => (seed = (seed * 16807) % 2147483647) / 2147483647;
  return canvas(512, 512, (c) => {
    const cw = 512 / 6;
    for (let i = 0; i < 6; i++) {
      let y = -r() * 220;
      while (y < 512) {
        const len = 170 + r() * 220, k = 0.88 + r() * 0.2;
        c.fillStyle = 'rgb(' + Math.round(214 * k) + ',' + Math.round(170 * k) + ',' + Math.round(116 * k) + ')';
        c.fillRect(i * cw, y, cw, len);
        c.globalAlpha = 0.13; c.strokeStyle = '#7a5530'; c.lineWidth = 1.2;
        for (let g = 0; g < 5; g++) { const x = i * cw + 5 + r() * (cw - 10); c.beginPath(); c.moveTo(x, y); c.bezierCurveTo(x + 3, y + len * 0.3, x - 3, y + len * 0.6, x + 1, y + len); c.stroke(); }
        c.globalAlpha = 1; c.fillStyle = 'rgba(90,58,30,.5)'; c.fillRect(i * cw, y, cw, 2);
        y += len;
      }
      c.fillStyle = 'rgba(90,58,30,.55)'; c.fillRect(i * cw, 0, 2, 512);
    }
  });
}
function signCanvas(text, bg, fg, sub) {
  return canvas(1200, sub ? 300 : 260, (c, w, h) => {
    c.fillStyle = bg; roundRect(c, 0, 0, w, h, 70); c.fill();
    c.fillStyle = fg || '#ffffff'; c.textAlign = 'center'; c.textBaseline = 'middle';
    fitText(c, text, w - 120, sub ? 116 : 124, 800, '"Baloo 2"'); c.fillText(text, w / 2, sub ? 120 : h / 2 + 12);
    if (sub) { c.globalAlpha = 0.85; fitText(c, sub, w - 160, 54, 900, 'Nunito'); c.fillText(sub, w / 2, 222); }
  });
}
function bubbleCanvas(emoji, ring) {
  return canvas(256, 256, (c) => {
    c.fillStyle = '#ffffff'; c.beginPath(); c.arc(128, 120, 104, 0, Math.PI * 2); c.fill();
    c.lineWidth = 14; c.strokeStyle = ring; c.stroke();
    c.fillStyle = '#ffffff'; c.beginPath(); c.moveTo(100, 214); c.lineTo(128, 252); c.lineTo(156, 214); c.fill();
    c.font = '110px "Noto Color Emoji","Apple Color Emoji","Segoe UI Emoji",sans-serif'; c.textAlign = 'center'; c.textBaseline = 'middle'; c.fillText(emoji, 128, 128);
  });
}
function placardCanvas(w, color) {
  return canvas(360, 240, (c, W, H) => {
    c.fillStyle = '#fff'; c.fillRect(0, 0, W, H);
    c.fillStyle = color; c.fillRect(0, 0, 14, H);
    c.fillStyle = '#16303f'; fitText(c, w.title, W - 50, 42, 800, '"Baloo 2"'); c.fillText(w.title, 32, 62);
    c.fillStyle = '#4f6572'; c.font = '800 28px Nunito';
    c.fillText(w.by + ', ' + w.age, 32, 108);
    c.fillText(w.medium || (w.kind[0].toUpperCase() + w.kind.slice(1)), 32, 148);
    c.fillStyle = '#d9650a'; roundRect(c, 32, 172, 196, 46, 23); c.fill();
    c.fillStyle = '#fff'; c.font = '900 26px Nunito'; c.fillText('SAMPLE ENTRY', 46, 205);
  });
}
// the screen of a game station
function stationCanvas(g, earned) {
  return canvas(1024, 640, (c, w, h) => {
    const grad = c.createLinearGradient(0, 0, 0, h); grad.addColorStop(0, '#1f4e6b'); grad.addColorStop(1, '#0e2233');
    c.fillStyle = grad; c.fillRect(0, 0, w, h);
    c.textAlign = 'center'; c.textBaseline = 'middle';
    c.font = '170px "Noto Color Emoji","Apple Color Emoji","Segoe UI Emoji",sans-serif'; c.fillText(g.icon, w / 2, 170);
    c.fillStyle = '#fff5df'; fitText(c, g.name, w - 80, 84, 800, '"Baloo 2"'); c.fillText(g.name, w / 2, 330);
    c.fillStyle = '#d9650a'; roundRect(c, w / 2 - 190, 400, 380, 110, 55); c.fill();
    c.fillStyle = '#fff'; c.font = '800 72px "Baloo 2"'; c.fillText('▶ PLAY', w / 2, 462);
    c.fillStyle = earned ? '#5fd39b' : '#ffc53d'; c.font = '900 40px Nunito'; c.fillText(earned ? '🏅 Badge earned!' : 'Win a badge for your Champion ID', w / 2, 585);
  });
}
// a poster on a game room's wall
function posterCanvas(title, color, rows) {
  return canvas(820, 1024, (c, w, h) => {
    c.fillStyle = '#fffaf0'; roundRect(c, 0, 0, w, h, 36); c.fill();
    c.fillStyle = color; roundRect(c, 0, 0, w, 170, 36); c.fill(); c.fillRect(0, 120, w, 50);
    c.fillStyle = '#fff'; c.textAlign = 'center'; fitText(c, title, w - 80, 84, 800, '"Baloo 2"'); c.fillText(title, w / 2, 112);
    c.textAlign = 'left';
    rows.forEach(([big, text], i) => {
      const y = 250 + i * (rows.length > 4 ? 140 : 190);
      const emoji = /\p{Extended_Pictographic}/u.test(big);
      c.fillStyle = color; c.font = emoji ? '72px "Noto Color Emoji","Apple Color Emoji","Segoe UI Emoji",sans-serif' : '800 110px "Baloo 2"'; c.fillText(big, 50, y + (emoji ? 26 : 34));
      c.fillStyle = '#16303f'; fitText(c, text, w - 230, 46, 800, 'Nunito'); c.fillText(text, 190, y + 20);
    });
  });
}
function glowCanvas() {
  return canvas(128, 256, (c, w, h) => {
    const g = c.createRadialGradient(w / 2, 40, 6, w / 2, 110, 150);
    g.addColorStop(0, 'rgba(255,240,205,1)'); g.addColorStop(0.55, 'rgba(255,236,196,.45)'); g.addColorStop(1, 'rgba(255,236,196,0)');
    c.fillStyle = g; c.fillRect(0, 0, w, h);
  });
}
function stripeCanvas() {
  return canvas(256, 8, (c) => PILLAR_HEX.forEach((col, i) => { c.fillStyle = col; c.fillRect(i * 64, 0, 64, 8); }));
}
// the museum map, drawn from the same rooms and doors as the 3D world
export function drawMap(c, w, h, opts) {
  const o = opts || {}, pad = o.pad == null ? 18 : o.pad;
  const sx = (w - pad * 2) / 26, sz = (h - pad * 2 - (o.top || 0)) / 29, s = Math.min(sx, sz);
  const ox = (w - 26 * s) / 2, oz = (o.top || 0) + (h - (o.top || 0) - 29 * s) / 2;
  const X = (x) => ox + (x + 13) * s, Z = (z) => oz + (z + 20) * s;
  ROOMS.forEach((r) => {
    c.fillStyle = r.id === 'bulwagan' ? '#16303f' : r.id === 'lobby' ? '#ffffff' : '#' + r.wall.toString(16).padStart(6, '0');
    c.strokeStyle = r.accent; c.lineWidth = Math.max(2, s * 0.22);
    roundRect(c, X(r.x0) + 2, Z(r.z0) + 2, (r.x1 - r.x0) * s - 4, (r.z1 - r.z0) * s - 4, s * 0.6); c.fill(); c.stroke();
  });
  c.fillStyle = '#e9dcc4';
  DOORS.forEach((d) => {
    if (d.axis === 'x') c.fillRect(X(d.from), Z(d.at) - s * 0.5, (d.to - d.from) * s, s);
    else c.fillRect(X(d.at) - s * 0.5, Z(d.from), s, (d.to - d.from) * s);
  });
  if (o.labels !== false) {
    c.textAlign = 'center'; c.textBaseline = 'middle';
    ROOMS.forEach((r) => {
      const cx = X((r.x0 + r.x1) / 2), cz = Z((r.z0 + r.z1) / 2);
      c.font = Math.round(s * 1.5) + 'px "Noto Color Emoji","Apple Color Emoji","Segoe UI Emoji",sans-serif'; c.fillText(r.icon, cx, cz - s * 0.9);
      c.fillStyle = r.id === 'bulwagan' ? '#ffc53d' : '#16303f';
      fitText(c, r.name, (r.x1 - r.x0) * s - 10, Math.round(s * 0.95), 900, 'Nunito'); c.fillText(r.name, cx, cz + s * 0.7);
      c.fillStyle = '#e9dcc4';
    });
  }
  return { X, Z, s };
}

export function buildWorld(T, renderer, onProgress, onLoad, opts) {
  const scene = new T.Scene();
  scene.background = new T.Color(0xf3ead9);
  const textures = [];
  const colliders = [];
  const items = [];
  const aniso = Math.min(8, renderer.capabilities.getMaxAnisotropy());
  const mats = new Map();
  const lam = (color) => { let m = mats.get(color); if (!m) { m = new T.MeshLambertMaterial({ color }); mats.set(color, m); } return m; };
  const tex = (cv) => { const t = new T.CanvasTexture(cv); t.anisotropy = aniso; textures.push(t); return t; };
  const basic = (map, extra) => new T.MeshBasicMaterial(Object.assign({ map, transparent: true }, extra || {}));
  const add = (m, x, y, z, parent) => { m.position.set(x, y, z); (parent || scene).add(m); return m; };
  const box = (w, h, d, color, x, y, z, parent) => add(new T.Mesh(new T.BoxGeometry(w, h, d), typeof color === 'number' ? lam(color) : color), x, y, z, parent);
  const plane = (w, h, mat) => new T.Mesh(new T.PlaneGeometry(w, h), mat);
  const solid = (x0, x1, z0, z1) => colliders.push({ x0: Math.min(x0, x1), x1: Math.max(x0, x1), z0: Math.min(z0, z1), z1: Math.max(z0, z1) });

  const manager = new T.LoadingManager(onLoad, (url, loaded, total) => onProgress(loaded / total));
  manager.onError = (url) => console.warn('TANGHALAN could not load ' + url);
  const loader = new T.TextureLoader(manager);
  const load = (url) => { const t = loader.load(url); t.anisotropy = aniso; textures.push(t); return t; };

  scene.add(new T.HemisphereLight(0xfffaf0, 0x9a8a72, 0.95));
  const sun = new T.DirectionalLight(0xffffff, 0.3); sun.position.set(-3, 9, 4); scene.add(sun);

  const ft = tex(floorCanvas()); ft.wrapS = ft.wrapT = T.RepeatWrapping; ft.repeat.set(26 / 2.8, 29 / 2.85);
  const floor = add(plane(26, 29, new T.MeshLambertMaterial({ map: ft })), 0, 0, -5.5); floor.rotation.x = -Math.PI / 2;
  const ceil = add(plane(26, 29, new T.MeshBasicMaterial({ color: 0xf6f1e8 })), 0, CEIL, -5.5); ceil.rotation.x = Math.PI / 2;

  const stripeTex = tex(stripeCanvas());
  const glowTex = tex(glowCanvas());
  const glowMat = new T.MeshBasicMaterial({ map: glowTex, transparent: true, opacity: 0.55, blending: T.AdditiveBlending, depthWrite: false });
  const bubbleMats = { look: new T.SpriteMaterial({ map: tex(bubbleCanvas('👀', '#f2760c')) }), read: new T.SpriteMaterial({ map: tex(bubbleCanvas('📖', '#eaa100')) }) };

  // A slab against a room's side: from t0 to t1 along it, y0 to y1 high, starting `inset` in from the wall line.
  function slab(r, s, t0, t1, y0, y1, inset, thick, material) {
    const sd = side(r, s), len = t1 - t0, mid = (t0 + t1) / 2, n = sd.line + sd.dir * (inset + thick / 2);
    return sd.horiz
      ? box(len, y1 - y0, thick, material, mid, (y0 + y1) / 2, n)
      : box(thick, y1 - y0, len, material, n, (y0 + y1) / 2, mid);
  }
  // Something flat hung on a side, facing into the room.
  function hang(r, s, along, y, mesh, inset) {
    const p = wallPoint(r, s, along, PANEL + (inset || 0.01));
    mesh.position.set(p.x, y, p.z); mesh.rotation.y = p.yaw; scene.add(mesh); return mesh;
  }

  ROOMS.forEach((r) => {
    const stripeMat = r.pillar ? lam(parseInt(r.accent.slice(1), 16)) : new T.MeshLambertMaterial({ map: stripeTex });
    ['N', 'S', 'W', 'E'].forEach((s) => {
      const sd = side(r, s), gs = gaps(r, s);
      let cur = sd.a;
      gs.concat([{ from: sd.b, to: sd.b }]).forEach((g) => {
        if (g.from - cur > 0.01) {
          slab(r, s, cur, g.from, 0, CEIL, 0, PANEL, r.wall);
          slab(r, s, cur, g.from, 0, 0.55, PANEL, 0.02, WAINSCOT);
          const st = slab(r, s, cur, g.from, 0.55, 0.63, PANEL, 0.03, r.pillar ? stripeMat : stripeMat.clone());
          if (!r.pillar) { st.material.map = stripeTex.clone(); st.material.map.needsUpdate = true; st.material.map.wrapS = T.RepeatWrapping; st.material.map.repeat.set((g.from - cur) / 2.4, 1); textures.push(st.material.map); }
          if (sd.horiz) solid(cur, g.from, sd.line, sd.line + sd.dir * PANEL);
          else solid(sd.line, sd.line + sd.dir * PANEL, cur, g.from);
        }
        if (g.other) {
          const col = parseInt(g.other.accent.slice(1), 16);
          slab(r, s, g.from, g.to, DOOR_H, CEIL, 0, PANEL, r.wall);
          slab(r, s, g.from - 0.14, g.from, 0, DOOR_H + 0.14, PANEL, 0.05, col);
          slab(r, s, g.to, g.to + 0.14, 0, DOOR_H + 0.14, PANEL, 0.05, col);
          slab(r, s, g.from, g.to, DOOR_H, DOOR_H + 0.14, PANEL, 0.05, col);
          const mid = (g.from + g.to) / 2;
          if (r.id === 'lobby' && g.other.id === 'bulwagan') { // the museum's name over its main doorway
            const ban = tex(canvas(1600, 400, (c, w, h) => {
              c.fillStyle = '#16303f'; roundRect(c, 0, 0, w, h, 60); c.fill(); drawLogo(c, 60, 60, 280);
              c.fillStyle = '#fff5df'; c.font = '800 200px "Baloo 2"'; c.fillText('TANGHALAN', 390, 230);
              c.fillStyle = '#ffc53d'; c.font = '900 76px Nunito'; c.fillText('Bulwagan ng Bayanihan →', 396, 330);
            }));
            hang(r, s, mid, 3.66, plane(3.6, 0.9, basic(ban)));
          } else {
            hang(r, s, mid, 3.62, plane(2.5, 0.54, basic(tex(signCanvas(g.other.icon + ' ' + g.other.name, g.other.accent)))));
          }
        }
        cur = g.to;
      });
    });
    if (r.pillar || r.game) { // a soft rug and the room's banner
      const tint = new T.Color(r.accent).lerp(new T.Color(0xffffff), 0.72);
      const rug = box(r.x1 - r.x0 - 3.2, 0.012, r.z1 - r.z0 - 3.4, new T.MeshLambertMaterial({ color: tint }), (r.x0 + r.x1) / 2, 0.006, (r.z0 + r.z1) / 2);
      rug.renderOrder = -1;
      const b = BANNERS[r.id];
      hang(r, b[0], b[1], 3.45, plane(3.6, 0.9, basic(tex(signCanvas(r.icon + ' ' + r.name, r.accent, '#ffffff', r.sub)))));
    }
  });

  // ---- the works ----
  WORKS.forEach((w) => {
    const r = room(w.room);
    const frameColor = w.kind === 'art' ? FRAMES[w.pillar] : STORY_FRAME;
    const item = { id: w.id, type: 'work', work: w, spot: spotOf(w), targets: [] };
    if (w.wall) {
      const [s, along, width] = w.wall;
      const portrait = ['gobag', 'dch', 'sunog', 'tula', 'kuwento', 'liham'].indexOf(w.id) >= 0;
      const h = portrait ? width * 4 / 3 : width * 3 / 4, cy = portrait ? 1.62 : 1.55;
      const g = new T.Group();
      g.add(add(new T.Mesh(new T.BoxGeometry(width + 0.2, h + 0.2, 0.07), lam(frameColor)), 0, 0, 0.035));
      g.add(add(new T.Mesh(new T.BoxGeometry(width + 0.09, h + 0.09, 0.075), lam(0xfdfbf5)), 0, 0, 0.04));
      const pic = add(plane(width, h, new T.MeshBasicMaterial({ map: load(imageOf(w)) })), 0, 0, 0.08, g);
      hang(r, s, along, cy, g);
      hang(r, s, along, cy + 0.32, plane(width + 1.0, h + 1.5, glowMat), 0.004);
      const right = (s === 'N' || s === 'E') ? 1 : -1; // the placard sits on the viewer's right
      hang(r, s, along + right * (width / 2 + 0.42), 1.02, plane(0.36, 0.24, new T.MeshBasicMaterial({ map: tex(placardCanvas(w, '#' + frameColor.toString(16).padStart(6, '0'))) })), 0.012);
      const p = wallPoint(r, s, along, PANEL + 0.35);
      item.center = { x: p.x, y: cy, z: p.z };
      item.bubbleAt = { x: p.x, y: cy + h / 2 + 0.5, z: p.z };
      item.targets.push(pic, g.children[0]);
    } else {
      const [x, z, yaw] = w.stand;
      const g = new T.Group(); g.position.set(x, 0, z); g.rotation.y = yaw; scene.add(g);
      box(0.12, 0.92, 0.12, 0x6e4826, 0, 0.46, 0, g);
      box(0.56, 0.05, 0.4, 0x6e4826, 0, 0.025, 0, g);
      const top = new T.Group(); top.position.set(0, 0.98, 0); top.rotation.x = 0.5; g.add(top);
      const board = box(0.8, 0.05, 0.56, 0x9c6b3f, 0, 0, 0, top);
      const pages = add(plane(0.74, 0.46, new T.MeshBasicMaterial({ map: load(imageOf(w)) })), 0, 0.032, 0, top); pages.rotation.x = -Math.PI / 2;
      solid(x - 0.35, x + 0.35, z - 0.35, z + 0.35);
      item.center = { x, y: 1.0, z };
      item.bubbleAt = { x, y: 1.85, z };
      item.targets.push(pages, board);
    }
    item.bubble = new T.Sprite(w.kind === 'art' ? bubbleMats.look : bubbleMats.read);
    item.bubble.scale.set(0.5, 0.5, 1);
    item.bubble.position.set(item.bubbleAt.x, item.bubbleAt.y, item.bubbleAt.z);
    scene.add(item.bubble);
    items.push(item);
  });

  // ---- the lobby: welcome board, map board, the way out, a rug with the logo ----
  const lobby = room('lobby');
  const welcome = tex(canvas(1024, 768, (c, w, h) => {
    c.fillStyle = '#16303f'; roundRect(c, 0, 0, w, h, 48); c.fill();
    drawLogo(c, 60, 56, 150);
    c.fillStyle = '#ffc53d'; c.font = '900 40px Nunito'; c.fillText('MALIGAYANG PAGDATING!', 240, 104);
    c.fillStyle = '#fff5df'; c.font = '800 96px "Baloo 2"'; c.fillText('TANGHALAN', 236, 196);
    // what the name stands for, on two lines, with the letters that spell it in gold (smaller type if a line runs long)
    [NAME_PARTS.slice(0, 4), NAME_PARTS.slice(4)].forEach((parts, i) => {
      c.font = '900 25px Nunito';
      const long = parts.reduce((sum, [a, b]) => sum + c.measureText(a + b).width, 0);
      c.font = `900 ${Math.min(25, Math.floor((25 * (w - 140)) / long))}px Nunito`;
      let x = 70;
      parts.forEach(([a, b]) => {
        if (a) { c.fillStyle = '#ffc53d'; c.fillText(a, x, 250 + i * 34); x += c.measureText(a).width; }
        c.fillStyle = '#cfe0e8'; c.fillText(b, x, 250 + i * 34); x += c.measureText(b).width;
      });
    });
    c.fillStyle = '#cfe0e8'; c.font = '800 32px Nunito';
    ['🕹️  Walk anywhere with the joystick', '👀  Near a picture, tap Look', '📖  At a story stand, tap Read', '🎮  Play in the Go Bag and Fire rooms'].forEach((t, i) => c.fillText(t, 70, 352 + i * 76));
    c.fillStyle = '#ffc53d'; c.font = '900 27px Nunito'; c.fillText('The works shown are samples of how', 70, 676); c.fillText('the virtual museum is envisioned.', 70, 712);
  }));
  const wb = hang(lobby, 'N', -3.2, 1.75, plane(1.9, 1.425, new T.MeshBasicMaterial({ map: welcome })));
  hang(lobby, 'N', -3.2, 1.75, plane(2.0, 1.52, lam(0x0e2233)), 0.002);
  const mapTex = tex(canvas(1024, 768, (c, w, h) => {
    c.fillStyle = '#fffaf0'; roundRect(c, 0, 0, w, h, 48); c.fill();
    c.fillStyle = '#16303f'; c.font = '800 64px "Baloo 2"'; c.textAlign = 'center'; c.fillText('Museum map', w / 2, 82);
    c.textAlign = 'left'; drawMap(c, w, h, { top: 96, pad: 30 });
  }));
  const mb = hang(lobby, 'N', 3.2, 1.75, plane(1.9, 1.425, new T.MeshBasicMaterial({ map: mapTex })));
  hang(lobby, 'N', 3.2, 1.75, plane(2.0, 1.52, lam(0x16303f)), 0.002);
  [[-3.2, 'intro', wb, '📋'], [3.2, 'map', mb, '🗺️']].forEach(([along, type, mesh]) => {
    const p = wallPoint(lobby, 'N', along, PANEL);
    items.push({ id: type, type, spot: { x: p.x, z: p.z + 1.7 }, center: { x: p.x, y: 1.75, z: p.z + 0.3 }, targets: [mesh] });
  });
  // the way out
  const exitDoor = new T.Group();
  [-0.62, 0.62].forEach((x) => { box(1.2, 2.5, 0.08, 0x8a5a2b, x, 1.25, 0, exitDoor); box(0.08, 0.3, 0.1, 0xe2c27a, x * 0.2, 1.2, 0.06, exitDoor); });
  box(2.6, 0.14, 0.12, 0x5a3a1a, 0, 2.57, 0, exitDoor);
  hang(lobby, 'S', 0, 0, exitDoor, 0.04);
  const exitSign = hang(lobby, 'S', 0, 3.0, plane(1.5, 0.42, basic(tex(signCanvas('🚪 LABAS · EXIT', '#128253')))));
  items.push({ id: 'exit', type: 'exit', spot: { x: 0, z: lobby.z1 - 1.5 }, center: { x: 0, y: 1.3, z: lobby.z1 - 0.3 }, targets: [exitDoor.children[0], exitDoor.children[2], exitSign] });
  const logoRug = tex(canvas(512, 512, (c, w) => {
    c.fillStyle = '#16303f'; c.beginPath(); c.arc(w / 2, w / 2, w / 2, 0, Math.PI * 2); c.fill();
    c.fillStyle = '#ffc53d'; c.beginPath(); c.arc(w / 2, w / 2, w / 2 - 16, 0, Math.PI * 2); c.fill();
    c.fillStyle = '#16303f'; c.beginPath(); c.arc(w / 2, w / 2, w / 2 - 30, 0, Math.PI * 2); c.fill();
    drawLogo(c, 136, 136, 240);
  }));
  const rugL = add(new T.Mesh(new T.CircleGeometry(1.5, 48), new T.MeshLambertMaterial({ map: logoRug })), 0, 0.008, 3.9); rugL.rotation.x = -Math.PI / 2; rugL.rotation.z = Math.PI;

  // ---- furniture ----
  function bench(x, z, along) {
    const g = new T.Group(); g.position.set(x, 0, z); if (along) g.rotation.y = Math.PI / 2; scene.add(g);
    box(2.0, 0.1, 0.5, 0xb07a3c, 0, 0.45, 0, g); box(0.08, 0.42, 0.42, 0x3d3127, -0.85, 0.21, 0, g); box(0.08, 0.42, 0.42, 0x3d3127, 0.85, 0.21, 0, g);
    if (along) solid(x - 0.3, x + 0.3, z - 1.05, z + 1.05); else solid(x - 1.05, x + 1.05, z - 0.3, z + 0.3);
  }
  function plant(x, z) {
    box(0.4, 0.38, 0.4, 0xc2673a, x, 0.19, z);
    add(new T.Mesh(new T.SphereGeometry(0.36, 16, 12), lam(0x3f9a4a)), x, 0.72, z);
    add(new T.Mesh(new T.SphereGeometry(0.26, 16, 12), lam(0x55b85e)), x + 0.1, 0.98, z + 0.05);
    solid(x - 0.3, x + 0.3, z - 0.3, z + 0.3);
  }
  bench(-4.2, 6.6, true); bench(4.2, 6.6, true);
  [[-4.4, 0.6], [4.4, 0.6], [-4.4, 8.4], [4.4, 8.4]].forEach(([x, z]) => plant(x, z));
  // Bulwagan: a runner, a bench, a clay bahay kubo on a plinth, the exhibit board
  box(2.2, 0.01, 11.2, 0x2b4b5e, 0, 0.006, -6); box(0.12, 0.012, 11.2, 0xffc53d, -1.1, 0.007, -6); box(0.12, 0.012, 11.2, 0xffc53d, 1.1, 0.007, -6);
  bench(0, -6.0, false);
  box(0.7, 0.9, 0.7, 0xffffff, -4.25, 0.45, -9.65); solid(-4.65, -3.85, -10.05, -9.25);
  const kubo = new T.Group(); kubo.position.set(-4.25, 0.9, -9.65); kubo.rotation.y = 0.6; kubo.scale.setScalar(1.25); scene.add(kubo);
  [[-0.17, -0.12], [0.17, -0.12], [-0.17, 0.12], [0.17, 0.12]].forEach(([x, z]) => add(new T.Mesh(new T.CylinderGeometry(0.018, 0.018, 0.16, 6), lam(0x7a5218)), x, 0.08, z, kubo));
  box(0.42, 0.04, 0.32, 0xb98a3e, 0, 0.18, 0, kubo); box(0.36, 0.2, 0.26, 0xe3bf68, 0, 0.3, 0, kubo); box(0.08, 0.13, 0.01, 0x6b4a22, -0.06, 0.27, 0.131, kubo);
  const roof = add(new T.Mesh(new T.ConeGeometry(0.34, 0.3, 4), lam(0xc48a32)), 0, 0.55, 0, kubo); roof.rotation.y = Math.PI / 4; roof.scale.set(1, 1, 0.85);
  plant(4.3, -11.3); plant(-4.3, -11.3); plant(4.3, -0.8); plant(-4.3, -0.8);
  const exhibit = tex(canvas(1024, 680, (c, w, h) => {
    c.fillStyle = '#16303f'; roundRect(c, 0, 0, w, h, 40); c.fill();
    c.fillStyle = '#ffc53d'; c.font = '900 36px Nunito'; c.fillText('FEATURED EXHIBIT · SAMPLE', 64, 92);
    c.fillStyle = '#fff5df'; c.font = '800 104px "Baloo 2"'; c.fillText('Bayanihan', 60, 210);
    c.fillStyle = '#cfe0e8'; c.font = '800 38px Nunito';
    ['Bayanihan means neighbors helping', 'neighbors. These sample works show', 'how kids\' art and stories will', 'fill this hall.'].forEach((t, i) => c.fillText(t, 64, 300 + i * 62));
    c.fillStyle = '#ffc53d'; c.font = '900 34px Nunito'; c.fillText('Sama-sama, handa!', 64, 610);
  }));
  hang(room('bulwagan'), 'W', -2.35, 1.7, plane(1.9, 1.26, new T.MeshBasicMaterial({ map: exhibit })));
  // Kuwentuhan Corner: a bookshelf, its sign, a reading rug and beanbags
  const shelf = new T.Group(); shelf.position.set(0, 0, -19.65); scene.add(shelf);
  box(4.2, 2.4, 0.45, 0x9c6b3f, 0, 1.2, 0, shelf);
  let seed = 5; const rnd = () => (seed = (seed * 16807) % 2147483647) / 2147483647;
  const bookCols = [0xe2382b, 0x2a6b8f, 0x128253, 0xf2760c, 0x7a53c6, 0xffc53d, 0xf07aa6, 0x1f8f8f];
  for (let i = 0; i < 4; i++) {
    box(4.0, 0.5, 0.3, 0x6e4826, 0, 0.36 + i * 0.56, 0.1, shelf);
    let x = -1.95;
    while (x < 1.9) { const bw = 0.07 + rnd() * 0.07, bh = 0.32 + rnd() * 0.14; box(bw, bh, 0.26, bookCols[(rnd() * bookCols.length) | 0], x + bw / 2, 0.14 + i * 0.56 + bh / 2, 0.14, shelf); x += bw + 0.01; }
  }
  solid(-2.2, 2.2, -20, -19.3);
  hang(room('kuwentuhan'), 'N', 0, 3.1, plane(2.9, 0.63, basic(tex(signCanvas('📖 KUWENTUHAN', '#16303f', '#ffc53d')))));
  const rugK = add(new T.Mesh(new T.CircleGeometry(1.7, 40), lam(0xf2a33a)), 0, 0.008, -16.8); rugK.rotation.x = -Math.PI / 2;
  const rugK2 = add(new T.Mesh(new T.CircleGeometry(1.3, 40), lam(0xffc53d)), 0, 0.01, -16.8); rugK2.rotation.x = -Math.PI / 2;
  [[-1.3, -18.4, 0x7a53c6], [1.4, -18.5, 0x2a6b8f]].forEach(([x, z, col]) => {
    const bb = add(new T.Mesh(new T.SphereGeometry(0.5, 20, 14), lam(col)), x, 0.28, z); bb.scale.set(1, 0.62, 1);
    solid(x - 0.45, x + 0.45, z - 0.45, z + 0.45);
  });
  plant(4.3, -19.3);
  // the pillar rooms: plants in the far corners, a bench by a wall
  plant(-12.4, -8.4); plant(-5.6, -11.4); plant(12.4, -8.4); plant(5.6, -11.4);
  plant(-12.4, 8.4); plant(-5.6, 8.4); plant(12.4, 8.4); plant(5.6, 8.4);
  bench(-12.3, -2.2, true); bench(12.3, -2.2, true);

  // ---- the game rooms: posters here; the things you play with are added by src/rooms/gobag.js and fire.js ----
  const flicker = [];
  const fire = room('fireroom');
  const gb = room('gobagroom');
  hang(fire, 'W', -16, 1.75, plane(1.4, 1.75, new T.MeshBasicMaterial({ map: tex(posterCanvas('P · A · S · S', '#c62f22', [['P', 'Pull the pin'], ['A', 'Aim at the base of the fire'], ['S', 'Squeeze the handle'], ['S', 'Sweep side to side']])) })));
  hang(fire, 'E', -17.6, 1.75, plane(1.4, 1.75, new T.MeshBasicMaterial({ map: tex(posterCanvas('If the fire grows', '#e8700b', [['🏃', 'Get out fast'], ['🚪', 'Close the door behind you'], ['🙅', 'Stay out'], ['☎️', 'Call 911 for help']])) })));
  hang(gb, 'E', -16, 1.75, plane(1.4, 1.75, new T.MeshBasicMaterial({ map: tex(posterCanvas('Go Bag checklist', '#0c7a4a', [['💧', 'Water and food'], ['🔦', 'Flashlight and batteries'], ['🩹', 'First aid kit and medicine'], ['📻', 'Radio and a whistle'], ['📄', 'Copies of family papers']])) })));
  hang(gb, 'W', -17.6, 1.75, plane(1.4, 1.75, new T.MeshBasicMaterial({ map: tex(posterCanvas('Pack it before you need it', '#128253', [['🎒', 'One bag for each person'], ['📍', 'Keep it near the door'], ['🔁', 'Check it every few months'], ['👪', 'Pack it with your family']])) })));

  // the glowing spot in front of the work you are next to
  const ring = new T.Group();
  const ringMesh = new T.Mesh(new T.RingGeometry(0.42, 0.56, 48), new T.MeshBasicMaterial({ color: 0xffc53d, transparent: true, opacity: 0.95, depthWrite: false }));
  const disc = new T.Mesh(new T.CircleGeometry(0.42, 48), new T.MeshBasicMaterial({ color: 0xffe58a, transparent: true, opacity: 0.35, depthWrite: false }));
  ringMesh.rotation.x = disc.rotation.x = -Math.PI / 2; ring.add(ringMesh, disc); ring.position.y = 0.02; ring.visible = false; scene.add(ring);

  function dispose() {
    const seen = new Set();
    scene.traverse((o) => {
      if (o.geometry && !seen.has(o.geometry)) { seen.add(o.geometry); o.geometry.dispose(); }
      const ms = o.material ? (Array.isArray(o.material) ? o.material : [o.material]) : [];
      ms.forEach((m) => { if (!seen.has(m)) { seen.add(m); m.dispose(); } });
    });
    Object.values(bubbleMats).forEach((m) => m.dispose());
    glowMat.dispose();
    textures.forEach((t) => t.dispose());
  }

  // drawing helpers for the game rooms, so their furniture matches the museum
  const kit = { lam, box, add, plane, tex, basic, solid, hang, room, canvas, roundRect, fitText, signCanvas, posterCanvas, bubbleCanvas, glowTex };
  return { scene, colliders, items, ring, flicker, dispose, kit };
}
