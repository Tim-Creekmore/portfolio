extends StaticBody3D

const _TERRAIN_SHADER: Shader = preload("res://world/shaders/terrain_chunk.gdshader")
const _WATER_SHADER: Shader = preload("res://world/shaders/water.gdshader")
const GRID := WorldData.GRID
const STEP := WorldData.STEP
const MAP := WorldData.SIZE
const SKIRT_Y := 0.0

@onready var mesh_instance: MeshInstance3D = $MeshInstance3D
@onready var water_mesh_instance: MeshInstance3D = $WaterMesh
@onready var collision_shape: CollisionShape3D = $CollisionShape3D


func _ready() -> void:
	collision_layer = 1
	collision_mask = 0
	_build_terrain()
	_build_water()


func _build_terrain() -> void:
	var verts := PackedVector3Array()
	var normals := PackedVector3Array()
	var colors := PackedColorArray()
	var indices := PackedInt32Array()
	var cols := GRID + 1
	var rows := GRID + 1

	var heights := PackedFloat32Array()
	heights.resize(cols * rows)
	for iz in rows:
		for ix in cols:
			var fx := float(ix) * STEP
			var fz := float(iz) * STEP
			heights[iz * cols + ix] = WorldData.height_smooth(fx, fz)

	for iz in rows:
		for ix in cols:
			var fx := float(ix) * STEP
			var fz := float(iz) * STEP
			var y := heights[iz * cols + ix]
			verts.append(Vector3(fx, y, fz))

			var eps := STEP
			var hL := WorldData.height_smooth(fx - eps, fz)
			var hR := WorldData.height_smooth(fx + eps, fz)
			var hD := WorldData.height_smooth(fx, fz - eps)
			var hU := WorldData.height_smooth(fx, fz + eps)
			var n := Vector3(hL - hR, 2.0 * eps, hD - hU).normalized()
			normals.append(n)

			var slope := n.y
			var grass := Color(0.28, 0.42, 0.18)
			var dirt := Color(0.42, 0.30, 0.18)
			var rock := Color(0.38, 0.34, 0.30)
			var c: Color
			if slope > 0.85:
				c = grass
			elif slope > 0.6:
				var t := (slope - 0.6) / 0.25
				c = dirt.lerp(grass, t)
			else:
				c = rock.lerp(dirt, clampf((slope - 0.3) / 0.3, 0.0, 1.0))
			colors.append(c)

	for iz in GRID:
		for ix in GRID:
			var tl := iz * cols + ix
			var tr := tl + 1
			var bl := (iz + 1) * cols + ix
			var br := bl + 1
			indices.append(tl)
			indices.append(tr)
			indices.append(bl)
			indices.append(tr)
			indices.append(br)
			indices.append(bl)

	_add_skirt_edge(verts, normals, colors, indices, heights, cols, rows)

	var arr := []
	arr.resize(Mesh.ARRAY_MAX)
	arr[Mesh.ARRAY_VERTEX] = verts
	arr[Mesh.ARRAY_NORMAL] = normals
	arr[Mesh.ARRAY_COLOR] = colors
	arr[Mesh.ARRAY_INDEX] = indices
	var amesh := ArrayMesh.new()
	amesh.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, arr)
	mesh_instance.mesh = amesh

	var smat := ShaderMaterial.new()
	smat.shader = _TERRAIN_SHADER
	mesh_instance.material_override = smat

	_build_collision(heights, cols, rows)


func _build_collision(heights: PackedFloat32Array, cols: int, rows: int) -> void:
	var col_tris := PackedVector3Array()
	for iz in GRID:
		for ix in GRID:
			var fx0 := float(ix) * STEP
			var fx1 := float(ix + 1) * STEP
			var fz0 := float(iz) * STEP
			var fz1 := float(iz + 1) * STEP
			var h00 := heights[iz * cols + ix]
			var h10 := heights[iz * cols + ix + 1]
			var h01 := heights[(iz + 1) * cols + ix]
			var h11 := heights[(iz + 1) * cols + ix + 1]
			var tl := Vector3(fx0, h00, fz0)
			var tr := Vector3(fx1, h10, fz0)
			var bl := Vector3(fx0, h01, fz1)
			var br := Vector3(fx1, h11, fz1)
			col_tris.append(tl)
			col_tris.append(bl)
			col_tris.append(tr)
			col_tris.append(tr)
			col_tris.append(bl)
			col_tris.append(br)
	if col_tris.size() > 0:
		var shape := ConcavePolygonShape3D.new()
		shape.set_backface_collision_enabled(true)
		shape.data = col_tris
		collision_shape.shape = shape


