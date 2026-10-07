extends Node2D
## Pick a scene and a sheet set, turn layers off to see how one is built; the camera drifts on its
## own, the arrow keys take over both ways. Asteroids tumble past and planets turn along the
## bottom, from their sheets; left alone, it crossfades through the scenes. With the live planet
## shader in the project, a planet drawn by it sits on the right: pick its preset.

const IDLE := 7.0
const DRIFT := Vector2(70.0, 18.0)
const FAST := 600.0
const LIVE := "res://addons/space_backgrounds/space_planet.gd"

var bg: SpaceBackground
var camera := Camera2D.new()
var buttons: Dictionary = {}
var idle := IDLE
var bodies := Node2D.new()
var rocks: Array[AnimatedSprite2D] = []
var live  # a SpacePlanet, by path: the free sampler has none


func _ready() -> void:
	add_child(camera)
	camera.make_current()
	var names := SpaceBackground.scenes()
	var first: Dictionary = SpaceBackground.layers(names[0])[0]
	var sets := ["1080", "540", "pixel"].filter(func(s: String) -> bool:  # the web build leaves 1080 out
		return ResourceLoader.exists(SpaceBackground.sheets_dir.path_join(s).path_join(first.file)))
	SpaceBackground.default_set = sets[0]
	bg = SpaceBackground.add(self, names[0])
	var front := CanvasLayer.new()
	front.layer = -5
	front.add_child(bodies)
	add_child(front)
	_bodies(sets[0])

	var ui := CanvasLayer.new()
	ui.layer = 20
	add_child(ui)
	var bar := VBoxContainer.new()
	bar.position = Vector2(16, 12)
	bar.size.x = 1248
	ui.add_child(bar)
	var row := HFlowContainer.new()
	bar.add_child(row)
	var group := ButtonGroup.new()
	for n in names:
		var b := Button.new()
		b.text = n.replace("_", " ")
		b.toggle_mode = true
		b.button_group = group
		b.pressed.connect(func() -> void:
			idle = IDLE * 3
			show_scene(n))
		row.add_child(b)
		buttons[n] = b
	var opts := HFlowContainer.new()
	bar.add_child(opts)
	var set_group := ButtonGroup.new()
	for s: String in sets:
		var b := Button.new()
		b.text = s
		b.toggle_mode = true
		b.button_group = set_group
		b.button_pressed = s == sets[0]
		b.pressed.connect(func() -> void:
			bg.setup(bg.scene, s)
			_bodies(s))
		opts.add_child(b)
	opts.add_child(VSeparator.new())
	for l: Dictionary in SpaceBackground.layers(names[0]):
		if l.shape == "space":
			continue
		var c := CheckButton.new()
		c.text = l.shape.replace("_", " ")
		c.button_pressed = true
		c.toggled.connect(func(on: bool) -> void:
			if on:
				bg.layers_off.erase(l.shape)
			else:
				bg.layers_off.append(l.shape))
		opts.add_child(c)
	var c := CheckButton.new()
	c.text = "sprites"
	c.button_pressed = true
	c.toggled.connect(func(on: bool) -> void: bodies.visible = on)
	opts.add_child(c)
	if ResourceLoader.exists(LIVE):
		var pick := OptionButton.new()
		var presets: PackedStringArray = load(LIVE).call("presets")
		for p in presets:
			pick.add_item(p.trim_prefix("planet_").replace("_", " "))
		pick.item_selected.connect(func(i: int) -> void: live.preset = presets[i])
		var label := Label.new()
		label.text = "live:"
		opts.add_child(label)
		opts.add_child(pick)
		live = load(LIVE).new()
		live.radius = 90.0
		live.position = Vector2(1080, 380)
		live.preset = presets[0]
		front.add_child(live)
	var hint := Label.new()
	hint.text = "Arrow keys pan."
	hint.add_theme_color_override("font_color", Color(0.9, 0.92, 1.0))
	hint.add_theme_color_override("font_outline_color", Color(0, 0, 0, 0.7))
	hint.add_theme_constant_override("outline_size", 4)
	opts.add_child(hint)
	buttons[names[0]].button_pressed = true


## Planets turning along the bottom and asteroids tumbling across, from the sheets of `set_name`.
func _bodies(set_name: String) -> void:
	for n in bodies.get_children():
		n.queue_free()
	rocks.clear()
	var planets := SpaceBackground.sheets("planets")
	var shown := mini(planets.size(), 6)
	var cell := 1e9   # a plain planet's cell; a ringed one's is bigger, the planet the same
	for p in planets:
		cell = minf(cell, SpaceBackground.entry(p).cell[set_name][0])
	for i in shown:
		var s := SpaceBackground.sprite(planets[i * planets.size() / shown], set_name)
		s.scale = Vector2.ONE * 110.0 / cell
		s.position = Vector2(110 + i * 150, 640)
		s.frame = i * 5
		bodies.add_child(s)
	var list := SpaceBackground.sheets("asteroids")
	for i in 7:
		if list.is_empty():
			break
		var s := SpaceBackground.sprite(list[i % list.size()], set_name)
		s.scale = Vector2.ONE * (30.0 + 14.0 * (i % 3)) / s.sprite_frames.get_frame_texture(&"default", 0).get_width()
		s.position = Vector2(i * 197.0, 200 + (i * 131) % 300)
		s.frame = i * 3
		s.speed_scale = 0.6 + 0.15 * (i % 4)
		bodies.add_child(s)
		rocks.append(s)


func show_scene(n: String) -> void:
	if n == bg.scene:
		return
	bg.change(n, 1.5)
	buttons[n].set_pressed_no_signal(true)


func _process(delta: float) -> void:
	var dir := Input.get_vector(&"ui_left", &"ui_right", &"ui_up", &"ui_down")
	if dir != Vector2.ZERO:
		idle = IDLE * 3
	camera.position += (dir * FAST if dir != Vector2.ZERO else DRIFT) * delta
	for i in rocks.size():
		var r := rocks[i]
		r.position.x = fposmod(r.position.x + (40.0 + 25.0 * (i % 3)) * delta, 1400.0) if dir == Vector2.ZERO \
				else fposmod(r.position.x - dir.x * FAST * 0.9 * delta, 1400.0)
		r.rotation += (0.2 + 0.1 * (i % 3)) * delta
	idle -= delta
	if idle > 0.0:
		return
	idle = IDLE
	var names := SpaceBackground.scenes()
	show_scene(names[(names.find(bg.scene) + 1) % names.size()])
