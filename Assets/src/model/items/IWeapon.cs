public interface IWeapon {
  (int, int) AttackSpread { get; }
}

// Called when this weapon is used for an attack
public interface IAttackHandler {
  void OnAttack(Actor target);
}
