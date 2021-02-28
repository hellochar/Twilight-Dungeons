using UnityEngine;

public class DamageFlashController : MonoBehaviour, ITakeAnyDamageHandler {
  private Animator animator;

  void Start() {
    animator = GetComponent<Animator>();
    GameModel.main.player.nonserializedModifiers.Add(this);
    Update();
  }

  public void HandleTakeAnyDamage(int damage) {
    animator.SetTrigger("Hit");
  }

  // Update is called once per frame
  void Update() {
    var isLowHP = GameModel.main.player.hp <= 3;
    animator.SetBool("isLowHP", isLowHP);
  }
}
