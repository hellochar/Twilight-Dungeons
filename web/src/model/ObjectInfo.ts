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

// ─── Early Game Enemies ───

registerObjectInfo('Blob', { description: 'Telegraphs attacks.', flavorText: 'An odorless mass advances towards you with a steady, brainless determination.' });
registerObjectInfo('MiniBlob', { description: 'Telegraphs attacks.' });
registerObjectInfo('Bird', { description: 'Jumps up to two tiles, then pauses.' });
registerObjectInfo('Snake', { description: 'Only moves or attacks if you\'re in the same row or column. Attacks other creatures. Attacks apply Weakness.' });
registerObjectInfo('Spider', { description: 'Spins Webs. Attacks deal no damage but apply Poison.' });
registerObjectInfo('Snail', { description: 'Slow. Goes into its shell when it takes damage. While in its shell, it takes 1 less attack damage.' });
registerObjectInfo('Wallflower', { description: 'Stays next to walls.' });
registerObjectInfo('Jackal', { description: 'Alternates moving 1 and 2 tiles. Runs away when another Jackal dies.' });
registerObjectInfo('Skully', { description: 'Regenerates after three turns. Attack then step on it to kill it.' });
registerObjectInfo('Octopus', { description: 'Range 2. Runs away if you get too close.' });


registerObjectInfo('Boombug', { description: 'Neutral. Leaves an explosive corpse on death.', flavorText: 'How such a creature was able to survive and breed is Nature\'s mystery.' });
registerObjectInfo('Scuttler', { description: 'Attacks its target, then burrows back into the ground.' });
registerObjectInfo('ScuttlerUnderground', { description: 'A Scuttler lies in wait for something to walk over it. Upon triggering a Scuttler will attack once then burrow again.' });
registerObjectInfo('FruitingBody', { description: 'Walk near it to telegraph a spray. After one turn, it dies, but adjacent creatures turn into Fruiting Bodies. Fruiting Bodies do not count as enemies.', flavorText: 'Did you know? Sporocarp of a basidiomycete is known as a basidiocarp or basidiome, while the fruitbody of an ascomycete is known as an ascocarp.' });
// registerObjectInfo('SporeBloat', { description: 'Pops after three turns, applying the Spored Status on to adjacent creatures.', flavorText: 'Inflated and swollen and looking to spread its seed.' });


registerObjectInfo('Bat', { description: 'Heals itself from attacks. Goes into Deep Sleep for 5 turns after 5 turns awake.', flavorText: '"Eat, sleep, fly, repeat!\nAs far as I\'m concerned, you\'re meat!"\n\t - Northland nursery rhyme' });

registerObjectInfo('Scorpion', { description: 'Attacks and moves twice.' });
registerObjectInfo('Golem', { description: 'Attacks and moves slowly. Leaves a trail of Rubble.', flavorText: 'Eager to prove himself, Aurogan managed to Will Life into the boulder on Boulder Hill. The Council was impressed, then horrified, then flattened.' });
registerObjectInfo('Crab', { description: 'Neutral. Only moves horizontally. Attacks anything in its path.' });
registerObjectInfo('HydraHeart', { description: 'Spawns Hydra Heads. Does not move or attack on its own.', flavorText: 'Thick veins writhe underneath this pulsating white mass, connecting it to an ever growing network of Heads.' });
registerObjectInfo('HydraHead', { description: 'Telegraph attacks anything adjacent to it. Stationary.', flavorText: 'A fleshy tube with a gaping jaw at the end, grasping at any food nearby.' });
registerObjectInfo('Clumpshroom', { description: 'Duplicates in 5-7 turns. You get Clumped Lung when killing it. If you have 10 Clumped Lung, you die.' });
registerObjectInfo('ClumpedLungStatus', { description: 'At 10 stacks, you die.' });
registerObjectInfo('Goo', { description: 'When damaged, it splits into two.' });

