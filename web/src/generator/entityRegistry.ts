import type { Vector2Int } from '../core/Vector2Int';
import type { Entity } from '../model/Entity';

type EntityConstructor = new (pos: Vector2Int, ...args: any[]) => Entity;

const registry = new Map<string, EntityConstructor>();

/**
 * Entity registry for encounters — maps entity names to constructors.
 * Encounters reference entities by name; unregistered entities silently no-op.
 * As more entities are ported, they register themselves and automatically
 * appear in generated floors.
 */
export const entityRegistry = {
  register(name: string, ctor: EntityConstructor): void {
    registry.set(name, ctor);
  },

  create(name: string, pos: Vector2Int, ...args: any[]): Entity | null {
    const ctor = registry.get(name);
    if (!ctor) return null;
    return new ctor(pos, ...args);
  },

  isRegistered(name: string): boolean {
    return registry.has(name);
  },

  get(name: string): EntityConstructor | undefined {
    return registry.get(name);
  },

  names(): string[] {
    return Array.from(registry.keys());
  },
};
