shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_burley, specular_schlick_ggx;

uniform vec3 leaf_color = vec3(0.24, 0.42, 0.16);
uniform float wind_strength : hint_range(0.0, 1.0) = 0.3;
uniform float wind_speed : hint_range(0.0, 4.0) = 1.2;
uniform float leaf_detail : hint_range(0.0, 1.0) = 0.6;

varying vec3 v_wp;
varying vec3 v_nrm;

float hash31(vec3 p) {
	return fract(sin(dot(p, vec3(127.1, 311.7, 74.7))) * 43758.5453);
}

float noise3(vec3 p) {
	vec3 i = floor(p);
	vec3 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);
	float a = hash31(i);
	float b = hash31(i + vec3(1.0, 0.0, 0.0));
	float c = hash31(i + vec3(0.0, 1.0, 0.0));
	float d = hash31(i + vec3(1.0, 1.0, 0.0));
	float e = hash31(i + vec3(0.0, 0.0, 1.0));
	float f2 = hash31(i + vec3(1.0, 0.0, 1.0));
	float g = hash31(i + vec3(0.0, 1.0, 1.0));
	float h = hash31(i + vec3(1.0, 1.0, 1.0));
	float x0 = mix(a, b, f.x);
	float x1 = mix(c, d, f.x);
	float x2 = mix(e, f2, f.x);
	float x3 = mix(g, h, f.x);
	float y0 = mix(x0, x1, f.y);
	float y1 = mix(x2, x3, f.y);
	return mix(y0, y1, f.z);
}

void vertex() {
	vec3 world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	v_wp = world;
	v_nrm = normalize((MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz);

	float bump = noise3(VERTEX * 3.5) * 0.15 + noise3(VERTEX * 7.0) * 0.08;
	VERTEX += NORMAL * bump;

	float height_factor = clamp((world.y - 1.0) / 4.0, 0.0, 1.0);
	float phase = hash31(floor(world * 0.5)) * 6.283;
	float sway = sin(TIME * wind_speed + world.x * 0.4 + phase) * wind_strength * height_factor;
	float sway2 = sin(TIME * wind_speed * 0.7 + world.z * 0.5 + phase * 1.3) * wind_strength * 0.5 * height_factor;
	VERTEX.x += sway;
	VERTEX.z += sway2;

	float leaf_flutter = sin(TIME * 4.0 + VERTEX.x * 8.0 + VERTEX.z * 6.0) * 0.012 * height_factor;
	VERTEX += NORMAL * leaf_flutter;
}

void fragment() {
	float n1 = noise3(v_wp * 8.0);
	float n2 = noise3(v_wp * 16.0 + vec3(5.0, 3.0, 7.0));
	float n3 = noise3(v_wp * 32.0 + vec3(13.0, 17.0, 11.0));

	float leaf_mask = smoothstep(0.35, 0.55, n1);
	float leaf_edge = smoothstep(0.3, 0.5, n2);

	vec3 base = leaf_color;
	vec3 dark = leaf_color * 0.55;
	vec3 light = leaf_color * 1.4 + vec3(0.06, 0.10, 0.0);

	vec3 col = mix(dark, base, leaf_mask);
	col = mix(col, light, leaf_edge * 0.45);
	col += (n3 - 0.5) * 0.08;

	float up_dot = dot(v_nrm, vec3(0.0, 1.0, 0.0)) * 0.5 + 0.5;
	float ssao = mix(0.55, 1.0, up_dot);
	col *= ssao;

	ALBEDO = clamp(col, vec3(0.0), vec3(1.0));
	ROUGHNESS = 0.7 + n1 * 0.15;
	SPECULAR = 0.18;
	METALLIC = 0.0;
	BACKLIGHT = leaf_color * 0.35;

	vec3 leaf_normal = NORMAL;
	leaf_normal.x += (n2 - 0.5) * leaf_detail * 0.6;
	leaf_normal.z += (n3 - 0.5) * leaf_detail * 0.6;
	NORMAL = normalize(leaf_normal);
}
