using System;
using System.Collections.Generic;
using System.Linq;

/// Trying to reapply an existing status will call "Stack"
public abstract class Status : IStepModifier {
  public virtual bool isDebuff => false;
  public event Action OnRemoved;
  private string firstRemoveStackTrace;
  private StatusList m_list;
  /// <summary>Should only be set by the StatusList.</summary>
  public StatusList list {
    get => m_list;
    set {
      if (value == null && m_list != null) {
        if (firstRemoveStackTrace != null) {
          throw new Exception("Removing " + this + "twice! First stack trace was " + firstRemoveStackTrace);
        } else {
          firstRemoveStackTrace = Environment.StackTrace;
          OnRemoved?.Invoke();
          End();
        }
      }
      m_list = value;
      if (value != null) {
        Start();
      }
    }
  }
  public Actor actor => list?.actor;

  /// Called when list and actor are setup
  public virtual void Start() {}

  /// Called right before the status is removed
  public virtual void End() {}

  public virtual string displayName => Util.WithSpaces(GetType().Name.Replace("Status", ""));
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
    GameModel.main.EnqueueEvent(() => list?.Remove(this));
  }
}

/// Number stacking statuses will just "add" their numbers when re-applied rather than having the old one
/// removed
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

public class StatusList {
  public Actor actor;
  public List<Status> list;

  public event Action<Status> OnAdded;
  public event Action<Status> OnRemoved;

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
      OnAdded?.Invoke(status);
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
    OnRemoved?.Invoke(status);
  }

  internal T FindOfType<T>() where T : Status {
    return (T) (list.Find((s) => s is T));
  }
}

