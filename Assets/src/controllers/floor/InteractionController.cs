using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// There's two main ways of interacting with the world: 
/// (1) is to interact with an Entity by either bumping into it or by long-tapping it
/// (2) is to tap on the Entity or "select" it with the side panel
public class InteractionController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {
  /// allow other things to globally enable/disable interacting with the world
  /// e.g. should disable interaction during the Ezra cutscene
  public static bool isInputAllowed = true;

  FloorController floorController;
  Floor floor => floorController.floor;

  /// Represents a single hold interaction. This stores:
  ///   The time the hold started
  ///   The pointerEventData that started iot
  ///   Whether the hold has been "triggered"
  /// and also has methods to determine whether the hold should trigger
  private InputHold hold;

  void Start() {
    floorController = GetComponent<FloorController>();
    isInputAllowed = true;
  }

  /// Register the hold.
  public void OnPointerDown(PointerEventData eventData) {
    if (isInputAllowed && !CameraController.IsZoomGuardActive) {
      hold = new InputHold(Time.time, eventData);
      HoldProgressBar.main.HoldStart();
    }
  }

  /// Show Popup if it's a short-tap
  public void OnPointerUp(PointerEventData eventData) {
    var isShortTap = hold == null || !hold.triggered;
    if (isInputAllowed && !CameraController.IsZoomGuardActive && isShortTap) {
      var pos = RaycastToTilePos(eventData.pointerCurrentRaycast);
      Tap(pos);
    }
    hold = null;
    HoldProgressBar.main.HoldEnd();
  }

  void Update() {
    if (hold != null && isInputAllowed) {
      /// zoom guard will only trigger *after* a hold has been created because
      /// zoom is 2 touches, so the first touch will be registered as the hold.
      if (CameraController.IsZoomGuardActive) {
        hold = null;
      } else {
        var time = Time.time;
        HoldProgressBar.main.HoldUpdate(hold.PercentDone(time));
        if (hold.ShouldTrigger(time)) {
          hold.triggered = true;
          var pos = RaycastToTilePos(hold.pointerEventData.pointerCurrentRaycast);
          LongTap(pos);
        }
      }
    }
  }

  public void LongTap(Vector2Int pos) {
    var entity = floorController.GetVisibleEntitiesInLayerOrder(pos).FirstOrDefault();
    if (entity != null) {
      ShowPopupFor(entity);
    }
  }

  public void Tap(Vector2Int pos) {
    Interact(pos, hold?.pointerEventData);
  }

  public void Interact(Vector2Int worldPos, PointerEventData eventData) {
    if (!isInputAllowed) {
      return;
    }
    var entities = floorController.GetVisibleEntitiesInLayerOrder(worldPos);
    if (floor is HomeFloor) {
      HomeFloorInteract(entities.FirstOrDefault(), worldPos);
      return;
    }

    if (floorController.TryGetFirstControllerComponent<IPlayerInteractHandler>(entities, out var handler, out var entity)) {
      var interaction = handler.GetPlayerInteraction(eventData);
      if (interaction == null) {
        return;
      }
      if (interaction is ArbitraryPlayerInteraction) {
        interaction.Perform();
        return;
      }

      var isLevelSimulating = floor.depth > 0 && GameModel.main.GetAllEntitiesInPlay().Count() > 1;
      if (isLevelSimulating) {
        InteractSimulatingLevel(handler, entity, interaction);
      } else {
        // not in combat, perform immediately
        interaction.Perform();
      }
      // var entityGameObject = floorController.GameObjectFor(handler);
      // /// HACK destroy highlight once it's been interacted with
      // var highlight = entityGameObject.transform.Find("Highlight(Clone)");
      // if (highlight != null) {
      //   highlight.gameObject.AddComponent<FadeThenDestroy>();
      // }
    }
  }

  private void HomeFloorInteract(Entity entity, Vector2Int worldPos) {
    if (entity.tile.visibility == TileVisiblity.Unexplored) {
      return;
    }

    if (entity.IsDead) {
      return;
    }

    var player = GameModel.main.player;

    // hack - make the home downstairs a Body (pit)
    if (entity is Tile && !(entity is Downstairs)) {
      new SetTasksPlayerInteraction(
        new MoveToTargetTask(player, worldPos)
      ).Perform();
    } else {
      new SetTasksPlayerInteraction(
        new MoveNextToTargetTask(player, worldPos),
        new GenericPlayerTask(player, () => ShowPopupFor(entity))
      ).Perform();
    }
  }

