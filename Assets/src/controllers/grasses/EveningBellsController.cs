using UnityEngine;

public class EveningBellsController : GrassController {
  public EveningBells bells => (EveningBells) grass;

  // Start is called before the first frame update
  public override void Start() {
    base.Start();
    var rot = transform.eulerAngles;
    rot.z = bells.angle;
    transform.eulerAngles = rot;
  }
}
