using System;

public class CannotPerformActionException : Exception {
  public bool showInUI { get; set; }
  public CannotPerformActionException(string message) : base(message) {
  }
}

public class NoActionException : Exception {}

[Serializable]
public class PlayerSelectCanceledException : Exception {
  public PlayerSelectCanceledException() {
  }

  public PlayerSelectCanceledException(string message) : base(message) {
  }
}