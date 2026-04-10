shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_burley, specular_schlick_ggx;

uniform float roughness_base : hint_range(0.0, 1.0) = 0.88;

varying vec3 v_world_pos;
varying vec3 v_world_nrm;

float hash31(vec3 p) {
	return fract(sin(dot(p, vec3(127.1, 311.7, 74.7))) * 43758.5453);
}

float noise3(vec3 p) {
	vec3 i = floor(p);
	vec3 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);
	float n000 = hash31(i);
	float n100 = hash31(i + vec3(1.0, 0.0, 0.0));
	float n010 = hash31(i + vec3(0.0, 1.0, 0.0));
	float n110 = hash31(i + vec3(1.0, 1.0, 0.0));
	float n001 = hash31(i + vec3(0.0, 0.0, 1.0));
	float n101 = hash31(i + vec3(1.0, 0.0, 1.0));
	float n011 = hash31(i + vec3(0.0, 1.0, 1.0));
	float n111 = hash31(i + vec3(1.0, 1.0, 1.0));
	float nx00 = mix(n000, n100, f.x);
	float nx10 = mix(n010, n110, f.x);
	float nx01 = mix(n001, n101, f.x);
	float nx11 = mix(n011, n111, f.x);
	float nxy0 = mix(nx00, nx10, f.y);
	float nxy1 = mix(nx01, nx11, f.y);
	return mix(nxy0, nxy1, f.z);
}

float fbm(vec3 p) {
	float a = 0.5;
	float s = 0.0;
	vec3 q = p;
	for (int i = 0; i < 4; i++) {
		s += a * noise3(q);
		q *= 2.07;
		a *= 0.48;
	}
	return s;
}

void vertex() {
	v_world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	v_world_nrm = normalize((MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz);
}

void fragment() {
	vec3 tint = COLOR.rgb;

	float n1 = fbm(v_world_pos * 2.5);
	float n2 = fbm(v_world_pos * 6.0 + vec3(13.0, 5.0, 21.0));
	float n3 = fbm(v_world_pos * 12.0 + vec3(7.0, 19.0, 3.0));

	vec3 albedo = tint;
	albedo *= 0.88 + n1 * 0.18;
	albedo *= 0.95 + n2 * 0.08;
	albedo += vec3(0.01, 0.015, 0.0) * n3;

	float slope = v_world_nrm.y;
	float path_noise = fbm(v_world_pos * vec3(3.0, 0.5, 3.0) + vec3(31.0, 0.0, 17.0));
	float path = smoothstep(0.55, 0.65, path_noise) * smoothstep(0.82, 0.92, slope) * 0.08;
	albedo = mix(albedo, vec3(0.35, 0.28, 0.18), path);

	ALBEDO = clamp(albedo, vec3(0.0), vec3(1.0));
	ROUGHNESS = clamp(roughness_base + n1 * 0.06 - n2 * 0.03, 0.0, 1.0);
	SPECULAR = 0.18;
	AO = clamp(0.82 + n1 * 0.18, 0.0, 1.0);
	METALLIC = 0.0;
}