registerObjectInfo('Dizapper', { description: 'Applies Vulnerable when it hits you.\nGets stunned when attacked.' });
registerObjectInfo('Hopper', { description: 'Jumps next to you. When hurt, it will eat a nearby Grass to heal itself to full HP.' });
registerObjectInfo('IronJelly', { description: 'Neutral. Invulnerable. Attacking the Iron Jelly pushes it away, attacking any Creature in its way.' });
registerObjectInfo('Grasper', { description: 'Surrounds you with its Tendril. If you are next to three contiguous Tendrils, Grasper attacks for 3 damage.' });
registerObjectInfo('Tendril', { description: ' Killing a Tendril kills descendant Tendrils.' });
registerObjectInfo('Poisoner', { description: 'Every other turn, poisons you if visible.' });
registerObjectInfo('Muckola', { description: 'Every other turn, place a Muck next to the Player, if visible.' });
registerObjectInfo('Thistlebog', { description: 'Moves slowly.\nSummons a ring of Brambles around you that disappear after 10 turns (needs vision).\nInterrupted when taking damage.' });

// ─── Early Game Grasses ───

registerObjectInfo('Web', { description: 'Prevents your next movement.' });
registerObjectInfo('Bladegrass', { description: 'Sharpens when walked over. Then the next creature walking into it takes 1 damage.' });
registerObjectInfo('SoftGrass', { description: 'Moving twice on Soft Grass gives the Player a Free Move.', flavorText: 'Feels nice on your feet.' });
registerObjectInfo('Guardleaf', { description: 'Blocks the next attack on the covered creature.', flavorText: 'Huge leaves, sprouting out from the ground, gently twist themselves around you in a protective cover.' });
registerObjectInfo('EveningBells', { description: 'Enemies fall asleep when walking into Evening Bells. When hit, they take 2x damage and awake.' });
registerObjectInfo('Llaora', { description: 'You may Disperse the Llaora, permanently confusing Enemies in radius 2. Confused enemies walk randomly and don\'t attack.' });
registerObjectInfo('Deathbloom', { description: 'Blooms when an adjacent creature dies. Then, walk over it to become Frenzied.' });
registerObjectInfo('HangingVines', { description: 'Constricts any creature that walks into its hook. You may destroy the Hanging Vines by tapping the Wall it\'s attached to.' });
registerObjectInfo('Violets', { description: 'Alternately opens and closes every 12 turns. While open, Pacifies the creature standing over it.' });
registerObjectInfo('Fern', { description: 'Blocks vision. You can cut it down.' });
registerObjectInfo('Poisonmoss', { description: 'Applies Poison to the creature standing over it every turn. Gradually turns adjacent Grass into Poisonmoss.' });
registerObjectInfo('Muck', { description: 'Regenerates into a Skully after three turns. Step on the Muck to remove it.' });

registerObjectInfo('Spores', { description: 'Releases three Spore Bloats when walked over.', flavorText: 'One man\'s dead brother is a fungi\'s feast.' });

registerObjectInfo('Brambles', { description: 'Take 1 attack damage when walking into Brambles.' });
registerObjectInfo('Tunnelroot', { description: 'Walking into the Tunnelroot teleports you to the paired Tunnelroot elsewhere on this level.\nEnemy creatures are surprised on teleport.' });
registerObjectInfo('Astoria', { description: 'Heals you for 4 HP if you\'re hurt.\nIf you\'re full HP, you can pick up the Astoria and use it later.' });
registerObjectInfo('Bloodwort', { description: 'When you walk over the Bloodwort, destroy it and gain 2 strength.' });
registerObjectInfo('VibrantIvy', { description: 'Camouflages creatures standing on it.' });


// ─── Mid/Late Game Enemies ───



