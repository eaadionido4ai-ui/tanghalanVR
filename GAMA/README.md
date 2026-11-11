# TANGHALAN VR in GAMA

This folder is a SIMPLE learning package. Add it to the SIMPLE WebPlatform; it reads `settings.json`
(experiment `vr_xp` of `models/TANGHALAN VR.gaml`, 1 to 6 headsets).

| File | What it is |
|---|---|
| `models/TANGHALAN.gaml` | The model: the flood in the street (a grid of one-metre cells, the rain, the canal blocked by trash, the culvert), the earthquake's shaking and its aftershock, and the neighbors who walk to the evacuation center. It runs on its own too, with its own display. |
| `models/TANGHALAN VR.gaml` | The link to the Unity app, through the SIMPLE plugin: the `unity_linker` and `unity_player` species and the `vr_xp` experiment. |
| `results/tanghalan_log.csv` | Written while the model runs: what each headset does, by headset id only (no names). |

The numbers (the street's grid, the flood, the shaking, the neighbors) are the same as in Unity
(`Unity/Assets/TANGHALAN/Scripts/Core`) and in the Python reference (`Tools/flood-reference`).
`Tools/check_constants.py` checks that the three agree; change a number in all three places.

Places use TANGHALAN's web coordinates (metres; x east, z south). GAMA draws them at (x + 30, z + 25), and
Unity puts them at (x, y, -z).

## The messages

**From GAMA to Unity** (`send_message` to every headset). Each is a map whose `tanghalan` entry says what it is:

| `tanghalan` | Other entries | Sent |
|---|---|---|
| `flood` | `t`: seconds of the storm; `rain`: mm/h × 10; `d`: the water depth of each cell in mm, row by row from the north | at every step of a storm |
| `storm_end` | `door_mm`: the deepest water at Rico's door, in mm | when the storm is over |
| `quake` | `i`: how strongly the ground shakes × 1000 | at every step of the earthquake |
| `people` | `p`: for each neighbor x, y, z (web coordinates, mm), heading (degrees), arrived (0 or 1) | while the neighbors walk |
| `drill` | | when the teacher presses **Start the earthquake drill** |

**From Unity to GAMA** (actions of `unity_linker`; every argument is a string, the first is the headset's id):

| Action | When |
|---|---|
| `tg_hello(id, lang)` | the visit starts (`fil` or `en`) |
| `tg_event(id, kind, detail)` | anything else worth logging: a work seen, the go bag's result, a step of the drill |
| `tg_canal(id, left)` | a piece of trash is picked up; `left` is how many are still in the canal |
| `tg_storm(id, left)` | the visitor starts the storm, with this much trash left |
| `tg_storm_stop(id)` | the visitor stops the storm early |
| `tg_quake(id)` | the earthquake starts |
| `tg_evacuate(id)` | the shaking is over and the walk to the evacuation center begins |
| `tg_arrived(id, stars, walk, duck, cover, hold)` | the visitor reaches the evacuation center: the drill's stars, the walk's seconds, the seconds to duck and to take cover, the share of the shaking spent holding on (%) |

If GAMA does not answer within 3 seconds of being asked, the app runs the same models itself, so the visit goes on without GAMA.

## Not yet done

- The model has not been run in GAMA yet. It was checked by hand against the SIMPLE plugin's source. Open
  `models/TANGHALAN VR.gaml` in GAMA 2025.06 with the SIMPLE plugin and report what does not compile.
- There is one storm and one earthquake for all the headsets: when one headset starts the earthquake, every
  headset shakes.
