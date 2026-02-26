import { Vector2Int } from './Vector2Int';

/**
 * Seeded PRNG using mulberry32 algorithm.
 * Replaces C# System.Random with deterministic seeded generation.
 */
class SeededRandom {
  private state: number;

  constructor(seed: number) {
    this.state = seed | 0;
  }

  /** Returns float in [0, 1) */
  next(): number {
    let t = (this.state += 0x6d2b79f5);
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  }

  /** Returns int in [min, max) */
  nextInt(min: number, max: number): number {
    if (min >= max) return min;
    return min + Math.floor(this.next() * (max - min));
  }
}

let generator = new SeededRandom(Date.now());

export const MyRandom = {
  get value(): number {
    return generator.next();
  },

  setSeed(seed: number): void {
    generator = new SeededRandom(seed);
  },

  /** min inclusive, max exclusive */
  Range(min: number, max: number): number {
    return generator.nextInt(min, max);
  },

  RangeVec(min: Vector2Int, max: Vector2Int): Vector2Int {
    return new Vector2Int(
      generator.nextInt(min.x, max.x),
      generator.nextInt(min.y, max.y),
    );
  },

  /** Randomly rounds a float — e.g. 3.7 has 70% chance of 4, 30% chance of 3 */
  RandRound(v: number): number {
    const mod = v % 1;
    const floor = Math.floor(v);
    const ceil = Math.ceil(v);
    if (mod === 0) return floor;
    return generator.next() < mod ? ceil : floor;
  },

  /** Shuffle array in place using Fisher-Yates */
  Shuffle<T>(arr: T[]): T[] {
    for (let i = arr.length - 1; i > 0; i--) {
      const j = generator.nextInt(0, i + 1);
      [arr[i], arr[j]] = [arr[j], arr[i]];
    }
    return arr;
  },

  /** Pick a random element from an array */
  Pick<T>(arr: readonly T[]): T {
    return arr[generator.nextInt(0, arr.length)];
  },
} as const;
