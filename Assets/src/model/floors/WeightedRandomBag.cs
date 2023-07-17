using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// https://gamedev.stackexchange.com/a/162996
[Serializable]
public class WeightedRandomBag<T> : IEnumerable<KeyValuePair<float, T>>, ICloneable {

  [Serializable]
  protected class Entry {
    public float weight;
    public T item;
    public override string ToString() {
      var i = item is Delegate d ? d.Method.Name : item.ToString();
      return $"{i}: {weight}";
    }
  }

  protected List<Entry> entries = new List<Entry>();

  public void Add(float weight, T item) {
    entries.Add(new Entry { item = item, weight = weight });
  }

  public void Remove(T entry) {
    entries.RemoveAll((Entry e) => e.item.Equals(entry));
  }

  public void SetWeight(T item, float weight) {
    var entry = entries.Find(e => e.item.Equals(item));
    entry.weight = weight;
  }

  public float? GetWeight(T item) {
    var entry = entries.Find(e => e.item.Equals(item));
    if (entry == null) {
      return null;
    }
    return entry.weight;
  }

  public T GetRandom() {
    var accumulatedWeight = GetAccumulatedWeight();
    /// TODO audit locations of WeightedRandomBag and make sure the stream isn't being corrupted
    float r = MyRandom.value * accumulatedWeight;
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
  public T GetRandomAndDiscount(float reduceChanceBy = 0.3f) {
    var item = GetRandom();
    if (item != null) {
      Discount(item);
    }
    return item;
  }

  public T GetRandomAndRemove() {
    return GetRandomAndDiscount(1);
  }

  public void Discount(T item, float reduceChanceBy = 0.3f) {
    var getWeight = GetWeight(item);
    if (!getWeight.HasValue) {
      return;
    }
    var currentWeight = getWeight.Value;
    var accumulatedWeight = GetAccumulatedWeight();

    var currentChance = currentWeight / GetAccumulatedWeight();
    var newChance = currentChance * (1f - reduceChanceBy);

    // newChance = newWeight / (newWeight + rest), solve for newWeight
    // newChance * (newWeight + rest) = newWeight
    // newChance * newWeight + newChance * rest = newWeight
    // newChance * rest = newWeight - newChance * newWeight
    // newChance * rest = newWeight * (1 - newChance)
    // newChance * rest / (1 - newChance) = newWeight

    var restWeight = accumulatedWeight - currentWeight;
    var newWeight = newChance * restWeight / (1f - newChance);

    // avoid degenerate case if there's only 1 item in the bag and it's trying to discount itself
    if (newWeight == 0 && entries.Count() == 1) {
      newWeight = currentWeight;
    }
    SetWeight(item, newWeight);
  }

  public T GetRandomWithout(IEnumerable<T> encounters) {
    T pick;
    do {
      pick = GetRandom();
      if (pick == null) {
        return pick;
      }
    } while (encounters.Contains(pick));
    return pick;
  }

  public T GetRandomWithoutAndDiscount(IEnumerable<T> encounters, float reduceChanceBy = 0.3f) {
    var pick = GetRandomWithout(encounters);
    Discount(pick, reduceChanceBy);
    return pick;
  }


  public void Clear() {
    entries.Clear();
  }

  public WeightedRandomBag<T> Clone() {
    var newBag = new WeightedRandomBag<T>();
    foreach (Entry e in entries) {
      newBag.Add(e.weight, e.item);
    }
    return newBag;
  }

  public WeightedRandomBag<T> Merge(WeightedRandomBag<T> other) {
    foreach (Entry entry in other.entries) {
      var thisEntry = entries.Find(e => e.item.Equals(entry.item));
      if (thisEntry != null) {
        thisEntry.weight += entry.weight;
      } else {
        entries.Add(entry);
      }
    }
    return this;
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

  public override string ToString() => $"[WeightedRandomBag {string.Join(", ", entries)}]";
}
