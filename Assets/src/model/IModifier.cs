using System;
using System.Collections.Generic;
using System.Linq;

interface IModifier<T> {
  T Modify(T input);
}

static class Modifiers {
  public static IEnumerable<T> ModifiersFor<T, I>(IEnumerable<object> enumerable) where T : IModifier<I> {
    return enumerable.Where((s) => s is T).Cast<T>();
  }

  public static T Process<T>(IEnumerable<IModifier<T>> modifiers, T initial) {
    return modifiers.Aggregate(initial, (current, modifier) => modifier.Modify(current));
  }

  public static IEnumerable<IActionCostModifier> ActionCostModifiers(IEnumerable<object> enumerable) {
    return Modifiers.ModifiersFor<IActionCostModifier, ActionCosts>(enumerable);
  }

  public static IEnumerable<IBaseActionModifier> BaseActionModifiers(IEnumerable<object> enumerable) {
    return Modifiers.ModifiersFor<IBaseActionModifier, BaseAction>(enumerable);
  }

  public static IEnumerable<IDamageTakenModifier> DamageTakenModifiers(IEnumerable<object> enumerable) {
    return Modifiers.ModifiersFor<IDamageTakenModifier, int>(enumerable);
  }

  public static IEnumerable<IAttackDamageModifier> AttackDamageModifiers(IEnumerable<object> enumerable) {
    return Modifiers.ModifiersFor<IAttackDamageModifier, int>(enumerable);
  }
}

interface IActionCostModifier : IModifier<ActionCosts> {}
interface IBaseActionModifier : IModifier<BaseAction> {}
interface IDamageTakenModifier : IModifier<int> {}
interface IAttackDamageModifier : IModifier<int> {}
