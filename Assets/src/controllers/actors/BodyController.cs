using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class BodyController : MonoBehaviour, IEntityController, IEntityClickedHandler, ITakeAnyDamageHandler, IHealHandler {
  private static GameObject hpChangeTextPrefab;
  [NonSerialized]
  public Body body;
  protected GameObject sprite;
  protected GameObject damageContainer;
  public virtual void Start() {
    if (hpChangeTextPrefab == null) {
      hpChangeTextPrefab = Resources.Load<GameObject>("Effects/HP Change Text");
    }

    sprite = transform.Find("Sprite")?.gameObject;
    damageContainer = sprite?.transform.Find("Damage Container").gameObject;
  }

  public virtual void HandleTakeAnyDamage(int damage) {
    if (!body.isVisible) {
      return;
    }
    GameObject hpChangeText = Instantiate(hpChangeTextPrefab, Util.withZ(body.pos), Quaternion.identity);
    hpChangeText.GetComponentInChildren<HPChangeTextColor>().SetHPChange(-damage, false);

    if(damage > 0) {
      GameObject damagedSpritePrefab = Resources.Load<GameObject>("Effects/Damaged Sprite");
      Instantiate(damagedSpritePrefab, Util.withZ(body.pos), Quaternion.identity);
    }
    // UpdateDamageTicks();
  }

  public virtual void HandleHeal(int heal) {
    if (!body.isVisible) {
      return;
    }
    GameObject healEffectPrefab = Resources.Load<GameObject>("Effects/Heal Effect");
    GameObject healEffect = Instantiate(healEffectPrefab, Util.withZ(body.pos), Quaternion.identity, transform);
    healEffect.transform.localPosition = new Vector3(0, 0, 0);

    GameObject hpChangeText = Instantiate(hpChangeTextPrefab, Util.withZ(body.pos), Quaternion.identity);
    hpChangeText.GetComponentInChildren<HPChangeTextColor>().SetHPChange(heal, true);
    // UpdateDamageTicks();
  }

  void UpdateDamageTicks() {
    GameModel.main.EnqueueEvent(() => {
      if (damageContainer == null) {
        return;
      }
      // int newDamage = body.maxHp - body.hp;
      // var spriteRenderer = damageContainer.GetComponent<SpriteRenderer>();
      // if (newDamage == 0) {
      //   spriteRenderer.sprite = null;
      // } else {
      //   var spriteName = $"damage-states_{Mathf.Clamp(newDamage - 1, 0, 9)}";
      //   spriteRenderer.sprite = MasterSpriteAtlas.atlas.GetSprite(spriteName);
      // }
      int newDamage = ((float) body.hp / body.maxHp > 0.5) ? 0 : 1;
      // remove extra damage ticks
      for (int i = damageContainer.transform.childCount; i > newDamage; i--) {
        Destroy(damageContainer.transform.GetChild(i - 1).gameObject);
      }
      // create new damage
      for (int i = damageContainer.transform.childCount; i < newDamage; i++) {
        var damageTickPrefab = PrefabCache.Effects.GetPrefabFor("Damage Tick");
        var rotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(45 / 2, 45 * 1.5f));
        var damageTick = Instantiate(damageTickPrefab, new Vector3(), rotation, damageContainer.transform);
        damageTick.transform.localPosition = new Vector3(0, -0.25f, 0);
        // damageTick.transform.localPosition = Random.insideUnitCircle * Random.insideUnitCircle * Random.insideUnitCircle * Random.insideUnitCircle * 0.5f;
      }
    });
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
