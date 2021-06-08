using System.Collections;
using UnityEngine;

public class BossController : ActorController, IEntityControllerRemoveOverride {
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
