extends SceneTree

const _TA := preload("res://world/terrain_atlas_gen.gd")


func _init() -> void:
	var gen = _TA.new()
	var img: Image = gen.build_image()
	var err: Error = img.save_png("res://world/textures/terrain_atlas.png")
	print("save_atlas_once: err=", err)
	quit()
