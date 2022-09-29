using UnityEngine;

public class ShieldLinkStatusController : StatusController {
  ShieldLinkStatus linkStatus => (ShieldLinkStatus) status;
  public LineRenderer lr;
  GameObject actorGO, shielderGO;

  public override void Start() {
    base.Start();
    actorGO = FloorController.current.GameObjectFor(linkStatus.actor);
    shielderGO = FloorController.current.GameObjectFor(linkStatus.shielder);
    lr.SetPosition(0, actorGO.transform.position);
    lr.SetPosition(1, shielderGO.transform.position);
  }

  void Update() {
    lr.SetPosition(0, actorGO.transform.position);
    lr.SetPosition(1, shielderGO.transform.position);
  }
}
