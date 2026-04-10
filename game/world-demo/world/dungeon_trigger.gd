extends Area3D

var _triggered: bool = false


func _ready() -> void:
	collision_mask = 2
	monitoring = true
	body_entered.connect(_on_body_entered)


func _on_body_entered(body: Node3D) -> void:
	if _triggered:
		return
	if body is CharacterBody3D:
		_triggered = true
		get_tree().call_group("dungeon_hint", "show_dungeon_hint")
