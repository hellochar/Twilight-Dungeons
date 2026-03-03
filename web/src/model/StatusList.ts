import { collectModifiers } from '../core/Modifiers';
import { STATUS_ADDED_HANDLER, STATUS_REMOVED_HANDLER } from './Actor';
import { Status } from './Status';
import type { Actor } from './Actor';
import { EventEmitter } from '../core/EventEmitter';

/**
 * Manages the list of Status effects on an Actor.
 * Port of C# StatusList from actors/Status.cs.
 */
export class StatusList {
  readonly list: Status[] = [];
  readonly actor: Actor;
  readonly onAdded = new EventEmitter<[Status]>();

  constructor(actor: Actor) {
    this.actor = actor;
  }

  add<T extends Status>(status: T): void {
    const existing = this.findOfType(status.constructor as new (...args: any[]) => T);
    const consumed = existing?.Consume(status) ?? false;

    if (!consumed) {
      this.list.push(status);
      status.list = this;
      this.onStatusAdded(status);
    }
  }

  remove(status: Status): void {
    const idx = this.list.indexOf(status);
    if (idx !== -1) {
      this.list.splice(idx, 1);
      status.list = null;
      this.onStatusRemoved(status);
    }
  }

  removeOfType<T extends Status>(statusType: new (...args: any[]) => T): void {
    const toRemove = this.list.filter(s => s instanceof statusType);
    for (const status of toRemove) {
      this.remove(status);
    }
  }

  has<T extends Status>(statusType: new (...args: any[]) => T): boolean {
    return this.list.some(s => s instanceof statusType);
  }

  findOfType<T extends Status>(statusType: new (...args: any[]) => T): T | undefined {
    return this.list.find(s => s instanceof statusType) as T | undefined;
  }

  /** Alias for findOfType. */
  get<T extends Status>(statusType: new (...args: any[]) => T): T | undefined {
    return this.findOfType(statusType);
  }

  [Symbol.iterator](): IterableIterator<Status> {
    return this.list[Symbol.iterator]();
  }

  private onStatusAdded(status: Status): void {
    interface IStatusAddedHandler {
      handleStatusAdded(status: Status): void;
    }
    const handlers = collectModifiers<IStatusAddedHandler>(this.actor, STATUS_ADDED_HANDLER);
    for (const handler of handlers) {
      handler.handleStatusAdded(status);
    }
    this.onAdded.emit(status);
  }

  private onStatusRemoved(status: Status): void {
    interface IStatusRemovedHandler {
      handleStatusRemoved(status: Status): void;
    }
    const handlers = collectModifiers<IStatusRemovedHandler>(this.actor, STATUS_REMOVED_HANDLER);
    for (const handler of handlers) {
      handler.handleStatusRemoved(status);
    }
  }

  toString(): string {
    return `[${this.list.join(', ')}]`;
  }
}
