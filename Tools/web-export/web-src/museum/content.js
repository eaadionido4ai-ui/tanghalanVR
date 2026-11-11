// TANGHALAN content: the rooms, the doorways between them, and the works on show.
// Plain data (from the BAYANIHanda library's src/museum/content.js), with the pictures bundled by the build. World units are metres;
// north is -z. The works in this first exhibit are samples of how the virtual museum is
// envisioned: names, ages and places are made up, and the pictures come from
// design/tanghalan/art.js. Learners' own works, shared with consent, will replace them.

// What TANGHALAN stands for: each pair is the letters that spell the name, then the rest of the word.
export const NAME_PARTS = [['T', 'hree-dimensional '], ['An', 'imated '], ['G', 'allery '], ['H', 'ighlighting '], ['', 'Youth-Generated '], ['A', 'rtworks, '], ['L', 'earnings '], ['a', 'nd '], ['', 'Disaster '], ['N', 'arratives']];
export const FULL_NAME = NAME_PARTS.map((p) => p[0] + p[1]).join('');

export const SAMPLE_NOTE = 'The artworks and stories here are samples of how the virtual museum is envisioned. Learners\' own works, shared with their parents\' consent, will replace them.';

export const CEIL = 4.2;
export const DOOR_H = 3.0;

// A 3 x 3 grid of rooms; every room opens into its neighbors. In the two game rooms in the far corners
// you play inside the room itself: pack a go bag in the Go Bag Room (src/rooms/gobag.js) and put out a
// practice fire in the Fire Safety Room (src/rooms/fire.js).
export const ROOMS = [
  { id: 'lobby', name: 'Lobby', icon: '🚪', x0: -5, x1: 5, z0: 0, z1: 9, wall: 0xf3ead9, accent: '#2a4b5e',
    hint: 'Welcome to TANGHALAN! Walk through any door to explore. Walk up to a picture or a story stand and tap the button.' },
  { id: 'bulwagan', name: 'Bulwagan ng Bayanihan', icon: '⭐', x0: -5, x1: 5, z0: -12, z1: 0, wall: 0xf3ead9, accent: '#16303f',
    hint: 'This is the Bulwagan ng Bayanihan, the featured hall. Bayanihan means helping each other.' },
  { id: 'kuwentuhan', name: 'Kuwentuhan Corner', icon: '📖', x0: -5, x1: 5, z0: -20, z1: -12, wall: 0xffe7b0, accent: '#eaa100',
    hint: 'Kuwentuhan Corner! Stories, poems and comics live here. Walk up to a story stand and tap Read.' },
  { id: 'mit', pillar: 'mit', name: 'Stop Hazards Early', icon: '🛡️', sub: 'Prevention and Mitigation', x0: -13, x1: -5, z0: -12, z1: 0, wall: 0xdcebf3, accent: '#2a6b8f',
    hint: 'Stop Hazards Early: ways to stop dangers before they happen.' },
  { id: 'prep', pillar: 'prep', name: 'Get Ready', icon: '🎒', sub: 'Preparedness', x0: 5, x1: 13, z0: -12, z1: 0, wall: 0xdcf0e4, accent: '#128253',
    hint: 'Get Ready: how families prepare before a disaster.' },
  { id: 'resp', pillar: 'resp', name: 'Act Fast', icon: '🚨', sub: 'Response', x0: -13, x1: -5, z0: 0, z1: 9, wall: 0xfbe5d3, accent: '#e8700b',
    hint: 'Act Fast: what to do when a disaster strikes.' },
  { id: 'rec', pillar: 'rec', name: 'Bounce Back', icon: '🏘️', sub: 'Recovery and Rehabilitation', x0: 5, x1: 13, z0: 0, z1: 9, wall: 0xebe3f8, accent: '#7a53c6',
    hint: 'Bounce Back: how we help each other recover.' },
  { id: 'fireroom', game: 'fire', name: 'Fire Safety Room', icon: '🧯', sub: 'Game room · Put out a practice fire', x0: -13, x1: -5, z0: -20, z1: -12, wall: 0xfbe0d8, accent: '#c62f22',
    hint: 'Fire Safety Room! A small practice fire is burning in the tray. Press the alarm by the door, take the extinguisher, then use PASS.' },
  { id: 'gobagroom', game: 'gobag', name: 'Go Bag Room', icon: '🎒', sub: 'Game room · Pack a go bag', x0: 5, x1: 13, z0: -20, z1: -12, wall: 0xdff3e8, accent: '#0c7a4a',
    hint: 'Go Bag Room! Take an empty go bag from the packing table, then walk around and pack what your family needs.' },
];

