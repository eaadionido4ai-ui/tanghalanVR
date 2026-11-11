// Low-poly children for TANGHALAN, in the games' style: the player and Bayani.
// Each faces +z; legs and arms hang from pivots so they can swing while walking.
// The player can carry a go bag on the back (it fills up as things are packed) and hold something in
// front with both hands (the fire extinguisher), and reaches out when picking something up.

export function makeKid(T, o) {
  const g = new T.Group();
  const body = new T.Group();
  g.add(body);
  const skin = o.skin || 0xd69a63;
  const mat = (color, extra) => new T.MeshLambertMaterial(Object.assign({ color }, extra || {}));
  const add = (parent, geo, color, x, y, z, extra) => { const m = new T.Mesh(geo, mat(color, extra)); m.position.set(x || 0, y || 0, z || 0); parent.add(m); return m; };

  const legs = [-1, 1].map((s) => {
    const hip = new T.Group(); hip.position.set(0.09 * s, 0.48, 0); body.add(hip);
    add(hip, new T.CylinderGeometry(0.065, 0.06, 0.42, 10), skin, 0, -0.21, 0);
    add(hip, new T.BoxGeometry(0.13, 0.08, 0.2), 0x2b2b2b, 0, -0.43, 0.03);
    return hip;
  });
  add(body, new T.CylinderGeometry(0.17, 0.18, 0.2, 16), o.pants || 0x35506b, 0, 0.5, 0);
  add(body, new T.CylinderGeometry(0.16, 0.185, 0.42, 16), o.shirt, 0, 0.78, 0);
  const arms = [-1, 1].map((s) => {
    const shoulder = new T.Group(); shoulder.position.set(0.235 * s, 0.98, 0); body.add(shoulder);
    const arm = add(shoulder, new T.CylinderGeometry(0.055, 0.05, 0.42, 10), skin, 0, -0.2, 0);
    arm.rotation.z = 0.12 * s; arm.position.x = 0.02 * s;
    return shoulder;
  });
  add(body, new T.SphereGeometry(0.2, 20, 16), skin, 0, 1.18, 0);
  if (o.face) {
    add(body, new T.SphereGeometry(0.025, 8, 6), 0x2a2320, -0.07, 1.2, 0.185);
    add(body, new T.SphereGeometry(0.025, 8, 6), 0x2a2320, 0.07, 1.2, 0.185);
    add(body, new T.SphereGeometry(0.03, 8, 6), 0xf29a7a, -0.12, 1.13, 0.16);
    add(body, new T.SphereGeometry(0.03, 8, 6), 0xf29a7a, 0.12, 1.13, 0.16);
    const smile = add(body, new T.TorusGeometry(0.05, 0.012, 6, 12, Math.PI), 0x2a2320, 0, 1.135, 0.19); smile.rotation.z = Math.PI;
  }
  if (o.hat) {
    add(body, new T.SphereGeometry(0.215, 20, 12, 0, Math.PI * 2, 0, Math.PI / 2), 0xffc53d, 0, 1.24, 0);
    add(body, new T.CylinderGeometry(0.27, 0.27, 0.03, 24), 0xeaa100, 0, 1.25, 0.02);
  } else {
    const hair = add(body, new T.SphereGeometry(0.215, 20, 14, 0, Math.PI * 2, 0, Math.PI * 0.62), o.hair || 0x2b1d16, 0, 1.2, -0.015);
    hair.rotation.x = -0.35;
  }
  // a go bag on the back; its group is centered on the bag so it can swell as it fills
  const bag = new T.Group(); bag.position.set(0, 0.84, -0.22); body.add(bag);
  const bagColor = o.bag || 0xe2382b;
  add(bag, new T.BoxGeometry(0.3, 0.34, 0.14), bagColor, 0, 0, 0);
  add(bag, new T.BoxGeometry(0.26, 0.12, 0.05), 0xb8261b, 0, -0.1, -0.09);
  add(bag, new T.BoxGeometry(0.3, 0.05, 0.15), 0xb8261b, 0, 0.17, 0);
  add(body, new T.BoxGeometry(0.05, 0.36, 0.03), 0x7a1a12, -0.1, 0.84, -0.155).userData.strap = true;
  add(body, new T.BoxGeometry(0.05, 0.36, 0.03), 0x7a1a12, 0.1, 0.84, -0.155).userData.strap = true;
  const straps = body.children.filter((c) => c.userData.strap);
  const showBag = (on) => { bag.visible = on; straps.forEach((s) => { s.visible = on; }); };
  showBag(!!o.bag);
  if (o.cape) add(body, new T.CylinderGeometry(0.2, 0.3, 0.6, 16, 1, true, Math.PI * 0.6, Math.PI * 0.8), 0xf2760c, 0, 0.72, 0.01, { side: T.DoubleSide });
  if (o.star) {
    const s = new T.Shape();
    for (let i = 0; i < 10; i++) { const a = Math.PI / 2 + i * Math.PI / 5, r = i % 2 ? 0.035 : 0.085; if (i) s.lineTo(Math.cos(a) * r, Math.sin(a) * r); else s.moveTo(Math.cos(a) * r, Math.sin(a) * r); }
    add(body, new T.ShapeGeometry(s), 0xffd24d, 0, 0.82, 0.18);
  }
  // where held things go: in front of the chest, between the hands
  const hands = new T.Group(); hands.position.set(0, 0.7, 0.27); body.add(hands);
  let holding = null, reachLeft = 0, holdStyle = 'front';

  // a soft shadow on the floor
  const shadow = new T.Mesh(new T.CircleGeometry(0.34, 24), new T.MeshBasicMaterial({ color: 0x000000, transparent: true, opacity: 0.2, depthWrite: false }));
  shadow.rotation.x = -Math.PI / 2; shadow.position.y = 0.026; shadow.renderOrder = 1; g.add(shadow);

  return {
    group: g,
    hands,
    bag,
    // the go bag on the back: shown or not, and how full (0 empty, 1 full, more is over full)
    setBag(on, fill) { showBag(on); const k = 1 + Math.min(1.4, Math.max(0, fill || 0)) * 0.42; bag.scale.set(k, k, 1 + (k - 1) * 1.6); },
    hold(obj) { if (holding) hands.remove(holding); holding = obj || null; if (holding) hands.add(holding); },
    // 'side': carried by the handle in the right hand while walking; 'front': both hands, ready to use
    setHoldStyle(s) { holdStyle = s; },
    get holding() { return holding; },
    reach() { reachLeft = 0.5; },
    // phase advances with distance walked; amount 0..1 blends from standing to a full stride
    pose(phase, amount, t, dt) {
      const swing = Math.sin(phase) * 0.6 * amount;
      legs[0].rotation.x = swing; legs[1].rotation.x = -swing;
      arms[0].rotation.x = -swing * 0.9; arms[1].rotation.x = swing * 0.9;
      arms[0].rotation.z = 0; arms[1].rotation.z = 0;
      body.position.y = Math.abs(Math.cos(phase)) * 0.04 * amount;
      if (holding && holdStyle === 'side') { // the right arm carries it down at the side
        arms[0].rotation.x = swing * 0.25; arms[0].rotation.z = -0.14;
      } else if (holding) { // both hands in front, holding it
        arms[0].rotation.x = arms[1].rotation.x = -1.05 + swing * 0.08;
        arms[0].rotation.z = 0.3; arms[1].rotation.z = -0.3;
      } else if (reachLeft > 0) { // reaching out to pick something up, then pulling it in
        reachLeft = Math.max(0, reachLeft - (dt || 0.016));
        const k = Math.sin((1 - reachLeft / 0.5) * Math.PI);
        arms[1].rotation.x = -1.5 * k; arms[0].rotation.x = -0.4 * k;
      } else if (o.wave && amount < 0.05) { // Bayani waves while standing
        arms[1].rotation.z = 2.4 + Math.sin(t * 6) * 0.25; arms[1].rotation.x = 0;
      }
    },
  };
}