registerObjectInfo('HardShell', { description: 'If the Hardshell would take more than 2 attack damage, it is reduced to 0.' });
registerObjectInfo('Bloodstone', { description: 'You deal +1 attack damage.\nYou take +1 attack damage.\n\nDestroy the Bloodstone to remove.' });
registerObjectInfo('Butterfly', { description: 'Every 5 turns, the Butterfly duplicates the Grass you\'re standing on to the four cardinally adjacent tiles.' });
registerObjectInfo('Wildekin', { description: 'Chases you.\nStays one Tile away from Walls or non-Wildekins, but will attack you if possible.\nRuns away for three turns after it attacks.' });
registerObjectInfo('Parasite', { description: 'Once Parasite deals attack damage, it applies the Parasite Status and dies.\nAttacks anything near it.\nMoves twice, but randomly.', flavorText: 'Blind but fast, these bloodthirsty ticks will latch onto anything they can feel out.' });
registerObjectInfo('ParasiteEgg', { description: 'Hatches into two Parasites after 3 turns.' });
registerObjectInfo('Healer', { description: 'Every turn, heals a random hurt enemy other than itself for 1 HP.' });
registerObjectInfo('Shielder', { description: 'Duplicates then waits.' });
registerObjectInfo('CheshireWeed', { description: 'Harmless.' });
registerObjectInfo('Pumpkin', { description: 'Drops a Pumpkin item on death.' });
registerObjectInfo('Dandyslug', { description: 'Neutral. Spawns Dandypuff on death.' });
registerObjectInfo('Zombie', { description: 'Attacks anything nearby and loses 1 HP per turn not standing on Necroroot.' });
registerObjectInfo('CheshireWeedSprout', { description: 'Any Creature walking over it takes 1 attack damage and clears the Sprout.\nAfter five turns, grows into a Cheshire Weed.' });
registerObjectInfo('Vulnera', { description: 'If the Player is next to it, Vulnera applies Vulnerable to the Player, then dies.' });
registerObjectInfo('Pistrala', { description: 'Every other turn, grow a Cheshire Weed Sprout next to the Player if visible.' });

// ─── Mid/Late Game Grasses ───

registerObjectInfo('Redcap', { description: 'You may pop the Redcap, applying the Vulnerable Status to adjacent enemies for 7 turns.', flavorText: '' });
registerObjectInfo('Mushroom', { description: 'Walk over it to harvest.' });
registerObjectInfo('DeathlyCreeper', { description: 'Spreads to unoccupied adjacent Tiles without Grass.\n\nIf all Ground tiles have Black Creeper, all creatures (including you) Die.' });
registerObjectInfo('Dandypuff', { description: 'When a creature walks over a Dandypuff, they gain Weakness, dealing -1 damage on their next attack.' });
registerObjectInfo('Necroroot', { description: 'Spawn a Zombie of any creature that dies on the Necroroot (this consumes the Necroroot).\nZombies attack anything nearby and lose 1 HP while not standing on Necroroot.' });
registerObjectInfo('Ninetails', { description: 'Occasionally drops a Floof.' });
registerObjectInfo('NinetailsFloof', { description: '.' });
registerObjectInfo('DandypuffTrail', { description: 'Leaves a trail of Dandypuffs.\nWhen a creature walks over a Dandypuff, they gain Weakness, dealing -1 damage on their next attack.' });
registerObjectInfo('Agave', { description: 'Walk over to harvest.' });
registerObjectInfo('BlobSlime', { description: 'Deals 1 damage to any non-Blob that walks into it.\nRemoved when you walk into it, or the Blobmother dies.' });

// ─── Bosses ───

registerObjectInfo('JackalBoss', { description: 'Summons jackals if there are none on the map.' });
registerObjectInfo('Blobmother', { description: 'Spawns a Blob or MiniBlob upon taking damage.\n\nLeaves a trail of Blob Slime.' });
registerObjectInfo('FungalColony', { description: 'Blocks 1 attack damage.\nSpawns a Fungal Sentinel when attacked.\nCan be damaged by Fungal Sentinel explosions.\nEvery 12 turns, summons a Fungal Breeder and moves itself to a random Fungal Wall.\nDoes not move or attack.' });
registerObjectInfo('FungalBreeder', { description: 'Summons a Fungal Sentinel every 7 turns.\nDoes not move or attack.' });
registerObjectInfo('FungalSentinel', { description: 'Explodes at melee range, dealing 2 damage to adjacent Creatures. This can trigger other Sentinels.\n\nKilling the Sentinel will prevent its explosion.\n\nLeaves a Fungal Wall on death.' });

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

