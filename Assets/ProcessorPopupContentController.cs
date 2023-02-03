using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProcessorPopupContentController : MonoBehaviour {
  public Entity entity => GetComponentInParent<PopupController>().target;
  public Processor processor => entity as Processor;
  public InventoryController outputInventory;

  void Start() {
    outputInventory.inventory = processor.processedInventory;
  }

  void Update() {
    transform.Find("Actions/Process").gameObject.SetActive(processor.isActive);
  }
}
