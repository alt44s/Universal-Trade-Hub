using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Universal_Trade_Hub
{
	[StaticConstructorOnStartup]
	public static class UTH_UIData
	{
		public static readonly SoundDef click1;
		public static readonly SoundDef hover1;

		public static readonly Texture2D UTH_logo1;

		public static readonly Texture2D[] mainButtonIcons;
		public static readonly Texture2D[] categoryButtonIcons;

		public static readonly SoundDef systemInit1;
		public static readonly SoundDef systemShutdown1;
		public static readonly SoundDef systemAlert1;
		public static readonly SoundDef systemComplete1;
		public static readonly SoundDef systemErr1;
		public static readonly SoundDef systemSub1;

		static UTH_UIData()
		{
			UTH_logo1 = ContentFinder<Texture2D>.Get("Icons/UTH_logo");

			click1 = SoundDef.Named("ButtonClick1");
			hover1 = SoundDef.Named("ButtonHover1");

			systemInit1 = SoundDef.Named("SystemInit1");
			systemShutdown1 = SoundDef.Named("SystemShutdown1");
			systemAlert1 = SoundDef.Named("SystemAlert1");
			systemComplete1 = SoundDef.Named("SystemComplete1");
			systemErr1 = SoundDef.Named("SystemErr1");
			systemSub1 = SoundDef.Named("SystemSub1");

			//put in order of labels
			mainButtonIcons = new Texture2D[]
			{
				ContentFinder<Texture2D>.Get("Icons/OrderButton1"),
				ContentFinder<Texture2D>.Get("Icons/ActiveOrderButton1"),
				ContentFinder<Texture2D>.Get("Icons/ExitButton1"),
				ContentFinder<Texture2D>.Get("Icons/SubscriptionButton1")
			};

			categoryButtonIcons = new Texture2D[]
			{
				ContentFinder<Texture2D>.Get("Icons/WeaponsTab1"),
				ContentFinder<Texture2D>.Get("Icons/ApparelTab1"),
				ContentFinder<Texture2D>.Get("Icons/FoodTab1"),
				ContentFinder<Texture2D>.Get("Icons/MedicineTab1"),
				ContentFinder<Texture2D>.Get("Icons/MaterialsTab1"),
				ContentFinder<Texture2D>.Get("Icons/BuildingsTab1"),
				ContentFinder<Texture2D>.Get("Icons/ExoticTab1"),
				ContentFinder<Texture2D>.Get("Icons/OtherTab1"),
			};
		}
	}

	public static class UTH_UIUtility
	{
		public static readonly Color bgColor = new Color(0.05f, 0.05f, 0.05f);
		public static readonly Color itemsMenuBGColor = new Color(0.08f, 0.08f, 0.08f);
		public static readonly Color miscBoxColor = new Color(0.06f, 0.06f, 0.06f);

		public static readonly Color buttonColor = new Color(0.2f, 0.2f, 0.2f);
		public static readonly Color highlightedButtonColor = new Color(0.1f, 0.1f, 0.1f);

		public static readonly float buttonWidth = 160f;
		public static readonly float buttonHeight = 40f;

		private static Dictionary<Rect, bool> hoverStates = new Dictionary<Rect, bool>();

		public static bool DrawCustomButton(Rect rect, string label, Texture2D icon)
		{
			bool isHovered = Mouse.IsOver(rect);
			if (!hoverStates.ContainsKey(rect))
			{
				hoverStates[rect] = false;
			}
			bool wasHovered = hoverStates[rect];

			bool clicked = Widgets.ButtonInvisible(rect, false);

			Widgets.DrawBoxSolid(rect, isHovered ? buttonColor : highlightedButtonColor);

			float iconSize = 24f;
			float padding = 10f;
			float iconOffset = 0f;

			if (icon != null)
			{
				Rect iconRect = new Rect(rect.x + padding, rect.y + (rect.height - iconSize) / 2f, iconSize, iconSize);
				GUI.color = Color.white;
				Widgets.DrawTextureFitted(iconRect, icon, 1f);
				iconOffset = iconSize + padding;
			}

			GUI.color = Color.white;
			Text.Anchor = TextAnchor.MiddleCenter;

			Rect labelRect = new Rect(rect.x + iconOffset, rect.y, rect.width - iconOffset, rect.height);
			Widgets.Label(labelRect, $"<size=14>{label}</size>");

			Text.Anchor = TextAnchor.UpperLeft;

			if (isHovered && !wasHovered)
			{
				UTH_UIData.hover1.PlayOneShotOnCamera();
			}

			if (clicked)
			{
				UTH_UIData.click1.PlayOneShotOnCamera();
			}

			hoverStates[rect] = isHovered;

			return clicked;
		}

		public static void HandleButtonClick(ref int count, int increment, bool isRightArrow)
		{
			Event current = Event.current;
			if (current.button == 1)
			{
				count = isRightArrow ? 9999 : 0;
			}
			else
			{
				int multiplier = 1;
				if (current.control)
				{
					multiplier = 10;
				}
				else if (current.shift)
				{
					multiplier = 100;
				}

				count += increment * multiplier;
			}

			if (count < 0)
			{
				count = 0;
			}
			else if (count > 9999)
			{
				count = 9999;
			}
		}

		public static void RemoveInvalidItems(Dictionary<ThingDef, int> orderedItems)
		{
			var invalidItems = orderedItems.Where(kv => kv.Value <= 0).Select(kv => kv.Key).ToList();
			foreach (var item in invalidItems)
			{
				orderedItems.Remove(item);
			}
		}

		public static int CalculateTotalAvailableSilver(Map map)
		{
			int totalSilver = 0;
			if (map != null)
			{
				foreach (Thing silver in TradeUtility.AllLaunchableThingsForTrade(map))
				{
					if (silver.def == ThingDefOf.Silver)
					{
						totalSilver += silver.stackCount;
					}
				}
			}
			else
			{
				Log.Error("[UTH] Tried to get total available silver from a null map.");
			}

			return totalSilver;
		}
	}

	public class UTH_TradingMenu : Window
	{
		private readonly Pawn interactingPawn;
		private readonly Building_UniversalTradeConsole console;

		private readonly string[] buttonLabels = { "UTH_Order".Translate(), "UTH_ActiveOrders".Translate(), "UTH_Shutdown".Translate(), "UTH_Subscription".Translate() };

		private bool showInitAnimation = UTH_Mod.settings.enableInitAnim;
		private bool initSoundPlayed = false;
		private float initAnimationDelay = 0.1f;
		private float okDisplayDelay = 0.2f;
		private float initDisplayTime = 1f;
		private float currentAnimationTime = 0f;
		private int currentStep = 0;

		private readonly List<string> initSteps = new List<string>
		{
			"UTH_LoadingSystemConfig".Translate(),
			"UTH_AttuningFrequency".Translate(),
			"UTH_ConnectingTradeNetwork".Translate(),
			"UTH_LoadingMarketData".Translate(),
			"UTH_SettingUpInterface".Translate(),
			"UTH_Initializing".Translate()
		};

		private readonly List<string> tradingMottos = new List<string>
		{
			"UTH_TradingMotto1".Translate(),
			"UTH_TradingMotto2".Translate(),
			"UTH_TradingMotto3".Translate(),
			"UTH_TradingMotto4".Translate(),
			"UTH_TradingMotto5".Translate(),
			"UTH_TradingMotto6".Translate(),
			"UTH_TradingMotto7".Translate(),
			"UTH_TradingMotto8".Translate(),
			"UTH_TradingMotto9".Translate(),
			"UTH_TradingMotto10".Translate(),
			"UTH_TradingMotto11".Translate(),
			"UTH_TradingMotto12".Translate(),
			"UTH_TradingMotto13".Translate(),
			"UTH_TradingMotto14".Translate(),
			"UTH_TradingMotto15".Translate(),
			"UTH_TradingMotto16".Translate(),
			"UTH_TradingMotto17".Translate(),
			"UTH_TradingMotto18".Translate(),
			"UTH_TradingMotto19".Translate(),
			"UTH_TradingMotto20".Translate()
		};

		public string randomMotto = "";

		public override void PreClose()
		{
			base.PreClose();
			if (UTH_OrderCategoriesMenu.selectedThingDefs != null && UTH_OrderCategoriesMenu.selectedThingDefs.Any())
			{
				UTH_OrderCategoriesMenu.selectedThingDefs.Clear();
			}
		}

		public UTH_TradingMenu(Pawn interactingPawn, Building_UniversalTradeConsole console)
		{
			this.interactingPawn = interactingPawn;
			this.console = console;
			closeOnAccept = true;
			closeOnCancel = true;
			doCloseX = false;
			absorbInputAroundWindow = true;
			forcePause = true;

			randomMotto = tradingMottos[UnityEngine.Random.Range(0, tradingMottos.Count)];
		}

		public override Vector2 InitialSize => new Vector2(700f, 500f);

		public override void DoWindowContents(Rect inRect)
		{
			Widgets.DrawBoxSolid(inRect, UTH_UIUtility.bgColor);
			GUI.color = Color.white;

			if (showInitAnimation)
			{
				DrawInitAnimation();
				return;
			}

			Listing_Standard listing = new Listing_Standard();
			listing.Begin(inRect.ContractedBy(10));

			float iconSize = 164f;

			Rect iconRect = new Rect(inRect.center.x - (iconSize / 2f), inRect.y + 10f, iconSize, iconSize);
			if (UTH_UIData.UTH_logo1 != null)
			{
				Widgets.DrawTextureFitted(iconRect, UTH_UIData.UTH_logo1, 1f);
			}

			Text.Font = GameFont.Small;
			Text.CurFontStyle.fontStyle = FontStyle.Italic;
			Widgets.Label(new Rect(inRect.center.x - Text.CalcSize(randomMotto).x / 2, iconRect.yMax - 15f, inRect.width, 30f), randomMotto);
			Text.CurFontStyle.fontStyle = FontStyle.Normal;

			float gapBetweenButtons = 15f;
			float startX = inRect.center.x - ((UTH_UIUtility.buttonWidth * 2 + gapBetweenButtons) / 2f);

			float totalButtonHeight = (UTH_UIUtility.buttonHeight * 2) + gapBetweenButtons;
			float startY = iconRect.yMax - 50 + (inRect.height - (iconRect.yMax) - totalButtonHeight) / 2f;

			int buttonsPerRow = 2;
			int rows = 2;
			int buttonIndex = 0;

			for (int row = 0; row < rows; row++)
			{
				for (int col = 0; col < buttonsPerRow; col++)
				{
					if (buttonIndex >= buttonLabels.Length)
						break;

					Rect buttonRect = new Rect(startX + col * (UTH_UIUtility.buttonWidth + gapBetweenButtons), startY + row * (UTH_UIUtility.buttonHeight + gapBetweenButtons), UTH_UIUtility.buttonWidth, UTH_UIUtility.buttonHeight);

					if (UTH_UIUtility.DrawCustomButton(buttonRect, buttonLabels[buttonIndex], UTH_UIData.mainButtonIcons[buttonIndex]))
					{
						HandleButtonClick(buttonIndex);
					}

					buttonIndex++;
				}
			}

			listing.End();

			GUI.color = Color.white;
		}

		private void DrawInitAnimation()
		{
			Rect animationRect = new Rect(10f, 10f, 300f, 150f);
			Text.Font = GameFont.Tiny;

			if (!initSoundPlayed)
			{
				UTH_UIData.click1.PlayOneShotOnCamera();
				UTH_UIData.systemInit1.PlayOneShotOnCamera();
				initSoundPlayed = true;
			}

			int maxStepsToShow = Mathf.Min(currentStep / 2, initSteps.Count);
			for (int i = 0; i < maxStepsToShow; i++)
			{
				string stepText = initSteps[i];
				if (i * 2 < currentStep)
				{
					stepText += " OK";
				}
				Widgets.Label(new Rect(animationRect.x, animationRect.y + i * 20, animationRect.width, 20), stepText);
			}

			Text.Font = GameFont.Small;

			currentAnimationTime -= Time.deltaTime;
			if (currentAnimationTime <= 0f && currentStep / 2 < initSteps.Count)
			{
				currentStep++;
				currentAnimationTime = currentStep % 2 == 0 ? initAnimationDelay : okDisplayDelay;
			}
			if (currentStep / 2 >= initSteps.Count)
			{
				initDisplayTime -= Time.deltaTime;
				if (initDisplayTime <= 0f)
				{
					showInitAnimation = false;
				}
			}
		}

		private void HandleButtonClick(int buttonIndex)
		{
			switch (buttonIndex)
			{
				case 0:
					Find.WindowStack.Add(new UTH_OrderCategoriesMenu(interactingPawn, console));
					break;

				case 1:
					Find.WindowStack.Add(new UTH_ActiveOrdersMenu());
					break;

				case 2:
					UTH_UIData.systemShutdown1.PlayOneShotOnCamera();
					this.Close();
					break;

				case 3:
					Find.WindowStack.Add(new UTH_SubscriptionMenu(console));
					break;

				default:
					break;
			}
		}
	}

	public class UTH_SubscriptionMenu : Window
	{
		private readonly Building_UniversalTradeConsole console;

		public bool hasActiveSubscription;
		private float daysRemaining;
		private readonly float priceReductionMultiplier = UTH_Mod.settings.priceReductionMultiplier;
		public float PriceReductionMultiplier => priceReductionMultiplier;
		private float multToPercent;
		private readonly float wealthMultiplier = UTH_Mod.settings.wealthMultiplier;

		private readonly float baseSubscriptionPrice = UTH_Mod.settings.baseSubscriptionPrice;
		private readonly float colonyWealth = WealthUtility.PlayerWealth;

		private List<(string label, int days, float price, float discount)> subscriptionPlans;
		private List<(string label, int days, float price, float discount)> updatedSubscriptionPlans = new List<(string label, int days, float price, float discount)>();

		private UTH_WorldComponent_SubscriptionTracker subscriptionTracker = Find.World.GetComponent<UTH_WorldComponent_SubscriptionTracker>();

		private float totalAvailableSilver;

		public UTH_SubscriptionMenu(Building_UniversalTradeConsole console)
		{
			this.console = console;
			closeOnAccept = false;
			closeOnCancel = true;
			doCloseX = false;
			absorbInputAroundWindow = true;
			forcePause = true;

			subscriptionPlans = new List<(string label, int days, float price, float discount)>
			{
				("UTH_5Days".Translate(), 5, baseSubscriptionPrice, 0.9f),
				("UTH_15Days".Translate(), 15, baseSubscriptionPrice * 3, 0.8f),
				("UTH_30Days".Translate(), 30, baseSubscriptionPrice * 3 * 2, 0.6f)
			};

			AdjustBasedOnColonyWealth(colonyWealth, subscriptionPlans);

			hasActiveSubscription = subscriptionTracker.hasSubscription;
			daysRemaining = Mathf.FloorToInt(GenDate.TicksToDays(subscriptionTracker.subscriptionTick));

			totalAvailableSilver = UTH_UIUtility.CalculateTotalAvailableSilver(console.Map);

			multToPercent = Mathf.Round((1 - priceReductionMultiplier) * 100);
		}

		public override Vector2 InitialSize => new Vector2(900f, 600f);

		public override void DoWindowContents(Rect inRect)
		{
			Widgets.DrawBoxSolid(inRect, UTH_UIUtility.bgColor);
			GUI.color = Color.white;

			float padding = 40f;

			float headerBoxHeight = 150f;
			float headerBoxWidth = inRect.width - padding * 5;
			float startX = inRect.center.x - headerBoxWidth / 2;
			float startY = inRect.yMin + padding;

			Rect subBoxRect = new Rect(startX, startY, headerBoxWidth, headerBoxHeight);
			Widgets.DrawBoxSolid(subBoxRect, UTH_UIUtility.itemsMenuBGColor);

			Text.Anchor = TextAnchor.MiddleCenter;

			Rect subMenuRect = new Rect(subBoxRect.xMin, subBoxRect.yMin + 10f, subBoxRect.width, 50f);
			Widgets.Label(subMenuRect, $"<size=20>{"UTH_ManageSubs".Translate().Resolve()}</size>");

			Rect descriptionRect = new Rect(subMenuRect.x, subMenuRect.yMax + 10f, subMenuRect.width, subMenuRect.height);
			Widgets.Label(descriptionRect, $"<size=16>{"UTH_SubDesc".Translate(multToPercent)}</size>");

			Text.Anchor = TextAnchor.MiddleLeft;

			float gapBetweenButtons = 60f;
			startX = inRect.center.x - ((UTH_UIUtility.buttonWidth * 3 + gapBetweenButtons * (subscriptionPlans.Count - 1)) / 2f);

			int buttonsPerRow = 3;
			int rows = 1;
			int buttonIndex = 0;

			float totalButtonHeight = rows * UTH_UIUtility.buttonHeight + (rows - 1) * gapBetweenButtons;
			float remainingHeight = inRect.height - totalButtonHeight;
			startY = inRect.y + remainingHeight / 2;

			float buttonX = inRect.center.x - UTH_UIUtility.buttonWidth / 2;
			float buttonY = inRect.height - UTH_UIUtility.buttonHeight * rows - 60f;

			float buttonXOffset = (UTH_UIUtility.buttonWidth + gapBetweenButtons) * (buttonsPerRow - 1) / 2;

			float buttonBackgroundWidth = 0;

			if (hasActiveSubscription)
			{
				buttonXOffset /= 2;
				float boxWidth = 500f;
				float boxHeight = 70f;

				buttonY -= 30f;

				Rect outerBoxRect = new Rect(inRect.center.x - boxWidth / 2, inRect.center.y - boxHeight / 2, boxWidth, boxHeight);
				Widgets.DrawBoxSolid(outerBoxRect, UTH_UIUtility.itemsMenuBGColor);

				Text.Anchor = TextAnchor.MiddleCenter;

				Rect activeSubRect = new Rect(outerBoxRect.xMin, outerBoxRect.yMin, outerBoxRect.width, outerBoxRect.height);
				Widgets.Label(activeSubRect, $"<size=22>{"UTH_ActiveSub".Translate($"<color=cyan>{daysRemaining}</color>")}</size>");

				Text.Anchor = TextAnchor.MiddleLeft;

				Rect cancelButtonRect = new Rect(buttonX + buttonXOffset, buttonY, UTH_UIUtility.buttonWidth, UTH_UIUtility.buttonHeight);
				if (UTH_UIUtility.DrawCustomButton(cancelButtonRect, "UTH_CancelSub".Translate(), null))
				{
					subscriptionTracker.hasSubscription = false;
					hasActiveSubscription = false;

					UTH_UIData.systemAlert1.PlayOneShotOnCamera();
					Messages.Message("UTH_SubCancelled".Translate(), MessageTypeDefOf.SilentInput, historical: false);
				}
			}
			else
			{
				for (int row = 0; row < rows; row++)
				{
					for (int col = 0; col < buttonsPerRow; col++)
					{
						if (buttonIndex >= subscriptionPlans.Count)
							break;

						float buttonPadding = 60f;

						Rect buttonRect = new Rect(startX + col * (UTH_UIUtility.buttonWidth + gapBetweenButtons), (startY - buttonPadding) + row * (UTH_UIUtility.buttonHeight + gapBetweenButtons) + 20f, UTH_UIUtility.buttonWidth, UTH_UIUtility.buttonHeight);

						float bbWidth = 40;
						float bbHeight = 200;

						Rect buttonBackgroundRect = new Rect(buttonRect.x - bbWidth / 2, buttonRect.y - (bbHeight / 2) + buttonPadding + 20f, buttonRect.width + bbWidth, buttonRect.height + bbHeight);
						Widgets.DrawBoxSolid(buttonBackgroundRect, UTH_UIUtility.itemsMenuBGColor);

						buttonBackgroundWidth = buttonBackgroundRect.width;

						if (UTH_UIUtility.DrawCustomButton(buttonRect, updatedSubscriptionPlans[buttonIndex].label, null))
						{
							if (totalAvailableSilver >= updatedSubscriptionPlans[buttonIndex].price)
							{
								ProcessPurchase(updatedSubscriptionPlans[buttonIndex].price, updatedSubscriptionPlans[buttonIndex].days);
								hasActiveSubscription = true;

								daysRemaining = Mathf.FloorToInt(GenDate.TicksToDays(subscriptionTracker.subscriptionTick));

								totalAvailableSilver = UTH_UIUtility.CalculateTotalAvailableSilver(console.Map);

								UTH_UIData.systemSub1.PlayOneShotOnCamera();
								Messages.Message("UTH_SubPurchased".Translate(daysRemaining), MessageTypeDefOf.SilentInput, historical: false);
							}
							else
							{
								UTH_UIData.systemErr1.PlayOneShotOnCamera();
								Messages.Message("UTH_NotEnoughSilver".Translate(), MessageTypeDefOf.SilentInput, historical: false);
							}
						}

						float discount = Mathf.Round((1 - updatedSubscriptionPlans[buttonIndex].discount) * 100);
						string subscriptionPrice = $"<size=20>{updatedSubscriptionPlans[buttonIndex].price.ToStringMoney()}</size>";
						string discountString = $"<size=20>{"UTH_PercentOff".Translate(discount).Resolve()}</size>";
						if (discount <= 0)
						{
							discountString = "\r\r\r";
						}

						Text.Anchor = TextAnchor.MiddleCenter;

						Rect priceRect = new Rect(buttonBackgroundRect.xMin, (buttonBackgroundRect.center.y - buttonBackgroundRect.height / 2) + 20f, buttonBackgroundRect.width, buttonBackgroundRect.height);

						buttonPadding = 40f;

						Rect outerBoxRect = new Rect(priceRect.x + buttonPadding / 2, priceRect.y + buttonPadding + 20f, priceRect.width - buttonPadding, priceRect.height / 2 - (buttonPadding - 50f));
						Widgets.DrawBoxSolid(outerBoxRect, UTH_UIUtility.highlightedButtonColor);

						Widgets.Label(priceRect, $"{subscriptionPrice}\n\n\n{discountString}");

						Text.Anchor = TextAnchor.MiddleLeft;

						buttonIndex++;
					}
				}
			}

			if (!hasActiveSubscription)
			{
				float outerBoxWidth = buttonBackgroundWidth + 20f;
				float outerBoxHeight = UTH_UIUtility.buttonHeight + 15f;

				Rect outerBoxRect = new Rect(inRect.center.x - outerBoxWidth / 2, buttonY - 5f, outerBoxWidth, outerBoxHeight);
				Widgets.DrawBoxSolid(outerBoxRect, UTH_UIUtility.highlightedButtonColor);

				Text.Anchor = TextAnchor.MiddleCenter;

				Rect moneyRect = new Rect(outerBoxRect.xMin, outerBoxRect.yMin, outerBoxRect.width, descriptionRect.height);
				Widgets.Label(moneyRect, $"<size=14>{"UTH_TotalAvailableSilver".Translate()}{totalAvailableSilver.ToStringMoney()}</size>");

				Text.Anchor = TextAnchor.MiddleLeft;
			}

			Rect backButtonRect = new Rect(buttonX - buttonXOffset, buttonY, UTH_UIUtility.buttonWidth, UTH_UIUtility.buttonHeight);
			if (UTH_UIUtility.DrawCustomButton(backButtonRect, "UTH_Back".Translate(), null))
			{
				Close();
			}
		}

		private void ProcessPurchase(float price, float days)
		{
			int subscriptionTick = Find.TickManager.TicksGame + (int)(days * GenDate.TicksPerDay);

			TradeUtility.LaunchSilver(console.Map, (int)price);

			subscriptionTracker.subscriptionTick = subscriptionTick;
			subscriptionTracker.hasSubscription = true;
		}

		private void AdjustBasedOnColonyWealth(float colonyWealth, List<(string label, int days, float price, float discount)> subscriptionPlans)
		{
			foreach (var plan in subscriptionPlans)
			{
				float discount = plan.discount;

				float adjustedPrice = (plan.price + colonyWealth * Mathf.Pow(wealthMultiplier, 2f)) * discount;

				updatedSubscriptionPlans.Add((plan.label, plan.days, adjustedPrice, discount));
			}
		}
	}

	public class UTH_OrderCategoriesMenu : Window
	{
		private readonly Pawn interactingPawn;
		private readonly Building_UniversalTradeConsole console;

		private readonly string[] categoryLabels = { "UTH_Weapons".Translate(), "UTH_Apparel".Translate(), "UTH_Food".Translate(), "UTH_Medicine".Translate(), "UTH_Materials".Translate(), "UTH_Buildings".Translate(), "UTH_Exotic".Translate(), "UTH_Other".Translate() };

		public static Dictionary<ThingDef, int> selectedThingDefs = new Dictionary<ThingDef, int>();

		public UTH_OrderCategoriesMenu(Pawn interactingPawn, Building_UniversalTradeConsole console)
		{
			this.interactingPawn = interactingPawn;
			this.console = console;
			closeOnAccept = true;
			closeOnCancel = true;
			doCloseX = false;
			absorbInputAroundWindow = true;
			forcePause = true;
		}

		public override Vector2 InitialSize => new Vector2(700f, 500f);

		public override void DoWindowContents(Rect inRect)
		{
			Widgets.DrawBoxSolid(inRect, UTH_UIUtility.bgColor);
			GUI.color = Color.white;

			Listing_Standard listing = new Listing_Standard();
			listing.Begin(inRect.ContractedBy(10));

			float gapBetweenButtons = 15f;
			float startX = inRect.center.x - ((UTH_UIUtility.buttonWidth * 3 + gapBetweenButtons * 2) / 2f);
			float startY = inRect.y + 50f;

			int buttonsPerRow = 3;
			int rows = 3;
			int buttonIndex = 0;

			for (int row = 0; row < rows; row++)
			{
				for (int col = 0; col < buttonsPerRow; col++)
				{
					if (buttonIndex >= categoryLabels.Length)
						break;

					Rect buttonRect = new Rect(startX + col * (UTH_UIUtility.buttonWidth + gapBetweenButtons), startY + row * (UTH_UIUtility.buttonHeight + gapBetweenButtons), UTH_UIUtility.buttonWidth, UTH_UIUtility.buttonHeight);

					if (UTH_UIUtility.DrawCustomButton(buttonRect, categoryLabels[buttonIndex], UTH_UIData.categoryButtonIcons[buttonIndex]))
					{
						HandleCategoryButtonClick(buttonIndex);
					}

					buttonIndex++;
				}
			}

			float buttonX = inRect.center.x - UTH_UIUtility.buttonWidth / 2;
			float buttonY = inRect.height - UTH_UIUtility.buttonHeight * rows - 40f;

			float buttonXOffset = (UTH_UIUtility.buttonWidth + gapBetweenButtons) * (buttonsPerRow - 1) / 2;

			Rect backButtonRect = new Rect(buttonX - buttonXOffset, buttonY, UTH_UIUtility.buttonWidth, UTH_UIUtility.buttonHeight);
			if (UTH_UIUtility.DrawCustomButton(backButtonRect, "UTH_Back".Translate(), null))
			{
				Close();
			}

			Rect orderButtonRect = new Rect(buttonX + buttonXOffset, buttonY, UTH_UIUtility.buttonWidth, UTH_UIUtility.buttonHeight);
			if (UTH_UIUtility.DrawCustomButton(orderButtonRect, "UTH_PlaceOrder".Translate(), null))
			{
				Find.WindowStack.Add(new UTH_FinalOrderMenu(interactingPawn, console, selectedThingDefs));
			}

			listing.End();
		}

		private void HandleCategoryButtonClick(int categoryIndex)
		{
			Find.WindowStack.Add(new UTH_OrderMenu(categoryIndex));
		}
	}

	public class UTH_OrderMenu : Window
	{
		private readonly int categoryIndex;

		private Vector2 scrollPosition = Vector2.zero;
		private Vector2 selectedItemsScrollPosition = Vector2.zero;
		private List<ThingDef> items;
		private List<ThingDef> filteredItems;
		private readonly Dictionary<ThingDef, int> itemCounts;
		private readonly Dictionary<ThingDef, float> adjustedMarketValues;
		private string searchText = "";

		private readonly float wealthMultiplier = UTH_Mod.settings.wealthMultiplier;
		private readonly float colonyWealth = WealthUtility.PlayerWealth;

		private float totalPrice = 0f;
		private float totalTax = 0f;
		private float taxRate = UTH_Mod.settings.taxRate;

		private float specialMultiplier = UTH_Mod.settings.specialMultiplier;

		private const float ItemHeight = 40f;

		public UTH_OrderMenu(int categoryIndex)
		{
			this.categoryIndex = categoryIndex;

			closeOnAccept = true;
			closeOnCancel = true;
			doCloseX = false;
			absorbInputAroundWindow = true;
			forcePause = true;

			itemCounts = new Dictionary<ThingDef, int>();
			adjustedMarketValues = new Dictionary<ThingDef, float>();

			LoadItems();

			foreach (var item in items)
			{
				if (UTH_OrderCategoriesMenu.selectedThingDefs.ContainsKey(item))
				{
					itemCounts[item] = UTH_OrderCategoriesMenu.selectedThingDefs[item];
				}
				else
				{
					itemCounts[item] = 0;
				}

				if (!adjustedMarketValues.ContainsKey(item))
				{
					adjustedMarketValues[item] = item.BaseMarketValue;
				}

				if (item.tradeTags != null && (item.tradeTags.Contains("UtilitySpecial") || item.tradeTags.Contains("ExoticMisc") || item.tradeTags.Contains("ExoticBuilding")))
				{
					adjustedMarketValues[item] = item.BaseMarketValue * specialMultiplier;
				}
			}

			FilterItems();
		}

		public override Vector2 InitialSize => new Vector2(900f, 600f);

		public override void DoWindowContents(Rect inRect)
		{
			Widgets.DrawBoxSolid(inRect, UTH_UIUtility.bgColor);
			GUI.color = Color.white;

			float padding = 40f;

			float itemsWidth = inRect.width / 2f + 10f;
			float itemsHeight = inRect.height - 200f;
			float itemsStartX = inRect.x + padding;
			float itemsStartY = inRect.y + padding;

			Rect searchRect = new Rect(itemsStartX, itemsStartY, itemsWidth, 30f);
			string newSearchText = Widgets.TextField(searchRect, searchText);
			if (newSearchText != searchText)
			{
				searchText = newSearchText;
				scrollPosition = Vector2.zero;
				FilterItems();
			}

			Rect scrollViewRect = new Rect(itemsStartX, searchRect.yMax, itemsWidth, itemsHeight);
			Rect scrollContentRect = new Rect(0f, 0f, scrollViewRect.width - 20f, items.Count * ItemHeight);

			Widgets.DrawBoxSolid(scrollViewRect, UTH_UIUtility.itemsMenuBGColor);
			Widgets.BeginScrollView(scrollViewRect, ref scrollPosition, scrollContentRect);

			int firstVisibleIndex = Mathf.Max(0, Mathf.FloorToInt(scrollPosition.y / ItemHeight));
			int lastVisibleIndex = Mathf.Min(filteredItems.Count, Mathf.CeilToInt((scrollPosition.y + itemsHeight) / ItemHeight));

			float y = firstVisibleIndex * ItemHeight;
			for (int i = firstVisibleIndex; i < lastVisibleIndex; i++)
			{
				Rect itemRect = new Rect(0f, y, scrollContentRect.width, ItemHeight);
				DrawItemRow(itemRect, filteredItems[i]);
				y += ItemHeight;
			}

			Widgets.EndScrollView();

			//selected items

			float selectedItemsStartX = scrollViewRect.xMax + 20f;
			float selectedItemsWidth = inRect.width - selectedItemsStartX - padding;

			Rect selectedItemsRect = new Rect(selectedItemsStartX, searchRect.yMax, selectedItemsWidth, itemsHeight - 150f);
			Widgets.DrawBoxSolid(selectedItemsRect, UTH_UIUtility.itemsMenuBGColor);

			Rect miscBoxRect = new Rect(selectedItemsRect.xMin, selectedItemsRect.yMin - searchRect.height, selectedItemsRect.width, searchRect.height);
			Widgets.DrawBoxSolidWithOutline(miscBoxRect, UTH_UIUtility.miscBoxColor, Color.gray);

			string labelText = "UTH_SelectedItems".Translate();
			Vector2 labelSize = Text.CalcSize(labelText);
			float labelX = miscBoxRect.xMin + (miscBoxRect.width - labelSize.x) / 2;
			float labelY = miscBoxRect.yMin + (miscBoxRect.height - labelSize.y) / 2;
			Widgets.Label(new Rect(labelX, labelY, labelSize.x, labelSize.y), labelText);

			UTH_UIUtility.RemoveInvalidItems(itemCounts);

			var selectedItems = itemCounts.Where(kv => kv.Value > 0).ToList();
			Rect selectedItemsScrollContentRect = new Rect(0f, 0f, selectedItemsWidth - 20f, selectedItems.Count * 40f);

			Widgets.BeginScrollView(selectedItemsRect, ref selectedItemsScrollPosition, selectedItemsScrollContentRect);

			float selectedItemsY = 5f;
			foreach (var pair in selectedItems)
			{
				Rect selectedItemRect = new Rect(0f, selectedItemsY, selectedItemsScrollContentRect.width, 40f);
				DrawSelectedItemRow(selectedItemRect, pair.Key);
				selectedItemsY += 40f;
			}

			Widgets.EndScrollView();

			totalPrice = itemCounts.Sum(ic => adjustedMarketValues[ic.Key] * ic.Value);
			totalTax = (totalPrice + colonyWealth * Mathf.Pow(wealthMultiplier, 2f)) * taxRate;
			if (totalPrice == 0)
			{
				totalTax = 0;
			}
			float finalPrice = totalPrice + totalTax;

			float priceBoxHeight = 140f;
			Rect boxRect = new Rect(selectedItemsStartX, scrollViewRect.yMax - priceBoxHeight, selectedItemsWidth, priceBoxHeight);
			Widgets.DrawBoxSolid(boxRect, UTH_UIUtility.itemsMenuBGColor);

			float labelWidth = boxRect.width - 20f;
			float labelHeight = 40f;

			float startX = boxRect.x + (boxRect.width - labelWidth) / 2;
			float startY = boxRect.y + (boxRect.height - labelHeight * 3) / 2;

			Text.Anchor = TextAnchor.MiddleCenter;

			Rect totalPriceLabelRect = new Rect(startX, startY, labelWidth, labelHeight);
			Widgets.Label(totalPriceLabelRect, $"<size=14>{"UTH_TotalPriceMinusTax".Translate().Resolve()}{totalPrice.ToStringMoney()}</size>");

			Rect totalTaxLabelRect = new Rect(startX, totalPriceLabelRect.yMax, labelWidth, labelHeight);
			Widgets.Label(totalTaxLabelRect, $"<size=14>{"UTH_TotalTax".Translate().Resolve()}{totalTax.ToStringMoney()}</size>");

			Rect finalPriceLabelRect = new Rect(startX, totalTaxLabelRect.yMax, labelWidth, labelHeight);
			Widgets.Label(finalPriceLabelRect, $"<size=14>{"UTH_FinalPriceWithTax".Translate().Resolve()}{finalPrice.ToStringMoney()}</size>");

			Text.Anchor = TextAnchor.MiddleLeft;

			float buttonX = inRect.center.x - UTH_UIUtility.buttonWidth / 2;
			float buttonY = scrollViewRect.yMax + 30f;
			float buttonXOffset = 200f;

			Rect backButtonRect = new Rect(buttonX - buttonXOffset, buttonY, UTH_UIUtility.buttonWidth, UTH_UIUtility.buttonHeight);
			if (UTH_UIUtility.DrawCustomButton(backButtonRect, "UTH_Back".Translate(), null))
			{
				Close();
			}

			Rect resetButtonRect = new Rect(buttonX, buttonY, UTH_UIUtility.buttonWidth, UTH_UIUtility.buttonHeight);
			if (UTH_UIUtility.DrawCustomButton(resetButtonRect, "UTH_Reset".Translate(), null))
			{
				itemCounts.Clear();
			}

			Rect addOrderButtonRect = new Rect(buttonX + buttonXOffset, buttonY, UTH_UIUtility.buttonWidth, UTH_UIUtility.buttonHeight);
			if (UTH_UIUtility.DrawCustomButton(addOrderButtonRect, "UTH_AddToOrder".Translate(), null))
			{
				UTH_UIData.systemAlert1.PlayOneShotOnCamera();

				int addedOrModifiedCount = 0;

				foreach (var item in itemCounts)
				{
					if (item.Value > 0)
					{
						if (!UTH_OrderCategoriesMenu.selectedThingDefs.ContainsKey(item.Key))
						{
							addedOrModifiedCount++;
						}
						UTH_OrderCategoriesMenu.selectedThingDefs[item.Key] = item.Value;
					}
					else if (UTH_OrderCategoriesMenu.selectedThingDefs.ContainsKey(item.Key))
					{
						addedOrModifiedCount++;
						UTH_OrderCategoriesMenu.selectedThingDefs.Remove(item.Key);
					}
				}
				if (addedOrModifiedCount > 0)
				{
					Messages.Message($"UTH_AddedRemovedItems".Translate(addedOrModifiedCount), MessageTypeDefOf.SilentInput);
				}

				this.Close();
			}
		}

		private void LoadItems()
		{
			items = DefDatabase<ThingDef>.AllDefsListForReading
				.Where(d => IsItemInSelectedCategory(d))
				.ToList();
		}

		private void FilterItems()
		{
			if (string.IsNullOrEmpty(searchText))
			{
				filteredItems = new List<ThingDef>(items);
			}
			else
			{
				string lowerSearchText = searchText.ToLower();
				filteredItems = items
					.Where(item => item != null && item.label.ToLower().Contains(lowerSearchText))
					.ToList();
			}
		}

		private bool IsItemInSelectedCategory(ThingDef item)
		{
			if (item == null)
			{
				return false;
			}

			if (item.category != ThingCategory.Item && item.category != ThingCategory.Building)
			{
				return false;
			}

			if (item.tradeability == Tradeability.None)
			{
				return false;
			}

			try
			{
				float baseMarketValue = item.BaseMarketValue;
				if (baseMarketValue <= 0)
				{
					return false;
				}
			}
			catch (Exception ex)
			{
				Log.Error($"[UTH] IsItemInSelectedCategory: Exception accessing item.BaseMarketValue - {ex.Message}");
				return false;
			}

			switch (categoryIndex)
			{
				//"Weapons", "Apparel", "Food", "Medicine", "Materials", "Buildings", "Exotic", "Other"
				case 0: return item.IsWeapon;
				case 1: return item.IsApparel;
				case 2: return item.IsNutritionGivingIngestible && !item.IsCorpse;
				case 3: return item.IsMedicine;
				case 4: return item.IsStuff;
				case 5: return item.Minifiable;
				case 6: return item.tradeTags != null && (item.tradeTags.Contains("UtilitySpecial") || item.tradeTags.Contains("ExoticMisc") || item.tradeTags.Contains("ExoticBuilding"));
				case 7:
					return !item.IsWeapon &&
				   !item.IsApparel &&
				   !item.IsNutritionGivingIngestible &&
				   !item.IsMedicine &&
				   !item.IsStuff &&
				   !item.IsCorpse &&
				   (item.tradeTags == null || !(item.tradeTags.Contains("UtilitySpecial") || item.tradeTags.Contains("ExoticMisc") || item.tradeTags.Contains("ExoticBuilding"))) &&
				   item.category == ThingCategory.Item;

				default: return false;
			}
		}

		private void DrawItemRow(Rect rect, ThingDef item)
		{
			Widgets.DrawHighlightIfMouseover(rect);
			GUI.color = Color.white;

			Rect iconRect = new Rect(rect.x, rect.y, rect.height, rect.height);
			Widgets.ThingIcon(iconRect, item);

			Rect nameRect = new Rect(iconRect.xMax + 10f, rect.y, rect.width / 2f - 20f, rect.height);
			Widgets.Label(nameRect, item.label);

			Rect priceRect = new Rect(nameRect.xMax + 10f, rect.y, 100f, rect.height);
			Widgets.Label(priceRect, adjustedMarketValues[item].ToStringMoney());

			if (!itemCounts.ContainsKey(item))
			{
				itemCounts[item] = 0;
			}
			int count = itemCounts[item];
			string buffer = count.ToString();

			Rect adjustRect = new Rect(rect.xMax - 120f, rect.y + 5f, 40f, rect.height - 10f);
			Widgets.TextFieldNumeric(adjustRect, ref count, ref buffer, 0, 9999);
			itemCounts[item] = count;

			Rect leftArrowRect = new Rect(adjustRect.xMax + 15f, rect.y, 30f, rect.height);
			Rect rightArrowRect = new Rect(leftArrowRect.xMax, rect.y, 30f, rect.height);

			if (UTH_UIUtility.DrawCustomButton(leftArrowRect, "<", null))
			{
				int newCount = count;
				UTH_UIUtility.HandleButtonClick(ref newCount, -1, false);
				itemCounts[item] = newCount;
			}

			if (UTH_UIUtility.DrawCustomButton(rightArrowRect, ">", null))
			{
				int newCount = count;
				UTH_UIUtility.HandleButtonClick(ref newCount, 1, true);
				itemCounts[item] = newCount;
			}

			totalPrice = itemCounts.Sum(ic => adjustedMarketValues[ic.Key] * ic.Value);
		}

		private void DrawSelectedItemRow(Rect rect, ThingDef item)
		{
			Widgets.DrawHighlightIfMouseover(rect);
			GUI.color = Color.white;

			Rect iconRect = new Rect(rect.x, rect.y, rect.height - 5f, rect.height - 5f);
			Widgets.ThingIcon(iconRect, item);

			Rect nameRect = new Rect(iconRect.xMax + 10f, rect.y, rect.width / 3f - 20f, rect.height);
			Widgets.Label(nameRect, item.label);

			int count = itemCounts[item];
			string buffer = count.ToString();

			Rect countRect = new Rect(nameRect.xMax + 10f, rect.y + 5f, 40f, rect.height - 10f);
			Widgets.TextFieldNumeric(countRect, ref count, ref buffer, 0, 9999);
			itemCounts[item] = count;

			Rect leftArrowRect = new Rect(countRect.xMax + 10f, rect.y, 30f, rect.height);
			Rect rightArrowRect = new Rect(leftArrowRect.xMax, rect.y, 30f, rect.height);

			if (UTH_UIUtility.DrawCustomButton(leftArrowRect, "<", null))
			{
				int newCount = count;
				UTH_UIUtility.HandleButtonClick(ref newCount, -1, false);
				itemCounts[item] = newCount;
			}

			if (UTH_UIUtility.DrawCustomButton(rightArrowRect, ">", null))
			{
				int newCount = count;
				UTH_UIUtility.HandleButtonClick(ref newCount, 1, true);
				itemCounts[item] = newCount;
			}

			Rect totalItemPriceRect = new Rect(rightArrowRect.xMax + 10f, rect.y, rect.width - rightArrowRect.xMax - 10f, rect.height);
			Widgets.Label(totalItemPriceRect, (adjustedMarketValues[item] * itemCounts[item]).ToStringMoney());

			totalPrice = itemCounts.Sum(ic => adjustedMarketValues[ic.Key] * ic.Value);
		}
	}

	public class UTH_FinalOrderMenu : Window
	{
		private readonly Building_UniversalTradeConsole console;

		private Dictionary<ThingDef, int> orderedItems;
		private readonly Dictionary<ThingDef, float> adjustedMarketValues;

		private bool expressDeliveryChecked = false;
		private bool insuranceChecked = false;

		private Vector2 selectedItemsScrollPosition = Vector2.zero;

		private float expressDeliveryBaseCost = UTH_Mod.settings.expressDeliveryBaseCost;
		private float expressDeliveryMultiplierPerKg = UTH_Mod.settings.expressDeliveryMultiplierPerKg;
		private float expressDeliveryTimeReduction = UTH_Mod.settings.expressDeliveryTimeReduction;
		private float insuranceBaseCost = UTH_Mod.settings.insuranceBaseCost;
		private float insuranceMultiplier = UTH_Mod.settings.insuranceMultiplier;
		private float deliveryTimePerKg = UTH_Mod.settings.deliveryTimePerKg;
		private float deliveryTimeReductionForStuff = UTH_Mod.settings.deliveryTimeReductionForStuff;

		private readonly float wealthMultiplier = UTH_Mod.settings.wealthMultiplier;
		private readonly float colonyWealth = WealthUtility.PlayerWealth;
		public float taxRate = UTH_Mod.settings.taxRate;

		private UTH_WorldComponent_SubscriptionTracker subscriptionTracker = Find.World.GetComponent<UTH_WorldComponent_SubscriptionTracker>();
		private UTH_SubscriptionMenu subscriptionMenu;
		private float multToPercent;

		private float specialMultiplier = UTH_Mod.settings.specialMultiplier;

		private float totalAvailableSilver;

		public UTH_FinalOrderMenu(Pawn interactingPawn, Building_UniversalTradeConsole console, Dictionary<ThingDef, int> orderedItems)
		{
			this.console = console;
			this.orderedItems = orderedItems;

			closeOnAccept = true;
			closeOnCancel = true;
			doCloseX = false;
			absorbInputAroundWindow = true;
			forcePause = true;

			totalAvailableSilver = UTH_UIUtility.CalculateTotalAvailableSilver(console.Map);

			adjustedMarketValues = new Dictionary<ThingDef, float>();

			subscriptionMenu = new UTH_SubscriptionMenu(console);
			multToPercent = (1 - subscriptionMenu.PriceReductionMultiplier) * 100;

			foreach (var item in orderedItems)
			{
				if (item.Key == null || item.Key.BaseMarketValue <= 0)
				{
					return;
				}

				if (!adjustedMarketValues.ContainsKey(item.Key))
				{
					adjustedMarketValues[item.Key] = item.Key.BaseMarketValue;
				}

				if (item.Key.tradeTags != null && (item.Key.tradeTags.Contains("UtilitySpecial") || item.Key.tradeTags.Contains("ExoticMisc") || item.Key.tradeTags.Contains("ExoticBuilding")))
				{
					adjustedMarketValues[item.Key] = item.Key.BaseMarketValue * specialMultiplier;
				}
			}
		}

		public override Vector2 InitialSize => new Vector2(900f, 600f);

		public override void DoWindowContents(Rect inRect)
		{
			Widgets.DrawBoxSolid(inRect, UTH_UIUtility.bgColor);
			GUI.color = Color.white;

			float padding = 80f;

			float selectedItemsWidth = inRect.width / 2f - 80f;
			float selectedItemsHeight = inRect.height - 220f;
			float selectedItemsStartX = inRect.xMin + padding;
			float selectedItemsStartY = inRect.y + padding;

			Rect selectedItemsRect = new Rect(selectedItemsStartX, selectedItemsStartY, selectedItemsWidth, selectedItemsHeight);
			Widgets.DrawBoxSolid(selectedItemsRect, UTH_UIUtility.itemsMenuBGColor);

			Rect miscBoxRect = new Rect(selectedItemsRect.xMin, selectedItemsRect.yMin - 30f, selectedItemsRect.width, 30f);
			Widgets.DrawBoxSolidWithOutline(miscBoxRect, UTH_UIUtility.miscBoxColor, Color.gray);

			string labelText = "UTH_Cart".Translate();
			Vector2 labelSize = Text.CalcSize(labelText);
			float labelX = miscBoxRect.xMin + (miscBoxRect.width - labelSize.x) / 2;
			float labelY = miscBoxRect.yMin + (miscBoxRect.height - labelSize.y) / 2;
			Widgets.Label(new Rect(labelX, labelY, labelSize.x, labelSize.y), labelText);

			UTH_UIUtility.RemoveInvalidItems(orderedItems);

			var selectedItems = orderedItems.Where(kv => kv.Value > 0).ToList();
			Rect selectedItemsScrollContentRect = new Rect(0f, 0f, selectedItemsWidth - 20f, selectedItems.Count * 40f);

			Widgets.BeginScrollView(selectedItemsRect, ref selectedItemsScrollPosition, selectedItemsScrollContentRect);

			float selectedItemsY = 5f;
			foreach (var pair in selectedItems)
			{
				Rect selectedItemRect = new Rect(0f, selectedItemsY, selectedItemsScrollContentRect.width, 40f);
				DrawSelectedItemRow(selectedItemRect, pair.Key);
				selectedItemsY += 40f;
			}

			Widgets.EndScrollView();

			float priceBoxHeight = 105f;
			Rect boxRect = new Rect(inRect.xMax - (selectedItemsWidth - 40f) - padding, selectedItemsRect.yMax - priceBoxHeight, selectedItemsWidth - 40f, priceBoxHeight);
			Widgets.DrawBoxSolid(boxRect, UTH_UIUtility.itemsMenuBGColor);

			float labelWidth = boxRect.width;
			float labelHeight = 40f;

			float startX = boxRect.x + 20f;
			float startY = boxRect.y + (boxRect.height - labelHeight * 2) / 2;

			//these checkboxes go below order summary info
			float expressDeliveryCost = CalculateExpressDeliveryCost(expressDeliveryBaseCost, expressDeliveryMultiplierPerKg, expressDeliveryChecked);
			float insuranceCost = CalculateInsuranceCost(insuranceBaseCost, insuranceMultiplier, insuranceChecked);

			Rect expressDeliveryRect = new Rect(startX, startY, 170f, UTH_UIUtility.buttonHeight);
			Widgets.CheckboxLabeled(expressDeliveryRect, "UTH_ExpressDelivery".Translate().Resolve(), ref expressDeliveryChecked);

			Rect insuranceRect = new Rect(startX, expressDeliveryRect.yMax, 170f, UTH_UIUtility.buttonHeight);
			Widgets.CheckboxLabeled(insuranceRect, "UTH_InsuranceCoverage".Translate().Resolve(), ref insuranceChecked);

			if (expressDeliveryChecked)
			{
				Rect expressDeliveryCostRect = new Rect(insuranceRect.xMax + 10f, expressDeliveryRect.y + 5f, 60f, labelHeight);
				Widgets.Label(expressDeliveryCostRect, $"+ {expressDeliveryCost.ToStringMoney()}");
			}

			if (insuranceChecked)
			{
				Rect insuranceCostRect = new Rect(insuranceRect.xMax + 10f, insuranceRect.y + 5f, 60f, labelHeight);
				Widgets.Label(insuranceCostRect, $"+ {insuranceCost.ToStringMoney()}");
			}

			float totalPriceWithoutTax = orderedItems.Sum(kv => adjustedMarketValues[kv.Key] * kv.Value);
			float totalTax = (totalPriceWithoutTax + colonyWealth * Mathf.Pow(wealthMultiplier, 2f)) * taxRate;
			if (totalPriceWithoutTax == 0)
			{
				totalTax = 0;
			}
			float finalPrice = totalPriceWithoutTax + totalTax + expressDeliveryCost + insuranceCost;

			if (subscriptionTracker.hasSubscription)
			{
				finalPrice *= subscriptionMenu.PriceReductionMultiplier;
			}

			float totalDeliveryTime = CalculateTotalDeliveryTime(expressDeliveryChecked);

			float priceBoxHeight2 = selectedItemsHeight - priceBoxHeight + 20f;
			Rect boxRect2 = new Rect(boxRect.x, selectedItemsRect.yMin - miscBoxRect.height, boxRect.width, priceBoxHeight2);
			Widgets.DrawBoxSolid(boxRect2, UTH_UIUtility.itemsMenuBGColor);

			string orderSummaryText = $"<size=16>{"UTH_OrderSummary".Translate().Resolve()}</size>";
			Vector2 textSize = Text.CalcSize(orderSummaryText);

			float startY2 = boxRect2.y + (boxRect2.height - labelHeight * 5) / 2;
			float centerX2 = boxRect2.xMin + (boxRect2.width - textSize.x) / 2;

			Rect orderSummaryRect = new Rect(centerX2, startY2 - 15f, labelWidth, labelHeight);
			Widgets.Label(orderSummaryRect, orderSummaryText);

			Rect totalAvailableSilverRect = new Rect(startX, orderSummaryRect.yMax, labelWidth, labelHeight);
			Widgets.Label(totalAvailableSilverRect, $"<size=14>{"UTH_TotalAvailableSilver".Translate().Resolve()}{totalAvailableSilver.ToStringMoney()}</size>");

			Rect totalPriceLabelRect = new Rect(startX, totalAvailableSilverRect.yMax, labelWidth, labelHeight);
			Widgets.Label(totalPriceLabelRect, $"<size=14>{"UTH_TotalPriceMinusTax".Translate().Resolve()}{totalPriceWithoutTax.ToStringMoney()}</size>");

			Rect totalTaxLabelRect = new Rect(startX, totalPriceLabelRect.yMax, labelWidth, labelHeight);
			Widgets.Label(totalTaxLabelRect, $"<size=14>{"UTH_TotalTax".Translate().Resolve()}{totalTax.ToStringMoney()}</size>");

			string finalPriceString = $"<size=14>{"UTH_FinalPriceWithTax".Translate().Resolve()}{finalPrice.ToStringMoney()}</size>";
			Vector2 finalPriceSize = Text.CalcSize(finalPriceString);

			Rect finalPriceLabelRect = new Rect(startX, totalTaxLabelRect.yMax, labelWidth, labelHeight);
			Widgets.Label(finalPriceLabelRect, finalPriceString);

			if (subscriptionTracker.hasSubscription)
			{
				Rect percentRect = new Rect(finalPriceLabelRect.xMin + finalPriceSize.x + 20f, finalPriceLabelRect.yMin, 80f, labelHeight);
				Widgets.Label(percentRect, $"(-{multToPercent}%)");
			}

			string formattedDeliveryTime = totalDeliveryTime <= 0 ? "< 1h" : totalDeliveryTime.ToString() + $" {"UTH_Days".Translate()}";

			Rect totalDeliveryTimeLabelRect = new Rect(startX, finalPriceLabelRect.yMax, labelWidth, labelHeight);
			Widgets.Label(totalDeliveryTimeLabelRect, $"<size=14>{"UTH_TotalDeliveryTime".Translate().Resolve()}{formattedDeliveryTime}</size>");

			float buttonX = inRect.center.x - UTH_UIUtility.buttonWidth / 2;
			float buttonY = selectedItemsRect.yMax + 30f;
			float buttonXOffset = 200f;

			Rect backButtonRect = new Rect(buttonX - buttonXOffset, buttonY, UTH_UIUtility.buttonWidth, UTH_UIUtility.buttonHeight);
			if (UTH_UIUtility.DrawCustomButton(backButtonRect, "UTH_Back".Translate(), null))
			{
				this.Close();
			}

			Rect resetButtonRect = new Rect(buttonX, buttonY, UTH_UIUtility.buttonWidth, UTH_UIUtility.buttonHeight);
			if (UTH_UIUtility.DrawCustomButton(resetButtonRect, "UTH_Reset".Translate(), null))
			{
				orderedItems.Clear();
			}

			Rect confirmButtonRect = new Rect(buttonX + buttonXOffset, buttonY, UTH_UIUtility.buttonWidth, UTH_UIUtility.buttonHeight);
			if (UTH_UIUtility.DrawCustomButton(confirmButtonRect, "UTH_ConfirmOrder".Translate(), null))
			{
				if (orderedItems.Count == 0)
				{
					UTH_UIData.systemErr1.PlayOneShotOnCamera();
					Messages.Message("UTH_NoItemsSelectedForOrder".Translate(), MessageTypeDefOf.SilentInput, historical: false);
				}
				else if (totalAvailableSilver >= finalPrice)
				{
					UTH_UIData.systemComplete1.PlayOneShotOnCamera();
					Messages.Message($"UTH_OrderConfirmedPodsArriveIn".Translate(formattedDeliveryTime.ToLower()), MessageTypeDefOf.SilentInput, historical: false);
					PlaceOrderAndScheduleDelivery(finalPrice, insuranceCost, totalDeliveryTime);

					orderedItems.Clear();

					this.Close();
				}
				else
				{
					UTH_UIData.systemErr1.PlayOneShotOnCamera();
					Messages.Message("UTH_NotEnoughSilver".Translate(), MessageTypeDefOf.SilentInput, historical: false);
				}
			}
		}

		private void DrawSelectedItemRow(Rect rect, ThingDef item)
		{
			Widgets.DrawHighlightIfMouseover(rect);
			GUI.color = Color.white;

			Rect iconRect = new Rect(rect.x, rect.y, rect.height - 5f, rect.height - 5f);
			Widgets.ThingIcon(iconRect, item);

			Rect nameRect = new Rect(iconRect.xMax + 10f, rect.y, rect.width / 3f - 20f, rect.height);
			Widgets.Label(nameRect, item.label);

			int count = orderedItems[item];
			string buffer = count.ToString();

			Rect countRect = new Rect(nameRect.xMax + 10f, rect.y + 5f, 40f, rect.height - 10f);
			Widgets.TextFieldNumeric(countRect, ref count, ref buffer, 0, 9999);
			orderedItems[item] = count;

			Rect leftArrowRect = new Rect(countRect.xMax + 10f, rect.y, 30f, rect.height);
			Rect rightArrowRect = new Rect(leftArrowRect.xMax, rect.y, 30f, rect.height);

			if (UTH_UIUtility.DrawCustomButton(leftArrowRect, "<", null))
			{
				int newCount = count;
				UTH_UIUtility.HandleButtonClick(ref newCount, -1, false);
				orderedItems[item] = newCount;
			}

			if (UTH_UIUtility.DrawCustomButton(rightArrowRect, ">", null))
			{
				int newCount = count;
				UTH_UIUtility.HandleButtonClick(ref newCount, 1, true);
				orderedItems[item] = newCount;
			}

			Rect totalItemPriceRect = new Rect(rightArrowRect.xMax + 10f, rect.y, rect.width - rightArrowRect.xMax - 10f, rect.height);
			Widgets.Label(totalItemPriceRect, (adjustedMarketValues[item] * orderedItems[item]).ToStringMoney());
		}

		private float CalculateInsuranceCost(float baseCost, float multiplier, bool isChecked)
		{
			if (isChecked)
			{
				int totalItems = orderedItems.Sum(kv => kv.Value);
				return baseCost + (totalItems * multiplier);
			}
			else
			{
				return 0f;
			}
		}

		private float CalculateExpressDeliveryCost(float baseCost, float multiplierPerKg, bool isChecked)
		{
			if (isChecked)
			{
				float totalWeight = orderedItems.Sum(kv => kv.Key.BaseMass * kv.Value);
				return baseCost + (totalWeight * multiplierPerKg);
			}
			else
			{
				return 0f;
			}
		}

		private float CalculateTotalDeliveryTime(bool isExpress)
		{
			float baseTime = orderedItems.Sum(kv => kv.Value * (kv.Key.BaseMass * (kv.Key.IsStuff ? deliveryTimeReductionForStuff : 1f)));
			baseTime = baseTime * deliveryTimePerKg;
			float totalTime = isExpress ? baseTime * expressDeliveryTimeReduction : baseTime;
			return Mathf.Round(totalTime * 10f) / 10f;
		}

		private void PlaceOrderAndScheduleDelivery(float finalPrice, float insuranceCost, float deliveryTimeDays)
		{
			List<Thing> items = new List<Thing>();
			foreach (var kvp in orderedItems)
			{
				Thing item = ThingMaker.MakeThing(kvp.Key, GenStuff.DefaultStuffFor(kvp.Key));
				if (item is Book book)
				{
					book.GenerateBook();
				}
				if (item is Building building)
				{
					item = building.TryMakeMinified();
				}
				item.stackCount = kvp.Value;
				items.Add(item);
			}

			int deliveryTick = Find.TickManager.TicksGame + (int)(deliveryTimeDays * GenDate.TicksPerDay);

			var orderData = new ScheduledOrderData
			{
				items = items,
				deliveryTick = deliveryTick,
				map = console.Map,
				deliveryPoint = DropCellFinder.TradeDropSpot(console.Map),
				insuranceChecked = insuranceChecked,
				insuranceCost = insuranceCost,
				orderCost = finalPrice
			};

			TradeUtility.LaunchSilver(console.Map, (int)finalPrice);

			Find.World.GetComponent<UTH_WorldComponent_OrderScheduler>().ScheduleOrder(orderData);
		}
	}

	public static class TimeUtility
	{
		public static string TicksToHoursAndMinutesString(int ticks)
		{
			int hours = (ticks / GenDate.TicksPerHour) + 1;
			int days = hours / 24;
			hours %= 24;

			return $"UTH_DaysHours".Translate(days, hours);
		}
	}

	public class UTH_ActiveOrdersMenu : Window
	{
		private List<ScheduledOrderData> activeOrders;

		private Vector2 activeOrdersScrollPosition = Vector2.zero;

		private HashSet<ScheduledOrderData> ordersToRemove = new HashSet<ScheduledOrderData>();

		private Dictionary<ScheduledOrderData, int> orderedItems = new Dictionary<ScheduledOrderData, int>();

		public UTH_ActiveOrdersMenu()
		{
			this.activeOrders = Find.World.GetComponent<UTH_WorldComponent_OrderScheduler>().ScheduledOrders;
			closeOnAccept = true;
			closeOnCancel = true;
			doCloseX = false;
			absorbInputAroundWindow = true;
			forcePause = true;

			foreach (var order in activeOrders)
			{
				orderedItems[order] = order.items.Count;
			}
		}

		public override Vector2 InitialSize => new Vector2(700f, 500f);

		public override void DoWindowContents(Rect inRect)
		{
			Widgets.DrawBoxSolid(inRect, UTH_UIUtility.bgColor);
			GUI.color = Color.white;

			float padding = 40f;

			float activeOrdersWidth = inRect.width - 40f;
			float activeOrdersHeight = inRect.height - 170f;
			float activeOrdersStartX = inRect.x + (inRect.width - activeOrdersWidth) / 2f; ;
			float activeOrdersStartY = inRect.y + padding + 20f;

			Rect activeOrdersRect = new Rect(activeOrdersStartX, activeOrdersStartY, activeOrdersWidth, activeOrdersHeight);
			Widgets.DrawBoxSolid(activeOrdersRect, UTH_UIUtility.itemsMenuBGColor);

			Rect miscBoxRect = new Rect(activeOrdersRect.xMin, activeOrdersRect.yMin - 30f, activeOrdersRect.width, 30f);
			Widgets.DrawBoxSolidWithOutline(miscBoxRect, UTH_UIUtility.miscBoxColor, Color.gray);

			string labelText = "UTH_ActiveOrders".Translate();
			Vector2 labelSize = Text.CalcSize(labelText);
			float labelX = miscBoxRect.xMin + (miscBoxRect.width - labelSize.x) / 2;
			float labelY = miscBoxRect.yMin + (miscBoxRect.height - labelSize.y) / 2;
			Widgets.Label(new Rect(labelX, labelY, labelSize.x, labelSize.y), labelText);

			Rect activeOrdersScrollContentRect = new Rect(0f, 0f, activeOrdersWidth - 20f, activeOrders.Count * 40f);

			Widgets.BeginScrollView(activeOrdersRect, ref activeOrdersScrollPosition, activeOrdersScrollContentRect);

			ProcessRemovals();

			float activeOrdersY = 5f;
			foreach (var order in activeOrders)
			{
				Rect orderRowRect = new Rect(0f, activeOrdersY, activeOrdersScrollContentRect.width, 40f);
				DrawOrderedItemsRow(orderRowRect, order);
				activeOrdersY += 40f;
			}

			Widgets.EndScrollView();

			float buttonX = inRect.center.x - UTH_UIUtility.buttonWidth / 2;
			float buttonY = activeOrdersRect.yMax + 20f;
			float buttonXOffset = 200f;

			Rect backButtonRect = new Rect(buttonX - buttonXOffset, buttonY, UTH_UIUtility.buttonWidth, UTH_UIUtility.buttonHeight);
			if (UTH_UIUtility.DrawCustomButton(backButtonRect, "UTH_Back".Translate(), null))
			{
				this.Close();
			}
		}

		private void DrawOrderedItemsRow(Rect rect, ScheduledOrderData order)
		{
			Widgets.DrawHighlightIfMouseover(rect);
			GUI.color = Color.white;

			float xOffset = 10f;
			float yOffset = 5f;
			float iconSize = rect.height - 2 * yOffset;

			for (int i = 0; i < Mathf.Min(3, order.items.Count); i++)
			{
				Thing item = order.items[i];

				Rect iconRect = new Rect(rect.x + xOffset, rect.y + yOffset, iconSize, iconSize);
				Widgets.ThingIcon(iconRect, item);

				xOffset += iconSize + 2;
			}

			int ticksUntilArrival = order.deliveryTick - Find.TickManager.TicksGame;
			string arrivalText = $"UTH_Arrival".Translate() + $": {TimeUtility.TicksToHoursAndMinutesString(ticksUntilArrival)}";
			Vector2 textSize = Text.CalcSize(arrivalText);
			float timeRectX = rect.x + (rect.width - textSize.x) / 2;
			Rect timeRect = new Rect(timeRectX, rect.y + (rect.height - textSize.y) / 2, textSize.x, textSize.y);
			Widgets.Label(timeRect, arrivalText);

			Rect cancelRect = new Rect(rect.xMax - 80f, rect.y + yOffset, 60f, rect.height - 2 * yOffset);
			if (UTH_UIUtility.DrawCustomButton(cancelRect, "UTH_Cancel".Translate(), null))
			{
				CancelOrder(order);
			}

			Rect infoButtonRect = new Rect(cancelRect.xMin - cancelRect.width - 10f, cancelRect.y, cancelRect.width, cancelRect.height);
			if (UTH_UIUtility.DrawCustomButton(infoButtonRect, "UTH_Info".Translate(), null))
			{
				Find.WindowStack.Add(new FloatMenu(GetOrderInfoMenuItems(order)));
			}
		}

		private List<FloatMenuOption> GetOrderInfoMenuItems(ScheduledOrderData order)
		{
			List<FloatMenuOption> options = new List<FloatMenuOption>();

			foreach (var item in order.items)
			{
				options.Add(new FloatMenuOption(item.LabelCap, () => Find.WindowStack.Add(new Dialog_InfoCard(item)), MenuOptionPriority.Default, null, null, 0f, null, null));
			}

			return options;
		}

		private void CancelOrder(ScheduledOrderData order)
		{
			UTH_WorldComponent_OrderScheduler orderScheduler = Find.World.GetComponent<UTH_WorldComponent_OrderScheduler>();

			UTH_UIData.systemAlert1.PlayOneShotOnCamera();
			orderScheduler.RefundOrder(order);
			Messages.Message("UTH_OrderCancelledAndRefunded".Translate(), MessageTypeDefOf.SilentInput, historical: false);

			orderScheduler.RemoveOrder(order);
			ordersToRemove.Add(order);
		}

		private void ProcessRemovals()
		{
			if (ordersToRemove.Any())
			{
				foreach (var order in ordersToRemove)
				{
					Find.World.GetComponent<UTH_WorldComponent_OrderScheduler>().RemoveOrder(order);
					activeOrders.Remove(order);
				}
				ordersToRemove.Clear();
			}
		}
	}
}