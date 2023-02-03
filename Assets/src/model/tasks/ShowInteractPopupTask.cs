public class ShowInteractPopupTask : DoOnceTask {
  private readonly Entity target;

  public ShowInteractPopupTask(Actor actor, Entity target) : base(actor) {
    this.target = target;
  }

  protected override BaseAction GetNextActionImpl() {
    return new GenericBaseAction(actor, ShowInteractPopup);
  }

  void ShowInteractPopup() {
    if (actor.IsNextTo(target)) {
      EntityPopup.Show(target);
    } else {
      throw new CannotPerformActionException("Cannot reach!");
    }
  }
}
