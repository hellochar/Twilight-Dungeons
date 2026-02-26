/**
 * Stub for Inventory — will be fully implemented in Session 7.
 */
export class Inventory {
  readonly maxSize: number;
  readonly items: any[] = [];

  constructor(maxSize: number) {
    this.maxSize = maxSize;
  }

  get isFull(): boolean {
    return this.items.length >= this.maxSize;
  }

  add(item: any): boolean {
    if (this.isFull) return false;
    this.items.push(item);
    return true;
  }

  remove(item: any): void {
    const idx = this.items.indexOf(item);
    if (idx !== -1) this.items.splice(idx, 1);
  }

  [Symbol.iterator](): IterableIterator<any> {
    return this.items[Symbol.iterator]();
  }
}
