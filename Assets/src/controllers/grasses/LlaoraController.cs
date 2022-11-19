using UnityEngine;

public class LlaoraController : GrassController, IOnTopActionHandler {
  public Llaora llaora => (Llaora) grass;
  public string OnTopActionName => "Disperse";
  public void HandleOnTopAction() {
    llaora.Disperse(GameModel.main.player);
  }

  public static void PlayPoofVfx(Entity e) {
    var poof = PrefabCache.Effects.Instantiate("RedcapPoof", null);
    poof.transform.position = FloorController.current.GameObjectFor(e).transform.position;
    var scale = Llaora.radius * 2f / 3;
    poof.transform.localScale = new Vector3(scale, scale, 1);
    var ps = poof.GetComponent<ParticleSystem>();
    var main = ps.main;
    Color c = new Color32(0, 172, 216, 255);
    main.startColor = c;
    main.startSize = main.startSize.constant / scale;
  }
}