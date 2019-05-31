﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.Localization;
using Kingmaker.RuleSystem;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.CasterCheckers;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using Kingmaker.Blueprints.Items;
using static Kingmaker.UnitLogic.ActivatableAbilities.ActivatableAbilityResourceLogic;
using static Kingmaker.UnitLogic.Commands.Base.UnitCommand;

namespace KingmakerRebalance
{

    class Common
    {
        static internal LibraryScriptableObject library => Main.library;

        internal enum DomainSpellsType
        {
            NoSpells = 1,
            SpecialList = 2,
            NormalList = 3
        }

        internal struct SpellId
        {
            public readonly string guid;
            public readonly int level;
            public SpellId(string spell_guid, int spell_level)
            {
                guid = spell_guid;
                level = spell_level;
            }
        }

        internal class ExtraSpellList
        {
            SpellId[] spells;

            public ExtraSpellList(params SpellId[] list_spells)
            {
                spells = list_spells;
            }


            public ExtraSpellList(params string[] list_spell_guids)
            {
                spells = new SpellId[list_spell_guids.Length];
                for (int i = 0; i < list_spell_guids.Length; i++)
                {
                    spells[i] = new SpellId(list_spell_guids[i], i + 1);
                }
            }


            public Kingmaker.Blueprints.Classes.Spells.BlueprintSpellList createSpellList(string name, string guid)
            {
                var spell_list = Helpers.Create<Kingmaker.Blueprints.Classes.Spells.BlueprintSpellList>();
                spell_list.name = name;
                library.AddAsset(spell_list, guid);
                spell_list.SpellsByLevel = new SpellLevelList[10];
                for (int i = 0; i < spell_list.SpellsByLevel.Length; i++)
                {
                    spell_list.SpellsByLevel[i] = new SpellLevelList(i);
                }
                foreach (var s in spells)
                {
                    var spell = library.Get<BlueprintAbility>(s.guid);
                    spell.AddToSpellList(spell_list, s.level);
                }
                return spell_list;
            }


            public Kingmaker.UnitLogic.FactLogic.LearnSpellList createLearnSpellList(string name, string guid, BlueprintCharacterClass character_class, BlueprintArchetype archetype = null)
            {
                Kingmaker.UnitLogic.FactLogic.LearnSpellList learn_spell_list = new Kingmaker.UnitLogic.FactLogic.LearnSpellList();
                learn_spell_list.Archetype = archetype;
                learn_spell_list.CharacterClass = character_class;
                learn_spell_list.SpellList = createSpellList(name, guid);
                return learn_spell_list;
            }

        }


        internal static BlueprintFeature createCantrips(string name, string display_name, string description, UnityEngine.Sprite icon, string guid, BlueprintCharacterClass character_class,
                                       StatType stat, BlueprintAbility[] spells)
        {
            var learn_spells = new LearnSpells();
            learn_spells.CharacterClass = character_class;
            learn_spells.Spells = spells;

            var bind_spells = Helpers.CreateBindToClass(character_class, stat, spells);
            bind_spells.LevelStep = 1;
            bind_spells.Cantrip = true;
            return Helpers.CreateFeature(name,
                                  display_name,
                                  description,
                                  guid,
                                  icon,
                                  FeatureGroup.None,
                                  Helpers.CreateAddFacts(spells),
                                  learn_spells,
                                  bind_spells
                                  );
        }

        internal static Kingmaker.UnitLogic.Mechanics.Actions.ContextActionConditionalSaved createContextSavedApplyBuff(BlueprintBuff buff, DurationRate duration_rate,
                                                                                                                        AbilityRankType rank_type = AbilityRankType.Default)
        {
            var context_saved = new Kingmaker.UnitLogic.Mechanics.Actions.ContextActionConditionalSaved();
            context_saved.Succeed = new Kingmaker.ElementsSystem.ActionList();
            var apply_buff = new Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff();
            apply_buff.IsFromSpell = true;
            apply_buff.Buff = buff;
            var bonus_value = Helpers.CreateContextValue(rank_type);
            bonus_value.ValueType = ContextValueType.Rank;
            apply_buff.DurationValue = Helpers.CreateContextDuration(bonus: bonus_value,
                                                                           rate: duration_rate);
            context_saved.Failed = Helpers.CreateActionList(apply_buff);
            return context_saved;
        }


