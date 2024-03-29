﻿using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class ActorController : BodyController,
  IActionPerformedHandler, IStatusAddedHandler, IStatusRemovedHandler, IDeathHandler {
  public Actor actor => (Actor)body;
  public Color bloodColor = new Color(0.75f, 0, 0, 0.5f);
  protected Animator animator;
  public bool hideWaitTask = false;
  public GameObject statuses;
  // if this is not null, there's a code animation controlling this actor
  // and we shouldn't clobber it
  protected Coroutine currentAnimation;

  // Start is called before the first frame update
  public override void Start() {
    if (body == null) {
      body = GameModel.main.player;
    }

    base.Start();

    animator = sprite?.GetComponent<Animator>();
    statuses = transform.Find("Statuses")?.gameObject;

    actor.OnAttackGround += HandleAttackGround;

    actor.OnSetTask += HandleSetTask;
    HandleSetTask(actor.task);

    /// match statuses that already exist
    foreach (var s in actor.statuses.list) {
      HandleStatusAdded(s);
    }

    // timeNextActionText = PrefabCache.UI.Instantiate("WorldText", transform);
    // timeNextActionText.GetComponent<TMPro.TMP_Text>().text = actor.timeNextAction.ToString();

    Update();
  }

  public void OnDestroy() {
    actor.OnAttackGround -= HandleAttackGround;
    actor.OnSetTask -= HandleSetTask;
  }

  public void ShowSpeechBubble() {
    PrefabCache.Effects.Instantiate("Speech", transform);
  }

  public override void HandleTakeAnyDamage(int dmg) {
    base.HandleTakeAnyDamage(dmg);

    if (dmg > 0) {
      var tileGameObject = GameModelController.main.CurrentFloorController.GameObjectFor(actor.tile);
      var blood = PrefabCache.Effects.Instantiate("Blood", tileGameObject.transform);
      blood.GetComponentInChildren<SpriteRenderer>().color = bloodColor;
      var scale = Random.Range(0.25f, 0.5f);
      blood.transform.localScale = new Vector3(scale, scale, 1);
      blood.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(0, 360));
      blood.transform.localPosition = Random.insideUnitCircle * 0.25f;
    }
  }

  public virtual void HandleDeath(Entity source) {
    var isNextToPlayer = actor.IsNextTo(GameModel.main.player);
    var wasKilledByPlayerOrAlly = source is Actor a && a.faction == Faction.Ally;
    var wasKilledBySelf = source == actor;
    if ((wasKilledByPlayerOrAlly || isNextToPlayer) && !wasKilledBySelf) {
      var audioSource = GetComponent<AudioSource>();
      if (audioSource != null) {
        audioSource.pitch = Random.Range(0.75f, 1.25f);
        audioSource.Play();
      }
    }
    PrefabCache.Effects.Instantiate("LightCast", transform);
  }

  // Update is called once per frame
  public virtual void Update() {
    float lerpSpeed = 16f / actor.GetActionCost(ActionType.MOVE);
    if (currentAnimation == null) {
      // sync positions
      if (Vector2.Distance(Util.getXY(this.transform.position), this.actor.pos) > 3) {
        this.transform.position = Util.withZ(this.actor.pos, this.transform.position.z);
      } else {
        this.transform.position = Util.withZ(Vector2.Lerp(Util.getXY(this.transform.position), actor.pos, lerpSpeed * Time.deltaTime), this.transform.position.z);
      }
    }

    if (animator != null) {
      animator.speed = 1f / actor.baseActionCost;
    }

    // timeNextActionText.GetComponent<TMPro.TMP_Text>().text = actor.timeNextAction.ToString();
  }

  public virtual void HandleStatusAdded(Status status) {
    if (animator != null) {
      animator.SetBool(status.GetType().Name, true);
    }
    var obj = PrefabCache.Statuses.MaybeInstantiateFor(status, statuses.transform);
    if (obj != null) {
      obj.GetComponent<StatusController>().status = status;
    }
  }

  public virtual void HandleStatusRemoved(Status status) {
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
    if (hideWaitTask && task is WaitTask) {
      /// do not double-show a wait icon on the player; a bigger one is created by PlayerController
      return;
    }
    var taskObject = PrefabCache.Tasks.MaybeInstantiateFor(task, transform);
    if (taskObject != null) {
      ActorTaskController actorTaskController = taskObject.GetComponent<ActorTaskController>();
      actorTaskController.actor = actor;
      actorTaskController.task = task;
    }
  }

  public virtual void HandleActionPerformed(BaseAction action, BaseAction initial) {
    if (action is StruggleBaseAction) {
      animator?.SetTrigger("Struggled");
    } else if (action is AttackBaseAction attack) {
      PlayAttackAnimation(attack.target.pos);
    } else if (action is AttackGroundBaseAction attackGround) {
      PlayAttackAnimation(attackGround.targetPosition);
    } else if (action is GenericBaseAction g) {
      gameObject.AddComponent<PulseAnimation>();
    } else if (action is JumpBaseAction j) {
      // cancel
      if (currentAnimation != null) {
        StopCoroutine(currentAnimation);
      }
      currentAnimation = StartCoroutine(PlayJumpAnimation(j.pos));
    }
  }

  private void PlayAttackAnimation(Vector2Int pos) {
    if (sprite != null) {
      // go -1 to be "in front"
      var z = sprite.transform.position.z - 1;
      sprite.AddComponent<BumpAndReturn>().target = Util.withZ(pos, z);
    }
  }

  private IEnumerator PlayJumpAnimation(Vector2 endPosition) {
    var startPosition = Util.getXY(transform.position);
    var D = Vector2.Distance(startPosition, endPosition);
    // parabolic arc for height H at midpoint
    // a = -4H/D^2
    // b = 4H/D

    // we get to choose what looks nice
    var H = D * 0.5f;
    var a = -4 * H / (D * D);
    var b = 4 * H / D;
    animator.enabled = false;
    yield return StartCoroutine(Transitions.Animate(0.5f, (tNorm) => {
        // do a parabolic arc where "z" is upwards
        var t = tNorm * D;
        var z = a * t * t + b * t;

        Vector2 worldPos = Vector2.Lerp(startPosition, endPosition, tNorm);
        // map "z" to moving upwards on the screen
        worldPos.y += z;

        transform.position = Util.withZ(worldPos, transform.position.z);
      },
      () => {
        currentAnimation = null;
        animator.enabled = true;
      }
    ));
  }

  private void HandleAttackGround(Vector2Int pos) {
    GameObject attackSpritePrefab = PrefabCache.Effects.GetPrefabFor("Attack Sprite");
    GameObject attackSprite = Instantiate(attackSpritePrefab, Util.withZ(pos), Quaternion.identity);
  }

  public override PlayerInteraction GetPlayerInteraction(PointerEventData pointerEventData) {
    Player player = GameModel.main.player;
    if (actor.IsDead) {
      return null; // don't do anything to dead actors
    }
    // depending on the faction:
    // (1) ally or neutral - walk to
    // (2) enemy - attack
    switch (actor.faction) {
      case Faction.Ally:
        if (player.IsNextTo(actor)) {
          return new SetTasksPlayerInteraction(new SwapPositionsTask(player, actor));
        } else {
          return new SetTasksPlayerInteraction(new MoveNextToTargetTask(player, actor.pos));
        }
      case Faction.Neutral:
      case Faction.Enemy:
        return new SetTasksPlayerInteraction(
          new ChaseTargetTask(player, actor),
          new AttackTask(player, actor)
        );
    }
    return null;
  }
}

public static class SpriteColorExtractor {
  public static Color32 AverageColorFromTexture(Texture2D tex, float alphaThreshold = 0.2f) {
    Color32[] texColors = tex.GetPixels32();
    int total = 0;
    float r = 0;
    float g = 0;
    float b = 0;

    for (int i = 0; i < total; i++) {
      if (texColors[i].a > alphaThreshold) {
        r += texColors[i].r;
        g += texColors[i].g;
        b += texColors[i].b;
        total++;
      }
    }

    return new Color32((byte)(r / total), (byte)(g / total), (byte)(b / total), 1);
  }
}