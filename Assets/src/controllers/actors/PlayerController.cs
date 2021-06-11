using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PlayerController : ActorController, IBodyMoveHandler, ITakeAnyDamageHandler {
  Player player => (Player) actor;
  public static PlayerController current;
  private AudioSource sfxAudio;

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
    player.OnBossNewlySeen += HandleBossNewlySeen;
    player.OnMaxHPAdded += HandleMaxHPAdded;
    this.sfxAudio = GetComponent<AudioSource>();
  }

  private void HandleBossNewlySeen() {
    var boss = player.floor.bosses[0];
    if (boss != null) {
      StartCoroutine(AnimateBossSeen(boss));
    }
  }

  IEnumerator AnimateBossSeen(Boss b) {
    InteractionController.isInputAllowed = false;
    yield return Transitions.ZoomAndPanCamera(4, b.pos, 0.5f);
    yield return Transitions.ZoomAndPanCamera(4, b.pos, 3);
    yield return Transitions.ZoomAndPanCamera(4, player.pos, 0.5f);
    InteractionController.isInputAllowed = true;
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

  private void HandleMaxHPAdded() {
    EnqueueOverheadText("+ Max HP");
  }

  public override void HandleStatusAdded(Status status) {
    base.HandleStatusAdded(status);
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
    var model = GameModel.main;
    model.EnqueueEvent(() => {
      model.currentFloor.ForceAddVisibility(model.currentFloor.EnumerateFloor());
    });

    #if !UNITY_EDITOR
    Serializer.DeleteSave0();
    #endif
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
    sfxAudio.PlayOneShot(clip, volume);
  }

  public override void Update() {
    if (Input.GetKeyDown(KeyCode.V)) {
      player.SetTasks(new SleepTask(player, 100, true));
    }
    if (Input.GetKeyDown(KeyCode.Space)) {
      player.floor.ForceAddVisibility(player.floor.EnumerateFloor());
    }
    if (Input.GetKeyDown(KeyCode.S)) {
      Serializer.SaveMainToFile();
    }
    if (Input.GetKeyDown(KeyCode.L)) {
      GameModel.main = Serializer.LoadSave0();
      SceneManager.LoadSceneAsync("Scenes/Game");
    }
    if (Input.GetKeyDown(KeyCode.N)) {
      GameModel.GenerateNewGameAndSetMain();
      SceneManager.LoadSceneAsync("Scenes/Game");
    }
    if (Input.GetKeyDown(KeyCode.Equals)) {
      var e = GameModel.main.StepUntilPlayerChoice();
      while(e.MoveNext()) { }
      GameModel.main.PutPlayerAt(GameModel.main.depth + 1);
    }
    base.Update();
  }

  public override void HandleActionPerformed(BaseAction action, BaseAction initial) {
    if (action is WaitBaseAction) {
      var waitPrefab = Resources.Load<GameObject>("Effects/Wait");
      var wait = Instantiate(waitPrefab, new Vector3(actor.pos.x, actor.pos.y + 0.9f, 0), Quaternion.identity);
      AudioClipStore.main.playerWait.Play();
    } else if (action is GenericBaseAction) {
      AudioClipStore.main.playerGeneric.Play();
    } else if (action.Type == ActionType.ATTACK) {
      AudioClipStore.main.attack.Play();
    }
    base.HandleActionPerformed(action, initial);
  }

  public override void HandleInteracted(PointerEventData pointerEventData) {
    if (player.task != null) {
      player.ClearTasks();
    } else {
      // on clicking self, wait for 1 turn
      player.task = new WaitTask(player, 1);
    }
  }
}