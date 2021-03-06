﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Mud.Logger;
using Mud.Server.Constants;
using Mud.Server.Helpers;
using Mud.Server.Input;

namespace Mud.Server.Abilities
{
    public class AbilityManager : IAbilityManager
    {
        private const int WeakenedSoulAbilityId = 999;
        private const int ParryAbilityId = 1000;
        private const int DodgeAbilityId = 1001;
        private const int ShieldBlockAbilityId = 1002;
        private const int DualWieldAbilityId = 1003;
        private const int ThirdWieldAbilityId = 1004;
        private const int FourthWieldAbilityId = 1005;

        private readonly List<IAbility> _abilities = new List<IAbility> // TODO: dictionary on id + Trie on name
        {
            // Linked to Power Word: Shield (cannot be used/casted)
            new Ability(WeakenedSoulAbilityId, "Weakened Soul", AbilityTargets.Target, AbilityBehaviors.None, AbilityKinds.Spell, ResourceKinds.None, AmountOperators.None, 0, 0, 0, 0, SchoolTypes.None, AbilityMechanics.None, DispelTypes.None, AbilityFlags.CannotBeUsed),
            //
            new Ability(10, "Bear Form", AbilityTargets.Self, AbilityBehaviors.None, AbilityKinds.Spell, ResourceKinds.None, AmountOperators.None, 0, 0, 0, 0, SchoolTypes.Physical, AbilityMechanics.None, DispelTypes.None, AbilityFlags.AuraIsHidden, new ChangeFormEffect(Forms.Bear)),
            new Ability(11, "Cat Form", AbilityTargets.Self, AbilityBehaviors.None, AbilityKinds.Spell, ResourceKinds.None, AmountOperators.None, 0, 0, 0, 0, SchoolTypes.Physical, AbilityMechanics.None, DispelTypes.None, AbilityFlags.AuraIsHidden, new ChangeFormEffect(Forms.Cat)),
            new Ability(19, "Shadow Form", AbilityTargets.Self, AbilityBehaviors.None, AbilityKinds.Spell, ResourceKinds.None, AmountOperators.None, 0, 0, 0, 0, SchoolTypes.Physical, AbilityMechanics.None, DispelTypes.None, AbilityFlags.AuraIsHidden, new ChangeFormEffect(Forms.Shadow)),
            //
            new Ability(100, "Wrath", AbilityTargets.Target, AbilityBehaviors.Harmful, AbilityKinds.Spell, ResourceKinds.Mana, AmountOperators.Percentage, 4, 4, 0, 0, SchoolTypes.Nature, AbilityMechanics.None, DispelTypes.None, AbilityFlags.None, new DamageAbilityEffect(149, SecondaryAttributeTypes.SpellPower, SchoolTypes.Nature)),
            new Ability(101, "Thrash(bear)", AbilityTargets.Room, AbilityBehaviors.Harmful, AbilityKinds.Skill, ResourceKinds.Rage, AmountOperators.Fixed, 50, 4, 0, 15, SchoolTypes.Physical, AbilityMechanics.Bleeding, DispelTypes.None, AbilityFlags.RequireBearForm, new DamageAbilityEffect(513, SecondaryAttributeTypes.AttackPower, SchoolTypes.Physical), new DotAbilityEffect(365, SecondaryAttributeTypes.SpellPower, SchoolTypes.Physical, 3)),
            new Ability(102, "Thrash(cat)", AbilityTargets.Room, AbilityBehaviors.Harmful, AbilityKinds.Skill, ResourceKinds.Energy, AmountOperators.Fixed, 50, 4, 0, 15, SchoolTypes.Physical, AbilityMechanics.Bleeding, DispelTypes.None, AbilityFlags.RequireCatForm, new DamageAbilityEffect(513, SecondaryAttributeTypes.AttackPower, SchoolTypes.Physical), new DotAbilityEffect(365, SecondaryAttributeTypes.SpellPower, SchoolTypes.Physical, 3)),
            new Ability(103, "Shadow Word: Pain", AbilityTargets.Target, AbilityBehaviors.Harmful, AbilityKinds.Spell, ResourceKinds.Mana, AmountOperators.Percentage, 1, 1, 0, 18, SchoolTypes.Shadow, AbilityMechanics.None, DispelTypes.Magic, AbilityFlags.None, new DamageAbilityEffect(475, SecondaryAttributeTypes.SpellPower, SchoolTypes.Shadow), new DotAbilityEffect(475, SecondaryAttributeTypes.SpellPower, SchoolTypes.Shadow, 3)),
            new Ability(104, "Rupture", AbilityTargets.Target, AbilityBehaviors.Harmful, AbilityKinds.Skill, ResourceKinds.Energy, AmountOperators.Fixed, 25, 4, 0, 8 /* TODO: multiplied by combo*/, SchoolTypes.Physical, AbilityMechanics.Bleeding, DispelTypes.None, AbilityFlags.None, new DotAbilityEffect(685, SecondaryAttributeTypes.AttackPower, SchoolTypes.Physical, 2)),
            new Ability(105, "Renew", AbilityTargets.TargetOrSelf, AbilityBehaviors.Friendly, AbilityKinds.Spell, ResourceKinds.Mana, AmountOperators.Percentage, 2, 4, 0, 12, SchoolTypes.Holy, AbilityMechanics.None, DispelTypes.Magic, AbilityFlags.None, new HealAbilityEffect(22, SecondaryAttributeTypes.SpellPower), new HotAbilityEffect(44, SecondaryAttributeTypes.SpellPower, 3)),
            new Ability(106, "Power Word: Shield", AbilityTargets.TargetOrSelf, AbilityBehaviors.Friendly, AbilityKinds.Spell, ResourceKinds.Mana, AmountOperators.Percentage, 2, 1, 6, 15, SchoolTypes.Holy, AbilityMechanics.Shielded, DispelTypes.Magic, AbilityFlags.None, new PowerWordShieldEffect()),
            // TODO: + %maxHP should only be done on friendly target
            new Ability(107, "Death Coil", AbilityTargets.TargetOrSelf, AbilityBehaviors.Any, AbilityKinds.Spell, ResourceKinds.Runic, AmountOperators.Fixed, 30, 4, 0, 30, SchoolTypes.Shadow, AbilityMechanics.None, DispelTypes.None, AbilityFlags.None, new DamageOrHealEffect(0.88m, 0.88m*5, SecondaryAttributeTypes.AttackPower, SchoolTypes.Shadow), new AuraAbilityEffect(AuraModifiers.MaxHitPoints, 3, AmountOperators.Percentage)),
            new Ability(108, "Berserking", AbilityTargets.Self, AbilityBehaviors.Friendly, AbilityKinds.Spell, ResourceKinds.None, AmountOperators.None, 0, 4, 3*60, 10, SchoolTypes.Physical, AbilityMechanics.None, DispelTypes.None, AbilityFlags.None, new AuraAbilityEffect(AuraModifiers.AttackSpeed, 15, AmountOperators.Percentage)),
            new Ability(109, "Battle Shout", AbilityTargets.Group, AbilityBehaviors.Friendly, AbilityKinds.Spell, ResourceKinds.None, AmountOperators.None, 0, 4, 0, 1*60*60, SchoolTypes.Physical, AbilityMechanics.None, DispelTypes.None, AbilityFlags.None, new AuraAbilityEffect(AuraModifiers.AttackPower, 10, AmountOperators.Percentage)),
            new Ability(110, "Swiftmend", AbilityTargets.TargetOrSelf, AbilityBehaviors.Friendly, AbilityKinds.Spell, ResourceKinds.Mana, AmountOperators.Percentage, 14, 1, 30, 0, SchoolTypes.Nature, AbilityMechanics.None, DispelTypes.None, AbilityFlags.None, new HealAbilityEffect(700,SecondaryAttributeTypes.SpellPower)),

            new Ability(ParryAbilityId, "Parry", AbilityTargets.Self, AbilityBehaviors.None, AbilityKinds.Skill, ResourceKinds.None, AmountOperators.None, 0, 0, 0, 0, SchoolTypes.Physical, AbilityMechanics.None, DispelTypes.None, AbilityFlags.Passive),
            new Ability(DodgeAbilityId, "Dodge", AbilityTargets.Self, AbilityBehaviors.None, AbilityKinds.Skill, ResourceKinds.None, AmountOperators.None, 0, 0, 0, 0, SchoolTypes.Physical, AbilityMechanics.None, DispelTypes.None, AbilityFlags.Passive),
            new Ability(ShieldBlockAbilityId, "Shield Block", AbilityTargets.Self, AbilityBehaviors.None, AbilityKinds.Skill, ResourceKinds.None, AmountOperators.None, 0, 0, 0, 0, SchoolTypes.Physical, AbilityMechanics.None, DispelTypes.None, AbilityFlags.Passive),
            new Ability(DualWieldAbilityId, "Dual wield", AbilityTargets.Self, AbilityBehaviors.None, AbilityKinds.Skill, ResourceKinds.None, AmountOperators.None, 0, 0, 0, 0, SchoolTypes.Physical, AbilityMechanics.None, DispelTypes.None, AbilityFlags.Passive),
            new Ability(ThirdWieldAbilityId, "Third wield", AbilityTargets.Self, AbilityBehaviors.None, AbilityKinds.Skill, ResourceKinds.None, AmountOperators.None, 0, 0, 0, 0, SchoolTypes.Physical, AbilityMechanics.None, DispelTypes.None, AbilityFlags.Passive),
            new Ability(FourthWieldAbilityId, "Fourth wield", AbilityTargets.Self, AbilityBehaviors.None, AbilityKinds.Skill, ResourceKinds.None, AmountOperators.None, 0, 0, 0, 0, SchoolTypes.Physical, AbilityMechanics.None, DispelTypes.None, AbilityFlags.Passive),

            new Ability(8888, "Smite", AbilityTargets.Target, AbilityBehaviors.Harmful, AbilityKinds.Spell, ResourceKinds.Mana, AmountOperators.Percentage, 1, 1, 2, 0, SchoolTypes.Holy, AbilityMechanics.None, DispelTypes.None, AbilityFlags.CannotMiss, new DamageRangeAbilityEffect(516, 577, SchoolTypes.Holy), new HealSourceAbilityEffect(0.69m, SecondaryAttributeTypes.MaxHitPoints)),

            new Ability(999999, "Test", AbilityTargets.TargetOrSelf, AbilityBehaviors.Harmful, AbilityKinds.Spell, ResourceKinds.None, AmountOperators.None, 0, 5, 0, 60, SchoolTypes.Shadow, AbilityMechanics.Shielded, DispelTypes.Magic, AbilityFlags.None, new AuraAbilityEffect(AuraModifiers.HealAbsorb, 200000, AmountOperators.Fixed))
        };

