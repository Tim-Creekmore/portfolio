extends Node3D

const TreeScene := preload("res://world/pine_tree.tscn")
const GrassShader := preload("res://world/shaders/grass.gdshader")
const CanopyShader := preload("res://world/shaders/canopy.gdshader")
const TrunkShader := preload("res://world/shaders/trunk.gdshader")
const ImpostorShader := preload("res://world/shaders/grass_impostor.gdshader")
const FlowerShader := preload("res://world/shaders/wildflower.gdshader")

const FLOWER_COLORS: Array[Color] = [
	Color(0.95, 0.90, 0.30),
	Color(1.00, 1.00, 0.85),
	Color(0.65, 0.40, 0.80),
	Color(0.90, 0.35, 0.40),
	Color(0.95, 0.65, 0.20),
	Color(0.50, 0.60, 0.90),
]

const TREE_COLORS: Array[Color] = [
	Color(0.22, 0.40, 0.14),
	Color(0.26, 0.44, 0.18),
	Color(0.30, 0.48, 0.16),
	Color(0.34, 0.42, 0.12),
	Color(0.50, 0.52, 0.10),
	Color(0.58, 0.44, 0.08),
	Color(0.62, 0.30, 0.08),
	Color(0.55, 0.22, 0.06),
]


func _ready() -> void:
	call_deferred("_place_all")


func _place_all() -> void:
	_place_grass_impostor()
	_place_trees()
	_place_rocks()
	_place_grass()
	_place_wildflowers()


func _place_grass_impostor() -> void:
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	var res := 48
	var cell := float(WorldData.SIZE) / float(res)
	for gz in res:
		for gx in res:
			var x0 := float(gx) * cell
			var z0 := float(gz) * cell
			var x1 := x0 + cell
			var z1 := z0 + cell
			var mx := (x0 + x1) * 0.5
			var mz := (z0 + z1) * 0.5
			if WorldData.is_river(mx, mz):
				continue
			if WorldData.river_sdf(mx, mz) < 0.5:
				continue
			var y00 := WorldData.height_smooth(x0, z0) + 0.01
			var y10 := WorldData.height_smooth(x1, z0) + 0.01
			var y01 := WorldData.height_smooth(x0, z1) + 0.01
			var y11 := WorldData.height_smooth(x1, z1) + 0.01
			var v00 := Vector3(x0, y00, z0)
			var v10 := Vector3(x1, y10, z0)
			var v01 := Vector3(x0, y01, z1)
			var v11 := Vector3(x1, y11, z1)
			st.set_uv(Vector2(x0 / float(WorldData.SIZE), z0 / float(WorldData.SIZE)))
			st.add_vertex(v00)
			st.set_uv(Vector2(x1 / float(WorldData.SIZE), z0 / float(WorldData.SIZE)))
			st.add_vertex(v10)
			st.set_uv(Vector2(x0 / float(WorldData.SIZE), z1 / float(WorldData.SIZE)))
			st.add_vertex(v01)
			st.set_uv(Vector2(x1 / float(WorldData.SIZE), z0 / float(WorldData.SIZE)))
			st.add_vertex(v10)
			st.set_uv(Vector2(x1 / float(WorldData.SIZE), z1 / float(WorldData.SIZE)))
			st.add_vertex(v11)
			st.set_uv(Vector2(x0 / float(WorldData.SIZE), z1 / float(WorldData.SIZE)))
			st.add_vertex(v01)
	st.generate_normals()
	var mesh := st.commit()
	var mat := ShaderMaterial.new()
	mat.shader = ImpostorShader
	var mi := MeshInstance3D.new()
	mi.mesh = mesh
	mi.material_override = mat
	mi.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	add_child(mi)


