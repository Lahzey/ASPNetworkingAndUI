shader_type canvas_item;

uniform vec4 background_color : source_color;
uniform vec4 border_color : source_color;
uniform bool animated;
uniform float animation_duration;
uniform vec4 highlight_color : source_color;
uniform bool loop_animation;
uniform int animation_mode;
uniform float start_time;
uniform float noise_scale;
uniform sampler2D noise_texture :  repeat_enable, filter_linear;

global uniform float ACTUAL_TIME;

float sampleNoise(vec2 position) {
	vec2 noiseSize = vec2(textureSize(noise_texture, 0));
	position = mod(position / noise_scale, noiseSize) / noiseSize;
	return texture(noise_texture, position).r;
}

// we could loop with mod(animationTime, 1.0), but we instead let each alogrithm decide for itself how to loop
float getAnimationProgress() {
	float animationTime = (ACTUAL_TIME - start_time) / animation_duration;
	return loop_animation ? animationTime : min(animationTime, 1.0);
}

vec4 lerp(vec4 a, vec4 b, float progress) {
	vec4 dist = b - a;
	return a + dist * progress;
}

vec4 getBlinkingColor(float animationProgress) {
	float lerpProgress = sin((animationProgress - 0.25) * PI * 2.0) * 0.5 + 0.5;
	return lerp(border_color, highlight_color, lerpProgress);
}

float getSweepPosition(vec2 uv) {
	vec2 position = uv * 2.0 - vec2(1.0); // Normalize UV to [-1, 1]
    return (atan(position.x, position.y) + PI) / (2.0*PI);
}

vec4 getRadialFillColor(vec2 uv, float animationProgress) {
	float normalizedProgress = mod(animationProgress, 2.0) * 0.5;
	float lerpProgress = sin((normalizedProgress - 0.25) * PI * 2.0) * 0.5 + 0.5;
	return getSweepPosition(uv) <= lerpProgress ? highlight_color : border_color;
}

vec4 getRadialTraceColor(vec2 uv, float animationProgress) {
	float modProgress = mod(animationProgress, 1.0);
	float distToProgressCenter = modProgress - getSweepPosition(uv);
	distToProgressCenter = sin((distToProgressCenter - 0.25) * PI * 2.0) * 0.5 + 0.5;
	float intensity = max((1.0 - distToProgressCenter) - 0.8, 0.0) * 5.0;
	return lerp(border_color, highlight_color, intensity);
}

void fragment() {
	float noise = sampleNoise(FRAGCOORD.xy);
	
	vec4 backgroundColor = background_color * vec4(noise, noise, noise, 1.0);
	
	float animationProgress = getAnimationProgress();
	vec4 animationColor;
	switch (animation_mode) {
		case 2:
			animationColor = getBlinkingColor(animationProgress);
			break;
		case 1:
			animationColor = getRadialTraceColor(UV, animationProgress);
			break;
		default:
			animationColor = getRadialFillColor(UV, animationProgress);
			break;
	}
	
	vec4 borderColor = animated ? animationColor : border_color;
	
	bool isBorder = COLOR == vec4(0.0, 1.0, 0.0, 1.0);
	bool isBackground = COLOR == vec4(0.0, 0.0, 1.0, 1.0);
	
	COLOR = isBorder ? borderColor : (isBackground ? backgroundColor : COLOR);
}