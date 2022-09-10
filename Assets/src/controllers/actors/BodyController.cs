using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class BodyController : MonoBehaviour, IEntityController, IPlayerInteractHandler, ITakeAnyDamageHandler, IHealHandler {
  [NonSerialized]
  public Body body;
  protected GameObject sprite;
  public bool showDamageMarks = true;

  public virtual void Start() {
    body.nonserializedModifiers.Add(this);
    sprite = transform.Find("Sprite")?.gameObject;
  }

  public virtual void HandleTakeAnyDamage(int damage) {
    // if (!body.isVisible) {
    //   return;
    // }

    if (showDamageMarks) {
      GameObject hpChangeText = Instantiate(PrefabCache.Effects.GetPrefabFor("HP Change Text"), Util.withZ(body.pos), Quaternion.identity);
      hpChangeText.GetComponentInChildren<HPChangeTextColor>().SetHPChange(-damage, false, !(body is Player));

      if(damage > 0) {
        GameObject damagedSpritePrefab = PrefabCache.Effects.GetPrefabFor("Damaged Sprite");
        Instantiate(damagedSpritePrefab, Util.withZ(body.pos), Quaternion.identity);
      }
    }
  }

  public virtual void HandleHeal(int heal) {
    if (!body.isVisible) {
      return;
    }
    GameObject hpChangeText = Instantiate(PrefabCache.Effects.GetPrefabFor("HP Change Text"), Util.withZ(body.pos), Quaternion.identity);
    hpChangeText.GetComponentInChildren<HPChangeTextColor>().SetHPChange(heal, false, !(body is Player));

    GameObject healEffectPrefab = PrefabCache.Effects.GetPrefabFor("Heal Effect");
    GameObject healEffect = Instantiate(healEffectPrefab, Util.withZ(body.pos), Quaternion.identity, transform);
    healEffect.transform.localPosition = new Vector3(0, 0, 0);
  }

  public virtual void HandleInteracted(PointerEventData pointerEventData) {
    Player player = GameModel.main.player;
    if (body.IsDead) {
      return; // don't do anything to dead actors
    }
    player.SetTasks(
      new ChaseTargetTask(player, body),
      new AttackTask(player, body)
    );
  }
}
