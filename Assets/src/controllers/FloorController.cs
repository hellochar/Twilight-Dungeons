using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

/// Adds and removes Tile prefabs to match the state of a Floor variable.
public class FloorController : MonoBehaviour, IPointerClickHandler {

  public Floor floor;
  private Dictionary<System.Type, GameObject> EntityPrefabs = new Dictionary<System.Type, GameObject>();
  private Dictionary<Entity, GameObject> gameObjectMap = new Dictionary<Entity, GameObject>();

  public GameObject GetEntityPrefab(Entity e) {
    var type = e.GetType();
    if (!EntityPrefabs.ContainsKey(type)) {
      string category = "";
      switch (e) {
        case Tile t:
          category = "Tiles/";
          break;
        case Grass g:
          category = "Grasses/";
          break;
      }
      string resourcePath = $"Entities/{category}{type.Name}";
      EntityPrefabs.Add(type, Resources.Load<GameObject>(resourcePath));
    }
    return EntityPrefabs[type];
  }

  // Start is called before the first frame update
  void Start() {
    this.InstantiateGameObjectsToMatchFloor();
    floor.OnEntityAdded += HandleEntityAdded;
    floor.OnEntityRemoved += HandleEntityRemoved;
  }

  void HandleEntityRemoved(Entity e) {
    if (e == GameModel.main.player) {
      gameObjectMap.Remove(e);
      return;
    }
    GameObject currentObject = gameObjectMap[e];
    if (currentObject == null) {
      Debug.LogWarning("" + e + " was removed from floor " + floor + " but didn't have a GameObject.");
    }
    currentObject.AddComponent<FadeThenDestroy>();
    gameObjectMap.Remove(e);
  }

  void HandleEntityAdded(Entity e) {
    InstantiateGameObjectForEntity(e);
  }

  void InstantiateGameObjectsToMatchFloor() {
    foreach (var e in floor.entities) {
      InstantiateGameObjectForEntity(e);
    }
  }

  private void InstantiateGameObjectForEntity(Entity entity) {
    /// Player is Instantiated separately, but we still associate it to the gameobjectmap
    /// so it gets caught by the OnPointerClick code
    if (entity is Player) {
      var gameObject = GameObject.Find("Player");
      gameObjectMap[entity] = gameObject;
      return;
    }
    if (gameObjectMap.ContainsKey(entity)) {
      throw new System.Exception("Creating gameObject for entity that already has one" + entity);
    }
    GameObject prefab = GetEntityPrefab(entity);
    if (prefab != null) {
      Vector3 pos = new Vector3(entity.pos.x, entity.pos.y, prefab.transform.position.z);
      GameObject gameObject = Instantiate(prefab, pos, Quaternion.identity, this.transform);
      if (entity is Tile tile) {
        gameObject.GetComponent<TileController>().tile = tile;
      } else if (entity is Actor actor) {
        gameObject.GetComponent<ActorController>().actor = actor;
      } else if (entity is Grass grass) {
        gameObject.GetComponent<GrassController>().grass = grass;
      } else if (entity is ItemOnGround itemOnGround) {
        gameObject.GetComponent<ItemOnGroundController>().itemOnGround = itemOnGround;
      }
      gameObjectMap[entity] = gameObject;
    } else {
      Debug.LogError($"Couldn't find prefab for {entity.GetType()}");
    }
  }

  public void OnPointerClick(PointerEventData eventData) {
    if (!CameraZoom.IsZoomGuardActive) {
      var worldPos = eventData.pointerCurrentRaycast.worldPosition;
      var pos = new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.y));
      UserInteractAt(pos, eventData);
    }
  }

  public void UserInteractAt(Vector2Int pos, PointerEventData eventData) {
    var tile = floor.tiles[pos];
    var actor = tile.actor;
    var itemOnGround = tile.item;
    var grass = tile.grass;

    if (TryGetFirstEntityClickHandler(out var handler, actor, grass, itemOnGround, tile)) {
      handler.PointerClick(eventData);
    }
  }

  bool TryGetEntityClickHandler(Entity e, out IEntityClickedHandler handler) {
    if (e != null && gameObjectMap.TryGetValue(e, out var gameObject)) {
      if (gameObject.TryGetComponent<IEntityController>(out var controller)) {
        if (controller is IEntityClickedHandler h) {
          handler = h;
          return true;
        }
      }
    }
    handler = null;
    return false;
  }

  bool TryGetFirstEntityClickHandler(out IEntityClickedHandler handler, params Entity[] entities) {
    foreach (var entity in entities) {
      if (TryGetEntityClickHandler(entity, out handler)) {
        return true;
      }
    }
    handler = null;
    return false;
  }
}
