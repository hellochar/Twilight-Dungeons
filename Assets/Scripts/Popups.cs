using UnityEngine;
using UnityEngine.UI;

public static class Popups {
  private static GameObject prefab;

  public static GameObject Create(Transform parent, string title, string info, string flavor, GameObject sprite) {
    if (prefab == null) {
      prefab = Resources.Load<GameObject>("UI/Popup");
    }
    var popup = UnityEngine.Object.Instantiate(prefab, new Vector3(), Quaternion.identity, parent);

    // take up the whole canvas
    var rectTransform = popup.GetComponent<RectTransform>();
    rectTransform.offsetMax = new Vector2();
    rectTransform.offsetMin = new Vector2();

    var titleText = popup.transform.Find("Frame/Title").GetComponent<TMPro.TMP_Text>();
    titleText.text = title;

    var infoText = popup.transform.Find("Frame/Stats").GetComponent<TMPro.TMP_Text>();
    infoText.text = info;

    var flavorText = popup.transform.Find("Frame/Flavor Text").GetComponent<TMPro.TMP_Text>();
    flavorText.text = flavor;

    // Add sprite
    var spriteContainer = popup.transform.Find("Frame/Sprite Container").gameObject;
    UnityEngine.Object.Instantiate(sprite, spriteContainer.GetComponent<RectTransform>().position, Quaternion.identity, spriteContainer.transform);

    // destroy popup when overlay is clicked
    var overlayButton = popup.transform.Find("Overlay").GetComponent<Button>();
    overlayButton.onClick.AddListener(() => {
      UnityEngine.Object.Destroy(popup);
    });

    return popup;
  }
}