extends Control


func _ready() -> void:
	$Center/VBox/VersionLabel.text = "v %s" % str(ProjectSettings.get_setting("application/config/version", "0.1.0"))
	if OS.has_feature("web"):
		$Center/VBox/QuitButton.visible = false
	$Center/VBox/PlayButton.pressed.connect(_on_play_pressed)
	$Center/VBox/QuitButton.pressed.connect(_on_quit_pressed)


func _on_play_pressed() -> void:
	get_tree().change_scene_to_file("res://world/world.tscn")


func _on_quit_pressed() -> void:
	get_tree().quit()
