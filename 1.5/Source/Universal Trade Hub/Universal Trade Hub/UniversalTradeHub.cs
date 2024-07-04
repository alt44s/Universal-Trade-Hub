using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace Universal_Trade_Hub
{
	public class Building_UniversalTradeConsole : Building_CommsConsole
	{
		private CompPowerTrader powerComp;

		public bool CanUseTradeConsoleNow
		{
			get
			{
				return base.Spawned && (powerComp == null || powerComp.PowerOn) && !base.Map.gameConditionManager.ElectricityDisabled(base.Map);
			}
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			powerComp = GetComp<CompPowerTrader>();
		}

		private FloatMenuOption GetFailureReason(Pawn pawn)
		{
			if (!pawn.CanReach(this, PathEndMode.InteractionCell, Danger.Some))
			{
				return new FloatMenuOption("CannotUseNoPath".Translate(), null);
			}
			if (base.Spawned && base.Map.gameConditionManager.ElectricityDisabled(base.Map))
			{
				return new FloatMenuOption("CannotUseSolarFlare".Translate(), null);
			}
			if (powerComp != null && !powerComp.PowerOn)
			{
				return new FloatMenuOption("CannotUseNoPower".Translate(), null);
			}
			if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking))
			{
				return new FloatMenuOption("CannotUseReason".Translate("IncapableOfCapacity".Translate(PawnCapacityDefOf.Talking.label, pawn.Named("PAWN"))), null);
			}
			if (!Building_OrbitalTradeBeacon.AllPowered(Map).Any())
			{
				return new FloatMenuOption("CannotUseReason".Translate("MessageNeedBeaconToTradeWithShip".Translate()), null);
			}
			if (!CanUseTradeConsoleNow)
			{
				Log.Error(string.Concat(pawn, " could not use trade console for unknown reason."));
				return new FloatMenuOption("CannotUseReason".Translate("Cannot use now."), null);
			}
			return null;
		}

		public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn pawn)
		{
			FloatMenuOption failureReason = GetFailureReason(pawn);
			if (failureReason != null)
			{
				yield return failureReason;
				yield break;
			}

			yield return new FloatMenuOption("UTH_ConnectToUniversalTradeHub".Translate(), () => CallHub(pawn));
		}

		private void CallHub(Pawn pawn)
		{
			var job = JobMaker.MakeJob(UTH_JobDefOf.UseUTConsole, this);
			pawn.jobs.StartJob(job, JobCondition.InterruptForced);
		}
	}
}