func _place_trees() -> void:
	var rng := RandomNumberGenerator.new()
	rng.seed = 0x54524545

	for _i in 22:
		var fx := rng.randf_range(0.8, 15.2)
		var fz := rng.randf_range(0.8, 15.2)
		if WorldData.is_river(fx, fz):
			continue
		if WorldData.river_sdf(fx, fz) < 1.2:
			continue
		var y := WorldData.height_smooth(fx, fz)

		var t := TreeScene.instantiate()
		var s := rng.randf_range(0.6, 1.15)
		t.scale = Vector3(s, s * rng.randf_range(0.85, 1.15), s)
		t.rotation.y = rng.randf() * TAU
		t.position = Vector3(fx, y, fz)

		var leaf_color: Color = TREE_COLORS[rng.randi() % TREE_COLORS.size()]
		leaf_color = leaf_color.lightened(rng.randf_range(-0.06, 0.06))

		var canopy_mat := ShaderMaterial.new()
		canopy_mat.shader = CanopyShader
		canopy_mat.set_shader_parameter("leaf_color", Vector3(leaf_color.r, leaf_color.g, leaf_color.b))
		canopy_mat.set_shader_parameter("wind_strength", rng.randf_range(0.2, 0.4))
		canopy_mat.set_shader_parameter("wind_speed", rng.randf_range(0.8, 1.4))

		var trunk_mat := ShaderMaterial.new()
		trunk_mat.shader = TrunkShader
		trunk_mat.set_shader_parameter("sway_speed", canopy_mat.get_shader_parameter("wind_speed"))
		trunk_mat.set_shader_parameter("sway_strength", 0.006)

		for child in t.get_children():
			if child is MeshInstance3D:
				if child.name.begins_with("Canopy"):
					child.material_override = canopy_mat
				elif child.name == "Trunk":
					child.material_override = trunk_mat

		add_child(t)


func _place_rocks() -> void:
	var rng := RandomNumberGenerator.new()
	rng.seed = 0x524f434b
	var mesh := SphereMesh.new()
	mesh.radius = 0.3
	mesh.height = 0.45
	mesh.radial_segments = 8
	mesh.rings = 4
	var mat := StandardMaterial3D.new()
	mat.diffuse_mode = BaseMaterial3D.DIFFUSE_BURLEY
	mat.specular_mode = BaseMaterial3D.SPECULAR_DISABLED
	mat.albedo_color = Color(0.38, 0.34, 0.30, 1)
	mat.roughness = 0.96
	var mm := MultiMesh.new()
	mm.transform_format = MultiMesh.TRANSFORM_3D
	mm.mesh = mesh
	var xforms: Array = []
	for _i in 30:
		var fx := rng.randf_range(0.5, 15.5)
		var fz := rng.randf_range(0.5, 15.5)
		if WorldData.is_river(fx, fz):
			continue
		var y := WorldData.height_smooth(fx, fz)
		var xf := Transform3D(
			Basis.from_euler(Vector3(
				rng.randf_range(-0.3, 0.3),
				rng.randf() * TAU,
				rng.randf_range(-0.3, 0.3)
			)).scaled(Vector3(
				rng.randf_range(0.5, 1.4),
				rng.randf_range(0.4, 0.9),
				rng.randf_range(0.5, 1.3)
			)),
			Vector3(fx, y + 0.05, fz)
		)
		xforms.append(xf)
	if xforms.is_empty():
		return
	mm.instance_count = xforms.size()
	for i in xforms.size():
		mm.set_instance_transform(i, xforms[i])
	var mmi := MultiMeshInstance3D.new()
	mmi.multimesh = mm
	mmi.material_override = mat
	add_child(mmi)


func _build_grass_blade() -> ArrayMesh:
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)

	var segs := 5
	var blade_w := 0.055
	var blade_h := 1.0

	for s in segs:
		var t0 := float(s) / float(segs)
		var t1 := float(s + 1) / float(segs)
		var y0 := t0 * blade_h
		var y1 := t1 * blade_h
		var w0 := blade_w * (1.0 - t0 * 0.85)
		var w1 := blade_w * (1.0 - t1 * 0.85)
		var uv_v0 := 1.0 - t0
		var uv_v1 := 1.0 - t1

		var l0 := Vector3(-w0, y0, 0.0)
		var r0 := Vector3(w0, y0, 0.0)
		var l1 := Vector3(-w1, y1, 0.0)
		var r1 := Vector3(w1, y1, 0.0)

		var nl := Vector3(-0.4, 0.0, 1.0).normalized()
		var nr := Vector3(0.4, 0.0, 1.0).normalized()

		st.set_normal(nl)
		st.set_uv(Vector2(0.0, uv_v0))
		st.add_vertex(l0)
		st.set_normal(nr)
		st.set_uv(Vector2(1.0, uv_v0))
		st.add_vertex(r0)
		st.set_normal(nl)
		st.set_uv(Vector2(0.0, uv_v1))
		st.add_vertex(l1)

		st.set_normal(nr)
		st.set_uv(Vector2(1.0, uv_v0))
		st.add_vertex(r0)
		st.set_normal(nr)
		st.set_uv(Vector2(1.0, uv_v1))
		st.add_vertex(r1)
		st.set_normal(nl)
		st.set_uv(Vector2(0.0, uv_v1))
		st.add_vertex(l1)

	return st.commit()