        internal static Kingmaker.UnitLogic.Mechanics.Actions.ContextActionConditionalSaved createContextSavedApplyBuff(BlueprintBuff buff, ContextDurationValue duration, bool is_from_spell = false,
                                                                                                                  bool is_child = false, bool is_permanent = false)
        {
            var context_saved = new Kingmaker.UnitLogic.Mechanics.Actions.ContextActionConditionalSaved();
            context_saved.Succeed = new Kingmaker.ElementsSystem.ActionList();
            var apply_buff = new Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff();
            apply_buff.IsFromSpell = true;
            apply_buff.Buff = buff;
            apply_buff.DurationValue = duration;
            apply_buff.IsFromSpell = is_from_spell;
            apply_buff.AsChild = is_child;
            apply_buff.Permanent = is_permanent;
            context_saved.Failed = Helpers.CreateActionList(apply_buff);
            return context_saved;
        }


        internal static Kingmaker.UnitLogic.FactLogic.AddSecondaryAttacks createAddSecondaryAttacks(params Kingmaker.Blueprints.Items.Weapons.BlueprintItemWeapon[] weapons)
        {
            var c = new Kingmaker.UnitLogic.FactLogic.AddSecondaryAttacks();
            c.Weapon = weapons;
            return c;
        }


        static internal Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff createContextActionApplyBuff(BlueprintBuff buff, ContextDurationValue duration, bool is_from_spell = false,
                                                                                                                  bool is_child = false, bool is_permanent = false)
        {
            var apply_buff = new Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff();
            apply_buff.IsFromSpell = is_from_spell;
            apply_buff.Buff = buff;
            apply_buff.Permanent = is_permanent;
            apply_buff.DurationValue = duration;
            return apply_buff;
        }


        static internal Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRandomize createContextActionRandomize(params Kingmaker.ElementsSystem.ActionList[] actions)
        {
            var c = new Kingmaker.UnitLogic.Mechanics.Actions.ContextActionRandomize();
            Type m_Action_type = Helpers.GetField(c, "m_Actions").GetType().GetElementType();
            var y = Array.CreateInstance(m_Action_type, actions.Length);
            var field = m_Action_type.GetField("Action");
            for (int i = 0; i < actions.Length; i++)
            {
                var yi = m_Action_type.GetConstructor(new System.Type[0]).Invoke(new object[0]);
                field.SetValue(yi, actions[i]);
                y.SetValue(yi, i);
            }
            Helpers.SetField(c, "m_Actions", y);
            return c;
        }


        static internal Kingmaker.Designers.Mechanics.Buffs.BuffStatusCondition createBuffStatusCondition(UnitCondition condition, SavingThrowType save_type = SavingThrowType.Unknown,
                                                                                                           bool save_each_round = true)
        {
            var c = new Kingmaker.Designers.Mechanics.Buffs.BuffStatusCondition();
            c.SaveType = save_type;
            c.SaveEachRound = save_each_round;
            c.Condition = condition;
            return c;
        }

        static internal Kingmaker.UnitLogic.Buffs.Conditions.BuffConditionCheckRoundNumber createBuffConditionCheckRoundNumber(int round_number, bool not = false)
        {
            var c = new Kingmaker.UnitLogic.Buffs.Conditions.BuffConditionCheckRoundNumber();
            c.RoundNumber = round_number;
            c.Not = not;
            return c;
        }


        static internal ContextValue createSimpleContextValue(int value)
        {
            var v = new ContextValue();
            v.Value = value;
            v.ValueType = ContextValueType.Simple;
            return v;
        }

        internal static BlueprintFeatureSelection copyRenameSelection(string original_selection_guid, string name_prefix, string description,string selection_guid, string[] feature_guids )
        {
            var old_selection = library.Get<BlueprintFeatureSelection>(original_selection_guid);
            var new_selection = library.CopyAndAdd<BlueprintFeatureSelection>(original_selection_guid, name_prefix + old_selection, selection_guid);

            new_selection.SetDescription(description);

            BlueprintFeature[] new_features = new BlueprintFeature[feature_guids.Length];

            var old_features = old_selection.AllFeatures;
            if (new_features.Length != old_features.Length)
            {
                throw Main.Error($"Incorrect number of guids passed to Common.createFavoredTerrainSelection:: guids.Length =  {new_features.Length}, terrains.Length: {old_features.Length}");
            }
            for (int i = 0; i < old_features.Length; i++)
            {
                new_features[i] = library.CopyAndAdd<BlueprintFeature>(old_features[i].AssetGuid, name_prefix + old_features[i].name, feature_guids[i]);
                new_features[i].SetDescription(description);
            }
            new_selection.AllFeatures = new_features;
            return new_selection;
        }





