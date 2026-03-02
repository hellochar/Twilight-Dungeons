/**
 * ObjectInfo registry — maps entity/item constructor names to descriptions.
 * Port of C# [ObjectInfo] attribute system.
 * Designed as a simple registry that any UI can query.
 */

export interface ObjectInfoEntry {
  description?: string;
  flavorText?: string;
}

const registry = new Map<string, ObjectInfoEntry>();

export function registerObjectInfo(name: string, info: ObjectInfoEntry): void {
  registry.set(name, info);
}

export function getObjectInfo(name: string): ObjectInfoEntry | undefined {
  return registry.get(name);
}

// ─── Enemies ───

registerObjectInfo('Blob', { description: 'Telegraphs attacks for 1 turn.\nChases you.', flavorText: 'An odorless mass advances towards you with a steady, brainless determination.' });
registerObjectInfo('MiniBlob', { description: 'Telegraphs attacks for 1 turn.\nChases you.' });
registerObjectInfo('Bird', { description: 'Jumps two tiles per turn and waits after every jump.' });
registerObjectInfo('Snake', { description: 'Only moves in the same row or column as you.' });
registerObjectInfo('Bat', { description: 'Targets the nearest creature.\nHeals when it deals damage.\nGoes into Deep Sleep after 7 turns awake.', flavorText: '"Eat, sleep, fly, repeat!\nAs far as I\'m concerned, you\'re meat!"\n\t - Northland nursery rhyme' });
registerObjectInfo('Goo', { description: 'When attacked, it duplicates into two with half HP.' });
registerObjectInfo('Spider', { description: 'Places Webs and Poisons on attack.' });
registerObjectInfo('Scorpion', { description: 'Chases you.\nAttacks and moves twice.' });
registerObjectInfo('Crab', { description: 'Only moves horizontally.\nAttacks anything in its path.' });
registerObjectInfo('Snail', { description: 'Pauses after every action.\nRetreats into shell when damaged.' });
registerObjectInfo('Golem', { description: 'Attacks and moves slowly.\nBlocks 1 attack damage.\nLeaves a trail of Rubble.', flavorText: 'Eager to prove himself, Aurogan managed to Will Life into the boulder on Boulder Hill. The Council was impressed, then horrified, then flattened.' });
registerObjectInfo('HardShell', { description: 'If the Hardshell would take more than 2 attack damage, it is reduced to 0.' });
registerObjectInfo('Wallflower', { description: 'Tethered to a wall.\nConfuses on attack.' });
registerObjectInfo('Dizapper', { description: 'Applies Vulnerable when it hits you.\nGets stunned when attacked.' });
registerObjectInfo('Bloodstone', { description: 'You deal +1 attack damage.\nYou take +1 attack damage.\n\nDestroy the Bloodstone to remove.' });
registerObjectInfo('Butterfly', { description: 'Every 5 turns, the Butterfly duplicates the Grass you\'re standing on to the four cardinally adjacent tiles.' });
registerObjectInfo('Boombug', { description: 'Does not attack.\nLeaves an explosive corpse on death.', flavorText: 'How such a creature was able to survive and breed is Nature\'s mystery.' });
registerObjectInfo('FruitingBody', { description: 'Infects one of your equipment slots if you\'re hit by its spray.\nIf you have all 5 infections, instead heal 1 HP.', flavorText: 'Did you know? Sporocarp of a basidiomycete is known as a basidiocarp or basidiome, while the fruitbody of an ascomycete is known as an ascocarp.' });
registerObjectInfo('Hopper', { description: 'Jumps next to you.\nWhen hurt, it will eat a nearby Grass to heal itself to full HP.' });
registerObjectInfo('Clumpshroom', { description: 'After 5-7 turns, turns into two or three Clumpshrooms in adjacent tiles.\nDoes not attack or move.\nOn death, it applies Clumped Lung to the killer.\nIf you have 20 Clumped Lung, you die.' });
registerObjectInfo('Wildekin', { description: 'Charges at you.\nStuns on wall collision.' });
registerObjectInfo('Scuttler', { description: 'Burrows underground and emerges near you.' });
registerObjectInfo('Jackal', { description: 'Alternates moving 1 and 2 tiles.\nRuns away when another Jackal dies.\nChases you.' });
registerObjectInfo('JackalBoss', { description: 'Summons jackals if there are none on the map.' });
registerObjectInfo('Parasite', { description: 'Once Parasite deals attack damage, it applies the Parasite Status and dies.\nAttacks anything near it.\nMoves twice, but randomly.', flavorText: 'Blind but fast, these bloodthirsty ticks will latch onto anything they can feel out.' });
registerObjectInfo('ParasiteEgg', { description: 'Hatches into two Parasites after 3 turns.' });
registerObjectInfo('Octopus', { description: 'Attacks at range 2.\nRuns away if you get too close.' });
registerObjectInfo('IronJelly', { description: 'Invulnerable.\nAttacking the Iron Jelly pushes it away, first attacking any Creature in its way.' });
registerObjectInfo('HydraHeart', { description: 'Every four turns, spawns a Hydra Head (max 6) within range 3.\nOn death, all Hydra Heads die as well.\nDoes not move or attack.', flavorText: 'Thick veins writhe underneath this pulsating white mass, connecting it to an ever growing network of Heads.' });
registerObjectInfo('HydraHead', { description: 'Attacks anything adjacent to it.\nStationary.', flavorText: 'A fleshy tube with a gaping jaw at the end, grasping at any food nearby.' });
registerObjectInfo('Grasper', { description: 'Shoots out a long, snaking Tendril that surrounds you.\nIf you are next to three or more Tendrils, Grasper deals 3 attack damage.' });
registerObjectInfo('Tendril', { description: 'If you next to 3 or more Tendrils, the Grasper deals 3 attack damage a turn.\nKilling a Tendril kills descendant Tendrils.' });
registerObjectInfo('Healer', { description: 'Every turn, heals a random hurt enemy other than itself for 1 HP.' });
registerObjectInfo('Poisoner', { description: 'Every other turn, applies Poisoned to the Player if visible.' });
registerObjectInfo('Muckola', { description: 'Every other turn, place a Muck next to the Player, if visible.' });
registerObjectInfo('Thistlebog', { description: 'Chases you.' });
registerObjectInfo('Skully', { description: 'Chases you.\nSpreads Muck on death.' });
registerObjectInfo('Shielder', { description: 'Duplicates then waits.' });
registerObjectInfo('CheshireWeed', { description: 'Harmless.' });
registerObjectInfo('Pumpkin', { description: 'Drops a Pumpkin item on death.' });
registerObjectInfo('Dandyslug', { description: 'Neutral. Spawns Dandypuff on death.' });
registerObjectInfo('Zombie', { description: 'Attacks anything nearby and loses 1 HP per turn not standing on Necroroot.' });
registerObjectInfo('SporeBloat', { description: 'Pops after three turns, applying the Spored Status on to adjacent creatures.', flavorText: 'Inflated and swollen and looking to spread its seed.' });

