public class PlayerActionAttribute : System.Attribute {
  public PlayerActionAttribute() {
  }

  public PlayerActionAttribute(string name) {
    Name = name;
  }

  public string Name { get; }
}