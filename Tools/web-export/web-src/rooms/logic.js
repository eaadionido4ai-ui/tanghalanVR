// The rules of the two games, taken from the BAYANIHanda Game Library's engine (public/engine/games.js:
// ITEMS, Logic.gobag and Logic.fire in "BYANI-Handa Games, prototype v0.2"), so the items, their notes, the
// scoring and the stars are the same as in Go Bag Packing 3D and Fire Extinguisher 3D. Only the words for the
// controls changed, because here you play inside the museum.
// Lead developer of the original games: Edward Andrew A. Dionido, UP Resilience Institute, assisted by AI.

export const ITEMS = [
  { id: 'water', name: 'Water bottle', icon: '💧', good: true, weight: 3, info: 'Clean water keeps you going. Try to pack enough for three days.' },
  { id: 'food', name: 'Ready to eat food', icon: '🥫', good: true, weight: 2, info: 'Canned goods or crackers give you energy and do not spoil quickly.' },
  { id: 'flashlight', name: 'Flashlight', icon: '🔦', good: true, weight: 1, info: 'A flashlight helps you see when the power goes out.' },
  { id: 'batteries', name: 'Spare batteries', icon: '🔋', good: true, weight: 1, info: 'Extra batteries keep your flashlight and radio working.' },
  { id: 'firstaid', name: 'First aid kit', icon: '🩹', good: true, weight: 2, info: 'A first aid kit helps you clean and cover small cuts and scrapes.' },
  { id: 'whistle', name: 'Whistle', icon: '🎺', good: true, weight: 1, info: 'A whistle lets you call for help without shouting. Rescuers can hear it far away.' },
  { id: 'radio', name: 'Small radio', icon: '📻', good: true, weight: 2, info: 'A small radio brings you news and warnings even with no internet.' },
  { id: 'powerbank', name: 'Power bank', icon: '🔌', good: true, weight: 1, info: 'A power bank charges your phone so you can reach your family.' },
  { id: 'documents', name: 'Copies of IDs', icon: '📄', good: true, weight: 1, info: 'Copies of your IDs and papers, kept dry, help you prove who you are.' },
  { id: 'meds', name: 'Medicine', icon: '💊', good: true, weight: 1, info: 'Any medicine you take every day should come with you.' },
  { id: 'mask', name: 'Face mask', icon: '😷', good: true, weight: 1, info: 'A face mask protects you from dust, smoke, and germs.' },
  { id: 'cash', name: 'Some cash', icon: '💵', good: true, weight: 1, info: 'A little cash helps when card machines are down.' },
  { id: 'clothes', name: 'Dry clothes', icon: '👕', good: true, weight: 2, info: 'A dry change of clothes keeps you warm and comfortable.' },
  { id: 'toy', name: 'Small comfort toy', icon: '🧸', good: true, weight: 1, info: 'One small toy can help you feel calm and brave. Choose a little one.' },
  { id: 'contacts', name: 'Family phone list', icon: '📝', good: true, weight: 1, info: 'A written list of family numbers helps if your phone dies.' },
  { id: 'console', name: 'Game console', icon: '🎮', good: false, weight: 3, info: 'Fun, but it is heavy and needs power you may not have.' },
  { id: 'soda', name: 'Soda', icon: '🥤', good: false, weight: 2, info: 'Sweet drinks make you thirstier. Plain water is better.' },
  { id: 'icecream', name: 'Ice cream', icon: '🍦', good: false, weight: 2, info: 'It melts fast and makes a mess. Leave it in the freezer.' },
  { id: 'tv', name: 'Television', icon: '📺', good: false, weight: 3, info: 'Far too big and heavy to carry.' },
  { id: 'bricks', name: 'Bricks', icon: '🧱', good: false, weight: 3, info: 'Heavy and will only tire you out.' },
  { id: 'books', name: 'Stack of books', icon: '📚', good: false, weight: 3, info: 'A big stack of books is too heavy for a go bag.' },
  { id: 'hairdryer', name: 'Hair dryer', icon: '💨', good: false, weight: 2, info: 'It needs a wall plug and is not needed in an emergency.' },
  { id: 'balloon', name: 'Balloons', icon: '🎈', good: false, weight: 1, info: 'Fun, but they will not help you stay safe.' },
  { id: 'beachball', name: 'Beach ball', icon: '🏐', good: false, weight: 2, info: 'It takes up space you need for real supplies.' },
];
export const CAPACITY = 14; // how much the bag holds, in the items' weight units
const ESS = 9, DIS = 5;     // a round: 9 useful things and 5 that could stay home

