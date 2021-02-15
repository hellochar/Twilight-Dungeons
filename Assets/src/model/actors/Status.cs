using System;
using System.Collections.Generic;
using System.Linq;

/// Trying to reapply an existing status will call "Stack"
[Serializable]
public abstract class Status : IStepModifier {
  [field:NonSerialized] /// Controller only
  public event Action OnRemoved;
  private StatusList m_list;
  /// <summary>Should only be set by the StatusList.</summary>
  public StatusList list {
    get => m_list;
    set {
      if (value == null && m_list != null) {
        OnRemoved?.Invoke();
        End();
      }
      m_list = value;
      if (value != null) {
        Start();
      }
    }
  }

  public virtual bool isDebuff => false;
  public Actor actor => list?.actor;
  public virtual string displayName => Util.WithSpaces(GetType().Name.Replace("Status", ""));

  /// Called when list and actor are setup
  public virtual void Start() {}

  /// Called right before the status is removed. NOT called if the actor dies
  /// with the status on it!
  public virtual void End() {}

  public abstract string Info();

  /// The parameter will be of the same type as this type.
  /// Return true if this Status has consumed the other.
  public abstract bool Consume(Status other);

  public virtual void Step() {}

  object IModifier<object>.Modify(object input) {
    Step();
    return input;
  }

  /// <summary>Schedule this status for removal.</summary>
  public void Remove() {
    /// Must be scheduled because Remove() may be called within
    /// a Modifiers.Of() handler; modifying the list would
    /// cause a concurrent modification
    GameModel.main.EnqueueEvent(() => list?.Remove(this));
  }
}

/// Number stacking statuses will just "add" their numbers when re-applied rather than having the old one
/// removed
[Serializable]
public abstract class StackingStatus : Status {
  public virtual StackingMode stackingMode => StackingMode.Add;
  private int m_stacks;
  public virtual int stacks {
    get => m_stacks;
    set {
      m_stacks = value;
      if (value <= 0) {
        Remove();
      }
    }
  }

  public StackingStatus(int stacks) {
    this.stacks = stacks;
  }

  public StackingStatus() : this(1) {}

  public override bool Consume(Status otherParam) {
    var other = (StackingStatus) otherParam;
    switch (stackingMode) {
      case StackingMode.Add:
        this.stacks += other.stacks;
        return true;
      case StackingMode.Max:
        this.stacks = Math.Max(stacks, other.stacks);
        return true;
      case StackingMode.Ignore:
        return true;
      case StackingMode.Independent:
      default:
        return false;
    }
  }
}

public enum StackingMode { Add, Max, Ignore, Independent }

[Serializable]
public class StatusList {
  public Actor actor;
  public List<Status> list;

  public StatusList(Actor actor, List<Status> statuses) {
    this.actor = actor;
    this.list = statuses;
  }

  public StatusList(Actor actor) : this(actor, new List<Status>()) { }

  public void Add<T>(T status) where T : Status {
    var consumed = FindOfType<T>()?.Consume(status) ?? false;
    
    if (!consumed) {
      this.list.Add(status);
      status.list = this;
      OnStatusAdded(status);
    }
  }

  internal bool Has<T>() where T : Status {
    return this.list.Where((Status) => Status is T).Any();
  }

  public void RemoveOfType<T>() where T : Status {
    var toRemove = this.list.Where((status) => status is T).ToList();
    foreach (var status in toRemove) {
      Remove(status);
    }
  }

  public void Remove(Status status) {
    this.list.Remove(status);
    status.list = null;
    OnStatusRemoved(status);
  }

  internal T FindOfType<T>() where T : Status {
    return (T) (list.Find((s) => s is T));
  }

  private void OnStatusAdded(Status status) {
    foreach (var handler in actor.Of<IStatusAddedHandler>()) {
      handler.HandleStatusAdded(status);
    }
  }

  private void OnStatusRemoved(Status status) {
    foreach (var handler in actor.Of<IStatusRemovedHandler>()) {
      handler.HandleStatusRemoved(status);
    }
  }
}