// Doorways. axis 'x': in a wall running along x at z = at; axis 'z': in a wall running along z at x = at.
export const DOORS = [
  { a: 'lobby', b: 'bulwagan', axis: 'x', at: 0, from: -1.3, to: 1.3 },
  { a: 'bulwagan', b: 'kuwentuhan', axis: 'x', at: -12, from: -1.3, to: 1.3 },
  { a: 'bulwagan', b: 'mit', axis: 'z', at: -5, from: -7.3, to: -4.7 },
  { a: 'bulwagan', b: 'prep', axis: 'z', at: 5, from: -7.3, to: -4.7 },
  { a: 'mit', b: 'resp', axis: 'x', at: 0, from: -10.3, to: -7.7 },
  { a: 'prep', b: 'rec', axis: 'x', at: 0, from: 7.7, to: 10.3 },
  { a: 'lobby', b: 'resp', axis: 'z', at: -5, from: 3.2, to: 5.8 },
  { a: 'lobby', b: 'rec', axis: 'z', at: 5, from: 3.2, to: 5.8 },
  { a: 'kuwentuhan', b: 'fireroom', axis: 'z', at: -5, from: -14.9, to: -12.3 },
  { a: 'kuwentuhan', b: 'gobagroom', axis: 'z', at: 5, from: -14.9, to: -12.3 },
  { a: 'mit', b: 'fireroom', axis: 'x', at: -12, from: -12.6, to: -10.0 },
  { a: 'prep', b: 'gobagroom', axis: 'x', at: -12, from: 10.0, to: 12.6 },
];

// Rooms shown in 3D on the Home card, each a picture in src/assets/peek/. The two game rooms' pictures
// are made by tests/e2e/peeks.py; the others come from the BAYANIHanda Game Library's museum.
export const PEEK_ROOMS = ['kuwentuhan', 'prep', 'rec', 'gobagroom', 'resp', 'mit', 'fireroom'];

export const SPAWN = { x: 0, z: 4.8, yaw: Math.PI };

export const KINDS = {
  art: { icon: '🖍️', label: 'Drawing', action: 'Look' },
  story: { icon: '📖', label: 'Story', action: 'Read' },
  poem: { icon: '✏️', label: 'Poem', action: 'Read' },
  letter: { icon: '💌', label: 'Letter', action: 'Read' },
  comic: { icon: '💬', label: 'Comic', action: 'Read' },
};

