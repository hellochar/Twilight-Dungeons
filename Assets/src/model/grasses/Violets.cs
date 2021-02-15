using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Alternately opens and closes every 12 turns. While open, Confuses the creature standing over it.")]
public class Violets : Grass, ISteppable {
  public const int turnsToChange = 12;
  public float timeNextAction { get; set; }
  public float turnPriority => 50;
  public bool isOpen = false;
  public int countUp = 0;

  public static bool CanOccupy(Tile tile) => tile is Ground;
  public Violets(Vector2Int pos) : base(pos) {
    timeNextAction = timeCreated + 1;
    countUp = MyRandom.Range(0, 4);
  }

  public float Step() {
    if (isOpen) {
      actor?.statuses.Add(new ConfusedStatus(3));
    }
    countUp++;
    if (countUp >= turnsToChange) {
      isOpen = !isOpen;
      countUp = 0;
    }
    return 1;
  }
}

[System.Serializable]
[ObjectInfo("confused")]
public class ConfusedStatus : StackingStatus, IBaseActionModifier {
  public override StackingMode stackingMode => StackingMode.Max;
  public override string Info() => "Your next turn must be spent moving in a random direction.";

  public ConfusedStatus(int stacks) : base(stacks) {}

  public BaseAction Modify(BaseAction input) {
    stacks--;
    return MoveRandomlyTask.GetRandomMove(input.actor);
  }
}