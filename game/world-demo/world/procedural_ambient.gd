extends Node

@onready var _sun: DirectionalLight3D = $"../DirectionalLight3D"

var _wind: AudioStreamPlayer
var _drone: AudioStreamPlayer
var _wind_playback: AudioStreamGeneratorPlayback
var _drone_playback: AudioStreamGeneratorPlayback
var _wind_smooth: float = 0.0
var _wind_slow: float = 0.0
var _drone_phase: float = 0.0
var _chirp_timer: float = 0.0


func _ready() -> void:
	_wind = AudioStreamPlayer.new()
	_wind.volume_db = -26.0
	var wg := AudioStreamGenerator.new()
	wg.mix_rate = 24000.0
	wg.buffer_length = 0.2
	_wind.stream = wg
	add_child(_wind)
	_wind.play()
	_wind_playback = _wind.get_stream_playback() as AudioStreamGeneratorPlayback

	_drone = AudioStreamPlayer.new()
	_drone.volume_db = -28.0
	var dg := AudioStreamGenerator.new()
	dg.mix_rate = 24000.0
	dg.buffer_length = 0.2
	_drone.stream = dg
	add_child(_drone)
	_drone.play()
	_drone_playback = _drone.get_stream_playback() as AudioStreamGeneratorPlayback


func _process(delta: float) -> void:
	if _wind_playback == null or _drone_playback == null:
		return

	var day_amt := 0.5
	if _sun:
		day_amt = clampf((-_sun.rotation_degrees.x - 25.0) / 120.0, 0.0, 1.0)

	_fill_wind(_wind_playback, day_amt)
	_fill_drone(_drone_playback, 1.0 - day_amt)

	_wind.volume_db = lerpf(-30.0, -22.0, day_amt)
	_drone.volume_db = lerpf(-16.0, -38.0, day_amt)

	if day_amt > 0.25:
		_chirp_timer -= delta
		if _chirp_timer <= 0.0:
			_chirp_timer = randf_range(4.5, 11.0)
			_play_chirp(day_amt)


## Soft wind: low-pass filtered white noise (brown-ish). The old code used
## fract(jitter) which sounds like harsh digital static.
func _fill_wind(playback: AudioStreamGeneratorPlayback, day: float) -> void:
	var n := playback.get_frames_available()
	var gain := 0.09 * lerpf(0.5, 1.0, day)
	while n > 0:
		var white := randf_range(-1.0, 1.0)
		_wind_smooth = lerpf(_wind_smooth, white, 0.018)
		_wind_slow = lerpf(_wind_slow, sin(Time.get_ticks_usec() * 0.00000035) * 0.4, 0.0008)
		var s := (_wind_smooth * 0.85 + _wind_slow * 0.15) * gain
		playback.push_frame(Vector2(s, s * 0.99))
		n -= 1


func _fill_drone(playback: AudioStreamGeneratorPlayback, night: float) -> void:
	var n := playback.get_frames_available()
	while n > 0:
		_drone_phase += 0.012 * (0.35 + night)
		var s := sin(_drone_phase * 55.0) * 0.04 * night
		playback.push_frame(Vector2(s, s * 1.02))
		n -= 1


func _play_chirp(day: float) -> void:
	var p := AudioStreamPlayer.new()
	var g := AudioStreamGenerator.new()
	g.mix_rate = 24000.0
	g.buffer_length = 0.25
	p.stream = g
	p.volume_db = lerpf(-30.0, -18.0, day)
	add_child(p)
	p.play()
	var pb := p.get_stream_playback() as AudioStreamGeneratorPlayback
	if pb == null:
		p.queue_free()
		return
	var f0 := randf_range(2800.0, 4200.0)
	var f1 := f0 * randf_range(1.15, 1.45)
	var notes := randi_range(2, 4)
	var t := 0.0
	var rate := 1.0 / 24000.0
	for _note in notes:
		var freq := f0 if (_note % 2 == 0) else f1
		var dur := randf_range(0.03, 0.06)
		var note_t := 0.0
		while pb.get_frames_available() > 0 and note_t < dur:
			var env := exp(-note_t * 28.0)
			var s := sin(note_t * TAU * freq) * env * 0.10
			var pan := randf_range(-0.3, 0.3)
			pb.push_frame(Vector2(s * (1.0 - pan), s * (1.0 + pan)))
			note_t += rate
			t += rate
		var gap := randf_range(0.015, 0.035)
		var gap_t := 0.0
		while pb.get_frames_available() > 0 and gap_t < gap:
			pb.push_frame(Vector2.ZERO)
			gap_t += rate
			t += rate
	p.queue_free()
