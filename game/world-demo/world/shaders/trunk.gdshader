shader_type spatial;
render_mode cull_back, diffuse_burley, specular_disabled;

uniform vec3 bark_color = vec3(0.3, 0.18, 0.1);
uniform float sway_speed : hint_range(0.0, 4.0) = 1.0;
uniform float sway_strength : hint_range(0.0, 0.1) = 0.008;
uniform float sway_phase_len : hint_range(1.0, 16.0) = 8.0;

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
	return mix(
		mix(mix(a, b, f.x), mix(c, d, f.x), f.y),
		mix(mix(e, f2, f.x), mix(g, h, f.x), f.y),
		f.z
	);
}

void vertex() {
	vec3 world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	float height_factor = clamp((world.y - 0.5) / 2.0, 0.0, 1.0);
	float strength = sway_strength * height_factor;
	float phase = hash31(floor(world * 0.5)) * 6.283;
	VERTEX.x += sin(world.x * sway_phase_len * 1.123 + TIME * sway_speed + phase) * strength;
	VERTEX.z += sin(world.z * sway_phase_len * 0.9123 + TIME * sway_speed * 1.3123 + phase) * strength;
}

void fragment() {
	vec3 world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	float bark_noise = noise3(vec3(UV * 12.0, 0.0));
	float bark_grain = noise3(vec3(UV.x * 4.0, UV.y * 24.0, 3.0));

	vec3 col = bark_color;
	col += (bark_noise - 0.5) * 0.1;
	col *= 0.9 + bark_grain * 0.15;

	float moss = smoothstep(0.52, 0.62, noise3(vec3(UV * 5.0, 7.0)));
	col = mix(col, vec3(0.18, 0.28, 0.1), moss * 0.3);

	ALBEDO = clamp(col, vec3(0.0), vec3(1.0));
	METALLIC = 0.0;
	ROUGHNESS = 0.92;
	AO = 0.8 + bark_noise * 0.2;
}
