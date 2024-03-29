using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SpriteFlyAnimation : MonoBehaviour {
  public AnimationCurve alpha;
  public static GameObject Create(Sprite sprite, Vector3 worldPos, GameObject hudTarget) {
    if (hudTarget != null) {
      var controller = PrefabCache.UI.Instantiate("Sprite Fly", GameObject.Find("Canvas").transform).GetComponent<SpriteFlyAnimation>();
      controller.sprite = sprite;
      controller.startWorldPos = worldPos;
      controller.hudTarget = hudTarget;
      return controller.gameObject;
    } else {
      return null;
    }
  }

  public Sprite sprite;
  public Vector3 startWorldPos;

  public GameObject hudTarget;
  private Image image;

  void Start() {
    var canvasRT = GameObject.Find("Canvas").GetComponent<RectTransform>();

    /// viewport: [0, 0] bottom left - [1, 1] top right
    var viewportPos = Camera.main.WorldToViewportPoint(startWorldPos);

    /// canvasRT.rect.size: [900, 506]
    /// canvasPos - coordinates within the canvas (width always 900)
    var screenSize = new Vector3(Screen.width, Screen.height, 1);
    var screenPos = viewportPos;
    screenPos.Scale(screenSize);

    transform.localScale = new Vector3(1,1,1);
    /// .position needs to be in screen space
    transform.position = screenPos;

    image = GetComponent<Image>();
    image.sprite = sprite;

    gameObject.AddComponent<PulseAnimation>().pulseScale = 1.1f;

    StartCoroutine(Animation(alpha));
  }

  IEnumerator Animation(AnimationCurve alpha) {
    float start = Time.time;
    var t = 0f;
    Vector3 startPos = transform.position;
    do {
      yield return new WaitForEndOfFrame();
      t = (Time.time - start) / 1.25f;
      transform.position = Vector3.Lerp(
        startPos,
        hudTarget.transform.position,
        EasingFunctions.EaseInOutCubic(0, 1, t)
        // EasingFunctions.EaseInExpo(0, 1, t)
      );
      var color = image.color;
      // color.a = Mathf.SmoothStep(1f, 0f, t);
      color.a = alpha.Evaluate(t);
      image.color = color;
    } while (t < 1);
    Destroy(gameObject);
  }
}