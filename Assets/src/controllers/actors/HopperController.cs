using UnityEngine;

public class HopperController : ActorController {
  public Hopper hopper => (Hopper) actor;
  public Sprite healthySprite, damagedSprite;

  public override void Update() {
    base.Update();
    var isDamaged = hopper.hp < hopper.maxHp;
    sprite.GetComponent<SpriteRenderer>().sprite = isDamaged ? damagedSprite : healthySprite;
    // damagedSprite.SetActive(showDamageMark);
  }
}