        internal static BlueprintFeature createSmite(string name, string display_name, string description, string guid, string ability_guid, UnityEngine.Sprite icon,
                                                     BlueprintCharacterClass[] classes, AlignmentComponent smite_alignment)
        {
            var smite_ability = library.CopyAndAdd<BlueprintAbility>("7bb9eb2042e67bf489ccd1374423cdec", name+"Ability", ability_guid);
            var smite_feature = library.CopyAndAdd<BlueprintFeature>("3a6db57fce75b0244a6a5819528ddf26", name +"Feature", guid);


            smite_feature.SetName(display_name);
            smite_feature.SetDescription(description);
            smite_feature.SetIcon(icon);

            smite_feature.ReplaceComponent<Kingmaker.UnitLogic.FactLogic.AddFacts>(Helpers.CreateAddFact(smite_ability));
            smite_ability.SetName(smite_feature.Name);
            smite_ability.SetDescription(smite_feature.Description);
            smite_ability.SetIcon(icon);
            smite_ability.RemoveComponent(smite_ability.GetComponent<Kingmaker.UnitLogic.Abilities.Components.CasterCheckers.AbilityCasterAlignment>());
            var context_rank_config = smite_ability.GetComponents<Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig>().Where(a => a.Type == AbilityRankType.DamageBonus).ElementAt(0);
            var new_context_rank_config = context_rank_config.CreateCopy();
            Helpers.SetField(new_context_rank_config, "m_Class", classes);
            smite_ability.ReplaceComponent(context_rank_config, new_context_rank_config);

            var smite_action = smite_ability.GetComponent<Kingmaker.UnitLogic.Abilities.Components.AbilityEffectRunAction>();
            var new_smite_action = smite_action.CreateCopy();
            var condition = (Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional)new_smite_action.Actions.Actions[0];
            new_smite_action.Actions =  Helpers.CreateActionList(new_smite_action.Actions.Actions);
            var old_conditional = (Kingmaker.Designers.EventConditionActionSystem.Actions.Conditional)new_smite_action.Actions.Actions[0];
            var conditions = new Kingmaker.ElementsSystem.Condition[] { Helpers.CreateContextConditionAlignment(smite_alignment, false, false),  old_conditional.ConditionsChecker.Conditions[1]};
            new_smite_action.Actions.Actions[0] = Helpers.CreateConditional(conditions, old_conditional.IfTrue.Actions, old_conditional.IfFalse.Actions);

            return smite_feature;
        }


        internal static PrerequisiteNoArchetype prerequisiteNoArchetype(BlueprintCharacterClass character_class, BlueprintArchetype archetype)
        {
            var p = new PrerequisiteNoArchetype();
            p.Archetype = archetype;
            p.CharacterClass = character_class;
            return p;
        }

        internal static BlueprintFeature createSpellResistance(string name, string display_name, string description, string guid, BlueprintCharacterClass character_class, int start_value)
        {
            var spell_resistance = library.CopyAndAdd<BlueprintFeature>("01182bcee8cb41640b7fa1b1ad772421", //monk diamond soul
                                                                        name,
                                                                        guid);
            spell_resistance.SetName(display_name);
            spell_resistance.SetDescription(description);
            spell_resistance.Groups = new FeatureGroup[0];
            spell_resistance.RemoveComponent(spell_resistance.GetComponent<Kingmaker.Blueprints.Classes.Prerequisites.PrerequisiteClassLevel>());
            var context_rank_config = spell_resistance.GetComponent<Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig>();
            Helpers.SetField(context_rank_config, "m_StepLevel", start_value);
            Helpers.SetField(context_rank_config, "m_Class", new BlueprintCharacterClass[] { character_class });

            return spell_resistance;
        }





