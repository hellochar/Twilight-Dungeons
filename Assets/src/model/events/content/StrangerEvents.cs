using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Category 20: The Stranger — recurring NPC, relationship tracked via homebase entity.
/// Encounter 1: first meeting, around depth 4.
/// </summary>
[Serializable]
public class StrangerEvent1 : NarrativeEvent {
  public override string Title => "The Stranger";
  public override string Description => "A figure in a tattered cloak slumps against the wall. They're hurt — badly. They look up at you with weary eyes.";
  public override string FlavorText => "\"Please...\"";
  public override int MinDepth => 3;
  public override int MaxDepth => 8;

  public override bool CanOccur(EventContext ctx) {
    // Only if we haven't met them yet
    return !ctx.home.bodies.OfType<StrangerNPC>().Any();
  }

  public override List<EventChoice> GetChoices(EventContext ctx) {
    return new List<EventChoice> {
      new EventChoice {
        Label = "Help them",
        Tooltip = "Lose 4 HP. They'll remember this.",
        Effect = (c) => {
          c.player.TakeDamage(4, null);
          c.model.DrainEventQueue();
          // Place the Stranger at home
          var homePos = FindEmptyHomePos(c.home);
          var stranger = new StrangerNPC(homePos);
          stranger.timesHelped = 1;
          c.home.Put(stranger);
        }
      },
      new EventChoice {
        Label = "Walk past",
        Tooltip = "You have your own problems.",
        Effect = (c) => { }
      }
    };
  }

  private Vector2Int FindEmptyHomePos(Floor home) {
    // Find a ground tile near the center that isn't occupied
    var center = new Vector2Int(home.width / 2, home.height / 2);
    var candidates = home.BreadthFirstSearch(center, t => true)
      .Where(t => t is Ground && t.CanBeOccupied() && !(t is Soil))
      .Take(10);
    var tile = candidates.FirstOrDefault();
    return tile?.pos ?? center;
  }
}

/// <summary>
/// Encounter 2: second meeting, around depth 10-14.
/// </summary>
[Serializable]
public class StrangerEvent2 : NarrativeEvent {
  public override string Title => "The Stranger";
  public override int MinDepth => 10;
  public override int MaxDepth => 16;

  public override string Description {
    get {
      var stranger = GameModel.main?.home?.bodies.OfType<StrangerNPC>().FirstOrDefault();
      if (stranger != null) {
        return "The cloaked figure steps from the shadows. They recognize you — there's warmth in their eyes. \"I found something. For you.\"";
      }
      return "A familiar figure blocks the passage. They glance at you with cold recognition. \"You left me to die. The passage demands payment.\"";
    }
  }
  public override string FlavorText => "";

  public override bool HasWalkAway {
    get {
      var stranger = GameModel.main?.home?.bodies.OfType<StrangerNPC>().FirstOrDefault();
      return stranger != null; // Can only walk away if you helped before
    }
  }

  public override bool CanOccur(EventContext ctx) {
    // Always can occur at this depth — behavior branches on whether Stranger is at home
    return true;
  }

  public override List<EventChoice> GetChoices(EventContext ctx) {
    var stranger = ctx.home.bodies.OfType<StrangerNPC>().FirstOrDefault();

    if (stranger != null) {
      // They helped before — reward them
      return new List<EventChoice> {
        new EventChoice {
          Label = "Accept their gift",
          Tooltip = "Receive a rare seed",
          Effect = (c) => {
            var seedTypes = new Type[] {
              typeof(Kingshroom), typeof(Weirdwood), typeof(ChangErsWillow), typeof(Frizzlefen)
            };
            var seedType = seedTypes[MyRandom.Range(0, seedTypes.Length)];
            c.player.inventory.AddItem(new ItemSeed(seedType, 1));
            stranger.timesHelped++;
          }
        }
      };
    } else {
      // They didn't help — forced payment
      return new List<EventChoice> {
        new EventChoice {
          Label = "Pay 4 HP to pass",
          IsAvailable = (c) => c.player.hp > 4,
          UnavailableReason = "Too dangerous",
          Effect = (c) => {
            c.player.TakeDamage(4, null);
            c.model.DrainEventQueue();
          }
        },
        new EventChoice {
          Label = "Pay 80 water to pass",
          IsAvailable = (c) => c.player.water >= 80,
          UnavailableReason = "Not enough water",
          Effect = (c) => {
            c.player.water -= 80;
          }
        }
      };
    }
  }
}

/// <summary>
/// Encounter 3: third meeting, around depth 18-22.
/// Rewards scale based on how many times you've helped.
/// </summary>
[Serializable]
public class StrangerEvent3 : NarrativeEvent {
  public override string Title => "The Stranger";
  public override int MinDepth => 18;
  public override int MaxDepth => 23;

  public override string Description {
    get {
      var stranger = GameModel.main?.home?.bodies.OfType<StrangerNPC>().FirstOrDefault();
      if (stranger != null && stranger.timesHelped >= 2) {
        return "The cloaked figure waits for you with open arms. \"I've been tending something. For your garden.\"";
      } else if (stranger != null) {
        return "The stranger nods at you from the shadows. \"I found a seed. It's not much, but...\"";
      }
      return "The figure appears again, blocking the way. Their expression is hard. \"You owe a debt to these depths.\"";
    }
  }
  public override string FlavorText => "";

  public override bool HasWalkAway {
    get {
      var stranger = GameModel.main?.home?.bodies.OfType<StrangerNPC>().FirstOrDefault();
      return stranger != null;
    }
  }

