extends DirectionalLight3D

@export var day_length_sec: float = 720.0

var _phase: float = 0.22

@onready var _env: WorldEnvironment = $"../WorldEnvironment"
var _sky_mat: ProceduralSkyMaterial


func _ready() -> void:
	if _env and _env.environment and _env.environment.sky:
		_sky_mat = _env.environment.sky.sky_material as ProceduralSkyMaterial


func _process(delta: float) -> void:
	_phase = fmod(_phase + delta / day_length_sec, 1.0)
	var a := _phase * TAU
	var u := 0.5 + 0.5 * sin(a)
	rotation_degrees.x = lerpf(-32.0, -172.0, u)
	rotation_degrees.y = cos(a * 0.85) * 22.0

	var day_amt := clampf((-rotation_degrees.x - 32.0) / 105.0, 0.0, 1.0)

	light_energy = lerpf(0.1, 1.32, day_amt)
	light_color = Color(
		1.0,
		lerpf(0.62, 0.93, day_amt),
		lerpf(0.38, 0.76, day_amt),
		1.0
	)

	var sunset_amt := 1.0 - absf(day_amt - 0.35) / 0.35
	sunset_amt = clampf(sunset_amt, 0.0, 1.0) * clampf(day_amt * 4.0, 0.0, 1.0)

	if _env and _env.environment:
		var env := _env.environment
		env.ambient_light_energy = lerpf(0.11, 0.4, day_amt)
		env.ambient_light_color = Color(
			lerpf(0.38, 0.56, day_amt) + sunset_amt * 0.08,
			lerpf(0.42, 0.48, day_amt),
			lerpf(0.52, 0.42, day_amt),
			1.0
		)

		env.fog_light_color = Color(
			lerpf(0.22, 0.68, day_amt) + sunset_amt * 0.15,
			lerpf(0.18, 0.62, day_amt) + sunset_amt * 0.05,
			lerpf(0.25, 0.52, day_amt),
			1.0
		)

	if _sky_mat:
		_sky_mat.sky_top_color = Color(
			lerpf(0.06, 0.32, day_amt),
			lerpf(0.08, 0.48, day_amt),
			lerpf(0.18, 0.72, day_amt),
			1.0
		)
		_sky_mat.sky_horizon_color = Color(
			lerpf(0.15, 0.72, day_amt) + sunset_amt * 0.2,
			lerpf(0.10, 0.65, day_amt) + sunset_amt * 0.08,
			lerpf(0.12, 0.55, day_amt) - sunset_amt * 0.1,
			1.0
		)
		_sky_mat.ground_horizon_color = Color(
			lerpf(0.10, 0.48, day_amt) + sunset_amt * 0.12,
			lerpf(0.08, 0.42, day_amt),
			lerpf(0.10, 0.36, day_amt),
			1.0
		)
		_sky_mat.ground_bottom_color = Color(
			lerpf(0.04, 0.18, day_amt),
			lerpf(0.04, 0.16, day_amt),
			lerpf(0.06, 0.12, day_amt),
			1.0
		)
