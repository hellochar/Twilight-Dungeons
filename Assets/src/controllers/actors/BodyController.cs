using UnityEngine;
using UnityEngine.EventSystems;

public class BodyController : MonoBehaviour, IEntityController, IEntityClickedHandler {
  private static GameObject hpChangeTextPrefab;
  public Body body;
  public virtual void Start() {
    if (hpChangeTextPrefab == null) {
      hpChangeTextPrefab = Resources.Load<GameObject>("Effects/HP Change Text");
    }

    body.OnTakeAnyDamage += HandleTakeDamage;
    body.OnHeal += HandleHeal;
  }

  void HandleTakeDamage(int damage) {
    if (!body.isVisible) {
      return;
    }
    GameObject hpChangeText = Instantiate(hpChangeTextPrefab, Util.withZ(body.pos), Quaternion.identity);
    hpChangeText.GetComponentInChildren<HPChangeTextColor>().SetHPChange(-damage, false);

    if(damage > 0) {
      GameObject damagedSpritePrefab = Resources.Load<GameObject>("Effects/Damaged Sprite");
      Instantiate(damagedSpritePrefab, Util.withZ(body.pos), Quaternion.identity);
    }
  }

  void HandleHeal(int heal, int newHp) {
    if (!body.isVisible) {
      return;
    }
    GameObject healEffectPrefab = Resources.Load<GameObject>("Effects/Heal Effect");
    GameObject healEffect = Instantiate(healEffectPrefab, Util.withZ(body.pos), Quaternion.identity, transform);
    healEffect.transform.localPosition = new Vector3(0, 0, 0);

    GameObject hpChangeText = Instantiate(hpChangeTextPrefab, Util.withZ(body.pos), Quaternion.identity);
    hpChangeText.GetComponentInChildren<HPChangeTextColor>().SetHPChange(heal, true);
  }

  public virtual void PointerClick(PointerEventData pointerEventData) {
    Player player = GameModel.main.player;
    if (body.IsDead) {
      return; // don't do anything to dead actors
    }
    player.SetTasks(
      new MoveNextToTargetTask(player, body.pos),
      new AttackTask(player, body)
    );
  }
}
