shader_type canvas_item;

vec2 hash( vec2 p ) // replace this by something better
{
	p = vec2( dot(p,vec2(127.1,311.7)), dot(p,vec2(269.5,183.3)) );
	return -1.0 + 2.0*fract(sin(p)*43758.5453123);
}

float noise( in vec2 p )
{
    const float K1 = 0.366025404; // (sqrt(3)-1)/2;
    const float K2 = 0.211324865; // (3-sqrt(3))/6;

	vec2  i = floor( p + (p.x+p.y)*K1 );
    vec2  a = p - i + (i.x+i.y)*K2;
    float m = step(a.y,a.x); 
    vec2  o = vec2(m,1.0-m);
    vec2  b = a - o + K2;
	vec2  c = a - 1.0 + 2.0*K2;
    vec3  h = max( 0.5-vec3(dot(a,a), dot(b,b), dot(c,c) ), 0.0 );
	vec3  n = h*h*h*h*vec3( dot(a,hash(i+0.0)), dot(b,hash(i+o)), dot(c,hash(i+1.0)));
    return dot( n, vec3(70.0) );
}

vec3 lerp(vec3 a, vec3 b, float progress) {
    vec3 dist = b - a;
    return a + (dist * progress);
}

vec3 doubleLerp(vec3 a, vec3 b, vec3 c, float progress) {
    vec3 distAB = b - a;
    vec3 distBC = c - b;
    return progress < 0.5 ? a + (distAB * progress * 2.0) : b + (distBC * (progress - 0.5) * 2.0);
}

float getTotalValue(vec2 fragCoord, float time) {
    float drag = noise(vec2(fragCoord.y * 0.05 + time * -0.5, -500.0));
    vec2 coord = fragCoord * 0.01 + vec2(time * -1.0, time * 0.1);
    float value = noise(vec2(coord.x + (drag * 2.0), coord.y));
    value = pow(value, 8.0);

    return value < 0.3 ? value : pow(value, 0.3);
}

void fragment() {
	float value = getTotalValue(FRAGCOORD.xy, TIME);
    COLOR = vec4(doubleLerp(vec3(0.145, 0.251, 0.302), vec3(0.55, 0.72, 0.7), vec3(0.7, 0.6, 0.3), value), 1.0);
}
