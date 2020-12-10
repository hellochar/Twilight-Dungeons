using System;
using System.Collections.Generic;
using UnityEngine;

class PrefabCache {
  public static PrefabCacheTyped<ActorTask> Tasks = new PrefabCacheTyped<ActorTask>("Tasks");
  public static PrefabCacheTyped<Status> Statuses = new PrefabCacheTyped<Status>("Statuses");
  public static PrefabCacheTyped<BaseAction> BaseActions = new PrefabCacheTyped<BaseAction>("BaseActions");
  public static PrefabCache Effects = new PrefabCache("Effects");
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

  public GameObject MaybeInstantiateFor(string name, Transform transform) {
    GameObject prefab = GetPrefabFor(name);
    if (prefab != null) {
      return UnityEngine.Object.Instantiate(prefab, transform.position, Quaternion.identity, transform);
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
      return MaybeInstantiateFor(obj.GetType().Name, transform);
    } else {
      return null;
    }
  }
}