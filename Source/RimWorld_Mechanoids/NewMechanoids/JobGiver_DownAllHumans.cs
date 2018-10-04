using System;
using System.Linq;
using Harmony;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace MoreMechanoids
{
    public class JobGiver_DownAllHumans : JobGiver_AIFightEnemies
    {
        public float targetKeepRadius = 72f;
        public float targetAcquireRadius = 65f;
        private Predicate<Thing> validDoor = t => t is Building_Door && !t.Destroyed && !((Building_Door) t).Open;
        private Predicate<Thing> validPawn = t => t is Pawn && !t.Destroyed && !((Pawn) t).Downed && t.def.race != null && t.def.race.IsFlesh;

        protected override bool ExtraTargetValidator(Pawn pawn, Thing target)
        {
            return validDoor(target) || validPawn(target);
        }

        protected override void UpdateEnemyTarget(Pawn pawn)
        {
            if (pawn == null) return;
            Thing thing = pawn.mindState.enemyTarget;
            if (thing != null)
            {
                if (thing.Destroyed || Find.TickManager.TicksGame - pawn.mindState.lastEngageTargetTick > 400 || !pawn.CanReach(thing, PathEndMode.Touch, Danger.Deadly, true)
                    || (pawn.Position - thing.Position).LengthHorizontalSquared > targetKeepRadius*targetKeepRadius)
                {
                    thing = null;
                }
                Pawn pawn2 = thing as Pawn;
                if (pawn2 != null && pawn2.Downed)
                {
                    thing = null;
                }
                Building_Door door = thing as Building_Door;
                if (door != null && door.Open)
                {
                    thing = null;
                }
            }
            // Select only flesh stuff
            if (thing == null)
            {
                //Log.Message(pawn.Label + ": trying to find target...");
                const TargetScanFlags targetScanFlags = TargetScanFlags.NeedReachable;

                thing = AttackTargetFinder.BestAttackTarget(pawn, targetScanFlags, validPawn, 0f, targetAcquireRadius) as Thing;
                if (thing == null)
                {
                    //Thing thing2 = GenAI.BestAttackTarget(pawn.Position, pawn, validatorDoor, targetAcquireRadius, 0f, targetScanFlags2);
                    Building_Door d =
                        pawn.Map.listerBuildings.AllBuildingsColonistOfClass<Building_Door>()
                            .Where(b => validDoor(b) && pawn.Map.reachability.CanReach(b.Position, pawn.Position, PathEndMode.Touch, TraverseMode.NoPassClosedDoorsOrWater, Danger.Deadly))
                            .OrderBy(t => t.Position.DistanceToSquared(pawn.Position))
                            .FirstOrDefault();
                    if (d != null)
                    {
                        //Log.Message("Selected door " + thing2.Label);
                        thing = d;
                    }
                }
                if (thing != null && thing != pawn.mindState.enemyTarget)
                {
                    Log.Message(pawn.LabelShort + " attacking " + thing.LabelShort + " at " + thing.Position);
                    Traverse.Create(pawn.mindState).Method("Notify_EngagedTarget");

                    Lord lord = pawn.Map.lordManager.LordOf(pawn);
                    if (lord != null)
                    {
                        lord.Notify_PawnAcquiredTarget(pawn, thing);
                    }

                    if (pawn.CurJob != null)
                    {
                        pawn.CurJob.SetTarget(TargetIndex.A, thing);
                    }
                    pawn.mindState.enemyTarget = thing;
                }
                if(thing == null)
                {
                    pawn.mindState.enemyTarget = null;
                }
            }
        }
    }
}
