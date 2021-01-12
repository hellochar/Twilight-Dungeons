using System;
using System.Collections.Generic;
using System.Linq;

interface IModifier<T> {
  T Modify(T input);
}

interface IModifierProvider {
  IEnumerable<object> MyModifiers { get; }
}


static class Modifiers {
  /// this recurses into IModifierProviders that may live in the enumerable, and ultimately flattens them all into one
  public static IEnumerable<T> Of<T>(this IModifierProvider provider) {
    var enumerable = provider.MyModifiers;
    foreach (var s in enumerable) {
      if (s is T t) {
        yield return t;
      }
      if (s is IModifierProvider p && p != provider) {
        foreach (var subT in p.Of<T>()) {
          yield return subT;
        }
      }
    }
  }

  public static T Process<T>(IEnumerable<IModifier<T>> modifiers, T initial) {
    return modifiers.Aggregate(initial, (current, modifier) => modifier.Modify(current));
  }

  public static IEnumerable<IActionCostModifier> ActionCostModifiers(this IModifierProvider provider) {
    return Modifiers.Of<IActionCostModifier>(provider);
  }

  public static IEnumerable<IBaseActionModifier> BaseActionModifiers(this IModifierProvider provider) {
    return Modifiers.Of<IBaseActionModifier>(provider);
  }

  public static IEnumerable<IAttackDamageTakenModifier> AttackDamageTakenModifiers(this IModifierProvider provider) {
    return Modifiers.Of<IAttackDamageTakenModifier>(provider);
  }

  public static IEnumerable<IAnyDamageTakenModifier> AnyDamageTakenModifiers(this IModifierProvider provider) {
    return Modifiers.Of<IAnyDamageTakenModifier>(provider);
  }

  public static IEnumerable<IAttackDamageModifier> AttackDamageModifiers(this IModifierProvider provider) {
    return Modifiers.Of<IAttackDamageModifier>(provider);
  }

  public static IEnumerable<IStepModifier> StepModifiers(this IModifierProvider provider) {
    return Modifiers.Of<IStepModifier>(provider);
  }

  public static IEnumerable<IMaxHPModifier> MaxHPModifiers(this IModifierProvider provider) {
    return Modifiers.Of<IMaxHPModifier>(provider);
  }
}

interface IActionCostModifier : IModifier<ActionCosts> {}
interface IBaseActionModifier : IModifier<BaseAction> {}
interface IAttackDamageTakenModifier : IModifier<int> {}
interface IAnyDamageTakenModifier : IModifier<int> {}
interface IAttackDamageModifier : IModifier<int> {}
/// <summary>Kind of a hack to get declarative step()-ing.</summary>
interface IStepModifier : IModifier<object> {}
interface IMaxHPModifier : IModifier<int> {}
interface IPathfindingCostModifier : IModifier<float> {}


/// <summary>Called when the Actor is Killed.</summary>
public interface IActorKilledHandler {
  void OnKilled(Actor a);
}

interface IActionPerformedHandler {
  void HandleActionPerformed(BaseAction final, BaseAction initial);
}

// Called when the attached actor attacks a target
public interface IAttackHandler {
  void OnAttack(Body target);
}