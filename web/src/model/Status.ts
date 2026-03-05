import { STEP_MOD, type IStepModifier } from '../core/Modifiers';
import { GameModelRef } from './GameModelRef';
import type { StatusList } from './StatusList';
import type { Actor } from './Actor';

/**
 * Base class for all status effects.
 * Port of C# Status class from actors/Status.cs.
 *
 * Statuses implement IStepModifier so their Step() is called
 * during the modifier chain each turn.
 */
export abstract class Status implements IStepModifier {
  readonly [STEP_MOD] = true as const;

  private _list: StatusList | null = null;
  private removeScheduled = false;

  /** Called when removed (for controller/view hooks). */
  onRemoved: (() => void) | null = null;

  get list(): StatusList | null {
    return this._list;
  }

  /** Should only be set by StatusList. Setting to non-null calls Start(); setting to null calls End(). */
  set list(value: StatusList | null) {
    if (value == null && this._list != null) {
      this.onRemoved?.();
      this.End();
    }
    this._list = value;
    if (value != null) {
      this.Start();
    }
  }

  get actor(): Actor | null {
    return this._list?.actor ?? null;
  }

  get isDebuff(): boolean {
    return false;
  }

  /** Returns true if this status prevents the actor from moving. Used to suppress idle bob. */
  blocksMovement(): boolean {
    return false;
  }

  get displayName(): string {
    return this.constructor.name.replace(/Status$/, '').replace(/([A-Z])/g, ' $1').trim();
  }

  /** Called when list and actor are set up. */
  Start(): void {}

  /** Called right before the status is removed. NOT called if the actor dies with it. */
  End(): void {}

  /**
   * Return true if this Status has consumed the other (same type).
   * If consumed, the other status is discarded.
   */
  abstract Consume(other: Status): boolean;

  /** Called each turn via the step modifier chain. */
  Step(): void {}

  /** IStepModifier — step is called via modifier chain. */
  modify(input: object): object {
    this.Step();
    return input;
  }

  /** Schedule this status for removal. Safe to call during modifier processing. */
  Remove(): void {
    if (!this.removeScheduled) {
      this.removeScheduled = true;
      GameModelRef.main.enqueuEvent(() => this._list?.remove(this));
    }
  }

  /** Debuffs auto-remove on floor change. */
  handleFloorChanged(): void {
    if (this.isDebuff) {
      this.Remove();
    }
  }
}

// --- Stacking ---

export enum StackingMode {
  Add,
  Max,
  Ignore,
  Independent,
}

/**
 * Status that merges stacks when re-applied.
 * Port of C# StackingStatus.
 */
export abstract class StackingStatus extends Status {
  get stackingMode(): StackingMode {
    return StackingMode.Add;
  }

  private _stacks: number;

  get stacks(): number {
    return this._stacks;
  }

  set stacks(value: number) {
    this._stacks = value;
    if (value <= 0) {
      this.Remove();
    }
  }

  constructor(stacks = 1) {
    super();
    this._stacks = stacks;
  }

  Consume(other: Status): boolean {
    const otherStacking = other as StackingStatus;
    switch (this.stackingMode) {
      case StackingMode.Add:
        this.stacks += otherStacking.stacks;
        return true;
      case StackingMode.Max:
        this.stacks = Math.max(this.stacks, otherStacking.stacks);
        return true;
      case StackingMode.Ignore:
        return true;
      case StackingMode.Independent:
      default:
        return false;
    }
  }
}
