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

  public static InteractionController current => FloorController.current?.GetComponent<InteractionController>();

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
    TileActionsMenu.Hide();
    var entity = floorController.GetVisibleEntitiesInLayerOrder(pos).FirstOrDefault();
    if (entity != null) {
      EntityPopup.Show(entity);
    }
  }

  public void Tap(Vector2Int pos) {
    if (!isInputAllowed) {
      return;
    }
    var player = GameModel.main.player;
    if (player.task != null && !player.task.IsPlayerOverridable) {
      return;
    }

    var eventData = hold?.pointerEventData;

    var entities = floorController.GetVisibleEntitiesInLayerOrder(pos);
    IPlayerInteractHandler handler = null;
    Entity entity = null;
    PlayerInteraction interaction = null;

    if (floorController.TryGetFirstControllerComponent<IPlayerInteractHandler>(entities, out handler, out entity)) {
      interaction = handler.GetPlayerInteraction(eventData);
      // Handle ArbitraryPlayerInteraction immediately (e.g. tapping on self to cancel task)
      if (interaction is ArbitraryPlayerInteraction) {
        interaction.Perform();
        TileActionsMenu.Hide();
        ClearProposed();
        return;
      }
    }

    // Tap-again to confirm: if menu is already showing for this tile, perform the primary action
    if (TileActionsMenu.IsShowingForPos(pos)) {
      TileActionsMenu.Hide();
      interaction?.Perform();
      ClearProposed();
      return;
    }

    // New tile selected - clear old state
    ClearProposed();
    player.ClearTasks();

    // Build contextual actions for the tile and show the popover
    var actions = TileActionsMenu.BuildActionsForTile(pos, floorController, eventData);
    if (actions.Count > 0) {
      TileActionsMenu.Show(pos, actions);
    } else {
      TileActionsMenu.Hide();
    }

    // Show path preview and handle auto-move
    var isInCombat = floor.depth > 0 && !floor.isCleared;
    if (interaction is SetTasksPlayerInteraction s && !(entity is Player)) {
      if (isInCombat) {
        // In combat: show path preview via proposed tasks (no auto-move)
        proposedInteract = handler;
        proposedTasks = s;
        OnProposedTasksChanged?.Invoke(proposedTasks);
      } else {
        // Not in combat: auto-move immediately (path shows via OnSetTask)
        interaction.Perform();
      }
    }
  }

  private void ClearProposed() {
    proposedInteract = null;
    proposedTasks = null;
    OnProposedTasksChanged?.Invoke(null);
  }

  public void Interact(Vector2Int worldPos, PointerEventData eventData) {
    if (!isInputAllowed) {
      return;
    }
    var player = GameModel.main.player;
    if (player.task != null && !player.task.IsPlayerOverridable) {
      return;
    }

    var entities = floorController.GetVisibleEntitiesInLayerOrder(worldPos);

    if (floorController.TryGetFirstControllerComponent<IPlayerInteractHandler>(entities, out var handler, out var entity)) {
      var interaction = handler.GetPlayerInteraction(eventData);
      if (interaction == null) {
        return;
      }
      if (interaction is ArbitraryPlayerInteraction) {
        interaction.Perform();
        return;
      }

      var isInCombat = floor.depth > 0 && !floor.isCleared;
      if (isInCombat) {
        InteractSimulatingLevel(handler, entity, interaction);
      } else {
        // not in combat, perform immediately
        interaction.Perform();
      }
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

  /// get the *first* handler out of a list of entities
  private static Vector2Int RaycastToTilePos(RaycastResult raycast) {
    var worldPos = raycast.worldPosition;
    var pos = new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.y));
    return pos;
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