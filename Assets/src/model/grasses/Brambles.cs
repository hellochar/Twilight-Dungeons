using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Brambles : Grass {
  public bool isSharp = false;
  public event Action OnSharpened;
  public Brambles(Vector2Int pos) : base(pos) {
    OnEnterFloor += HandleEnterFloor;
    OnLeaveFloor += HandleLeaveFloor;
  }

  private void HandleEnterFloor() {
    tile.OnActorLeave += HandleActorLeave;
    tile.OnActorEnter += HandleActorEnter;
  }

  private void HandleLeaveFloor() {
    tile.OnActorLeave -= HandleActorLeave;
    tile.OnActorEnter -= HandleActorEnter;
  }

  private void HandleActorLeave(Actor obj) {
    if (!isSharp) {
      isSharp = true;
      OnSharpened?.Invoke();
      AddTimedEvent(10, () => Kill());
    }
  }

  private void HandleActorEnter(Actor actor) {
    if (isSharp) {
      actor.TakeDamage(2, actor);
      Kill();
    }
  }

  // public float Modify(float input) {
  //   if (input > 0) {
  //     return input + 20;
  //   } else {
  //     return input;
  //   }
  // }
}

// internal class ItemBrambleThorn : EquippableItem {
//   public ItemDeathbloomFlower(int stacks) {
//     this.stacks = stacks;
//   }
//   public int stacksMax => 5;

//   private int _stacks;
//   public int stacks {
//     get => _stacks;
//     set {
//       if (value < 0) {
//         throw new ArgumentException("Setting negative stack!" + this + " to " + value);
//       }
//       _stacks = value;
//       if (_stacks == 0) {
//         Destroy();
//       }
//     }
//   }

//   public void Eat(Actor a) {
//     a.statuses.Add(new FrenziedStatus(10));
//     stacks--;
//   }
// }
