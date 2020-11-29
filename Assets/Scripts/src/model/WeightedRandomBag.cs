using System;
using System.Collections;
using System.Collections.Generic;

/// https://gamedev.stackexchange.com/a/162996
public class WeightedRandomBag<T> : IEnumerable<KeyValuePair<float, T>> {

  private struct Entry {
    public float accumulatedWeight;
    public T item;
  }

  private List<Entry> entries = new List<Entry>();
  private float accumulatedWeight;
  private Random rand = new Random();

  public void Add(float weight, T item) {
    accumulatedWeight += weight;
    entries.Add(new Entry { item = item, accumulatedWeight = accumulatedWeight });
  }

  public T GetRandom() {
    float r = (float)rand.NextDouble() * accumulatedWeight;

    foreach (Entry entry in entries) {
      if (entry.accumulatedWeight >= r) {
        return entry.item;
      }
    }
    return default(T); //should only happen when there are no entries
  }

  public IEnumerator<KeyValuePair<float, T>> GetEnumerator() {
    foreach (var entry in entries) {
      yield return new KeyValuePair<float, T>(entry.accumulatedWeight, entry.item);
    }
  }

  IEnumerator IEnumerable.GetEnumerator() {
    return this.GetEnumerator();
  }
}
