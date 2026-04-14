shader_type spatial;
render_mode diffuse_burley, specular_schlick_ggx;

uniform vec3 leaf_color = vec3(0.24, 0.42, 0.16);
uniform float wind_strength : hint_range(0.0, 1.0) = 0.3;
uniform float wind_speed : hint_range(0.0, 4.0) = 1.2;
uniform float color_variation : hint_range(0.0, 0.3) = 0.12;

varying vec3 v_wp;

float hash31(vec3 p) {
	return fract(sin(dot(p, vec3(127.1, 311.7, 74.7))) * 43758.5453);
}

void vertex() {
	vec3 world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	v_wp = world;

	float height_factor = clamp((world.y - 1.0) / 5.0, 0.0, 1.0);
	float phase = hash31(floor(world * 0.5)) * 6.283;

	float sway = sin(TIME * wind_speed + world.x * 0.3 + phase) * wind_strength * 0.15 * height_factor;
	float sway2 = sin(TIME * wind_speed * 0.7 + world.z * 0.4 + phase * 1.3) * wind_strength * 0.08 * height_factor;
	VERTEX.x += sway;
	VERTEX.z += sway2;
}

void fragment() {
	// Per-face color variation for low-poly look
	vec3 face_id = floor(v_wp * 3.0);
	float variation = (hash31(face_id) - 0.5) * color_variation * 2.0;

	vec3 dark = leaf_color * 0.65;
	vec3 light = leaf_color * 1.3 + vec3(0.04, 0.06, 0.0);

	// Blend based on normal direction for natural shading
	float top_facing = clamp(dot(NORMAL, vec3(0.0, 1.0, 0.0)), 0.0, 1.0);
	vec3 col = mix(dark, light, top_facing * 0.7 + 0.3);
	col += variation;

	ALBEDO = clamp(col, vec3(0.0), vec3(1.0));
	ROUGHNESS = 0.75;
	SPECULAR = 0.1;
	METALLIC = 0.0;
	BACKLIGHT = leaf_color * 0.25;
}
