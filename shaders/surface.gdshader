shader_type spatial;
render_mode cull_disabled, diffuse_burley, specular_schlick_ggx;

uniform int   render_mode_id : hint_range(0, 4) = 0;
uniform bool  show_grid = false;
uniform float grid_density   = 16.0;
uniform float contour_count  = 16.0;
uniform float roughness_val : hint_range(0.0, 1.0) = 0.55;
uniform float metallic_val  : hint_range(0.0, 1.0) = 0.0;
uniform vec3  glossy_color : source_color = vec3(0.55, 0.65, 0.92);
uniform float rim_strength   : hint_range(0.0, 1.0) = 0.30;
uniform float detail_strength: hint_range(0.0, 1.0) = 0.40;
uniform bool  lit = true;   // éclairage activé / mode plat « sans éclairage »

varying float height_t;
varying vec2  uv_coord;

// Bruit procédural (value noise + fbm)
float hash(vec2 p) {
    p = fract(p * vec2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return fract(p.x * p.y);
}
float vnoise(vec2 p) {
    vec2 i = floor(p), f = fract(p);
    vec2 u = f * f * (3.0 - 2.0 * f);
    float a = hash(i), b = hash(i + vec2(1, 0));
    float c = hash(i + vec2(0, 1)), d = hash(i + vec2(1, 1));
    return mix(mix(a, b, u.x), mix(c, d, u.x), u.y);
}
float fbm(vec2 p) {
    float s = 0.0, amp = 0.5;
    for (int k = 0; k < 4; k++) { s += amp * vnoise(p); p *= 2.0; amp *= 0.5; }
    return s;
}

// Colormap viridis (fit polynomial sRGB)
vec3 viridis(float t) {
    const vec3 c0 = vec3(0.2777273272234177, 0.005407344544966578, 0.3340998053353061);
    const vec3 c1 = vec3(0.1050930431085774, 1.404613529898575, 1.384590162594685);
    const vec3 c2 = vec3(-0.3308618287255563, 0.214847559468213, 0.09509516302823659);
    const vec3 c3 = vec3(-4.634230498983486, -5.799100973351585, -19.33244095627987);
    const vec3 c4 = vec3(6.228269936347081, 14.17993336680509, 56.69055260068105);
    const vec3 c5 = vec3(4.776384997670288, -13.74514537774601, -65.35303263337234);
    const vec3 c6 = vec3(-5.435455855934631, 4.645852612178535, 26.3124352495832);
    return c0 + t * (c1 + t * (c2 + t * (c3 + t * (c4 + t * (c5 + t * c6)))));
}

float line_factor(float value, float density) {
    float v = value * density;
    float d = abs(fract(v - 0.5) - 0.5);
    float w = fwidth(v) * 1.5 + 1e-5;
    return 1.0 - smoothstep(0.0, w, d);
}

// Bump par dérivées (Mikkelsen) : perturbe N sans tangentes.
vec3 perturb_normal(vec3 N, vec3 P, float h) {
    vec3 dPdx = dFdx(P);
    vec3 dPdy = dFdy(P);
    float dHdx = dFdx(h);
    float dHdy = dFdy(h);
    vec3 r1 = cross(dPdy, N);
    vec3 r2 = cross(N, dPdx);
    float det = dot(dPdx, r1);
    vec3 grad = (r1 * dHdx + r2 * dHdy) / max(abs(det), 1e-6);
    return normalize(N - grad);
}

void vertex() {
    height_t = clamp(UV2.x, 0.0, 1.0);
    uv_coord = UV;
}

void fragment() {
    vec3 albedo;
    vec3 emission = vec3(0.0);
    // Modes « données » (viridis / damier / topo) : mats, faible spéculaire →
    // la couleur (la donnée) domine, pas de voile du ciel réfléchi. Seuls les
    // modes « matière » (brillant / iridescent) sont réfléchissants.
    float rough = 0.72;
    float metal = 0.0;
    float spec  = 0.16;
    float rim_scale = 0.55;

    // Couleur de base par mode
    if (render_mode_id == 1) {
        vec2 cell = floor(uv_coord * grid_density);
        float checker = mod(cell.x + cell.y, 2.0);
        albedo = mix(vec3(0.78, 0.80, 0.84), vec3(0.16, 0.18, 0.24), checker);
    } else if (render_mode_id == 3) {
        albedo = glossy_color;
        rough = roughness_val;
        metal = metallic_val;
        spec = 0.5; rim_scale = 1.0;
    } else if (render_mode_id == 4) {
        float f = pow(1.0 - clamp(dot(normalize(NORMAL), normalize(VIEW)), 0.0, 1.0), 1.2);
        float hue = fract(f * 1.4 + height_t * 0.3);
        vec3 irid = 0.5 + 0.5 * cos(6.2831853 * (hue + vec3(0.0, 0.33, 0.67)));
        albedo = pow(irid, vec3(2.2));
        rough = 0.25; metal = 0.6;
        spec = 0.5; rim_scale = 1.0;
    } else {
        albedo = pow(clamp(viridis(height_t), 0.0, 1.0), vec3(2.2));
        if (render_mode_id == 2) {
            // Bandes assombries entre niveaux + ligne émissive nette.
            float band = fract(height_t * contour_count);
            albedo *= 0.78 + 0.22 * smoothstep(0.0, 0.15, band) * smoothstep(1.0, 0.85, band);
            float c = line_factor(height_t, contour_count);
            albedo = mix(albedo, vec3(0.02), c * 0.5);
            emission += vec3(0.15, 0.65, 0.5) * c * 2.4;
        }
    }

    // Grille de paramétrage (émissive)
    if (show_grid && render_mode_id != 1) {
        float g = max(line_factor(uv_coord.x, grid_density), line_factor(uv_coord.y, grid_density));
        emission += vec3(0.35, 0.6, 1.1) * g * 1.6;
        albedo = mix(albedo, vec3(0.06, 0.08, 0.12), g * 0.3);
    }

    // Micro-relief procédural
    // Échantillonné en UV (paramétrage) et non en monde : la densité du grain
    // reste constante quelle que soit la taille physique de la surface — donc
    // aussi visible sur les grandes nappes plates (Partie 2) que sur la sphère.
    // Perturbation en espace vue (VERTEX) pour rester cohérent avec NORMAL.
    vec2 noise_uv = uv_coord * 20.0;
    float bump = fbm(noise_uv) * detail_strength * 0.05;
    vec3 n = perturb_normal(normalize(NORMAL), VERTEX, bump);
    NORMAL = n;
    // Rugosité variée → micro-scintillement.
    rough = clamp(rough + (fbm(noise_uv * 2.0) - 0.5) * 0.18 * detail_strength, 0.04, 1.0);

    // Rim lumineux (Fresnel)
    // Exposant élevé → confiné à la vraie silhouette ; atténué sur les modes
    // « données » (rim_scale) pour ne pas voiler de bleu une nappe plate.
    float fres = pow(1.0 - clamp(dot(n, normalize(VIEW)), 0.0, 1.0), 4.5);
    emission += vec3(0.45, 0.6, 0.95) * fres * rim_strength * rim_scale;

    if (lit) {
        ALBEDO = albedo;
        ROUGHNESS = rough;
        METALLIC = metal;
        SPECULAR = spec;
        EMISSION = emission;
    } else {
        // Sans éclairage : la couleur est émise telle quelle (aplat).
        ALBEDO = vec3(0.0);
        ROUGHNESS = 1.0;
        METALLIC = 0.0;
        SPECULAR = 0.0;
        EMISSION = albedo + emission;
    }
}
