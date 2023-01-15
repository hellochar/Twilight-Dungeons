using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class Rewards {
  public List<Inventory> inventories;
  private Inventory chosenInventory;

  public Rewards(List<Inventory> inventories) {
    this.inventories = inventories;
  }

  public async Task<Inventory> ShowRewardUIAndWaitForChoice() {
    ShowUI();
    while (true) {
      await Task.Delay(16);
      if (chosenInventory != null) {
        return chosenInventory;
      }
      // if (cancelled) {
      //   throw new PlayerSelectCanceledException();
      // }
    }
  }

  public void ShowUI() {
    var parent = GameObject.Find("Canvas").transform;
    var obj = PrefabCache.UI.Instantiate("Reward UI", parent);
    obj.GetComponent<RewardUIController>().rewards = this;
  }

  internal void Choose(Inventory inventory) {
    chosenInventory = inventory;
    // add to home
    inventory.TryDropAllItems(GameModel.main.home, GameModel.main.home.center);
  }
}