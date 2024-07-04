using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Universal_Trade_Hub
{
	public class JobDriver_UseUTConsole : JobDriver
	{
		private Building_UniversalTradeConsole console => (Building_UniversalTradeConsole)TargetThingA;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(console, job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedOrNull(TargetIndex.A);
			yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.InteractionCell).FailOn((Toil to) => !((Building_UniversalTradeConsole)to.actor.jobs.curJob.GetTarget(TargetIndex.A).Thing).CanUseTradeConsoleNow); ;

			Toil openToil = ToilMaker.MakeToil("MakeNewToils");
			openToil.initAction = delegate
			{
				Pawn actor = openToil.actor;
				if (((Building_UniversalTradeConsole)actor.jobs.curJob.GetTarget(TargetIndex.A).Thing).CanUseTradeConsoleNow)
				{
					Find.WindowStack.Add(new UTH_TradingMenu(pawn, console));
				}
			};
			yield return openToil;
		}
	}

	[DefOf]
	public static class UTH_JobDefOf
	{
		public static JobDef UseUTConsole;
	}
}