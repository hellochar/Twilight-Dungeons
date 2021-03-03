using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentOnPlayerController : ItemSlotController {
  private GameObject itemPrefab;

  public EquipmentSlot slot;
  public override Item item => GameModel.main.player.equipment[slot];

  void Start() {
    itemPrefab = Resources.Load<GameObject>("UI/Equipment On Player");
    itemChild = transform.Find("ItemOnPlayer")?.gameObject;
  }

  protected override void UpdateUnused() {
    activeItem.OnDestroyed -= HandleItemDestroyed;
    base.UpdateUnused();
  }

  protected override GameObject UpdateInUse(Item item) {
    item.OnDestroyed += HandleItemDestroyed;
    var showOnPlayer = ObjectInfo.InfoFor(item).showOnPlayer;
    if (showOnPlayer) {
      var child = Instantiate(itemPrefab, new Vector3(), Quaternion.identity, this.transform);
      child.transform.localPosition = new Vector3(0, 0, 0);
      child.transform.localRotation = Quaternion.identity;
      child.GetComponent<SpriteRenderer>().sprite = ObjectInfo.GetSpriteFor(item);
      return child;
    }
    return null;
  }

  private void HandleItemDestroyed() {
    if (this != null) {
      StartCoroutine(PlayItemBreakingAnimation3x());
    }
  }

  IEnumerator PlayItemBreakingAnimation3x() {
    var clone = Instantiate(this.itemChild, this.itemChild.transform.parent);
    clone.SetActive(false);
    PlayItemBreakingAnimation(clone);
    yield return new WaitForSeconds(0.3f);
    PlayItemBreakingAnimation(clone);
    yield return new WaitForSeconds(0.3f);
    PlayItemBreakingAnimation(clone);
    Destroy(clone);
  }

  private void PlayItemBreakingAnimation(GameObject itemChild) {
    var newChild = Instantiate(itemChild, itemChild.transform.parent);
    newChild.SetActive(true);
    var ftd = newChild.AddComponent<FadeThenDestroy>();
    ftd.fadeTime = 0.5f;
    ftd.shrink = -3;
  }
}
