using System.Collections;
using UnityEngine;

public class BossController : ActorController, IEntityControllerRemoveOverride {
  public Boss boss => (Boss) actor;

  public override void Start() {
    base.Start();
    GameModel.main.OnBossSeen += HandleBossSeen;
  }

  void OnDestroy() {
    GameModel.main.OnBossSeen -= HandleBossSeen;
  }

  private void HandleBossSeen(Boss boss) {
    StartCoroutine(AnimateBossSeen(boss));
  }

  IEnumerator AnimateBossSeen(Boss b) {
    InteractionController.isInputAllowed = false;
    var tiles = b.floor.EnumerateCircle(b.pos, 3.99f);
    foreach (var t in tiles) {
      b.floor.tiles[t].visibility = TileVisiblity.Visible;
    }
    yield return Transitions.ZoomAndPanCamera(4, b.pos, 0.5f);
    yield return Transitions.ZoomAndPanCamera(4, b.pos, 3);
    foreach (var t in tiles) {
      b.floor.tiles[t].visibility = TileVisiblity.Explored;
    }
    b.floor.RecomputeVisiblity(player);
    yield return Transitions.ZoomAndPanCamera(4, player.pos, 0.5f);
    InteractionController.isInputAllowed = true;
  }

  // add special effect
  public void OverrideRemoved() {
    StartCoroutine(BossDeathAnimation());
  }

  IEnumerator BossDeathAnimation() {
    IEnumerator CameraMotion() {
      InteractionController.isInputAllowed = false;
      yield return Transitions.ZoomAndPanCamera(4, actor.pos, 0.5f);
      yield return Transitions.ZoomAndPanCamera(4, actor.pos, 3);
      yield return Transitions.ZoomAndPanCamera(4, GameModel.main.player.pos, 0.5f);
      InteractionController.isInputAllowed = true;
    }
    Camera.main.GetComponent<CameraFollowEntity>().StartCoroutine(CameraMotion());

    animator.SetTrigger("Vibrate");
    AudioClipStore.main.bossDeath.Play();

    // time big poof with the crash of the boss death sound effect
    yield return new WaitForSeconds(1.704f);
    var poof = PrefabCache.Effects.Instantiate("Big Poof");
    poof.transform.position = this.transform.position + Util.withZ(Random.insideUnitCircle * 0.25f);
    gameObject.AddComponent<FadeThenDestroy>();
  }
}

interface IEntityControllerRemoveOverride {
  void OverrideRemoved();
}
