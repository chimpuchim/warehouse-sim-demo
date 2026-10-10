# Warehouse Sim Demo

A small first-person warehouse simulation built in Unity 6 to prove three things: physics
interaction (grab, carry, rotate, throw, stack), data-driven PICK / PACK / LOAD tasks, and a
fixed-timestep gameplay logger with an exact replay. It is a prototype, not a product: one map,
primitive art, mouse + keyboard only.

**Try it:** _(link added when the WebGL build is published)_ — click the game to lock the mouse.
Best in a desktop browser; the first load is about `SIZE` MB.

## What is in it

- **First-person controls** — WASD, mouse look, Shift sprint, Space jump (CharacterController).
- **Physics interaction** — hold the left mouse button to grab the object you are looking at,
  release to place it, right mouse button to throw, mouse wheel to change the hold distance,
  R + mouse to rotate it. Held objects stay dynamic rigidbodies driven by velocity, so they
  collide with the world and can be stacked. The maximum carry speed and throw speed fall with
  mass, and heavy loads slow the player, so a 25 kg pallet feels different from a 0.5 kg part.
- **52 interactable objects** of 8 types (small / medium / large box, crate, pallet, paint can,
  spare part, bottle) with per-type mass, friction, bounciness and collider shape. Dynamic
  bodies use box or capsule colliders only.
- **Three task types** from JSON: PICK (items from the racks into a tote), PACK (items into a
  box, then close the lid with F), LOAD (boxes up the ramp onto the truck bed). Each task has an
  order (required items and counts), a time limit, a base score and a time bonus.
- **Order panel, timer, scoring, local leaderboard** (top 10, kept in PlayerPrefs).
- **Session logger** — every physics tick (50 Hz) it writes the player's input and head pose and
  the pose + velocity of every object that moved. JSON Lines, spec below.
- **Replay** — loads a log and drives every tracked transform from it (kinematic playback) with a
  timeline you can scrub, pause, step and speed up, a POV / third-person camera toggle and the
  event list. It is transform playback, not a re-simulation.
- **Input seam** — movement reads `IPlayerInput`, grabbing reads `IGrabInput`. The shipped
  implementation is mouse + keyboard; a VR controller version would implement the same two
  interfaces. No VR support is included.

## Controls

| Action | Key |
|---|---|
| Move / sprint / jump | WASD / Shift / Space |
| Grab (hold) and place (release) | Left mouse button |
| Throw | Right mouse button while holding |
| Hold distance | Mouse wheel |
| Rotate held object | R + mouse |
| Open / close a pack box lid | F while looking at it |
| Pause (unlock mouse) / resume | Esc / click |
| Replay | Space play-pause, Left/Right step one tick, drag the timeline |

## Architecture

```
Bootstrap                 the only scene object; creates EventSystem, GameFlow (+ DevBridge in dev mode)
GameFlow                  state machine Menu -> Playing -> Result -> Replay; owns everything below
  WarehouseMap -> World   builds the map from primitives; World = items, zones, containers
  FirstPersonController   CharacterController locomotion, reads IPlayerInput
  GrabController          hold-to-grab physics carry, reads IGrabInput
  KeyboardMouseInput      IPlayerInput + IGrabInput for mouse/keyboard (Input System package)
  ScriptedInput           IPlayerInput + IGrabInput driven by code (automation, tests)
  TaskLibrary             loads StreamingAssets/Tasks/tasks.json + task files (JsonUtility)
  TaskRunner              counts required items inside the target Zone, lid state, timer -> TaskResult
  Leaderboard             local top 10
  SessionRecorder         FixedUpdate logger -> ILogSink (MemorySink always, FileSink on desktop)
  SessionLog / LogFormat  line models, tick serializer, parser, per-tick snapshots
  ReplayPlayer            kinematic playback of a SessionLog; Seek / Step / ApplyTick
  Hud, MenuScreen, ResultScreen, ReplayScreen   uGUI built in code (UI.cs), built-in font
```

Data flow during a task:

```
input device --> KeyboardMouseInput --> FirstPersonController / GrabController --> PhysX
                                                 |                                   |
                                        (input sample, held id)              (rigidbody poses)
                                                 v                                   v
                                     SessionRecorder.FixedUpdate  ---- tick line ----> sinks
TaskRunner (0.2 s poll) -- counts items in Zone --> progress / finished --> events, result line
```

## Session log format

One file per task attempt, `<taskId>_<yyyyMMdd_HHmmss>.jsonl`, UTF-8, one JSON object per line,
`type` says which. Numbers are written with 4 decimals (positions, velocities, 0.1 mm) and
5 decimals (quaternions), invariant culture.

