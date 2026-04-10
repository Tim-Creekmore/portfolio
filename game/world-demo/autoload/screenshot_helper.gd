extends Node

## F9: screenshot everywhere. F12: same on desktop; skipped on Web (browser devtools).
## Writes PNG + JSON next to each other under user://screenshots/
## Web: triggers browser downloads for both files.

const SUBDIR := "screenshots"


func _unhandled_input(event: InputEvent) -> void:
	if not event is InputEventKey:
		return
	if not event.pressed or event.echo:
		return
	var key: int = event.physical_keycode
	var want_shot: bool = (key == KEY_F9)
	if not want_shot and key == KEY_F12 and not OS.has_feature("web"):
		want_shot = true
	if not want_shot:
		return
	get_viewport().set_input_as_handled()
	call_deferred("_run_capture")


func _run_capture() -> void:
	await get_tree().process_frame
	await get_tree().process_frame

	var vp := get_viewport()
	var tex := vp.get_texture()
	if tex == null:
		push_warning("ScreenshotHelper: no viewport texture")
		return
	var img := tex.get_image()
	if img == null:
		push_warning("ScreenshotHelper: could not get image")
		return
	# WebGL viewport buffer is already top-down; flipping again inverts saved PNGs.
	if not OS.has_feature("web"):
		img.flip_y()

	var dt := Time.get_datetime_dict_from_system()
	var base := "screenshot_%04d%02d%02d_%02d%02d%02d_%d" % [
		int(dt.year), int(dt.month), int(dt.day),
		int(dt.hour), int(dt.minute), int(dt.second),
		Time.get_ticks_msec()
	]

	var scene_path := ""
	var scene_name := ""
	if get_tree().current_scene:
		scene_path = str(get_tree().current_scene.scene_file_path)
		scene_name = get_tree().current_scene.name

	var meta: Dictionary = {
		"captured_at_local": Time.get_datetime_string_from_system(),
		"unix_time": Time.get_unix_time_from_system(),
		"engine_version": Engine.get_version_info(),
		"app_version": ProjectSettings.get_setting("application/config/version", ""),
		"app_name": ProjectSettings.get_setting("application/config/name", ""),
		"scene_path": scene_path,
		"scene_name": scene_name,
		"fps": Engine.get_frames_per_second(),
		"platform": OS.get_name(),
		"renderer": ProjectSettings.get_setting("rendering/renderer/rendering_method", ""),
		"window_size": [DisplayServer.window_get_size().x, DisplayServer.window_get_size().y],
		"viewport_size": [vp.get_visible_rect().size.x, vp.get_visible_rect().size.y],
	}

	var player := get_tree().get_first_node_in_group("player")
	if player is Node3D:
		var p: Node3D = player
		meta["player_global_position"] = {
			"x": snappedf(p.global_position.x, 0.01),
			"y": snappedf(p.global_position.y, 0.01),
			"z": snappedf(p.global_position.z, 0.01),
		}
		var yaw := p.rotation.y
		meta["player_yaw_rad"] = snappedf(yaw, 0.001)

	var json_text := JSON.stringify(meta, "\t")

	if OS.has_feature("web"):
		_web_download(img.save_png_to_buffer(), base + ".png", "image/png")
		_web_download(json_text.to_utf8_buffer(), base + ".json", "application/json")
		print("ScreenshotHelper: browser download started — ", base, ".png + .json")
		return

	_ensure_user_screenshots_dir()
	var root := "user://".path_join(SUBDIR)
	var png_path := root.path_join(base + ".png")
	var json_path := root.path_join(base + ".json")

	var err := img.save_png(png_path)
	if err != OK:
		push_error("ScreenshotHelper: save_png failed: %d" % err)
		return
	var f := FileAccess.open(json_path, FileAccess.WRITE)
	if f:
		f.store_string(json_text)
		f.close()
	else:
		push_error("ScreenshotHelper: could not write JSON")

	var abs_base := ProjectSettings.globalize_path(root)
	print("ScreenshotHelper: saved ", ProjectSettings.globalize_path(png_path))
	print("ScreenshotHelper: saved ", ProjectSettings.globalize_path(json_path))
	print("ScreenshotHelper: folder ", abs_base)


func _ensure_user_screenshots_dir() -> void:
	var d := DirAccess.open("user://")
	if d == null:
		push_error("ScreenshotHelper: cannot open user://")
		return
	if not d.dir_exists(SUBDIR):
		var e := d.make_dir_recursive(SUBDIR)
		if e != OK and e != ERR_ALREADY_EXISTS:
			push_warning("ScreenshotHelper: make_dir_recursive returned ", e)


func _web_download(data: PackedByteArray, filename: String, mime: String) -> void:
	if not Engine.has_singleton("JavaScriptBridge"):
		return
	var js: Object = Engine.get_singleton("JavaScriptBridge")
	if js and js.has_method("download_buffer"):
		js.download_buffer(data, filename, mime)
