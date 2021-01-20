using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class ActorController : BodyController {
  public Actor actor => (Actor) body;
  protected GameObject spriteObject;
  protected Animator animator;

  // Start is called before the first frame update
  public override void Start() {
    if (body == null) {
      body = GameModel.main.player;
    }

    base.Start();

    spriteObject = transform.Find("Sprite")?.gameObject;
    animator = spriteObject?.GetComponent<Animator>();

    actor.OnDeath += HandleDeath;

    actor.OnAttackGround += HandleAttackGround;

    actor.OnSetTask += HandleSetTask;
    HandleSetTask(actor.task);

    actor.OnMove += HandleMove;
    actor.OnActionPerformed += HandleActionPerformed;

    actor.statuses.OnAdded += HandleStatusAdded;
    actor.statuses.OnRemoved += HandleStatusRemoved;
    foreach (var s in actor.statuses.list) {
      HandleStatusAdded(s);
    }

    // timeNextActionText = PrefabCache.UI.Instantiate("WorldText", transform);
    // timeNextActionText.GetComponent<TMPro.TMP_Text>().text = actor.timeNextAction.ToString();

    Update();
  }

  private void HandleMove(Vector2Int arg1, Vector2Int arg2) {
    AudioClipStore.main.move.PlayAtPoint(transform.position, 0.5f);
  }

  private void HandleDeath() {
    AudioClipStore.main.death.PlayAtPoint(transform.position);
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

    // timeNextActionText.GetComponent<TMPro.TMP_Text>().text = actor.timeNextAction.ToString();
  }

  protected virtual void HandleStatusAdded(Status status) {
    var name = status.GetType().Name;
    if (animator != null) {
      animator.SetBool(name, true);
    }
    var obj = PrefabCache.Statuses.MaybeInstantiateFor(status, transform);
    if (obj != null) {
      obj.GetComponent<StatusController>().status = status;
    }
  }

  private void HandleStatusRemoved(Status status) {
    if (animator != null) {
      animator.logWarnings = false;
      animator?.SetBool(status.GetType().Name, false);
    }
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
      AudioClipStore.main.attack.PlayAtPoint(transform.position);
    } else if (action is AttackGroundBaseAction attackGround) {
      PlayAttackAnimation(attackGround.targetPosition);
    } else if (action is GenericBaseAction g) {
      gameObject.AddComponent<PulseAnimation>();
    }
  }

  /// also triggers audio
  private void PlayAttackAnimation(Vector2Int pos) {
    if (spriteObject != null) {
      // go -1 to be "in front"
      var z = spriteObject.transform.position.z - 1;
      spriteObject.AddComponent<BumpAndReturn>().target = Util.withZ(pos, z);
    }
  }

  private void HandleAttackGround(Vector2Int pos) {
    GameObject attackSpritePrefab = Resources.Load<GameObject>("Effects/Attack Sprite");
    GameObject attackSprite = Instantiate(attackSpritePrefab, Util.withZ(pos), Quaternion.identity);
    if (actor.floor.bodies[pos] == null) {
      AudioClipStore.main.attackMiss.PlayAtPoint(transform.position);
    } else {
      AudioClipStore.main.attack.PlayAtPoint(transform.position);
    }
  }

  public override void PointerClick(PointerEventData pointerEventData) {
    Player player = GameModel.main.player;
    if (actor.IsDead) {
      return; // don't do anything to dead actors
    }
    // depending on the faction:
    // (1) ally or neutral - walk to
    // (2) enemy - attack
    switch (actor.faction) {
      case Faction.Ally:
        if (player.IsNextTo(actor)) {
          player.task = new GenericTask(player, (_) => {
            player.SwapPositions(actor);
          }, ActionType.MOVE);
        } else {
          player.task = new MoveNextToTargetTask(player, actor.pos);
        }
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
