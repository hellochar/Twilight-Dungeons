using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// Adds and removes Tile prefabs to match the state of a Floor variable.
public class FloorController : MonoBehaviour {

  [NonSerialized]
  public Floor floor;
  public static Dictionary<System.Type, GameObject> EntityPrefabs = new Dictionary<System.Type, GameObject>();
  public Dictionary<Entity, GameObject> gameObjectMap = new Dictionary<Entity, GameObject>();
  public static FloorController current => GameModelController.main.CurrentFloorController;

  public static GameObject GetEntityPrefab(Entity e) {
    if (e == null) {
      return null;
    }
    var type = e.GetType();
    return GetEntityPrefab(type);
  }

  public static GameObject GetEntityPrefab(Type type) {
    if (!EntityPrefabs.ContainsKey(type)) {
      if (type == typeof(Player)) {
        EntityPrefabs.Add(type, Resources.Load<GameObject>("Player"));
      } else {
        string category = "";
        if (type.IsSubclassOf(typeof(Tile))) {
          category = "Tiles/";
        } else if (type.IsSubclassOf(typeof(Grass))) {
          category = "Grasses/";
        } else if (type.IsSubclassOf(typeof(Plant))) {
          category = "Plants/";
        } else if (type.IsSubclassOf(typeof(Body))) {
          category = "Actors/";
        }
        string resourcePath = $"Entities/{category}{type.Name}";
        if (type.IsSubclassOf(typeof(ItemOnGround))) {
          resourcePath = "Entities/ItemOnGround";
        }
        EntityPrefabs.Add(type, Resources.Load<GameObject>(resourcePath));
      }
    }
    return EntityPrefabs[type];
  }

  public GameObject GameObjectFor(Entity entity) {
    if (gameObjectMap.TryGetValue(entity, out var go)) {
      return go;
    }
    return null;
  }

  // Start is called before the first frame update
  public virtual void Start() {
    this.InstantiateGameObjectsToMatchFloor();
    floor.OnEntityAdded += HandleEntityAdded;
    floor.OnEntityRemoved += HandleEntityRemoved;
  }

  void OnDestroy() {
    floor.OnEntityAdded -= HandleEntityAdded;
    floor.OnEntityRemoved -= HandleEntityRemoved;
  }

  void HandleEntityRemoved(Entity e) {
    if (e == GameModel.main.player) {
      gameObjectMap.Remove(e);
      return;
    }
    if (gameObjectMap.TryGetValue(e, out var currentObject)) {
      if (currentObject.TryGetComponent<IEntityControllerRemoveOverride>(out var removeOverride)) {
        removeOverride.OverrideRemoved();
      } else {
        currentObject.AddComponent<FadeThenDestroy>();
      }
      gameObjectMap.Remove(e);
      // ignore triggers for warnings
    } else if (!(e is Trigger)) {
      Debug.LogWarning("" + e + " was removed from floor " + floor + " but didn't have a GameObject.");
    }
  }

  void HandleEntityAdded(Entity e) {
    InstantiateGameObjectForEntity(e);
  }

  void InstantiateGameObjectsToMatchFloor() {
    foreach (var e in floor.entities) {
      InstantiateGameObjectForEntity(e);
    }
    // var tempWalls = floor.EnumeratePerimeter(-1).Select(p => {
    //   var w = new Wall(p);
    //   w.visibility = TileVisiblity.Visible;
    //   return w;
    // });
    // foreach (var e in tempWalls) {
    //   InstantiateGameObjectForEntity(e);
    // }
    // add one more perimeter with temp entities
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
      } else if (entity is Body body) {
        gameObject.GetComponent<BodyController>().body = body;
      } else if (entity is Grass grass) {
        gameObject.GetComponent<GrassController>().grass = grass;
      } else if (entity is ItemOnGround itemOnGround) {
        gameObject.GetComponent<ItemOnGroundController>().itemOnGround = itemOnGround;
      } else if (entity is Soil s) {
        gameObject.GetComponent<SoilController>().soil = s;
      } else if (entity is Piece p) {
        gameObject.GetComponent<PieceController>().piece = p;
      }
      gameObjectMap[entity] = gameObject;
    } else if (!(entity is Trigger)) {
      Debug.LogWarning($"Couldn't find prefab for {entity.GetType()}");
    }
  }

  /// Layer here refers to Entity layers - tile (lowest), grass, item, body (highest)
  public Entity[] GetVisibleEntitiesInLayerOrder(Vector2Int pos) {
    if (!floor.InBounds(pos)) {
      return new Entity[0];
    }

    var tile = floor.tiles[pos];
    var body = tile.body;
    var itemOnGround = tile.item;
    var grass = tile.grass;
  
    var visibleEntities = new List<Entity>();
    if (body != null) visibleEntities.Add(body);
    if (itemOnGround != null) visibleEntities.Add(itemOnGround);
    if (grass != null) visibleEntities.Add(grass);
    if (floor is HomeFloor f && f.soils[pos] != null) visibleEntities.Add(f.soils[pos]);
    if (tile?.isExplored ?? false) visibleEntities.Add(tile);

    return visibleEntities.ToArray();
  }

  public bool TryGetFirstControllerComponent<T>(Entity[] entities, out T component, out Entity e) {
    foreach (var entity in entities) {
      if (TryGetControllerComponent<T>(entity, out component)) {
        e = entity;
        return true;
      }
    }
    e = null;
    component = default(T);
    return false;
  }

  /// find gameObject for entity in floor controller, see if it has a IPlayerInteractHandler
  public bool TryGetControllerComponent<T>(Entity e, out T component) {
    if (e != null && gameObjectMap.TryGetValue(e, out var gameObject)) {
      if (gameObject.TryGetComponent<T>(out component)) {
        return true;
      }
    }
    component = default(T);
    return false;
  }

  void OnDrawGizmos() {
    foreach(var room in floor.rooms) {
      Gizmos.color = new Color(1, 1, 1, 0.25f);
      // Gizmos.DrawCube(room.centerFloat, new Vector3(room.width, room.height, 1));
      Gizmos.DrawWireCube(room.centerFloat, new Vector3(room.width, room.height, 1));
    }
  }
}