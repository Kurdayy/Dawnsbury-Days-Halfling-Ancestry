using System;
using System.Collections.Generic;
using System.Linq;
using Dawnsbury.Audio;
using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Modding;
using Dawnsbury.Core;
using System.Threading.Tasks;


namespace Dawnsbury.Mods.Ancestries.Halfling;

public class HalflingAncestryLoader
{

    public static Trait HalflingTrait;
    public static Trait ModTrait;

    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        HalflingTrait = ModManager.RegisterTrait(
        "Halfling",
        new TraitProperties("Halfling", true)
        {
            IsAncestryTrait = true
        });
        ModTrait = ModManager.RegisterTrait(
        "Halfling Ancestry",
        new TraitProperties("Halfling Ancestry", true));


        AddFeats(CreateHalflingAncestryFeats());
        RegisterNewWeapons();
        RegisterNewBackgrounds();

        ModManager.AddFeat(new AncestrySelectionFeat(
            FeatName.CustomFeat,
            "Claiming no place as their own, halflings control few settlements larger than villages. Instead, they frequently live among humans within the walls of larger cities, carving out small communities alongside taller folk. Many halflings lead perfectly fulfilling lives in the shadows of their larger neighbors, while others prefer a nomadic existence, traveling the world and taking advantage of opportunities and adventures as they come.",
            new List<Trait> { Trait.Humanoid, HalflingTrait, ModTrait },
            6,
            5,
            new List<AbilityBoost>()
            {
                new EnforcedAbilityBoost(Ability.Dexterity),
                new EnforcedAbilityBoost(Ability.Wisdom),
                new FreeAbilityBoost()
            },
            CreateHalflingHeritages().ToList())
            .WithAbilityFlaw(Ability.Strength)
            .WithCustomName("Halfling")
        );


    }

    private static void AddFeats(IEnumerable<Feat> feats)
    {
        foreach (Feat feat in feats)
        {
            ModManager.AddFeat(feat);
        }
    }


    public static void RegisterNewWeapons()
    {

        ModManager.RegisterNewItemIntoTheShop("Halfling Sling Staff HA", itemName =>
            new Item(itemName, IllustrationName.Quarterstaff, "Halfling Sling Staff", 0, 5, Trait.TwoHanded, Trait.Reload1, Trait.Propulsive, Trait.Martial, HalflingTrait, ModTrait)
                .WithWeaponProperties(new WeaponProperties("1d10", DamageKind.Bludgeoning)
                .WithRangeIncrement(16)));

        ModManager.RegisterNewItemIntoTheShop("Frying Pan HA", itemName =>
            new Item(itemName, IllustrationName.LightMace, "Frying Pan", 0, 0, Trait.FatalD8, Trait.Simple, HalflingTrait, ModTrait)
                .WithWeaponProperties(new WeaponProperties("1d4", DamageKind.Bludgeoning)));

        ModManager.RegisterNewItemIntoTheShop("Filcher's Fork HA", itemName =>
            new Item(itemName, IllustrationName.Spear, "Filcher's Fork", 0, 1, Trait.Agile, Trait.Backstabber, Trait.DeadlyD6, Trait.Finesse, Trait.Thrown20Feet, Trait.Martial, HalflingTrait, ModTrait)
                .WithWeaponProperties(new WeaponProperties("1d4", DamageKind.Piercing)
                .WithRangeIncrement(4)
                ));

    }


    public static void RegisterNewBackgrounds()
    {
        /*
        ModManager.AddFeat(
        new BackgroundSelectionFeat(FeatName.CustomFeat,
            "You have sexy feet.",
            "You're trained in {b}Acrobatics{/b}.",
            new List<AbilityBoost>
            {
                new LimitedAbilityBoost(Ability.Dexterity, Ability.Charisma),
                new FreeAbilityBoost()
            }).WithOnSheet((sheet) =>
            {
                sheet.GrantFeat(FeatName.Acrobatics);
            })
            .WithCustomName("Halfling Of The Night")
            );
        */
    }



    public Func<QEffect, Creature, DamageStuff, Creature, Task<DamageModification?>>? AfterSavingThrow { get; set; }

    

    private static IEnumerable<Feat> CreateHalflingAncestryFeats()
    {



        yield return new HalflingAncestryFeat(
                "Halfing Luck",
                "Your happy-go-lucky nature makes it seem like misfortune avoids you, and to an extent, that might even be true.",
                "You can choose to roll a save or skill check twice and take the better result once per encounter.")
            .WithPrerequisite(sheet =>
            {
                Feat ft = sheet.AllFeats.Find(ft => ft.Name == "Jinxed Halfling");
                if (ft != null) return false;
                else return true;

            }, "Cannot have the Jinxed Halfling heritage."
            )
            .WithOnCreature(creature =>
            {

                creature.AddQEffect(new QEffect("Halfling Luck", "You can choose to roll a save or skill check twice and take the better result once per encounter.")
                {
                    AdjustSavingThrowResult = (qfSelf, action, check) =>
                    {
                        return CheckResult.Success;
                    },
                    BeforeYourSavingThrow = async (qfSelf, action, enemy) =>
                    {
                        Creature halfling = qfSelf.Owner;
                        creature.Battle.Log("Test a save.");
                        if (halfling.PersistentUsedUpResources.UsedUpActions.Contains("HalflingLuck")) return;

                        if (await creature.Battle.AskForConfirmation(creature, IllustrationName.BitOfLuck, "Use Halfling Luck to roll the save twice?", "Yes", "No"))
                        {
                            creature.AddQEffect(new QEffect("Halfling Luck", "Next save will be rolled twice.", ExpirationCondition.Ephemeral, null, IllustrationName.BitOfLuck) { ProvideFortuneEffect = (fortune) => "Halfling Luck" });
                            halfling.PersistentUsedUpResources.UsedUpActions.Add("HalflingLuck");

                        }
                    },
                    ProvideMainAction = (qfSelf) =>
                    {
                        if (qfSelf.Owner.PersistentUsedUpResources.UsedUpActions.Contains("HalflingLuck")) return null;


                        return new ActionPossibility(new CombatAction(creature, IllustrationName.BitOfLuck, "Halfling Luck", new Trait[] { Trait.Fortune, HalflingTrait },
                            "Cause your next skill check to be rolled twice and take the better result.",
                            Target.Self())
                            .WithActionCost(0)
                            .WithSoundEffect(SfxName.BitOfLuck)
                            .WithEffectOnChosenTargets(async (spell, caster, targets) =>
                            {
                                qfSelf.Owner.PersistentUsedUpResources.UsedUpActions.Add("HalflingLuck");

                                caster.AddQEffect(new QEffect("Halfling Luck", "Next check will be rolled twice.", ExpirationCondition.Never, null, IllustrationName.BitOfLuck)
                                {
                                    BeforeYourActiveRoll = async (qfSelf, action, target) =>
                                    {
                                        ActionId act = action.ActionId;
                                        if (act == ActionId.Demoralize || act == ActionId.CreateADiversion || act == ActionId.Hide || act == ActionId.Seek || act == ActionId.Administer)
                                        {
                                            creature.AddQEffect(new QEffect(ExpirationCondition.Ephemeral) { ProvideFortuneEffect = (fortune) => "Halfling Luck" });
                                            qfSelf.ExpiresAt = ExpirationCondition.Immediately;
                                        }
                                    }
                                });
                            }
                        ));
                    },
                    StartOfCombat = async (qfSelf) =>
                    {
                        if (qfSelf.Owner.PersistentUsedUpResources.UsedUpActions.Contains("HalflingLuck")) qfSelf.Owner.PersistentUsedUpResources.UsedUpActions.Remove("HalflingLuck");
                    }
                });

            });
            

        yield return new HalflingAncestryFeat(
                "Unfettered Halfling",
                "You were forced into service as a laborer, either pressed into indentured servitude or shackled by the evils of slavery, but you've since escaped and have trained to ensure you'll never be caught again.",
                "Whenever you roll a success on a check to Escape, you get a critical success instead. When a creature attempts to Grab you, it must succeed at an Athletics check to grab you instead of automatically grabbing you.")
            .WithOnCreature(creature =>
            {

                creature.AddQEffect(new QEffect("Unfettered Halfling", "Whenever you roll a success on a check to Escape, you get a critical success instead. When a creature attempts to Grab you, it must succeed at an Athletics check to grab you instead of automatically grabbing you.")
                {
                    //Counter Grabs
                    YouAcquireQEffect = (qf, qf2) =>
                    {
                        if (qf2.Id == QEffectId.Grappled)
                        {
                            creature.Battle.Log(creature.Name + " attempts to dodge " + qf2.Owner.Name + "'s grab!");
                            CheckResult check = CommonSpellEffects.RollCheck("Grapple", new ActiveRollSpecification(Checks.SkillCheck(Skill.Athletics), Checks.DefenseDC(Defense.Fortitude)), qf2.Owner, creature);
                            if (check == CheckResult.Failure || check == CheckResult.CriticalFailure)
                            {
                                //await creature.StrideAsync()
                                return new QEffect("Dodged", "Dodged it!", ExpirationCondition.ExpiresAtStartOfYourTurn, null, IllustrationName.Seek);
                            }
                            else return qf2;
                        }

                        return qf2;
                    },
                    //Critical Escape
                    AfterYouTakeAction = async (qf, action) =>
                    {
                        if(action.ActionId == ActionId.Escape && (action.CheckResult == CheckResult.Success || action.CheckResult == CheckResult.CriticalSuccess))
                        {
                            QEffect effect = qf.Owner.QEffects.FirstOrDefault(effect => effect.Name== "Grabbed", null);
                            if (effect != null) {
                                foreach (QEffect ef in qf.Owner.QEffects){
                                    creature.Battle.Log("QF -> " + ef.Name);
                                }
                                qf.Owner.RemoveAllQEffects(effect => effect.Name == "Grabbed" || effect.Name == "Grappled" || effect.Name == "Immobilized" || effect.Name == "Flanked");
                                creature.Battle.Log("ESCAPED");
                            }

                            foreach (QEffect ef in qf.Owner.QEffects)
                            {
                                creature.Battle.Log("QF Post Mortem -> " + ef.Name);
                            }

                            await qf.Owner.StrideAsync("Free 5 ft stride", false, true, null, true, true, false);
                        }
                    }
                });

                /*
                 *  Test Function to inflict grappled.
                 * 
                creature.AddQEffect(new QEffect("Grabbler", "Grabble Them")
                {
                    ProvideMainAction = (qfSelf) =>
                    {
                        return new ActionPossibility(new CombatAction(creature, IllustrationName.Grab, "GRAPPLE THEM", new Trait[] { Trait.Athletics, HalflingTrait },
                            "Cause your next skill check to be rolled twice and take the better result.",
                            Target.RangedCreature(10))
                            .WithActionCost(1)
                            .WithEffectOnEachTarget(async (spell, caster, target, result) =>
                            {
                                await Possibilities.Grapple(caster, target, CheckResult.Success);
                            })
                        );
                    }
                });
                */

            });

        

        yield return new HalflingAncestryFeat(
                "Innocuous",
                "Halflings have been unobtrusive assistants of larger folk for untold ages, and your people count on this assumption of innocence.",
                "You gain the trained proficiency rank in Deception.")
            .WithOnSheet((sheet) =>
            {
                sheet.GrantFeat(FeatName.Deception);
            });


        yield return new HalflingAncestryFeat(
                "Halfling Lore",
                "You've dutifully learned how to keep your balance and how to stick to the shadows where it's safe, important skills passed down through generations of halfling tradition.",
                "You gain the trained proficiency rank in Acrobatics and Stealth. If you would automatically become trained in one of those skills (from your background or class, for example), you instead become trained in a skill of your choice.")
            .WithOnSheet((sheet) =>
            {
                sheet.GrantFeat(FeatName.Acrobatics);
                sheet.GrantFeat(FeatName.Stealth);
            });



        yield return new HalflingAncestryFeat(
                "Halfling Weapon Familiarity",
                "You favor traditional halfling weapons, so you've learned how to use them more effectively.",
                "You have familiarity with weapons with the halfling trait\n For the purposes of proficiency, you treat any of these that are martial weapons as simple weapons and any that are advanced weapons as martial weapons.")
            .WithOnSheet(sheet =>
            {
                sheet.SetProficiency(Trait.Sword, Proficiency.Trained);
                sheet.Proficiencies.AddProficiencyAdjustment(traits => traits.Contains(HalflingTrait) && traits.Contains(Trait.Martial), Trait.Simple);
            });
        
    }


    private static IEnumerable<Feat> CreateHalflingHeritages()
    {
        // Gutsy
        yield return new HeritageSelectionFeat(FeatName.CustomFeat,
            "Your family line is known for keeping a level head and staving off fear when the chips were down.",
            "When you roll a success on a saving throw against an emotion effect, you get a critical success instead.")
            .WithCustomName("Gutsy Halfling")
            .WithOnCreature((creature) =>
            {
                creature.AddQEffect(new QEffect("Gutsy Halfling",
                    "When you roll a success on a saving throw against an emotion effect, you get a critical success instead.",
                    ExpirationCondition.Never,
                    null,
                    IllustrationName.None)
                {
                    Innate = true,
                    AdjustSavingThrowResult = (QEffect qf, CombatAction action, CheckResult originalResult) => (action.HasTrait(Trait.Emotion) && originalResult == CheckResult.Success) ? CheckResult.CriticalSuccess : originalResult
                });
            });

        // free picker
        /*
        yield return new HeritageSelectionFeat(FeatName.CustomFeat,
            "You are unique among your kind in your capabilities.",
            "You may choose any 2 ability boosts instead of the usual ancestry boosts.")
            .WithCustomName("Unusual Halfling")
            .WithOnSheet(sheet =>
            {
                sheet.AbilityBoostsFabric.AbilityFlaw = null;
                sheet.AbilityBoostsFabric.AncestryBoosts =
                    new List<AbilityBoost>
                    {
                        new FreeAbilityBoost(),
                        new FreeAbilityBoost()
                    };
            });
        */
        
        // Observant
        yield return new HeritageSelectionFeat(FeatName.CustomFeat,
            "Your finely honed senses quickly clue you in to danger or trickery.",
            "You gain a +1 circumstance bonus to your Perception.")
            .WithCustomName("Observant Halfling")
            .WithOnCreature((sheet, creature) =>
            {
                creature.AddQEffect(new QEffect("Observant Halfling", "Your finely honed senses quickly clue you in to danger or trickery. You gain a +1 circumstance bonus to your Perception.", ExpirationCondition.Never, null, IllustrationName.None)
                {
                    Innate = true,
                    BonusToDefenses = (QEffect qf, CombatAction attackingAction, Defense defense) => (defense != Defense.Perception) ? null : new Bonus(1, BonusType.Untyped, "Observant Halfling")
                });

            });

        //Wildwood
        yield return new HeritageSelectionFeat(FeatName.CustomFeat,
            "You hail from deep within a jungle or forest, and you've learned how to use your small size to wriggle through undergrowth and other obstacles.",
            "You ignore difficult terrain.")
            .WithCustomName("Wildwood Halfling")
            .WithOnCreature((sheet, creature) =>
            {
               creature.AddQEffect(new QEffect("Wildwood Halfling", "You ignore difficult terrain.")
               {
                   Id = QEffectId.IgnoresDifficultTerrain
               });
            });

        // Jinxed
        yield return new HeritageSelectionFeat(FeatName.CustomFeat,
            "You were born with a strange blessing: bereft of the typical halfling luck, you can instead manipulate the fortunes of others.",
            "You can never take the Halfling Luck feat, and you gain the Jinx action.")
            .WithCustomName("Jinxed Halfling")
            .WithOnCreature((sheet, creature) => 
            {
            creature.AddQEffect(new QEffect("Halfling Jinx", "You were born with a strange blessing: bereft of the typical halfling luck, you can instead manipulate the fortunes of others.")
            {
                ProvideMainAction = (qfSelf) =>
                {

                    var halfling = qfSelf.Owner;
                    if (halfling.PersistentUsedUpResources.UsedUpActions.Contains("HalflingJinx")) return null;

                    int dcClass = halfling.PersistentCharacterSheet!.Class != null ? halfling.Proficiencies.Get(halfling.PersistentCharacterSheet.Class.ClassTrait).ToNumber(halfling.Level)
                                                                                  + halfling.Abilities.Get(halfling.Abilities.KeyAbility) + 10 : 10;
                    int dcSpell = halfling.Spellcasting != null ? halfling.Proficiencies.Get(Trait.Spell).ToNumber(halfling.Level) + 10 + halfling.Spellcasting.Sources.Max(source => source.SpellcastingAbilityModifier) : 10;
                    int dc = Math.Max(dcClass, dcSpell);

                    return new ActionPossibility(new CombatAction(halfling, IllustrationName.Fear, "Jinx", new Trait[] { Trait.Curse, Trait.Occult },
                            "You can curse another creature with clumsiness. This curse has a range of 30 feet, and you must be able to see your target. The target gets a Will saving throw against your class DC or spell DC, whichever is higher.",
                            Target.RangedCreature(6))
                        .WithActionCost(2)
                        .WithSoundEffect(SfxName.Fear)
                        .WithSavingThrow(new SavingThrow(Defense.Will, (caster) => dc))
                        .WithEffectOnEachTarget(async (spell, caster, target, result) =>
                        {
                            switch (result)
                            {
                                case CheckResult.Failure:
                                    target.AddQEffect(QEffect.Clumsy(1).WithExpirationAtStartOfSourcesTurn(caster, 10)); break;
                                case CheckResult.CriticalFailure:
                                    target.AddQEffect(QEffect.Clumsy(2).WithExpirationAtStartOfSourcesTurn(caster, 10)); break;
                                default:
                                    break;
                            }
                        })
                        .WithEffectOnChosenTargets(async (spell, caster, targets) =>
                        {
                            creature.PersistentUsedUpResources.UsedUpActions.Add("HalflingJinx");
                        }))
                    {
                        PossibilityGroup = Constants.POSSIBILITY_GROUP_ADDITIONAL_NATURAL_STRIKE
                    };

                }
            });
            });

    }

}

public class HalflingAncestryFeat : TrueFeat
{
    public HalflingAncestryFeat(string name, string flavorText, string rulesText)
        : base(FeatName.CustomFeat, 1, flavorText, rulesText, new Trait[2] { HalflingAncestryLoader.HalflingTrait, HalflingAncestryLoader.ModTrait })
    {
        WithCustomName(name);
    }
}