  public override List<EventChoice> GetChoices(EventContext ctx) {
    var stranger = ctx.home.bodies.OfType<StrangerNPC>().FirstOrDefault();

    if (stranger != null && stranger.timesHelped >= 2) {
      // Helped twice — a mature plant appears at home
      return new List<EventChoice> {
        new EventChoice {
          Label = "Accept their gift",
          Tooltip = "A mature plant appears in your garden",
          Effect = (c) => {
            var homeTile = c.home.EnumerateFloor()
              .Select(p => c.home.tiles[p])
              .Where(t => t is Soil && t.CanBeOccupied())
              .FirstOrDefault();
            if (homeTile == null) {
              homeTile = c.home.EnumerateFloor()
                .Select(p => c.home.tiles[p])
                .Where(t => t is Ground && t.CanBeOccupied())
                .FirstOrDefault();
            }
            if (homeTile != null) {
              var plantTypes = new Type[] { typeof(Kingshroom), typeof(Weirdwood), typeof(Frizzlefen) };
              var plantType = plantTypes[MyRandom.Range(0, plantTypes.Length)];
              var constructor = plantType.GetConstructor(new Type[] { typeof(Vector2Int) });
              var plant = (Plant)constructor.Invoke(new object[] { homeTile.pos });
              plant.GoNextStage();
              plant.GoNextStage();
              c.home.Put(plant);
            }
            stranger.timesHelped++;
          }
        }
      };
    } else if (stranger != null) {
      // Helped once — just a seed
      return new List<EventChoice> {
        new EventChoice {
          Label = "Accept the seed",
          Tooltip = "Receive a seed",
          Effect = (c) => {
            var seedTypes = new Type[] { typeof(Wildwood), typeof(Thornleaf), typeof(StoutShrub) };
            c.player.inventory.AddItem(new ItemSeed(seedTypes[MyRandom.Range(0, seedTypes.Length)], 1));
            stranger.timesHelped++;
          }
        }
      };
    } else {
      // Never helped — forced, pay more
      return new List<EventChoice> {
        new EventChoice {
          Label = "Pay 4 HP to pass",
          IsAvailable = (c) => c.player.hp > 4,
          UnavailableReason = "Too dangerous",
          Effect = (c) => {
            c.player.TakeDamage(4, null);
            c.model.DrainEventQueue();
          }
        },
        new EventChoice {
          Label = "Pay 100 water to pass",
          IsAvailable = (c) => c.player.water >= 100,
          UnavailableReason = "Not enough water",
          Effect = (c) => {
            c.player.water -= 100;
          }
        }
      };
    }
  }
}

/// <summary>
/// Encounter 4: final meeting, near the end.
/// If helped 3 times: gains an ally. If never helped: an enemy appears.
/// </summary>
[Serializable]
public class StrangerEvent4 : NarrativeEvent {
  public override string Title => "The Stranger";
  public override int MinDepth => 24;
  public override int MaxDepth => 27;

  public override string Description {
    get {
      var stranger = GameModel.main?.home?.bodies.OfType<StrangerNPC>().FirstOrDefault();
      if (stranger != null && stranger.timesHelped >= 3) {
        return "The stranger stands tall, cloak thrown back. They draw a blade you didn't know they carried. \"Let me fight beside you. Just this once.\"";
      } else if (stranger != null) {
        return "The stranger gives you a final nod. \"Take this. I hope it's enough.\" They press something into your hands and vanish.";
      }
      return "The figure steps from the dark one last time. They are not alone.";
    }
  }
  public override string FlavorText => "";

  public override bool HasWalkAway {
    get {
      var stranger = GameModel.main?.home?.bodies.OfType<StrangerNPC>().FirstOrDefault();
      return stranger != null;
    }
  }

  public override List<EventChoice> GetChoices(EventContext ctx) {
    var stranger = ctx.home.bodies.OfType<StrangerNPC>().FirstOrDefault();

    if (stranger != null && stranger.timesHelped >= 3) {
      // Full friendship — spawn charmed ally
      return new List<EventChoice> {
        new EventChoice {
          Label = "Fight together",
          Tooltip = "The Stranger joins you as an ally",
          Effect = (c) => {
            var floor = GameModel.main.currentFloor;
            var allyTile = floor.GetAdjacentTiles(c.player.pos)
              .Where(t => t.CanBeOccupied())
              .FirstOrDefault();
            if (allyTile != null) {
              var ally = new Golem(allyTile.pos);
              ally.faction = Faction.Ally;
              ally.SetAI(new CharmAI(ally));
              floor.Put(ally);
            }
            stranger.timesHelped++;
          }
        }
      };
    } else if (stranger != null) {
      // Partial help — one last gift
      return new List<EventChoice> {
        new EventChoice {
          Label = "Accept their parting gift",
          Tooltip = "Heal to full and gain 100 water",
          Effect = (c) => {
            c.player.Heal(c.player.maxHp);
            c.player.water += 100;
          }
        }
      };
    } else {
      // Never helped — forced, enemy spawns
      return new List<EventChoice> {
        new EventChoice {
          Label = "Face the ambush",
          Tooltip = "An enemy appears",
          Effect = (c) => {
            var floor = GameModel.main.currentFloor;
            var enemyTile = floor.BreadthFirstSearch(c.player.pos, t => true)
              .Where(t => t.CanBeOccupied() && t.pos != c.player.pos)
              .Skip(2)
              .FirstOrDefault();
            if (enemyTile != null) {
              floor.Put(new Golem(enemyTile.pos));
            }
          }
        }
      };
    }
  }
}
