﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld;

namespace PCC_Code
{

    public class Worker_ChangeTrait : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            System.Random rand = new System.Random();
            if (rand.NextDouble() * 10 > PCC_Mod.settings.freqChange) {
                Log.Message("PCC: Trait change aborted due to set frequency");
                return false;
            }
            Pawn pawntochange = this.GetRandPawn(parms.target);
            if (pawntochange == null)
            {
                return false;
            }
            if (pawntochange.story.traits.allTraits.Count >= ((int)LoadedModManager.GetMod<PCC_Mod>().GetSettings<PCCSettings>().maxTraits))
            {
                return false;
            }

            if (PCC_AllowedTraits.PawnIsInvalid(pawntochange, true)) return false;

            this.SetTrait(pawntochange);
            return true;
        }
        private Pawn GetRandPawn(IIncidentTarget target)
        {
            Map map = target as Map;
            if (map != null)
            {
                /*IEnumerable<Pawn> allpawns = map.mapPawns.FreeColonistsAndPrisoners;
                List<Pawn> listofpawns = new List<Pawn>();
                foreach (Pawn pawn in allpawns)
                {
                    if (pawn.IsColonist && !pawn.NonHumanlikeOrWildMan())
                    {
                        listofpawns.Add(pawn);
                    }
                }
                IntRange pawnlistindices = new IntRange(0, listofpawns.Count - 1);
                int pawnnum = pawnlistindices.RandomInRange;
                return listofpawns[pawnnum];*/
                return map.mapPawns.FreeColonistsAndPrisonersSpawned.RandomElement();
            }
            return null;
        }
        private void SetTrait(Pawn pawn)
        {
            /*if (((int)LoadedModManager.GetMod<PCC_Mod>().GetSettings<PCCSettings>().maxTraits) <= pawn.story.traits.allTraits.Count)
            {
                return 0;
            }*/ //should be an impossible case, leaving just in case
            List<TraitDef> validtraits = PCC_AllowedTraits.getValidTraits(adding: true);
            validtraits.Remove(TraitDefOf.DislikesWomen); // if this were still here, the chance would be too high and a pawn could get the wrong gendered one. Instead it's handled below.

            if (validtraits.Count == 0)
            {
                Log.Message("PCC: no valid traits to change, skipping...");
                return;
            }

            TraitDef traitdeftouse = validtraits[new IntRange(0, validtraits.Count - 1).RandomInRange];
            if (traitdeftouse.Equals(TraitDefOf.Transhumanist) && pawn.story.traits.HasTrait(TraitDefOf.BodyPurist)) {
                Log.Message("tried to give Transhumanist to pawn with Body Purist");return;
            }
            if (traitdeftouse.Equals(TraitDefOf.BodyPurist) && pawn.story.traits.HasTrait(TraitDefOf.Transhumanist)) {
                Log.Message("tried to give Body Purist to pawn with Transhumanist"); return;
            }
            if (traitdeftouse.Equals(TraitDefOf.DislikesMen) && pawn.gender.Equals(Gender.Male))
            {
                traitdeftouse = TraitDefOf.DislikesWomen;
            }
            List<TraitDegreeData> degrees = new List<TraitDegreeData>();
            foreach (TraitDegreeData degreedata in traitdeftouse.degreeDatas)
            {
                degrees.Add(degreedata);
            }
            TraitDegreeData degreedatatouse = degrees.RandomElement();
            int index = 0;
            if (pawn.story.traits.HasTrait(traitdeftouse))
            {
                foreach (Trait trait in pawn.story.traits.allTraits)
                {
                    if (trait.def.Equals(traitdeftouse))
                    {
                        if (trait.Degree == degreedatatouse.degree)
                        {
                            return;
                        }
                        else
                        {
                            pawn.story.traits.allTraits.RemoveAt(index);
                            break;
                        }
                    }
                    index++;
                }
            }
            if (traitdeftouse.Equals(TraitDef.Named("ShootingAccuracy")) && pawn.story.traits.HasTrait(TraitDefOf.Brawler))
            {
                return;
            }
            if (traitdeftouse.Equals(TraitDef.Named("Jealous")) || traitdeftouse.Equals(TraitDefOf.Greedy) || traitdeftouse.Equals(TraitDefOf.Ascetic))
            {
                index = 0;
                foreach (Trait trait in pawn.story.traits.allTraits)
                {
                    if (trait.def.Equals(TraitDefOf.Ascetic) || trait.def.Equals(TraitDef.Named("Jealous")) || trait.def.Equals(TraitDefOf.Greedy))
                    {
                        pawn.story.traits.allTraits.RemoveAt(index);
                        break;
                    }
                    index++;
                }
            }
            Trait toGain = new Trait(traitdeftouse, degreedatatouse.degree);
            pawn.story.traits.GainTrait(toGain);
            Find.LetterStack.ReceiveLetter("PCC_TraitChanged".Translate(), "PCC_GainedTraitMessage".Translate(pawn, toGain.Label), LetterDefOf.NeutralEvent, pawn);

            if (pawn.workSettings != null)
            {
                pawn.workSettings.Notify_DisabledWorkTypesChanged(); // honestly idk if this line
                pawn.workSettings.Notify_UseWorkPrioritiesChanged(); // or this line will work (but no problems yet)
            }
                if (pawn.skills != null)
            {
                pawn.skills.Notify_SkillDisablesChanged();
            }
            if (!pawn.Dead && pawn.RaceProps.Humanlike)
            {
                pawn.needs.mood.thoughts.situational.Notify_SituationalThoughtsDirty();
            }
            return;
        }
    }
    public class Worker_RemoveTrait : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            System.Random rand = new System.Random();
            if (rand.NextDouble() *  10 > PCC_Mod.settings.freqRemove) {
                Log.Message("PCC: Trait remove aborted due to set frequency");
                return false;
            }
            Pawn pawntochange = this.GetRandPawn(parms.target);
            if (pawntochange == null)
            {
                return false;
            }

            if (PCC_AllowedTraits.PawnIsInvalid(pawntochange, true)) return false;

            this.SetTrait(pawntochange);
            return true;
        }
        private Pawn GetRandPawn(IIncidentTarget target)
        {
            Map map = target as Map;
            if (map != null)
            {
                /*IEnumerable<Pawn> allpawns = map.mapPawns.FreeColonistsAndPrisoners;
                List<Pawn> listofpawns = new List<Pawn>();
                foreach (Pawn pawn in allpawns)
                {
                    if (pawn.IsColonist && !pawn.NonHumanlikeOrWildMan())
                    {
                        listofpawns.Add(pawn);
                    }
                }
                IntRange pawnlistindices = new IntRange(0, listofpawns.Count - 1);
                int pawnnum = pawnlistindices.RandomInRange;
                return listofpawns[pawnnum];*/
                return map.mapPawns.FreeColonistsAndPrisonersSpawned.RandomElement();
            }
            return null;
        }
        private void SetTrait(Pawn pawn)
        {
            if (pawn.story.traits.allTraits.Count == 0)
            {
                Log.Message("Pawn has no traits: " + pawn.Name.ToString());
                return;
            }
            if (pawn.story.traits.allTraits.Count <= PCC_Mod.settings.minTraits) {
                Log.Message("Pawn has too few traits to lose any: " + pawn.Name.ToString() + " has " + pawn.story.traits.allTraits.Count + " but min " + PCC_Mod.settings.minTraits);
                return;
            }

            List<TraitDef> validtraits = PCC_AllowedTraits.getValidTraits(adding: false);

            if (validtraits.Count == 0)
            {
                Log.Message("PCC: no valid traits to remove, skipping...");
                return;
            }

            int index = new IntRange(0, pawn.story.traits.allTraits.Count - 1).RandomInRange;

            bool flag = false;
            foreach (Trait trait in pawn.story.traits.allTraits) {
                if (validtraits.Contains(trait.def)) flag = true;
            }
            if (!flag) {
                Log.Message("Valid traits doesn't contain trait " + pawn.story.traits.allTraits.ElementAt(index).def.label);
                return;
            }

            while (!validtraits.Contains(pawn.story.traits.allTraits.ElementAt(index).def)) {
                index = new IntRange(0, pawn.story.traits.allTraits.Count - 1).RandomInRange;
            }

            Log.Message("removed: " + pawn.story.traits.allTraits.ElementAt(index).def.defName + " from pawn " + pawn.NameShortColored);

            Find.LetterStack.ReceiveLetter("PCC_TraitChanged".Translate(), "PCC_LostTraitMessage".Translate(pawn, pawn.story.traits.allTraits.ElementAt(index).Label), LetterDefOf.NeutralEvent, pawn);
            pawn.story.traits.allTraits.RemoveAt(index);
            if (pawn.workSettings != null)
            {
                pawn.workSettings.Notify_DisabledWorkTypesChanged(); // honestly idk if this line
                pawn.workSettings.Notify_UseWorkPrioritiesChanged(); // or this line will work
            }
            if (pawn.skills != null)
            {
                pawn.skills.Notify_SkillDisablesChanged();
            }
            if (!pawn.Dead && pawn.RaceProps.Humanlike)
            {
                pawn.needs.mood.thoughts.situational.Notify_SituationalThoughtsDirty();
            }
            return;
        }
    }
    public class OnTick : MapComponent
    {
        Dictionary<Pawn, int> checkColonistMoods = new Dictionary<Pawn, int>();
        Dictionary<Pawn, int> checkColonistAcclimation = new Dictionary<Pawn, int>();
        Dictionary<Pawn, bool> checkColonistCannibal = new Dictionary<Pawn, bool>();
        List<Pawn> pawns = new List<Pawn>(); // why? idk but ExposeData makes you keep one of these for each class that implements ILoadReferenceable
        List<int> moods = new List<int>();
        List<int> acclimations = new List<int>();
        List<bool> cannibals = new List<bool>();
        System.Random rand = new System.Random();

        private int evalInterval = 120; // tick interval before evaluating colonist moods, to reduce lag. Currently should be about once per second.

        public override void MapComponentTick() {
            if (Find.TickManager.TicksGame % evalInterval == 0) {
                for (int pawnNum = 0; pawnNum < map.mapPawns.FreeColonistsAndPrisonersSpawned.Count; pawnNum++) {
                    Pawn pawn = map.mapPawns.FreeColonistsAndPrisonersSpawned[pawnNum];

                    if (PCC_AllowedTraits.PawnIsInvalid(pawn, false)) continue;

                    // below: tests for other mods, easier to recomment than rewrite
                    /*foreach (Thought_Memory thought in pawn.needs.mood.thoughts.memories.Memories) {
                        Messages.Message(new Message(thought.def.defName, MessageTypeDefOf.NeutralEvent));
                    }*/
                    /*Messages.Message(new Message(pawn.CurJobDef.defName, MessageTypeDefOf.NeutralEvent));*/
                    if (!checkColonistMoods.ContainsKey(pawn)) {
                        checkColonistMoods.Add(pawn, 0);
                        if (!pawn.story.traits.HasTrait(TraitDefOf.BodyPurist)) checkColonistCannibal.Add(pawn, false);
                    }
                    if (prosthetics(pawn) > 0 && pawn.story.traits.HasTrait(TraitDefOf.BodyPurist)) {
                        if (!checkColonistAcclimation.ContainsKey(pawn)) {
                            checkColonistAcclimation.Add(pawn, 0);
                        }
                        checkColonistAcclimation[pawn] += prosthetics(pawn);
                        if (checkColonistAcclimation[pawn] > 1800000) {
                            if (rand.NextDouble() < (1.0 / 3.0)) {
                                checkColonistAcclimation.Remove(pawn);
                                TraitSetConditional(pawn, TraitDefOf.Transhumanist); // not guaranteed Transhumanist, possibly just no longer body purist, chance is handled in TraitSetConditional
                            }
                            else {
                                checkColonistAcclimation[pawn] = 0;
                            }
                        }
                    }

                    if (PCC_Mod.settings.doOptimistDepressive)
                    {
                        MentalBreaker breaker = new MentalBreaker(pawn);
                        if (breaker.BreakMinorIsImminent)
                        {
                            checkColonistMoods[pawn] -= 1 * evalInterval;
                        }
                        else if (breaker.BreakMajorIsImminent)
                        {
                            checkColonistMoods[pawn] -= 2 * evalInterval;
                        }
                        else if (breaker.BreakExtremeIsImminent)
                        {
                            checkColonistMoods[pawn] -= 4 * evalInterval;
                        }
                        else if (pawn.needs.mood.CurLevelPercentage > .9)
                        {
                            checkColonistMoods[pawn] += 3 * evalInterval;
                        }
                        else if (pawn.needs.mood.CurLevelPercentage > .7)
                        {
                            checkColonistMoods[pawn] += 1 * evalInterval;
                        }
                        else
                        {
                            if (checkColonistMoods[pawn] < 0)
                            {
                                checkColonistMoods[pawn] += 1 * evalInterval;
                            }
                            if (checkColonistMoods[pawn] > 0)
                            {
                                checkColonistMoods[pawn] -= 1 * evalInterval;
                            }
                        }
                        if (checkColonistMoods[pawn] < -720000)
                        {
                            if (new IntRange(1, 2).RandomInRange == 1)
                            {
                                this.TraitSetConditional(pawn, TraitDefOf.NaturalMood, -2);
                            }
                            else
                            {
                                checkColonistMoods[pawn] = 0;
                            }
                        }
                        if (checkColonistMoods[pawn] > 900000) //900000
                        { 
                            if (new IntRange(1, 2).RandomInRange == 1 && !(pawn.story.traits.HasTrait(TraitDefOf.NaturalMood) && pawn.story.traits.DegreeOfTrait(TraitDefOf.NaturalMood) == 2))
                            {
                                this.TraitSetConditional(pawn, TraitDefOf.NaturalMood, 1);
                            }
                            else
                            {
                                checkColonistMoods[pawn] = 0;
                            }
                        }
                    }

                    List<Thought> thoughtslist = new List<Thought>();
                    pawn.needs.mood.thoughts.GetAllMoodThoughts(thoughtslist);
                    if (this.ThoughtListContains(pawn, ThoughtDefOf.AteHumanlikeMeatDirect) && !checkColonistCannibal[pawn]) {
                        checkColonistCannibal[pawn] = true;
                        if (new IntRange(1, 300).RandomInRange == 1) // 300
                        {
                            this.TraitSetConditional(pawn, TraitDefOf.Cannibal);
                            pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(ThoughtDefOf.AteHumanlikeMeatAsIngredient);
                            pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(ThoughtDefOf.AteHumanlikeMeatDirect);
                            pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.AteHumanlikeMeatDirectCannibal);
                        }
                    }
                    if (this.ThoughtListContains(pawn, ThoughtDefOf.AteHumanlikeMeatAsIngredient) && !checkColonistCannibal[pawn]) {
                        checkColonistCannibal[pawn] = true;
                        if (new IntRange(1, 100).RandomInRange == 1) // 100
                        {
                            this.TraitSetConditional(pawn, TraitDefOf.Cannibal);
                            pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(ThoughtDefOf.AteHumanlikeMeatAsIngredient);
                            pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(ThoughtDefOf.AteHumanlikeMeatDirect);
                            pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.AteHumanlikeMeatAsIngredientCannibal);
                        }
                    }
                    if (!this.ThoughtListContains(pawn, ThoughtDefOf.AteHumanlikeMeatAsIngredient) && !ThoughtListContains(pawn, ThoughtDefOf.AteHumanlikeMeatDirect)) {
                        checkColonistCannibal[pawn] = false;
                    }
                }
            } // end if referencing evalInterval
        }
        public OnTick(Map map) : base(map)
        {}
        private void TraitSetConditional(Pawn pawn, TraitDef traitdef, int degree = 0)
        {
            if (pawn.story.traits.allTraits.Count < 4)
            {
                if (traitdef.Equals(TraitDefOf.NaturalMood)) {
                    if (pawn.story.traits.HasTrait(TraitDefOf.NaturalMood) && pawn.story.traits.GetTrait(TraitDefOf.NaturalMood).Degree == degree) {
                        return;
                    }
                    if (pawn.story.traits.HasTrait(TraitDefOf.NaturalMood)) {
                        int index = 0;
                        foreach (Trait trait in pawn.story.traits.allTraits) {
                            if (trait.def.Equals(TraitDefOf.NaturalMood)) {
                                break;
                            }
                            index++;
                        }
                        pawn.story.traits.allTraits.RemoveAt(index);
                    }
                    pawn.story.traits.GainTrait(new Trait(traitdef, degree));
                    if (pawn.workSettings != null) {
                        pawn.workSettings.Notify_DisabledWorkTypesChanged(); // honestly idk if this line
                        pawn.workSettings.Notify_UseWorkPrioritiesChanged(); // or this line will work
                    }
                        if (pawn.skills != null) {
                        pawn.skills.Notify_SkillDisablesChanged();
                    }
                    if (!pawn.Dead && pawn.RaceProps.Humanlike) {
                        pawn.needs.mood.thoughts.situational.Notify_SituationalThoughtsDirty();
                    }
                    if (degree == 1 && pawn.story.traits.DegreeOfTrait(TraitDefOf.NaturalMood) != 2) {
                        Find.LetterStack.ReceiveLetter("PCC_Optimist".Translate(), "PCC_OptimistMessage".Translate(pawn), LetterDefOf.PositiveEvent, pawn);
                    }
                    else if (degree == -2) {
                        Find.LetterStack.ReceiveLetter("PCC_Depressive".Translate(), "PCC_DepressiveMessage".Translate(pawn), LetterDefOf.NegativeEvent, pawn);
                    }
                }
                else if (traitdef.Equals(TraitDefOf.Cannibal)) {
                    if (!pawn.story.traits.HasTrait(TraitDefOf.Cannibal)) {
                        pawn.story.traits.GainTrait(new Trait(traitdef));
                        if (pawn.workSettings != null) {
                            pawn.workSettings.Notify_DisabledWorkTypesChanged(); // honestly idk if this line
                            pawn.workSettings.Notify_UseWorkPrioritiesChanged(); // or this line will work
                            }
                            if (pawn.skills != null) {
                            pawn.skills.Notify_SkillDisablesChanged();
                        }
                        if (!pawn.Dead && pawn.RaceProps.Humanlike) {
                            pawn.needs.mood.thoughts.situational.Notify_SituationalThoughtsDirty();
                        }
                        Find.LetterStack.ReceiveLetter("PCC_Cannibal".Translate(), "PCC_CannibalMessage".Translate(pawn), LetterDefOf.NegativeEvent, pawn);
                    }
                }
                else if (traitdef.Equals(TraitDefOf.Transhumanist)) { // IMPORTANT: ASSUMES PAWN HAS EXACTLY ONE COPY OF BODY PURIST (I don't think there can be multiple but worth mentioning)
                    Trait toRemove = null;
                    foreach (Trait trait in pawn.story.traits.allTraits) {
                        if (trait.def.Equals(TraitDefOf.BodyPurist)) {
                            toRemove = trait;
                        }
                    }
                    pawn.story.traits.allTraits.Remove(toRemove);
                    if (pawn.workSettings != null) {
                                pawn.workSettings.Notify_DisabledWorkTypesChanged(); // honestly idk if this line
                                pawn.workSettings.Notify_UseWorkPrioritiesChanged(); // or this line will work
                        }
                                if (pawn.skills != null) {
                        pawn.skills.Notify_SkillDisablesChanged();
                    }
                    if (!pawn.Dead && pawn.RaceProps.Humanlike) {
                        pawn.needs.mood.thoughts.situational.Notify_SituationalThoughtsDirty();
                    }
                    if (rand.NextDouble() < (1.0/3.0)) { // 1 in 3 to become transhumanist, else just lose body purist
                        pawn.story.traits.GainTrait(new Trait(traitdef));
                        Find.LetterStack.ReceiveLetter("PCC_Transhumanist".Translate(), "PCC_TranshumanistMessage".Translate(pawn), LetterDefOf.NegativeEvent, pawn);
                    }
                    else {
                        Find.LetterStack.ReceiveLetter("PCC_PuristLost".Translate(), "PCC_PuristLostMessage".Translate(pawn), LetterDefOf.NegativeEvent, pawn);
                    }
                }
            }
        }
        private bool ThoughtListContains(Pawn pawn, ThoughtDef def)
        {
            bool yes = false;
            List<Thought> thoughtslist = new List<Thought>();
            pawn.needs.mood.thoughts.GetAllMoodThoughts(thoughtslist);
            foreach (Thought thought in thoughtslist)
            {
                if (thought.def.Equals(def))
                {
                    yes = true;
                }
            }
            return yes;
        }
        private int prosthetics(Pawn pawn) {
            int count = 0;
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs) {
                AddedBodyPartProps addedPartProps = hediff.def.addedPartProps;
                if (addedPartProps != null && addedPartProps.betterThanNatural) {
                    count += 1;
                }
            }
            return count;
        }
        public override void ExposeData() {
            /*        IDictionary<Pawn, int> checkColonistMoods = new Dictionary<Pawn, int>();
        IDictionary<Pawn, int> checkColonistAcclimation = new Dictionary<Pawn, int>();
        IDictionary<Pawn, bool> checkColonistCannibal = new Dictionary<Pawn, bool>();*/
            //if(Scribe.mode == LoadSaveMode.Saving) {
                IDictionary<Pawn, int> checkColonistMoodsSave = ReturnNonNullKeys(checkColonistMoods);
                IDictionary<Pawn, int> checkColonistAcclimationSave = ReturnNonNullKeys(checkColonistAcclimation);
                IDictionary<Pawn, bool> checkColonistCannibalSave = ReturnNonNullKeys(checkColonistCannibal);
            
            Scribe_Collections.Look(ref checkColonistMoods, "PCC_checkColonistMoods", LookMode.Reference, LookMode.Value, ref pawns, ref moods);
            Scribe_Collections.Look(ref checkColonistAcclimation, "PCC_checkColonistAcclimation", LookMode.Reference, LookMode.Value, ref pawns, ref acclimations);
            Scribe_Collections.Look(ref checkColonistCannibal, "PCC_checkColonistCannibal", LookMode.Reference, LookMode.Value, ref pawns, ref cannibals);

        }
        private Dictionary<K,V> ReturnNonNullKeys<K,V>(Dictionary<K,V> dict) {
            Dictionary<K, V> nonNulls = new Dictionary<K, V>();
            foreach (KeyValuePair<K,V> data in dict) {
                if (data.Key != null) nonNulls.Add(data.Key, data.Value);
            }
            return nonNulls;
        }
    }
    public class PCC_AllowedTraits {
        public static List<TraitDef> getValidTraits(bool adding) { // Set adding to true when gaining/swapping a trait, and false otherwise
            List<TraitDef> validtraits = new List<TraitDef>(); // note: this would work better w/yield return, should refactor later but not important
            if (!PCC_Mod.settings.usingNewSelector) {
                if (PCC_Mod.settings.sexualitiesAllowedToChange) { validtraits.Add(TraitDefOf.Gay); validtraits.Add(TraitDefOf.Asexual); validtraits.Add(TraitDefOf.Bisexual); }
                if (!PCC_Mod.settings.moddedTraits) {
                    validtraits.Add(TraitDefOf.Abrasive);
                    validtraits.Add(TraitDefOf.Ascetic);
                    validtraits.Add(TraitDefOf.Bloodlust);
                    validtraits.Add(TraitDefOf.Cannibal);
                    validtraits.Add(TraitDefOf.DislikesMen);
                    validtraits.Add(TraitDefOf.DrugDesire);
                    validtraits.Add(TraitDefOf.Greedy);
                    validtraits.Add(TraitDefOf.Undergrounder);
                    validtraits.Add(TraitDefOf.Industriousness);
                    validtraits.Add(TraitDefOf.Kind);
                    validtraits.Add(TraitDefOf.NaturalMood);
                    validtraits.Add(TraitDefOf.Nerves);
                    validtraits.Add(TraitDefOf.Nudist);
                    validtraits.Add(TraitDefOf.Transhumanist);
                    validtraits.Add(TraitDef.Named("Gourmand"));
                    validtraits.Add(TraitDefOf.PsychicSensitivity);
                    validtraits.Add(TraitDefOf.Psychopath);
                    validtraits.Add(TraitDefOf.TooSmart);
                    validtraits.Add(TraitDef.Named("Neurotic"));
                    validtraits.Add(TraitDef.Named("ShootingAccuracy"));
                    validtraits.Add(TraitDef.Named("Masochist"));
                    validtraits.Add(TraitDef.Named("NightOwl"));
                    validtraits.Add(TraitDef.Named("Jealous"));
                    validtraits.Add(TraitDef.Named("Wimp"));
                    validtraits.Add(TraitDef.Named("Nimble"));
                    validtraits.Add(TraitDef.Named("FastLearner"));
                    validtraits.Add(TraitDef.Named("Immunity"));
                }
                else {
                    foreach (TraitDef tdef in DefDatabase<TraitDef>.AllDefsListForReading) {
                        if (tdef.Equals(TraitDefOf.Brawler)) { continue; }
                        if (tdef.Equals(TraitDefOf.Beauty)) { continue; }
                        if (tdef.Equals(TraitDefOf.AnnoyingVoice)) { continue; }
                        if (tdef.Equals(TraitDefOf.CreepyBreathing)) { continue; }
                        if (tdef.Equals(TraitDefOf.Gay) || tdef.Equals(TraitDefOf.Asexual) || tdef.Equals(TraitDefOf.Bisexual)) { continue; } // added separately
                        validtraits.Add(tdef);
                    }
                }
            } else {
                foreach (TraitDef def in DefDatabase<TraitDef>.AllDefs) {
                    if (TraitIsValid(def, adding)) { // TraitDefOf.DislikesWomen is handled separately so pawns get the right one for their gender (it's arbitrary which one gets excluded from this list)
                        validtraits.Add(def);
                    }
                }
            }
            return validtraits;
        }

        /**
         * Returns true if a pawn should not be considered by the mod at all (i.e. no trait changes or removals, no learned changes like Optimist, etc.)
         **/
        public static bool PawnIsInvalid(Pawn pawn, bool outputWhy)
        {
            List<Thought> outThoughts = new List<Thought>();
            pawn.needs.mood.thoughts.GetAllMoodThoughts(outThoughts); // For some reason, just searching thoughts.memories.Memories doesn't show non-social thoughts
            foreach (Thought thought in outThoughts)
            {
                if (thought.def.defName.Equals("ChJDroidAlways")) // Assumption: pawn is a droid from the Androids mod by ChJeez if and only if it has this specific thought
                {
                    if (outputWhy) Log.Message("Pawn " + pawn.Name + " is a droid from Androids mod, ignoring...");
                    return true;
                }
            }
            return false;
        }

        private static bool TraitIsValid(TraitDef def, bool adding) // set adding to true if adding a trait, false if removing
        {
            PCCSettings.TraitSetting s = PCC_Mod.settings.GetTraitSetting(def.defName);
            if (s == PCCSettings.TraitSetting.AnyChange) return true;
            if (s == PCCSettings.TraitSetting.NeverChange || s == PCCSettings.TraitSetting.NeverChangeModConflict) return false;
            if (adding && s == PCCSettings.TraitSetting.AddNoRemove) return true;
            else if (!adding && s == PCCSettings.TraitSetting.RemoveNoAdd) return true;
            return false;
        }
    }
}