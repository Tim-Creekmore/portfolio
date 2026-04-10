extends Node3D


func _ready() -> void:
	$Player.global_position = WorldData.get_spawn_position()
	$Player.rotation.y = 0.5 * PI
	Input.mouse_mode = Input.MOUSE_MODE_CAPTURED

	if OS.has_feature("web"):
		var env: Environment = $WorldEnvironment.environment
		if env:
			env.glow_intensity *= 0.72
			env.glow_bloom *= 0.6
