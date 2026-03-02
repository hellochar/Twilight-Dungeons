import { Status } from '../Status';

/**
 * Info-only status — lets the player see enemy HP.
 * Port of C# ThirdEyeStatus from FruitingBody.cs.
 */
export class ThirdEyeStatus extends Status {
  Consume(_other: Status): boolean {
    return true;
  }
}
