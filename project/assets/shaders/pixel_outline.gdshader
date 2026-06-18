shader_type canvas_item;

uniform vec4 outline_color : source_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform int outline_width : hint_range(1, 3) = 1;

void fragment() {
    vec4 tex_color = texture(TEXTURE, UV);
    vec2 pixel_size = 1.0 / vec2(textureSize(TEXTURE, 0));

    if (tex_color.a < 0.1) {
        for (int x = -outline_width; x <= outline_width; x++) {
            for (int y = -outline_width; y <= outline_width; y++) {
                if (x == 0 && y == 0) continue;
                vec2 offset = vec2(float(x), float(y)) * pixel_size;
                vec4 neighbor = texture(TEXTURE, UV + offset);
                if (neighbor.a > 0.1) {
                    COLOR = outline_color;
                    return;
                }
            }
        }
        discard;
    } else {
        COLOR = tex_color;
    }
}
