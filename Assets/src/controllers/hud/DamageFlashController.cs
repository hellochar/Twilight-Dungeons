using UnityEngine;

public class DamageFlashController : MonoBehaviour, ITakeAnyDamageHandler {
  private Animator animator;

  void Start() {
    animator = GetComponent<Animator>();
    GameModel.main.player.nonserializedModifiers.Add(this);
    Update();
  }

  public void HandleTakeAnyDamage(int damage) {
    if (damage > 0) {
      animator.SetTrigger("Hit");
    }
  }

  // Update is called once per frame
  void Update() {
    var isLowHP = GameModel.main.player.hp <= 3 && !GameModel.main.player.IsDead;
    animator.SetBool("isLowHP", isLowHP);
  }
}