func _place_grass() -> void:
	var rng := RandomNumberGenerator.new()
	rng.seed = 0x47524153
	var blade_mesh := _build_grass_blade()

	var grass_mat := ShaderMaterial.new()
	grass_mat.shader = GrassShader

	var mm := MultiMesh.new()
	mm.transform_format = MultiMesh.TRANSFORM_3D
	mm.mesh = blade_mesh

	var xforms: Array = []
	for _i in 50000:
		var fx := rng.randf_range(0.2, 15.8)
		var fz := rng.randf_range(0.2, 15.8)
		if WorldData.is_river(fx, fz):
			continue
		if WorldData.river_sdf(fx, fz) < 0.4:
			continue
		var y := WorldData.height_smooth(fx, fz)
		var xf := Transform3D(
			Basis.from_euler(Vector3(
				rng.randf_range(-0.1, 0.1),
				rng.randf() * TAU,
				rng.randf_range(-0.1, 0.1)
			)),
			Vector3(fx, y, fz)
		)
		xforms.append(xf)

	if xforms.is_empty():
		return
	mm.instance_count = xforms.size()
	for i in xforms.size():
		mm.set_instance_transform(i, xforms[i])

	var mmi := MultiMeshInstance3D.new()
	mmi.multimesh = mm
	mmi.material_override = grass_mat
	mmi.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	add_child(mmi)


func _build_flower_mesh() -> ArrayMesh:
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)

	var petals := 5
	var petal_w := 0.035
	var petal_h := 0.06
	var center_r := 0.015

	for p in petals:
		var angle := float(p) / float(petals) * TAU
		var ca := cos(angle)
		var sa := sin(angle)
		var base := Vector3(ca * center_r, 0.0, sa * center_r)
		var tip := Vector3(ca * (center_r + petal_h), 0.0, sa * (center_r + petal_h))
		var perp := Vector3(-sa, 0.0, ca) * petal_w * 0.5
		var left := base + perp
		var right := base - perp

		st.set_normal(Vector3.UP)
		st.set_uv(Vector2(0.0, 1.0))
		st.add_vertex(left)
		st.set_normal(Vector3.UP)
		st.set_uv(Vector2(1.0, 1.0))
		st.add_vertex(right)
		st.set_normal(Vector3.UP)
		st.set_uv(Vector2(0.5, 0.0))
		st.add_vertex(tip)

	return st.commit()


func _place_wildflowers() -> void:
	var rng := RandomNumberGenerator.new()
	rng.seed = 0x464C5752
	var flower_mesh := _build_flower_mesh()

	var flower_mat := ShaderMaterial.new()
	flower_mat.shader = FlowerShader

	var mm := MultiMesh.new()
	mm.transform_format = MultiMesh.TRANSFORM_3D
	mm.use_colors = true
	mm.mesh = flower_mesh

	var xforms: Array[Transform3D] = []
	var colors: Array[Color] = []

	for _i in 3000:
		var fx := rng.randf_range(0.5, 15.5)
		var fz := rng.randf_range(0.5, 15.5)
		if WorldData.is_river(fx, fz):
			continue
		if WorldData.river_sdf(fx, fz) < 0.8:
			continue
		var y := WorldData.height_smooth(fx, fz)
		var scale := rng.randf_range(0.7, 1.3)
		var xf := Transform3D(
			Basis.from_euler(Vector3(
				rng.randf_range(-0.15, 0.15),
				rng.randf() * TAU,
				rng.randf_range(-0.15, 0.15)
			)).scaled(Vector3(scale, scale, scale)),
			Vector3(fx, y + 0.15 + rng.randf_range(0.0, 0.1), fz)
		)
		xforms.append(xf)
		var col: Color = FLOWER_COLORS[rng.randi() % FLOWER_COLORS.size()]
		col = col.lightened(rng.randf_range(-0.08, 0.08))
		colors.append(col)

	if xforms.is_empty():
		return
	mm.instance_count = xforms.size()
	for i in xforms.size():
		mm.set_instance_transform(i, xforms[i])
		mm.set_instance_color(i, colors[i])

	var mmi := MultiMeshInstance3D.new()
	mmi.multimesh = mm
	mmi.material_override = flower_mat
	mmi.cast_shadow = GeometryInstance3D.SHADOW_CASTING_SETTING_OFF
	add_child(mmi)
