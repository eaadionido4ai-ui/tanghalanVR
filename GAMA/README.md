# TANGHALAN VR in GAMA

This folder is a SIMPLE learning package. Add it to the SIMPLE WebPlatform; it reads `settings.json`
(experiment `vr_xp` of `models/TANGHALAN VR.gaml`, 1 to 6 headsets).

| File | What it is |
|---|---|
| `models/TANGHALAN.gaml` | The model: the flood in the street (a grid of one-metre cells, the rain, the canal blocked by trash, the culvert), the earthquake's shaking and its aftershock, and the neighbors who walk to the evacuation center. It runs on its own too, with its own display. |
| `models/TANGHALAN VR.gaml` | The link to the Unity app, through the SIMPLE plugin: the `unity_linker` and `unity_player` species and the `vr_xp` experiment. It also decides when the class's earthquake drill starts. |
| `results/tanghalan_log.csv` | Written while the model runs: what each headset does, by headset id only (no names). |

The numbers (the street's grid, the flood, the shaking, the neighbors) are the same as in Unity
(`Unity/Assets/TANGHALAN/Scripts/Core`) and in the Python reference (`Tools/flood-reference`).
`Tools/check_constants.py` checks that the three agree; change a number in all three places.

Places use TANGHALAN's web coordinates (metres; x east, z south). GAMA draws them at (x + 30, z + 25), and
Unity puts them at (x, y, -z).

The eight neighbors (`evacuee`) start from where they are outside before the drill, in front of their houses
(in Unity they sweep, talk and play there: `StreetLife`). When the visitor walks out after the shaking, they walk
along the west sidewalk to the court's west side, clear of the summary board. In the aftershock they stop and
duck until it passes.

## The earthquake drill for a class

The earthquake comes after the go bag. With several headsets, GAMA makes the class drill together:

1. Each headset tells GAMA when its go bag is zipped (`tg_bag`). GAMA's map shows "Go bags zipped: 3 of 5
   headsets", and every two seconds GAMA sends this count to the headsets (`class`). A visitor who is waiting
   reads "Waiting for your classmates: 3 of 5 go bags are zipped", and may pack again for more stars.
2. When the bag of every headset is zipped, GAMA waits 8 seconds (`drill_delay`, the time to read the stars),
   then tells all of them to start (`drill`). The teacher can do this at any time with the
   **Start the earthquake drill** button, in GAMA's experiment panel.
3. On each headset the alarm rings and, 3 seconds later, the shaking starts. The first headset to ask
   (`tg_quake`) starts GAMA's earthquake, and the ones that ask within 5 seconds (`join_window`) share it: the
   same shaking, the same neighbors on the street, the same aftershock.
4. A headset that comes to its drill later runs its own earthquake while the class's is still going. Once
   that one is over (every headset in it has reached the evacuation center), GAMA runs a new one for it.

With one headset this is simply: the bag is zipped, and 8 seconds later the earthquake comes.

If GAMA says nothing for 10 seconds after a bag is zipped (it is paused, stopped, or not this model), the
headset starts its drill alone.

## The messages

**From GAMA to Unity** (`send_message`). Each is a map whose `tanghalan` entry says what it is:

| `tanghalan` | Other entries | Sent | To |
|---|---|---|---|
| `flood` | `t`: seconds of the storm; `rain`: mm/h × 10; `d`: the water depth of each cell in mm, row by row from the north | at every step of a storm | the headset that asked for the storm |
| `storm_end` | `door_mm`: the deepest water at Rico's door, in mm | when the storm is over | the same headset |
| `storm_busy` | | when a headset asks for a storm while another's is running: it runs its own | that headset |
| `class` | `ready`: how many go bags are zipped; `players`: how many headsets | every two seconds | every headset |
| `drill` | | when every go bag is zipped, or when the teacher presses **Start the earthquake drill** | the headsets that have not had their drill |
| `quake` | `i`: how strongly the ground shakes × 1000 | at every step of the earthquake | the headsets in that earthquake |
| `people` | `p`: for each neighbor x, y, z (web coordinates, mm), heading (degrees), arrived (0 or 1) | while the neighbors walk | the headsets in that earthquake |

**From Unity to GAMA** (actions of `unity_linker`; every argument is a string, the first is the headset's id):

| Action | When |
|---|---|
| `tg_hello(id, lang)` | the visit starts (`fil` or `en`) |
| `tg_event(id, kind, detail)` | anything else worth logging: a work seen, the go bag's result, a step of the drill |
| `tg_canal(id, left)` | a piece of trash is picked up; `left` is how many are still in the canal |
| `tg_storm(id, left)` | the visitor starts the storm, with this much trash left |
| `tg_storm_stop(id)` | the visitor stops the storm early |
| `tg_bag(id, stars)` | the go bag is zipped; said again every 4 seconds while the headset waits for the drill |
| `tg_quake(id)` | the shaking starts on the headset |
| `tg_evacuate(id)` | the shaking is over and the walk to the evacuation center begins (the first headset of an earthquake sends the neighbors on their way) |
| `tg_arrived(id, stars, walk, duck, cover, hold)` | the visitor reaches the evacuation center: the drill's stars, the walk's seconds, the seconds to duck and to take cover, the share of the shaking spent holding on (%) |

If GAMA does not answer within 3 seconds of being asked, the app runs the same models itself, so the visit goes on without GAMA.

## Not yet done

- The model has not been run in GAMA yet. It was checked by hand against the SIMPLE plugin's source. Open
  `models/TANGHALAN VR.gaml` in GAMA 2025.06 with the SIMPLE plugin and report what does not compile.
- GAMA runs one storm and one earthquake at a time. A headset that asks for another while one is running runs
  its own (the same model, in the app), so nobody's storm or drill is disturbed; but only one of them is
  GAMA's.
- The visitors do not see each other. The SIMPLE plugin can show the other players (`player_unity_properties`),
  but TANGHALAN VR does not use it yet.
- A visitor who is on the street when the drill starts is called back inside the museum, and their drill
  starts there, a little after the class's.
