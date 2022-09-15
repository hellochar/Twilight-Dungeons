using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HPChangeTextColor : MonoBehaviour {
  // Start is called before the first frame update
  public TMP_Text text;

  /// set isHealing to true to show green even on a 0
  public void SetHPChange(int hpChange, bool isHealing, bool isEnemy) {
    text = GetComponent<TMP_Text>();
    text.text = "" + hpChange;
    if (isHealing || hpChange > 0) {
      text.color = HealColor;
    } else if (!isHealing && (hpChange == 0 || isEnemy)) {
      text.color = NeutralColor;
    }
    if (isEnemy) {
      text.fontSize *= 0.75f;
    }
  }

  private readonly static Color DamageColor = new Color(0.7921569f, 0.1411765f, 0.03137255f);
  private readonly static Color NeutralColor = new Color(0.8f, 0.8f, 0.8f);
  private readonly static Color HealColor = new Color(0.109082f, 0.9803922f, 0.04313723f);
}
