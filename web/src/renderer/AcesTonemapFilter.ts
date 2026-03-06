import { Filter, GlProgram, defaultFilterVert } from 'pixi.js';

const acesFragment = /* glsl */ `
in vec2 vTextureCoord;
out vec4 finalColor;

uniform sampler2D uTexture;
uniform float uExposure;

vec3 acesFilmic(vec3 x) {
  float a = 2.51;
  float b = 0.03;
  float c = 2.43;
  float d = 0.59;
  float e = 0.14;
  return clamp((x * (a * x + b)) / (x * (c * x + d) + e), 0.0, 1.0);
}

void main() {
  vec4 color = texture(uTexture, vTextureCoord);
  color.rgb *= uExposure;
  color.rgb = acesFilmic(color.rgb);
  finalColor = color;
}
`;

export class AcesTonemapFilter extends Filter {
  constructor(exposure = 1.0) {
    super({
      glProgram: new GlProgram({
        fragment: acesFragment,
        vertex: defaultFilterVert,
      }),
      resources: {
        acesUniforms: {
          uExposure: { value: exposure, type: 'f32' },
        },
      },
    });
  }

  get exposure(): number {
    return this.resources.acesUniforms.uniforms.uExposure;
  }

  set exposure(value: number) {
    this.resources.acesUniforms.uniforms.uExposure = value;
  }
}