// ─── Bosses ───

registerObjectInfo('Blobmother', { description: 'Spawns a Blob or MiniBlob upon taking damage.\n\nLeaves a trail of Blob Slime.' });
registerObjectInfo('FungalColony', { description: 'Blocks 1 attack damage.\nSpawns a Fungal Sentinel when attacked.\nCan be damaged by Fungal Sentinel explosions.\nEvery 12 turns, summons a Fungal Breeder and moves itself to a random Fungal Wall.\nDoes not move or attack.' });
registerObjectInfo('FungalBreeder', { description: 'Summons a Fungal Sentinel every 7 turns.\nDoes not move or attack.' });
registerObjectInfo('FungalSentinel', { description: 'Explodes at melee range, dealing 2 damage to adjacent Creatures. This can trigger other Sentinels.\n\nKilling the Sentinel will prevent its explosion.\n\nLeaves a Fungal Wall on death.' });

// ─── Grasses ───

registerObjectInfo('Web', { description: 'Walking into a Web applies the Webbed status.' });
registerObjectInfo('Bladegrass', { description: 'Sharpens when you walk over it.\nDeals 2 damage when sharp.' });
registerObjectInfo('SoftGrass', { description: 'Moving twice on Soft Grass gives the Player one Free Move.', flavorText: 'Feels nice on your feet.' });
registerObjectInfo('Guardleaf', { description: 'Blocks up to 5 attack damage dealt to the creature standing on the Guardleaf.', flavorText: 'Huge leaves, sprouting out from the ground, gently twist themselves around you in a protective cover.' });
registerObjectInfo('Brambles', { description: 'Take 1 attack damage when walking into Brambles.' });
registerObjectInfo('Mushroom', { description: 'Walk over it to harvest.' });
registerObjectInfo('Astoria', { description: 'Walk over to heal 4 HP or pick up.' });
registerObjectInfo('EveningBells', { description: 'Puts non-player creatures to sleep.' });
registerObjectInfo('Bloodwort', { description: 'Walk over to gain Strength.' });
registerObjectInfo('Redcap', { description: 'You may pop the Redcap, applying the Vulnerable Status to adjacent enemies for 7 turns.', flavorText: '' });
registerObjectInfo('Violets', { description: 'Alternately opens and closes every 12 turns.\nWhile open, Pacifies the creature standing over it.' });
registerObjectInfo('Llaora', { description: 'You may Disperse the Llaora, confusing Enemies in radius 2 for 10 turns.' });
registerObjectInfo('Poisonmoss', { description: 'Applies Poison to the creature standing over it every turn.\nGradually turns adjacent Grass into Poisonmoss.' });
registerObjectInfo('Fern', { description: 'Walk over to disperse. Blocks vision.' });
registerObjectInfo('DeathlyCreeper', { description: 'Spreads to unoccupied adjacent Tiles without Grass.\n\nIf all Ground tiles have Black Creeper, all creatures (including you) Die.' });
registerObjectInfo('Tunnelroot', { description: 'Walking into the Tunnelroot teleports you to the paired Tunnelroot elsewhere on this level.\nEnemy creatures are surprised on teleport.' });
registerObjectInfo('Dandypuff', { description: 'Walk over to gain Dandy status.' });
registerObjectInfo('Deathbloom', { description: 'Blooms when a creature dies adjacent to it.' });
registerObjectInfo('HangingVines', { description: 'Constricts any creature that walks into its hook.\nYou may destroy the Hanging Vines by tapping the Wall it\'s attached to.' });
registerObjectInfo('Necroroot', { description: 'Spawn a Zombie of any creature that dies on the Necroroot (this consumes the Necroroot).' });
registerObjectInfo('Spores', { description: 'Releases three Spore Bloats when any creature steps over it.', flavorText: 'One man\'s dead brother is a fungi\'s feast.' });
registerObjectInfo('Ninetails', { description: 'Occasionally drops a Floof.' });
registerObjectInfo('Agave', { description: 'Walk over to harvest.' });
registerObjectInfo('BlobSlime', { description: 'Deals 1 damage to any non-Blob that walks into it.\nRemoved when you walk into it, or the Blobmother dies.' });
registerObjectInfo('Muck', { description: 'Slimes creatures that walk over it.' });
registerObjectInfo('VibrantIvy', { description: 'Camouflages creatures standing on it.' });

