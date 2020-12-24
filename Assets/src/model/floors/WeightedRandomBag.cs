using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// https://gamedev.stackexchange.com/a/162996
public class WeightedRandomBag<T> : IEnumerable<KeyValuePair<float, T>>, ICloneable {

  private struct Entry {
    public float weight;
    public T item;
  }

  private List<Entry> entries = new List<Entry>();

  public void Add(float weight, T item) {
    entries.Add(new Entry { item = item, weight = weight });
  }

  public void SetWeight(T item, float weight) {
    var entry = entries.Find(e => e.item.Equals(item));
    entry.weight = weight;
  }

  public float GetWeight(T item) {
    var entry = entries.Find(e => e.item.Equals(item));
    return entry.weight;
  }

  public T GetRandom() {
    var accumulatedWeight = GetAccumulatedWeight();
    float r = UnityEngine.Random.value * accumulatedWeight;
    var accum = 0f;

    foreach (Entry entry in entries) {
      accum += entry.weight;
      if (accum >= r) {
        return entry.item;
      }
    }
    return default(T); //should only happen when there are no entries
  }

  private float GetAccumulatedWeight() => entries.Select(e => e.weight).Sum();

  /// Get an item and then mutate this bag such that the item has v% less weight
  internal T GetRandomAndDiscount(float v) {
    var item = GetRandom();

    var currentWeight = GetWeight(item);
    var accumulatedWeight = GetAccumulatedWeight();

    var currentChance = currentWeight / GetAccumulatedWeight();
    var newChance = currentChance * (1f - v);

    // newChance = newWeight / (newWeight + rest), solve for newWeight
    // newChance * (newWeight + rest) = newWeight
    // newChance * newWeight + newChance * rest = newWeight
    // newChance * rest = newWeight - newChance * newWeight
    // newChance * rest = newWeight * (1 - newChance)
    // newChance * rest / (1 - newChance) = newWeight

    var restWeight = accumulatedWeight - currentWeight;
    var newWeight = newChance * restWeight / (1f - newChance);

    SetWeight(item, newWeight);
    return item;
  }

  public T GetRandomWithout(params T[] encounters) {
    T pick;
    do {
      pick = GetRandom();
    } while (encounters.Contains(pick));
    return pick;
  }

  public WeightedRandomBag<T> Clone() {
    var newBag = new WeightedRandomBag<T>();
    foreach (Entry e in entries) {
      newBag.Add(e.weight, e.item);
    }
    return newBag;
  }

  object ICloneable.Clone() => this.Clone();

  public IEnumerator<KeyValuePair<float, T>> GetEnumerator() {
    foreach (var entry in entries) {
      yield return new KeyValuePair<float, T>(entry.weight, entry.item);
    }
  }

  IEnumerator IEnumerable.GetEnumerator() {
    return this.GetEnumerator();
  }
}
