extends CharacterBody3D

const SPEED := 4.2
const SWIM_SPEED := 2.4
const JUMP_VELOCITY := 5.5
const MOUSE_SENS := 0.0022
const SWIM_UP_FORCE := 4.5
const SINK_SPEED := 1.8
const BUOYANCY := 3.0
const WATER_DRAG := 4.0

@onready var neck: Node3D = $Neck
@onready var cam_fp: Camera3D = $Neck/CameraFP
@onready var spring: SpringArm3D = $Neck/SpringArm3D
@onready var cam_tp: Camera3D = $Neck/SpringArm3D/CameraTP
@onready var body_visual: MeshInstance3D = $BodyVisual

var _third_person: bool = false
var _bob_time: float = 0.0
var _pitch: float = deg_to_rad(-7.0)
var _space_was_down: bool = false
var _in_water: bool = false


func _ready() -> void:
	collision_layer = 2
	collision_mask = 1
	if body_visual:
		body_visual.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_ON
	_apply_camera_mode()


func _physics_process(delta: float) -> void:
	var gravity: float = float(ProjectSettings.get_setting("physics/3d/default_gravity"))
	var water_y: float = WorldData.WATER_Y
	var feet_y := global_position.y - 0.8
	_in_water = feet_y < water_y and WorldData.is_river(global_position.x, global_position.z)
	var submerge_depth := water_y - feet_y

	if _in_water:
		_process_swimming(delta, gravity, submerge_depth)
	else:
		_process_ground(delta, gravity)

	var g := _get_move_input()
	var dir := (transform.basis * Vector3(g.x, 0.0, -g.y)).normalized()
	var move_speed := SWIM_SPEED if _in_water else SPEED

	if g.length_squared() > 0.0001:
		velocity.x = dir.x * move_speed
		velocity.z = dir.z * move_speed
	else:
		var decel := WATER_DRAG if _in_water else 12.0
		velocity.x = move_toward(velocity.x, 0.0, move_speed * delta * decel)
		velocity.z = move_toward(velocity.z, 0.0, move_speed * delta * decel)

	move_and_slide()
	_update_head_bob(delta, g)


func _process_ground(delta: float, gravity: float) -> void:
	if not is_on_floor():
		velocity.y -= gravity * delta

	var space_down := Input.is_physical_key_pressed(KEY_SPACE)
	if is_on_floor() and space_down and not _space_was_down:
		velocity.y = JUMP_VELOCITY
	_space_was_down = space_down


func _process_swimming(delta: float, gravity: float, submerge_depth: float) -> void:
	var space_down := Input.is_physical_key_pressed(KEY_SPACE)
	var shift_down := Input.is_physical_key_pressed(KEY_SHIFT)
	_space_was_down = space_down

	var buoyancy_force := clampf(submerge_depth / 1.5, 0.0, 1.0) * BUOYANCY
	velocity.y -= gravity * delta
	velocity.y += buoyancy_force * delta * 10.0

	if space_down:
		velocity.y = move_toward(velocity.y, SWIM_UP_FORCE, SWIM_UP_FORCE * delta * 6.0)
	elif shift_down:
		velocity.y = move_toward(velocity.y, -SINK_SPEED, SINK_SPEED * delta * 6.0)

	velocity.y *= (1.0 - WATER_DRAG * delta)


func _get_move_input() -> Vector2:
	var g := Vector2.ZERO
	if Input.is_physical_key_pressed(KEY_A):
		g.x -= 1.0
	if Input.is_physical_key_pressed(KEY_D):
		g.x += 1.0
	if Input.is_physical_key_pressed(KEY_W):
		g.y += 1.0
	if Input.is_physical_key_pressed(KEY_S):
		g.y -= 1.0
	return g.limit_length(1.0)


func _update_head_bob(delta: float, g: Vector2) -> void:
	if _third_person:
		cam_fp.position = Vector3.ZERO
		return
	if _in_water:
		_bob_time += delta * 2.5
		var bob := sin(_bob_time) * 0.06
		cam_fp.position.y = bob
		cam_fp.position.x = cos(_bob_time * 0.7) * 0.03
		return
	var spd := Vector2(velocity.x, velocity.z).length()
	if spd > 0.35 and is_on_floor():
		_bob_time += delta * 9.0
		var bob := sin(_bob_time) * 0.038
		cam_fp.position.y = bob
		cam_fp.position.x = cos(_bob_time * 0.5) * 0.018
	elif g.length_squared() < 0.01:
		_bob_time = 0.0
		cam_fp.position = cam_fp.position.lerp(Vector3.ZERO, delta * 10.0)


func _input(event: InputEvent) -> void:
	if event is InputEventKey and event.pressed and not event.echo:
		match event.physical_keycode:
			KEY_V:
				_third_person = not _third_person
				_apply_camera_mode()
			KEY_ESCAPE:
				Input.mouse_mode = Input.MOUSE_MODE_VISIBLE

	if event is InputEventMouseMotion and Input.mouse_mode == Input.MOUSE_MODE_CAPTURED:
		rotate_y(-event.relative.x * MOUSE_SENS)
		_pitch -= event.relative.y * MOUSE_SENS
		_pitch = clampf(_pitch, deg_to_rad(-88.0), deg_to_rad(88.0))
		if _third_person:
			spring.rotation.x = _pitch
		else:
			cam_fp.rotation.x = _pitch

	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		if Input.mouse_mode != Input.MOUSE_MODE_CAPTURED:
			Input.mouse_mode = Input.MOUSE_MODE_CAPTURED


func _apply_camera_mode() -> void:
	if body_visual:
		body_visual.visible = _third_person
	cam_fp.current = not _third_person
	cam_tp.current = _third_person
	if _third_person:
		spring.rotation.x = _pitch
		cam_fp.rotation.x = 0.0
	else:
		cam_fp.rotation.x = _pitch
		spring.rotation.x = 0.0
