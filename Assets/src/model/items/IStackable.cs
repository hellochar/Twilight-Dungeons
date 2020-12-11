using System;

interface IStackable {
  int stacks { get; set; }
  int stacksMax { get; }
}

static class IStackableExtensions {

  /// remove newStacks from this stack and return a new
  /// stackable with that many stacks
  public static T Split<T>(this T s, int newStacks) where T : IStackable {
    if (newStacks >= s.stacks) {
      throw new ArgumentException($"Cannot split a stack of {s.stacks} into one of {newStacks}!");
    }

    var type = typeof(T);
    var constructor = type.GetConstructor(new Type[] { typeof(int) });
    var newInstance = (T) constructor.Invoke(new object[] { newStacks });

    s.stacks -= newStacks;

    return newInstance;
  }

  /// Merge as many stacks as possible from other into this one.
  /// Return true if the other stack is now empty.
  /// NOTE: it's the responsibility of the caller to then dispose of the IStackable!
  public static bool Merge<T>(this T s, T other) where T : IStackable {
    var spaceLeft = s.stacksMax - s.stacks;
    var stacksToAdd = UnityEngine.Mathf.Clamp(other.stacks, 0, spaceLeft);
    s.stacks += stacksToAdd;
    other.stacks -= stacksToAdd;
    return other.stacks == 0;
  }
}