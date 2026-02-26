export enum Faction {
  Ally = 1,
  Neutral = 2,
  Enemy = 4,
}

export enum TileVisibility {
  Unexplored,
  Visible,
  Explored,
}

/** Bitflag enum for movement/collision layers */
export enum CollisionLayer {
  None = 0,
  Walking = 1 << 0,
  Flying = 1 << 1,
  All = Walking | Flying,
}

export enum ActionType {
  MOVE,
  ATTACK,
  WAIT,
  GENERIC,
}

/** Implement on Grass (or other entities) to make them block movement. */
export interface IBlocksMovement {
  readonly blockedLayers: CollisionLayer;
}

/** Called when the Actor is killed. */
export interface IActorKilledHandler {
  onKilled(actor: any): void;
}

/** Called on the killer when they kill an entity. */
export interface IKillEntityHandler {
  onKill(entity: any): void;
}

/** Called when an actor enters a tile with this entity. */
export interface IActorEnterHandler {
  handleActorEnter(actor: any): void;
}

/** Called when a body moves. */
export interface IBodyMoveHandler {
  handleMove(body: any, oldPos: any, newPos: any): void;
}

/** Called when an entity takes any damage. */
export interface ITakeAnyDamageHandler {
  handleTakeAnyDamage(damage: number, source: any): void;
}

/** Called when an action is performed. */
export interface IActionPerformedHandler {
  handleActionPerformed(action: any): void;
}

/** Called when an entity deals attack damage. */
export interface IDealAttackDamageHandler {
  handleDealAttackDamage(attack: any): void;
}

/** Marker for entities that skip the initial turn delay. */
export interface INoTurnDelay {
  readonly noTurnDelay: true;
}

export interface IDeathHandler {
  handleDeath(source: any): void;
}
