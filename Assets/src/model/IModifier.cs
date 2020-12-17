using System;
using System.Collections.Generic;
using System.Linq;

interface IModifier<T> {
  T Modify(T input);
}

static class Modifiers {
  public static IEnumerable<T> Of<T>(IEnumerable<object> enumerable) {
    return enumerable.Where((s) => s is T).Cast<T>();
  }

  public static T Process<T>(IEnumerable<IModifier<T>> modifiers, T initial) {
    return modifiers.Aggregate(initial, (current, modifier) => modifier.Modify(current));
  }

  public static IEnumerable<IActionCostModifier> ActionCostModifiers(IEnumerable<object> enumerable) {
    return Modifiers.Of<IActionCostModifier>(enumerable);
  }

  public static IEnumerable<IBaseActionModifier> BaseActionModifiers(IEnumerable<object> enumerable) {
    return Modifiers.Of<IBaseActionModifier>(enumerable);
  }

  public static IEnumerable<IDamageTakenModifier> DamageTakenModifiers(IEnumerable<object> enumerable) {
    return Modifiers.Of<IDamageTakenModifier>(enumerable);
  }

  public static IEnumerable<IAttackDamageModifier> AttackDamageModifiers(IEnumerable<object> enumerable) {
    return Modifiers.Of<IAttackDamageModifier>(enumerable);
  }

  public static IEnumerable<IStepModifier> StepModifiers(IEnumerable<object> enumerable) {
    return Modifiers.Of<IStepModifier>(enumerable);
  }
}

interface IActionCostModifier : IModifier<ActionCosts> {}
interface IBaseActionModifier : IModifier<BaseAction> {}
interface IDamageTakenModifier : IModifier<int> {}
interface IAttackDamageModifier : IModifier<int> {}
/// <summary>Kind of a hack to get declarative step()-ing.</summary>
interface IStepModifier : IModifier<object> {}
