class_name WorldData
extends RefCounted

## Smooth heightmap terrain data.
## West: rolling meadow. Center: north-south stream. East: rising ridge.

const SIZE := 16
const GRID := 64
const STEP := float(SIZE) / float(GRID)
const WATER_Y := 3.2
const RIVER_HALF := 1.6
const RIVER_BED_Y := 2.2


static func height_smooth(fx: float, fz: float) -> float:
	var meadow := sin(fz * 0.22) * 0.35 + cos(fx * 0.45 + fz * 0.15) * 0.25
	meadow += sin(fx * 0.8 + fz * 0.6) * 0.12
	var base_h := 4.5 + meadow

	var ridge_mask := _smoothstep(6.0, 11.5, fx) * _smoothstep(3.5, 12.0, fz)
	var ridge_shape := 4.2 * ridge_mask
	ridge_shape += 0.5 * ridge_mask * sin(fz * 0.38)
	ridge_shape += 0.4 * ridge_mask * sin(fx * 0.35)
	ridge_shape += 0.2 * ridge_mask * sin(fx * 1.2 + fz * 0.9)
	var h := base_h + ridge_shape

	var rd := river_sdf(fx, fz)
	if rd < RIVER_HALF:
		var bank := _smoothstep(0.0, RIVER_HALF, rd)
		h = lerpf(RIVER_BED_Y, h, bank)

	return clampf(h, 1.0, 14.0)


static func river_sdf(fx: float, _fz: float) -> float:
	var center_x := 5.0 + sin(_fz * 0.35) * 0.6
	return absf(fx - center_x)


static func is_river(fx: float, fz: float) -> bool:
	return river_sdf(fx, fz) < RIVER_HALF * 0.7 and fz > 1.5 and fz < 14.5


static func get_spawn_position() -> Vector3:
	var sx := 3.0
	var sz := 8.0
	var sy := height_smooth(sx, sz)
	return Vector3(sx, sy + 0.85, sz)


static func _smoothstep(edge0: float, edge1: float, x: float) -> float:
	var t := clampf((x - edge0) / (edge1 - edge0), 0.0, 1.0)
	return t * t * (3.0 - 2.0 * t)
