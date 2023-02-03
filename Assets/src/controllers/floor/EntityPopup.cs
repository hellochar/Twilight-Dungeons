using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public interface IPopupOverride {
  void HandleShowPopup();
}

public static class EntityPopup {
  public static void Show(Entity entity) {
    var floorController = FloorController.current;
    GameObject entityGameObject = floorController.GameObjectFor(entity);
    if (entityGameObject.TryGetComponent<IPopupOverride>(out var popupOverride)) {
      popupOverride.HandleShowPopup();
      return;
    }

    string description = entity.description + "\n\n";
    if (entity is Body b) {
      if (b is Actor a && a.BaseAttackDamage() != (0, 0)) {
        description += "Deals " + Util.DescribeDamageSpread(a.BaseAttackDamage());
      }
      if (b.maxHp > 0) {
        description += $"Max HP: {b.maxHp}\n";
      }
    }

    var spritePrefab = PrefabCache.UI.GetPrefabFor("Entity Image");
    var spriteGameObject = UnityEngine.Object.Instantiate(spritePrefab);
    var image = spriteGameObject.GetComponentInChildren<Image>();
    var sprite = ObjectInfo.GetSpriteFor(entity) ?? entityGameObject.GetComponentInChildren<SpriteRenderer>()?.sprite;
    image.sprite = sprite;
    image.color = entityGameObject.GetComponentInChildren<SpriteRenderer>().color;
    List<(string, Action)> buttons = new List<(string, Action)>();
    Inventory inventory = null;

    var player = GameModel.main.player;
    // if (player.floor.depth == 0) {
      List<MethodInfo> playerActions = entity.GetType().GetMethods()
        .Where(m => m.GetCustomAttributes(typeof(PlayerActionAttribute), true).Any())
        .ToList();

      entity.GetAvailablePlayerActions(playerActions);

      foreach(var action in playerActions) {
        buttons.Add((Util.WithSpaces(action.Name), () => {
          try {
            action.Invoke(entity, new object[0]);
          } catch (CannotPerformActionException e) {
            GameModel.main.turnManager.OnPlayerCannotPerform(e);
          }
        }));
      }
    // }
    if (entity is IInteractableInventory i) {
      inventory = i.inventory;
    }

    var controller = Popups.CreateStandard(
      title: entity.displayName,
      category: GetCategoryForEntity(entity),
      info: description.Trim(),
      flavor: ObjectInfo.GetFlavorTextFor(entity),
      sprite: spriteGameObject,
      buttons: buttons,
      inventory: inventory,
      entity: entity,
      prefab: entity.popupPrefab ?? "StandardPopupContent"
    );
    controller.target = entity;
    controller.Init(TextAnchor.MiddleRight);
    CameraController.main.SetCameraOverride(controller);
    UnityEngine.Object.Destroy(spriteGameObject);
  }

  private static string GetCategoryForEntity(Entity entity) {
    switch (entity) {
      case Tile t:
        return "Tile";
      case Actor a:
        return "Creature";
      case Grass g:
        return "Grass";
      case Destructible d:
        return "Destructible";
      default:
        return "Other";
    }
  }

}