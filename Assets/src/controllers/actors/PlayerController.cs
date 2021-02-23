using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PlayerController : ActorController, IBodyMoveHandler, ITakeAnyDamageHandler {
  Player player => (Player) actor;

  public override void Start() {
    base.Start();
    player.inventory.OnItemAdded += HandleInventoryItemAdded;
    player.equipment.OnItemAdded += HandleEquipmentItemAdded;
    player.equipment.OnItemRemoved += HandleItemRemoved;
    GameModel.main.OnPlayerChangeFloor += HandlePlayerChangeFloor;
  }

  void OnApplicationPause(bool isPaused) {
    if (isPaused) {
      player.ClearTasks();
    }
  }

  void OnApplicationFocus(bool hasFocus) {
    if (!hasFocus) {
      player.ClearTasks();
    }
  }

  public override void HandleDeath(Entity source) {
    base.HandleDeath(source);
    var model = GameModel.main;
    model.EnqueueEvent(() => {
      model.currentFloor.ForceAddVisibility(model.currentFloor.EnumerateFloor());
    });

    #if !UNITY_EDITOR
    Serializer.DeleteSave();
    #endif
  }

  private void HandleInventoryItemAdded(Item arg1, Entity arg2) {
    AudioClipStore.main.playerPickupItem.PlayAtPoint(transform.position);
  }

  public void HandleMove(Vector2Int arg1, Vector2Int arg2) {
    AudioClipStore.main.move.PlayAtPoint(transform.position, 0.5f);
  }

  public override void HandleHeal(int heal) {
    base.HandleHeal(heal);
    AudioClipStore.main.playerHeal.PlayAtPoint(transform.position);
  }

  public override void HandleTakeAnyDamage(int dmg) {
    if (dmg > 0) {
      var store = AudioClipStore.main;
      var clip = Util.RandomPickParams(store.playerHurt1, store.playerHurt2, store.playerHurt3);
      clip.PlayAtPoint(transform.position);
    }
    base.HandleTakeAnyDamage(dmg);
    /// treat tutorial specially
    if (player.floor is TutorialFloor && dmg >= player.hp) {
      player.Heal(8);
      player.pos = new Vector2Int(3, 4);
      Messages.Create("There is permadeath in the real game.");
    }
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
      player.SetTasks(new SleepTask(player, 100, true));
    }
    if (Input.GetKeyDown(KeyCode.Space)) {
      player.floor.ForceAddVisibility(player.floor.EnumerateFloor());
    }
    if (Input.GetKeyDown(KeyCode.S) && Input.GetKey(KeyCode.LeftControl)) {
      Serializer.SaveMainToFile();
    }
    if (Input.GetKeyDown(KeyCode.L) &Input.GetKey(KeyCode.LeftControl)) {
      GameModel.main = Serializer.LoadFromFile();
      SceneManager.LoadSceneAsync("Scenes/Game");
    }
    var model = GameModel.main;
    if (Input.GetKeyDown(KeyCode.Equals)) {
      GameModel.main.PutPlayerAt(model.depth + 1);
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
    } else if (action.Type == ActionType.ATTACK) {
      AudioClipStore.main.attack.PlayAtPoint(transform.position);
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