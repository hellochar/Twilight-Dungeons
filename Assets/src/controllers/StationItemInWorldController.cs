using UnityEngine;

public class StationItemInWorldController : ItemSlotController {
  public Station station => GetComponent<StationController>().station;
  public override Item item => station.inventory[0];

  public Transform itemInWorldContainer;

  private GameObject itemPrefab;
  void Start() {
    itemPrefab = PrefabCache.UI.GetPrefabFor("Equipment On Player");
  }

  protected override GameObject UpdateInUse(Item item) {
    var child = Instantiate(itemPrefab, new Vector3(), Quaternion.identity, itemInWorldContainer);
    child.transform.localPosition = new Vector3(0, 0, 0);
    child.transform.localRotation = Quaternion.identity;
    child.GetComponent<SpriteRenderer>().sprite = ObjectInfo.GetSpriteFor(item);
    return child;
  }
}
