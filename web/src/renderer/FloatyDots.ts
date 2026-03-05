import { Container, Graphics } from 'pixi.js';
import { Camera } from './Camera';

/**
 * Ambient floating dot particles matching Unity's "Floor Floaty Dots" ParticleSystem.
 *
 * Unity params: emission 20/sec, lifetime 5s, startSize 0.1, startSpeed 1,
 * velocity x=-0.3, noise strength 0.05 freq 0.1, material Particle-Circle5-50.
 */

interface Particle {
  graphic: Graphics;
  /** World X in tile units. */
  x: number;
  /** World Y in tile units. */
  y: number;
  /** Seconds alive. */
  age: number;
  /** Noise phase offsets for pseudo-random wobble. */
  noisePhaseX: number;
  noisePhaseY: number;
}

const EMISSION_RATE = 5;
const LIFETIME = 10;
const MAX_PARTICLES = 120;
const DRIFT_X = -0.1; // tiles/sec leftward
const NOISE_STRENGTH = 0.05; // tiles displacement
const NOISE_FREQ = 0.1; // Hz
const BASE_ALPHA = 0.5;
/** Particle radius as fraction of tile size. */

export class FloatyDots {
  readonly container = new Container();
  private particles: Particle[] = [];
  private pool: Graphics[] = [];
  private elapsed = 0;
  private spawnAccum = 0;

  constructor() {
    this.container.interactiveChildren = false;
  }

  update(dt: number, camera: Camera): void {
    this.elapsed += dt;
    this.spawnAccum += dt * EMISSION_RATE;

    const ts = camera.tileSize;

    // const RADIUS_FRAC = 0.03;
    // const radius = Math.max(ts * RADIUS_FRAC, 1);
    const radius = 4;

    // Spawn new particles within floor bounds
    while (this.spawnAccum >= 1 && this.particles.length < MAX_PARTICLES) {
      this.spawnAccum -= 1;
      const p = this.acquire(radius);
      // Random position across the floor area
      p.x = Math.random() * (camera.floorWidth + 4) - 2;
      p.y = Math.random() * (camera.floorHeight + 4) - 2;
      p.age = 0;
      p.noisePhaseX = Math.random() * Math.PI * 2;
      p.noisePhaseY = Math.random() * Math.PI * 2;
      p.graphic.alpha = BASE_ALPHA;
      this.particles.push(p);
      this.container.addChild(p.graphic);
    }
    // Discard excess accumulation
    if (this.spawnAccum >= 1) this.spawnAccum = 0;

    // Update existing particles
    const t = this.elapsed;
    for (let i = this.particles.length - 1; i >= 0; i--) {
      const p = this.particles[i];
      p.age += dt;

      if (p.age >= LIFETIME) {
        this.release(p);
        this.particles[i] = this.particles[this.particles.length - 1];
        this.particles.pop();
        continue;
      }

      // Drift
      p.x += DRIFT_X * dt;

      // Noise wobble (sine approximation)
      const noiseX = Math.sin(t * NOISE_FREQ * Math.PI * 2 + p.noisePhaseX) * NOISE_STRENGTH;
      const noiseY = Math.sin(t * NOISE_FREQ * Math.PI * 2 * 1.3 + p.noisePhaseY) * NOISE_STRENGTH;

      // Convert tile position to pixel
      const px = camera.offsetX + p.x * ts + noiseX * ts;
      const py = camera.offsetY + (camera.floorHeight - 1 - p.y) * ts + noiseY * ts;
      p.graphic.position.set(px, py);

      // Fade in/out at edges of lifetime
      const fadeIn = Math.min(p.age / 0.5, 1);
      const fadeOut = Math.min((LIFETIME - p.age) / 0.5, 1);
      p.graphic.alpha = BASE_ALPHA * fadeIn * fadeOut;
    }
  }

  clear(): void {
    for (const p of this.particles) {
      this.container.removeChild(p.graphic);
      this.pool.push(p.graphic);
    }
    this.particles.length = 0;
    this.spawnAccum = 0;
  }

  /** Prewarm by simulating several seconds of emission. */
  prewarm(camera: Camera): void {
    const steps = 50;
    const stepDt = LIFETIME / steps;
    for (let i = 0; i < steps; i++) {
      this.update(stepDt, camera);
    }
  }

  private acquire(radius: number): Particle {
    const graphic = this.pool.pop() ?? this.createGraphic(radius);
    // Resize if tile size changed
    graphic.scale.set(1);
    return {
      graphic,
      x: 0,
      y: 0,
      age: 0,
      noisePhaseX: 0,
      noisePhaseY: 0,
    };
  }

  private release(p: Particle): void {
    this.container.removeChild(p.graphic);
    this.pool.push(p.graphic);
  }

  private createGraphic(radius: number): Graphics {
    const g = new Graphics();
    g.circle(0, 0, radius).fill({ color: 0xffffff, alpha: 1 });
    return g;
  }
}