        public AbilityManager()
        {
            WeakenedSoulAbility = this[WeakenedSoulAbilityId];
            ParryAbility = this[ParryAbilityId];
            DodgerAbility = this[DodgeAbilityId];
            ShieldBlockAbility = this[ShieldBlockAbilityId];
            DualWieldAbility = this[DualWieldAbilityId];
            ThirdWieldAbility = this[ThirdWieldAbilityId];
            FourthWieldAbility = this[FourthWieldAbilityId];
        }

        #region IAbilityManager

        public IAbility WeakenedSoulAbility { get; }
        public IAbility ParryAbility { get; }
        public IAbility DodgerAbility { get; }
        public IAbility ShieldBlockAbility { get; }
        public IAbility DualWieldAbility { get; }
        public IAbility ThirdWieldAbility { get; }
        public IAbility FourthWieldAbility { get; }

        public IEnumerable<IAbility> Abilities => _abilities.AsReadOnly();

        public IAbility this[int id] =>_abilities.FirstOrDefault(x => x.Id == id);

        public IAbility this[string name] => _abilities.FirstOrDefault(x => FindHelpers.StringEquals(x.Name, name));

        public IAbility Search(CommandParameter parameter, bool includePassive = false)
        {
            return _abilities.Where(x =>
                (x.Flags & AbilityFlags.CannotBeUsed) != AbilityFlags.CannotBeUsed
                && (!includePassive || (x.Flags & AbilityFlags.Passive) != AbilityFlags.Passive)
                && FindHelpers.StringStartsWith(x.Name, parameter.Value)).ElementAtOrDefault(parameter.Count - 1);
        }

