using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Universal_Trade_Hub
{
	public class UTH_WorldComponent_SubscriptionTracker : WorldComponent
	{
		//can only have 1 subscription
		public int subscriptionTick;

		public bool hasSubscription = false;

		public UTH_WorldComponent_SubscriptionTracker(World world) : base(world)
		{
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref subscriptionTick, "subscriptionTick");
			Scribe_Values.Look(ref hasSubscription, "hasSubscription", false);
		}

		public override void WorldComponentTick()
		{
			base.WorldComponentTick();
			if (Find.TickManager.TicksGame % GenDate.TicksPerDay == 0)
			{
				if (hasSubscription)
				{
					CheckSubscription();
				}
			}
		}

		private void CheckSubscription()
		{
			if (Find.TickManager.TicksGame >= subscriptionTick)
			{
				UTH_UIData.systemAlert1.PlayOneShotOnCamera();
				Messages.Message("UTH_SubExpired".Translate(), MessageTypeDefOf.SilentInput, historical: false);
				hasSubscription = false;
			}
		}
	}

	public class UTH_WorldComponent_OrderScheduler : WorldComponent
	{
		private List<ScheduledOrderData> scheduledOrders = new List<ScheduledOrderData>();

		public List<ScheduledOrderData> ScheduledOrders => scheduledOrders;

		private readonly float attackChance = UTH_Mod.settings.attackChance;
		private int nextAttackCheckTick;
		private int attackDelay = GenDate.TicksPerDay * 2;

		private HashSet<ScheduledOrderData> ordersToRemove = new HashSet<ScheduledOrderData>();

		public UTH_WorldComponent_OrderScheduler(World world) : base(world)
		{
			nextAttackCheckTick = Find.TickManager.TicksGame + attackDelay;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look(ref scheduledOrders, "scheduledOrders", LookMode.Deep);
		}

		public override void WorldComponentTick()
		{
			base.WorldComponentTick();
			if (Find.TickManager.TicksGame % GenDate.TicksPerHour == 0)
			{
				if (scheduledOrders.Any())
				{
					CheckAndDeliverOrders();
				}
			}
		}

		public void ScheduleOrder(ScheduledOrderData orderData)
		{
			scheduledOrders.Add(orderData);
		}

		public void RemoveOrder(ScheduledOrderData order)
		{
			ordersToRemove.Add(order);
		}

		private void CheckAndDeliverOrders()
		{
			ProcessRemovals();

			List<ScheduledOrderData> deliveredOrders = new List<ScheduledOrderData>();
			foreach (var order in scheduledOrders)
			{
				if (order.map == null)
				{
					UTH_UIData.systemAlert1.PlayOneShotOnCamera();
					Messages.Message("UTH_OrderDestinationInvalid".Translate(), MessageTypeDefOf.SilentInput, historical: false);
					deliveredOrders.Add(order);
				}
				else if (Find.TickManager.TicksGame >= nextAttackCheckTick && Random.value < attackChance)
				{
					if (order.insuranceChecked)
					{
						UTH_UIData.systemAlert1.PlayOneShotOnCamera();
						RefundOrder(order, true);
						Messages.Message("UTH_OrderAttackedAndRefunded".Translate(), MessageTypeDefOf.SilentInput, historical: false);
						deliveredOrders.Add(order);
					}
					else
					{
						UTH_UIData.systemAlert1.PlayOneShotOnCamera();
						Messages.Message("UTH_OrderAttackedAndLost".Translate(), MessageTypeDefOf.SilentInput, historical: false);
						deliveredOrders.Add(order);
					}

					nextAttackCheckTick = Find.TickManager.TicksGame + attackDelay;
				}
				else if (Find.TickManager.TicksGame >= order.deliveryTick)
				{
					DeliverOrder(order);

					deliveredOrders.Add(order);
				}
			}

			foreach (var order in deliveredOrders)
			{
				scheduledOrders.Remove(order);
			}
		}

		public void RefundOrder(ScheduledOrderData order, bool insuranceDeduction = false)
		{
			List<Thing> refundItems = new List<Thing>();
			Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
			int orderCost = (int)order.orderCost;
			if (insuranceDeduction)
			{
				orderCost -= (int)order.insuranceCost;
			}

			silver.stackCount = orderCost;
			refundItems.Add(silver);

			DropPodUtility.DropThingsNear(order.deliveryPoint, order.map, refundItems, 110, canInstaDropDuringInit: false, leaveSlag: true, canRoofPunch: false);
		}

		private void DeliverOrder(ScheduledOrderData order)
		{
			DropPodUtility.DropThingsNear(order.deliveryPoint, order.map, order.items, 110, canInstaDropDuringInit: false, leaveSlag: true, canRoofPunch: false, forbid: UTH_Mod.settings.forbidOnDrop);

			UTH_UIData.systemAlert1.PlayOneShotOnCamera();
			Messages.Message("UTH_OrderDelivered".Translate(), MessageTypeDefOf.SilentInput, historical: false);
		}

		private void ProcessRemovals()
		{
			if (ordersToRemove.Any())
			{
				foreach (var order in ordersToRemove)
				{
					scheduledOrders.Remove(order);
				}
				ordersToRemove.Clear();
			}
		}
	}

	[StaticConstructorOnStartup]
	public static class OrderSchedulerInitializer
	{
		static OrderSchedulerInitializer()
		{
			if (Current.Game != null && Current.Game.World != null)
			{
				if (Current.Game.World.GetComponent<UTH_WorldComponent_OrderScheduler>() == null)
				{
					Current.Game.World.components.Add(new UTH_WorldComponent_OrderScheduler(Current.Game.World));
				}
			}
		}
	}

	public class ScheduledOrderData : IExposable
	{
		public List<Thing> items;
		public int deliveryTick;
		public Map map;
		public IntVec3 deliveryPoint;
		public bool insuranceChecked;
		public float insuranceCost;
		public float orderCost;

		public void ExposeData()
		{
			Scribe_Collections.Look(ref items, "items", LookMode.Deep);
			Scribe_Values.Look(ref deliveryTick, "deliveryTick");
			Scribe_References.Look(ref map, "map");
			Scribe_Values.Look(ref deliveryPoint, "deliveryPoint");
			Scribe_Values.Look(ref insuranceChecked, "insuranceChecked");
			Scribe_Values.Look(ref insuranceCost, "insuranceCost");
			Scribe_Values.Look(ref orderCost, "orderCost");
		}
	}
}