  public static IPlayerInteractHandler proposedInteract = null;
  public static SetTasksPlayerInteraction proposedTasks;
  public static event Action<SetTasksPlayerInteraction> OnProposedTasksChanged;
  private void InteractSimulatingLevel(IPlayerInteractHandler handler, Entity entity, PlayerInteraction interaction) {
    if (entity.IsNextTo(GameModel.main.player)) {
      interaction.Perform();
      return;
    }
    // if we're setting tasks, first show the interaction "proposed"
    if (interaction is SetTasksPlayerInteraction s) {
      if (proposedInteract != handler) {
        GameModel.main.player.ClearTasks();
        proposedInteract = handler;
        proposedTasks = s;
        OnProposedTasksChanged?.Invoke(proposedTasks);
      } else {
        // ok, we've confirmed!
        // proposedInteract = null;
        // proposedTasks = null;
        // OnProposedTasksChanged?.Invoke(proposedTasks);
        s.Perform();
      }
    } else {
      interaction.Perform();
    }
  }

  public static void ShowPopupFor(Entity entity) {
    var floorController = FloorController.current;
    GameObject entityGameObject = floorController.GameObjectFor(entity);
    if (entityGameObject.TryGetComponent<IPopupOverride>(out var popupOverride)) {
      popupOverride.HandleShowPopup();
      return;
    }

    string description = entity.description + "\n\n";
    if (entity is Body b) {
      if (b is Actor a && a.BaseAttackDamage() != (0, 0)) {
        description += "Deals " + Util.DescribeDamageSpread(a.BaseAttackDamage());
      }
      if (b.maxHp > 0) {
        description += $"Max HP: {b.maxHp}\n";
      }
    }

    var spritePrefab = PrefabCache.UI.GetPrefabFor("Entity Image");
    var spriteGameObject = Instantiate(spritePrefab);
    var image = spriteGameObject.GetComponentInChildren<Image>();
    var sprite = ObjectInfo.GetSpriteFor(entity) ?? entityGameObject.GetComponentInChildren<SpriteRenderer>()?.sprite;
    image.sprite = sprite;
    image.color = entityGameObject.GetComponentInChildren<SpriteRenderer>().color;
    List<(string, Action)> buttons = new List<(string, Action)>();
    Inventory inventory = null;

    var player = GameModel.main.player;
    if (player.floor.depth == 0) {
      var playerActions = entity.GetType().GetMethods().Where(m => m.GetCustomAttributes(typeof(PlayerActionAttribute), true).Any());
      foreach(var action in playerActions) {
        buttons.Add((Util.WithSpaces(action.Name), () => {
          action.Invoke(entity, new object[0]);
        }));
      }
      // if (entity is IDaySteppable d) {
      //   buttons.Add(("Activate", () => {
      //     player.UseActionPointOrThrow();
      //     d.StepDay();
      //   }));
      // }
    }
    if (entity is IInteractableInventory i) {
      inventory = i.inventory;
    }

    var controller = Popups.CreateStandard(
      title: entity.displayName,
      category: GetCategoryForEntity(entity),
      info: description.Trim(),
      flavor: ObjectInfo.GetFlavorTextFor(entity),
      sprite: spriteGameObject,
      buttons: buttons,
      inventory: inventory
    );
    controller.target = entity;
    controller.Init(TextAnchor.MiddleRight);
    CameraController.main.SetCameraOverride(controller);
    Destroy(spriteGameObject);
  }

  /// get the *first* handler out of a list of entities
  private static Vector2Int RaycastToTilePos(RaycastResult raycast) {
    var worldPos = raycast.worldPosition;
    var pos = new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.y));
    return pos;
  }

  private static string GetCategoryForEntity(Entity entity) {
    switch (entity) {
      case Tile t:
        return "Tile";
      case Actor a:
        return "Creature";
      case Grass g:
        return "Grass";
      case Destructible d:
        return "Destructible";
      default:
        return "Other";
    }
  }
}

class InputHold {
  public const float THRESHOLD = 0.5f;
  public readonly float time;
  public readonly PointerEventData pointerEventData;
  public readonly float threshold = THRESHOLD;
  public bool triggered = false;

  public InputHold(float time, PointerEventData pointerEventData) {
    this.time = time;
    this.pointerEventData = pointerEventData;
  }

  public float PercentDone(float t) => Mathf.Clamp((t - time) / threshold, 0, 1);

  public bool ShouldTrigger(float t) => !triggered && PercentDone(t) >= 1;
}