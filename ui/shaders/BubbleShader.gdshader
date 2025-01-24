shader_type canvas_item;

uniform vec4 background_color : source_color;
uniform float background_noise_scale;
uniform vec4 bubble_color : source_color;
uniform float bubble_size : hint_range(0.0, 1.0, 0.01);
uniform float bubble_noise_scale;
uniform sampler2D bubble_noise_texture :  repeat_enable, filter_linear;
uniform sampler2D background_texture :  repeat_enable, filter_linear;

float sampleBubbleNoise(vec2 position) {
	vec2 noiseSize = vec2(textureSize(bubble_noise_texture, 0));
	position = mod(position / bubble_noise_scale, noiseSize) / noiseSize;
	return 1.0 - texture(bubble_noise_texture, position).r;
}

vec4 sampleBackground(vec2 position) {
	vec2 noiseSize = vec2(textureSize(background_texture, 0));
	position = mod(position / background_noise_scale, noiseSize) / noiseSize;
	return texture(background_texture, position) * background_color;
}

void fragment() {
	float noise = sampleBubbleNoise(vec2(FRAGCOORD.x + TIME * 5.0, FRAGCOORD.y + TIME * 30.0));
	float cutoff = 1.0 / bubble_size;
	noise = max(noise * cutoff - (cutoff - 1.0), 0.0); // gradiant for only the upper third, everything else is set to 0
	noise = sin(pow(noise, 0.1) * PI); // will now raise quickly to 1 and fall off back to 0 (0->0, 0.000977->1, 1->0)
	vec4 effectColor = vec4(bubble_color.rgb, bubble_color.a * noise);
	vec4 backgroundColor = sampleBackground(vec2(FRAGCOORD.x + TIME * 7.0, FRAGCOORD.y + TIME * 3.0));
	vec4 targetColor = mix(backgroundColor, effectColor, effectColor.a);
	COLOR = COLOR == vec4(1.0, 1.0, 1.0, 1.0) ? vec4(targetColor.rgb, max(backgroundColor.a, targetColor.a)) : COLOR;
}