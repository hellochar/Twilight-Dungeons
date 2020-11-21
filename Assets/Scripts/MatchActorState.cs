using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MatchActorState : MonoBehaviour, IPointerClickHandler {
  public Actor actor;
  public new SpriteRenderer renderer;

  // Start is called before the first frame update
  public virtual void Start() {
    if (actor == null) {
      actor = GameModel.main.player;
    }
    actor.OnTakeDamage += HandleTakeDamage;
    actor.OnHeal += HandleHeal;
    actor.OnAttack += HandleAttack;
    actor.OnAttackGround += HandleAttackGround;
    actor.OnSetAction += HandleSetAction;
    this.renderer = GetComponent<SpriteRenderer>();
    this.transform.position = Util.withZ(this.actor.pos);
  }

  // Update is called once per frame
  public virtual void Update() {
    // sync positions
    if (Vector2.Distance(Util.getXY(this.transform.position), this.actor.pos) > 3) {
      this.transform.position = Util.withZ(this.actor.pos, this.transform.position.z);
    } else {
      this.transform.position = Util.withZ(Vector2.Lerp(Util.getXY(this.transform.position), actor.pos, 20f * Time.deltaTime), this.transform.position.z);
    }
    // don't need this because the renderer is sprite-masked
    // if (renderer != null) {
    //   renderer.enabled = actor.visible;
    // }
  }

  private void HandleSetAction(ActorAction obj) {
    if (obj != null) {
      GameObject prefab = GetPrefabForAction(obj);
      if (prefab != null) {
        Instantiate(prefab, transform.position, Quaternion.identity, transform);
      }
    }
  }

  private static Dictionary<Type, GameObject> actionPrefabCache = new Dictionary<Type, GameObject>();
  private GameObject GetPrefabForAction(ActorAction obj) {
    var type = obj.GetType();
    if (!actionPrefabCache.ContainsKey(type)) {
      // attempt to load it
      var name = type.Name;
      var prefabOrNull = Resources.Load<GameObject>($"UI/Actions/{name}");
      actionPrefabCache.Add(type, prefabOrNull);
    }
    return actionPrefabCache[type];
  }

  void HandleTakeDamage(int damage, int newHp, Actor source) {
    GameObject damageTextPrefab = Resources.Load<GameObject>("UI/Damage Text");
    GameObject damageText = Instantiate(damageTextPrefab, Util.withZ(actor.pos), Quaternion.identity);
    damageText.GetComponentInChildren<TMPro.TMP_Text>().text = $"-{damage}";
  }

  void HandleHeal(int amount, int newHp) {
    GameObject healEffectPrefab = Resources.Load<GameObject>("UI/Heal Effect");
    GameObject healEffect = Instantiate(healEffectPrefab, Util.withZ(actor.pos), Quaternion.identity);

    GameObject damageTextPrefab = Resources.Load<GameObject>("UI/Damage Text");
    GameObject damageText = Instantiate(damageTextPrefab, Util.withZ(actor.pos), Quaternion.identity);
    TMPro.TMP_Text text = damageText.GetComponentInChildren<TMPro.TMP_Text>();
    text.text = $"{amount}";
    text.color = HealTextColor;
  }

  public readonly static Color HealTextColor = new Color(0.109082f, 0.9803922f, 0.04313723f);

  private void HandleAttack(int damage, Actor target) {}

  private void HandleAttackGround(Vector2Int targetPosition, Actor occupant) {
    GameObject attackSpritePrefab = Resources.Load<GameObject>("UI/Attack Sprite");
    GameObject attackSprite = Instantiate(attackSpritePrefab, Util.withZ(targetPosition), Quaternion.identity);
  }

  public virtual void OnPointerClick(PointerEventData pointerEventData) {
    // depending on the faction:
    // (1) ally or neutral - walk to
    // (2) enemy - attack
    Player player = GameModel.main.player;
    switch (actor.faction) {
      case Faction.Ally:
      case Faction.Neutral:
        player.action = new MoveNextToTargetAction(player, actor.pos);
      break;
      case Faction.Enemy:
        player.SetActions(
          new MoveNextToTargetAction(player, actor.pos),
          new AttackAction(player, actor)
        );
      break;
    }
  }
}
