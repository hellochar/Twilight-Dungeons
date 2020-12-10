using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class ActorController : MonoBehaviour, IPointerClickHandler {
  private static GameObject hpChangeTextPrefab;
  public Actor actor;
  private Animator animator;

  // Start is called before the first frame update
  public virtual void Start() {
    if (hpChangeTextPrefab == null) {
      hpChangeTextPrefab = Resources.Load<GameObject>("UI/HP Change Text");
    }

    if (actor == null) {
      actor = GameModel.main.player;
    }

    animator = transform.Find("Sprite")?.GetComponent<Animator>();

    actor.OnTakeDamage += HandleTakeDamage;
    actor.OnHeal += HandleHeal;
    actor.OnAttack += HandleAttack;
    actor.OnAttackGround += HandleAttackGround;
    actor.OnSetTask += HandleSetTask;
    HandleSetTask(actor.task);
    actor.OnActionPerformed += HandleActionPerformed;
    actor.statuses.OnAdded += HandleStatusAdded;

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

    if (animator != null) {
      animator.speed = 1f / actor.baseActionCost;
    }
  }

  private void HandleStatusAdded(Status status) {
    var obj = PrefabCache.Statuses.MaybeInstantiateFor(status, transform);
    if (obj != null) {
      obj.GetComponent<StatusController>().status = status;
    }
  }

  private void HandleSetTask(ActorTask task) {
    PrefabCache.Tasks.MaybeInstantiateFor(task, transform);
  }

  private void HandleActionPerformed(BaseAction action, BaseAction initial) {
    if (action is StruggleBaseAction) {
      animator?.SetTrigger("Struggled");
    }
  }

  void HandleTakeDamage(int damage, int newHp, Actor source) {
    if (!actor.isVisible) {
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
    if (!actor.isVisible) {
      return;
    }
    GameObject healEffectPrefab = Resources.Load<GameObject>("UI/Heal Effect");
    GameObject healEffect = Instantiate(healEffectPrefab, Util.withZ(actor.pos), Quaternion.identity, transform);
    healEffect.transform.localPosition = new Vector3(0, 0, 0);

    GameObject hpChangeText = Instantiate(hpChangeTextPrefab, Util.withZ(actor.pos), Quaternion.identity);
    hpChangeText.GetComponentInChildren<HPChangeTextColor>().SetHPChange(heal, true);
  }

  private void HandleAttack(int damage, Actor target) {}

  private void HandleAttackGround(Vector2Int pos) {
    GameObject attackSpritePrefab = Resources.Load<GameObject>("UI/Attack Sprite");
    GameObject attackSprite = Instantiate(attackSpritePrefab, Util.withZ(pos), Quaternion.identity);
  }

  public virtual void OnPointerClick(PointerEventData pointerEventData) {
    Player player = GameModel.main.player;
    // on clicking self, wait for 1 turn
    if (actor == player) {
      player.task = new WaitTask(player, 1);
      return;
    }
    if (actor.IsDead) {
      return; // don't do anything to dead actors
    }
    // depending on the faction:
    // (1) ally or neutral - walk to
    // (2) enemy - attack
    switch (actor.faction) {
      case Faction.Ally:
        player.task = new MoveNextToTargetTask(player, actor.pos);
        break;
      case Faction.Neutral:
      case Faction.Enemy:
        player.SetTasks(
          new MoveNextToTargetTask(player, actor.pos),
          new AttackTask(player, actor)
        );
      break;
    }
  }
}