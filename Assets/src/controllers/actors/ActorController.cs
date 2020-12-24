using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class ActorController : MonoBehaviour, IEntityController, IEntityClickedHandler {
  private static GameObject hpChangeTextPrefab;
  public Actor actor;
  protected GameObject spriteObject;
  protected Animator animator;

  // Start is called before the first frame update
  public virtual void Start() {
    if (hpChangeTextPrefab == null) {
      hpChangeTextPrefab = Resources.Load<GameObject>("Effects/HP Change Text");
    }

    if (actor == null) {
      actor = GameModel.main.player;
    }

    spriteObject = transform.Find("Sprite")?.gameObject;
    animator = spriteObject?.GetComponent<Animator>();

    actor.OnTakeDamage += HandleTakeDamage;
    actor.OnHeal += HandleHeal;
    actor.OnAttack += HandleAttack;
    actor.OnAttackGround += HandleAttackGround;

    actor.OnSetTask += HandleSetTask;
    HandleSetTask(actor.task);

    actor.OnActionPerformed += HandleActionPerformed;

    actor.statuses.OnAdded += HandleStatusAdded;
    actor.statuses.OnRemoved += HandleStatusRemoved;
    foreach (var s in actor.statuses.list) {
      HandleStatusAdded(s);
    }

    Update();
  }

  // Update is called once per frame
  public virtual void Update() {
    float lerpSpeed = 20f / actor.GetActionCost(ActionType.MOVE);
    // sync positions
    if (Vector2.Distance(Util.getXY(this.transform.position), this.actor.pos) > 3) {
      this.transform.position = Util.withZ(this.actor.pos, this.transform.position.z);
    } else {
      this.transform.position = Util.withZ(Vector2.Lerp(Util.getXY(this.transform.position), actor.pos, lerpSpeed * Time.deltaTime), this.transform.position.z);
    }

    if (animator != null) {
      animator.speed = 1f / actor.baseActionCost;
    }
  }

  protected virtual void HandleStatusAdded(Status status) {
    var name = status.GetType().Name;
    animator?.SetBool(name, true);
    var obj = PrefabCache.Statuses.MaybeInstantiateFor(status, transform);
    if (obj != null) {
      obj.GetComponent<StatusController>().status = status;
    }
  }

  private void HandleStatusRemoved(Status status) {
    animator?.SetBool(status.GetType().Name, false);
  }

  private void HandleSetTask(ActorTask task) {
    if (animator != null) {
      if (task is SleepTask) {
        animator.SetBool("SleepingTask", true);
      } else {
        animator.SetBool("SleepingTask", false);
      }
    }
    var taskObject = PrefabCache.Tasks.MaybeInstantiateFor(task, transform);
    if (taskObject != null) {
      ActorTaskController actorTaskController = taskObject.GetComponent<ActorTaskController>();
      actorTaskController.actor = actor;
      actorTaskController.task = task;
    }
  }

  protected virtual void HandleActionPerformed(BaseAction action, BaseAction initial) {
    if (action is StruggleBaseAction) {
      animator?.SetTrigger("Struggled");
    } else if (action is AttackBaseAction attack) {
      PlayAttackAnimation(attack.target.pos);
    } else if (action is AttackGroundBaseAction attackGround) {
      PlayAttackAnimation(attackGround.targetPosition);
    } else if (action is GenericBaseAction g) {
      gameObject.AddComponent<PulseAnimation>();
    }
  }

  private void PlayAttackAnimation(Vector2Int pos) {
    if (spriteObject != null) {
      // go -1 to be "in front"
      var z = spriteObject.transform.position.z - 1;
      spriteObject.AddComponent<BumpAndReturn>().target = Util.withZ(pos, z);
    }
  }

  void HandleTakeDamage(int damage, int newHp, Actor source) {
    if (!actor.isVisible) {
      return;
    }
    GameObject hpChangeText = Instantiate(hpChangeTextPrefab, Util.withZ(actor.pos), Quaternion.identity);
    hpChangeText.GetComponentInChildren<HPChangeTextColor>().SetHPChange(-damage, false);

    if(damage > 0) {
      GameObject damagedSpritePrefab = Resources.Load<GameObject>("Effects/Damaged Sprite");
      Instantiate(damagedSpritePrefab, Util.withZ(actor.pos), Quaternion.identity);
    }
  }

  void HandleHeal(int heal, int newHp) {
    if (!actor.isVisible) {
      return;
    }
    GameObject healEffectPrefab = Resources.Load<GameObject>("Effects/Heal Effect");
    GameObject healEffect = Instantiate(healEffectPrefab, Util.withZ(actor.pos), Quaternion.identity, transform);
    healEffect.transform.localPosition = new Vector3(0, 0, 0);

    GameObject hpChangeText = Instantiate(hpChangeTextPrefab, Util.withZ(actor.pos), Quaternion.identity);
    hpChangeText.GetComponentInChildren<HPChangeTextColor>().SetHPChange(heal, true);
  }

  private void HandleAttack(int damage, Actor target) {}

  private void HandleAttackGround(Vector2Int pos) {
    GameObject attackSpritePrefab = Resources.Load<GameObject>("Effects/Attack Sprite");
    GameObject attackSprite = Instantiate(attackSpritePrefab, Util.withZ(pos), Quaternion.identity);
  }

  public virtual void PointerClick(PointerEventData pointerEventData) {
    Player player = GameModel.main.player;
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