// registerObjectInfo('Player', { description: 'Only you can use and equip items.\nOnly you can take stairs.', flavorText: 'Though your illness makes you physically weak, your knowledge of flora and fauna helps you navigate these strange caves.' });

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

// ─── Statuses ───

registerObjectInfo('FrenziedStatus', { description: '+2 damage for the next {stacks} attacks.\nWhen Frenzied ends, gain Weakness, dealing -1 damage on the next attack.' });
registerObjectInfo('CharmedStatus', { description: 'On your team!' });
registerObjectInfo('ConfusedStatus', { description: 'Your next {stacks} turns must be spent moving in a random direction.' });
registerObjectInfo('ConstrictedStatus', { description: 'You must break free of vines before you can move or attack!\n{stacks} stacks left.' });
registerObjectInfo('DandyStatus', { description: 'Weakness, dealing -1 damage on your next attack.' });
registerObjectInfo('FreeMoveStatus', { description: 'Get another turn immediately after you move!' });
registerObjectInfo('InfectedStatus', { description: 'Each turn, take 1 damage and spawn a Thick Mushroom adjacent to you.' });
registerObjectInfo('FungalInfectionStatus', { description: 'Fungal infection from a Fruiting Body.\n{stacks} of 3 infections. Does not wear off.\nAt 4 infections, you die.' });
registerObjectInfo('PacifiedStatus', { description: 'You cannot attack while standing on an open Violet.' });
registerObjectInfo('ParasiteStatus', { description: 'A parasite is inside you! Take 1 attack damage per 10 turns.\nHealing or clearing the floor cures immediately.\nIf you die, a Parasite Egg spawns over your corpse.' });
registerObjectInfo('PoisonedStatus', { description: 'At 3 stacks, take 3 damage and remove 3 stacks.\nLose one stack every 5 turns.' });
registerObjectInfo('SlimedStatus', { description: 'Sticky, gooey, and warm. Eeeew.' });
registerObjectInfo('SporedStatus', { description: 'Deal 0 attack damage!\nMoving removes Spored Status.\nWhen you die, Spores grow at your position.' });
registerObjectInfo('SurprisedStatus', { description: "You're surprised! You must spend the next turn shaking it off." });
registerObjectInfo('ThirdEyeStatus', { description: "You can see creatures' exact HP!" });
registerObjectInfo('VulnerableStatus', { description: 'Take 1 more attack damage.\n{stacks} turns remaining.' });
registerObjectInfo('WeaknessStatus', { description: 'Your next {stacks} attacks deal -1 damage!\nCan be Removed by eating a Deathbloom Flower.' });
registerObjectInfo('WebbedStatus', { description: 'Prevents your next movement.' });
registerObjectInfo('FlyingStatus', { description: 'Can fly over chasms and other ground obstacles.' });
registerObjectInfo('StrengthStatus', { description: 'Your next {stacks} attacks deal +1 damage!' });
registerObjectInfo('ArmoredStatus', { description: 'Block 1 damage from the next {stacks} attacks!' });
registerObjectInfo('BarkmealStatus', { description: '+{stacks} max HP.' });
registerObjectInfo('HeartyVeggieStatus', { description: 'Heal {stacks} more HP over {stacks*25} turns. Next tick in {turnsLeft} turns (paused while at full HP).' });
registerObjectInfo('ShieldLinkStatus', { description: 'The Shielder has linked you! Block 1 damage from all sources.' });
registerObjectInfo('GuardedStatus', { description: 'Guardleaf protects you from the next attack!' });
registerObjectInfo('ZenStatus', { description: 'Your next {stacks} moves on a non-cleared Floor are Free Moves. Removed once you take or deal damage.' });
