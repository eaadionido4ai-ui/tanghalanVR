# TANGHALAN VR

**Three-dimensional Animated Gallery Highlighting Youth-Generated Artworks, Learnings and Disaster Narratives, in virtual reality**

TANGHALAN VR is the [TANGHALAN](https://github.com/eaadionido4ai-ui/tanghalan) walk-in museum of children's
disaster art and stories, rebuilt with the tools of the SIMPLE VR contest:

- **Unity**: the project is the SIMPLE Unity Template VR (`project-SIMPLE/simple.toolchain`, branch `Unity-6`) for
  Meta Quest headsets, with TANGHALAN added in `Unity/Assets/TANGHALAN`.
- **GAMA**: the storm over the street, the earthquake and the neighbors who walk to the evacuation center are an
  agent-based model in `GAMA/models`. It talks to the headsets through the SIMPLE GAMA plugin and the SIMPLE
  WebPlatform.
- **Blender**: every model (the museum, the street, the go bag items, the people, the trash) passes
  through Blender. The `.blend` files are in `Blender/`, and the scripts that make them are in `Tools/blender`.

The museum, its 19 sample works, their Filipino and English texts, and the Go Bag Room's rules all come from the
TANGHALAN web app. A tool exports them with the web app's own code, so the two stay the same.

> 🧪 Version 0.1.0, for review. The code compiles against Unity 6000.3.8f1, and the rules and simulations pass
> their tests. It has **not yet been played in the Unity editor or on a headset**, and the GAMA model has not yet
> been run in GAMA (see [What has been checked](#what-has-been-checked)). The artworks and stories are samples;
> learners' own works, shared with their parents' consent, will replace them.

## The visit

1. **Lobby.** Choose Filipino or English and start. Bayani, the museum's guide, walks beside you. The tasks board
   on the left wall shows what to do, and says to pack the go bag last: the earthquake drill comes right after
   it. The board on the right shows whether GAMA is connected.
2. **The halls.** Point at a drawing or a story stand and press the trigger. A card opens with the work, its text in
   Filipino or English, and Bayani's tip.
   - You are not alone: a class on a field trip walks through the halls with their teacher, in three small
     groups. They stop at the works and look; the teacher explains and a child points. They wait or go round you
     when you are in their way, step aside when you come close or stand between them and a work, and move on
     from a work you want to read. Now and then a child waves at you.
3. **The street** (through the door in the *Stop Hazards Early* hall). This is the street from Rico's story "Ang
   Kanal sa Aming Kalye": the canal is full of trash.
   - The neighbors are out: Aling Rosa sweeps the sidewalk with a *walis tingting* while her daughter sits on the
     doorstep, Mang Ben (Rico's father) talks with Lola Ising, Rico and Lola Ising's granddaughter throw a ball,
     and two children play tag at the end of the street. They stop for you when you are in their way.
   - Point at a piece of trash to pick it up, then point at the right bin: *nabubulok*, *nare-recycle* or
     *di-nabubulok* (RA 9003).
   - Then press **See what the rain would do**. GAMA replays two hours of heavy rain in about a minute and a half:
     the water rises in the street, and the gauge at Rico's door shows how deep it gets with the trash that is
     left. The board compares this with a canal nobody cleaned. The neighbors go inside while it rains, and come
     out again after.
4. **The Go Bag Room.** The web app's Go Bag Challenge, in VR:
   - Take a bag at the packing table. On Easy, the things are on the tables; on Difficult, they are hidden in the
     cabinet, the drawers, the crate and the cooler.
   - Look at each thing, then pack it or leave it. The bag hangs from your left hand and swells as it fills. Over
     14 weight units, you walk slower.
   - Zip it up to see your stars. Zipping the bag is what brings the earthquake.
5. **The earthquake drill.** It comes about 8 seconds after the go bag is zipped, whatever else you have or have
   not done yet. (With a class on GAMA, it comes when everyone's bag is zipped: see
   [Running it](#running-it).)
   - The alarm rings, the ground trembles, and Bayani calls "Earthquake! Duck, Cover, and Hold!". Three seconds
     later the ground shakes. The pictures rattle, the controllers rumble and a deep sound fills the room. Your
     own view never shakes, for comfort.
   - **Duck**: crouch, or press A. **Cover**: point at the nearest sturdy table and press the trigger; you are moved
     under it. **Hold**: squeeze a grip button until the shaking stops. Then wait a moment.
   - The class does it too: the children and their teacher take cover under the tables near them (or drop and
     cover their heads where there is none), and the neighbors drop where they are.
6. **The evacuation.**
   - Arrows on the floor show the safe route: out of the street door, down the steps, over the bridge, along the
     sidewalk to the covered court, the barangay's evacuation center. Your go bag comes with you.
   - Bayani walks ahead. The class walks out after you, and the neighbors walk along the sidewalk; GAMA moves the
     neighbors.
   - An aftershock comes on the way: stop, duck and cover your head. Everyone else stops and ducks too.
7. **At the evacuation center.** Bayani counts everyone, the neighbors and the class, and the board shows the visit: the works
   you saw, the canal, the flood at Rico's door, the go bag's stars, and the drill (how fast you ducked and took
   cover, how long you held on, your walk to the center). It also says what you left out, such as a canal you
   did not clear.

The Fire Safety Room is in the museum, but its game is not built yet in VR. Its rules are ported and tested, and
the room says "coming soon".

## Running it

You need **Unity 6000.3.8f1** (exactly this version, as the SIMPLE template requires) with Android Build Support,
OpenJDK and the Android SDK & NDK tools. See the SIMPLE guide:
[doc.project-simple.eu/unity/installation](https://doc.project-simple.eu/unity/installation).

1. In Unity Hub, **Add project from disk** and choose the `Unity` folder of this repository. The first import takes a while.
2. Open `Assets/TANGHALAN/Scenes/Main Scene` and press **Play**.

**With no headset**, the scene plays with the keyboard and the mouse (TANGHALAN VR switches to this by itself):

| | With a Meta Quest | With the keyboard and mouse |
|---|---|---|
| Walk | left stick | W A S D |
| Jump to a spot | right stick forward, aim, let go | |
| Turn | right stick left and right | right mouse button and drag, or Q and E |
| Point and choose | aim the controller, **trigger** | aim with the mouse, **left click** |
| Duck | crouch, or **A** / **X** | **C** |
| Hold on (in the drill) | **grip** button | **Space** |

**On a Meta Quest**:

1. In **File > Build Profiles**, choose **Android** and click **Switch Platform**.
2. With the headset in developer mode and plugged in, click **Build and Run**.
3. The app opens the SIMPLE Startup Menu. Start it, or set the computer's IP address first in the IP Menu.

**With GAMA** (a SIMPLE session):

1. Install GAMA 2025.06 with the SIMPLE plugin, and the SIMPLE WebPlatform
   ([doc.project-simple.eu](https://doc.project-simple.eu)).
2. Add this repository's `GAMA` folder as a learning package: it has the `settings.json` the WebPlatform reads.
3. Launch **TANGHALAN VR** from the WebPlatform, then start the app in Unity or on the headsets.

The board in the lobby says when GAMA is connected. While it is, GAMA runs the storm, the earthquake and the
neighbors. GAMA also writes what the visitors do to `GAMA/results/tanghalan_log.csv` (by headset id only, no
names).

**A class on GAMA drills together.** Up to 6 headsets can join one session.

- Each headset tells GAMA when its go bag is zipped. GAMA's map shows how many are.
- A visitor who zips early reads "Waiting for your classmates: 3 of 5 go bags are zipped", and can pack again
  for more stars.
- When every bag is zipped, GAMA waits 8 seconds and starts the earthquake on all the headsets at once: the same
  shaking, the same neighbors on the street, the same aftershock.
- The teacher can start it at any time with GAMA's **Start the earthquake drill** button.

Each visitor still has their own museum, Bayani and score: the visitors do not see each other yet.

**Without GAMA**, after a few seconds, or as soon as GAMA stops answering, the app runs the same models itself (in
`Tanghalan.Core`). A class without a laptop can still play. See [GAMA/README.md](GAMA/README.md) for the messages
between GAMA and Unity.

## How it is put together

```
Unity/                          the SIMPLE Unity Template VR (Unity 6000.3.8f1), with:
  Assets/TANGHALAN/
    Scenes/Main Scene.unity     the template's FPS player scene, with TANGHALAN VR's Game Manager and app
    Scripts/Core/               the rules and the simulations, with no Unity in them (also built and tested outside Unity)
      GoBagRules, FireRules     the web app's rules (src/rooms/logic.js), unchanged
      MuseumData, GoBagItems    the rooms, works and items, written from the web app's content (Generated/)
      StreetPlan, FloodModel    the street's grid and its flood
      Waste                     the canal's trash and the three bins
      Drill, Evacuation         when the earthquake comes, Duck-Cover-Hold, the way out, the neighbors' walk
      Visitors, StreetLife      the class in the museum, and the neighbors on the street: what each one does
      People, NavGrid, Plan     a person's place and pose; where people can walk in the museum, and the way (A*)
      MuseumPlan (Generated/)   the museum's walls, furniture and sturdy tables, from the exported model
      Strings, Visit            every text in English and Filipino; the visit and its summary
    Scripts/Runtime/            the Unity side: TanghalanApp (the visit), World (loading the models), Rig (the player),
                                Ui (panels, pointing), Tour, Street, GoBagRoom, DrillController, Bayani, Sfx,
                                Crowds (the other people), Figure (a person's legs and arms, moved without a skeleton),
                                Scenario (GAMA or local), TanghalanSimulationManager (the template's SimulationManager)
    Tests/EditMode/             the tests, for Unity's Test Runner and for `dotnet test Tests`
    Resources/TANGHALAN/        the models (FBX), textures and materials
  Assets/Scripts, Scenes, ...   the SIMPLE template, unchanged
GAMA/                           the SIMPLE learning package: settings.json, models/TANGHALAN.gaml (the model) and
                                models/TANGHALAN VR.gaml (the link to Unity)
Blender/                        the Blender files of the museum, the street and the props
Tools/
  web-export/                   builds the museum, the street, the props and the people with the web app's three.js code, and exports them
  blender/                      turns the exports into .blend and FBX files, with one palette of colors; renders previews
  build_assets.sh               runs the whole asset pipeline
  unity_meta.py, make_materials.py, make_scene.py    write Unity's .meta files, materials, main scene and build settings
  flood-reference/              the flood model in Python: the reference that Unity's and GAMA's are checked against
  check_constants.py            checks that Unity, GAMA and the reference use the same numbers
  unity-compile-check/          compiles all the Unity scripts against Unity's own libraries, without the editor
Tests/                          runs the tests with .NET:  dotnet test Tests
```

**The asset pipeline.** `Tools/build_assets.sh` rebuilds everything from the web app's sources:

1. It exports the museum (as the web app builds it, plus the street door, the drill tables and the Go Bag Room's
   furniture), the street, and the props as glTF.
2. Blender joins the pieces and paints them from one shared palette, then saves `.blend` and FBX files.
3. It writes the textures, Unity's `.meta` files and the C# data.

The models carry named boxes and points (see `Tools/web-export/src/tag.js`): walls, floors you can teleport to,
works you can point at, the bins, the tables to hide under, and where Bayani, the trash and the go bag items go.

**Where things are.** Everything shares the web app's coordinates: metres, x to the east, z to the south, y up. In
Unity a point (x, y, z) is at (x, y, -z), and in GAMA it is at (x + 30, z + 25).

## What has been checked

| Check | How | Result |
|---|---|---|
| The rules match the web app | the web app's tests, ported (`Tests/`); the same go bag rounds for the same seeds | 55 tests pass |
| The flood | C# against the Python reference, step by step; water conserved; more trash, deeper water | pass |
| The drill and the evacuation | when the earthquake comes (alone, with a class, with GAMA silent); scored runs of the drill; every room has a way out through a doorway; all neighbors arrive | pass |
| Unity, GAMA and the reference agree | `Tools/check_constants.py` | pass |
| The Unity scripts compile | `Tools/unity-compile-check`: the template and TANGHALAN VR, against Unity 6000.3.8f1's libraries and the packages' sources | pass |
| The other people | the visitors' ways stay clear of walls and furniture and go through the doorways; in ten minutes they look at many works; they keep out of the visitor's way and out of the line to the work being read; they take cover, stay down, duck in the aftershock and all reach the court; the neighbors sweep, sit, talk, throw the ball and play tag, go in when it rains and come out after, and reach the court | pass |
| The models look right | renders from the Blender files (`Tools/blender/render_previews.py`); every pose of the people rendered with the joint angles of `Figure.cs` | checked by eye |
| **Playing in the Unity editor and on a Quest** | | **not done yet** |
| **The GAMA model in GAMA** | GAMA could not be run where this was built; the model was checked against the SIMPLE plugin's source | **not done yet** |

So the first things to do are: open the project in Unity 6000.3.8f1, play the main scene, then open
`GAMA/models/TANGHALAN VR.gaml` in GAMA 2025.06 with the SIMPLE plugin. Please report what does not work.

## Still to do

- Play-test in Unity and on Meta Quest headsets, then fix what comes up: placing of panels, sizes of text, comfort.
- Have a Filipino teacher read the Filipino written for TANGHALAN VR: the go bag items, Bayani's tips and lines,
  the drill, and the summary. The works' own texts are the web app's.
- Record narration, so the works and Bayani can be heard as well as read.
- Build the Fire Safety Room's game in VR, on its ported rules.
- Show classmates to each other in the museum (the SIMPLE plugin can; TANGHALAN VR does not use it yet).
- Make the museum's other visitors agents in GAMA, as the neighbors are, so that a class on GAMA sees the same
  visitors and GAMA runs their walk out (for now each headset moves its own).
- A drill for a visitor who is outdoors when the earthquake comes. For now, a visitor on the street is called
  back inside the museum first.
- Replace the sample works with learners' own, with consent.

## Credits

TANGHALAN: conceptualized and developed with AI assistance by Edward Andrew A. Dionido, UP Resilience Institute,
University of the Philippines. TANGHALAN VR was built with AI assistance (Claude) from the TANGHALAN web app.

Built with the SIMPLE toolchain of project SIMPLE (IRD, UMMISCO; the ACROSS program; co-funded by the European
Union): the SIMPLE Unity Template VR and the GAMA plugin (`project-SIMPLE/simple.toolchain`), GAMA
(gama-platform.org), and Blender (blender.org). The template includes third-party packages and assets with their
own terms: DOTween, websocket-sharp, NativeWebSocket, the Supercyan Character Pack Free Sample, polyextrude, and
Unity's XR samples. The go bag rules and items come from the BAYANIHanda Game Library's Go Bag Packing 3D and Fire
Extinguisher 3D.

## License

No license has been chosen for TANGHALAN's own content and code, so all rights are reserved by the author for now,
as for TANGHALAN. The SIMPLE template, the GAMA plugin (LGPL-2.1) and the third-party packages keep their own
licenses.
