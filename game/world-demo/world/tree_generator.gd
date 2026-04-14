class_name TreeGenerator
extends RefCounted

## Low-poly tree mesh builder.
## Generates stylized trees with chunky trunks and faceted canopies.

var _rng: RandomNumberGenerator
var _st_wood: SurfaceTool
var _st_leaf: SurfaceTool

const UP := Vector3.UP


func generate(tree_seed: int) -> Dictionary:
	_rng = RandomNumberGenerator.new()
	_rng.seed = tree_seed

	_st_wood = SurfaceTool.new()
	_st_wood.begin(Mesh.PRIMITIVE_TRIANGLES)
	_st_leaf = SurfaceTool.new()
	_st_leaf.begin(Mesh.PRIMITIVE_TRIANGLES)

	var style := _rng.randi_range(0, 2)
	match style:
		0: _build_pine()
		1: _build_round()
		2: _build_oak()

	_st_wood.generate_normals()
	_st_leaf.generate_normals()

	return {
		"wood": _st_wood.commit(),
		"leaves": _st_leaf.commit(),
	}


func _build_pine() -> void:
	var trunk_h := _rng.randf_range(3.5, 5.0)
	var trunk_r := _rng.randf_range(0.10, 0.15)
	_add_cylinder(_st_wood, Vector3.ZERO, trunk_h, trunk_r, trunk_r * 0.5, 6)

	var layers := _rng.randi_range(4, 6)
	var base_y := trunk_h * 0.25
	var max_r := _rng.randf_range(1.4, 2.0)

	for i in layers:
		var t := float(i) / float(layers)
		var y := base_y + t * (trunk_h - base_y) * 0.95
		var r := max_r * (1.0 - t * 0.55) * _rng.randf_range(0.9, 1.1)
		var h := _rng.randf_range(1.0, 1.6) * (1.0 - t * 0.3)
		var offset := Vector3(
			_rng.randf_range(-0.05, 0.05), 0.0,
			_rng.randf_range(-0.05, 0.05)
		)
		var sides := 7 if i < 2 else 6
		_add_cone(_st_leaf, Vector3(0.0, y, 0.0) + offset, h, r, sides)


func _build_round() -> void:
	var trunk_h := _rng.randf_range(3.0, 4.5)
	var trunk_r := _rng.randf_range(0.08, 0.12)
	_add_cylinder(_st_wood, Vector3.ZERO, trunk_h, trunk_r, trunk_r * 0.6, 6)

	var canopy_r := _rng.randf_range(1.5, 2.2)
	var canopy_center := Vector3(
		_rng.randf_range(-0.1, 0.1),
		trunk_h + canopy_r * 0.15,
		_rng.randf_range(-0.1, 0.1)
	)
	_add_icosphere(_st_leaf, canopy_center, canopy_r, 2)

	if _rng.randf() > 0.3:
		var sub_r := canopy_r * _rng.randf_range(0.45, 0.65)
		var angle := _rng.randf() * TAU
		var sub_offset := Vector3(
			cos(angle) * canopy_r * 0.5,
			_rng.randf_range(-0.4, 0.1),
			sin(angle) * canopy_r * 0.5
		)
		_add_icosphere(_st_leaf, canopy_center + sub_offset, sub_r, 2)


func _build_oak() -> void:
	var trunk_h := _rng.randf_range(2.8, 3.8)
	var trunk_r := _rng.randf_range(0.18, 0.28)
	_add_cylinder(_st_wood, Vector3.ZERO, trunk_h * 0.55, trunk_r, trunk_r * 0.75, 7)

	var fork_y := trunk_h * 0.55
	var branches := _rng.randi_range(2, 4)
	for b in branches:
		var angle := float(b) / float(branches) * TAU + _rng.randf_range(-0.4, 0.4)
		var lean := _rng.randf_range(0.3, 0.55)
		var dir := Vector3(cos(angle) * lean, 1.0, sin(angle) * lean).normalized()
		var branch_len := _rng.randf_range(1.2, 2.0)
		var branch_end := Vector3(0.0, fork_y, 0.0) + dir * branch_len
		var br := trunk_r * _rng.randf_range(0.35, 0.55)
		_add_cylinder(_st_wood, Vector3(0.0, fork_y, 0.0), branch_len, br, br * 0.4, 5, dir)

		var blob_r := _rng.randf_range(1.2, 2.0)
		_add_icosphere(_st_leaf, branch_end + Vector3(0.0, blob_r * 0.3, 0.0), blob_r, 2)

	var top_r := _rng.randf_range(1.4, 2.2)
	_add_icosphere(_st_leaf, Vector3(0.0, trunk_h + top_r * 0.1, 0.0), top_r, 2)


# --- Primitive builders ---

