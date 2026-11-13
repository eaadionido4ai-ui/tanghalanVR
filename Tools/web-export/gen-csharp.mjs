// Writes the C# data files of TANGHALAN VR from what the web app exported (out/data/content.json):
//   Unity/Assets/TANGHALAN/Scripts/Core/Generated/MuseumData.g.cs   the rooms and the works, with their texts
//   Unity/Assets/TANGHALAN/Scripts/Core/Generated/GoBagItems.g.cs   the 24 go bag items
//   Unity/Assets/TANGHALAN/Scripts/Core/Generated/MuseumPlan.g.cs   the walls, the furniture and the sturdy
//                                                                   tables, for the people who walk in it
// Run after export.mjs (Tools/build_assets.sh does both).
//
// The web app has the works' texts in Filipino and English, and everything else in English. The Filipino
// below (item names and notes, Bayani's tips) was written for TANGHALAN VR and should be read over by a
// Filipino teacher before children use it.
import { readFile, writeFile, mkdir } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const data = JSON.parse(await readFile(path.join(here, 'out', 'data', 'content.json'), 'utf8'));
const outDir = path.join(here, '..', '..', 'Unity', 'Assets', 'TANGHALAN', 'Scripts', 'Core', 'Generated');
await mkdir(outDir, { recursive: true });

const ITEMS_FIL = {
  water: ['Bote ng tubig', 'Kailangan mo ng malinis na tubig. Maghanda ng sapat para sa tatlong araw.'],
  food: ['Pagkaing handa nang kainin', 'Nagbibigay ng lakas ang de-lata o biskwit, at hindi agad napapanis.'],
  flashlight: ['Flashlight', 'Tutulungan ka ng flashlight na makakita kapag nawalan ng kuryente.'],
  batteries: ['Ekstrang baterya', 'Pinapanatiling gumagana ng ekstrang baterya ang flashlight at radyo mo.'],
  firstaid: ['First aid kit', 'Panlinis at pantakip sa maliliit na sugat at gasgas ang first aid kit.'],
  whistle: ['Pito', 'Gamit ang pito, makakahingi ka ng tulong nang hindi sumisigaw. Naririnig ito ng mga rescuer kahit malayo.'],
  radio: ['Maliit na radyo', 'Naghahatid ang maliit na radyo ng balita at babala kahit walang internet.'],
  powerbank: ['Power bank', 'Pang-charge ng telepono ang power bank, para matawagan mo ang pamilya mo.'],
  documents: ['Kopya ng mga ID', 'Ang mga kopya ng ID at papeles mo, na nakatagong tuyo, ay patunay kung sino ka.'],
  meds: ['Gamot', 'Dalhin ang anumang gamot na iniinom mo araw-araw.'],
  mask: ['Face mask', 'Panangga ang face mask sa alikabok, usok, at mikrobyo.'],
  cash: ['Kaunting pera', 'Makakatulong ang kaunting pera kapag hindi gumagana ang mga card machine.'],
  clothes: ['Tuyong damit', 'Ang tuyong pamalit na damit ay magpapanatili sa iyong mainit at komportable.'],
  toy: ['Maliit na laruan', 'Makakatulong ang isang maliit na laruan para kumalma ka at lumakas ang loob mo. Pumili ng maliit lang.'],
  contacts: ['Listahan ng numero ng pamilya', 'Makakatulong ang nakasulat na listahan ng mga numero ng pamilya kapag namatay ang telepono mo.'],
  console: ['Game console', 'Masaya ito, pero mabigat at kailangan ng kuryenteng baka wala ka.'],
  soda: ['Soda', 'Lalo kang mauuhaw sa matatamis na inumin. Mas mabuti ang tubig.'],
  icecream: ['Ice cream', 'Mabilis itong matunaw at makalat. Iwan na lang sa freezer.'],
  tv: ['Telebisyon', 'Masyadong malaki at mabigat para buhatin.'],
  bricks: ['Mga ladrilyo', 'Mabigat ito at papagurin ka lang.'],
  books: ['Patong-patong na libro', 'Masyadong mabigat ang maraming libro para sa go bag.'],
  hairdryer: ['Hair dryer', 'Kailangan nito ng saksakan, at hindi ito kailangan sa oras ng sakuna.'],
  balloon: ['Mga lobo', 'Masaya ito, pero hindi ka nito matutulungang maging ligtas.'],
  beachball: ['Beach ball', 'Kumakain ito ng espasyong kailangan mo para sa totoong gamit.'],
};
const NOTES_FIL = {
  bayanihan: 'Mas ligtas sa baha ang mga bahay na malayo sa pampang ng ilog.',
  kuwento: 'Ang bayanihan ay pagtutulungan. Ligtas ang pamilya kapag lumayo sa panganib.',
  tula: 'Yuko, takip, kapit. Lumabas lang kapag tumigil na ang yanig.',
  komiks: 'Kapag may paparating na bagyo, kunin ang go bag at sundin ang plano ng pamilya.',
  'baha-kuwento': 'Lumikas nang maaga, bago tumaas ang tubig, at dumaan sa ligtas na daan.',
  drill: 'Yuko, takip, kapit. Lumabas lang kapag tumigil na ang yanig.',
  tanim: 'Sinisipsip ng mga puno ang ulan at hinahawakan ang lupa, kaya nababawasan ang baha at pagguho.',
  kanal: 'Bumabara ang basura sa kanal at sapa. Panatilihing malinis ang mga ito para makadaloy ang tubig-ulan.',
  'kanal-kuwento': 'Inilalayo ng malinis na kanal at sapa ang tubig-ulan, kaya hindi gaanong binabaha ang kalye.',
  gobag: 'Ihanda ang go bag bago pa dumating ang sakuna, at ilagay kung saan madaling makuha ng lahat.',
  liham: 'Sabay-sabay na suriin ang go bag ng pamilya, at alamin ang daan papunta sa evacuation center.',
  plano: 'Magkasundo sa tagpuan at sa ligtas na daan, para magkita-kita ang pamilya.',
  dch: 'Sa ilalim ng matibay na mesa, kumapit sa paa nito para manatili ito sa ibabaw mo.',
  sunog: 'Gumapang nang mababa sa ilalim ng usok, lumabas, at humingi ng tulong.',
  baha: 'Kapag sinabi ng barangay na lumikas, umalis nang maaga at dumaan sa ligtas na daan.',
  pito: 'Tinutulungan ng pito ang mga rescuer na mahanap ka. Maglagay nito sa go bag mo.',
  bahaghari: 'Pagkatapos ng bagyo, maglinis nang ligtas: bota, guwantes, at huwag hahawak ng kable.',
  tulong: 'Mas mabilis makabangon ang komunidad kapag nagtutulungan ang magkakapitbahay.',
  'bagyo-kuwento': 'Lumayo sa mga nabuwal na kable, at magsuot ng bota at guwantes kapag naglilinis.',
};

