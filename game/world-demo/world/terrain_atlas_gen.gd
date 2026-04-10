extends RefCounted

## Procedural 512×512 atlas, 4×4 grid of 128×128 tiles (uses first 8 slots).


func ensure_atlas_file() -> void:
	if not Engine.is_editor_hint():
		return
	var path := "res://world/textures/terrain_atlas.png"
	if FileAccess.file_exists(path):
		return
	var img := build_image()
	var err := img.save_png(path)
	if err != OK:
		push_warning("TerrainAtlasGen: could not save %s (%s)" % [path, err])


func build_image() -> Image:
	var tw := 128
	var grid := 4
	var size := tw * grid
	var img := Image.create(size, size, false, Image.FORMAT_RGB8)
	img.fill(Color(0.2, 0.15, 0.12))
	var tiles: Array[Color] = [
		Color(0.48, 0.44, 0.4),
		Color(0.42, 0.3, 0.2),
		Color(0.22, 0.38, 0.18),
		Color(0.58, 0.48, 0.34),
		Color(0.28, 0.46, 0.22),
		Color(0.32, 0.2, 0.12),
		Color(0.38, 0.28, 0.2),
		Color(0.4, 0.36, 0.32),
	]
	for idx in tiles.size():
		var gx := idx % grid
		var gy := idx / grid
		_fill_tile(img, gx * tw, gy * tw, tw, tiles[idx], float(idx * 17 + 3))
	return img


func _fill_tile(img: Image, ox: int, oy: int, tw: int, base: Color, seed: float) -> void:
	for ly in tw:
		for lx in tw:
			var fx := float(lx) / float(tw)
			var fy := float(ly) / float(tw)
			var edge := mini(mini(lx, tw - 1 - lx), mini(ly, tw - 1 - ly))
			var shade := 0.88 + 0.12 * sin(fx * 6.2 + seed) * cos(fy * 5.1 + seed * 0.7)
			shade *= lerpf(0.82, 1.0, clampf(float(edge) / 10.0, 0.0, 1.0))
			var n := _hash2(float(ox + lx) * 0.31, float(oy + ly) * 0.29 + seed)
			var c := base * shade * (0.92 + n * 0.14)
			c.r = clampf(c.r, 0.0, 1.0)
			c.g = clampf(c.g, 0.0, 1.0)
			c.b = clampf(c.b, 0.0, 1.0)
			img.set_pixel(ox + lx, oy + ly, c)


func _hash2(x: float, y: float) -> float:
	var t: float = sin(x * 127.1 + y * 311.7) * 43758.5453
	return t - floor(t)


func create_texture() -> ImageTexture:
	var tex := ImageTexture.create_from_image(build_image())
	tex.set_filter_recursive(true)
	return tex
