// Small 3D models of the go bag items, from Go Bag Packing 3D (GoBag._make in the BAYANIHanda Game Library's
// engine), drawn with the museum's matte materials. Each model sits with its base at y = 0.

export function makeItem(T, id) {
  const g = new T.Group();
  const mats = makeItem.mats || (makeItem.mats = new Map());
  const M = (c, r, m) => { // the original's roughness and metalness are dropped: the museum is matte
    const key = 'm' + c; let mm = mats.get(key);
    if (!mm) { mm = new T.MeshLambertMaterial({ color: c }); mats.set(key, mm); }
    return mm;
  };
  const EM = (c, e, i) => new T.MeshLambertMaterial({ color: c, emissive: e, emissiveIntensity: i == null ? 0.7 : i });
  const box = (w, h, d, m) => new T.Mesh(new T.BoxGeometry(w, h, d), m);
  const cyl = (a, b, h, m, s) => new T.Mesh(new T.CylinderGeometry(a, b, h, s || 18), m);
  const sph = (r, m) => new T.Mesh(new T.SphereGeometry(r, 18, 12), m);
  const cone = (r, h, m) => new T.Mesh(new T.ConeGeometry(r, h, 18), m);
  const tor = (r, t, m) => new T.Mesh(new T.TorusGeometry(r, t, 10, 20), m);
  const glass = (c, o) => new T.MeshLambertMaterial({ color: c, transparent: true, opacity: o == null ? 0.55 : o });
  function add(mesh, x, y, z, rx, ry, rz) { mesh.position.set(x || 0, y || 0, z || 0); if (rx) mesh.rotation.x = rx; if (ry) mesh.rotation.y = ry; if (rz) mesh.rotation.z = rz; g.add(mesh); return mesh; }
  const PI = Math.PI;
  let i, j;
  // eslint-disable-next-line no-var
  var bmat, mshell;
        switch (id) {
    case "water":
      add(cyl(0.052, 0.062, 0.15, glass(0xa8ddf5, 0.5)), 0, 0.1);
      add(cyl(0.05, 0.058, 0.09, M(0x3aa0d8, 0.2, 0.15)), 0, 0.06);
      add(cyl(0.03, 0.052, 0.045, glass(0xa8ddf5, 0.5)), 0, 0.19);
      add(cyl(0.026, 0.026, 0.05, M(0x2a6fb0)), 0, 0.225);
      add(cyl(0.03, 0.03, 0.035, M(0x1f5f95)), 0, 0.255);
      add(cyl(0.063, 0.063, 0.045, M(0xf4f7f9, 0.8)), 0, 0.09); break;
    case "food":
      add(cyl(0.07, 0.07, 0.13, M(0xb8bcc0, 0.3, 0.75)), 0, 0.065);
      add(cyl(0.072, 0.072, 0.09, M(0xcf3b3b, 0.6)), 0, 0.062);
      add(cyl(0.073, 0.073, 0.018, M(0xf0e2c0, 0.6)), 0, 0.062);
      add(tor(0.069, 0.007, M(0x9aa0a5, 0.3, 0.8)), 0, 0.128, 0, PI / 2);
      add(tor(0.069, 0.007, M(0x9aa0a5, 0.3, 0.8)), 0, 0.004, 0, PI / 2); break;
    case "flashlight":
      add(cyl(0.033, 0.036, 0.17, M(0x222528, 0.5)), 0, 0.085);
      add(cyl(0.04, 0.04, 0.02, M(0x111214)), 0, 0.02);
      add(cyl(0.055, 0.04, 0.05, M(0x4a4f54, 0.4, 0.6)), 0, 0.195);
      add(cyl(0.05, 0.05, 0.012, M(0xbfc4c8, 0.3, 0.9)), 0, 0.222);
      add(cyl(0.044, 0.044, 0.01, EM(0xfff6c0, 0xffe066, 1.0)), 0, 0.227);
      add(box(0.02, 0.03, 0.012, M(0xd23b3b)), 0, 0.11, 0.036);
      add(tor(0.037, 0.006, M(0x111)), 0, 0.13, 0, PI / 2); break;
    case "batteries":
      add(cyl(0.02, 0.02, 0.12, M(0x2aa84a, 0.5)), -0.026, 0.06);
      add(cyl(0.02, 0.02, 0.12, M(0x2aa84a, 0.5)), 0.026, 0.06);
      add(cyl(0.021, 0.021, 0.03, M(0x111)), -0.026, 0.02);
      add(cyl(0.021, 0.021, 0.03, M(0x111)), 0.026, 0.02);
      add(cyl(0.009, 0.009, 0.018, M(0xd4af37, 0.3, 0.8)), -0.026, 0.129);
      add(cyl(0.009, 0.009, 0.018, M(0xd4af37, 0.3, 0.8)), 0.026, 0.129); break;
    case "firstaid": // green box, white cross on the front (the library's fix: the red cross is a protected emblem, RA 10530)
      add(box(0.2, 0.13, 0.13, M(0x1e9c5a, 0.6)), 0, 0.065);
      add(box(0.2, 0.035, 0.13, M(0x17834a, 0.6)), 0, 0.13);
      add(box(0.052, 0.016, 0.004, M(0xffffff)), 0, 0.056, 0.067);
      add(box(0.016, 0.052, 0.004, M(0xffffff)), 0, 0.056, 0.067);
      add(box(0.07, 0.02, 0.025, M(0x333333)), 0, 0.15, 0, 0, 0, 0); break;
    case "whistle":
      add(box(0.09, 0.05, 0.05, M(0xd94444, 0.5)), 0, 0.03);
      add(box(0.05, 0.036, 0.05, M(0xd94444, 0.5)), 0.06, 0.03);
      add(cyl(0.02, 0.02, 0.05, M(0xb03636)), 0.005, 0.058, 0, 0, 0, PI / 2);
      add(tor(0.014, 0.005, M(0x888c90, 0.3, 0.7)), -0.052, 0.05, 0, PI / 2); break;
    case "radio":
      add(box(0.16, 0.12, 0.06, M(0x2b2f33, 0.6)), 0, 0.06);
      add(box(0.075, 0.09, 0.006, M(0x14161a)), -0.035, 0.065, 0.031);
      for (i = -1; i <= 1; i++) for (j = -1; j <= 1; j++) add(cyl(0.006, 0.006, 0.006, M(0x050505)), -0.035 + i * 0.02, 0.065 + j * 0.024, 0.033, PI / 2);
      add(box(0.05, 0.022, 0.006, EM(0x1aae63, 0x0a5a30, 0.6)), 0.04, 0.095, 0.031);
      add(cyl(0.014, 0.014, 0.012, M(0xd0d3d6, 0.4, 0.6)), 0.03, 0.04, 0.031, 0, 0, PI / 2);
      add(cyl(0.014, 0.014, 0.012, M(0xd0d3d6, 0.4, 0.6)), 0.065, 0.04, 0.031, 0, 0, PI / 2);
      add(cyl(0.004, 0.004, 0.17, M(0x9aa0a5, 0.3, 0.7)), 0.07, 0.19, 0, 0, 0, -0.28);
      add(sph(0.008, M(0x9aa0a5)), 0.115, 0.27); break;
    case "powerbank":
      add(box(0.14, 0.04, 0.075, M(0x1c1c22, 0.5)), 0, 0.05);
      add(box(0.14, 0.008, 0.075, M(0x2a2a32)), 0, 0.074);
      add(box(0.02, 0.008, 0.012, M(0x111)), -0.05, 0.05, 0.04);
      add(box(0.01, 0.005, 0.006, EM(0x3ee39b, 0x1aa46a, 0.9)), 0.03, 0.074, 0.02);
      add(box(0.01, 0.005, 0.006, EM(0x3ee39b, 0x1aa46a, 0.9)), 0.045, 0.074, 0.02); break;
    case "documents":
      add(box(0.15, 0.02, 0.11, M(0xe8b34a, 0.7)), 0, 0.02);
      add(box(0.14, 0.012, 0.1, M(0xffffff)), 0.006, 0.04, 0.004, 0, -0.03);
      add(box(0.14, 0.012, 0.1, M(0xf3f3ee)), -0.004, 0.055, -0.004, 0, 0.04);
      add(box(0.05, 0.01, 0.032, M(0x9aa7b0)), 0.03, 0.066, 0.02, 0, 0.04);
      add(box(0.018, 0.006, 0.02, M(0x3f6fb0)), 0.018, 0.072, 0.02, 0, 0.04); break;
    case "meds":
      add(cyl(0.045, 0.05, 0.1, M(0xc98a3a, 0.35)), 0, 0.055);
      add(cyl(0.052, 0.052, 0.028, M(0xffffff)), 0, 0.12);
      add(box(0.06, 0.055, 0.002, M(0xffffff)), 0, 0.055, 0.05);
      add(box(0.03, 0.008, 0.001, M(0xd23b3b)), 0, 0.065, 0.051); break;
    case "mask":
      mshell = sph(0.09, M(0xeef4f8, 0.7)); mshell.scale.set(1, 0.6, 0.5); add(mshell, 0, 0.075);
      add(box(0.16, 0.004, 0.02, M(0xdbe6ee)), 0, 0.088, -0.006);
      add(box(0.16, 0.004, 0.02, M(0xdbe6ee)), 0, 0.075, -0.008);
      add(box(0.16, 0.004, 0.02, M(0xdbe6ee)), 0, 0.062, -0.006);
      add(cyl(0.02, 0.008, 0.006, M(0xb0b8be)), 0, 0.105, 0.028);
      add(tor(0.05, 0.005, M(0xcfd8de)), 0.088, 0.075, 0, 0, PI / 2);
      add(tor(0.05, 0.005, M(0xcfd8de)), -0.088, 0.075, 0, 0, PI / 2); break;
    case "cash":
      add(box(0.16, 0.008, 0.075, M(0x3a9d5a, 0.7)), 0, 0.02);
      add(box(0.16, 0.008, 0.075, M(0x41a866, 0.7)), 0.004, 0.03, 0.003);
      add(box(0.16, 0.008, 0.075, M(0x3a9d5a, 0.7)), -0.003, 0.04, -0.003);
      add(box(0.02, 0.03, 0.078, M(0xe8b84a)), 0, 0.03);
      add(cyl(0.03, 0.03, 0.01, M(0xd4af37, 0.3, 0.8)), 0.095, 0.01, 0.03, 0, 0, PI / 2); break;
    case "clothes":
      add(box(0.16, 0.05, 0.12, M(0x4f8fd0, 0.9)), 0, 0.03);
      add(box(0.14, 0.04, 0.1, M(0x5a9bd8, 0.9)), 0.006, 0.07);
      add(box(0.16, 0.008, 0.12, M(0x3d78b8, 0.9)), 0, 0.056);
      add(box(0.04, 0.05, 0.12, M(0x4f8fd0, 0.9)), -0.062, 0.05); break;
    case "toy":
      add(sph(0.075, M(0x9a6a34, 0.9)), 0, 0.085);
      add(sph(0.05, M(0x9a6a34, 0.9)), 0, 0.185);
      add(sph(0.02, M(0x8a5a2b)), -0.038, 0.225); add(sph(0.02, M(0x8a5a2b)), 0.038, 0.225);
      add(sph(0.026, M(0xd7b183)), 0, 0.17, 0.042);
      add(sph(0.008, M(0x2b1d10)), -0.02, 0.2, 0.045); add(sph(0.008, M(0x2b1d10)), 0.02, 0.2, 0.045);
      add(sph(0.009, M(0x2b1d10)), 0, 0.165, 0.062);
      add(sph(0.026, M(0x8a5a2b)), -0.072, 0.095, 0.02); add(sph(0.026, M(0x8a5a2b)), 0.072, 0.095, 0.02);
      add(sph(0.03, M(0x8a5a2b)), -0.04, 0.03, 0.03); add(sph(0.03, M(0x8a5a2b)), 0.04, 0.03, 0.03); break;
    case "contacts":
      add(box(0.11, 0.02, 0.15, M(0x3f6fb0, 0.7)), 0, 0.02);
      add(box(0.1, 0.016, 0.14, M(0xffffff)), 0, 0.036);
      for (i = -1; i <= 1; i++) add(box(0.07, 0.002, 0.003, M(0xbcc4cc)), 0.005, 0.045, i * 0.028);
      add(cyl(0.004, 0.004, 0.15, M(0x9aa0a5, 0.3, 0.7)), -0.05, 0.03, 0, PI / 2); break;
    case "console":
      add(box(0.19, 0.045, 0.09, M(0x2a2a30, 0.5)), 0, 0.045);
      add(box(0.06, 0.03, 0.06, EM(0x24384a, 0x0a1a2a, 0.4)), 0, 0.05, 0);
      add(sph(0.016, M(0x151519)), -0.06, 0.075); add(sph(0.016, M(0x151519)), 0.06, 0.075);
      add(box(0.01, 0.006, 0.03, M(0x151519)), -0.085, 0.072); add(box(0.03, 0.006, 0.01, M(0x151519)), -0.085, 0.072);
      add(sph(0.008, M(0xd23b3b)), 0.09, 0.072, 0.01); add(sph(0.008, M(0x2e7d32)), 0.075, 0.072, 0.02); break;
    case "soda":
      add(cyl(0.058, 0.058, 0.13, M(0xcf2030, 0.3, 0.5)), 0, 0.065);
      add(cyl(0.06, 0.06, 0.05, M(0xf4f4f4, 0.5)), 0, 0.07);
      add(cyl(0.056, 0.058, 0.02, M(0xd7d7d7, 0.3, 0.8)), 0, 0.009);
      add(cyl(0.05, 0.056, 0.02, M(0xd7d7d7, 0.3, 0.8)), 0, 0.132);
      add(box(0.024, 0.006, 0.02, M(0x9a9a9a, 0.3, 0.8)), 0, 0.142, 0.015); break;
    case "icecream":
      add(cone(0.05, 0.13, M(0xd8a86a, 0.9)), 0, 0.07, 0, PI);
      add(sph(0.058, M(0xff9ec1)), 0, 0.15);
      add(sph(0.045, M(0xfff0b0)), 0.035, 0.17);
      add(sph(0.018, M(0xd21f3c)), 0, 0.205);
      add(cyl(0.003, 0.003, 0.03, M(0x5a3a1a)), 0, 0.225); break;
    case "tv":
      add(box(0.24, 0.15, 0.028, M(0x14161b, 0.5)), 0, 0.13);
      add(box(0.2, 0.12, 0.006, EM(0x2a4a66, 0x0a1a2a, 0.5)), 0, 0.13, 0.017);
      add(box(0.11, 0.02, 0.05, M(0x2a2a30)), 0, 0.02);
      add(box(0.02, 0.05, 0.03, M(0x2a2a30)), 0, 0.05); break;
    case "bricks":
      add(box(0.18, 0.06, 0.09, M(0xa6522d, 0.95)), 0, 0.03);
      add(box(0.18, 0.06, 0.09, M(0x9c4a27, 0.95)), 0.025, 0.09);
      add(box(0.18, 0.008, 0.092, M(0xcbb89a)), 0, 0.061);
      add(box(0.18, 0.008, 0.092, M(0xcbb89a)), 0.025, 0.121); break;
    case "books":
      add(box(0.17, 0.035, 0.12, M(0xc0392b, 0.85)), 0, 0.02, 0, 0, 0.04);
      add(box(0.15, 0.03, 0.1, M(0xf3f0e6)), 0.012, 0.02, 0, 0, 0.04);
      add(box(0.17, 0.035, 0.12, M(0x2e7d32, 0.85)), 0.006, 0.056, 0, 0, -0.05);
      add(box(0.17, 0.035, 0.12, M(0x2c4a8a, 0.85)), -0.004, 0.092, 0, 0, 0.03); break;
    case "hairdryer":
      add(cyl(0.045, 0.048, 0.13, M(0xe0679a, 0.6)), 0, 0.13, 0, 0, 0, PI / 2);
      add(cyl(0.05, 0.03, 0.03, M(0xcf4f86)), 0.085, 0.13, 0, 0, 0, PI / 2);
      add(cyl(0.046, 0.046, 0.012, M(0x333)), -0.075, 0.13, 0, 0, 0, PI / 2);
      add(box(0.045, 0.1, 0.045, M(0xe0679a, 0.6)), -0.02, 0.05);
      add(box(0.03, 0.014, 0.02, M(0x222)), -0.02, 0.09, 0.026); break;
    case "balloon":
      bmat = new T.MeshLambertMaterial({ color: 0xe23a2b, transparent: true, opacity: 0.9 });
      add(sph(0.085, bmat), 0, 0.18);
      add(cone(0.018, 0.03, M(0xc02a1e)), 0, 0.095, 0, PI);
      add(cyl(0.003, 0.003, 0.12, M(0x999)), 0, 0.06);
      add(sph(0.02, EM(0xffffff, 0xffffff, 0.15)), -0.03, 0.21, 0.05); break;
    case "beachball":
      add(sph(0.1, M(0xffffff, 0.5)), 0, 0.1);
      add(tor(0.1, 0.02, M(0xe23a2b)), 0, 0.1, 0, 0, 0, 0.5);
      add(tor(0.1, 0.02, M(0x2aa7d6)), 0, 0.1, 0, PI / 2, 0, 0.5);
      add(tor(0.1, 0.02, M(0xffd23d)), 0, 0.1, 0, 0, PI / 2, 0.5); break; // the original put this ring 1.57 m away (PI / 2 as its z)
    default:
      add(box(0.12, 0.12, 0.12, M(0x9aa7b0)), 0, 0.06);
  }
  return g;
}
