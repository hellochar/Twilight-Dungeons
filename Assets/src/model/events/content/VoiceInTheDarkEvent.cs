using System;
using System.Collections.Generic;

/// <summary>
/// Category 2: The Bargain — gain something powerful at a permanent cost.
/// Between-floor event.
/// </summary>
[Serializable]
public class VoiceInTheDarkEvent : NarrativeEvent {
  public override string Title => "Voice in the Dark";
  public override string Description => "As you descend, a whisper curls from the walls. \"I can give you what you need. But everything has a price.\"";
  public override string FlavorText => "The air grows cold around you.";
  public override int MinDepth => 4;
  public override int MaxDepth => 22;

  public override bool CanOccur(EventContext ctx) => ctx.player.baseMaxHp >= 8;

  public override List<EventChoice> GetChoices(EventContext ctx) {
    return new List<EventChoice> {
      new EventChoice {
        Label = "Accept the offer",
        Tooltip = "Gain a strong weapon, lose 1 heart permanently",
        Effect = (c) => {
          c.player.baseMaxHp -= 4;
          if (c.player.hp > c.player.maxHp) {
            c.player.SetHPDirect(c.player.maxHp);
          }
          c.player.inventory.AddItem(new ItemThickBranch());
        }
      },
      new EventChoice {
        Label = "Refuse",
        Tooltip = "\"Then face what comes with what you have.\"",
        Effect = (c) => { }
      }
    };
  }
}
