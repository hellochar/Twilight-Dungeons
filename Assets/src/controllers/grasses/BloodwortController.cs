using UnityEngine;

public class BloodwortController : GrassController, IOnTopActionHandler {
  public Bloodwort bloodwort => (Bloodwort) grass;
  public string OnTopActionName => "Eat";
  public void HandleOnTopAction() {
    bloodwort.Eat();
  }
}