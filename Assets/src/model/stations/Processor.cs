using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;

[Serializable]
[ObjectInfo("processor", description: "Turns Grass into equipment.\nDrag in a Grass.")]
public class Processor : Station, IInteractableInventory {
  public override int maxDurability => 9;
  public ItemGrass itemGrass => inventory[0] as ItemGrass;
  public override bool isActive => itemGrass != null;
  public Inventory processedInventory = new Inventory(1);
  public Item processedItem => processedInventory[0];
  public override string popupPrefab => "ProcessorPopup";
  public Processor(Vector2Int pos) : base(pos) {
    inventory.allowDragAndDrop = true;
    HandleDeserialized();
  }

  [OnDeserialized]
  void HandleDeserialized() {
    inventory.OnItemAdded += HandleOnItemAdded;
    inventory.OnItemRemoved += HandleOnItemRemoved;
  }

  private void HandleOnItemAdded(Item item, Entity source) {
    if (itemGrass != null) {
      processedInventory.AddItem(EntityExtensions.GetHomeItem(itemGrass.grassType));
    }
  }

  private void HandleOnItemRemoved(Item obj) {
    processedInventory.RemoveItem(processedItem);
  }

  public override string description => isActive ?
    $"{itemGrass.displayName} will become {processedItem.displayName}." :
    base.description;

  public override void GetAvailablePlayerActions(List<MethodInfo> methods) {
    // if (isActive) {
      methods.Add(GetType().GetMethod("Process"));
    // }
  }

  public void Process() {
    if (!isActive) {
      throw new CannotPerformActionException("Drag in a Grass!");
    }
    var player = GameModel.main.player;
    if (!player.inventory.AddItem(processedItem, this)) {
      throw new CannotPerformActionException("Inventory full!");
    }
    itemGrass.stacks--;
    // settings stacks to 0 will Destroy() the item, removing it from the inventory
    // and making itemGrass null.
    if (itemGrass != null) {
      // we still have more stacks; create another processed item
      HandleOnItemAdded(itemGrass, this);
    }
  }
}