// wall: [side N|S|E|W of the room, position along that wall, width in metres]
// stand: [x, z, yaw]: a story stand; readers stand on the side it faces
// about: what the artist says about a drawing. pages: the text of a story, poem, letter or comic.
// illus: the drawing shown with a story stand's text. note and game: Bayani's tip and the game it leads to.
export const WORKS = [
  // Bulwagan ng Bayanihan, the featured hall
  { id: 'bayanihan', kind: 'art', room: 'bulwagan', wall: ['E', -2.35, 2.0], featured: true,
    title: 'Bayanihan!', by: 'Ana', age: 10, grade: 'Grade 4', place: 'Iloilo', medium: 'Crayon on paper', pillar: 'mit', game: 'sapa',
    about: {
      fil: 'Malapit sa ilog ang dati naming bahay, kaya laging binabaha. Nagtulong-tulong ang mga kapitbahay na ilipat ito sa mataas na lugar. Ngayon, ligtas na kami.',
      en: 'Our old house was by the river, so it always flooded. Our neighbors helped move it to higher ground. Now we are safe.',
    },
    note: 'Homes away from riverbanks stay safer in floods.' },
  { id: 'kuwento', kind: 'story', room: 'bulwagan', wall: ['E', -9.65, 1.0],
    title: 'Ang Bahay sa Tabi ng Ilog', by: 'Ana', age: 10, grade: 'Grade 4', place: 'Iloilo', pillar: 'mit', game: 'sapa',
    pages: {
      fil: [['Tuwing may bagyo, umaapaw ang ilog sa tabi ng bahay namin.', 'Isang araw, nagpulong ang barangay.', '"Ilipat natin ang bahay sa mataas na lugar," sabi ni Kapitan.', 'Nagtulong-tulong ang lahat.', 'Ngayon, ligtas na kami kahit malakas ang ulan.']],
      en: [['Every typhoon, the river beside our house overflowed.', 'One day, the whole barangay held a meeting.', '"Let\'s move the house to higher ground," said the Kapitan.', 'Everyone helped.', 'Now we are safe, even when the rain is strong.']],
    },
    note: 'Bayanihan means helping each other. Moving away from danger keeps families safe.' },

  // Kuwentuhan Corner
  { id: 'tula', kind: 'poem', room: 'kuwentuhan', wall: ['W', -17.6, 1.0],
    title: 'Handa Kami', by: 'Paolo', age: 12, grade: 'Grade 6', place: 'Cebu', pillar: 'prep', game: 'quake',
    pages: {
      fil: [['Kapag dumilim ang langit,', 'hindi ako matatakot.', 'May flashlight sa aking bag,', 'may tubig, pito, at gamot.'], ['Kapag lumindol, yuyuko,', 'tatakpan, kakapit nang mahigpit.', 'Sama-sama kaming lalabas', 'kapag tumigil na ang yanig.']],
      en: [['When the sky turns dark,', 'I will not be afraid.', 'There is a flashlight in my bag,', 'with water, a whistle and medicine.'], ['When the ground shakes, I will drop,', 'cover, and hold on tight.', 'Together we will walk out', 'once the shaking stops.']],
    },
    note: 'Drop, cover, and hold on. Walk out only after the shaking stops.' },
  { id: 'komiks', kind: 'comic', room: 'kuwentuhan', wall: ['E', -17.6, 1.6],
    title: 'Si Bayani at ang Bagyo', by: 'Bea', age: 11, grade: 'Grade 5', place: 'Bulacan', pillar: 'prep', game: 'gobag',
    pages: {
      fil: [['"May bagyo!" sigaw ni Bayani.', 'Inihanda niya ang go bag ng pamilya.', 'Sama-sama silang lumikas sa evacuation center.', '"Ligtas kami!"']],
      en: [['"A typhoon is coming!" Bayani shouts.', 'He gets the family\'s go bag ready.', 'Together they go to the evacuation center.', '"We are safe!"']],
    },
    note: 'When a typhoon is coming, grab your go bag and follow your family\'s plan.' },
  { id: 'baha-kuwento', kind: 'story', room: 'kuwentuhan', stand: [-2.2, -14.4, 0], illus: 'komiks',
    title: 'Si Bayani at ang Baha', by: 'Bea', age: 11, grade: 'Grade 5', place: 'Bulacan', pillar: 'resp', game: 'path',
    pages: {
      fil: [['Umuulan nang malakas buong gabi.', 'Tumataas ang tubig sa ilog.'], ['"Lumikas na tayo!" sabi ni Bayani.', 'Dala nila ang go bag at flashlight.'], ['Naglakad sila sa ligtas na daan papunta sa evacuation center.', 'Ligtas ang lahat, at may mainit na sopas pa!']],
      en: [['It rained hard all night.', 'The water in the river kept rising.'], ['"Let\'s evacuate now!" said Bayani.', 'They brought the go bag and a flashlight.'], ['They walked the safe route to the evacuation center.', 'Everyone was safe, and there was even warm soup!']],
    },
    note: 'Leave early, before the water gets high, and take the safe route.' },
  { id: 'drill', kind: 'story', room: 'kuwentuhan', stand: [2.2, -14.4, 0], illus: 'dch',
    title: 'Ang Unang Drill Ko', by: 'Lia', age: 8, grade: 'Grade 2', place: 'Davao del Sur', pillar: 'resp', game: 'quake',
    pages: {
      fil: [['Tumunog ang alarma sa aming silid-aralan.', '"Drill ito," sabi ni Teacher. "Gawin natin nang tama!"'], ['Yumuko ako, nagtago sa ilalim ng mesa, at kumapit nang mahigpit.', 'Naghintay ako hanggang sabihin ni Teacher na tapos na ang yanig.'], ['Pagkatapos, nakapila kaming lumabas papunta sa palaruan.', 'Hindi ako natakot, dahil alam ko na ang gagawin!']],
      en: [['The alarm rang in our classroom.', '"This is a drill," said Teacher. "Let\'s do it right!"'], ['I dropped, hid under my desk, and held on tight.', 'I waited until Teacher said the shaking was over.'], ['Then we walked out in a line to the playground.', 'I was not scared, because I knew what to do!']],
    },
    note: 'Drop, cover, and hold on. Walk out only when the shaking stops.' },

  // Stop Hazards Early
  { id: 'tanim', kind: 'art', room: 'mit', wall: ['N', -7.6, 1.6],
    title: 'Magtanim ng Puno', by: 'Mara', age: 9, grade: 'Grade 3', place: 'Albay', medium: 'Crayon on paper', pillar: 'mit', game: 'cares',
    about: {
      fil: 'Nagtanim kami ng mga puno sa burol malapit sa paaralan. Hinahawakan ng mga ugat ang lupa para hindi ito gumuho kapag umuulan.',
      en: 'We planted trees on the hill near our school. Their roots hold the soil so it does not slide when it rains.',
    },
    note: 'Trees soak up rain and hold the soil, so floods and landslides are less likely.' },
  { id: 'kanal', kind: 'art', room: 'mit', wall: ['W', -6, 1.6],
    title: 'Linisin ang Kanal!', by: 'Rico', age: 10, grade: 'Grade 4', place: 'Rizal', medium: 'Crayon on paper', pillar: 'mit', game: 'sapa',
    about: {
      fil: 'Ito ang araw na naglinis kami ng kanal. Kapag malinis ang kanal, hindi binabaha ang aming kalye.',
      en: 'This is the day we cleaned the canal. When the canal is clean, our street does not flood.',
    },
    note: 'Trash blocks canals and creeks. Keep them clean so rainwater can flow away.' },
  { id: 'kanal-kuwento', kind: 'story', room: 'mit', stand: [-9.5, -5.2, Math.PI / 2], illus: 'kanal',
    title: 'Ang Kanal sa Aming Kalye', by: 'Rico', age: 10, grade: 'Grade 4', place: 'Rizal', pillar: 'mit', game: 'sapa',
    pages: {
      fil: [['Puno ng plastik ang kanal sa aming kalye.', 'Kapag umuulan, umaapaw ang tubig sa daan.'], ['Isang Sabado, naglinis kami kasama ang mga kapitbahay.', 'May guwantes kami at malalaking sako.'], ['Ngayon, malinis na ang kanal at mabilis dumaloy ang tubig.', 'Hindi na kami nagtatapon ng basura kahit saan.']],
      en: [['The canal on our street was full of plastic.', 'Whenever it rained, water spilled onto the road.'], ['One Saturday, we cleaned it with our neighbors.', 'We wore gloves and carried big sacks.'], ['Now the canal is clean and the water flows fast.', 'We do not throw trash just anywhere anymore.']],
    },
    note: 'Clean canals and creeks carry rain away, so streets flood less.' },

  // Get Ready
  { id: 'gobag', kind: 'art', room: 'prep', wall: ['N', 6.6, 1.05],
    title: 'Ang Go Bag Ko', by: 'Jun', age: 8, grade: 'Grade 3', place: 'Pampanga', medium: 'Crayon on paper', pillar: 'prep', game: 'gobag',
    about: {
      fil: 'Iginuhit ko ang laman ng go bag namin: flashlight, tubig, pito, gamot, pagkain, at radyo.',
      en: 'I drew what is in our go bag: a flashlight, water, a whistle, medicine, food and a radio.',
    },
    note: 'Pack a go bag before a disaster, and keep it where everyone can grab it.' },
  { id: 'liham', kind: 'letter', room: 'prep', wall: ['N', 8.6, 1.0],
    title: 'Liham kay Bayani', by: 'Jun', age: 8, grade: 'Grade 3', place: 'Pampanga', pillar: 'prep', game: 'gobag',
    pages: {
      fil: [['Mahal kong Bayani,', 'Salamat sa mga paalala mo.', 'Inayos namin ang go bag ng buong pamilya: may tubig, flashlight, pito, at gamot na.', 'Alam na rin namin ang daan papunta sa evacuation center.', 'Handa na kami!', 'Nagmamahal, Jun']],
      en: [['Dear Bayani,', 'Thank you for your reminders.', 'We packed our whole family\'s go bag: it has water, a flashlight, a whistle and medicine.', 'We also know the way to the evacuation center.', 'We are ready!', 'Love, Jun']],
    },
    note: 'Check your family\'s go bag together, and know the way to your evacuation center.' },
  { id: 'plano', kind: 'art', room: 'prep', wall: ['E', -6, 1.6],
    title: 'Plano ng Pamilya', by: 'Lea', age: 11, grade: 'Grade 5', place: 'Leyte', medium: 'Crayon on paper', pillar: 'prep', game: 'path',
    about: {
      fil: 'Ito ang plano ng aming pamilya. Kapag may sakuna, magkikita kami sa malaking puno, tapos sabay kaming pupunta sa paaralan.',
      en: 'This is our family\'s plan. If disaster strikes, we will meet at the big tree, then go to the school together.',
    },
    note: 'Agree on a meeting place and a safe route, so your family can find each other.' },

  // Act Fast
  { id: 'dch', kind: 'art', room: 'resp', wall: ['S', -11, 1.05],
    title: 'Duck, Cover, Hold', by: 'Lia', age: 8, grade: 'Grade 2', place: 'Davao del Sur', medium: 'Crayon on paper', pillar: 'resp', game: 'quake',
    about: {
      fil: 'Sa drill, nagtago ako sa ilalim ng mesa at kumapit sa paa nito. Yuko, takip, kapit!',
      en: 'In the drill, I hid under the table and held on to its leg. Drop, cover, hold!',
    },
    note: 'Under a sturdy table, hold on to its leg so it stays over you.' },
  { id: 'sunog', kind: 'art', room: 'resp', wall: ['S', -7, 1.05],
    title: 'Kapag May Sunog', by: 'Nico', age: 10, grade: 'Grade 4', place: 'Laguna', medium: 'Crayon on paper', pillar: 'resp', game: 'fire',
    about: {
      fil: 'Kapag may usok, gumapang nang mababa papunta sa labasan. Mas malinis ang hangin sa ibaba.',
      en: 'If there is smoke, crawl low to the exit. The air near the floor is cleaner.',
    },
    note: 'Crawl low under smoke, get out, and call for help.' },
  { id: 'baha', kind: 'art', room: 'resp', wall: ['W', 4.5, 1.6],
    title: 'Lumikas Agad!', by: 'Carlo', age: 11, grade: 'Grade 5', place: 'Camarines Sur', medium: 'Crayon on paper', pillar: 'resp', game: 'path',
    about: {
      fil: 'Iginuhit ko ang mga rescuer na tumulong sa amin. Mas mabuting lumikas nang maaga, bago tumaas ang tubig.',
      en: 'I drew the rescuers who helped us. It is better to evacuate early, before the water rises.',
    },
    note: 'When the barangay says evacuate, go early and use the safe route.' },
  { id: 'pito', kind: 'story', room: 'resp', stand: [-9.2, 4.6, Math.PI / 2], illus: 'baha',
    title: 'Ang Pito ni Lolo', by: 'Carlo', age: 11, grade: 'Grade 5', place: 'Camarines Sur', pillar: 'resp', game: 'gobag',
    pages: {
      fil: [['Binigyan ako ni Lolo ng isang pito.', '"Ilagay mo ito sa go bag mo," sabi niya.'], ['"Kapag naipit ka o naligaw, pumito ka nang malakas.', 'Mas malayo ang naaabot ng tunog ng pito kaysa sa sigaw."'], ['Sa drill sa paaralan, ipinakita ko ito sa aking mga kaklase.', 'Ngayon, may pito na rin sila sa kanilang go bag!']],
      en: [['Lolo gave me a whistle.', '"Put this in your go bag," he said.'], ['"If you are ever stuck or lost, blow it hard.', 'A whistle carries farther than a shout."'], ['At our school drill, I showed it to my classmates.', 'Now they have whistles in their go bags too!']],
    },
    note: 'A whistle helps rescuers find you. Pack one in your go bag.' },

  // Bounce Back
  { id: 'bahaghari', kind: 'art', room: 'rec', wall: ['S', 9, 1.6],
    title: 'Babangon Tayo!', by: 'Paolo', age: 12, grade: 'Grade 6', place: 'Cebu', medium: 'Crayon on paper', pillar: 'rec', game: 'cleanup',
    about: {
      fil: 'Pagkatapos ng bagyo, may bahaghari. Naglinis kami nang sama-sama, at babangon kami.',
      en: 'After the storm came a rainbow. We cleaned up together, and we will rise again.',
    },
    note: 'After a storm, clean up safely: boots, gloves, and no touching wires.' },
  { id: 'tulong', kind: 'art', room: 'rec', wall: ['E', 4.5, 1.6],
    title: 'Tulong-Tulong Tayo!', by: 'Joy', age: 9, grade: 'Grade 3', place: 'Eastern Samar', medium: 'Crayon on paper', pillar: 'rec', game: 'cares',
    about: {
      fil: 'Sa barangay hall, nagbahagi kami ng relief goods sa mga kapitbahay na nasalanta ng bagyo.',
      en: 'At the barangay hall, we shared relief goods with neighbors hit by the typhoon.',
    },
    note: 'Communities recover faster when neighbors help each other.' },
  { id: 'bagyo-kuwento', kind: 'story', room: 'rec', stand: [9.2, 4.6, -Math.PI / 2], illus: 'tulong',
    title: 'Pagkatapos ng Bagyo', by: 'Joy', age: 9, grade: 'Grade 3', place: 'Eastern Samar', pillar: 'rec', game: 'cleanup',
    pages: {
      fil: [['Paglipas ng bagyo, maputik ang aming bahay.', 'Maraming nabuwal na sanga sa bakuran.'], ['Sabi ni Nanay, huwag hawakan ang mga nakalaylay na kable.', 'Nagsuot kami ng bota at guwantes bago maglinis.'], ['Tumulong din ang mga kapitbahay, at nagbahagi kami ng pagkain.', 'Unti-unti, bumangon muli ang aming barangay.']],
      en: [['After the typhoon, our house was full of mud.', 'Lots of fallen branches lay in the yard.'], ['Nanay said not to touch any hanging wires.', 'We put on boots and gloves before cleaning up.'], ['Our neighbors helped too, and we shared food.', 'Little by little, our barangay rose again.']],
    },
    note: 'Stay away from fallen wires, and wear boots and gloves when you clean up.' },
];

