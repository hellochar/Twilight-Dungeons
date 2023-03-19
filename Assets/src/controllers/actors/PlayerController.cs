using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PlayerController : ActorController, IBodyMoveHandler, ITakeAnyDamageHandler, IDealAttackDamageHandler {
  Player player => (Player) actor;
  public static PlayerController current;
  private AudioSource sfxAudio;
  public Sprite deadSprite;

  void Awake() {
    PlayerController.current = this;
  }

  public override void Start() {
    base.Start();
    player.inventory.OnItemAdded += HandleInventoryItemAdded;
    player.equipment.OnItemAdded += HandleEquipmentItemAdded;
    player.equipment.OnItemRemoved += HandleEquipmentItemRemoved;
    player.equipment.OnItemDestroyed += HandleEquipmentDestroyed;
    player.OnChangeWater += HandleChangeWater;
    player.OnChangeOrganicMatter += HandleChangeOrganicMatter;
    player.OnMaxHPAdded += HandleMaxHPAdded;
    this.sfxAudio = GetComponent<AudioSource>();
  }

  private void HandleChangeWater(int delta) {
    /// ignore passive water lost
    if (Math.Abs(delta) <= 1) {
      return;
    }
    AudioClipStore.main.playerChangeWater.Play(0.2f);
    var worldText = PrefabCache.UI.Instantiate("WorldText", transform);
    if (delta > 0) {
      worldText.GetComponent<TMPro.TMP_Text>().text = delta.ToString("+0");
      SpriteFlyAnimation.Create(MasterSpriteAtlas.atlas.GetSprite("water_0"), transform.position, GameObject.Find("Water Droplet"));
    } else {
      worldText.GetComponent<TMPro.TMP_Text>().text = delta.ToString("# <sprite name=water>");
    }
  }

  private void HandleChangeOrganicMatter(int delta) {
    var worldText = PrefabCache.UI.Instantiate("WorldText", transform);
    if (delta > 0) {
      worldText.GetComponent<TMPro.TMP_Text>().text = delta.ToString("+0 <sprite name=organic-matter>");
      SpriteFlyAnimation.Create(MasterSpriteAtlas.atlas.GetSprite("plant-matter"), transform.position, GameObject.Find("Organic Matter Icon"));
    } else {
      worldText.GetComponent<TMPro.TMP_Text>().text = delta.ToString("# <sprite name=organic-matter>");
    }
  }

  void OnApplicationPause(bool isPaused) {
    if (isPaused) {
      player?.ClearTasks();
    }
  }

  void OnApplicationFocus(bool hasFocus) {
    if (!hasFocus) {
      player?.ClearTasks();
    }
  }

  private void HandleMaxHPAdded() {
    EnqueueOverheadText("+ Max HP");
    EnqueueOverheadText("Healed to Full");
  }

  public override void HandleStatusAdded(Status status) {
    base.HandleStatusAdded(status);
    if (status.isDebuff) {
      AudioClipStore.main.playerGetDebuff.Play();
    }
    EnqueueOverheadText(status.displayName);
  }

  private Queue<string> overheadTextQueue = new Queue<string>();
  private Coroutine overheadTextQueueCoroutine;
  public void EnqueueOverheadText(string s) {
    overheadTextQueue.Enqueue(s);
    /// ensure coroutine is started
    if (overheadTextQueueCoroutine == null) {
      overheadTextQueueCoroutine = StartCoroutine(StaggerOverheadText());
    }
  }

  private IEnumerator StaggerOverheadText() {
    while (overheadTextQueue.Any()) {
      var worldText = PrefabCache.UI.Instantiate("WorldText", transform);
      worldText.GetComponent<TMPro.TMP_Text>().text = overheadTextQueue.Dequeue();
      // worldText lasts 2 seconds; go a bit faster
      yield return new WaitForSeconds(0.5f);
    }
    overheadTextQueueCoroutine = null;
  }

  public override void HandleDeath(Entity source) {
    base.HandleDeath(source);
    animator.enabled = false;
    sprite.GetComponent<SpriteRenderer>().sprite = deadSprite;
    sprite.transform.Find("Equipment").gameObject.SetActive(false);
    statuses.SetActive(false);
  }

  private void HandleInventoryItemAdded(Item arg1, Entity arg2) {
    AudioClipStore.main.playerPickupItem.Play();
  }

  public void HandleMove(Vector2Int arg1, Vector2Int arg2) {
    AudioClipStore.main.move.Play(0.25f);
  }

  public override void HandleHeal(int heal) {
    base.HandleHeal(heal);
    AudioClipStore.main.playerHeal.Play();
  }

  public override void HandleTakeAnyDamage(int dmg) {
    if (dmg > 0) {
      var store = AudioClipStore.main;
      var clip = Util.RandomPickParams(store.playerHurt1, store.playerHurt2, store.playerHurt3);
      clip.Play(3f);
    }
    base.HandleTakeAnyDamage(dmg);
    /// treat tutorial specially
    if (player.floor is TutorialFloor && dmg >= player.hp) {
      player.Heal(8);
      player.pos = new Vector2Int(3, 4);
      Messages.Create("There is permadeath in the real game.");
    }
  }

  private void HandleEquipmentItemRemoved(Item obj) {
    PlayEquipSound();
  }

  private void HandleEquipmentItemAdded(Item arg1, Entity arg2) {
    PlayEquipSound();
  }

  private void HandleEquipmentDestroyed(Item obj) {
    IEnumerator DelayedPlay() {
      yield return new WaitForSeconds(0.25f);
      AudioClipStore.main.playerEquipmentBreak.Play();
    }
    StartCoroutine(DelayedPlay());
  }

  private void PlayEquipSound() {
    AudioClipStore.main.playerEquip.Play();
  }

  public void PlaySFX(AudioClip clip, float volume = 1) {
    if (sfxAudio != null) {
      sfxAudio.PlayOneShot(clip, volume);
    }
  }

  public override void Update() {
    #if UNITY_EDITOR
    UpdateDebug();
    #endif
    base.Update();
  }

  void UpdateDebug() {
    if (IngameDebugConsole.DebugLogManager.Instance.IsLogWindowVisible) {
      return;
    }
    if (InteractionController.current == null) {
      return;
    }

    Action<Vector2Int, PointerEventData> Interact = InteractionController.current.Interact;
    if (Input.GetKeyDown(KeyCode.W)) {
      var pos = GameModel.main.player.pos + Vector2Int.up;
      Interact(pos, null);
    } else if (Input.GetKeyDown(KeyCode.A)) {
      var pos = GameModel.main.player.pos + Vector2Int.left;
      Interact(pos, null);
    } else if (Input.GetKeyDown(KeyCode.S)) {
      var pos = GameModel.main.player.pos + Vector2Int.down;
      Interact(pos, null);
    } else if (Input.GetKeyDown(KeyCode.D)) {
      var pos = GameModel.main.player.pos + Vector2Int.right;
      Interact(pos, null);
    } else if (Input.GetKeyDown(KeyCode.Space)) {
      // wait a turn
      Interact(GameModel.main.player.pos, null);
    }
    // gameObject
    if (Input.GetKeyDown(KeyCode.K)) {
      Serializer.SaveMainToFile();
    }
    if (Input.GetKeyDown(KeyCode.L)) {
      GameModel.main = Serializer.LoadSave0(false);
      SceneManager.LoadSceneAsync("Scenes/Game");
    }
    if (Input.GetKeyDown(KeyCode.N)) {
      GameModel.GenerateNewGameAndSetMain();
      SceneManager.LoadSceneAsync("Scenes/Game");
    }
    if (Input.GetKeyDown(KeyCode.Minus)) {
      var e = GameModel.main.StepUntilPlayerChoice();
      while(e.MoveNext()) { }
      GameModel.main.PutPlayerAt(0);
    }
    if (Input.GetKeyDown(KeyCode.Equals)) {
      var e = GameModel.main.StepUntilPlayerChoice();
      while(e.MoveNext()) { }
      GameModel.main.currentFloor.PlayerGoDownstairs();
      // GameModel.main.PutPlayerAt(GameModel.main.cave.depth + 1);
    }
    if (Input.GetKeyDown(KeyCode.R)) {
      GameModel.main = Serializer.LoadSave0(false);
      GameModel.main.floorSeeds[GameModel.main.cave.depth + 1] = new System.Random().Next();
      var e = GameModel.main.StepUntilPlayerChoice();
      while(e.MoveNext()) { }
      GameModel.main.currentFloor.PlayerGoDownstairs();
      GameModel.main.DrainEventQueue();
      player.floor.ForceAddVisibility();
      SceneManager.LoadSceneAsync("Scenes/Game");
    }
    if (Input.GetKeyDown(KeyCode.Z)) {
      ScreenCapture.CaptureScreenshot("screenshot.png", 2);
    }
  }

  public override void HandleActionPerformed(BaseAction action, BaseAction initial) {
    if (action is WaitBaseAction) {
      var waitPrefab = PrefabCache.Effects.GetPrefabFor("Wait");
      var wait = Instantiate(waitPrefab, new Vector3(actor.pos.x, actor.pos.y + 0.9f, 0), Quaternion.identity);
      AudioClipStore.main.playerWait.Play();
    } else if (action is GenericBaseAction) {
      AudioClipStore.main.playerGeneric.Play();
    }
    base.HandleActionPerformed(action, initial);
  }

  public override PlayerInteraction GetPlayerInteraction(PointerEventData pointerEventData) {
    if (player.task != null) {
      return new ArbitraryPlayerInteraction(() => {
        player.ClearTasks();
      });
    } else {
      // if (player.floor is HomeFloor) {
      //   return new ArbitraryPlayerInteraction(() => EntityPopup.Show(player));
      // }
      return new SetTasksPlayerInteraction(
        new WaitTask(player, 1)
      );
    }
  }

  public void HandleDealAttackDamage(int damage, Body target) {
    if (damage > 0) {
      AudioClipStore.main.attack.Play();
    } else {
      AudioClipStore.main.attackNoDamage.Play();
    }
  }
}