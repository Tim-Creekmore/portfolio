extends CanvasLayer


func _ready() -> void:
	add_to_group("dungeon_hint")
	$Panel.visible = false


func show_dungeon_hint() -> void:
	$Panel.visible = true
	$Panel/Margin/Label.text = "Something stirs below..."
	var t := get_tree().create_timer(7.0)
	t.timeout.connect(_hide)


func _hide() -> void:
	$Panel.visible = false