        internal static Kingmaker.UnitLogic.FactLogic.AddDamageResistancePhysical createAlignmentDR(int dr_value, DamageAlignment alignment)
        {
            var feat = new Kingmaker.UnitLogic.FactLogic.AddDamageResistancePhysical();
            feat.Alignment = alignment;
            feat.BypassedByAlignment = true;
            feat.Value.ValueType = ContextValueType.Simple;
            feat.Value.Value = dr_value;

            return feat;
        }


        internal static Kingmaker.UnitLogic.FactLogic.AddDamageResistancePhysical createAlignmentDRContextRank(DamageAlignment alignment)
        {
            var feat = new Kingmaker.UnitLogic.FactLogic.AddDamageResistancePhysical();
            feat.Alignment = alignment;
            feat.BypassedByAlignment = true;
            feat.Value = Helpers.CreateContextValueRank(AbilityRankType.StatBonus);
            return feat;
        }


        internal static Kingmaker.UnitLogic.FactLogic.AddDamageResistanceEnergy createEnergyDR(int dr_value, DamageEnergyType energy)
        {
            var feat = new Kingmaker.UnitLogic.FactLogic.AddDamageResistanceEnergy();
            feat.Type = energy;
            feat.Value.ValueType = ContextValueType.Simple;
            feat.Value.Value = dr_value;

            return feat;
        }


        internal static Kingmaker.UnitLogic.FactLogic.AddDamageResistanceEnergy createEnergyDRContextRank(DamageEnergyType energy)
        {
            var feat = new Kingmaker.UnitLogic.FactLogic.AddDamageResistanceEnergy();
            feat.Type = energy;
            feat.Value = Helpers.CreateContextValueRank(AbilityRankType.StatBonus);
            return feat;
        }


        internal static void addClassToDomains(BlueprintCharacterClass class_to_add, BlueprintArchetype[] archetypes_to_add, DomainSpellsType spells_type, BlueprintFeatureSelection domain_selection)
        {
            var domains = domain_selection.AllFeatures;
            foreach (var domain_feature in domains)
            {
                
                BlueprintProgression domain = (BlueprintProgression)domain_feature;
                domain.Classes = domain.Classes.AddToArray(class_to_add);
                domain.Archetypes = domain.Archetypes.AddToArray(archetypes_to_add);
               // Main.logger.Log("Processing " + domain.Name);

                foreach (var entry in domain.LevelEntries)
                {
                    foreach (var feat in entry.Features)
                    {
                        addClassToFeat(class_to_add, archetypes_to_add, spells_type, feat);
                    }
                }

                if (spells_type == DomainSpellsType.NormalList)
                {
                    
                    var spell_list = domain.GetComponent<Kingmaker.UnitLogic.FactLogic.LearnSpellList>().SpellList;

                    if (archetypes_to_add.Empty())
                    {
                        var learn_spells_fact = Helpers.Create<Kingmaker.UnitLogic.FactLogic.LearnSpellList>();
                        learn_spells_fact.SpellList = spell_list;
                        learn_spells_fact.CharacterClass = class_to_add;
                        domain.AddComponent(learn_spells_fact);
                        
                    }
                    else
                    {
                        foreach (var ar_type in archetypes_to_add)
                        {
                            var learn_spells_fact = Helpers.Create<Kingmaker.UnitLogic.FactLogic.LearnSpellList>();
                            learn_spells_fact.SpellList = spell_list;
                            learn_spells_fact.CharacterClass = class_to_add;
                            learn_spells_fact.Archetype = ar_type;
                            domain.AddComponent(learn_spells_fact);
                        }
                    }
                }
            }
        }


        static void addClassToContextRankConfig(BlueprintCharacterClass class_to_add, ContextRankConfig c)
        {
            BlueprintCharacterClass cleric_class = library.Get<BlueprintCharacterClass>("67819271767a9dd4fbfd4ae700befea0");
            var classes = Helpers.GetField<BlueprintCharacterClass[]>(c, "m_Class");
            if (classes.Contains(cleric_class))
            {
                classes = classes.AddToArray(class_to_add);
                Helpers.SetField(c, "m_Class", classes);
            }
        }


