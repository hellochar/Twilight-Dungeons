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
  [NonSerialized]
  public Item item;
  public GameObject maturePlantBackground;
  public Image itemImage;
  private TMPro.TMP_Text stacksText;

  void Start() {
    /// on click - toggle the popup for this item
    GetComponent<Button>().onClick.AddListener(HandleItemClicked);

    var wantedSprite = ObjectInfo.GetSpriteFor(item);
    if (wantedSprite != null) {
      itemImage.sprite = wantedSprite;
    }

    if (item is ItemSeed seed && GameModel.main.player.inventory.HasItem(item)) {
      var plantType = seed.plantType;
      var plantPrefab = Resources.Load<GameObject>($"Entities/Plants/{plantType.Name}");
      var maturePlant = plantPrefab.transform.Find("Mature");
      var renderer = maturePlant.GetComponent<SpriteRenderer>();
      maturePlantBackground.GetComponent<Image>().sprite = renderer.sprite;
      var pos = itemImage.transform.localPosition;
      pos.y -= 2;
      itemImage.transform.localPosition = pos;
    } else {
      maturePlantBackground.SetActive(false);
    }

    stacksText = GetComponentInChildren<TMPro.TMP_Text>(true);
    Update();
  }

  private void HandleItemClicked() {
    ShowItemPopup(item, itemImage.gameObject);
  }

  public static void ShowItemPopup(Item item, GameObject image) {
    GameObject popup = null;
    List<(string, Action)> buttons = null;

    Player player = GameModel.main.player;
    if (item.inventory == player.inventory || item.inventory == player.equipment) {
      List<MethodInfo> methods = item.GetAvailableMethods(player);

      // put more fundamental actions later
      methods.Reverse();

      buttons = methods.Select((method) => {
        Action action = () => {
          // player.SetTasks(new GenericTask(player, (_) => {
          //   method.Invoke(item, new object[] { player });
          // }).Named(method.Name));
          try {
            method.Invoke(item, new object[] { player });
          } catch (TargetInvocationException outer) {
            if (outer.InnerException is CannotPerformActionException e) {
              GameModel.main.turnManager.OnPlayerCannotPerform(e);
            }
          }
        }; 
        return (method.Name, action);
      }).ToList();

      if (item is ItemSeed seed) {
        buttons.Insert(0, ("Plant", () => PlantWithUI(seed, player, popup)));
      }
      if (item is ItemCharmBerry charmBerry) {
        buttons.Insert(0, ("Charm", () => CharmWithUI(charmBerry, player, popup)));
      }
      if (item is ItemKingshroomPowder spores) {
        buttons.Insert(0, ("Use", () => PowderInfectWithUI(spores, player, popup)));
      }
      if (item is ItemBoombugCorpse boombugCorpse) {
        buttons.Insert(0, ("Throw", () => ThrowBoombugCorpseWithUI(boombugCorpse, player, popup)));
      }
      if (item is ItemSnailShell snailShell) {
        buttons.Insert(0, ("Throw", () => ThrowSnailShellWithUI(snailShell, player, popup)));
      }
    }

    popup = Popups.Create(
      title: item.displayName,
      category: GetCategoryForItem(item),
      info: item.GetStatsFull(),
      flavor: ObjectInfo.GetFlavorTextFor(item),
      sprite: image,
      buttons: buttons
    );
    var popupMatchItem = popup.AddComponent<ItemPopupController>();
    popupMatchItem.item = item;
  }

  private static string GetCategoryForItem(Item item) {
    switch (item) {
      case EquippableItem e:
        return e.slot.ToString();
      case IEdible e:
        return "Food";
      default:
        return "Item";
    }
  }

  public static async void PlantWithUI(ItemSeed seed, Player player, GameObject popup) {
    try {
      var soil = await MapSelector.SelectUI(
        GameModel.main.currentFloor.tiles.Where(tile => tile is Soil && tile.isVisible && tile.CanBeOccupied()).Cast<Soil>()
      );
      seed.MoveAndPlant(soil);
    } catch (PlayerSelectCanceledException) {}
  }

  public static async void PowderInfectWithUI(ItemKingshroomPowder powder, Player player, GameObject popup) {
    try {
      var enemy = await MapSelector.SelectUI(player.floor.AdjacentActors(player.pos).Where((a) => a != player));
      player.SetTasks(
        new ChaseTargetTask(player, enemy),
        new GenericPlayerTask(player, () => {
          powder.Infect(player, enemy);
        })
      );
    } catch (PlayerSelectCanceledException) {}
  }

  public static async void CharmWithUI(ItemCharmBerry charmBerry, Player player, GameObject popup) {
    try {
      var enemy = await MapSelector.SelectUI(player.ActorsInSight(Faction.Enemy).Where((a) => a is AIActor).Cast<AIActor>());
      player.SetTasks(
        new ChaseTargetTask(player, enemy),
        new GenericPlayerTask(player, () => {
          charmBerry.Charm(enemy);
        })
      );
    } catch (PlayerSelectCanceledException) {}
  }

  public static async void ThrowBoombugCorpseWithUI(ItemBoombugCorpse corpse, Player player, GameObject popup) {
    var floor = player.floor;
    try {
      var tile = await MapSelector.SelectUI(
        floor
          .EnumerateCircle(player.pos, player.visibilityRange)
          .Select(p => floor.tiles[p])
          .Where((p) => p.CanBeOccupied() && p.visibility == TileVisiblity.Visible)
      );
      player.SetTasks(
        new GenericPlayerTask(player, () => {
          corpse.Throw(player, tile.pos);
        })
      );
    } catch (PlayerSelectCanceledException) {}
  }
  public static async void ThrowSnailShellWithUI(ItemSnailShell shell, Player player, GameObject popup) {
    var floor = player.floor;
    try {
      var enemy = await MapSelector.SelectUI(player.ActorsInSight(Faction.Enemy).Concat(player.ActorsInSight(Faction.Neutral)));
      player.SetTasks(
        new GenericPlayerTask(player, () => {
          shell.Throw(player, enemy);
        })
      );
    } catch (PlayerSelectCanceledException) {}
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
