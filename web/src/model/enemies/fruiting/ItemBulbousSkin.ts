import { EquippableItem, DURABLE_TAG, STICKY_TAG, reduceDurability, type IDurable, type ISticky } from '../../Item';
import { GenericPlayerTask } from '../../tasks/GenericTask';
import { Ground } from '../../Tile';
import { Mushroom } from '../../grasses/Mushroom';
import { EquipmentSlot } from '../../Equipment';
import type { Player } from '../../Player';

/**
 * Armor infection. Germinate: self-damage 1 + spawn Mushrooms on cardinal Ground neighbors.
 * Port of C# ItemBulbousSkin from FruitingBody.cs.
 */
export class ItemBulbousSkin extends EquippableItem implements IDurable, ISticky {
  readonly [DURABLE_TAG] = true as const;
  readonly [STICKY_TAG] = true as const;

  durability: number;

  get slot(): EquipmentSlot {
    return EquipmentSlot.Armor;
  }

  get maxDurability(): number {
    return 1;
  }

  constructor() {
    super();
    this.durability = this.maxDurability;
  }

  germinate(player: Player): void {
    player.setTasks(new GenericPlayerTask(player, () => this.germinateAction()));
    reduceDurability(this);
  }

  private germinateAction(): void {
    this.player.takeDamage(1, this.player);
    for (const tile of this.player.floor!.getCardinalNeighbors(this.player.pos)) {
      if (tile instanceof Ground) {
        this.player.floor!.put(new Mushroom(tile.pos));
      }
    }
  }

  override getAvailableMethods(): string[] {
    const methods = super.getAvailableMethods();
    methods.push('Germinate');
    return methods;
  }

  getStats(): string {
    return "You're infected with Bulbous Skin!\nPress Germinate to take 1 damage and create 4 Mushrooms around you.";
  }
}
