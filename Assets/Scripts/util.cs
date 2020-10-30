using UnityEngine;

public class Util {

  public static Vector2 getXY(Vector3 v3) {
    return new Vector2(v3.x, v3.y);
  }

  public static Vector3 withZ(Vector2 vector2, float z = 0) {
    return new Vector3(vector2.x, vector2.y, z);
  }
  
  // public static Vector3 LerpOrJump(Vector3 a, Vector3 b, float t, float threshold) {
  // }

  public static int manhattanDistance(Vector2Int vector) {
    return Mathf.Abs(vector.x) + Mathf.Abs(vector.y);
  }

  private Util() { }
}