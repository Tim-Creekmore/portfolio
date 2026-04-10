extends Node3D

## Invisible collision so the 16×16 voxel slice has edges (no walking into the void).

const EDGE := 16.0
const HEIGHT := 20.0
const THICK := 0.75


func _ready() -> void:
	_add_wall("West", Vector3(THICK, HEIGHT, EDGE + THICK * 2.0), Vector3(-THICK * 0.5, HEIGHT * 0.5, EDGE * 0.5))
	_add_wall("East", Vector3(THICK, HEIGHT, EDGE + THICK * 2.0), Vector3(EDGE + THICK * 0.5, HEIGHT * 0.5, EDGE * 0.5))
	_add_wall("North", Vector3(EDGE + THICK * 4.0, HEIGHT, THICK), Vector3(EDGE * 0.5, HEIGHT * 0.5, -THICK * 0.5))
	_add_wall("South", Vector3(EDGE + THICK * 4.0, HEIGHT, THICK), Vector3(EDGE * 0.5, HEIGHT * 0.5, EDGE + THICK * 0.5))


func _add_wall(wall_name: String, box_size: Vector3, center: Vector3) -> void:
	var body := StaticBody3D.new()
	body.name = wall_name
	body.collision_layer = 1
	body.collision_mask = 0
	var shape := CollisionShape3D.new()
	var box := BoxShape3D.new()
	box.size = box_size
	shape.shape = box
	body.add_child(shape)
	body.position = center
	add_child(body)
