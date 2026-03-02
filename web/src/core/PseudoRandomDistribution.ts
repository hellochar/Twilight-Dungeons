import { MyRandom } from './MyRandom';

/**
 * Port of C# PseudoRandomDistribution.cs.
 * Uses the Dota 2 PRD algorithm to smooth out random procs.
 * https://gaming.stackexchange.com/a/178681
 */

export function CfromP(p: number): number {
  let Cupper = p;
  let Clower = 0;
  let Cmid = 0;
  let p1: number;
  let p2 = 1;
  while (true) {
    Cmid = (Cupper + Clower) / 2;
    p1 = PfromC(Cmid);
    if (Math.abs(p1 - p2) <= 0) break;
    if (p1 > p) {
      Cupper = Cmid;
    } else {
      Clower = Cmid;
    }
    p2 = p1;
  }
  return Cmid;
}

export function PfromC(C: number): number {
  let pProcOnN = 0;
  let pProcByN = 0;
  let sumNpProcOnN = 0;
  const maxFails = Math.ceil(1 / C);
  for (let N = 1; N <= maxFails; ++N) {
    pProcOnN = Math.min(1, N * C) * (1 - pProcByN);
    pProcByN += pProcOnN;
    sumNpProcOnN += N * pProcOnN;
  }
  return 1 / sumNpProcOnN;
}

export class PseudoRandomDistribution {
  private timesSinceLastProc = 0;
  private C: number;

  constructor(C: number) {
    this.C = C;
  }

  test(): boolean {
    const pNow = (this.timesSinceLastProc + 1) * this.C;
    const result = MyRandom.value < pNow;
    if (result) {
      this.timesSinceLastProc = 0;
      return true;
    } else {
      this.timesSinceLastProc++;
      return false;
    }
  }
}
