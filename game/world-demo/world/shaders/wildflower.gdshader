shader_type spatial;
render_mode cull_disabled, diffuse_burley, specular_schlick_ggx;

uniform vec2 wind_direction = vec2(1.0, 0.3);
uniform float wind_strength : hint_range(0.0, 0.3) = 0.04;
uniform float wind_speed : hint_range(0.0, 3.0) = 0.8;

varying float v_height;

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

void vertex() {
	v_height = 1.0 - UV.y;

	vec2 world_xz = NODE_POSITION_WORLD.xz;
	vec2 wind_pos = world_xz / 10.0 - TIME * normalize(wind_direction) * wind_speed;
	float wind_noise = value_noise(wind_pos);
	float sway = wind_noise * wind_strength * v_height * 2.0;

	mat4 inv_model = inverse(MODEL_MATRIX);
	vec2 local_dir = (inv_model * vec4(wind_direction.x, 0.0, wind_direction.y, 0.0)).xz;
	VERTEX.xz += sway * local_dir;
	VERTEX.y -= sway * 0.2;
}

void fragment() {
	if (!FRONT_FACING) {
		NORMAL = -NORMAL;
	}
	ALBEDO = COLOR.rgb;
	ROUGHNESS = 0.6;
	SPECULAR = 0.15;
	METALLIC = 0.0;
	BACKLIGHT = COLOR.rgb * 0.3;
}