        static void addClassToAreaEffect(BlueprintCharacterClass class_to_add, Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbilityAreaEffect a)
        {
            var components = a.ComponentsArray;
            foreach (var c in components)
            {
                if (c is ContextRankConfig)
                {
                    var c_typed = (Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig)c;
                    addClassToContextRankConfig(class_to_add, c_typed);
                }
                else if (c is Kingmaker.UnitLogic.Abilities.Components.AreaEffects.AbilityAreaEffectBuff)
                {
                    var c_typed = (Kingmaker.UnitLogic.Abilities.Components.AreaEffects.AbilityAreaEffectBuff)c;
                    addClassToBuff(class_to_add, c_typed.Buff);
                }
            }
        }


        static void addClassToBuff(BlueprintCharacterClass class_to_add ,BlueprintBuff b)
        {
            var components = b.ComponentsArray;
            foreach (var c in components)
            {
                if (c is ContextRankConfig)
                {
                    var c_typed = (Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig)c;
                    addClassToContextRankConfig(class_to_add, c_typed);
                }
                else if (c is Kingmaker.UnitLogic.Buffs.Components.AddAreaEffect)
                {
                    var c_typed = (Kingmaker.UnitLogic.Buffs.Components.AddAreaEffect)c;
                    addClassToAreaEffect(class_to_add, c_typed.AreaEffect);
                }
            }
        }


        static void addClassToAbility(BlueprintCharacterClass class_to_add, BlueprintAbility a)
        {
            var components = a.ComponentsArray;
            foreach (var c in components)
            {
                if (c is Kingmaker.UnitLogic.Abilities.Components.AbilityVariants)
                {
                    var c_typed = (Kingmaker.UnitLogic.Abilities.Components.AbilityVariants)c;
                    foreach (var v in c_typed.Variants)
                    {
                        addClassToAbility(class_to_add, v);
                    }
                }
                else if (c is Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig)
                {
                    var c_typed = (Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig)c;
                    addClassToContextRankConfig(class_to_add, c_typed);
                }
                else if (c is AbilityEffectRunAction)
                {
                    var c_typed = (AbilityEffectRunAction)c;
                    foreach (var aa in c_typed.Actions.Actions)
                    {
                        if (aa == null)
                        {
                            continue;
                        }
                        if (aa is Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff)
                        {
                            var aa_typed = (Kingmaker.UnitLogic.Mechanics.Actions.ContextActionApplyBuff)aa;
                            addClassToBuff(class_to_add, aa_typed.Buff);
                        }
                    }
                }

            }
        }


        static void addClassToFact(BlueprintCharacterClass class_to_add, BlueprintArchetype[] archetypes_to_add, DomainSpellsType spells_type, BlueprintUnitFact f)
        {
            if (f is Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility)
            {
                var f_typed = (Kingmaker.UnitLogic.Abilities.Blueprints.BlueprintAbility)f;
                addClassToAbility(class_to_add, f_typed);
            }
            else if (f is Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility)
            {
                var f_typed = (Kingmaker.UnitLogic.ActivatableAbilities.BlueprintActivatableAbility)f;
                addClassToBuff(class_to_add, f_typed.Buff);
            }
        }


        static void addClassToResource(BlueprintCharacterClass class_to_add, BlueprintArchetype[] archetypes_to_add, BlueprintAbilityResource rsc)
        {
            BlueprintCharacterClass cleric_class = library.Get<BlueprintCharacterClass>("67819271767a9dd4fbfd4ae700befea0");
            var amount = ExtensionMethods.getMaxAmount(rsc);
            var classes = Helpers.GetField<BlueprintCharacterClass[]>(amount, "Class");
            var archetypes = Helpers.GetField<BlueprintArchetype[]>(amount, "Archetypes");

            if (classes.Contains(cleric_class))
            {
                classes = classes.AddToArray(class_to_add);
                archetypes = archetypes.AddToArray(archetypes_to_add);
                Helpers.SetField(amount, "Class", classes);
                Helpers.SetField(amount, "Archetypes", archetypes);
                ExtensionMethods.setMaxAmount(rsc, amount);
            }

        }


