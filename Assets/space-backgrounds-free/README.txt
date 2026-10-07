The full pack: Space Backgrounds has 12 scenes (an aurora, an alien world under a ringed giant,
crimson, golden, an inferno, a galactic core with a black hole and more), 20 rotating planets, 8
asteroids, 6 nebula loops, and the planet shader live in Godot to draw any of them at any size.
Same layers, same player.
https://heyheythere.itch.io/space-backgrounds

sheets/1080/   every scene's six layers at 1920x1080; the planets (256x256 a frame, 448x448 for the
               ringed ones, the stars and the black hole), asteroids (128x128) and nebula loops
               (640x360) as sheets of 8 frames a row
sheets/540/    the same at half size
sheets/pixel/  pixel-art versions, drawn at their size (layers 320x180, planets 64x64 or 112x112,
               asteroids 32x32, loops 320x180): scale them up with nearest filtering
sheets/effects.json  for each image: its scene and layer, or its group (planets, asteroids,
               nebulae); size per set, frames, fps, columns; the layer's "scroll" and "drift"

A scene is six PNGs in sheets/<set>/<scene>/, back to front:
    <scene>_space       the opaque back: deep space, faint glow, star dust     scroll 0
    <scene>_stars_far   many faint stars                                       scroll 0.03
    <scene>_nebula      the nebula, see-through                                scroll 0.08, drifts 4 px/s
    <scene>_stars_near  fewer, brighter stars, some with spikes                scroll 0.18
    <scene>_planet      the scene's planet                                     scroll 0.3
    <scene>_asteroids   rocks and dust                                         scroll 0.7
Every layer repeats seamlessly both ways. Move each by the camera's position times its scroll,
and the nebula also sideways by its drift (pixels a second at 1080 high).

The planets (planet_<name>.png), asteroids (asteroid_<name>.png) and nebula loops
(nebula_<scene>.png) are animation sheets, 8 frames a row: the planets turn once in 48 frames at
12 fps, the asteroids tumble in 32, the loops churn in 16 at 8 fps. All loop seamlessly.

Godot 4.3+
    Copy addons/space_backgrounds/ into your project and sheets/ into addons/space_backgrounds/,
    then
        var bg := SpaceBackground.add(self, "alien")    # behind everything, follows the Camera2D
        SpaceBackground.add(self, "crimson", "pixel")    # the pixel set
        bg.change("void", 3.0)                           # crossfade to another scene
        bg.layers_off = ["planet"]                       # leave a layer out
        add_child(SpaceBackground.sprite("planet_terran"))            # a planet, turning
        $Rock.sprite_frames = SpaceBackground.frames("asteroid_ice")  # SpriteFrames for your own
    SpaceBackground.default_set = "540" (or "pixel") switches every one made after it; set
    SpaceBackground.sheets_dir if sheets/ is elsewhere. Or use your own Parallax2D nodes: one per
    layer, scroll_scale its scroll, repeat_size the layer's size, autoscroll.x the nebula's drift.

    SpacePlanet (the full pack) draws a planet or asteroid live with its planet.gdshader, any
    size, smooth or as pixels:
        var p := SpacePlanet.new()
        p.preset = "planet_ringed"       # any planet_ or asteroid_ sheet's name
        p.radius = 120.0
        p.pixels = 96                    # 0 for smooth
        p.set_param("ring_color", Color.GOLD)
        add_child(p)
    Every uniform in planet.gdshaderinc can be set; they are described there.
    Open addons/space_backgrounds/demo/demo.tscn to try it all.

Unity
    Import the layers (Wrap Mode Repeat; for the pixel set Filter Mode Point, no compression), put
    each on a Sprite Renderer in Tiled draw mode bigger than the screen, children of the camera,
    and each frame offset it by the camera's position times its scroll, wrapped to its size. Slice
    the planet, asteroid and loop sheets with the Sprite Editor's Grid By Cell Size (the size in
    effects.json) and drag the frames into the scene for an animation.

GameMaker
    Add each layer as a background layer, back to front, with horizontal and vertical tiling on,
    and set its x and y to camera_x * (1 - scroll) and camera_y * (1 - scroll) each step (and its
    hspeed to the drift for the nebula). Import the sheets as sprite strips.

Anything else
    Draw the six back to front, each offset by the camera's position times its scroll, wrapped to
    its size; play the sheets as frame animations.
