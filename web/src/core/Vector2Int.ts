export class Vector2Int {
  constructor(
    public readonly x: number,
    public readonly y: number,
  ) {}

  static add(a: Vector2Int, b: Vector2Int): Vector2Int {
    return new Vector2Int(a.x + b.x, a.y + b.y);
  }

  static sub(a: Vector2Int, b: Vector2Int): Vector2Int {
    return new Vector2Int(a.x - b.x, a.y - b.y);
  }

  static scale(a: Vector2Int, s: number): Vector2Int {
    return new Vector2Int(a.x * s, a.y * s);
  }

  static equals(a: Vector2Int, b: Vector2Int): boolean {
    return a.x === b.x && a.y === b.y;
  }

  static distance(a: Vector2Int, b: Vector2Int): number {
    const dx = a.x - b.x;
    const dy = a.y - b.y;
    return Math.sqrt(dx * dx + dy * dy);
  }

  static manhattanDistance(a: Vector2Int, b: Vector2Int): number {
    return Math.abs(a.x - b.x) + Math.abs(a.y - b.y);
  }

  /** Chebyshev distance — max of axis deltas. Used for "is adjacent" checks. */
  static chebyshevDistance(a: Vector2Int, b: Vector2Int): number {
    return Math.max(Math.abs(a.x - b.x), Math.abs(a.y - b.y));
  }

  static key(v: Vector2Int): string {
    return `${v.x},${v.y}`;
  }

  toString(): string {
    return `(${this.x}, ${this.y})`;
  }

  static readonly zero = new Vector2Int(0, 0);
  static readonly up = new Vector2Int(0, 1);
  static readonly down = new Vector2Int(0, -1);
  static readonly left = new Vector2Int(-1, 0);
  static readonly right = new Vector2Int(1, 0);

  /** 4 cardinal directions */
  static readonly cardinalDirections = [
    Vector2Int.up,
    Vector2Int.down,
    Vector2Int.left,
    Vector2Int.right,
  ] as const;

  /** 8 directions including diagonals */
  static readonly allDirections = [
    new Vector2Int(0, 1),
    new Vector2Int(1, 1),
    new Vector2Int(1, 0),
    new Vector2Int(1, -1),
    new Vector2Int(0, -1),
    new Vector2Int(-1, -1),
    new Vector2Int(-1, 0),
    new Vector2Int(-1, 1),
  ] as const;
}