const s = (v) => v == null ? 'null' : '"' + String(v).replace(/\\/g, '\\\\').replace(/"/g, '\\"').replace(/\n/g, '\\n') + '"';
const n = (v) => Number.isInteger(v) ? String(v) : String(v);
const head = (what) => `// <auto-generated>\n// ${what}\n// Written by Tools/web-export/gen-csharp.mjs from the TANGHALAN web app's content. Do not edit by hand:\n// change the source and run Tools/build_assets.sh.\n// </auto-generated>\n`;
const kind = { art: 'Art', story: 'Story', poem: 'Poem', letter: 'Letter', comic: 'Comic' };
const pages = (p) => p ? 'new[] { ' + p.map((pg) => 'new[] { ' + pg.map(s).join(', ') + ' }').join(', ') + ' }' : 'null';

let m = head('The rooms and the works of the museum.') + 'namespace Tanghalan\n{\n    public static partial class MuseumData\n    {\n';
m += `        public const string FullName = ${s(data.fullName)};\n        public const string SampleNote = ${s(data.sampleNote)};\n        public const double Ceiling = ${n(data.ceil)};\n        public const double SpawnX = ${n(data.spawn.x)}, SpawnZ = ${n(data.spawn.z)};\n\n`;
m += '        public static readonly Room[] Rooms =\n        {\n';
for (const r of data.rooms) m += `            new Room(${s(r.id)}, ${s(r.name)}, ${s(r.sub)}, ${n(r.x0)}, ${n(r.x1)}, ${n(r.z0)}, ${n(r.z1)}, ${s(r.pillar)}, ${s(r.game)}),\n`;
m += '        };\n\n        public static readonly Door[] Doors =\n        {\n';
for (const d of data.doors) m += `            new Door(${s(d.a)}, ${s(d.b)}, '${d.axis}', ${n(d.at)}, ${n(d.from)}, ${n(d.to)}),\n`;
m += '        };\n\n        public static readonly Work[] Works =\n        {\n';
for (const w of data.works) {
  if (!NOTES_FIL[w.id]) throw new Error('no Filipino note for ' + w.id);
  m += '            new Work\n            {\n';
  m += `                Id = ${s(w.id)}, Kind = WorkKind.${kind[w.kind]}, Room = ${s(w.room)}, Stand = ${w.stand ? 'true' : 'false'}, Featured = ${w.featured ? 'true' : 'false'},\n`;
  m += `                Title = ${s(w.title)}, By = ${s(w.by)}, Age = ${w.age}, Grade = ${s(w.grade)}, Place = ${s(w.place)}, Medium = ${s(w.medium || null)},\n`;
  m += `                Pillar = ${s(w.pillar)}, Game = ${s(w.game)}, Hot = ${s(w.hot)}, Picture = ${s(w.picture)},\n`;
  if (w.about) m += `                AboutFil = ${s(w.about.fil)},\n                AboutEn = ${s(w.about.en)},\n`;
  if (w.pages) m += `                PagesFil = ${pages(w.pages.fil)},\n                PagesEn = ${pages(w.pages.en)},\n`;
  m += `                NoteEn = ${s(w.note)},\n                NoteFil = ${s(NOTES_FIL[w.id])},\n            },\n`;
}
m += '        };\n    }\n}\n';
await writeFile(path.join(outDir, 'MuseumData.g.cs'), m);

let g = head('The 24 go bag items: their names, their notes, whether they belong in a go bag, and their weights.') + 'namespace Tanghalan\n{\n    public static class GoBagItems\n    {\n        public static readonly GoBagItem[] All =\n        {\n';
for (const it of data.gobag.items) {
  const f = ITEMS_FIL[it.id]; if (!f) throw new Error('no Filipino text for ' + it.id);
  g += `            new GoBagItem(${s(it.id)}, ${s(it.name)}, ${s(f[0])}, ${it.good ? 'true' : 'false'}, ${it.weight},\n                ${s(it.info)},\n                ${s(f[1])}),\n`;
}
g += '        };\n    }\n}\n';
if (data.gobag.capacity !== 14) throw new Error('the bag capacity changed in the web app: update GoBagRules.Capacity');
await writeFile(path.join(outDir, 'GoBagItems.g.cs'), g);

// the plan: boxes are x0, x1, y0, y1, z0, z1 in the web app's coordinates; set is the switchable set a box
// belongs to ("" for none)
const box = (b) => `new PlanBox(${s(b.id)}, ${s(b.set)}, ${[b.x0, b.x1, b.y0, b.y1, b.z0, b.z1].map(n).join(', ')})`;
const list = (type, name, rows) => `        public static readonly ${type}[] ${name} =\n        {\n` + rows.map((r) => '            ' + r + ',\n').join('') + '        };\n';
let pl = head("The museum's plan: the boxes nobody walks through, the boxes the visitor points at, and the sturdy tables.") + 'namespace Tanghalan\n{\n    public static partial class MuseumPlan\n    {\n';
pl += list('PlanBox', 'Solids', data.plan.solids.filter((b) => b.id !== 'ceiling').map(box)) + '\n';
pl += list('PlanBox', 'Hot', data.plan.hot.map(box)) + '\n';
pl += list('PlanTable', 'Tables', data.plan.tables.map((t) => `new PlanTable(${s(t.id)}, ${n(t.x)}, ${n(t.z)}, ${n(t.nx)}, ${n(t.nz)})`));
pl += '    }\n}\n';
await writeFile(path.join(outDir, 'MuseumPlan.g.cs'), pl);
console.log('C# data written: ' + data.rooms.length + ' rooms, ' + data.works.length + ' works, ' + data.gobag.items.length + ' items, ' + data.plan.solids.length + ' solid boxes, ' + data.plan.tables.length + ' tables');