```
{"type":"header","version":1,"app":"WarehouseSimDemo 0.1.0","task":"pick_1042","player":"Cuong",
 "fixedDt":0.02,"startedUtc":"2026-10-10T08:12:31.120Z",
 "objects":[{"id":0,"name":"Player","item":"player"},{"id":1,"name":"box_large_01","item":"box_large"}, ...]}
{"type":"tick","t":0,"mv":[0,0],"lk":[0,0],"b":0,"g":-1,
 "ids":[0,1,2,...],"p":[x,y,z, x,y,z, ...],"r":[x,y,z,w, ...],"v":[vx,vy,vz, ...],"w":[wx,wy,wz, ...]}
{"type":"event","t":87,"name":"grab","obj":14,"data":""}
{"type":"tick","t":87,"mv":[0,1],"lk":[0.4,-0.1],"b":5,"g":14,"ids":[0,14],"p":[...],"r":[...],"v":[...],"w":[...]}
...
{"type":"result","t":1120,"outcome":"success","score":296,"durationSeconds":22.4}
```

| Field | Meaning |
|---|---|
| `t` | tick index; tick `t` is the state at time `t * fixedDt` seconds |
| `mv`, `lk` | move axis (strafe, forward) and look delta (yaw, pitch degrees) sampled that tick |
| `b` | button bits: 1 sprint, 2 jump, 4 grab held, 8 throw, 16 interact, 32 rotate |
| `g` | object id currently held, -1 if none |
| `ids` | objects written in this tick, in order; `p`/`r`/`v`/`w` hold 3/4/3/3 floats per id |
| object 0 | the player's head (camera) position and rotation; `v` is the body velocity |

Only objects whose pose changed since they were last written appear in a tick (tick 0 has all
of them), so idle objects cost nothing. The parser expands this into a full pose per object per
tick, which is what the replay scrubs through. Events: `task_start`, `grab`, `drop`, `throw`,
`lid_closed`, `lid_opened`, `progress`, `task_success` / `task_timeout` / `task_aborted`.

Where logs go: on Windows/macOS/Linux builds and in the Editor, `persistentDataPath/Sessions/`
(streamed to disk while playing, flushed every second). On WebGL there is no disk: the log is kept
in memory and the result screen's **Download log** button saves it through the browser;
**Open log file...** on the menu loads one back for replay.

Measured size: `MEASURED_SIZE`.

## Adding a task (no code)

1. Copy a file in `Assets/StreamingAssets/Tasks/`, e.g. `pick_order_1042.json`:

   ```json
   {
     "id": "pick_1050",
     "type": "PICK",
     "title": "Pick order #1050",
     "description": "Shown on the HUD.",
     "timeLimitSeconds": 120,
     "baseScore": 100,
     "timeBonusPerSecond": 2,
     "targetZone": "tote_b",
     "requireClosed": false,
     "requiredItems": [ { "item": "box_medium", "count": 1 }, { "item": "bottle", "count": 2 } ]
   }
   ```

2. Add the file name to `tasks.json`. The menu lists it on the next start; invalid files are
   reported on the menu instead of loaded.

`type` is `PICK`, `PACK` or `LOAD` (a label for the player; all three are evaluated the same
way: required items inside `targetZone`, plus the lid when `requireClosed` is true).
Zones in the map: `tote_a`, `tote_b`, `pack_box_a`, `pack_box_b` (lidded), `truck_bed`.
Items: `box_small`, `box_medium`, `box_large`, `crate`, `pallet`, `can`, `part`, `bottle`
(`ItemCatalog.cs` has the sizes and masses). Standalone builds read the folder next to the
executable, so tasks can be edited after building.

## Tests

```bash
unity test --mode EditMode     # task JSON parsing/validation, scoring, log line round trip, parser
unity test --mode PlayMode     # record 150 physics ticks, replay them, compare every transform; 4-box stack stable 10 s
```

`TEST_RESULTS`

## Build

```bash
unity build --target WebGL --execute-method Warehouse.EditorTools.WarehouseBuild.BuildWebGL -o Build/WebGL
unity build --target StandaloneWindows64 --execute-method Warehouse.EditorTools.WarehouseBuild.BuildWindows -o Build/Windows/WarehouseSimDemo.exe
python3 tools/serve.py 8790      # serve Build/WebGL on http://127.0.0.1:8790/
```

The build method creates the scene and applies the player settings on first run.
Unity 6000.0.77f1, Universal RP, Input System, uGUI. Physics and logging run at a fixed 50 Hz.

## Developer mode

`?dev=1` on the WebGL URL (always on in the Editor) adds a `Dev` object that accepts commands via
`unityInstance.SendMessage("Dev", "Cmd", "...")`: `start <taskId>`, `walkto x z`, `look yaw pitch`,
`lookat <object>`, `grab`, `release`, `throw`, `interact`, `stat`, `replay`, `seek <tick>`,
`stacktest`, `menu`. They drive the game through `ScriptedInput`, so an automated run exercises
the real movement, grab and task code. Used for the screenshots and checks in this README.

## Known limits

- One map, built from primitives; no art, audio or animation.
- Replay is transform playback of the log. It does not re-run the physics engine, and it does not
  replay input; the log keeps the input so a future deterministic-replay experiment has the data.
- WebGL keeps the log in memory and downloads it on request; it is not written to disk.
- Plain JSON Lines, no compression: see the measured size above. Gzip would cut it several times.
- No VR: the input interfaces exist, the controller implementation does not.
- Legacy uGUI with the built-in font; the UI is functional, not styled.
- Task evaluation counts an item inside a zone by its centre point and polls every 0.2 s.

## Not done

`NOT_DONE`
