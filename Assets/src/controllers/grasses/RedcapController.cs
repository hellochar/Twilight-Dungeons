public class RedcapController : GrassController, IOnTopActionHandler {
  public Redcap cap => (Redcap) grass;
  public string OnTopActionName => "Pop";
  public void HandleOnTopAction() {
    cap.Pop(GameModel.main.player);
    var poof = PrefabCache.Effects.Instantiate("RedcapPoof", null);
    poof.transform.position = transform.position;
  }
}