export const room = (id) => ROOMS.find((r) => r.id === id) || ROOMS[0];
export const work = (id) => WORKS.find((w) => w.id === id) || null;
export const roomAt = (x, z) => ROOMS.find((r) => x >= r.x0 && x <= r.x1 && z >= r.z0 && z <= r.z1) || null;

// A point on a room's inner wall, facing into the room.
export function wallPoint(r, side, along, inset = 0.1) {
  if (side === 'N') return { x: along, z: r.z0 + inset, yaw: 0 };
  if (side === 'S') return { x: along, z: r.z1 - inset, yaw: Math.PI };
  if (side === 'W') return { x: r.x0 + inset, z: along, yaw: Math.PI / 2 };
  return { x: r.x1 - inset, z: along, yaw: -Math.PI / 2 };
}

// Where a reader stands to look at a work.
export function spotOf(w) {
  if (w.stand) { const [x, z, yaw] = w.stand; return { x: x + Math.sin(yaw) * 0.95, z: z + Math.cos(yaw) * 0.95 }; }
  const p = wallPoint(room(w.room), w.wall[0], w.wall[1]);
  return { x: p.x + Math.sin(p.yaw) * 1.6, z: p.z + Math.cos(p.yaw) * 1.6 };
}

// The picture file for a work: the drawing or written page itself, or a story stand's book.
const PICS = import.meta.glob('../assets/works/*.webp', { eager: true, import: 'default' });
const PEEKS = import.meta.glob('../assets/peek/*.webp', { eager: true, import: 'default' });
export const pic = (name) => PICS['../assets/works/' + name + '.webp'];
export const peekOf = (roomId) => PEEKS['../assets/peek/' + roomId + '.webp'];
export const imageOf = (w) => pic(w.stand ? 'book-' + w.id : w.id);
export const picOf = (w) => pic(w.stand ? w.illus : w.id);

