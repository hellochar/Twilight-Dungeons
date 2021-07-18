using System.Collections.Generic;

public static class DictionaryExtensions {
  public static TValue GetValueOrDefault<TKey, TValue>
      (this IDictionary<TKey, TValue> dictionary, TKey key) {
    TValue ret;
    // Ignore return value
    dictionary.TryGetValue(key, out ret);
    return ret;
  }
}