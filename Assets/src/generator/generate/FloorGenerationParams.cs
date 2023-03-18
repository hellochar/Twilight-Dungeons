using System;

[Serializable]
public abstract class FloorGenerationParams {
  public readonly int depth;
  public int seed = (new System.Random().Next());

  public FloorGenerationParams(int depth) {
    this.depth = depth;
  }

  public abstract Floor generate();
}

[Serializable]
public class HomeFloorParams : FloorGenerationParams {
  public HomeFloorParams() : base(0) { }

  public override Floor generate() {
    return GameModel.main.generator.generateHomeFloor();
  }
}

[Serializable]
public class TutorialFloorParams : FloorGenerationParams {
  public TutorialFloorParams() : base(0) { }

  public override Floor generate() {
    return new TutorialFloor();
  }
}