export const clamp = (v, lo, hi) => (v < lo ? lo : v > hi ? hi : v);

export const gobag = {
  byId: (id) => ITEMS.find((x) => x.id === id) || null,
  shuffle(arr, rnd = Math.random) {
    const a = arr.slice();
    for (let i = a.length - 1; i > 0; i--) { const j = Math.floor(rnd() * (i + 1)); const t = a[i]; a[i] = a[j]; a[j] = t; }
    return a;
  },
  buildRound(essCount, disCount, rnd = Math.random) {
    const good = ITEMS.filter((x) => x.good), bad = ITEMS.filter((x) => !x.good);
    const ne = essCount == null ? ESS : Math.min(essCount, good.length);
    const nd = disCount == null ? DIS : Math.min(disCount, bad.length);
    return this.shuffle(this.shuffle(good, rnd).slice(0, ne).concat(this.shuffle(bad, rnd).slice(0, nd)), rnd);
  },
  bagWeight: (items) => items.reduce((w, it) => w + (it.weight || 0), 0),
  isOverCapacity(items, cap) { return this.bagWeight(items) > (cap || CAPACITY); },
  scoreRound(tray, packedIds) {
    let totalGood = 0, packedGood = 0, wrongPacks = 0, missedGood = 0;
    tray.forEach((it) => {
      const packed = packedIds.indexOf(it.id) !== -1;
      if (it.good) { totalGood++; if (packed) packedGood++; else missedGood++; } else if (packed) wrongPacks++;
    });
    return { totalGood, packedGood, missedGood, wrongPacks };
  },
  rateStars(r) {
    const ratio = r.totalGood > 0 ? r.packedGood / r.totalGood : 0;
    let s = ratio >= 0.9 ? 3 : ratio >= 0.6 ? 2 : 1;
    if (r.wrongPacks >= 3) s = Math.max(1, s - 1);
    return s;
  },
};

export const fire = {
  // how fast the fire drops, given the technique
  extinguishRate(s) {
    if (!s || !s.pinPulled) return 0;
    if (s.agentLeft != null && s.agentLeft <= 0) return 0;
    let rate = s.aimAtBase ? 1.0 : 0.25;
    rate *= s.sweeping ? 1.5 : 0.7;
    const d = typeof s.distance === 'number' ? s.distance : 3;
    let df = 1.0;
    if (d < 1.0) df = 0.4;        // too close
    else if (d > 5) df = 0.25;    // too far to reach
    else if (d > 4) df = 0.7;
    return rate * df;
  },
  // the fire grows back when it is not being fought well (teaches: do not stop early)
  growth(s) {
    if (!s) return 0;
    const fighting = s.pinPulled && s.spraying && s.aimAtBase && (s.agentLeft == null || s.agentLeft > 0);
    return fighting ? 0 : 2.2; // intensity per second
  },
  nextHint(s) {
    s = s || {};
    if (!s.pinPulled) return 'Pull the safety pin first.';
    if (s.agentLeft != null && s.agentLeft <= 0) return 'The extinguisher is empty.';
    if (s.distance != null && s.distance > 4.2) return 'Step a little closer.';
    if (s.distance != null && s.distance < 1.0) return 'Not too close. Step back a step.';
    if (!s.spraying) return 'Hold the Squeeze button.';
    if (!s.aimAtBase) return 'Aim low, at the base of the fire.';
    if (!s.sweeping) return 'Sweep from side to side.';
    return 'Great, keep sweeping the base!';
  },
  rateStars(r) {
    r = r || {};
    let s = 3;
    if (!r.easy && (r.timeSec || 0) > 22) s -= 1;
    if ((typeof r.aimAccuracy === 'number' ? r.aimAccuracy : 1) < 0.7) s -= 1;
    return Math.max(1, s);
  },
  // the original's numbers: the fire drops by rate x 8 a second; on Difficult the extinguisher empties at 6 a second
  DROP: 8,
  AGENT_USE: 6,
};
