using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Context passed to all event methods. Contains references to game state
/// and a callback to continue after the event resolves.
/// </summary>
public class EventContext {
  public GameModel model;
  public Player player;
  public Floor home;
  public int currentDepth;
  /// <summary>Call this to continue after the event (e.g., descend to next floor, or remove entity).</summary>
  public Action afterEvent;

  public static EventContext ForDescent(int nextDepth) {
    return new EventContext {
      model = GameModel.main,
      player = GameModel.main.player,
      home = GameModel.main.home,
      currentDepth = GameModel.main.depth,
      afterEvent = () => GameModel.main.PutPlayerAt(nextDepth)
    };
  }

  public static EventContext ForEntity(Entity source) {
    return new EventContext {
      model = GameModel.main,
      player = GameModel.main.player,
      home = GameModel.main.home,
      currentDepth = GameModel.main.depth,
      afterEvent = () => source.Kill(GameModel.main.player)
    };
  }

  /// <summary>
  /// Check all items the player has, regardless of whether they are in inventory or equipment.
  /// </summary>
  public IEnumerable<Item> AllPlayerItems() =>
    player.inventory.ItemsNonNull().Concat(player.equipment.ItemsNonNull());
}

/// <summary>
/// A single choice within a narrative event.
/// </summary>
public class EventChoice {
  public string Label;
  /// <summary>Optional preview text explaining the consequences.</summary>
  public string Tooltip;
  /// <summary>Items to render in the popup so the player can inspect them before choosing.</summary>
  public Inventory PreviewItems;
  /// <summary>
  /// If false, the choice button is shown grayed out with UnavailableReason text.
  /// If you don't want the choice to appear at all, simply don't add it to the list.
  /// </summary>
  public Func<EventContext, bool> IsAvailable;
  /// <summary>Text shown beneath the grayed-out button explaining why it's unavailable.</summary>
  public string UnavailableReason;
  /// <summary>The effect that runs when this choice is selected.</summary>
  public Action<EventContext> Effect;
}

/// <summary>
/// Abstract base class for all narrative events. Events are standalone logic classes,
/// decoupled from how they are triggered. The same event can fire between floors,
/// from an in-world EventBody entity, or from any other trigger source.
/// </summary>
[Serializable]
public abstract class NarrativeEvent {
  public abstract string Title { get; }
  public abstract string Description { get; }
  public virtual string FlavorText => "";
  /// <summary>Minimum depth (inclusive) where this event can appear.</summary>
  public virtual int MinDepth => 0;
  /// <summary>Maximum depth (inclusive) where this event can appear.</summary>
  public virtual int MaxDepth => 27;
  /// <summary>Selection weight for random event pool.</summary>
  public virtual float Weight => 1f;
  /// <summary>Can this event occur more than once per run?</summary>
  public virtual bool Repeatable => false;
  /// <summary>If true, a free "Leave" button is added to choices.</summary>
  public virtual bool HasWalkAway => true;

  /// <summary>
  /// Dynamic condition check. Return false to exclude this event from the pool.
  /// Use this to check homebase state, player items, etc.
  /// </summary>
  public virtual bool CanOccur(EventContext ctx) => true;

  /// <summary>
  /// Return the list of choices available to the player.
  /// Choices with IsAvailable=false will be shown grayed out.
  /// Choices you don't want shown at all should simply be omitted.
  /// </summary>
  public abstract List<EventChoice> GetChoices(EventContext ctx);

  /// <summary>
  /// Show this event's popup using Popups.CreateStandard().
  /// Can be called from any trigger source.
  /// </summary>
  public virtual void Present(EventContext ctx) {
    var choices = GetChoices(ctx);
    var buttons = new List<(string, Action)>();

    foreach (var choice in choices) {
      bool available = choice.IsAvailable?.Invoke(ctx) ?? true;
      if (available) {
        buttons.Add((FormatChoiceLabel(choice), () => {
          choice.Effect?.Invoke(ctx);
          ctx.afterEvent?.Invoke();
        }));
      } else {
        // Add disabled button with reason text
        var label = choice.Label;
        if (!string.IsNullOrEmpty(choice.UnavailableReason)) {
          label += $" ({choice.UnavailableReason})";
        }
        buttons.Add((label, null)); // null action = disabled
      }
    }

    if (HasWalkAway && !choices.Any(c => c.Label == "Leave" || c.Label == "Walk away")) {
      buttons.Add(("Leave", () => {
        ctx.afterEvent?.Invoke();
      }));
    }

    Popups.CreateStandard(
      title: Title,
      category: "Event",
      info: Description,
      flavor: FlavorText,
      buttons: buttons,
      inventory: GetPreviewInventory(choices, ctx)
    );
  }

  private string FormatChoiceLabel(EventChoice choice) {
    if (!string.IsNullOrEmpty(choice.Tooltip)) {
      return $"{choice.Label}\n<size=10>{choice.Tooltip}</size>";
    }
    return choice.Label;
  }

  /// <summary>
  /// If any choice has PreviewItems, return the first available one for the popup's inventory display.
  /// </summary>
  private Inventory GetPreviewInventory(List<EventChoice> choices, EventContext ctx) {
    foreach (var choice in choices) {
      if (choice.PreviewItems != null) {
        bool available = choice.IsAvailable?.Invoke(ctx) ?? true;
        if (available) {
          return choice.PreviewItems;
        }
      }
    }
    return null;
  }
}
