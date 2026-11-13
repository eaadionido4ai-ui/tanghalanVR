// Runs in a headless browser (see ../export.mjs). Builds the museum, the street and the props with the web
// app's own three.js code, and hands the results to Node: glTF models, textures and data.
import * as THREE from 'three';
import { GLTFExporter } from 'three/examples/jsm/exporters/GLTFExporter.js';
import '@fontsource/baloo-2/latin-700.css';
import '@fontsource/baloo-2/latin-800.css';
import '@fontsource/nunito/latin-700.css';
import '@fontsource/nunito/latin-800.css';
import '@fontsource/nunito/latin-900.css';

window.THREE = THREE;
const log = (t) => { document.getElementById('log').textContent += '\n' + t; if (window.__log) window.__log(t); };

const toBase64 = (blob) => new Promise((res, rej) => { const r = new FileReader(); r.onload = () => res(String(r.result).split(',')[1]); r.onerror = rej; r.readAsDataURL(blob); });
async function save(path, blob) {
  const b64 = await toBase64(blob), CH = 4 * 1024 * 1024, n = Math.max(1, Math.ceil(b64.length / CH));
  for (let i = 0; i < n; i++) await window.__save(path, i, n, b64.slice(i * CH, (i + 1) * CH));
}
const toGLB = (objects) => new Promise((res, rej) => {
  const scene = new THREE.Scene();
  objects.forEach((o) => scene.add(o));
  try { new GLTFExporter().parse(scene, (buf) => res(new Blob([buf], { type: 'model/gltf-binary' })), { binary: true, embedImages: true, onlyVisible: false }); } catch (e) { rej(e); }
});
const toPNG = (img) => new Promise((res) => {
  let cv = img;
  if (img.tagName !== 'CANVAS') { cv = document.createElement('canvas'); cv.width = img.naturalWidth || img.width; cv.height = img.naturalHeight || img.height; cv.getContext('2d').drawImage(img, 0, 0); }
  cv.toBlob((b) => res(b), 'image/png');
});

async function run() {
  await Promise.all(['800 20px "Baloo 2"', '700 20px "Baloo 2"', '900 20px Nunito', '800 20px Nunito', '700 20px Nunito'].map((f) => document.fonts.load(f)));
  await document.fonts.ready;
  const { buildMuseum, KALYE } = await import('./museum.js');
  const { buildStreet, PLAN, WASTE_CLUSTERS } = await import('./street.js');
  const { buildProps, buildIcons, WASTE_MODELS } = await import('./props.js');
  const C = await import('../web-src/museum/content.js');
  const L = await import('../web-src/rooms/logic.js');

  log('building the museum');
  const museum = await buildMuseum(THREE);
  log('  ' + museum.meshes.length + ' pieces, ' + museum.extra.length + ' markers, ' + museum.textures.size + ' textures');
  log('building the street');
  const street = buildStreet(THREE, museum.W, KALYE.wall);
  log('  ' + street.meshes.length + ' pieces, ' + street.extra.length + ' markers, ' + street.textures.size + ' textures');
  const props = buildProps(THREE);
  const icons = buildIcons(museum.W);

  await save('glb/museum.glb', await toGLB(museum.meshes.concat(museum.extra)));
  await save('glb/street.glb', await toGLB(street.meshes.concat(street.extra)));
  for (const name of Object.keys(props)) await save('glb/' + name + '.glb', await toGLB(props[name].meshes.concat(props[name].extra)));
  log('models saved');

  // the museum's plan, for the people who walk in it (Tanghalan.Core, MuseumPlan): the boxes nobody walks
  // through (walls and furniture), the boxes the visitor points at (the works, the tables), and the sturdy
  // tables, with the way out from under each
  const r3 = (v) => Math.round(v * 1000) / 1000;
  const boxOf = (o) => {
    const q = o.geometry.parameters;
    return { x0: r3(o.position.x - q.width / 2), x1: r3(o.position.x + q.width / 2), y0: r3(o.position.y - q.height / 2), y1: r3(o.position.y + q.height / 2), z0: r3(o.position.z - q.depth / 2), z1: r3(o.position.z + q.depth / 2) };
  };
  const plan = { solids: [], hot: [], tables: [] };
  const anchors = new Map();
  museum.extra.forEach((o) => {
    const t = o.name.split('__');
    if (t[0] === 'A') anchors.set(o.name.slice(3), o.position);
    if (t[0] === 'COL') plan.solids.push(Object.assign({ id: t[1], set: t[2] ? t[2].slice(4) : '' }, boxOf(o)));
    if (t[0] === 'HOT') plan.hot.push(Object.assign({ id: t[1], set: t[2] ? t[2].slice(4) : '' }, boxOf(o)));
  });
  for (const [id, p] of anchors) {
    if (!id.startsWith('drill_') || id.endsWith('__N')) continue;
    const n = anchors.get(id + '__N');
    plan.tables.push({ id, x: r3(p.x), z: r3(p.z), nx: r3(n.x - p.x), nz: r3(n.z - p.z) });
  }
  log('  plan: ' + plan.solids.length + ' solid boxes, ' + plan.hot.length + ' boxes to point at, ' + plan.tables.length + ' sturdy tables');

  const textures = new Map([...museum.textures, ...street.textures, ...icons]);
  for (const [name, img] of textures) await save('textures/' + name + '.png', await toPNG(img));
  log(textures.size + ' textures saved');

  const data = {
    source: 'TANGHALAN web app, src/museum/content.js and src/rooms/logic.js',
    fullName: C.FULL_NAME, sampleNote: C.SAMPLE_NOTE, ceil: C.CEIL, spawn: C.SPAWN,
    rooms: museum.layout.rooms, kalye: { id: KALYE.id, name: KALYE.name, sub: KALYE.sub, hint: KALYE.hint },
    kinds: C.KINDS, games: C.GAMES, libraryGames: C.LIBRARY_GAMES, doors: C.DOORS,
    works: C.WORKS.map((w) => Object.assign({}, w, museum.layout.works.find((x) => x.id === w.id))),
    gobag: { items: L.ITEMS, capacity: L.CAPACITY, slots: museum.layout.gobag },
    fire: { drop: L.fire.DROP, agentUse: L.fire.AGENT_USE },
    street: { plan: PLAN, wasteClusters: WASTE_CLUSTERS, wasteModels: WASTE_MODELS },
    plan,
    textures: [...textures.keys()].sort(),
    models: ['museum', 'street'].concat(Object.keys(props)),
  };
  await save('data/content.json', new Blob([JSON.stringify(data, null, 1)], { type: 'application/json' }));
  log('done');
  window.__done && window.__done('ok');
}
run().catch((e) => { log('ERROR ' + (e && e.stack || e)); window.__done && window.__done('error: ' + (e && e.message || e)); });
