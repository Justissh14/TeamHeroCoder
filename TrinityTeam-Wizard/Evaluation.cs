using System;
using System.Collections.Generic;
using TeamHeroCoderLibrary;
using static TeamHeroCoderLibrary.TeamHeroCoder;

public static class Evaluation
{
    static public bool AttemptToPerformAction(bool hasPerformedAction, Ability ability, Hero target)
    {
        if (!hasPerformedAction)
        {
            TeamHeroCoder.PerformHeroAbility(ability, target);
            return true;
        }

        return false;
    }

    public static bool hasPerformedAction = false;
    static public void AttemptToPerformHeroAbility(Ability ability, Hero target)
    {
        if (!hasPerformedAction)
        {
            TeamHeroCoder.PerformHeroAbility(ability, target);
        }
    }
}
