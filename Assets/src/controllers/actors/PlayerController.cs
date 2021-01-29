using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerController : ActorController {
  Player player => (Player) actor;

  public override void Start() {
    base.Start();
    player.inventory.OnItemAdded += HandleInventoryItemAdded;
    player.equipment.OnItemAdded += HandleEquipmentItemAdded;
    player.equipment.OnItemRemoved += HandleItemRemoved;
    GameModel.main.OnPlayerChangeFloor += HandlePlayerChangeFloor;
  }

  public override void HandleDeath() {
    base.HandleDeath();
    var model = GameModel.main;
    model.EnqueueEvent(() => {
      model.currentFloor.ForceAddVisibility(model.currentFloor.EnumerateFloor());
    });
  }

  private void HandleInventoryItemAdded(Item arg1, Entity arg2) {
    AudioClipStore.main.playerPickupItem.PlayAtPoint(transform.position);
  }

  public override void HandleHeal(int heal) {
    base.HandleHeal(heal);
    AudioClipStore.main.playerHeal.PlayAtPoint(transform.position);
  }

  private void HandleItemRemoved(Item obj) {
    PlayEquipSound();
  }

  private void HandleEquipmentItemAdded(Item arg1, Entity arg2) {
    PlayEquipSound();
  }

  private void HandlePlayerChangeFloor(Floor arg1, Floor arg2) {
    // AudioClipStore.main.playerTakeStairs.Play(1);
  }


  private void PlayEquipSound() {
    AudioClipStore.main.playerEquip.PlayAtPoint(transform.position);
  }

  public override void Update() {
    if (Input.GetKeyDown(KeyCode.V)) {
      player.SetTasks(new SleepTask(player));
    }
    if (Input.GetKeyDown(KeyCode.Space)) {
      player.floor.ForceAddVisibility(player.floor.EnumerateFloor());
    }
    var model = GameModel.main;
    if (Input.GetKeyDown(KeyCode.Equals)) {
      GameModel.main.PutPlayerAt(model.floors[model.activeFloorIndex + 1], false);
    } else if (Input.GetKeyDown(KeyCode.Minus)) {
      GameModel.main.PutPlayerAt(model.floors[model.activeFloorIndex - 1], false);
    }
    base.Update();
  }

  public override void HandleActionPerformed(BaseAction action, BaseAction initial) {
    if (action is WaitBaseAction) {
      var waitPrefab = Resources.Load<GameObject>("Effects/Wait");
      var wait = Instantiate(waitPrefab, new Vector3(actor.pos.x, actor.pos.y + 0.9f, 0), Quaternion.identity);
      AudioClipStore.main.playerWait.PlayAtPoint(transform.position);
    } else if (action is GenericBaseAction) {
      AudioClipStore.main.playerGeneric.PlayAtPoint(transform.position);
    }
    base.HandleActionPerformed(action, initial);
  }

  public override void PointerClick(PointerEventData pointerEventData) {
    if (player.task != null) {
      player.ClearTasks();
    } else {
      // on clicking self, wait for 1 turn
      player.task = new WaitTask(player, 1);
    }
  }
}