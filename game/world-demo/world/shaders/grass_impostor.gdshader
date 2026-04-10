shader_type spatial;
render_mode cull_back, diffuse_burley, specular_schlick_ggx;

uniform vec3 color_young = vec3(0.20, 0.48, 0.06);
uniform vec3 color_old = vec3(0.42, 0.54, 0.10);
uniform vec3 ground_color = vec3(0.12, 0.10, 0.06);

uniform float patch_scale : hint_range(0.5, 20.0) = 6.0;
uniform float detail_scale : hint_range(0.5, 10.0) = 2.0;

uniform vec2 wind_direction = vec2(1.0, 0.3);
uniform float wind_strength : hint_range(0.0, 0.5) = 0.06;
uniform float wind_speed : hint_range(0.0, 3.0) = 0.8;
uniform float wind_ao_affect : hint_range(0.0, 1.0) = 0.35;

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
	v_world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
	float normal_to_view = 1.0 - dot(VIEW, NORMAL);

	float patch = value_noise(v_world_pos.xz / patch_scale);
	patch = patch * 0.65 + value_noise(v_world_pos.xz / (patch_scale * 0.35) + vec2(7.3, 3.1)) * 0.35;

	patch = mix(patch, 1.0, normal_to_view * 0.4);

	vec3 grass_col = mix(color_young, color_old, patch);
	grass_col = mix(grass_col * 0.7, grass_col * 1.15, patch);

	float high_freq = value_noise(v_world_pos.xz / detail_scale);
	float high_freq2 = value_noise(v_world_pos.xz / (detail_scale * 0.4) + vec2(3.7, 9.1));

	float spottiness = (1.0 - normal_to_view) * 0.5 - patch * 0.2;
	float ground_factor = smoothstep(spottiness + 0.1, spottiness - 0.1, high_freq);

	ALBEDO = mix(grass_col, ground_color, ground_factor);
	ALBEDO = clamp(ALBEDO, vec3(0.0), vec3(1.0));

	vec2 wind_pos = v_world_pos.xz / 10.0;
	wind_pos -= TIME * normalize(wind_direction) * wind_speed;
	float current_wind = wind_fbm(wind_pos) * wind_strength;

	float btt_sim = high_freq + smoothstep(0.6, 1.0, normal_to_view) * 0.4;
	AO = mix(0.5, 1.0, btt_sim) - current_wind * wind_ao_affect;
	AO_LIGHT_AFFECT = 1.0;

	float nx = (high_freq2 - 0.5) * 0.6;
	float nz = (high_freq - 0.5) * 0.6;
	NORMAL = normalize(NORMAL + vec3(nx, 0.0, nz));

	ROUGHNESS = 0.4;
	SPECULAR = 0.12;
	METALLIC = 0.0;
	BACKLIGHT = vec3(0.2, 0.26, 0.06);
}
