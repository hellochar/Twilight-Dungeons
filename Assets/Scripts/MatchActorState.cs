using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MatchActorState : MonoBehaviour, IPointerClickHandler {
  public Actor actor;
  private static GameObject hpChangeTextPrefab;

  // Start is called before the first frame update
  public virtual void Start() {
    if (hpChangeTextPrefab == null) {
      hpChangeTextPrefab = Resources.Load<GameObject>("UI/HP Change Text");
    }
    if (actor == null) {
      actor = GameModel.main.player;
    }
    actor.OnTakeDamage += HandleTakeDamage;
    actor.OnHeal += HandleHeal;
    actor.OnAttack += HandleAttack;
    actor.OnAttackGround += HandleAttackGround;
    actor.OnSetTask += HandleSetAction;
    Update();
  }

  // Update is called once per frame
  public virtual void Update() {
    // sync positions
    if (Vector2.Distance(Util.getXY(this.transform.position), this.actor.pos) > 3) {
      this.transform.position = Util.withZ(this.actor.pos, this.transform.position.z);
    } else {
      this.transform.position = Util.withZ(Vector2.Lerp(Util.getXY(this.transform.position), actor.pos, 20f * Time.deltaTime), this.transform.position.z);
    }
  }

  private void HandleSetAction(ActorTask task) {
    if (task != null) {
      GameObject prefab = GetPrefabForTask(task);
      if (prefab != null) {
        Instantiate(prefab, transform.position, Quaternion.identity, transform);
      }
    }
  }

  private static Dictionary<Type, GameObject> taskPrefabCache = new Dictionary<Type, GameObject>();
  private GameObject GetPrefabForTask(ActorTask obj) {
    var type = obj.GetType();
    if (!taskPrefabCache.ContainsKey(type)) {
      // attempt to load it
      var name = type.Name;
      var prefabOrNull = Resources.Load<GameObject>($"UI/Tasks/{name}");
      taskPrefabCache.Add(type, prefabOrNull);
    }
    return taskPrefabCache[type];
  }

  void HandleTakeDamage(int damage, int newHp, Actor source) {
    if (!actor.visible) {
      return;
    }
    GameObject hpChangeText = Instantiate(hpChangeTextPrefab, Util.withZ(actor.pos), Quaternion.identity);
    hpChangeText.GetComponentInChildren<HPChangeTextColor>().SetHPChange(-damage, false);

    if(damage > 0) {
      GameObject damagedSpritePrefab = Resources.Load<GameObject>("UI/Damaged Sprite");
      Instantiate(damagedSpritePrefab, Util.withZ(actor.pos), Quaternion.identity);
    }
  }

  void HandleHeal(int heal, int newHp) {
    if (!actor.visible) {
      return;
    }
    GameObject healEffectPrefab = Resources.Load<GameObject>("UI/Heal Effect");
    GameObject healEffect = Instantiate(healEffectPrefab, Util.withZ(actor.pos), Quaternion.identity);

    GameObject hpChangeText = Instantiate(hpChangeTextPrefab, Util.withZ(actor.pos), Quaternion.identity);
    hpChangeText.GetComponentInChildren<HPChangeTextColor>().SetHPChange(heal, true);
  }

  private void HandleAttack(int damage, Actor target) {}

  private void HandleAttackGround(Vector2Int targetPosition, Actor occupant) {
    GameObject attackSpritePrefab = Resources.Load<GameObject>("UI/Attack Sprite");
    GameObject attackSprite = Instantiate(attackSpritePrefab, Util.withZ(targetPosition), Quaternion.identity);
  }

  public virtual void OnPointerClick(PointerEventData pointerEventData) {
    Player player = GameModel.main.player;
    // on clicking self, wait for 1 turn
    if (actor == player) {
      player.task = new WaitTask(player, 1);
      return;
    }
    // depending on the faction:
    // (1) ally or neutral - walk to
    // (2) enemy - attack
    switch (actor.faction) {
      case Faction.Ally:
      case Faction.Neutral:
        player.task = new MoveNextToTargetTask(player, actor.pos);
      break;
      case Faction.Enemy:
        player.SetTasks(
          new MoveNextToTargetTask(player, actor.pos),
          new AttackTask(player, actor)
        );
      break;
    }
  }
}
