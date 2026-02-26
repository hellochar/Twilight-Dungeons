type Listener<T extends any[]> = (...args: T) => void;

/**
 * Simple typed event emitter replacing C# `event` delegates.
 *
 * Usage:
 *   const onDeath = new EventEmitter<[Entity, Entity]>();
 *   onDeath.on((killed, killer) => { ... });
 *   onDeath.emit(entity, source);
 */
export class EventEmitter<T extends any[] = []> {
  private listeners: Listener<T>[] = [];

  on(listener: Listener<T>): () => void {
    this.listeners.push(listener);
    return () => this.off(listener);
  }

  once(listener: Listener<T>): () => void {
    const wrapper = (...args: T) => {
      this.off(wrapper);
      listener(...args);
    };
    return this.on(wrapper);
  }

  off(listener: Listener<T>): void {
    const idx = this.listeners.indexOf(listener);
    if (idx !== -1) this.listeners.splice(idx, 1);
  }

  emit(...args: T): void {
    // Defensive copy — listeners may remove themselves during iteration
    const snapshot = [...this.listeners];
    for (const fn of snapshot) {
      fn(...args);
    }
  }

  clear(): void {
    this.listeners.length = 0;
  }

  get listenerCount(): number {
    return this.listeners.length;
  }
}
