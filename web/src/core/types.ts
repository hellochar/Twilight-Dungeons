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

/** Symbol marking non-AIActor entities (e.g. Muck, ParasiteEgg) as enemies for floor clearing. */
export const ENEMY_ENTITY_TAG = Symbol.for('IEnemyEntity');

/** Marker interface for entities that count as enemies for floor clearing. */
export interface IEnemyEntity {
  readonly [ENEMY_ENTITY_TAG]: true;
}

/** Symbol for collecting IActorEnterHandler via collectModifiers. */
export const ACTOR_ENTER_HANDLER = Symbol.for('IActorEnterHandler');

/** Symbol for collecting IActorLeaveHandler via collectModifiers. */
export const ACTOR_LEAVE_HANDLER = Symbol.for('IActorLeaveHandler');

/** Called when an actor enters a tile with this entity. */
export interface IActorEnterHandler {
  readonly [ACTOR_ENTER_HANDLER]: true;
  handleActorEnter(actor: any): void;
}

/** Called when an actor leaves a tile with this entity. */
export interface IActorLeaveHandler {
  readonly [ACTOR_LEAVE_HANDLER]: true;
  handleActorLeave(actor: any): void;
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

/** Symbol for collecting IOnTopActionHandler via symbol tag. */
export const ON_TOP_ACTION_HANDLER = Symbol.for('IOnTopActionHandler');

/** Entity that provides an action button when the player stands on it. */
export interface IOnTopActionHandler {
  readonly [ON_TOP_ACTION_HANDLER]: true;
  readonly onTopActionName: string;
  handleOnTopAction(): void;
}
