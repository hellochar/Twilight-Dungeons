using System;
using System.Collections.Generic;
using UnityEngine;

class PrefabCache {
  public static PrefabCacheTyped<ActorTask> Tasks = new PrefabCacheTyped<ActorTask>("Tasks");
  public static PrefabCacheTyped<Status> Statuses = new PrefabCacheTyped<Status>("Statuses");
  public static PrefabCacheTyped<BaseAction> BaseActions = new PrefabCacheTyped<BaseAction>("BaseActions");
  public static PrefabCache Effects = new PrefabCache("Effects");
  public static PrefabCache UI = new PrefabCache("UI");

  private Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();
  public string Path { get; set; }

  public PrefabCache(string path) {
    Path = path;
  }

  public GameObject GetPrefabFor(string name) {
    if (!cache.ContainsKey(name)) {
      // attempt to load it
      var prefabOrNull = Resources.Load<GameObject>($"{Path}/{name}");
      cache.Add(name, prefabOrNull);
    }
    return cache[name];
  }

  public GameObject Instantiate(string name, Transform parent, bool worldPositionStays = true) {
    GameObject prefab = GetPrefabFor(name);
    if (prefab != null) {
      return UnityEngine.Object.Instantiate(prefab, parent, worldPositionStays);
    }
    return null;
  }
}

class PrefabCacheTyped<T> : PrefabCache {
  public PrefabCacheTyped(string path) : base(path) {
  }

  public GameObject GetPrefabFor(T obj) {
    return GetPrefabFor(obj.GetType().Name);
  }

  public GameObject MaybeInstantiateFor(T obj, Transform transform) {
    if (obj != null) {
      return Instantiate(obj.GetType().Name, transform);
    } else {
      return null;
    }
  }
}