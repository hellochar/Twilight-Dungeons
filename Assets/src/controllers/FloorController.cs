﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// Adds and removes Tile prefabs to match the state of a Floor variable.
public class FloorController : MonoBehaviour {

  public Floor floor;
  private Dictionary<System.Type, GameObject> EntityPrefabs = new Dictionary<System.Type, GameObject>();
  private Dictionary<Entity, GameObject> gameObjectMap = new Dictionary<Entity, GameObject>();

  public GameObject GetEntityPrefab(Entity e) {
    var type = e.GetType();
    if (!EntityPrefabs.ContainsKey(type)) {
      string resourcePath = $"Entities/{type.Name}";
      EntityPrefabs.Add(type, Resources.Load<GameObject>(resourcePath));
    }
    return EntityPrefabs[type];
  }

  // Start is called before the first frame update
  void Start() {
    this.instantiateGameObjectsToMatchFloor();
    // TODO do the same thing for tile changes
    floor.OnEntityAdded += HandleEntityAdded;
    floor.OnEntityRemoved += HandleEntityRemoved;
  }

  void HandleEntityAdded(Entity e) {
    InstantiateGameObjectForEntity(e);
  }

  void HandleEntityRemoved(Entity e) {
    if (e == GameModel.main.player) {
      return;
    }
    GameObject currentObject = gameObjectMap[e];
    if (currentObject == null) {
      Debug.LogWarning("" + e + " was removed from floor " + floor + " but didn't have a GameObject.");
    }
    currentObject.AddComponent<FadeThenDestroy>();
    gameObjectMap.Remove(e);
  }

  void instantiateGameObjectsToMatchFloor() {
    foreach (Tile tile in floor.tiles) {
      InstantiateGameObjectForEntity(tile);
    }
    foreach (Actor actor in floor.Actors()) {
      InstantiateGameObjectForEntity(actor);
    }
    foreach (Grass grass in floor.Grasses()) {
      InstantiateGameObjectForEntity(grass);
    }
  }

  private void InstantiateGameObjectForEntity(Entity entity) {
    if (gameObjectMap.ContainsKey(entity)) {
      throw new System.Exception("Creating gameObject for entity that already has one" + entity);
    }
    /// Player is Instantiated separately; not responsible here
    if (entity is Player) {
      return;
    }
    Vector3Int pos = new Vector3Int(entity.pos.x, entity.pos.y, 0);
    GameObject prefab = GetEntityPrefab(entity);
    if (prefab != null) {
      GameObject gameObject = Instantiate(prefab, pos, Quaternion.identity, this.transform);
      if (entity is Tile tile) {
        gameObject.GetComponent<TileController>().owner = tile;
      } else if (entity is Actor actor) {
        gameObject.GetComponent<ActorController>().actor = actor;
      } else if (entity is Grass grass) {
        gameObject.GetComponent<GrassController>().grass = grass;
      }
      gameObjectMap[entity] = gameObject;
    } else {
      Debug.LogError($"Couldn't find prefab for {entity.GetType()}");
    }
  }
}