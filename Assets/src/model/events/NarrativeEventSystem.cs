using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

/// <summary>
/// Manages the pool of all narrative events, handles selection logic,
/// and provides the trigger hooks for between-floor and floor-cleared events.
/// Lives on GameModel and serializes with the game state.
/// </summary>
[Serializable]
public class NarrativeEventSystem {
  /// <summary>
  /// Set of event type names that have already occurred this run (for non-repeatable events).
  /// </summary>
  public HashSet<string> usedEvents = new HashSet<string>();

  /// <summary>
  /// Depth at which the last between-floor event triggered.
  /// Used to enforce minimum gap between events.
  /// </summary>
  public int lastEventDepth = -10;

  /// <summary>Minimum number of floors between between-floor events.</summary>
  public int MinEventGap = 2;

  /// <summary>Base probability that a between-floor event triggers (0-1).</summary>
  public float EventChance = 0.35f;

  /// <summary>
  /// All registered event types. Non-serialized — rebuilt on load.
  /// </summary>
  [NonSerialized]
  private List<NarrativeEvent> eventPool;

  [OnDeserialized]
  void HandleDeserialized(StreamingContext context) {
    InitEventPool();
  }

  public NarrativeEventSystem() {
    InitEventPool();
  }

  private void InitEventPool() {
    eventPool = new List<NarrativeEvent>();
    // Register all event types here. Add new events to this list.
    eventPool.Add(new TransmutationAltarEvent());
    eventPool.Add(new VoiceInTheDarkEvent());
    eventPool.Add(new HealingSpringEvent());
    eventPool.Add(new HungryDoorEvent());
    eventPool.Add(new StrangerEvent1());
    eventPool.Add(new StrangerEvent2());
    eventPool.Add(new FertileAshEvent());
  }

  /// <summary>
  /// Try to trigger a between-floor event. Returns true if an event was presented
  /// (descent is deferred to the event's button callbacks). Returns false if no event
  /// triggered and the caller should descend immediately.
  /// </summary>
  public bool TryTriggerBetweenFloors(int nextDepth) {
    // Enforce minimum gap
    if (nextDepth - lastEventDepth < MinEventGap) {
      return false;
    }

    // Roll for event
    if (MyRandom.value > EventChance) {
      return false;
    }

    var ctx = EventContext.ForDescent(nextDepth);
    var candidates = GetEligibleEvents(ctx, betweenFloorsOnly: true);

    if (candidates.Count == 0) {
      return false;
    }

    // Weighted random selection
    var selected = WeightedRandomSelect(candidates);
    if (selected == null) {
      return false;
    }

    lastEventDepth = nextDepth;
    MarkUsed(selected);
    selected.Present(ctx);
    return true;
  }

  /// <summary>
  /// Get a random event suitable for placing as an EventBody on a floor at the given depth.
  /// Returns null if no eligible events remain.
  /// </summary>
  public NarrativeEvent GetEventForFloor(int depth) {
    var ctx = new EventContext {
      model = GameModel.main,
      player = GameModel.main.player,
      home = GameModel.main.home,
      currentDepth = depth
    };
    var candidates = GetEligibleEvents(ctx, betweenFloorsOnly: false);
    if (candidates.Count == 0) return null;

    var selected = WeightedRandomSelect(candidates);
    if (selected != null) {
      MarkUsed(selected);
    }
    return selected;
  }

  private List<NarrativeEvent> GetEligibleEvents(EventContext ctx, bool betweenFloorsOnly) {
    return eventPool.Where(e => {
      // Depth range check
      if (ctx.currentDepth < e.MinDepth || ctx.currentDepth > e.MaxDepth) return false;

      // Repeatable check
      if (!e.Repeatable && usedEvents.Contains(e.GetType().Name)) return false;

      // Dynamic condition
      if (!e.CanOccur(ctx)) return false;

      return true;
    }).ToList();
  }

  private void MarkUsed(NarrativeEvent evt) {
    if (!evt.Repeatable) {
      usedEvents.Add(evt.GetType().Name);
    }
  }

  private NarrativeEvent WeightedRandomSelect(List<NarrativeEvent> candidates) {
    if (candidates.Count == 0) return null;

    float totalWeight = candidates.Sum(e => e.Weight);
    float roll = MyRandom.value * totalWeight;
    float cumulative = 0;

    foreach (var evt in candidates) {
      cumulative += evt.Weight;
      if (roll <= cumulative) {
        return evt;
      }
    }

    return candidates.Last();
  }
}
