import { Status } from '../Status';

/**
 * Marker status for charmed (allied) enemies.
 * Port of C# CharmedStatus from ItemCharmBerry.cs.
 */
export class CharmedStatus extends Status {
  Consume(_other: Status): boolean {
    return true;
  }
}