        public bool Process(ICharacter source, params CommandParameter[] parameters)
        {
            //0/ Search ability (in known abilities)
            if (parameters.Length == 0)
            {
                source.Send("Cast/Use what ?");
                return false;
            }
            IAbility ability = Search(source.KnownAbilities, source.Level, parameters[0]);
            if (ability == null)
            {
                source.Send("You don't know any abilities of that name.");
                return false;
            }
            //1/ Check flags
            if ((ability.Flags & AbilityFlags.RequiresMainHand) == AbilityFlags.RequiresMainHand && !source.Equipments.Any(x => (x.Slot == EquipmentSlots.Wield || x.Slot == EquipmentSlots.Wield2H) && x.Item != null))
            {
                source.Send("You must be wielding in main-hand something prior using {0}", ability.Name);
                return false;
            }
            if ((ability.Flags & AbilityFlags.RequireBearForm) == AbilityFlags.RequireBearForm && source.Form != Forms.Bear)
            {
                source.Send("You must be in Bear form prior using {0}", ability.Name);
                return false;
            }
            if ((ability.Flags & AbilityFlags.RequireCatForm) == AbilityFlags.RequireCatForm && source.Form != Forms.Cat)
            {
                source.Send("You must be in Cat form prior using {0}", ability.Name);
                return false;
            }
            if ((ability.Flags & AbilityFlags.RequireMoonkinForm) == AbilityFlags.RequireMoonkinForm && source.Form != Forms.Moonkin)
            {
                source.Send("You must be in Moonkin form prior using {0}", ability.Name);
                return false;
            }
            if ((ability.Flags & AbilityFlags.RequireShadowForm) == AbilityFlags.RequireShadowForm && source.Form != Forms.Shadow)
            {
                source.Send("You must be in Shadow form prior using {0}", ability.Name);
                return false;
            }
            // TODO: shapeshift, combo
            //2/ Check cooldown
            int cooldownSecondsLeft = source.CooldownSecondsLeft(ability);
            if (cooldownSecondsLeft > 0)
            {
                source.Send("{0} is in cooldown for {1}.", ability.Name, StringHelpers.FormatDelay(cooldownSecondsLeft));
                return false;
            }
            //3/ Check resource
            int cost = ability.CostAmount; // default value (always overwritten if significant)
            if (ability.ResourceKind != ResourceKinds.None && ability.CostAmount > 0 && ability.CostType != AmountOperators.None)
            {
                if (!source.CurrentResourceKinds.Contains(ability.ResourceKind)) // TODO: not sure about this test
                {
                    source.Send("You can't use {0} as resource for the moment.", ability.ResourceKind);
                    return false;
                }
                int resourceLeft = source[ability.ResourceKind];
                if (ability.CostType == AmountOperators.Fixed)
                    cost = ability.CostAmount;
                else //ability.CostType == AmountOperators.Percentage
                {
                    int maxResource = source.GetMaxResource(ability.ResourceKind);
                    cost = maxResource*ability.CostAmount/100;
                }
                bool enoughResource = cost <= resourceLeft;
                if (!enoughResource)
                {
                    source.Send("You don't have enough {0}.", ability.ResourceKind);
                    return false;
                }
            }
            //4/ Check target(s)
            List<ICharacter> targets;
            switch (ability.Target)
            {
                case AbilityTargets.Self:
                    targets = new List<ICharacter> {source};
                    break;
                case AbilityTargets.Target:
                {
                    ICharacter target;
                    if (parameters.Length == 1 && ability.Behavior == AbilityBehaviors.Harmful && source.Fighting != null)
                        target = source.Fighting;
                    else if (parameters.Length == 1)
                    {
                        source.Send("{0} on whom ?", ability.Name);
                        return false;
                    }
                    else
                    {
                        target = FindHelpers.FindByName(source.Room.People, parameters[1]);
                        if (target == null)
                        {
                            source.Send(StringHelpers.CharacterNotFound);
                            return false;
                        }
                    }
                    targets = new List<ICharacter> {target};
                    break;
                }
                case AbilityTargets.TargetOrSelf:
                {
                    if (parameters.Length == 1)
                        targets = new List<ICharacter> {source};
                    else
                    {
                        ICharacter target = FindHelpers.FindByName(source.Room.People, parameters[1]);
                        if (target == null)
                        {
                            source.Send(StringHelpers.CharacterNotFound);
                            return false;
                        }
                        targets = new List<ICharacter> {target};
                    }
                    break;
                }
                case AbilityTargets.Group:
                {
                    // Source + group members
                    targets = new List<ICharacter>(source.GroupMembers)
                    {
                        source
                    };
                    break;
                }
                case AbilityTargets.Room:
                {
                        // Friendly -> everyone in room
                        // Harmful -> everyone not in group
                        // Any -> everyone in room
                    if (ability.Behavior == AbilityBehaviors.Friendly)
                        targets = source.Room.People.ToList();
                    else if (ability.Behavior == AbilityBehaviors.Harmful)
                        targets = source.Room.People.Where(x => x != source && !source.GroupMembers.Contains(x)).ToList();
                    else
                        targets = source.Room.People.ToList();
                    break;
                }
                default:
                    Log.Default.WriteLine(LogLevels.Error, "Unknown target {0} for ability {1}[{2}]!!!", ability.Target, ability.Name, ability.Id);
                    targets = Enumerable.Empty<ICharacter>().ToList();
                    break;
            }
            //5/ Say ability
            source.Send("You cast/use '{0}'.", ability.Name); // TODO: better wording
            //6/ Pay resource cost
            if (ability.ResourceKind != ResourceKinds.None && ability.CostAmount > 0 && ability.CostType != AmountOperators.None)
                source.ChangeResource(ability.ResourceKind, -cost);
            //7/ Perform effect(s) on target(s)
            IReadOnlyCollection<ICharacter> clone = new ReadOnlyCollection<ICharacter>(targets);
            foreach (ICharacter target in clone)
            {
                if (source != target)
                    target.Act(ActOptions.ToCharacter, "{0} casts/uses '{1}' on you.", source, ability.Name); // TODO: better wording
                ProcessOnOneTarget(source, target, ability, (ability.Flags & AbilityFlags.CannotMiss) == AbilityFlags.CannotMiss, (ability.Flags & AbilityFlags.CannotBeDodgedParriedBlocked) == AbilityFlags.CannotBeDodgedParriedBlocked);
            }
            //8/ Set global cooldown
            source.ImpersonatedBy?.SetGlobalCooldown(ability.GlobalCooldown);
            // TODO: if ability cannot be used because an effect cannot be casted (ex. power word: shield with weakened soul is still affecting)
            //9/ Set cooldown
            if (ability.Cooldown > 0)
                source.SetCooldown(ability);
            return true;
        }

