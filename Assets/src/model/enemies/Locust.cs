using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Locust : AIActor {

  public Locust(Vector2Int pos) : base(pos) {
    faction = Faction.Neutral;
    hp = baseMaxHp = 1;
    ai = AI().GetEnumerator();
  }

  private IEnumerable<ActorTask> AI() {
    yield return new SleepTask(this);
    var lifeRemaining = 100;
    while (true) {
      if (--lifeRemaining <= 0) {
        yield return new GenericTask(this, (_) => {
          Kill();
        });
        continue;
      }
      if (grass != null) {
        yield return new GenericTask(this, (_) => {
          grass.Kill();
          lifeRemaining = 100;
        });
        continue;
      }

      while(grass == null) {
        var visibleGrasses = floor
          .EnumerateCircle(pos, 5)
          .Where(pos => floor.tiles[pos].CanBeOccupied() && floor.grasses[pos] != null && floor.TestVisibility(pos, this.pos));
        
        if (visibleGrasses.Any()) {
          var closestGrassPos = visibleGrasses.OrderBy((pos) => DistanceTo(pos)).FirstOrDefault();
          yield return new MoveToTargetTask(this, closestGrassPos);
        } else {
          if (isVisible) {
            yield return new MoveNextToTargetTask(this, GameModel.main.player.pos);
          } else {
            yield return new MoveRandomlyTask(this);
          }
        }
      }
    }
  }
}