using UnityEngine;

[System.Serializable]
[ObjectInfo(spriteName: "Purple_5", description: "Alternately opens and closes every 12 turns.\nWhile open, Pacifies the creature standing over it.")]
public class Violets : Grass, ISteppable, IActorEnterHandler {
  public const int turnsToChange = 12;
  public float timeNextAction { get; set; }
  public float turnPriority => 0;
  public bool isOpen = false;
  public int countUp = 0;

  public static bool CanOccupy(Tile tile) => tile is Ground;
  public Violets(Vector2Int pos) : base(pos) {
    timeNextAction = timeCreated + 1;
    countUp = MyRandom.Range(0, 4);
  }

  public void HandleActorEnter(Actor who) {
    if (isOpen) {
      actor?.statuses.Add(new PacifiedStatus());
    }
  }

  public float Step() {
    countUp++;
    if (countUp >= turnsToChange) {
      isOpen = !isOpen;
      countUp = 0;
    }
    if (isOpen) {
      actor?.statuses.Add(new PacifiedStatus());
    }
    return 1;
  }
}

[System.Serializable]
[ObjectInfo("peace")]
public class PacifiedStatus : Status, IBaseActionModifier {
  public override string Info() => "You cannot attack while standing on an open Violet.";

  public BaseAction Modify(BaseAction input) {
    if (input.Type == ActionType.ATTACK) {
      return new StruggleBaseAction(input.actor);
    }
    return input;
  }

  public override void Step() {
    var shouldKeep = actor.grass is Violets v && v.isOpen;
    if (!shouldKeep) {
      Remove();
    }
  }

  public override bool Consume(Status other) => true;
}
