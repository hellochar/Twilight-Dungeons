import { ChaseTargetTask } from './ChaseTargetTask';
import type { Actor } from '../Actor';
import type { Body } from '../Body';

export class ChaseDynamicTargetTask extends ChaseTargetTask {
  private targetDecider: () => Body;

  constructor(actor: Actor, targetDecider: () => Body) {
    super(actor, targetDecider());
    this.targetDecider = targetDecider;
  }

  preStep(): void {
    this.targetBody = this.targetDecider();
    super.preStep();
  }
}
