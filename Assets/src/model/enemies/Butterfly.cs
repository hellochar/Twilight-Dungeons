using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Butterfly : AIActor {

  public Butterfly(Vector2Int pos) : base(pos) {
    faction = Faction.Ally;
    hp = baseMaxHp = 1;
    ai = AI().GetEnumerator();
  }

  private IEnumerable<ActorTask> AI() {
    statuses.Add(new CharmedStatus());

    var player = GameModel.main.player;

    player.OnEnterFloor += () => {
      GameModel.main.EnqueueEvent(() => {
        var freeTile = player.floor.GetAdjacentTiles(player.pos).Where((tile) => tile.CanBeOccupied()).First();
        GameModel.main.PutActorAt(actor, player.floor, freeTile.pos);
      });
    };

    var duplicateCooldown = 0;

    while (true) {
      // we want to duplicate
      if (duplicateCooldown <= 0) {
        // find a grass
        var playerGrass = player.grass;
        if (playerGrass != null) {
          // we're on top of a grass, duplicate!
          yield return new GenericTask(this, (_) => {
            DuplicateGrass(playerGrass);
            duplicateCooldown = 20;
          });
          continue;
        }
      }
      // we can't duplicate, either because there's nothing nearby or we're on CD
      if (duplicateCooldown > 0) {
        duplicateCooldown--;
      }
      yield return new ChaseTargetTask(this, player);
    }
  }

  private void DuplicateGrass(Grass grass) {
    var neighborTiles = grass.floor.GetCardinalNeighbors(grass.pos).Where((tile) => tile is Ground && tile.grass == null);
    var constructorInfo = grass.GetType().GetConstructor(new System.Type[1] { typeof(Vector2Int) });
    foreach (var tile in neighborTiles) {
      var newGrass = (Grass)constructorInfo.Invoke(new object[] { tile.pos });
      grass.floor.Put(newGrass);
    }
  }
}