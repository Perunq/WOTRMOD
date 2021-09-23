using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Items.Slots;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI.ServiceWindow;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.Utility;
using UnityEngine;

namespace WOTRMOD
{
	internal static class Functions
	{
		public static void FixCoupDeGrace()
		{
			/*var groetusFeature = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>("c3e4d5681906d5246ab8b0637b98cbfe");
                groetusFeature.ComponentsArray = groetusFeature.ComponentsArray
                    .Where(c => !(c is PrerequisiteFeature))
                    .ToArray();*/

			ContextActionProvokeAttackOfOpportunity Action = Helpers.Create<ContextActionProvokeAttackOfOpportunity>(null);
			Action.ApplyToCaster = true;
			AbilityEffectCDG replacement = Helpers.Create<AbilityEffectCDG>(delegate (AbilityEffectCDG a)
			{
				a.Actions = Helpers.CreateActionList(new GameAction[]
				{
					Action
				});
			});
			/*cdg.SetDescription( "As a full-round action, you can use a melee weapon" +
				" to deliver a coup de grace to a helpless opponent." +
				"\nYou automatically hit and score a critical hit.If the defender survives the damage, he must make a Fortitude " +
				"save (DC 10 + damage dealt) or die.\nDelivering a coup de grace provokes attacks of opportunity from threatening" +
				" opponents.\nYou can't deliver a coup de grace against a creature that is immune to critical hits.");
			//not implemented*/
			cdg.ReplaceComponent<AbilityEffectRunAction>(replacement);
		}

		private static BlueprintAbility cdg = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>("32280b137ca642c45be17e2d92898758");


	}

	[ComponentName("AbilityEffectNewCoup")]
	[AllowedOn(typeof(BlueprintAbility))]
	public class AbilityEffectCDG : AbilityCustomLogic
	{
		[UsedImplicitly]
		public override IEnumerator<AbilityDeliveryTarget> Deliver(AbilityExecutionContext context, TargetWrapper target)
		{
			UnitEntityData caster = context.MaybeCaster;
			bool flag = caster == null;
			if (flag)
			{
				
				yield break;
			}
			WeaponSlot threatHand = caster.GetThreatHand();
			bool flag2 = threatHand == null;
			if (flag2)
			{
				
				yield break;
			}
			UnitEntityData targetUnit = target.Unit;
			bool flag3 = targetUnit == null;
			if (flag3)
			{
				yield break;
			}
			int attackPenalty = 0;
			AbilityEffectCDG.EventHandlers handlers = new AbilityEffectCDG.EventHandlers();
			handlers.Add(new AbilityEffectCDG.Coup(caster));
			RuleAttackWithWeapon rule = new RuleAttackWithWeapon(caster, targetUnit, threatHand.Weapon, attackPenalty)
			{
				AutoHit = true,
				AutoCriticalConfirmation = true,
				AutoCriticalThreat = true
			};
			using (handlers.Activate())
			{
				context.TriggerRule<RuleAttackWithWeapon>(rule);
			}
			AbilityEffectCDG.EventHandlers eventHandlers = null;
			yield return new AbilityDeliveryTarget(target);
			RuleSavingThrow rule3 = new RuleSavingThrow(targetUnit, SavingThrowType.Fortitude, AbilityEffectCDG.m_coupDamage + 10);
			context.TriggerRule<RuleSavingThrow>(rule3);
			bool flag6 = !rule3.IsPassed;
			if (flag6)
			{
				targetUnit.Descriptor.State.MarkedForDeath = true;
			}
			using (context.GetDataScope(target))
			{
				this.Actions.Run();
			}
			rule3 = null;
			yield break;
		}


		public override void Cleanup(AbilityExecutionContext context)
		{
		}

		private static int m_coupDamage;
		public ActionList Actions;

		private class EventHandlers : IDisposable
		{

			public void Add(object handler)
			{
				this.m_Handlers.Add(handler);
			}

			public AbilityEffectCDG.EventHandlers Activate()
			{
				foreach (object subscriber in this.m_Handlers)
				{
					EventBus.Subscribe(subscriber);
				}
				return this;
			}

			public void Dispose()
			{
				foreach (object subscriber in this.m_Handlers)
				{
					EventBus.Unsubscribe(subscriber);
				}
			}

			private readonly List<object> m_Handlers = new List<object>();
		}


		public class Coup : IInitiatorRulebookHandler<RuleDealDamage>, IRulebookHandler<RuleDealDamage>, IInitiatorRulebookSubscriber
		{

			public Coup(UnitEntityData unit)
			{
				this.m_Unit = unit;
			}

			public UnitEntityData GetSubscribingUnit()
			{
				return this.m_Unit;
			}

			public void OnEventAboutToTrigger(RuleDealDamage evt)
			{
			}

			public void OnEventDidTrigger(RuleDealDamage evt)
			{
				AbilityEffectCDG.m_coupDamage = evt.Result;
			}

			private readonly UnitEntityData m_Unit;
		}
	}





}