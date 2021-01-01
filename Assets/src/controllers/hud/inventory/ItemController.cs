using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

/// Renders one Item in the UI.
public class ItemController : MonoBehaviour {
  private static GameObject ActionButtonPrefab;
  public Item item;
  private Image image;
  private TMPro.TMP_Text stacksText;

  void Start() {
    if (ActionButtonPrefab == null) {
      ActionButtonPrefab = Resources.Load<GameObject>("UI/Action Button");
    }
    /// on click - toggle the popup for this item
    GetComponent<Button>().onClick.AddListener(HandleItemClicked);

    image = GetComponentInChildren<Image>(true);
    var wantedSprite = ObjectInfo.GetSpriteFor(item);
    if (wantedSprite != null) {
      image.sprite = wantedSprite;
      image.rectTransform.sizeDelta = wantedSprite.rect.size * 2;
    }

    stacksText = GetComponentInChildren<TMPro.TMP_Text>(true);
    Update();
  }

  private void HandleItemClicked() {
    Player player = GameModel.main.player;
    List<MethodInfo> methods = item.GetAvailableMethods(player);

    // put more fundamental actions later
    methods.Reverse();

    GameObject popup = null;

    var buttons = methods.Select((method) => MakeButton(method.Name, () => {
      method.Invoke(item, new object[] { player });
      PopupInteractionDone(popup);
    })).ToList();

    if (item is ItemSeed seed) {
      buttons.Insert(0, MakeButton("Plant", () => PlantWithUI(seed, player, popup)));
    }
    if (item is ItemCharmBerry charmBerry) {
      buttons.Insert(0, MakeButton("Charm", () => CharmWithUI(charmBerry, player, popup)));
    }
    if (item is ItemBoombugCorpse boombugCorpse) {
      buttons.Insert(0, MakeButton("Throw", () => ThrowBoombugCorpseWithUI(boombugCorpse, player, popup)));
    }
    if (item is ItemSnailShell snailShell) {
      buttons.Insert(0, MakeButton("Throw", () => ThrowSnailShellWithUI(snailShell, player, popup)));
    }

    popup = Popups.Create(
      title: item.displayName,
      info: item.GetStatsFull(),
      flavor: ObjectInfo.GetFlavorTextFor(item),
      sprite: image.gameObject,
      buttons: buttons
    );
    var popupMatchItem = popup.AddComponent<ItemPopupController>();
    popupMatchItem.item = item;
  }

  private GameObject MakeButton(string name, Action onClicked) {
    var button = Instantiate(ActionButtonPrefab, new Vector3(), Quaternion.identity);
    button.GetComponentInChildren<TMPro.TMP_Text>().text = name;
    button.GetComponent<Button>().onClick.AddListener(new UnityEngine.Events.UnityAction(onClicked));
    return button;
  }

  private void PopupInteractionDone(GameObject popup) {
    Player player = GameModel.main.player;
    Destroy(popup);
    // if (player.IsInCombat()) {
    //   CloseInventory();
    // }
  }

  public async void PlantWithUI(ItemSeed seed, Player player, GameObject popup) {
    CloseInventory();
    popup.SetActive(false);
    try {
      var soil = await MapSelector.SelectUI(
        GameModel.main.currentFloor.tiles.Where(tile => tile is Soil && tile.isVisible && tile.CanBeOccupied()).Cast<Soil>()
      );
      player.SetTasks(
        new MoveNextToTargetTask(player, soil.pos),
        new GenericTask(player, (p) => {
          if (p.IsNextTo(soil)) {
            seed.Plant(soil);
          }
        })
      );
      PopupInteractionDone(popup);
    } catch (PlayerSelectCanceledException) {
      // if player cancels selection, go back to before
      OpenInventory();
      popup.SetActive(true);
    }
  }

  public async void CharmWithUI(ItemCharmBerry charmBerry, Player player, GameObject popup) {
    CloseInventory();
    popup.SetActive(false);
    try {
      var enemy = await MapSelector.SelectUI(player.ActorsInSight(Faction.Enemy).Where((a) => a is AIActor).Cast<AIActor>());
      player.SetTasks(
        new ChaseTargetTask(player, enemy),
        new GenericTask(player, (_) => {
          charmBerry.Charm(enemy);
        })
      );
      PopupInteractionDone(popup);
    } catch (PlayerSelectCanceledException) {
      // if player cancels selection, go back to before
      OpenInventory();
      popup.SetActive(true);
    }
  }

  public async void ThrowBoombugCorpseWithUI(ItemBoombugCorpse corpse, Player player, GameObject popup) {
    CloseInventory();
    popup.SetActive(false);
    var floor = player.floor;
    try {
      var tile = await MapSelector.SelectUI(
        floor
          .EnumerateCircle(player.pos, player.visibilityRange)
          .Select(p => floor.tiles[p])
          .Where((p) => p.CanBeOccupied() && p.isVisible)
      );
      player.SetTasks(
        new GenericTask(player, (_) => {
          corpse.Throw(player, tile.pos);
        })
      );
      PopupInteractionDone(popup);
    } catch (PlayerSelectCanceledException) {
      // if player cancels selection, go back to before
      OpenInventory();
      popup.SetActive(true);
    }
  }
  public async void ThrowSnailShellWithUI(ItemSnailShell shell, Player player, GameObject popup) {
    CloseInventory();
    popup.SetActive(false);
    var floor = player.floor;
    try {
      var enemy = await MapSelector.SelectUI(player.ActorsInSight(Faction.Enemy).Concat(player.ActorsInSight(Faction.Neutral)));
      player.SetTasks(
        new GenericTask(player, (_) => {
          shell.Throw(player, enemy);
        })
      );
      PopupInteractionDone(popup);
    } catch (PlayerSelectCanceledException) {
      // if player cancels selection, go back to before
      OpenInventory();
      popup.SetActive(true);
    }
  }

  public void OpenInventory() {
    /// suuuper hack
    GameObject.Find("Canvas")
      .GetComponentsInChildren<Transform>(true)
      .First((c) => c.gameObject.name == "Inventory Container")
      .gameObject.SetActive(true);
  }

  public void CloseInventory() {
    GameObject.Find("Inventory Container")?.SetActive(false);
  }

  // Update is called once per frame
  void Update() {
    if (item is IStackable i) {
      stacksText.text = i.stacks.ToString();
    } else if (item is IDurable d) {
      if (d.durability < d.maxDurability) {
        stacksText.text = $"{d.durability}/{d.maxDurability}";
      } else {
        stacksText.text = "";
      }
    } else {
      stacksText.gameObject.SetActive(false);
    }
  }
}
