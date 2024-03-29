﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerBackpackController : MonoBehaviour {
  public GameObject inventoryContainer;
  private Inventory inventory => GameModel.main.player.inventory;

  void Start() {
    inventory.OnItemAdded += HandleItemAdded;
  }

  private void HandleItemAdded(Item item, Entity source) {
    if (source != null) {
      var slot = HUDController.main.playerInventory.GetSlot(item);
      if (slot != null) {
        var sprite = ObjectInfo.GetSpriteFor(item);
        /// Create the animation of the item coming in/out of the backpack slot
        SpriteFlyAnimation.Create(sprite, Util.withZ(source.pos), slot.gameObject);
      }
    }
  }

  public void ToggleInventory() {
    inventoryContainer.SetActive(!inventoryContainer.activeSelf);
  }

  public void Inspect() {
    new InspectAction().ShowTargetingUIThenPerform(GameModel.main.player);
  }

  class InspectAction : ITargetedAction<Entity> {
    public string TargettedActionName => "Inspect";

    public string TargettedActionDescription => "Choose a creature to Inspect.";

    public void PerformTargettedAction(Player player, Entity target) {
      EntityPopup.Show(target);
    }

    public IEnumerable<Entity> Targets(Player player) {
      var floor = player.floor;
      return floor.bodies.Cast<Entity>().Concat(floor.grasses);
    }
  }
}
