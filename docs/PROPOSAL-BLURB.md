# Upwork proposal blurb (2-3 sentences, factual)

To show the parts of this job I consider risky, I built a small warehouse prototype in Unity 6
(source + playable WebGL build): first-person grab/carry/rotate/throw with 73 dynamic objects of
eight types with per-type mass and friction, PICK / PACK / LOAD tasks defined in JSON files, a
50 Hz fixed-timestep logger that records inputs and every moving object's pose and velocity
(about 0.3-0.6 MB per minute as plain JSON lines), and a replay mode that drives the recorded
transforms with a scrubbable timeline. A PlayMode test records real physics, replays it and checks
every object at every tick; the README lists what is simplified (single primitive map, transform
playback rather than re-simulation, mouse/keyboard only).

Live demo: https://chimpuchim.github.io/warehouse-sim-demo/
Source: https://github.com/chimpuchim/warehouse-sim-demo
