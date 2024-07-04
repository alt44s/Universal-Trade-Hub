using RimWorld;
using System;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Universal_Trade_Hub
{
	public class UTH_ModSettings : ModSettings
	{
		public bool enableInitAnim = true;

		public float taxRate = 0.1f;
		public float expressDeliveryBaseCost = 50f;
		public float expressDeliveryMultiplierPerKg = 0.9f;
		public float expressDeliveryTimeReduction = 0.4f;
		public float insuranceBaseCost = 500f;
		public float insuranceMultiplier = 0.1f;
		public float deliveryTimePerKg = 0.05f;
		public float deliveryTimeReductionForStuff = 0.05f;

		public float priceReductionMultiplier = 0.6f;
		public float wealthMultiplier = 0.1f;
		public float baseSubscriptionPrice = 1000;

		public float attackChance = 0.05f;

		public override void ExposeData()
		{
			Scribe_Values.Look(ref enableInitAnim, "enableInitAnim", true);
			Scribe_Values.Look(ref taxRate, "taxRate", 0.1f);
			Scribe_Values.Look(ref expressDeliveryBaseCost, "expressDeliveryBaseCost", 50f);
			Scribe_Values.Look(ref expressDeliveryMultiplierPerKg, "expressDeliveryMultiplierPerKg", 0.9f);
			Scribe_Values.Look(ref expressDeliveryTimeReduction, "expressDeliveryTimeReduction", 0.4f);
			Scribe_Values.Look(ref insuranceBaseCost, "insuranceBaseCost", 500f);
			Scribe_Values.Look(ref insuranceMultiplier, "insuranceMultiplier", 0.1f);
			Scribe_Values.Look(ref deliveryTimePerKg, "deliveryTimePerKg", 0.05f);
			Scribe_Values.Look(ref deliveryTimeReductionForStuff, "deliveryTimeReductionForStuff", 0.05f);

			Scribe_Values.Look(ref priceReductionMultiplier, "priceReductionMultiplier", 0.6f);
			Scribe_Values.Look(ref wealthMultiplier, "wealthMultiplier", 0.1f);
			Scribe_Values.Look(ref baseSubscriptionPrice, "baseSubscriptionPrice", 1000);

			Scribe_Values.Look(ref attackChance, "attackChance", 0.05f);

			base.ExposeData();
		}
	}

	public class UTH_Mod : Mod
	{
		public static UTH_ModSettings settings;
		private Vector2 scrollPosition = Vector2.zero;

		private enum Tab
		{
			Orders,
			Credits,
		}

		private Tab currentTab = Tab.Orders;

		public UTH_Mod(ModContentPack content) : base(content)
		{
			settings = GetSettings<UTH_ModSettings>();
		}

		public override string SettingsCategory() => "Universal Trade Hub";

		public override void DoSettingsWindowContents(Rect inRect)
		{
			var listingStandard = new Listing_Standard();
			listingStandard.Begin(inRect);

			Text.Font = GameFont.Small;

			DrawTabs(listingStandard);

			listingStandard.Gap(50f);

			Rect settingsAreaRect = new Rect(inRect.x, listingStandard.CurHeight, inRect.width, inRect.height - listingStandard.CurHeight);

			DrawSettingsBackground(settingsAreaRect);

			settingsAreaRect = settingsAreaRect.ContractedBy(20f);

			switch (currentTab)
			{
				case Tab.Orders:
					DrawOrder_Settings(listingStandard, settingsAreaRect);
					break;

				case Tab.Credits:
					DrawCredits(listingStandard, settingsAreaRect);
					break;
			}

			listingStandard.Gap(20f);

			listingStandard.End();

			DrawResetButton(inRect);
		}

		private void DrawSettingsBackground(Rect rect)
		{
			GUI.color = new Color(0.07f, 0.07f, 0.07f);
			Widgets.DrawBoxSolid(rect, GUI.color);
			GUI.color = Color.white;

			Widgets.DrawBox(rect, 2);
		}

		private void DrawTabs(Listing_Standard listingStandard)
		{
			float tabWidth = listingStandard.ColumnWidth / Enum.GetValues(typeof(Tab)).Length;
			Rect tabsRect = new Rect(listingStandard.GetRect(5f).x, listingStandard.CurHeight, tabWidth, 30f);

			foreach (Tab tab in Enum.GetValues(typeof(Tab)))
			{
				Rect tabRect = new Rect(tabsRect.x, tabsRect.y, Mathf.Min(tabWidth, listingStandard.ColumnWidth / Enum.GetValues(typeof(Tab)).Length), tabsRect.height);

				bool mouseOver = Mouse.IsOver(tabRect);
				bool isSelected = (tab == currentTab);

				GUI.color = isSelected ? new Color(0.1f, 0.1f, 0.1f) : (mouseOver ? new Color(0.15f, 0.15f, 0.15f) : new Color(0.05f, 0.05f, 0.05f));
				Widgets.DrawBoxSolid(tabRect, GUI.color);
				GUI.color = Color.white;

				if (Widgets.ButtonInvisible(tabRect))
				{
					currentTab = tab;
					SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
				}

				if (isSelected)
				{
					Widgets.DrawHighlightSelected(tabRect);
				}

				Text.Anchor = TextAnchor.MiddleCenter;
				Widgets.Label(tabRect, tab.ToString().Replace("_", " "));
				Text.Anchor = TextAnchor.UpperLeft;

				Widgets.DrawBox(tabRect, 2);

				tabsRect.x += Mathf.Min(tabWidth, listingStandard.ColumnWidth / Enum.GetValues(typeof(Tab)).Length);
			}
		}

		private void DrawOrder_Settings(Listing_Standard listingStandard, Rect settingsAreaRect)
		{
			listingStandard.Begin(settingsAreaRect);

			Text.Anchor = TextAnchor.MiddleCenter;
			Text.Font = GameFont.Medium;
			listingStandard.Label("UTH_OrderSettings".Translate());
			Text.Anchor = TextAnchor.UpperLeft;
			Text.Font = GameFont.Small;

			float viewWidth = settingsAreaRect.width - 30f;
			float viewHeight = settingsAreaRect.height + 100f;

			Rect outRect = new Rect((settingsAreaRect.width - viewWidth) / 2f, settingsAreaRect.yMin - 30f, viewWidth + 30f, settingsAreaRect.height / 2 + 100f);
			Rect viewRect = new Rect(outRect.x, outRect.y, viewWidth, viewHeight);
			Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

			Listing_Standard innerListing = new Listing_Standard();
			innerListing.Begin(viewRect);

			innerListing.Gap(15f);

			innerListing.CheckboxLabeled("UTH_EnableInitAnim".Translate(), ref settings.enableInitAnim);

			innerListing.Gap(25f);

			Rect sliderRect1 = innerListing.GetRect(22f);
			settings.taxRate = Widgets.HorizontalSlider(sliderRect1, settings.taxRate, 0f, 1.5f, true, "UTH_TaxRateLabel".Translate() + ": " + settings.taxRate.ToString("P0"), "0%", "150%");
			TooltipHandler.TipRegion(sliderRect1, "UTH_TaxRateTooltip".Translate());

			innerListing.Gap(25f);

			Rect sliderRect2 = innerListing.GetRect(22f);
			settings.expressDeliveryBaseCost = Mathf.RoundToInt(Widgets.HorizontalSlider(sliderRect2, settings.expressDeliveryBaseCost, 0f, 1500f, true, "UTH_ExpressDeliveryBaseCostLabel".Translate() + ": " + settings.expressDeliveryBaseCost.ToString(), "0", "1500"));
			TooltipHandler.TipRegion(sliderRect2, "UTH_ExpressDeliveryBaseCostTooltip".Translate());

			innerListing.Gap(25f);

			Rect sliderRect3 = innerListing.GetRect(22f);
			settings.expressDeliveryMultiplierPerKg = Widgets.HorizontalSlider(sliderRect3, settings.expressDeliveryMultiplierPerKg, 0f, 1.5f, true, "UTH_ExpressDeliveryMultiplierPerKgLabel".Translate() + ": " + settings.expressDeliveryMultiplierPerKg.ToString("P0"), "0%", "150%");
			TooltipHandler.TipRegion(sliderRect3, "UTH_ExpressDeliveryMultiplierPerKgTooltip".Translate());

			innerListing.Gap(25f);

			Rect sliderRect4 = innerListing.GetRect(22f);
			settings.expressDeliveryTimeReduction = Widgets.HorizontalSlider(sliderRect4, settings.expressDeliveryTimeReduction, 0f, 1f, true, "UTH_ExpressDeliveryTimeReductionLabel".Translate() + ": " + settings.expressDeliveryTimeReduction.ToString("P0"), "0%", "100%");
			TooltipHandler.TipRegion(sliderRect4, "UTH_ExpressDeliveryTimeReductionTooltip".Translate());

			innerListing.Gap(25f);

			Rect sliderRect5 = innerListing.GetRect(22f);
			settings.insuranceBaseCost = Mathf.RoundToInt(Widgets.HorizontalSlider(sliderRect5, settings.insuranceBaseCost, 0f, 3000f, true, "UTH_InsuranceBaseCostLabel".Translate() + ": " + settings.insuranceBaseCost.ToString(), "0", "3000"));
			TooltipHandler.TipRegion(sliderRect5, "UTH_InsuranceBaseCostTooltip".Translate());

			innerListing.Gap(25f);

			Rect sliderRect6 = innerListing.GetRect(22f);
			settings.insuranceMultiplier = Widgets.HorizontalSlider(sliderRect6, settings.insuranceMultiplier, 0f, 1.5f, true, "UTH_InsuranceMultiplierLabel".Translate() + ": " + settings.insuranceMultiplier.ToString("P0"), "0%", "150%");
			TooltipHandler.TipRegion(sliderRect6, "UTH_InsuranceMultiplierTooltip".Translate());

			innerListing.Gap(25f);

			Rect sliderRect7 = innerListing.GetRect(22f);
			settings.deliveryTimePerKg = Widgets.HorizontalSlider(sliderRect7, settings.deliveryTimePerKg, 0f, 1.5f, true, "UTH_DeliveryTimePerKgLabel".Translate() + ": " + settings.deliveryTimePerKg.ToString("P0"), "0%", "150%");
			TooltipHandler.TipRegion(sliderRect7, "UTH_DeliveryTimePerKgTooltip".Translate());

			innerListing.Gap(25f);

			Rect sliderRect8 = innerListing.GetRect(22f);
			settings.deliveryTimeReductionForStuff = Widgets.HorizontalSlider(sliderRect8, settings.deliveryTimeReductionForStuff, 0f, 1.5f, true, "UTH_DeliveryTimeReductionForStuffLabel".Translate() + ": " + settings.deliveryTimeReductionForStuff.ToString("P0"), "0%", "150%");
			TooltipHandler.TipRegion(sliderRect8, "UTH_DeliveryTimeReductionForStuffTooltip".Translate());

			innerListing.Gap(25f);

			Rect sliderRect9 = innerListing.GetRect(22f);
			settings.priceReductionMultiplier = Widgets.HorizontalSlider(sliderRect9, settings.priceReductionMultiplier, 0f, 1.5f, true, "UTH_PriceReductionMultiplierLabel".Translate() + ": " + settings.priceReductionMultiplier.ToString("P0"), "0%", "150%");
			TooltipHandler.TipRegion(sliderRect9, "UTH_PriceReductionMultiplierTooltip".Translate());

			innerListing.Gap(25f);

			Rect sliderRect10 = innerListing.GetRect(22f);
			settings.wealthMultiplier = Widgets.HorizontalSlider(sliderRect10, settings.wealthMultiplier, 0f, 1.5f, true, "UTH_WealthMultiplierLabel".Translate() + ": " + settings.wealthMultiplier.ToString("P0"), "0%", "150%");
			TooltipHandler.TipRegion(sliderRect10, "UTH_WealthMultiplierTooltip".Translate());

			innerListing.Gap(25f);

			Rect sliderRect11 = innerListing.GetRect(22f);
			settings.baseSubscriptionPrice = Mathf.RoundToInt(Widgets.HorizontalSlider(sliderRect11, settings.baseSubscriptionPrice, 0f, 10000f, true, "UTH_BaseSubscriptionPriceLabel".Translate() + ": " + settings.baseSubscriptionPrice.ToString(), "0", "10000"));
			TooltipHandler.TipRegion(sliderRect11, "UTH_BaseSubscriptionPriceTooltip".Translate());

			innerListing.Gap(25f);

			Rect sliderRect12 = innerListing.GetRect(22f);
			settings.attackChance = Widgets.HorizontalSlider(sliderRect12, settings.attackChance, 0f, 1.5f, true, "UTH_attackChanceLabel".Translate() + ": " + settings.attackChance.ToString("P0"), "0%", "150%");
			TooltipHandler.TipRegion(sliderRect12, "UTH_attackChanceTooltip".Translate());

			innerListing.Gap(25f);

			innerListing.End();
			Widgets.EndScrollView();

			listingStandard.End();
		}

		private void DrawCredits(Listing_Standard listingStandard, Rect settingsAreaRect)
		{
			listingStandard.Begin(settingsAreaRect);

			listingStandard.Gap(40);

			Text.Font = GameFont.Medium;

			Text.Anchor = TextAnchor.MiddleCenter;
			listingStandard.Label("UTH_CreditsTranslation".Translate());

			listingStandard.Gap(20);

			listingStandard.Label("UTH_CreditsTranslation2".Translate());

			Text.Font = GameFont.Small;

			listingStandard.End();
		}

		private void DrawResetButton(Rect inRect)
		{
			float buttonWidth = 192f;
			float buttonHeight = 35f;

			float buttonX = inRect.x + (inRect.width - buttonWidth) / 2f;
			float buttonY = inRect.height - buttonHeight;

			Rect buttonRect = new Rect(buttonX, buttonY - 5, buttonWidth, buttonHeight);
			if (UTH_UIUtility.DrawCustomButton(buttonRect, "UTH_Reset".Translate(), null))
			{
				ResetToDefaults();
			}
		}

		private void ResetToDefaults()
		{
			settings.enableInitAnim = true;

			settings.taxRate = 0.1f;
			settings.expressDeliveryBaseCost = 50f;
			settings.expressDeliveryMultiplierPerKg = 0.9f;
			settings.expressDeliveryTimeReduction = 0.4f;
			settings.insuranceBaseCost = 500f;
			settings.insuranceMultiplier = 0.1f;
			settings.deliveryTimePerKg = 0.05f;
			settings.deliveryTimeReductionForStuff = 0.05f;

			settings.priceReductionMultiplier = 0.6f;
			settings.wealthMultiplier = 0.1f;
			settings.baseSubscriptionPrice = 1000;

			settings.attackChance = 0.05f;
		}
	}
}