        #endregion

        // TEST: TO REMOVE
        public bool Process(ICharacter source, ICharacter target, IAbility ability)
        {
            ProcessOnOneTarget(source, target, ability, false, false);
            return true;
        }

        private static void ProcessOnOneTarget(ICharacter source, ICharacter victim, IAbility ability, bool cannotMiss, bool cannotBeDodgedParriedBlocked)
        {
            if (ability?.Effects == null || ability.Effects.Count == 0 || !source.IsValid || !victim.IsValid)
                return;
            // Miss/Dodge/Parray/Block check (only for harmful ability)
            CombatHelpers.AttackResults attackResult = CombatHelpers.AttackResults.Hit;
            if (ability.Behavior == AbilityBehaviors.Harmful)
            {
                // Starts fight if needed (if A attacks B, A fights B and B fights A)
                if (source != victim)
                {
                    if (source.Fighting == null)
                        source.StartFighting(victim);
                    if (victim.Fighting == null)
                        victim.StartFighting(source);
                    // TODO: Cannot attack slave without breaking slavery
                }
                if (ability.Kind == AbilityKinds.Skill)
                {
                    // TODO: refactor same code in Character.OneHit
                    // Miss, dodge, parry, ...
                    attackResult = CombatHelpers.YellowMeleeAttack(source, victim, cannotMiss, cannotBeDodgedParriedBlocked);
                    Log.Default.WriteLine(LogLevels.Debug, $"{source.DebugName} -> {victim.DebugName} : attack result = {attackResult}");
                    switch (attackResult)
                    {
                        case CombatHelpers.AttackResults.Miss:
                            victim.Act(ActOptions.ToCharacter, "{0} misses you.", source);
                            source.Act(ActOptions.ToCharacter, "You miss {0}.", victim);
                            return; // no effect applied
                        case CombatHelpers.AttackResults.Dodge:
                            victim.Act(ActOptions.ToCharacter, "You dodge {0}'s {1}.", source, ability.Name);
                            source.Act(ActOptions.ToCharacter, "{0} dodges your {1}.", victim, ability.Name);
                            return; // no effect applied
                        case CombatHelpers.AttackResults.Parry:
                            victim.Act(ActOptions.ToCharacter, "You parry {0}'s {1}.", source, ability.Name);
                            source.Act(ActOptions.ToCharacter, "{0} parries your {1}.", victim, ability.Name);
                            return; // no effect applied
                        case CombatHelpers.AttackResults.Block:
                            EquipedItem victimShield = victim.Equipments.FirstOrDefault(x => x.Item != null && x.Slot == EquipmentSlots.Shield);
                            if (victimShield != null) // will never be null because MeleeAttack will not return Block if no shield
                            {
                                victim.Act(ActOptions.ToCharacter, "You block {0}'s {1} with {2}.", source, ability.Name, victimShield.Item);
                                source.Act(ActOptions.ToCharacter, "{0} blocks your {1} with {2}.", victim, ability.Name, victimShield.Item);
                            }
                            // effect applied
                            break;
                        case CombatHelpers.AttackResults.Critical:
                        case CombatHelpers.AttackResults.CrushingBlow:
                        case CombatHelpers.AttackResults.Hit:
                            // effect applied
                            break;
                        default:
                            Log.Default.WriteLine(LogLevels.Error, $"Ability {ability.Name}[{ability.Kind}] returned an invalid attack result: {attackResult}");
                            break;
                    }
                }
                else if (ability.Kind == AbilityKinds.Spell && ability.Behavior == AbilityBehaviors.Harmful)
                {
                    // Miss/Hit/Critical
                    attackResult = CombatHelpers.SpellAttack(source, victim, cannotMiss);
                    switch (attackResult)
                    {
                        case CombatHelpers.AttackResults.Miss:
                            victim.Act(ActOptions.ToCharacter, "{0} misses you.", source);
                            source.Act(ActOptions.ToCharacter, "You miss {0}.", victim);
                            return; // no effect applied
                        case CombatHelpers.AttackResults.Hit:
                        case CombatHelpers.AttackResults.Critical:
                            // effect applied
                            break;
                        default:
                            Log.Default.WriteLine(LogLevels.Error, $"Ability {ability.Name}[{ability.Kind}] returned an invalid attack result: {attackResult}");
                            break;
                    }
                }
                else
                    Log.Default.WriteLine(LogLevels.Error, $"Ability {ability.Name} has an invalid kind: {ability.Kind}");
            }
            // Apply effects
            foreach (AbilityEffect effect in ability.Effects)
                effect.Process(source, victim, ability, attackResult);
        }

        public IAbility Search(IEnumerable<AbilityAndLevel> abilities, int level, CommandParameter parameter)
        {
            return abilities.Where(x =>
                (x.Ability.Flags & AbilityFlags.CannotBeUsed) != AbilityFlags.CannotBeUsed
                && (x.Ability.Flags & AbilityFlags.Passive) != AbilityFlags.Passive
                && x.Level <= level
                && FindHelpers.StringStartsWith(x.Ability.Name, parameter.Value))
                .Select(x => x.Ability)
                .ElementAtOrDefault(parameter.Count - 1);
        }

    }
}
