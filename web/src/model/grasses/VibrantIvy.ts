import { Grass } from './Grass';
import { Status } from '../Status';
import {
  ACTOR_ENTER_HANDLER,
  ACTOR_LEAVE_HANDLER,
  type IActorEnterHandler,
  type IActorLeaveHandler,
} from '../../core/types';
import { ATTACK_HANDLER, type IAttackHandler } from '../Actor';
import { Ground, Wall, Water } from '../Tile';
import { GameModelRef } from '../GameModelRef';
import { entityRegistry } from '../../generator/entityRegistry';
import type { Vector2Int } from '../../core/Vector2Int';
import type { Tile } from '../Tile';
import type { Body } from '../Body';

const CAMOUFLAGE = Symbol.for('IPlayerCamouflage');

/**
 * Camouflages the player. Stacks = adjacent cardinal walls.
 * Moving or attacking from within reduces stacks; at 0 the grass dies.
 * Port of C# VibrantIvy.cs.
 */
export class VibrantIvy extends Grass implements IActorEnterHandler, IAttackHandler, IActorLeaveHandler {
  readonly [ACTOR_ENTER_HANDLER] = true as const;
  readonly [ACTOR_LEAVE_HANDLER] = true as const;
  readonly [ATTACK_HANDLER] = true as const;

  private _stacks = 0;

  get stacks(): number {
    return this._stacks;
  }

  get bodyModifier(): object | null {
    return this;
  }

  constructor(pos: Vector2Int) {
    super(pos);
  }

  static canOccupy(tile: Tile): boolean {
    const floor = tile.floor!;
    const isHuggingWall = floor.getCardinalNeighbors(tile.pos).some(t => t instanceof Wall);
    const isGround = tile instanceof Ground || tile instanceof Water;
    const isNotIvy = !(tile.grass instanceof VibrantIvy);
    return isHuggingWall && isGround && isNotIvy;
  }

  protected handleEnterFloor(): void {
    this.computeStacks();
    const standing = this.floor?.bodies.get(this.pos);
    if (standing?.constructor.name === 'Player') {
      (standing as any).statuses.add(new CamouflagedStatus());
    }
  }

  protected handleLeaveFloor(): void {
    const standing = this.floor?.bodies.get(this.pos);
    if (standing?.constructor.name === 'Player') {
      (standing as any).statuses.removeOfType(CamouflagedStatus);
    }
  }

  handleActorEnter(who: any): void {
    if (who.constructor.name === 'Player') {
      who.statuses.add(new CamouflagedStatus());
      this.onNoteworthyAction();
    }
  }

  onAttack(_damage: number, _target: Body): void {
    const standing = this.floor?.bodies.get(this.pos);
    if (standing?.constructor.name === 'Player') {
      this.loseStack(standing as any);
    }
  }

  handleActorLeave(who: any): void {
    if (who.constructor.name === 'Player') {
      this.loseStack(who);
    }
  }

  private loseStack(player: any): void {
    this._stacks--;
    if (this._stacks === 0) {
      this.kill(player);
    }
  }

  computeStacks(): void {
    this._stacks = this.floor!.getCardinalNeighbors(this.pos).filter(t => t instanceof Wall).length;
  }
}

/**
 * Player is camouflaged — enemies won't chase, sleeping creatures won't wake.
 * Removes itself when the player is no longer standing on VibrantIvy.
 * Port of C# CamouflagedStatus from VibrantIvy.cs.
 */
export class CamouflagedStatus extends Status {
  readonly [CAMOUFLAGE] = true as const;

  Start(): void {
    this.actor?.floor?.recomputeVisibility();
  }

  End(): void {
    this.actor?.floor?.recomputeVisibility();
  }

  Consume(_other: Status): boolean {
    return true;
  }

  Step(): void {
    if (!(this.actor?.grass instanceof VibrantIvy)) {
      this.Remove();
    }
  }
}

entityRegistry.register('VibrantIvy', VibrantIvy);
