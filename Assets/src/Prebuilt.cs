using System;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Prebuilt {
  // this is the opposite of FloorController.EntityPrefabs, plus constructor info, and not lazy loaded
  static Dictionary<string, System.Reflection.ConstructorInfo> AllEntityTypeConstructors = null;
  private static void InitEntityTypeConstructors() {
    AllEntityTypeConstructors = new Dictionary<string, System.Reflection.ConstructorInfo>();
    AddConstructorForName("Player");

    var entityPrefabs = Resources.LoadAll<GameObject>("Entities");
    foreach(var prefab in entityPrefabs) {
      var name = prefab.name;
      AddConstructorForName(name);
    }

    static void AddConstructorForName(string name) {
      // load Type of name
      Assembly asm = typeof(Player).Assembly;
      Type type = asm.GetType(name);
      if (type == null) {
        Debug.LogWarning("Couldn't find Type for " + name);
      } else {
        ConstructorInfo constructorInfo = type.GetConstructor(new Type[1] { typeof(Vector2Int) });

        if (constructorInfo == null) {
          Debug.LogWarning("Couldn't find constructor(Vector2Int) for " + name);
        } else {
          AllEntityTypeConstructors.Add(name, constructorInfo);
        }
      }
    }
  }

  public static Prebuilt ConvertSceneIntoPrebuilt(Scene scene) {
    if (AllEntityTypeConstructors == null) {
      InitEntityTypeConstructors();
    }
    Prebuilt prebuilt = new Prebuilt();
    prebuilt.entitiesWithoutPlayer = new List<Entity>();

    List<GameObject> gameObjects = new List<GameObject>();
    scene.GetRootGameObjects(gameObjects);

    foreach(var gameObject in gameObjects) {
      Entity e = ConvertGameObjectToEntity(gameObject);
      if (e != null) {
        if (e is Player p) {
          prebuilt.player = p;
        } else {
          prebuilt.entitiesWithoutPlayer.Add(e);
        }
        // Debug.Log($"Created {e}");
      }
    }

    int GetSortOrder(Entity e) {
      if (e is Tile) {
        return 0;
      }
      if (e is Grass) {
        return 1;
      }
      if (e is ItemOnGround) {
        return 2;
      }
      if (e is Body) {
        return 3;
      }
      return 4;
    }

    prebuilt.entitiesWithoutPlayer.Sort(delegate (Entity e1, Entity e2) {
      return GetSortOrder(e1).CompareTo(GetSortOrder(e2));
    });
    return prebuilt;
  }

  public static Entity ConvertGameObjectToEntity(GameObject go) {
    Vector2Int pos = GetBestEntityPosition(go);
    var nameWithoutDuplications = go.name.Split(' ')[0];
    var constructorInfo = AllEntityTypeConstructors.GetValueOrDefault(nameWithoutDuplications);
    if (constructorInfo == null) {
      Debug.LogWarning($"No constructorInfo for {go} ({nameWithoutDuplications})");
      return null;
    }

    var entity = (Entity) constructorInfo.Invoke(new object[1] { pos });
    return entity;
  }

  static Vector2Int GetBestEntityPosition(GameObject o) {
    return new Vector2Int(
      Mathf.FloorToInt(o.transform.position.x),
      Mathf.FloorToInt(o.transform.position.y)
    );
  }

  public Player player = null;
  public List<Entity> entitiesWithoutPlayer = null;

  public Prebuilt() {
  }

  public Floor createRepresentativeFloor(int depth = -1) {
    // find bounding box
    Vector2Int max = Vector2Int.zero;
    foreach(var e in entitiesWithoutPlayer) {
      max = Vector2Int.Max(max, e.pos);
    }
    max += Vector2Int.one;

    var floor = new Floor(depth, max.x, max.y);
    floor.root = new Room(floor);
    floor.PutAll(entitiesWithoutPlayer);
    return floor;
  }
}
