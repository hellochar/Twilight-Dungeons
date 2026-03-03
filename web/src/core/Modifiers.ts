/**
 * Modifier chain system — TypeScript port of C# IModifier<T> / IModifierProvider.
 *
 * C# uses runtime type checks (`is IModifier<T>`). TypeScript uses unique symbols
 * as property keys for zero-cost type narrowing.
 *
 * Usage:
 *   class PoisonedStatus implements IAttackDamageTakenModifier {
 *     [ATTACK_DAMAGE_TAKEN_MOD] = true as const;
 *     modify(value: number): number { return value + 1; }
 *   }
 */

// --- Symbol tags for each modifier type ---

export const ACTION_COST_MOD = Symbol('ActionCostModifier');
export const BASE_ACTION_MOD = Symbol('BaseActionModifier');
export const ATTACK_DAMAGE_TAKEN_MOD = Symbol('AttackDamageTakenModifier');
export const ANY_DAMAGE_TAKEN_MOD = Symbol('AnyDamageTakenModifier');
export const ATTACK_DAMAGE_MOD = Symbol('AttackDamageModifier');
export const STEP_MOD = Symbol('StepModifier');
export const MAX_HP_MOD = Symbol('MaxHPModifier');
export const MOVEMENT_LAYER_MOD = Symbol('MovementLayerModifier');

// Forward-declared types used in modifier interfaces.
// These are imported by the concrete model files; declared here to avoid circular deps.
// ActionCosts and BaseAction will be defined in model/BaseAction.ts and re-exported.
// CollisionLayer is defined in core/types.ts.

export interface IModifier<T> {
  modify(value: T): T;
}

/** Anything that provides a list of modifier objects for the chain to traverse. */
export interface IModifierProvider {
  readonly myModifiers: Iterable<object | null | undefined>;
}

/**
 * Recursively collect all modifiers of a given type from a provider.
 * Mirrors C# `Modifiers.Of<T>(provider)` — recurses into sub-providers,
 * prevents double-counting when a sub-provider is also the root.
 */
export function collectModifiers<T>(
  provider: IModifierProvider | null | undefined,
  tag: symbol,
): T[] {
  if (!provider) return [];
  const result: T[] = [];
  const visited = new Set<object>();
  const seen = new Set<object>();
  visited.add(provider);
  _collect(provider, tag, result, visited, seen);
  return result;
}

function _collect(
  provider: IModifierProvider,
  tag: symbol,
  result: unknown[],
  visited: Set<object>,
  seen: Set<object>,
): void {
  for (const obj of provider.myModifiers) {
    if (obj == null) continue;

    // Check if this object has the tag (deduplicated: each object added at most once)
    if (typeof obj === 'object' && tag in obj && !seen.has(obj)) {
      result.push(obj);
      seen.add(obj);
    }

    // Recurse into sub-providers (skip already-visited to prevent cycles)
    if (
      typeof obj === 'object' &&
      !visited.has(obj) &&
      'myModifiers' in obj &&
      typeof (obj as IModifierProvider).myModifiers !== 'undefined'
    ) {
      visited.add(obj);
      _collect(obj as IModifierProvider, tag, result, visited, seen);
    }
  }
}

/**
 * Fold modifiers over an initial value.
 * Mirrors C# `Modifiers.Process(modifiers, initial)`.
 */
export function processModifiers<T>(
  modifiers: IModifier<T>[],
  initial: T,
): T {
  return modifiers.reduce((val, mod) => mod.modify(val), initial);
}

// --- Convenience typed interfaces ---
// Concrete classes implement these + set the symbol tag.
// Example:
//   class Foo implements IActionCostModifier {
//     [ACTION_COST_MOD] = true as const;
//     modify(value: ActionCosts): ActionCosts { ... }
//   }

// These will use the actual types once BaseAction.ts and types.ts are defined.
// For now, we define the marker interface pattern; the generic parameter
// is resolved at the usage site.

export interface IActionCostModifier extends IModifier<any> {
  [ACTION_COST_MOD]: true;
}
export interface IBaseActionModifier extends IModifier<any> {
  [BASE_ACTION_MOD]: true;
}
export interface IAttackDamageTakenModifier extends IModifier<number> {
  [ATTACK_DAMAGE_TAKEN_MOD]: true;
}
export interface IAnyDamageTakenModifier extends IModifier<number> {
  [ANY_DAMAGE_TAKEN_MOD]: true;
}
export interface IAttackDamageModifier extends IModifier<number> {
  [ATTACK_DAMAGE_MOD]: true;
}
export interface IStepModifier extends IModifier<object> {
  [STEP_MOD]: true;
}
export interface IMaxHPModifier extends IModifier<number> {
  [MAX_HP_MOD]: true;
}
export interface IMovementLayerModifier extends IModifier<number> {
  [MOVEMENT_LAYER_MOD]: true;
}
