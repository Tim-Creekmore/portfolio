shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_disabled, diffuse_burley, specular_schlick_ggx;

uniform vec3 shallow_color = vec3(0.18, 0.45, 0.52);
uniform vec3 deep_color = vec3(0.06, 0.18, 0.28);
uniform float alpha_base : hint_range(0.0, 1.0) = 0.72;

uniform float wave_speed : hint_range(0.0, 4.0) = 0.6;
uniform float wave_scale : hint_range(0.5, 20.0) = 4.0;
uniform float wave_height : hint_range(0.0, 0.3) = 0.04;
uniform float ripple_strength : hint_range(0.0, 1.0) = 0.35;

varying vec3 v_world_pos;

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

float water_waves(vec2 p, float t) {
	float w = 0.0;
	w += sin(p.x * 1.2 + p.y * 0.8 + t * 1.1) * 0.4;
	w += sin(p.x * 0.7 - p.y * 1.5 + t * 0.9) * 0.3;
	w += sin(p.x * 2.1 + p.y * 1.7 + t * 1.6) * 0.15;
	w += value_noise(p * 2.0 + vec2(t * 0.3, t * 0.2)) * 0.15;
	return w;
}

void vertex() {
	v_world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;

	vec2 wp = v_world_pos.xz * wave_scale;
	float t = TIME * wave_speed;
	float h = water_waves(wp, t);
	VERTEX.y += h * wave_height;
}

void fragment() {
	vec2 wp = v_world_pos.xz * wave_scale;
	float t = TIME * wave_speed;

	float eps = 0.1;
	float h_c = water_waves(wp, t);
	float h_r = water_waves(wp + vec2(eps, 0.0), t);
	float h_u = water_waves(wp + vec2(0.0, eps), t);
	vec3 ripple_normal = normalize(vec3(
		(h_c - h_r) * ripple_strength,
		1.0,
		(h_c - h_u) * ripple_strength
	));

	NORMAL = normalize(mix(NORMAL, ripple_normal, 0.7));

	float fresnel = pow(1.0 - abs(dot(VIEW, NORMAL)), 3.0);
	vec3 col = mix(deep_color, shallow_color, fresnel * 0.6 + 0.2);

	float foam_noise = value_noise(v_world_pos.xz * 8.0 + vec2(TIME * 0.15));
	float foam = smoothstep(0.72, 0.82, foam_noise) * 0.15;
	col += vec3(foam);

	ALBEDO = clamp(col, vec3(0.0), vec3(1.0));
	ALPHA = clamp(alpha_base + fresnel * 0.2, 0.0, 0.92);
	ROUGHNESS = 0.05;
	SPECULAR = 0.7;
	METALLIC = 0.15;
	EMISSION = deep_color * 0.08;
}
