using UnityEngine;

public class IrridineController : GrassController {
  public Irridine irridine => (Irridine) grass;

  // Start is called before the first frame update
  public override void Start() {
    base.Start();
    var rot = transform.eulerAngles;
    rot.z = irridine.angle;
    transform.eulerAngles = rot;
  }
}
