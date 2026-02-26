import type { Actor } from './Actor';

/**
 * Stub for StatusList — will be fully implemented in Session 7.
 * Manages the list of Status effects on an Actor.
 */
export class Status {
  get isDebuff(): boolean {
    return false;
  }

  start(_actor: any): void {}
  end(_actor: any): void {}
  step(): void {}

  consume(_other: Status): boolean {
    return false;
  }
}

export class StatusList {
  readonly list: Status[] = [];
  private actor: Actor;

  constructor(actor: Actor) {
    this.actor = actor;
  }

  add(status: Status): void {
    // Check if an existing status of the same type can consume this one
    for (const existing of this.list) {
      if (existing.constructor === status.constructor && existing.consume(status)) {
        return;
      }
    }
    this.list.push(status);
    status.start(this.actor);
  }

  remove(status: Status): void {
    const idx = this.list.indexOf(status);
    if (idx !== -1) {
      this.list.splice(idx, 1);
      status.end(this.actor);
    }
  }

  has(statusType: new (...args: any[]) => Status): boolean {
    return this.list.some(s => s instanceof statusType);
  }

  get<T extends Status>(statusType: new (...args: any[]) => T): T | undefined {
    return this.list.find(s => s instanceof statusType) as T | undefined;
  }

  [Symbol.iterator](): IterableIterator<Status> {
    return this.list[Symbol.iterator]();
  }
}
