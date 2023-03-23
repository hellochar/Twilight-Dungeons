using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public static class MapSelector {

  /// Contract: if player properly selects, return the Soil.
  /// If player cancels, throw PlayerSelectCanceledException.
  /// how to "send" messages to this method from another gameobject?
  public static async Task<Entity> SelectUI(List<Entity> entities, string message = "") {
    var mapSelectorPrefab = PrefabCache.UI.GetPrefabFor("Map Selector");
    var mapSelector = UnityEngine.GameObject.Instantiate(mapSelectorPrefab);
    var controller = mapSelector.GetComponent<MapSelectorController>();
    controller.entities = new List<Entity>(entities);
    controller.message = message;
    Entity entity = null;
    bool cancelled = false;
    controller.OnSelected += (e) => entity = e;
    controller.OnCancelled += () => cancelled = true;
    while (true) {
      await Task.Delay(16);
      if (entity != null) {
        return entity;
      }
      if (cancelled) {
        throw new PlayerSelectCanceledException();
      }
    }
  }
}