func _add_cylinder(st: SurfaceTool, base: Vector3, height: float,
		r_bottom: float, r_top: float, sides: int, dir := Vector3.UP) -> void:
	var perp := dir.cross(UP)
	if perp.length_squared() < 0.001:
		perp = dir.cross(Vector3.RIGHT)
	perp = perp.normalized()
	var binorm := dir.cross(perp).normalized()

	var rings: Array[PackedVector3Array] = []
	var segs := 4
	for ring_i in (segs + 1):
		var t := float(ring_i) / float(segs)
		var center := base + dir * height * t
		var r := lerpf(r_bottom, r_top, t)
		var ring := PackedVector3Array()
		ring.resize(sides)
		for s in sides:
			var a := float(s) / float(sides) * TAU
			ring[s] = center + (perp * cos(a) + binorm * sin(a)) * r
		rings.append(ring)

	for i in segs:
		for s in sides:
			var s_next := (s + 1) % sides
			st.add_vertex(rings[i][s])
			st.add_vertex(rings[i + 1][s])
			st.add_vertex(rings[i][s_next])
			st.add_vertex(rings[i][s_next])
			st.add_vertex(rings[i + 1][s])
			st.add_vertex(rings[i + 1][s_next])


func _add_cone(st: SurfaceTool, base_center: Vector3, height: float,
		radius: float, sides: int) -> void:
	var tip := base_center + Vector3.UP * height
	for s in sides:
		var a0 := float(s) / float(sides) * TAU
		var a1 := float(s + 1) / float(sides) * TAU
		var v0 := base_center + Vector3(cos(a0) * radius, 0.0, sin(a0) * radius)
		var v1 := base_center + Vector3(cos(a1) * radius, 0.0, sin(a1) * radius)

		st.add_vertex(v0)
		st.add_vertex(v1)
		st.add_vertex(tip)

		var mid := base_center
		st.add_vertex(v1)
		st.add_vertex(v0)
		st.add_vertex(mid)


func _add_icosphere(st: SurfaceTool, center: Vector3, radius: float, subdivisions: int) -> void:
	var t := (1.0 + sqrt(5.0)) / 2.0
	var verts: Array[Vector3] = [
		Vector3(-1.0,  t, 0.0).normalized(),
		Vector3( 1.0,  t, 0.0).normalized(),
		Vector3(-1.0, -t, 0.0).normalized(),
		Vector3( 1.0, -t, 0.0).normalized(),
		Vector3(0.0, -1.0,  t).normalized(),
		Vector3(0.0,  1.0,  t).normalized(),
		Vector3(0.0, -1.0, -t).normalized(),
		Vector3(0.0,  1.0, -t).normalized(),
		Vector3( t, 0.0, -1.0).normalized(),
		Vector3( t, 0.0,  1.0).normalized(),
		Vector3(-t, 0.0, -1.0).normalized(),
		Vector3(-t, 0.0,  1.0).normalized(),
	]

	var tris: Array[PackedInt32Array] = [
		PackedInt32Array([0,11,5]), PackedInt32Array([0,5,1]), PackedInt32Array([0,1,7]),
		PackedInt32Array([0,7,10]), PackedInt32Array([0,10,11]),
		PackedInt32Array([1,5,9]), PackedInt32Array([5,11,4]), PackedInt32Array([11,10,2]),
		PackedInt32Array([10,7,6]), PackedInt32Array([7,1,8]),
		PackedInt32Array([3,9,4]), PackedInt32Array([3,4,2]), PackedInt32Array([3,2,6]),
		PackedInt32Array([3,6,8]), PackedInt32Array([3,8,9]),
		PackedInt32Array([4,9,5]), PackedInt32Array([2,4,11]), PackedInt32Array([6,2,10]),
		PackedInt32Array([8,6,7]), PackedInt32Array([9,8,1]),
	]

	for _sub in subdivisions:
		var new_tris: Array[PackedInt32Array] = []
		var midpoint_cache: Dictionary = {}
		for tri in tris:
			var a := tri[0]
			var b := tri[1]
			var c := tri[2]
			var ab := _get_midpoint(verts, midpoint_cache, a, b)
			var bc := _get_midpoint(verts, midpoint_cache, b, c)
			var ca := _get_midpoint(verts, midpoint_cache, c, a)
			new_tris.append(PackedInt32Array([a, ab, ca]))
			new_tris.append(PackedInt32Array([b, bc, ab]))
			new_tris.append(PackedInt32Array([c, ca, bc]))
			new_tris.append(PackedInt32Array([ab, bc, ca]))
		tris = new_tris

	var displaced: Array[Vector3] = []
	for v in verts:
		var jitter := 1.0 + _rng.randf_range(-0.06, 0.06)
		displaced.append(v * radius * jitter)

	for tri in tris:
		var v0 := center + displaced[tri[0]]
		var v1 := center + displaced[tri[1]]
		var v2 := center + displaced[tri[2]]
		st.add_vertex(v0)
		st.add_vertex(v2)
		st.add_vertex(v1)


func _get_midpoint(verts: Array[Vector3], cache: Dictionary, a: int, b: int) -> int:
	var key := mini(a, b) * 10000 + maxi(a, b)
	if cache.has(key):
		return cache[key]
	var mid := ((verts[a] + verts[b]) * 0.5).normalized()
	verts.append(mid)
	var idx := verts.size() - 1
	cache[key] = idx
	return idx
