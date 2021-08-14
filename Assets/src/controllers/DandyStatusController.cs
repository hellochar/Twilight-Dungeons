using UnityEngine;

public class DandyStatusController : StatusController {
  DandyStatus dandy => (DandyStatus) status;
  public GameObject baseSprite;

  public override void Start() {
    base.Start();
    Update();
  }

  void Update() {
    // make more
    if (dandy.stacks > transform.childCount) {
      var numToMake = dandy.stacks - transform.childCount;
      for(; numToMake > 0; numToMake--) {
        var x = transform.childCount + 1;
        var rotIndex = x / 2 * (x % 2 == 1 ? 1 : -1);

        var zRot = rotIndex * -20;
        var xPos = rotIndex * 0.12f;
        var go = Instantiate(baseSprite,
          baseSprite.transform.position + new Vector3(xPos, 0, 0),
          Quaternion.Euler(0, 0, zRot),
          transform
        );
      }
    } else if (dandy.stacks < transform.childCount) {
      // make less
      var numToDelete = transform.childCount - dandy.stacks;
      for (; numToDelete > 0; numToDelete--) {
        Destroy(transform.GetChild(transform.childCount - 1).gameObject);
      }
    }
  }
}
