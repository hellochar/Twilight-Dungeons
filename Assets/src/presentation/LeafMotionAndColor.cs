using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeafMotionAndColor : MonoBehaviour {
  float initialAngle;
  float tOffset = 0;
  float angleOffset;
  SpriteRenderer spriteRenderer;
  void Start() {
    tOffset = Random.value * 0.05f - 0.025f;
    angleOffset = Random.value * 5f - 2.5f;
    initialAngle = transform.rotation.z;
    spriteRenderer = GetComponent<SpriteRenderer>();
    Update();
  }

  void Update() {
    float time = Time.time / 8f + tOffset;
    float timeInt = Mathf.Floor(time);
    float timeRemainder = time - timeInt;
    float timeRemainderNew = EasingFunctions.EaseInOutQuad(0, 1, timeRemainder);
    float timeNew = timeInt + timeRemainderNew;

    float perlinZ = timeNew;
    float amount = Perlin.Noise(transform.position.x * 0.04f, transform.position.y * 0.04f, perlinZ);
    transform.rotation = Quaternion.Euler(0, 0, amount * 360 + angleOffset);
    var color = Color.HSVToRGB(((amount + 0.5f) % 1 + 1) % 1, 0.98f, 0.78f);
    color.a = 0.8f;
    spriteRenderer.color = color;
  }
}
