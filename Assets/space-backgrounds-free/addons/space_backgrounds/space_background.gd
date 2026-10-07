class_name SpaceBackground
extends Node2D
## One Space Backgrounds scene as a parallax background: its layers drawn back to front over a rect
## (the whole screen by default), each repeating both ways and moving at its own rate as the view
## scrolls, the nebula drifting on its own. And the turning planets, asteroids and nebula loops as
## SpriteFrames. Read from sheets/effects.json.
##
##     var bg := SpaceBackground.add(self, "alien")          # behind everything, in a CanvasLayer of its own
##     SpaceBackground.add(self, "crimson", "pixel")          # the pixel set, nearest-filtered
##     bg.change("void", 3.0)                                 # crossfades to another scene
##     add_child(SpaceBackground.sprite("planet_terran"))     # a turning planet, playing
##     $Rock.sprite_frames = SpaceBackground.frames("asteroid_ice", "pixel")
##
## It follows the current Camera2D both ways; set `follow_camera` off and `scroll` for any other
## source. A layer moves `scroll` times its effects.json "scroll" (0 for the back, 0.7 for the
## asteroids) and drifts its "drift" pixels a second (at 1080 high) times `speed`.

## Where sheets/ is: found in this folder, or at res://sheets/ as the zip has it. Set it if the
## sheets are elsewhere.
static var sheets_dir := ""
static var default_set := "1080"
static var _data: Dictionary = {}   # scene -> its layers' entries, back to front
static var _sheets: Dictionary = {}   # planet, asteroid or nebula loop -> its entry

var scene := ""
var sheet_set := ""
## The area covered, in the node's space; empty covers the viewport.
var rect := Rect2()
var scroll := Vector2.ZERO
var follow_camera := true
var speed := 1.0
var time := 0.0
## Layers left out, by shape: "stars_far", "nebula", "stars_near", "planet", "asteroids".
var layers_off: Array[String] = []
var _layers: Array[Dictionary] = []   # {entry, texture}
var _old: Array[Dictionary] = []
var _mix := 1.0
var _fading: Tween


## The scene names in the installed sheets, e.g. "alien".
static func scenes() -> PackedStringArray:
	return PackedStringArray(_scenes().keys())


## A scene's layers' effects.json entries, back to front.
static func layers(scene_name: String) -> Array:
	return _scenes()[scene_name]


## The animated sheets of a group: "planets", "asteroids" or "nebulae"; e.g. "planet_terran".
static func sheets(group: String) -> PackedStringArray:
	_scenes()
	return PackedStringArray(_sheets.keys().filter(func(n: String) -> bool: return _sheets[n].group == group))


## An animated sheet's effects.json entry.
static func entry(sheet: String) -> Dictionary:
	_scenes()
	return _sheets[sheet]


## An animated sheet cut into SpriteFrames: one looping "default" animation at the sheet's fps.
static func frames(sheet: String, set_name := "") -> SpriteFrames:
	set_name = set_name if set_name else default_set
	var e := entry(sheet)
	var tex: Texture2D = load(sheets_dir.path_join(set_name).path_join(e.file))
	var cell := Vector2(e.cell[set_name][0], e.cell[set_name][1])
	var out := SpriteFrames.new()
	out.set_animation_speed(&"default", e.fps)
	out.set_animation_loop(&"default", e.loop)
	for i in int(e.frames):
		var a := AtlasTexture.new()
		a.atlas = tex
		a.region = Rect2(Vector2(i % int(e.columns), i / int(e.columns)) * cell, cell)
		out.add_frame(&"default", a)
	return out


## An AnimatedSprite2D playing a sheet, nearest-filtered for the pixel set.
static func sprite(sheet: String, set_name := "") -> AnimatedSprite2D:
	set_name = set_name if set_name else default_set
	var s := AnimatedSprite2D.new()
	s.sprite_frames = frames(sheet, set_name)
	s.texture_filter = TEXTURE_FILTER_NEAREST if set_name == "pixel" else TEXTURE_FILTER_LINEAR
	s.play()
	return s


## Adds `scene_name` behind the scene: a CanvasLayer at `layer` under `parent`, holding it.
static func add(parent: Node, scene_name: String, set_name := "", layer := -10) -> SpaceBackground:
	var canvas := CanvasLayer.new()
	canvas.layer = layer
	var s := SpaceBackground.new()
	s.setup(scene_name, set_name)
	canvas.add_child(s)
	parent.add_child(canvas)
	return s


static func _scenes() -> Dictionary:
	if _data.is_empty():
		if sheets_dir.is_empty():
			var here := "res://addons/space_backgrounds/sheets/"
			sheets_dir = here if ResourceLoader.exists(here + "effects.json") else "res://sheets/"
		var json: JSON = load(sheets_dir.path_join("effects.json"))
		for e: Dictionary in json.data.effects:
			if "group" in e:
				_sheets[e.name] = e
				continue
			if e.element not in _data:
				_data[e.element] = []
			_data[e.element].append(e)
	return _data


func setup(scene_name: String, set_name := "") -> void:
	if scene_name not in _scenes():
		push_error("SpaceBackground: no scene %s in %s" % [scene_name, sheets_dir])
		return
	scene = scene_name
	sheet_set = set_name if set_name else default_set
	_layers.clear()
	for e: Dictionary in _scenes()[scene_name]:
		_layers.append({"entry": e, "texture": load(sheets_dir.path_join(sheet_set).path_join(e.file))})
	texture_repeat = TEXTURE_REPEAT_ENABLED
	texture_filter = TEXTURE_FILTER_NEAREST if sheet_set == "pixel" else TEXTURE_FILTER_LINEAR
	queue_redraw()


## Crossfades to `scene_name` over `seconds`, in the same set.
func change(scene_name: String, seconds := 2.0) -> void:
	if _fading:
		_fading.kill()
	_old = _layers.duplicate()
	setup(scene_name, sheet_set)
	_mix = 0.0
	_fading = create_tween()
	_fading.tween_property(self, "_mix", 1.0, seconds)
	_fading.finished.connect(func() -> void: _old.clear())


func _process(delta: float) -> void:
	time += delta * speed
	if follow_camera:
		var cam := get_viewport().get_camera_2d()
		if cam:
			scroll = cam.get_screen_center_position()
	queue_redraw()


func _draw() -> void:
	var area := rect if rect.has_area() else get_viewport_rect()
	if _mix < 1.0:
		_draw_layers(_old, area, 1.0)
	_draw_layers(_layers, area, _mix)


func _draw_layers(list: Array[Dictionary], area: Rect2, alpha: float) -> void:
	for l: Dictionary in list:
		var e: Dictionary = l.entry
		if e.shape in layers_off:
			continue
		var tex: Texture2D = l.texture
		var k := tex.get_height() / area.size.y   # texture pixels per screen pixel
		var at: Vector2 = scroll * e.get("scroll", 0.0) + Vector2(time * e.get("drift", 0.0) * area.size.y / 1080.0, 0.0)
		draw_texture_rect_region(tex, area, Rect2(at * k, area.size * k), Color(1, 1, 1, alpha))
