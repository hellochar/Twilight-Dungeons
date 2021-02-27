using UnityEngine;

public class DamageFlashController : MonoBehaviour {
  private Animator animator;

  void Start() {
    animator = GetComponent<Animator>();
    Update();
  }

  // Update is called once per frame
  void Update() {
    var isLowHP = GameModel.main.player.hp <= 3;
    animator.SetBool("isLowHP", isLowHP);
  }
}
