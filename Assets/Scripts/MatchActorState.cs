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
    actor.OnAttack += HandleAttack;
    actor.OnAttackGround += HandleAttackGround;
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

  void HandleTakeDamage(int damage, int newHp, Actor source) {
    GameObject damageTextPrefab = Resources.Load<GameObject>("UI/Damage Text");
    GameObject damageText = Instantiate(damageTextPrefab, Util.withZ(actor.pos), Quaternion.identity);
    damageText.GetComponentInChildren<TMPro.TMP_Text>().text = $"-{damage}";
  }

  private void HandleAttack(int damage, Actor target) {
    /// do nothing for now
  }

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

public class AttackAction : ActorAction {
  public AttackAction(Actor actor, Actor _target) : base(actor) {
    target = _target;
  }

  public Actor target { get; }

  public override int Perform() {
    if (actor.IsNextTo(target)) {
      actor.Attack(target);
    }
    return base.Perform();
  }
}

public class AttackGroundAction : ActorAction {

  public AttackGroundAction(Actor actor, Vector2Int targetPosition) : base(actor) {
    TargetPosition = targetPosition;
  }

  public Vector2Int TargetPosition { get; }

  public override int Perform() {
    if (actor.IsNextTo(TargetPosition)) {
      actor.AttackGround(TargetPosition);
    }
    return base.Perform();
  }
}