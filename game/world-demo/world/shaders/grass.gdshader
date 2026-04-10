shader_type spatial;
render_mode cull_disabled, diffuse_burley, specular_schlick_ggx;

uniform float size_small : hint_range(0.01, 1.0) = 0.4;
uniform float size_large : hint_range(0.1, 2.0) = 0.75;
uniform float blade_bend : hint_range(0.0, 2.0) = 0.2;

uniform float patch_scale : hint_range(0.5, 20.0) = 6.0;

uniform vec2 wind_direction = vec2(1.0, 0.3);
uniform float wind_strength : hint_range(0.0, 0.5) = 0.06;
uniform float wind_speed : hint_range(0.0, 3.0) = 0.8;
uniform float wind_bend_strength : hint_range(0.0, 5.0) = 1.0;
uniform float wind_ao_affect : hint_range(0.0, 1.0) = 0.35;

uniform vec3 color_young = vec3(0.20, 0.48, 0.06);
uniform vec3 color_old = vec3(0.42, 0.54, 0.10);

varying float v_btt;
varying float v_patch;
varying float v_wind_bend;

float hash21(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

float value_noise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);
	float a = hash21(i);
	float b = hash21(i + vec2(1.0, 0.0));
	float c = hash21(i + vec2(0.0, 1.0));
	float d = hash21(i + vec2(1.0, 1.0));
	return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

float ridged_noise(vec2 p) {
	float v = value_noise(p);
	return 1.0 - abs(v * 2.0 - 1.0);
}

float wind_fbm(vec2 p) {
	float amp = 0.5;
	float sum = 0.0;
	float gain = 0.45;
	for (int i = 0; i < 4; i++) {
		sum += amp * ridged_noise(p);
		p *= 2.1;
		amp *= gain;
	}
	return sum;
}

void vertex() {
	float btt = 1.0 - UV.y;
	v_btt = btt;

	vec2 world_xz = NODE_POSITION_WORLD.xz;

	float patch = value_noise(world_xz / patch_scale);
	patch = patch * 0.65 + value_noise(world_xz / (patch_scale * 0.35) + vec2(7.3, 3.1)) * 0.35;
	v_patch = patch;

	float sz = mix(size_small, size_large, patch);
	VERTEX *= sz;

	float bend_amount = blade_bend * (0.5 + patch * 0.5);
	VERTEX.z += bend_amount * pow(btt, 2.0) * sz;

	vec2 wind_pos = world_xz / 10.0;
	wind_pos -= TIME * normalize(wind_direction) * wind_speed;
	float wind_noise = wind_fbm(wind_pos);
	float current_wind = wind_noise * wind_strength * btt * 2.0;
	v_wind_bend = current_wind;

	mat4 inv_model = inverse(MODEL_MATRIX);
	vec2 local_dir = (inv_model * vec4(wind_direction.x, 0.0, wind_direction.y, 0.0)).xz;

	VERTEX.xz += current_wind * local_dir * wind_bend_strength;
	VERTEX.y -= current_wind * 0.4 * sz;
}

void fragment() {
	if (!FRONT_FACING) {
		NORMAL = -NORMAL;
	}

	NORMAL = normalize(mix(NORMAL, vec3(0.0, 1.0, 0.0), v_btt));

	vec3 col_base = mix(color_young, color_old, v_patch);
	vec3 col = mix(col_base * 0.65, col_base * 1.25, v_btt);

	ALBEDO = clamp(col, vec3(0.0), vec3(1.0));

	AO = v_btt - v_wind_bend * wind_ao_affect;
	AO_LIGHT_AFFECT = 1.0;

	ROUGHNESS = 0.4;
	SPECULAR = 0.2;
	METALLIC = 0.0;
	BACKLIGHT = vec3(0.22, 0.28, 0.08);
}
