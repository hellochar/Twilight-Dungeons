import { SimpleStatusApplicationEnemy } from './SimpleStatusApplicationEnemy';
import { AIActor } from './AIActor';
import { Vector2Int } from '../../core/Vector2Int';
import { MyRandom } from '../../core/MyRandom';
import { entityRegistry } from '../../generator/entityRegistry';

/**
 * Every turn, heals a random hurt enemy other than itself for 1 HP.
 * Port of C# Healer.
 */
export class Healer extends SimpleStatusApplicationEnemy {
  get cooldown(): number { return 0; }

  constructor(pos: Vector2Int) {
    super(pos);
    this._hp = 5;
    this._baseMaxHp = 5;
  }

  doTask(): void {
    const hurtEnemies = this.floor!.bodies
      .where((b: import('../Entity').Entity) => b instanceof AIActor && b !== this && (b as AIActor).hp < (b as AIActor).maxHp) as AIActor[];
    if (hurtEnemies.length > 0) {
      const choice = MyRandom.Pick(hurtEnemies);
      choice.heal(1);
    }
  }
}

entityRegistry.register('Healer', Healer);
