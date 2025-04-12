using System.Security;
using TeamHeroCoderLibrary;

namespace PlayerCoder
{
    class Program
    {
        static void Main(string[] args)
        { 
            Console.WriteLine("Connecting...");
            GameClientConnectionManager connectionManager;
            connectionManager = new GameClientConnectionManager();
            connectionManager.SetExchangePath(MyAI.FolderExchangePath);
            connectionManager.onHeroHasInitiative = MyAI.ProcessAI;
            connectionManager.StartListeningToGameClientForHeroPlayRequests();
        }
    }

    public static class MyAI
    {
        public static string FolderExchangePath = "C:/Users/justi/AppData/LocalLow/Wind Jester Games/Team Hero Coder";
        const int RESURRECTION_COST = 25;
        const int QUICK_HIT_COST = 15;
        const int CURE_SERIOUS_COST = 20;
        const int QUICK_DISPEL_COST = 10;
        const int POISON_NOVA_COST = 15;
        const int METEOR_COST = 60;
        const int FIREBALL_COST = 25;
        const int DOOM_COST = 15;
        const int FLAME_STRIKE_COST = 30;



        static public void ProcessAI()
        {
            bool hasPerformedAction = false;
            Evaluation.hasPerformedAction = false;

            Hero activeHero = null;

            #region Fighter Logic

            if (TeamHeroCoder.BattleState.heroWithInitiative.jobClass == HeroJobClass.Fighter)
            {
                Console.WriteLine("this is a fighter");
                Console.WriteLine("The hero in slot 1 is " + TeamHeroCoder.BattleState.allyHeroes[0].jobClass);

                activeHero = TeamHeroCoder.BattleState.heroWithInitiative;
                //The character with initiative is a figher, do something here...
                //What is the goal of a Fighter? IE What should a Fighter be doing?

                //HIGHEST PRIORITY CHECK GOES HERE
                //Ressurecting the Cleric if they are dead
                //Sucks to be a wizard; he's the cleric's job
                Console.WriteLine("CHECK: Ressurecting the Cleric if they are dead");
                foreach (Hero ally in TeamHeroCoder.BattleState.allyHeroes)
                {
                    if (ally.health <= 0)
                    {
                        Console.WriteLine("We found a dead ally");
                        if (ally.jobClass == HeroJobClass.Cleric)
                        {
                            Console.WriteLine("We found a dead cleric.");
                            if (activeHero.mana >= RESURRECTION_COST)
                            {
                                Console.WriteLine("We have the Mana for Ressurection. Casting it.");
                                hasPerformedAction = Evaluation.AttemptToPerformAction(hasPerformedAction, Ability.Resurrection, ally);
                                //TeamHeroCoder.PerformHeroAbility(Ability.Resurrection, ally);
                                //return;
                            }
                        }
                    }
                }

                //Check to see if any ally needs an ether (35%/40% Mana remaining.)
                //Things we need to know before we can perform the "Use Ether" action
                //1: Do we have an ether availabe?
                Console.WriteLine("CHECK: See if any ally needs an ether (35%/40% Mana remaining.)");
                bool hasEther = false;

                foreach (InventoryItem item in TeamHeroCoder.BattleState.allyInventory)
                {
                    if (item.item == Item.Ether)
                    {
                        Console.WriteLine("We still have Ethers");
                        hasEther = true;
                    }
                }

                //2: Which team member needs the ether?
                foreach (Hero ally in TeamHeroCoder.BattleState.allyHeroes)
                {
                    if ((float)ally.mana / (float)ally.maxMana <= 0.35 && hasEther)
                    {
                        Console.WriteLine("An ally is below 35% Mana. Using an Ether");
                        hasPerformedAction = Evaluation.AttemptToPerformAction(hasPerformedAction, Ability.Resurrection, ally);
                        //TeamHeroCoder.PerformHeroAbility(Ability.Resurrection, ally);
                        //return;
                    }
                }

                //Buff self with Brave if we (Fighter) don't have the Status
                //  Check to see if we have enough mana to still cast resurrection before casting brave
                //  Check to see what the Cleric's HP is before we commit to casting "buff" spells.
                Console.WriteLine("CHECK: Buff self with Brave if we (Fighter) don't have the Status");
                bool hasBrave = false;
                bool shouldCastBrave = false;

                foreach (StatusEffectAndDuration se in activeHero.statusEffectsAndDurations)
                {
                    if (se.statusEffect == StatusEffect.Brave)
                    {
                        Console.WriteLine("Fighter already has brave");
                        hasBrave = true;
                        break;
                    }

                    hasBrave = false;
                }

                if (activeHero.mana >= RESURRECTION_COST)
                {
                    foreach (Hero ally in TeamHeroCoder.BattleState.allyHeroes)
                    {
                        if (ally.jobClass == HeroJobClass.Cleric)
                        {
                            if ((float)ally.health / (float)ally.maxHealth >= 0.3)
                            {
                                Console.WriteLine("We are saving enough MP for emergency Res and Cleric is healthy. Brave is okay");
                                shouldCastBrave = true;
                            }
                        }
                    }
                }
                

                if (!hasBrave)
                {
                    Console.WriteLine("Fighter doesn't have brave");
                    if (shouldCastBrave)
                    {
                        Console.WriteLine("We have determined that casting brave is a good idea.");
                        hasPerformedAction = Evaluation.AttemptToPerformAction(hasPerformedAction, Ability.Brave, activeHero);
                        //TeamHeroCoder.PerformHeroAbility(Ability.Brave, activeHero);
                        //return;
                    }
                }

                //Check to see if any ally needs negative status effect removal.
                Console.Write("CHECK: See if any ally needs negative status effect removal.");
                bool haspoisonremedy = false;
                //MISSING:
                //Silence Rem Check
                //Petrify Rem Check
                //Full Rem Check

                foreach (InventoryItem item in TeamHeroCoder.BattleState.allyInventory)
                {
                    if (item.item == Item.PoisonRemedy)
                    {
                        Console.WriteLine("we still have poison remedies");
                        haspoisonremedy = true;
                    }
                }

                foreach (Hero ally in TeamHeroCoder.BattleState.allyHeroes)
                {
                    foreach (StatusEffectAndDuration se in ally.statusEffectsAndDurations)
                    {
                        if (se.statusEffect == StatusEffect.Poison && haspoisonremedy)
                        {
                            Console.WriteLine("Using Poison Rem");
                            hasPerformedAction = Evaluation.AttemptToPerformAction(hasPerformedAction, Ability.PoisonRemedy, ally);
                            //TeamHeroCoder.PerformHeroAbility(Ability.PoisonRemedy, ally);
                            //return;
                        }
                    }
                }

                //Check if a party member is low HP AND if the cleric's turn is "far away". If so, Cure Serious
                //  Prioritize healing cleric over wizard or self
                Console.WriteLine("CHECK: Cure Serious if a party member is low HP and cleric's turn is far away.");

                if (activeHero.mana >= CURE_SERIOUS_COST)
                {
                    Hero cleric = null;
                    List<Hero> lowHP = new List<Hero>();
                    foreach (Hero hero in TeamHeroCoder.BattleState.allyHeroes)
                    {
                        if ((float)hero.health / (float)hero.maxHealth <= 0.40)
                        {
                            lowHP.Add(hero);
                        }
                        if (hero.jobClass == HeroJobClass.Cleric)
                        {
                            cleric = hero;
                        }
                    }
                    foreach (Hero hero in TeamHeroCoder.BattleState.allyHeroes)
                    {
                        if (cleric.initiativePercent <= 50 && lowHP.Contains(cleric))
                        {
                            Console.WriteLine("Cleric's initiavePercent <= 50 and there is an ally below 40% Health. Casting Cure Serious");

                            hasPerformedAction = Evaluation.AttemptToPerformAction(hasPerformedAction, Ability.CureSerious, cleric);
                            //TeamHeroCoder.PerformHeroAbility(Ability.CureSerious, cleric);
                            //return;
                        }
                        else if (lowHP.Contains(hero))
                        {
                            hasPerformedAction = Evaluation.AttemptToPerformAction(hasPerformedAction, Ability.CureSerious, hero);
                            //TeamHeroCoder.PerformHeroAbility(Ability.CureSerious, hero);
                            //return;
                        }
                    }
                }
                

                //Look into checking to see if we can "one-shot" a damaged enemy.


                //Use quick hit if there are enemy alchemists/Rogues
                Console.WriteLine("CHECK: Use quick hit if there are enemy alchemists/Rogues");

                Hero quickHitTarget = null;
                foreach (Hero hero in TeamHeroCoder.BattleState.foeHeroes)
                {
                    if (hero.jobClass == HeroJobClass.Alchemist || hero.jobClass == HeroJobClass.Rogue)
                    {
                        if (quickHitTarget == null)
                            quickHitTarget = hero;

                        Console.WriteLine("We found a " + hero.jobClass + "in opposing team. Using Quick Hit");
                        hasPerformedAction = Evaluation.AttemptToPerformAction(hasPerformedAction, Ability.QuickHit, quickHitTarget);
                        //TeamHeroCoder.PerformHeroAbility(Ability.QuickHit, quickHitTarget);
                        //return;
                    }
                }

                if (activeHero.mana >= RESURRECTION_COST + QUICK_HIT_COST)
                {
                    Console.WriteLine("Fighter's total MP is greater than the cost of Res + Quick hit");
                    foreach (Hero h in TeamHeroCoder.BattleState.foeHeroes)
                    {
                        if (h.jobClass == HeroJobClass.Alchemist || h.jobClass == HeroJobClass.Rogue)
                        {
                            Console.WriteLine("We found a " + h.jobClass + "in opposing team. Using Quick Hit");
                            hasPerformedAction = Evaluation.AttemptToPerformAction(hasPerformedAction, Ability.QuickHit, h);
                            //TeamHeroCoder.PerformHeroAbility(Ability.QuickHit, h);
                            //return;
                        }
                    }
                }

                //  Check to see if we have enough mana to still cast resurrection before casting brave

                //Target Enemy with Lowest HP
                Console.WriteLine("CHECK: Attacking foe with lowest HP");
                Hero target = null;

                foreach (Hero hero in TeamHeroCoder.BattleState.foeHeroes)
                {
                    if (hero.health > 0)
                    {
                        if (target == null)
                            target = hero;
                        else if (hero.health < target.health)
                            target = hero;
                    }
                }

                //This is the line of code that tells Team Hero Coder that we want to perform the attack action and target the foe with the lowest HP
                hasPerformedAction =  Evaluation.AttemptToPerformAction(hasPerformedAction, Ability.Attack, target);
                //TeamHeroCoder.PerformHeroAbility(Ability.Attack, target);
                //return;
                //LOWEST PRIORITY CHECK GOES HERE

            }

            #endregion

            #region Cleric Logic
            else if (TeamHeroCoder.BattleState.heroWithInitiative.jobClass == HeroJobClass.Cleric)
            {
                //The character with initiative is a cleric, do something here...
                //AUTOLIFE IS IMPORTANT
                Console.WriteLine("this is a cleric");
                Hero target = null;

                foreach (Hero hero in TeamHeroCoder.BattleState.foeHeroes)
                {
                    if (hero.health > 0)
                    {
                        if (target == null)
                            target = hero;
                        else if (hero.health < target.health)
                            target = hero;
                    }
                }

                //This is the line of code that tells Team Hero Coder that we want to perform the attack action and target the foe with the lowest HP
                TeamHeroCoder.PerformHeroAbility(Ability.Attack, target);
            }

            #endregion

            #region Wizard Logic
            else if (TeamHeroCoder.BattleState.heroWithInitiative.jobClass == HeroJobClass.Wizard)
            {
                //The character with initiative is a wizard, do something here...
                Console.WriteLine("\nthis is a wizard");

                //This is the ONLY target variable we are going to use in this statement
                Hero target = null;
                activeHero = TeamHeroCoder.BattleState.heroWithInitiative;


                //HIGHEST PRIORITY SITS HERE

                //Use revive of the ally if they are dead
                Console.WriteLine("CHECK: ALLY DEATH CHECK");
                foreach (Hero ally in TeamHeroCoder.BattleState.allyHeroes)
                {
                    if (ally.health <= 0)
                    {
                        foreach (InventoryItem ii in TeamHeroCoder.BattleState.allyInventory)
                        {
                            if (ii.item == Item.Revive)
                            {
                                Console.WriteLine("We found a elixer");
                                hasPerformedAction = Evaluation.AttemptToPerformAction(hasPerformedAction, Ability.Revive, ally);
                                TeamHeroCoder.PerformHeroAbility(Ability.Revive, ally);
                                return;
                            }
                        }


                    }
                }

                //Use potion of the ally if they are below 60% health
                Console.WriteLine("CHECK: ALLY HEALTH < 60%");
                foreach (Hero ally in TeamHeroCoder.BattleState.allyHeroes)
                {
                    float healthPercent = (float)ally.health / ally.maxHealth;

                    if (healthPercent <= 0.6f)
                    {
                        foreach (InventoryItem ii in TeamHeroCoder.BattleState.allyInventory)
                        {
                            if (ii.item == Item.Potion)
                            {
                                if (!hasPerformedAction)
                                {
                                    hasPerformedAction = Evaluation.AttemptToPerformAction(hasPerformedAction, Ability.Potion, ally);
                                    TeamHeroCoder.PerformHeroAbility(Ability.Potion, ally);
                                    return;
                                }
                            }
                        }
                    }
                }

                //Check to see if any ally needs an elixer (<= 50% Mana remaining.)
                Console.WriteLine("CHECK: ALLY MANA CHECK");
                foreach (Hero ally in TeamHeroCoder.BattleState.allyHeroes)
                {
                    if (ally.mana <= 50)
                    {
                        foreach (InventoryItem ii in TeamHeroCoder.BattleState.allyInventory)
                        {
                            if (ii.item == Item.Elixir)
                            {
                                Console.WriteLine("We found a elixer");
                                hasPerformedAction = Evaluation.AttemptToPerformAction(hasPerformedAction, Ability.Elixir, ally);
                                TeamHeroCoder.PerformHeroAbility(Ability.Elixir, ally);
                                return;
                            }
                        }


                    }
                }



                //Use ability Meteor if number of standing foes is greater than 2
                // Check for standing foes
                List<Hero> standingFoes = new List<Hero>();

                foreach (Hero foe in TeamHeroCoder.BattleState.foeHeroes)
                {
                    if (foe.health > 0) 
                    {
                        standingFoes.Add(foe);
                    }
                }

                // Use Meteor if 2 or more foes are still standing
                if (standingFoes.Count >= 2)
                {
                    Console.WriteLine("CHECK: More than 2 foes standing, using Meteor!");
                    hasPerformedAction = Evaluation.AttemptToPerformAction(hasPerformedAction, Ability.Meteor, standingFoes[0]); 
                    TeamHeroCoder.PerformHeroAbility(Ability.Meteor, standingFoes[0]); 
                    return;
                }


                

                


                //Quick Dispel if foe has positive status effects
                //Console.WriteLine("CHECK: QUICK DISPEL");
                //foreach (Hero hero in TeamHeroCoder.BattleState.foeHeroes)
                //{
                //    if (hero.statusEffectsAndDurations.Count > 1)
                //    {
                //        TeamHeroCoder.PerformHeroAbility(Ability.QuickDispel, target);
                //        return;
                //    }
                //}

                //Console.WriteLine("CHECK: Enemy Buffs to see if we should dispell");
                //bool hasBuff = false;
                //bool shouldDispel = false;


                //foreach (Hero foe in TeamHeroCoder.BattleState.foeHeroes)
                //{
                //    foreach (StatusEffectAndDuration se in foe.statusEffectsAndDurations)
                //    {
                //        if (se.statusEffect == StatusEffect.Faith || 
                //            se.statusEffect == StatusEffect.AutoLife ||
                //            se.statusEffect == StatusEffect.Brave ||
                //            se.statusEffect == StatusEffect.Haste)
                //        {
                //            Console.WriteLine("Enemy has a positive status condition");
                //            hasBuff = true;
                //            target = foe;
                //            break;
                //        }

                //        hasBuff = false;
                //    }

                //    //if (activeHero.mana >= QUICK_DISPEL_COST)
                //    //{

                //    //    Console.WriteLine("We found buffs it is ok to dispel and have the mana");
                //    //    shouldDispel = true;

                //    //}


                //    //if (!hasBuff)
                //    //{
                //    //    Console.WriteLine("Enemy does not have any buffs");
                //    //    if (shouldDispel)
                //    //    {
                //    //        Console.WriteLine("We have determined that casting Quick Dispel is a good idea.");
                //    //        TeamHeroCoder.PerformHeroAbility(Ability.QuickDispel, target);
                //    //        return;
                //    //    }
                //    //}
                //}

                //Check to see what status effects they DO have vs what remedies they have

                //Console.WriteLine("CHECK: SEEING ENEMY STATUS EFFECTS VS REMEMDIES");

                //bool foePoisonRemedy = false;
                //bool foeSilenceRemedy = false;
                //bool foePetrifyRemedy = false;
                //bool foeFullRemedy = false;

                //foreach (InventoryItem item in TeamHeroCoder.BattleState.foeInventory)
                //{
                //    if (item.item == Item.PoisonRemedy)
                //    {
                //        Console.WriteLine("they have poison remedies");
                //        foePoisonRemedy = true;
                //    }
                //    else if (item.item == Item.SilenceRemedy)
                //    {
                //        Console.WriteLine("they have silence remedies");
                //        foeSilenceRemedy = true;
                //    }
                //    else if (item.item == Item.PetrifyRemedy)
                //    {
                //        Console.WriteLine("they have petrify remedies");
                //        foePetrifyRemedy = true;
                //    }
                //    else if (item.item == Item.FullRemedy)
                //    {
                //        Console.WriteLine("they have full remedies");
                //        foeFullRemedy = true;
                //    }

                //}


                //Cast poison nova if foe does not have poison condition

                //if (activeHero.mana >= POISON_NOVA_COST)
                //{
                //    Console.WriteLine("Wizard's total MP is greater than the cost of Poison Nova!");
                //    foreach (Hero h in TeamHeroCoder.BattleState.foeHeroes)
                //    {
                //        foreach (StatusEffectAndDuration se in h.statusEffectsAndDurations)
                //        {
                //            if (se.statusEffect != StatusEffect.Poison)
                //            {
                //                Console.WriteLine("Target is not Poisoned. Casting Poison Nova");
                //                Evaluation.AttemptToPerformHeroAbility(Ability.PoisonNova, h);
                //            }
                //        }

                //    }
                //}
                //Cast slow if foe does not have slow condition
                //foreach (StatusEffectAndDuration se in target.statusEffectsAndDurations)
                //{
                //    if (se.statusEffect != StatusEffect.Slow)
                //    {
                //        //We have found a character that is not slow, do something here...
                //        TeamHeroCoder.PerformHeroAbility(Ability.Slow, target);
                //        return;
                //    }
                //}
                //Cast doom if foes does not have doom condition
                //if (foeFullRemedy == false)
                //{
                //    foreach (Hero hero in TeamHeroCoder.BattleState.foeHeroes)
                //    {
                //        if (target == null) target = hero;
                //    }

                //    Console.WriteLine("they don't have full remedies casting doom");
                //    if (activeHero.mana >= DOOM_COST)
                //    {
                //        TeamHeroCoder.PerformHeroAbility(Ability.Doom, target);
                //        return;
                //    }
                //}
                //Consider Flame Strike when a "priority enemy" is low HP
                //Priority Targets
                //Cleric with Healer Perk / Any Cleric
                //Alchemist (Any)
                //TODO: Consider how to prioritize targets within our list of priority targets
                List<Hero> priorityTargets = new List<Hero>();
                Console.WriteLine("Gathering Priority Targets for Flame Strike");
                foreach (Hero enemy in TeamHeroCoder.BattleState.foeHeroes)
                {
                    if (enemy.health <= 0) continue;
                
                    if (enemy.perks.Contains(Perk.ClericHealer) ||
                        enemy.jobClass == HeroJobClass.Alchemist)
                    {
                        Console.WriteLine("Added a " + enemy.jobClass);
                        priorityTargets.Add(enemy);
                    }
                    //continue; -> Go look at the next enemy
                    //break; -> Exits the loop and continues executing code
                    //return; -> Exit the loop and "finish" the character's turn
                }

                float flameStrikeDamage = activeHero.special * 15;

                foreach (Hero possibleTarget in priorityTargets)
                {
                    if (possibleTarget.health <= flameStrikeDamage - possibleTarget.specialDefense * flameStrikeDamage)
                    {
                        Console.WriteLine("Found a priority target we can one-shot");
                        if (activeHero.mana <= FLAME_STRIKE_COST)
                        {
                            Console.WriteLine("We have the mana to Flame Strike");
                            hasPerformedAction = Evaluation.AttemptToPerformAction(hasPerformedAction, Ability.FlameStrike, target);
                        }
                    }
                }

                //Cast meteor if the number of standing foes > 1
                //  Cast fireball instead if we don't have enough mana for meteor

                bool isSilenced = false;
                foreach (StatusEffectAndDuration s in activeHero.statusEffectsAndDurations)
                {
                    if (s.statusEffect == StatusEffect.Silence)
                    {
                        isSilenced = true;
                        break;
                    }
                }

                if (!isSilenced)
                {
                    // getting live enemies count
                    

                    //// check cast meteor or fireball
                    //if (liveEnemies > 1)
                    //{
                    //    if (activeHero.mana >= METEOR_COST)
                    //    {
                    //        Console.WriteLine("Casting Meteor");
                    //        Evaluation.AttemptToPerformHeroAbility(Ability.Meteor, target);
                    //    }
                    //    else if (activeHero.mana >= FIREBALL_COST)
                    //    {
                    //        Console.WriteLine("Casting Fireball");
                    //        Evaluation.AttemptToPerformHeroAbility(Ability.Fireball, target);
                    //    }
                    //}
                }
                //LOWEST PRIORITY SITS HERE


                //Attack foe with lowest Health

                //foreach (Hero hero in TeamHeroCoder.BattleState.foeHeroes)
                //{
                //    if (hero.health > 0)
                //    {
                //        if (target == null)
                //            target = hero;
                //        else if (hero.health < target.health)
                //            target = hero;
                //    }
                //}

                //This is the line of code that tells Team Hero Coder that we want to perform the attack action and target the foe with the lowest HP
                hasPerformedAction = Evaluation.AttemptToPerformAction(hasPerformedAction, Ability.Attack, target);
            }
            #endregion


            //foreach (InventoryItem ii in TeamHeroCoder.BattleState.allyInventory)
            //{
            //    //How we look THROUGH our inventory
            //    if (ii.item == Item.Potion)
            //    {
            //        //We found a potion
            //    }
            //}

            ////Find the foe with the lowest health
            //Hero target = null;

            //foreach (Hero hero in TeamHeroCoder.BattleState.foeHeroes)
            //{
            //    if (hero.health > 0)
            //    {
            //        if (target == null)
            //            target = hero;
            //        else if (hero.health < target.health)
            //            target = hero;
            //    }
            //}

            ////This is the line of code that tells Team Hero Coder that we want to perform the attack action and target the foe with the lowest HP
            //TeamHeroCoder.PerformHeroAbility(Ability.Attack, target);

            //Searching for a poisoned hero 
            //foreach (Hero hero in TeamHeroCoder.BattleState.allyHeroes)
            //{
            //    foreach (StatusEffectAndDuration se in hero.statusEffectsAndDurations)
            //    {
            //        if (se.statusEffect == StatusEffect.Poison)
            //        {
            //            //We have found a character that is poisoned, do something here...
            //        }
            //    }
            //}

        }
    }
}