// ─── Tiles ───

registerObjectInfo('Ground', { description: 'Creatures can walk here. Grass can grow here.', flavorText: 'Earth - the first element.' });
registerObjectInfo('Wall', { description: 'Blocks vision and movement.', flavorText: 'Hard earth that has weathered centuries of erosion; it\'s not going anywhere.' });
registerObjectInfo('Chasm', { description: 'Blocks walking. Flying creatures can pass.', flavorText: 'You look down and cannot see the bottom. Be careful not to fall!' });
registerObjectInfo('HardGround', { description: 'Grass cannot grow on Hard Ground.', flavorText: 'Any workable earth has been blown or washed away.' });
registerObjectInfo('Soil', { description: 'Plant seeds here.', flavorText: 'Fresh, moist, and perfect for growing. Hard to come by in the caves.' });
registerObjectInfo('FungalWall', { description: 'Blocks vision and movement.\nWalk into to remove.' });

// ─── Destructibles ───

registerObjectInfo('Rubble', { description: 'Destructible. Blocks vision.' });
registerObjectInfo('Stump', { description: 'Destructible.' });
registerObjectInfo('Stalk', { description: 'Blocks vision. Destroying one destroys all Stalks.' });

// ─── Player ───

registerObjectInfo('Player', { description: 'Only you can use and equip items.\nOnly you can take stairs.', flavorText: 'Though your illness makes you physically weak, your knowledge of flora and fauna helps you navigate these strange caves.' });

// ─── FruitingBody infection items ───

registerObjectInfo('ItemTanglefoot', { description: 'Infection. Sticky.\nOccasionally applies Constricted and spawns a Guardleaf when you move.' });
registerObjectInfo('ItemStiffarm', { description: 'Infection. Sticky.\nDeals 2-3 damage.\nYou take +1 attack damage.' });
registerObjectInfo('ItemBulbousSkin', { description: 'Infection. Sticky.\nGerminate to deal 1 damage to yourself and spawn 4 Mushrooms around you.' });
registerObjectInfo('ItemThirdEye', { description: 'Infection. Sticky.\nYou take +1 attack damage.\nLoses durability every action.' });
registerObjectInfo('ItemScalySkin', { description: 'Infection. Sticky.\nBlocks 1 attack damage (uses durability).' });

