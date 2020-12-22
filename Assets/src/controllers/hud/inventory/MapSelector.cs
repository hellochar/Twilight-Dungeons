using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public static class MapSelector {

  /// Contract: if player properly selects, return the Soil.
  /// If player cancels, throw PlayerSelectCanceledException.
  /// how to "send" messages to this method from another gameobject?
  public static async Task<T> Select<T>(IEnumerable<T> entities) where T : Entity {
    var mapSelectorPrefab = PrefabCache.UI.GetPrefabFor("Map Selector");
    var mapSelector = UnityEngine.GameObject.Instantiate(mapSelectorPrefab);
    var controller = mapSelector.GetComponent<MapSelectorController>();
    controller.entities = entities;
    T entity = null;
    bool cancelled = false;
    controller.OnSelected += (e) => entity = (T) e;
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