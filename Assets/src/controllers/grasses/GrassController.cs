using UnityEngine;

public class GrassController : MonoBehaviour, IEntityController {
  public Grass grass;

  // Start is called before the first frame update
  public virtual void Start() {
    this.transform.position = Util.withZ(this.grass.pos, this.transform.position.z);
    grass.OnNoteworthyAction += HandleNoteworthyAction;
  }

  private void HandleNoteworthyAction() {
    if (GetComponent<GrowAtStart>() == null) {
      gameObject.AddComponent<PulseAnimation>();
    }
  }
}