        static void addClassToFeat(BlueprintCharacterClass class_to_add, BlueprintArchetype[] archetypes_to_add, DomainSpellsType spells_type, BlueprintFeatureBase feat)
        {
            foreach (var c in feat.ComponentsArray)
            {
                if (c is Kingmaker.Designers.Mechanics.Buffs.IncreaseSpellDamageByClassLevel)
                {
                    var c_typed = (Kingmaker.Designers.Mechanics.Buffs.IncreaseSpellDamageByClassLevel)c;
                    c_typed.AdditionalClasses = c_typed.AdditionalClasses.AddToArray(class_to_add);
                    c_typed.Archetypes = c_typed.Archetypes.AddToArray(archetypes_to_add);
                }
                else if (c is Kingmaker.Designers.Mechanics.Facts.AddFeatureOnClassLevel)
                {
                    var c_typed = (Kingmaker.Designers.Mechanics.Facts.AddFeatureOnClassLevel)c;
                    if (c_typed.Feature.ComponentsArray.Length > 0 
                          && c_typed.Feature.ComponentsArray[0] is Kingmaker.UnitLogic.FactLogic.AddSpecialSpellList)
                    {
                        if (spells_type == DomainSpellsType.SpecialList)
                        {
                            //TODO: will need to make a copy of feature and replace CharacterClass in component with class_to_add
                        }
                        else
                        {
                            continue;
                        }
                    }
                    c_typed.AdditionalClasses = c_typed.AdditionalClasses.AddToArray(class_to_add);
                    c_typed.Archetypes = c_typed.Archetypes.AddToArray(archetypes_to_add);
                    addClassToFeat(class_to_add, archetypes_to_add, spells_type, c_typed.Feature);
                }
                else if (c is Kingmaker.UnitLogic.FactLogic.AddSpecialSpellList && spells_type == DomainSpellsType.SpecialList)
                {
                    /*var c_typed = (Kingmaker.UnitLogic.FactLogic.AddSpecialSpellList)c;
                    if (c_typed.CharacterClass != class_to_add)
                    {
                        var c2 = Helpers.Create<Kingmaker.UnitLogic.FactLogic.AddSpecialSpellList>();
                        c2.CharacterClass = class_to_add;
                        c2.SpellList = c_typed.SpellList;
                        feat.AddComponent(c2);
                    }*/
                }
                else if (c is Kingmaker.UnitLogic.FactLogic.AddFacts)
                {
                    var c_typed = (Kingmaker.UnitLogic.FactLogic.AddFacts)c;
                    foreach (var f in c_typed.Facts)
                    {
                        addClassToFact(class_to_add, archetypes_to_add, spells_type, f);
                    }
                }
                else if (c is Kingmaker.Designers.Mechanics.Facts.AddAbilityResources)
                {
                    var c_typed = (Kingmaker.Designers.Mechanics.Facts.AddAbilityResources)c;
                    addClassToResource(class_to_add, archetypes_to_add, c_typed.Resource);
                }
                else if (c is Kingmaker.Designers.Mechanics.Facts.FactSinglify)
                {
                    var c_typed = (Kingmaker.Designers.Mechanics.Facts.FactSinglify)c;
                    foreach (var f in c_typed.NewFacts)
                    {
                        addClassToFact(class_to_add, archetypes_to_add, spells_type, f);
                    }
                }
                else if (c is Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig)
                {
                    var c_typed = (Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig)c;
                    addClassToContextRankConfig(class_to_add, c_typed);
                }


            }
        }


        static internal Kingmaker.UnitLogic.FactLogic.AddConditionImmunity createAddConditionImmunity(UnitCondition condition)
        {
            Kingmaker.UnitLogic.FactLogic.AddConditionImmunity c = new Kingmaker.UnitLogic.FactLogic.AddConditionImmunity();
            c.Condition = condition;
            return c;
        }


        static internal Kingmaker.Designers.Mechanics.Facts.SavingThrowBonusAgainstDescriptor createSavingThrowBonusAgainstDescriptor(int bonus, ModifierDescriptor descriptor, SpellDescriptor spell_descriptor)
        {
            Kingmaker.Designers.Mechanics.Facts.SavingThrowBonusAgainstDescriptor c = new Kingmaker.Designers.Mechanics.Facts.SavingThrowBonusAgainstDescriptor();
            c.Bonus = bonus;
            c.ModifierDescriptor = descriptor;
            c.SpellDescriptor = spell_descriptor;
            return c;
        }
    }
}
