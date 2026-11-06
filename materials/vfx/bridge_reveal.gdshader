shader_type canvas_item;
render_mode unshaded;

uniform float age = 0.0;
uniform float exit_progress = 0.0;

float rail_glow(vec2 point, vec2 start, vec2 end, float phase) {
    vec2 direction = normalize(end - start);
    vec2 normal = vec2(-direction.y, direction.x);
    float glow = 0.0;
    for (int i = 0; i < 2; i++) {
        float travel = fract(max(age - 0.48, 0.0) * 0.17 + float(i) * 0.5 + phase);
        vec2 delta = point - mix(start, end, travel);
        float along = dot(delta, direction);
        float across = dot(delta, normal);
        // Rounded head and a short soft tail; no line caps or cross-shaped particle head.
        float reach = along > 0.0 ? 5.5 : 13.0;
        float core = exp(-pow(along / reach, 2.0) - pow(across / 1.3, 2.0));
        float halo = exp(-pow(along / (reach * 1.5), 2.0) - pow(across / 3.5, 2.0));
        glow += (core + halo * 0.20) * sin(travel * 3.14159265);
    }
    return glow * smoothstep(0.48, 0.76, age);
}

void fragment() {
    vec4 painted = texture(TEXTURE, UV);
    float front = mix(-0.18, 1.20, smoothstep(0.10, 0.44, age));
    // Reveal and the travelling highlight launch from the large left end toward the small right end.
    float along_bridge = UV.x;
    float reveal = 1.0 - smoothstep(front - 0.16, front + 0.02, along_bridge);
    float fade = 1.0 - smoothstep(0.0, 1.0, exit_progress);
    float moving_light = exp(-pow((along_bridge - front + 0.06) / 0.095, 2.0));
    painted.rgb *= vec3(0.92, 0.87, 0.90) + moving_light * vec3(0.25, 0.20, 0.11);
    float sweep = mix(-0.14, 1.14, fract(max(age - 0.48, 0.0) * 0.24));
    float glint = exp(-pow((UV.x - sweep) / 0.075, 2.0));
    float span = smoothstep(0.11, 0.18, UV.x) * (1.0 - smoothstep(0.88, 0.94, UV.x));
    span *= 1.0 - smoothstep(0.39, 0.44, UV.y);
    painted.rgb += vec3(0.22, 0.15, 0.065) * glint * span * smoothstep(0.48, 0.70, age);
    // Texture-space highlights follow the painted rails and inherit their alpha/occlusion.
    vec2 surface = UV * vec2(1280.0, 720.0);
    float rail_light = rail_glow(surface, vec2(233.0, 94.0), vec2(1093.0, 232.0), 0.0);
    rail_light += rail_glow(surface, vec2(233.0, 168.0), vec2(1093.0, 283.0), 0.19);
    painted.rgb += vec3(0.64, 0.53, 0.31) * rail_light;
    // Edge supports dissolve above the actors' lower bodies, keeping the overhead scenery light.
    float soft_crop = 1.0 - smoothstep(0.38, 0.60, UV.y);
    COLOR = vec4(painted.rgb, painted.a * reveal * fade * soft_crop);
}
