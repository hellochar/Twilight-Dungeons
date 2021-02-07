using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Walking into the Tunnelroot teleports you to a paired Tunnelroot elsewhere on this level. 5 turn cooldown.")]
public class Tunnelroot : Grass, IActorEnterHandler, IDeathHandler {
  public static bool CanOccupy(Tile tile) => tile is Ground;

  private Tunnelroot partner;
  private bool isOpen = true;

  public Tunnelroot(Vector2Int pos) : base(pos) {
  }

  public void PartnerWith(Tunnelroot other) {
    if (other.partner != null) {
      Debug.LogWarning("Partnering an already partnered Tunnelroot!");
    }
    other.partner = this;
    this.partner = other;
  }

  public void HandleDeath(Entity source) {
    // kill partner at the same time
    GameModel.main.EnqueueEvent(() => {
      if (!partner.IsDead) {
        partner.Kill(this);
      }
    });
  }

  public void HandleActorEnter(Actor who) {
    if (partner == null) {
      Debug.LogWarning("Unpartnered Tunnelroot!");
    }

    if (!partner.IsDead && partner.body == null && isOpen) {
      Close();
      partner.Close();
      who.pos = partner.pos;
    }
  }

  private void Close() {
    isOpen = false;
    AddTimedEvent(5, Reopen);
    OnNoteworthyAction();
  }

  void Reopen() {
    isOpen = true;
  }
}
