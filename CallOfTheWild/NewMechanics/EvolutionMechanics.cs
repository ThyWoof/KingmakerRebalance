﻿using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.ActivatableAbilities.Restrictions;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CallOfTheWild.EvolutionMechanics
{
    public class UnitPartEvolution : UnitPart
    {
        public class EvolutionEntry
        {
            public Fact buff;
            public int cost;

            public EvolutionEntry(Fact parent_buff, int evolution_cost)
            {
                buff = parent_buff;
                cost = evolution_cost;
            }


            public EvolutionEntry()
            {
                buff = null;
                cost = 0;
            }
        }

        [JsonProperty]
        Dictionary<string, EvolutionEntry> temporary_evolutions = new Dictionary<string, EvolutionEntry>(); //id of feature and reference to buff (might be null)
        Dictionary<string, EvolutionEntry> permanent_evolutions = new Dictionary<string, EvolutionEntry>(); //id of feature and reference to buff (null)
        [JsonProperty]
        Dictionary<Fact, int> evolution_points = new Dictionary<Fact, int>();
        private int evolution_points_spent = 0;


        public int getNumEvolutionPoints()
        {
            int amount = 0;
            foreach (var kv in evolution_points)
            {
                amount += kv.Value;
            }
            return amount - evolution_points_spent;
        }

        public void increaseNumberOfEvolutionPoints(int bonus, Fact fact)
        {
            evolution_points[fact] = bonus;
        }

        public void removeEvolutionPointsIncrease(Fact fact)
        {
            evolution_points.Remove(fact);
        }

        public void addTemporaryEvolution(BlueprintFeature feature, int cost, Fact buff = null)
        {
            removeTemporaryEvolution(feature, buff);
            temporary_evolutions[feature.AssetGuid] = new EvolutionEntry(buff, cost);
            evolution_points_spent += temporary_evolutions[feature.AssetGuid].cost;
        }


        public void addPermanentEvolution(BlueprintFeature feature)
        {
            permanent_evolutions[feature.AssetGuid] = null;
        }


        public bool hasEvolution(BlueprintFeature feature)
        {
            return temporary_evolutions.ContainsKey(feature.AssetGuid) || permanent_evolutions.ContainsKey(feature.AssetGuid);
        }


        public bool hasPermanentEvolution(BlueprintFeature feature)
        {
            return permanent_evolutions.ContainsKey(feature.AssetGuid);
        }


        private void removeTemporaryEvolutionInternal(string feature_id, Fact buff)
        {
            if (temporary_evolutions.ContainsKey(feature_id) && temporary_evolutions[feature_id].buff == buff)
            {
                evolution_points_spent -= temporary_evolutions[feature_id].cost;
                temporary_evolutions.Remove(feature_id);
            }
        }


        public void removeTemporaryEvolution(BlueprintFeature feature, Fact buff)
        {
            removeTemporaryEvolutionInternal(feature.AssetGuid, buff);
        }


        public void removePermanentEvolution(BlueprintFeature feature)
        {
            permanent_evolutions.Remove(feature.AssetGuid);
        }


        public void removeTemporaryEvolutions()
        {
            foreach (var kv in temporary_evolutions.ToArray())
            {                
                if (kv.Value.buff != null)
                {
                    this.Owner.RemoveFact(kv.Value.buff);
                }
            }
        }
    }



    [AllowedOn(typeof(BlueprintUnitFact))]
    public class AddTemporaryEvolution : AddFeatureToCompanion
    {
        public int cost = 0;

        public override void OnFactActivate()
        {
            base.OnFactActivate();
            this.Owner.Ensure<UnitPartEvolution>().addTemporaryEvolution(Feature, cost, this.Fact);
        }

        public override void OnTurnOn()
        {
            base.OnTurnOn();
            this.Owner.Ensure<UnitPartEvolution>().addTemporaryEvolution(Feature, cost, this.Fact);
        }

        public override void OnTurnOff()
        {          
            base.OnFactDeactivate();
            this.Owner.Ensure<UnitPartEvolution>().removeTemporaryEvolution(Feature, this.Fact);
        }


        public override void OnFactDeactivate()
        {         
            base.OnFactDeactivate();
            this.Owner.Ensure<UnitPartEvolution>().removeTemporaryEvolution(Feature, this.Fact);
        }
    }


    [AllowedOn(typeof(BlueprintUnitFact))]
    public class AddPermanentEvolution : AddFeatureToCompanion
    {
        public override void OnFactActivate()
        {
            base.OnFactActivate();
            this.Owner.Ensure<UnitPartEvolution>().addPermanentEvolution(Feature);
        }


        public override void OnTurnOn()
        {
            base.OnTurnOn();
            this.Owner.Ensure<UnitPartEvolution>().addPermanentEvolution(Feature);
        }

        public override void OnTurnOff()
        {
            base.OnFactDeactivate();
            this.Owner.Ensure<UnitPartEvolution>().removePermanentEvolution(Feature);
        }

        public override void OnFactDeactivate()
        {
            base.OnFactDeactivate();
            this.Owner.Ensure<UnitPartEvolution>().removePermanentEvolution(Feature);
        }
    }


    [AllowedOn(typeof(BlueprintUnitFact))]
    public class AddFakeEvolution : OwnedGameLogicComponent<UnitDescriptor>
    {
        public BlueprintFeature Feature;
        public override void OnFactActivate()
        {
            this.Owner.Ensure<UnitPartEvolution>().addPermanentEvolution(Feature);
        }


        public override void OnTurnOn()
        {
            this.Owner.Ensure<UnitPartEvolution>().addPermanentEvolution(Feature);
        }

        public override void OnTurnOff()
        {
            this.Owner.Ensure<UnitPartEvolution>().removePermanentEvolution(Feature);
        }

        public override void OnFactDeactivate()
        {
            this.Owner.Ensure<UnitPartEvolution>().removePermanentEvolution(Feature);
        }
    }


    [AllowedOn(typeof(BlueprintUnitFact))]
    public class IncreaseEvolutionPool : OwnedGameLogicComponent<UnitDescriptor>
    {
        public ContextValue amount;
        public override void OnFactActivate()
        {
            this.Owner.Ensure<UnitPartEvolution>().increaseNumberOfEvolutionPoints(amount.Calculate(this.Fact.MaybeContext), this.Fact);
        }


        public override void OnFactDeactivate()
        {
            this.Owner.Ensure<UnitPartEvolution>().removeEvolutionPointsIncrease(this.Fact);
        }
    }



    [AllowMultipleComponents]
    public class PrerequisiteEnoughEvolutionPoints : Prerequisite
    {
        public int amount;
        public BlueprintFeature feature;

        public override bool Check(
          FeatureSelectionState selectionState,
          UnitDescriptor unit,
          LevelUpState state)
        {
                return unit.Ensure<UnitPartEvolution>().getNumEvolutionPoints() >= amount || unit.Progression.Features.HasFact((BlueprintFact)this.feature);
        }

        public override string GetUIText()
        {
            return $"At least {amount} unused evolution point{(amount == 1 ? "" : "s")}.";
        }
    }


    [AllowedOn(typeof(BlueprintAbility))]
    public class AbilityShowIfHasEvolution : BlueprintComponent, IAbilityVisibilityProvider
    {
        public BlueprintFeature evolution;
        public bool not;

        public bool IsAbilityVisible(AbilityData ability)
        {
            if (ability.Caster.Pet == null)
            {
                return false;
            }

            return ability.Caster.Ensure<UnitPartEvolution>().hasEvolution(evolution) != not;
        }
    }

    [AllowedOn(typeof(BlueprintAbility))]
    public class PrerequisiteEvolution : Prerequisite
    {
        public BlueprintFeature evolution;
        public bool not;

        public override bool Check(
          FeatureSelectionState selectionState,
          UnitDescriptor unit,
          LevelUpState state)
        {
            return unit.Ensure<UnitPartEvolution>().hasEvolution(evolution) != not; ;
        }

        public override string GetUIText()
        {
            StringBuilder stringBuilder = new StringBuilder();
            if ((UnityEngine.Object)this.evolution == (UnityEngine.Object)null)
            {
                UberDebug.LogError((object)("Empty Feature field in prerequisite component: " + this.name), (object[])Array.Empty<object>());
            }
            else
            {
                if (string.IsNullOrEmpty(this.evolution.Name))
                    UberDebug.LogError((object)string.Format("{0} has no Display Name", (object)this.evolution.name), (object[])Array.Empty<object>());
                stringBuilder.Append(this.evolution.Name);
            }
            if (!not)
            {
                return "Has Evolution: " + stringBuilder.ToString();
            }
            else
            {
                return "No Evolution: " + stringBuilder.ToString();
            }
        }
    }



    [AllowedOn(typeof(BlueprintAbility))]
    public class PrerequisiteNoPermanentEvolution : Prerequisite
    {
        public BlueprintFeature evolution;

        public override bool Check(
          FeatureSelectionState selectionState,
          UnitDescriptor unit,
          LevelUpState state)
        {
            return !unit.Ensure<UnitPartEvolution>().hasPermanentEvolution(evolution);
        }

        public override string GetUIText()
        {
            StringBuilder stringBuilder = new StringBuilder();
            if ((UnityEngine.Object)this.evolution == (UnityEngine.Object)null)
            {
                UberDebug.LogError((object)("Empty Feature field in prerequisite component: " + this.name), (object[])Array.Empty<object>());
            }
            else
            {
                if (string.IsNullOrEmpty(this.evolution.Name))
                    UberDebug.LogError((object)string.Format("{0} has no Display Name", (object)this.evolution.name), (object[])Array.Empty<object>());
                stringBuilder.Append(this.evolution.Name);
            }
            return "No Permanent Evolution: " + stringBuilder.ToString();
        }
    }



    public abstract class ComponentAppliedOnceOnLevelUp : OwnedGameLogicComponent<UnitDescriptor>, ILevelUpCompleteUIHandler
    {
        public override void OnFactActivate()
        {
            try
            {

                // If we're in the level-up UI, apply the component
                var levelUp = Game.Instance.UI.CharacterBuildController.LevelUpController;
                if (Owner == levelUp.Preview || Owner == levelUp.Unit)
                {
                    Apply(levelUp.State);
                }
            }
            catch (Exception e)
            {
                Main.logger.Log(e.ToString());
            }
        }

        // Optionally remove this fact to free some memory; useful if the fact is already applied
        // and there is no reason to track its overall rank.
        protected virtual bool RemoveAfterLevelUp => false;

        public void HandleLevelUpComplete(UnitEntityData unit, bool isChargen)
        {

        }

        protected abstract void Apply(LevelUpState state);
    }

    [AllowMultipleComponents]
    [AllowedOn(typeof(BlueprintUnitFact))]
    public class RefreshEvolutionsOnLevelUp : OwnedGameLogicComponent<UnitDescriptor>, ILevelUpCompleteUIHandler
    {
        public override void OnFactActivate()
        {
            try
            {
                // If we're in the level-up UI, apply the component
                var levelUp = Game.Instance.UI.CharacterBuildController.LevelUpController;
                if (Owner == levelUp.Preview || Owner == levelUp.Unit)
                {                   
                    this.Owner.Ensure<UnitPartEvolution>().removeTemporaryEvolutions();
                }
            }
            catch (Exception e)
            {
                Main.logger.Log(e.ToString());
            }
        }

        public void HandleLevelUpComplete(UnitEntityData unit, bool isChargen)
        {

        }
    }


    public class addEvolutionSelection : OwnedGameLogicComponent<UnitDescriptor>, ILevelUpCompleteUIHandler
    {
        public BlueprintFeatureSelection selection;

        public void HandleLevelUpComplete(UnitEntityData unit, bool isChargen)
        {
        }

        public override void OnFactActivate()
        {
            try
            {
                var levelUp = Game.Instance.UI.CharacterBuildController.LevelUpController;
                if (Owner == levelUp.Preview || Owner == levelUp.Unit)
                {
                    if (this.Owner.Ensure<UnitPartEvolution>().getNumEvolutionPoints() > 0)
                    {
                        int index = levelUp.State.Selections.Count<FeatureSelectionState>((Func<FeatureSelectionState, bool>)(s => s.Selection == selection));
                        FeatureSelectionState featureSelectionState = new FeatureSelectionState(null, null, selection, index, 0);
                        levelUp.State.Selections.Add(featureSelectionState);
                    }
                }
            }
            catch (Exception e)
            {
                Main.logger.Error(e.ToString());
            }
        }
    }
}