// The two games you play inside the museum, and what the works' tips can lead to. Games that live only in the
// BAYANIHanda Game Library are named in a work's tip without a Play button.
export const GAMES = {
  gobag: { id: 'gobag', room: 'gobagroom', name: 'Go Bag Challenge', icon: '🎒', badge: ['Go Bag', '🎒'],
    modes: [['🧺', 'Easy', 'On the tables', 'Everything is laid out on three tables. Good for a first try.'],
      ['🔎', 'Difficult', 'Search the room', 'The supplies are hidden in a cabinet, drawers, a crate and a cooler. Open them to find what you need.']],
    tip: 'Your bag has limited room, so choose the supplies that matter most.',
    remind: 'Keep your family go bag near the door, and check it with a grown-up every few months.' },
  fire: { id: 'fire', room: 'fireroom', name: 'Fire Practice', icon: '🧯', badge: ['Fire Out', '🧯'],
    modes: [['🙂', 'Easy', 'Easy', 'Take your time, and you will never run out of spray. A circle on the floor shows where to stand.'],
      ['⏱️', 'Difficult', 'Difficult', 'Watch your extinguisher, beat the clock, and find a safe spot on your own.']],
    tip: 'Stand a safe distance away, about two big steps back, with a clear way out behind you.',
    remind: 'Only fight a small fire, and only with a clear way out behind you. If the fire is big, spreading, or the room fills with smoke, get out fast, close the door, pull the fire alarm, and call for help.' },
};
// Games named in the works' tips that are not in this app (they are in the BAYANIHanda Game Library).
export const LIBRARY_GAMES = { sapa: ['Guide the Stream', '🏞️'], quake: ['Quake in Order', '🏗️'], path: ['Safe Path', '🧭'], cleanup: ['House Cleanup', '🧹'], cares: ['CARES: Community and Resilience', '🏘️'] };
