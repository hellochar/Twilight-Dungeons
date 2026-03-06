import { MyRandom } from '../core/MyRandom';

/**
 * Weighted random selection with discount/subtract mechanics.
 * Port of C# WeightedRandomBag<T>.
 */
export class WeightedRandomBag<T> {
  protected entries: { weight: number; item: T }[] = [];

  add(weight: number, item: T): void {
    this.entries.push({ weight, item });
  }

  remove(item: T): void {
    this.entries = this.entries.filter(e => e.item !== item);
  }

  setWeight(item: T, weight: number): void {
    const entry = this.entries.find(e => e.item === item);
    if (entry) entry.weight = weight;
  }

  getWeight(item: T): number | null {
    const entry = this.entries.find(e => e.item === item);
    return entry ? entry.weight : null;
  }

  private getAccumulatedWeight(): number {
    return this.entries.reduce((sum, e) => sum + e.weight, 0);
  }

  getRandom(): T {
    const accumulatedWeight = this.getAccumulatedWeight();
    const r = MyRandom.value * accumulatedWeight;
    let accum = 0;
    for (const entry of this.entries) {
      accum += entry.weight;
      if (accum >= r) return entry.item;
    }
    return this.entries[this.entries.length - 1]?.item;
  }

  /** Get an item and reduce its weight by reduceChanceBy fraction */
  getRandomAndDiscount(reduceChanceBy = 0.3): T {
    // 11th hour hack - prevent drawing the same item twice
    reduceChanceBy = 1;
    const item = this.getRandom();
    if (item != null) this.discount(item, reduceChanceBy);
    return item;
  }

  getRandomAndSubtractWeight(subtract: number): T {
    const item = this.getRandom();
    if (item != null) this.subtractWeight(item, subtract);
    return item;
  }

  getRandomAndRemove(): T {
    return this.getRandomAndDiscount(1);
  }

  /**
   * Reduce item's selection probability by reduceChanceBy fraction,
   * preserving relative weights of other items.
   */
  discount(item: T, reduceChanceBy = 0.3): void {
    const currentWeight = this.getWeight(item);
    if (currentWeight == null) return;

    const accumulatedWeight = this.getAccumulatedWeight();
    const currentChance = currentWeight / accumulatedWeight;
    const newChance = currentChance * (1 - reduceChanceBy);

    // newChance = newWeight / (newWeight + rest), solve for newWeight:
    // newWeight = newChance * rest / (1 - newChance)
    const restWeight = accumulatedWeight - currentWeight;
    const newWeight = newChance * restWeight / (1 - newChance);
    if (newWeight <= 0) {
      this.remove(item);
    } else {
      this.setWeight(item, newWeight);
    }
  }

  subtractWeight(item: T, subtractWeightBy: number): void {
    const currentWeight = this.getWeight(item);
    if (currentWeight == null) return;
    if (this.entries.length === 1) return;
    this.setWeight(item, Math.max(currentWeight - subtractWeightBy, 0));
  }

  getRandomWithout(excludes: Set<T> | T[]): T {
    const excludeSet = excludes instanceof Set ? excludes : new Set(excludes);
    let pick: T;
    let guard = 0;
    do {
      pick = this.getRandom();
      if (pick == null) return pick;
    } while (excludeSet.has(pick) && ++guard < 100);
    return pick;
  }

  getRandomWithoutAndDiscount(excludes: Set<T> | T[], reduceChanceBy = 0.3): T {
    const pick = this.getRandomWithout(excludes);
    if (pick != null) this.discount(pick, reduceChanceBy);
    return pick;
  }

  clear(): void {
    this.entries = [];
  }

  clone(): WeightedRandomBag<T> {
    const bag = new WeightedRandomBag<T>();
    for (const e of this.entries) {
      bag.add(e.weight, e.item);
    }
    return bag;
  }

  merge(other: WeightedRandomBag<T>): this {
    for (const otherEntry of other.entries) {
      const existing = this.entries.find(e => e.item === otherEntry.item);
      if (existing) {
        existing.weight += otherEntry.weight;
      } else {
        this.entries.push({ ...otherEntry });
      }
    }
    return this;
  }

  get size(): number {
    return this.entries.length;
  }
}