func _add_skirt_edge(
	verts: PackedVector3Array,
	normals: PackedVector3Array,
	colors: PackedColorArray,
	indices: PackedInt32Array,
	heights: PackedFloat32Array,
	cols: int,
	rows: int
) -> void:
	var cliff_color := Color(0.36, 0.30, 0.22)

	for ix in cols:
		var top_idx := ix
		var si := verts.size()
		verts.append(Vector3(float(ix) * STEP, SKIRT_Y, 0.0))
		normals.append(Vector3.BACK)
		colors.append(cliff_color)
		if ix > 0:
			var prev_top := top_idx - 1
			var prev_bot := si - 1
			indices.append_array([prev_top, prev_bot, top_idx, top_idx, prev_bot, si])

	for ix in cols:
		var top_idx := GRID * cols + ix
		var si := verts.size()
		verts.append(Vector3(float(ix) * STEP, SKIRT_Y, float(MAP)))
		normals.append(Vector3.FORWARD)
		colors.append(cliff_color)
		if ix > 0:
			var prev_top := top_idx - 1
			var prev_bot := si - 1
			indices.append_array([top_idx, si, prev_top, prev_top, si, prev_bot])

	for iz in cols:
		var top_idx := iz * cols
		var si := verts.size()
		verts.append(Vector3(0.0, SKIRT_Y, float(iz) * STEP))
		normals.append(Vector3.LEFT)
		colors.append(cliff_color)
		if iz > 0:
			var prev_top := (iz - 1) * cols
			var prev_bot := si - 1
			indices.append_array([top_idx, si, prev_top, prev_top, si, prev_bot])

	for iz in cols:
		var top_idx := iz * cols + GRID
		var si := verts.size()
		verts.append(Vector3(float(MAP), SKIRT_Y, float(iz) * STEP))
		normals.append(Vector3.RIGHT)
		colors.append(cliff_color)
		if iz > 0:
			var prev_top := (iz - 1) * cols + GRID
			var prev_bot := si - 1
			indices.append_array([prev_top, prev_bot, top_idx, top_idx, prev_bot, si])


func _build_water() -> void:
	var st := SurfaceTool.new()
	st.begin(Mesh.PRIMITIVE_TRIANGLES)
	var water_y := WorldData.WATER_Y
	var res := 48
	var step := float(MAP) / float(res)

	for iz in res:
		for ix in res:
			var fx := float(ix) * step
			var fz := float(iz) * step
			var fx1 := fx + step
			var fz1 := fz + step
			var cx := (fx + fx1) * 0.5
			var cz := (fz + fz1) * 0.5
			var terrain_h := WorldData.height_smooth(cx, cz)
			if terrain_h > water_y + 0.05 or cz < 1.0 or cz > 15.0:
				continue
			var c := Color(0.18, 0.52, 0.68, 0.8)
			var n := Vector3.UP
			st.set_normal(n)
			st.set_color(c)
			st.add_vertex(Vector3(fx, water_y, fz))
			st.set_normal(n)
			st.set_color(c)
			st.add_vertex(Vector3(fx1, water_y, fz))
			st.set_normal(n)
			st.set_color(c)
			st.add_vertex(Vector3(fx, water_y, fz1))
			st.set_normal(n)
			st.set_color(c)
			st.add_vertex(Vector3(fx1, water_y, fz))
			st.set_normal(n)
			st.set_color(c)
			st.add_vertex(Vector3(fx1, water_y, fz1))
			st.set_normal(n)
			st.set_color(c)
			st.add_vertex(Vector3(fx, water_y, fz1))

	water_mesh_instance.mesh = st.commit()
	var wmat := ShaderMaterial.new()
	wmat.shader = _WATER_SHADER
	water_mesh_instance.material_override = wmat
