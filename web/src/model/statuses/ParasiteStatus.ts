import { StackingMode, StackingStatus } from '../Status';
import { HEAL_HANDLER, type IHealHandler } from '../Body';
import type { IDeathHandler } from '../../core/types';
import { GameModelRef } from '../GameModelRef';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Floor } from '../Floor';

const DEATH_HANDLER = Symbol.for('IDeathHandler');

/**
 * A parasite is inside you! Take 1 attack damage per 10 turns.
 * Healing or clearing the floor cures immediately.
 * If you die, a Parasite Egg spawns over your corpse.
 * Port of C# ParasiteStatus.
 */
export class ParasiteStatus extends StackingStatus implements IDeathHandler, IHealHandler {
  readonly [DEATH_HANDLER] = true as const;
  readonly [HEAL_HANDLER] = true as const;

  get stackingMode(): StackingMode {
    return StackingMode.Independent;
  }

  get isDebuff(): boolean {
    return true;
  }

  private floorClearedUnsub: (() => void) | null = null;

  constructor() {
    super(1);
  }

  Start(): void {
    // Subscribe to floor cleared events
    const model = GameModelRef.mainOrNull;
    if (model) {
      this.floorClearedUnsub = model.onFloorCleared.on((_floor: Floor) => this.handleFloorCleared());
    }
  }

  End(): void {
    this.floorClearedUnsub?.();
    this.floorClearedUnsub = null;
  }

  private handleFloorCleared(): void {
    this.Remove();
  }

  handleHeal(_amount: number): void {
    this.Remove();
  }

  handleDeath(_source: any): void {
    const actor = this.actor;
    if (!actor) return;
    const floor = actor.floor;
    const pos = actor.pos;
    GameModelRef.main.enqueuEvent(() => {
      // Use entityRegistry to break circular dep (Parasite ↔ ParasiteStatus)
      const egg = entityRegistry.create('ParasiteEgg', pos);
      if (egg && floor) {
        floor.put(egg);
      }
    });
  }

  Step(): void {
    this.stacks = this.stacks + 1;
    if (this.stacks > 10) {
      GameModelRef.main.enqueuEvent(() => {
        this.actor?.takeAttackDamage(1, this.actor);
      });
      this.stacks = 1;
    }
  }
}
