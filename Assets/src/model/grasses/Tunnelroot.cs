using System;
using System.Linq;
using UnityEngine;

[System.Serializable]
[ObjectInfo(description: "Walking into the Tunnelroot teleports you to the paired Tunnelroot elsewhere on this level.")]
public class Tunnelroot : Grass, IActorEnterHandler {
  public static bool CanOccupy(Tile tile) => tile is Ground;

  private Tunnelroot partner;
  private bool isOpen = true;
  [field:NonSerialized] /// controller only
  public event Action<bool> OnOpenChanged;

  public Tunnelroot(Vector2Int pos) : base(pos) {
  }

  public bool IsOpen() => isOpen;

  public void PartnerWith(Tunnelroot other) {
    if (other.partner != null) {
      Debug.LogWarning("Partnering an already partnered Tunnelroot!");
    }
    other.partner = this;
    this.partner = other;
  }

  protected override void HandleLeaveFloor() {
    base.HandleLeaveFloor();
    if (partner != null && partner.floor != null) {
      // let my floor = null code finish first and *then* remove partner, otherwise we recurse infinitely
      GameModel.main.EnqueueEvent(() => {
        partner.floor.Remove(partner);
      });
    }
  }

  public void HandleActorEnter(Actor who) {
    if (partner == null) {
      Debug.LogWarning("Unpartnered Tunnelroot!");
    }

    if (!partner.IsDead && partner.body == null && isOpen) {
      // Close();
      // partner.Close();
      var newTile = floor.BreadthFirstSearch(partner.pos, (tile) => true).Skip(1).Where(t => t.CanBeOccupied()).FirstOrDefault();
      if (newTile != null) {
        OnNoteworthyAction();
        partner.OnNoteworthyAction();
        who.pos = newTile.pos;
        who.statuses.Add(new SurprisedStatus());
      }
    }
  }

  private void Close() {
    isOpen = false;
    OnOpenChanged?.Invoke(isOpen);
    AddTimedEvent(5, Reopen);
    OnNoteworthyAction();
  }

  void Reopen() {
    isOpen = true;
    OnOpenChanged?.Invoke(isOpen);
  }
}