// ─── Plant items ───

registerObjectInfo('ItemLeecher', { description: 'Use to summon a 1 HP allied Leecher adjacent.' });
registerObjectInfo('ItemBacillomyte', { description: 'Use to place a Bacillomyte grass. Enemies on Bacillomyte deal +1 attack damage.' });
registerObjectInfo('ItemBroodleaf', { description: 'Weapon. Deals 2-3 damage.\nApplies Vulnerable on attack.\nHalf attack action cost.' });
registerObjectInfo('ItemFlowerBuds', { description: 'Eat to heal 1 HP and gain 2 Strength.' });
registerObjectInfo('ItemCatkin', { description: 'Headwear. Grants Recovering status when you take damage.' });
registerObjectInfo('ItemHardenedSap', { description: 'Armor. +4 max HP.\nGrants Armored when you heal.' });
registerObjectInfo('ItemCrescentVengeance', { description: 'Weapon. Deals 3-5 damage.\nUses Armored stacks instead of durability.' });
registerObjectInfo('ItemFaegrass', { description: 'Use to place a Faegrass. Creatures damaged on Faegrass teleport to another Faegrass.' });
registerObjectInfo('ItemThickBranch', { description: 'Weapon. Deals 3 damage.' });
registerObjectInfo('ItemPlatedArmor', { description: 'Armor. Blocks increasing damage (1→4) as durability decreases.' });
registerObjectInfo('ItemBarkmeal', { description: 'Eat to heal 2 HP and gain 4 BarkmealStatus.' });
registerObjectInfo('ItemStompinBoots', { description: 'Footwear. Kills grass you step on and grants Armored.' });
registerObjectInfo('ItemGerm', { description: 'Use to spawn ThickMushroom allies at adjacent tiles.' });
registerObjectInfo('ItemMushroomCap', { description: 'Headwear. Blocks Spored status and heals 1 HP instead.' });
registerObjectInfo('ItemKingshroomPowder', { description: 'Target an adjacent creature to infect it. Infected creatures spawn ThickMushrooms.' });
registerObjectInfo('ItemLivingArmor', { description: 'Armor. Sticky.\nBlocks 2 attack damage.\nMoving costs durability.' });
registerObjectInfo('ItemThicket', { description: 'Armor. Constricts attackers for 6 turns.' });
registerObjectInfo('ItemPrickler', { description: 'Weapon. Deals 1-2 damage.\nSpawns PricklyGrowth on attacked tile.' });
registerObjectInfo('ItemStoutShield', { description: 'Offhand. 50% chance to block all attack damage.' });
registerObjectInfo('ItemHeartyVeggie', { description: 'Eat to gain HeartyVeggie status.' });
registerObjectInfo('ItemCrownOfThorns', { description: 'Headwear. Reflects 2 damage to attackers.' });
registerObjectInfo('ItemThornShield', { description: 'Offhand. Reduces damage taken by 1 and increases damage dealt by 1.\nBoth use durability.' });
registerObjectInfo('ItemBlademail', { description: 'Armor. Blocks 2 attack damage.\nWhen damaged, spawns Bladegrass on adjacent tiles.' });
registerObjectInfo('ItemVilePotion', { description: 'Use to spawn VileGrowth lines to all visible enemies.\nVileGrowth deals 1 damage per turn.' });
registerObjectInfo('ItemBackstepShoes', { description: 'Footwear. Grants 3 Free Moves after attacking.' });
registerObjectInfo('ItemWitchsShiv', { description: 'Weapon. Deals 2 damage.\nScares hit enemies for 10 turns.' });
registerObjectInfo('ItemWildwoodRod', { description: 'Weapon. Deals 3-5 damage.\nAutomatically attacks an adjacent enemy when you move.' });

// ─── Co-located entities from plant items ───

registerObjectInfo('Leecher', { description: 'Allied. 1 HP. Attacks nearby enemies.' });
registerObjectInfo('Bacillomyte', { description: 'Enemies standing on Bacillomyte deal +1 attack damage.' });
registerObjectInfo('Faegrass', { description: 'Creatures damaged on Faegrass teleport to a random other Faegrass.' });
registerObjectInfo('ThickMushroom', { description: 'Allied. 1 HP. Stationary. Does not attack.' });
registerObjectInfo('PricklyGrowth', { description: 'Deals 3 attack damage next turn to the creature standing on it.' });
registerObjectInfo('VileGrowth', { description: 'Deals 1 damage per turn to the creature standing on it.\nDisappears after 9 turns.' });
