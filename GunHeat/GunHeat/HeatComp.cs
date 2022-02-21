using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CombatExtended;
using Verse;
using UnityEngine;
using HarmonyLib;
using Verse.AI;
using RimWorld;
using System.Reflection;

namespace GunHeat
{
    public class FamilyModExt : DefModExtension
    {
        public WeaponFamilyDef familydef;
    }

    [StaticConstructorOnStartup]
    public class Patcherer  
    {
        static Patcherer()
        {
            var harmony = new Harmony("Caulaflower.GunHeat");

            harmony.PatchAll();

            StatDefOf.Mass.parts.Add(new BarrelWeight());

            CE_StatDefOf.ShotSpread.parts.Add(new AccuracyShift());

            Utils.MakeLMGBarrel();

            ThingDef lmgBarrel = Utils.barrelbase;

            foreach (AmmoDef ammo in DefDatabase<AmmoDef>.AllDefs)
            {
                string[] strs = new string[] { 
                    "fullmetaljacket" ,
                    "hollowpoint",
                    "armorpiercing",
                    "sabot",
                    "incendiaryap",
                    "explosiveap"
                };
                
                switch (ammo?.ammoClass?.defName.ToLower() ?? "")
                {
                    case "fullmetaljacket":
                        ammo.AddModExt(new HeatModExt { heatPerShot = 2f });
                        break;
                    case "hollowpoint":
                        ammo.AddModExt(new HeatModExt { heatPerShot = 2.5f });
                        break;
                    case "armorpiercing":
                        ammo.AddModExt(new HeatModExt { heatPerShot = 2.5f });
                        break;
                    case "sabot":
                        ammo.AddModExt(new HeatModExt { heatPerShot = 4f });
                        break;
                    case "incendiaryap":
                        ammo.AddModExt(new HeatModExt { heatPerShot = 4.5f });
                        break;
                    case "explosiveap":
                        ammo.AddModExt(new HeatModExt { heatPerShot = 4f });
                        break;


                }

                if (!strs.Contains((ammo?.ammoClass?.defName.ToLower() ?? "")))
                {
                    ammo.AddModExt(new HeatModExt { heatPerShot = 2f });
                }
            }



            #region old all lmg patch (obsolete)
            /*foreach (ThingDef gun in DefDatabase<ThingDef>.AllDefs.Where(x => x.weaponTags != null && x.weaponTags.Contains("CE_AI_LMG") | x.weaponTags.Contains("CE_MachineGun")))
            {
                Log.Message(gun.defName);
                gun.comps.Add(new CompProperties { compClass = typeof(HeatComp) });
                gun.AddModExt(new HeatCapExt { heatMax = 600f, heatDis = 0.15f, shitf = 1.5f, barrels = new List<ThingDef> { lmgBarrel }, BarrelSwapRequiresTools = false, timeToSwap = 2 });
                gun.statBases.Add(new StatModifier { stat = Utils.JamStat, value = 0.01f });
                gun.Verbs.Find(x => x is VerbPropertiesCE).verbClass = typeof(ShootWithJam);
                gun.AddModExt(new AdditionalJamInfo { clearMult = 3f });
            }*/
            #endregion
            var rifles = DefDatabase<ThingDef>.AllDefs.Where(x => (x.Verbs?.Count ?? 0) > 0);

            var barrelsAll = new List<ThingDef>();

            foreach (WeaponFamilyDef def in DefDatabase<WeaponFamilyDef>.AllDefs)
            {
                var myrifles = new List<ThingDef>();

                foreach (ThingDef gun in rifles)
                {
                    string truelabel = gun.label.ToLower();

                    if (truelabel.Contains("-"))
                    {
                        truelabel = truelabel.Replace("-", "");
                    }

                    if (def.names.Contains(truelabel) | (def.tags != null && gun.weaponTags != null && def.tags.Intersect(gun.weaponTags).Any()))
                    {
                        Log.Message(gun.label.Colorize(Color.red));

                        myrifles.Add(gun);

                        if (gun.Verbs.Any(x => x.verbClass == typeof(Verb_ShootCE)))
                        {
                            gun.Verbs.Find(x => x is VerbPropertiesCE).verbClass = typeof(ShootWithJam);
                        }

                        if (!gun.comps.Any(x => x.compClass == typeof(HeatComp)))
                        {
                            gun.comps.Add(new CompProperties { compClass = typeof(HeatComp) });
                            gun.AddModExt(new HeatCapExt { heatMax = def.heatmax, heatDis = def.heatdis, shitf = def.shift, BarrelSwapRequiresTools = def.barrelChangeRequiresTools, timeToSwap = def.BarrelSwapTime });
                            gun.statBases.Add(new StatModifier { stat = Utils.JamStat, value = 0.01f });
                        }
                        else
                        {
                            gun.comps.RemoveAll(x => x.compClass == typeof(HeatComp));
                            gun.comps.Add(new CompProperties { compClass = typeof(HeatComp) });
                            gun.AddModExt(new HeatCapExt { heatMax = def.heatmax, heatDis = def.heatdis, shitf = def.shift, BarrelSwapRequiresTools = def.barrelChangeRequiresTools, timeToSwap = def.BarrelSwapTime });
                            gun.statBases.Add(new StatModifier { stat = Utils.JamStat, value = 0.01f });
                        }

                        gun.AddModExt(new FamilyModExt { familydef = def });

                        gun.tickerType = TickerType.Normal;
                    }
                }

                List<ThingDef> barrels = new List<ThingDef>();

                if (def.makeNewBarrel)
                {
                    

                    IEnumerable<IGrouping<AmmoSetDef, ThingDef>> idk = myrifles.GroupBy(x => x.GetCompProperties<CompProperties_AmmoUser>().ammoSet);

                    

                    if (def.curBarrels == null)
                    {
                        def.curBarrels = new List<ThingDef>();
                    }

                    foreach (IGrouping<AmmoSetDef, ThingDef> group in idk)
                    {
                        if (!barrels.Any(x => x.GetModExtension<HeatCapExt>().GeneratedCaliber == group.Key))
                        {
                            var barrelTechs = new List<BarrelType> { BarrelType.heavy, BarrelType.pencil, BarrelType.high_tech_pencil, BarrelType.high_techh_heavy, BarrelType.standard };

                            foreach (BarrelType level in barrelTechs)
                            {
                                var barrel = ThingDefOf.ReinforcedBarrel.CloneDef();

                                barrelsAll.Add(barrel);

                                barrel.statBases = new List<StatModifier>();

                                barrel.comps = new List<CompProperties>();

                                barrel.modExtensions = new List<DefModExtension>();

                                var gunHeatCap = group.Last().GetModExtension<HeatCapExt>();

                                barrels.Add(barrel);

                                #region barrel stat changes
                                if (barrel.statBases == null)
                                {
                                    barrel.statBases = new List<StatModifier>();
                                }

                                float heatMax = gunHeatCap.heatMax;

                                float shift = gunHeatCap.shitf;

                                switch (level)
                                {
                                    case BarrelType.standard:
                                        barrel.statBases.AddOrChangeStat(new StatModifier { stat = StatDefOf.Mass, value = (group.Last().statBases.Find(x => x.stat == StatDefOf.Mass).value / 3.2f) });
                                        break;
                                    case BarrelType.pencil:
                                        heatMax -= (20f * ((int)Math.Ceiling(heatMax / 200)) );
                                        shift += 1f;
                                        barrel.statBases.AddOrChangeStat(new StatModifier { stat = StatDefOf.Mass, value = (group.Last().statBases.Find(x => x.stat == StatDefOf.Mass).value / 5f) });
                                        break;
                                    case BarrelType.heavy:
                                        heatMax += (50f * ((int)Math.Ceiling(heatMax / 200)));
                                        shift -= 1f;
                                        barrel.statBases.AddOrChangeStat(new StatModifier { stat = StatDefOf.Mass, value = (group.Last().statBases.Find(x => x.stat == StatDefOf.Mass).value / 2.5f) });
                                        break;
                                    case BarrelType.high_tech_pencil:
                                        heatMax += (5f * ((int)Math.Ceiling(heatMax / 200)));
                                        shift += 0.25f;
                                        barrel.statBases.AddOrChangeStat(new StatModifier { stat = StatDefOf.Mass, value = (group.Last().statBases.Find(x => x.stat == StatDefOf.Mass).value / 5f) });
                                        break;
                                    case BarrelType.high_techh_heavy:
                                        heatMax += (90f * ((int)Math.Ceiling(heatMax / 200)));
                                        shift -= 1.25f;
                                        barrel.statBases.AddOrChangeStat(new StatModifier { stat = StatDefOf.Mass, value = (group.Last().statBases.Find(x => x.stat == StatDefOf.Mass).value / 3.5f) });
                                        break;

                                }

                                #endregion
                                barrel.AddModExt(new HeatCapExt
                                {
                                    GeneratedCaliber = group.Key,
                                    timeToSwap = gunHeatCap.timeToSwap,
                                    shitf = gunHeatCap.shitf,
                                    BarrelSwapRequiresTools = gunHeatCap.BarrelSwapRequiresTools,
                                    heatMax = heatMax,
                                    heatDis = shift,
                                    barrels = barrels.FindAll(x => (x?.GetModExtension<HeatCapExt>()?.GeneratedCaliber ?? null) == group.Key),
                                    barrelType = level
                                });
                                string name = level.ToString();

                                if (name.Contains("_"))
                                {
                                    name = name.Replace('_', ' ');
                                }


                                barrel.label = name + " " + def.label + " barrel " + group.Key.label;

                                barrel.comps = new List<CompProperties> { new CompProperties { compClass = typeof(HeatOther) } };

                                barrel.defName = level.ToString() + " barrel" + group.Key.defName + group.Key.ammoTypes.First().ammo.defName + def.defName;

                                barrel.shortHash = (ushort)(DefDatabase<ThingDef>.AllDefs.Max(x => x.shortHash) + DefDatabase<ThingDef>.AllDefs.Last().shortHash);

                                DefGenerator.AddImpliedDef(barrel);

                                DefDatabase<ThingDef>.AllDefs.AddItem(barrel);

                                var barrelRecipe = new RecipeDef();

                                barrelRecipe.products = new List<ThingDefCountClass>() { new ThingDefCountClass { count = 1, thingDef = barrel } };

                                barrelRecipe.recipeUsers = new List<ThingDef> { StatDefOfHeat.TableMachining };

                                barrelRecipe.ingredients = new List<IngredientCount>();

                                var filtr = new ThingFilter();

                                filtr.SetAllow(ThingDefOf.Steel, true);

                                filtr.AllowedThingDefs.AddItem(ThingDefOf.Steel);

                                var ingr = new IngredientCount { filter = filtr };

                                switch (level)
                                {
                                    case BarrelType.standard:
                                        ingr.SetBaseCount(25f);
                                        break;
                                    case BarrelType.pencil:
                                        ingr.SetBaseCount(20f);
                                        break;
                                    case BarrelType.heavy:
                                        ingr.SetBaseCount(35f);
                                        break;
                                    case BarrelType.high_tech_pencil:
                                        ingr.SetBaseCount(35f);

                                        var filtr2 = new ThingFilter();

                                        filtr2.SetAllow(ThingDefOf.Plasteel, true);

                                        filtr2.AllowedThingDefs.AddItem(ThingDefOf.Plasteel);

                                        var ingr2 = new IngredientCount { filter = filtr };

                                        ingr2.SetBaseCount(5f);

                                        barrelRecipe.ingredients.Add(ingr2);

                                        break;
                                    case BarrelType.high_techh_heavy:
                                        ingr.SetBaseCount(40f);

                                        var filtr2a = new ThingFilter();

                                        filtr2a.SetAllow(ThingDefOf.Plasteel, true);

                                        filtr2a.AllowedThingDefs.AddItem(ThingDefOf.Plasteel);

                                        var ingr2a = new IngredientCount { filter = filtr };

                                        ingr2a.SetBaseCount(10f);

                                        barrelRecipe.ingredients.Add(ingr2a);

                                        break;

                                }

                                barrelRecipe.ingredients.Add(ingr);

                                barrelRecipe.defaultIngredientFilter = filtr;

                                barrelRecipe.fixedIngredientFilter = filtr;

                                StatDefOfHeat.TableMachining.AllRecipes.Add(barrelRecipe);

                                barrelRecipe.AllRecipeUsers.AddItem(StatDefOfHeat.TableMachining);

                                barrelRecipe.label = "make " + barrel.label;

                                barrelRecipe.recipeUsers = new List<ThingDef> { StatDefOfHeat.TableMachining };

                                StatDefOfHeat.TableMachining.recipes.Add(barrelRecipe);
                            }
                            foreach(ThingDef fed in barrels)
                            {
                                fed.GetModExtension<HeatCapExt>().barrels.AddRange(barrels.FindAll(x =>
                                x.GetModExtension<HeatCapExt>().GeneratedCaliber == fed.GetModExtension<HeatCapExt>().GeneratedCaliber
                                ));
                            }
                        }
                        
                    }

                    def.curBarrels.AddRange(barrels);
                }

               
            }
        }
    }

    public class WeaponFamilyDef : Def
    {
        public List<string> tags;

        public List<string> names;

        public float heatmax;

        public float heatdis;

        public float shift;

        public bool barrelChangeRequiresTools = false;

        public bool makeNewBarrel = true;

        public WeaponFamilyDef tiedFamily;

        public int barrelTypeCount = 1;

        public int BarrelSwapTime = 1;

        public List<ThingDef> curBarrels = new List<ThingDef>();
    }

    #region heat
    public class HeatOther : ThingComp
    {
        public float heat;

        public HeatCapExt ext
        {
            get
            {
                if (capExtInt != null)
                {
                    return capExtInt;
                }
                return this.parent.def.GetModExtension<HeatCapExt>();
            }
        }

        public HeatCapExt capExtInt = null;

        public int ticks;

        public override void CompTick()
        {
            ticks++;
            if (ticks == 10)
            {
                ticks = 0;

                if (heat > 0)
                {
                    heat -= ext.heatDis;
                }
                else
                {
                    heat = 0;
                }

            }
        }
    }

    public class HeatModExt : DefModExtension
    {
        public float heatPerShot = 0f;
    }
    public class HeatCapExt : DefModExtension
    {
        public BarrelType barrelType = BarrelType.standard;

        public float heatMax = 0f;

        public float heatDis = 0f;

        public float shitf = 0f;

        public bool BarrelSwapRequiresTools = true;

        public int timeToSwap = 1;

        public List<ThingDef> barrels;

        public AmmoSetDef GeneratedCaliber;
    }
    #endregion 

    #region comp and gizmos
    public class HeatComp : CompRangedGizmoGiver
    {
        public MalfunctionTypeDef curMalf;

        public float curHeat;

        public float maxHeatInt = 0f;

        public const int tickMax = 5;

        public bool decreaseDisInt = false;

        public HeatCapExt capExt
        {
            get
            {
                if (extInt != null)
                {
                    maxHeatInt = extInt.heatMax;
                    return extInt;
                }
                return this.parent.def.GetModExtension<HeatCapExt>();
            }
        }

        public HeatCapExt extInt;

        public float chanceToJam
        {
            get
            {
                return curHeat / (maxHeat * 4f);
            }
        }

        public float heatDis
        {
            get
            {
                if (heatDisInt < 0.3f)
                {
                    return this.parent.def.GetModExtension<HeatCapExt>().heatDis;
                }
                else
                {
                    decreaseDisInt = false;
                    return heatDisInt;
                }
            }
        }

        public float heatDisInt = 0f;

        public int ticks = tickMax;

        public float maxHeat
        {
            get
            {
                if (maxHeatInt != 0f)
                {
                    return maxHeatInt;
                }
                else
                {
                    return this.parent.def.GetModExtension<HeatCapExt>().heatMax;
                }
            }
        }

        public override string TransformLabel(string label)
        {
            if (curMalf == null)
            {
                return base.TransformLabel(label);
            }
            else if(curMalf.fieldClearable)
            {
                return base.TransformLabel(label) + " (Jammed)";
            }
            else
            {
                return base.TransformLabel(label) + " (Hard Jammed)".Colorize(Color.red);
            }
        }

        public override string GetDescriptionPart()
        {
            if (curMalf == null)
            {
                return base.GetDescriptionPart();
            }
            else if (curMalf.fieldClearable)
            {
                return base.GetDescriptionPart() + " (Jammed, clearable by just racking the action. Reequip and try to fire to do that)";
            }
            else
            {
                return base.GetDescriptionPart() + " (Hard Jammed, haul it to machning bench to fix it)".Colorize(Color.red);
            }        
        }

        public void DoJamAction(bool throwText = false)
        {
            if (!this.curMalf.fieldClearable)
            {
                user.Wielder.equipment.TryTransferEquipmentToContainer(this.parent, user.Wielder.inventory.innerContainer);
            }

            ThingWithComps gun;
            user.Wielder.TryGetComp<CompInventory>().TryFindViableWeapon(out gun);
            if ( (CombatExtended.Utilities.GenClosest.HostilesInRange(user.Wielder.Position, user.Wielder.Map, user.Wielder.Faction, 10).EnumerableNullOrEmpty() | gun == null) && this.curMalf.fieldClearable )
            {
                if (throwText)
                {
                    MoteMaker.ThrowText(user.Wielder.Position.ToVector3(), user.Wielder.Map, curMalf.label);
                }

                
                var newDef = new JobDef { defName = "FixJamJobDef", driverClass = typeof(FixJam), reportString = "clearing malfunction", label = "clearing malfunction", casualInterruptible = false, modExtensions = new List<DefModExtension> { new CombatExtended.JobDefExtensionCE { isCrouchJob = true } } };
                var job = JobMaker.MakeJob(newDef, this.parent);
                user.Wielder.jobs.StartJob(job, JobCondition.InterruptForced);
            }
            else
            {
                user.Wielder.TryGetComp<CompInventory>().TrySwitchToWeapon(gun);
            }
        }

        public float oneorzero(MalfunctionTypeDef fed)
        {
            if (curHeat >= fed.minHeat)
            {
                return 1f;
            }
            return 0f;
        }

        public float ValueForContains(FloatRange source, float chanceAd)
        {
            if (source.Includes(chanceAd))
            {
                return 2f;
            }
            return 1f;
        }

        public CompAmmoUser user
        {
            get
            {
                return this.parent.TryGetComp<CompAmmoUser>();
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            ticks--;
            if (ticks <= 0 && curHeat > 0)
            {
                if (heatDisInt > 0f)
                {
                    heatDisInt -= 0.1f;
                }

                if (curHeat != 0)
                {
                    curHeat -= heatDis;
                }
                ticks = tickMax;
            }




            
        }

        public float jamChance
        {
            get
            {
                float result = 0f;

                if (this.parent.def.statBases.Any(x => x.stat == Utils.JamStat))
                {
                    return this.parent.GetStatValue(Utils.JamStat);
                }

                return result;
            }
        }

        public override void Notify_UsedWeapon(Pawn pawn)
        {
            base.Notify_UsedWeapon(pawn);
            curHeat += (user.CurrentAmmo?.GetModExtension<HeatModExt>()?.heatPerShot ?? 0f);

            if (Rand.Chance(0.5f))
            {
                //Log.Message("test 1");
                if (Rand.Chance(jamChance))
                {
                    //Log.Message("test 2");
                    Func<MalfunctionTypeDef, float> func1 = delegate(MalfunctionTypeDef x)
                    {
                        var result = ((x.baseChance) / (Math.Abs(x.optimalHeat.Average - curHeat))) * oneorzero(x);

                        if (x.optimalHeat.Includes(curHeat))
                        {
                            result *= Rand.Range(1f, 2.1f);
                        }

                        return result;
                    };
                    var malfunction = DefDatabase<MalfunctionTypeDef>.AllDefs.RandomElementByWeightWithFallback
                        (
                           func1
                        );
                    curMalf = malfunction;
                    DoJamAction();
                }
            }

           
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<float>(ref curHeat, "curHeat");
            Scribe_Values.Look<float>(ref maxHeatInt, "maxHeatInt");
            Scribe_Values.Look<float>(ref heatDisInt, "heatDisInt");
            Scribe_Values.Look<bool>(ref decreaseDisInt, "decreaseDisInt");
            Scribe_Defs.Look<ThingDef>(ref replacedBarrel, "curBarrel");

        }

        public NeedDef idfk => DefDatabase<NeedDef>.AllDefsListForReading.Find(x => x.defName == "Bladder");

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (user.Wielder != null)
            {
                #region meme
                if (ModLister.HasActiveModWithName("Dubs Bad Hygiene"))
                {
                    if (user.Wielder.needs.TryGetNeed(idfk).CurLevelPercentage < 0.65f)
                    {
                        yield return new Command_Action
                        {
                            defaultLabel = "piss on the barrel",
                            defaultDesc = "credits to Peyniri",
                            icon = SolidColorMaterials.NewSolidColorTexture(Color.yellow),
                            action = delegate
                            {
                                var piss = DefDatabase<ThingDef>.AllDefsListForReading.Find(x => x.defName == "FilthUrine");

                                FilthMaker.TryMakeFilth(user.Wielder.Position, user.Wielder.Map, piss, Rand.Range(4, 16));

                                FleckMaker.Static(user.Wielder.Position, user.Wielder.Map, FleckDefOf.Smoke, 3f);

                                user.Wielder.needs.TryGetNeed(idfk).CurLevelPercentage = 1f;

                                this.heatDisInt = 2.4f;

                                this.decreaseDisInt = true;
                            }
                        };
                    }

                }
                #endregion

                if (!capExt.BarrelSwapRequiresTools && (user.Wielder.inventory.innerContainer.Any(x => x.def.HasModExtension<HeatCapExt>() && x.def.Verbs.NullOrEmpty())))
                {
                    List<FloatMenuOption> opts = new List<FloatMenuOption>();

                    foreach (Thing ing in user.Wielder.inventory.innerContainer.Where(x => x.def.HasModExtension<HeatCapExt>()))
                    {
                        if (capExt.barrels?.Contains(ing.def) ?? false)
                        {
                            var opt = new FloatMenuOption("swap barrel to " + ing.Label, swapBarrel(ing, user.Wielder));
                            opts.Add(opt);
                        }
                    }

                    if (!opts.NullOrEmpty())
                    {
                        yield return new Command_Action
                        {
                            defaultLabel = "swap barrel",
                            defaultDesc = "swap barrel",
                            icon = ContentFinder<Texture2D>.Get(ThingDefOf.ReinforcedBarrel.graphicData.texPath),
                            action = delegate { Find.WindowStack.Add(new FloatMenu(opts)); }
                        };
                    }

                }
            }


            yield return new GizmoHeat()
            {
                heatComp = this
            };
        }

        public ThingDef replacedBarrel;

        public Action swapBarrel(Thing barrel, Pawn actor)
        {
            return delegate
            {
                JobDef jobdef = new JobDef { driverClass = typeof(SwitchBarrel), reportString = "swapping barrel" };
                var newJob = JobMaker.MakeJob(jobdef, barrel, this.parent);
                newJob.count = 1;
                actor.jobs.StartJob(newJob, JobCondition.InterruptForced);
            };
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            if (Find.CurrentMap.listerThings.AllThings.Any(x => x.def.defName == "TableMachining"))
            {
                if (capExt.BarrelSwapRequiresTools | (!curMalf?.fieldClearable ?? true))
                {
                    yield return new FloatMenuOption("haul to machining bench", delegate { selPawn.jobs.StartJob(new Job(JobDefOf.HaulToCell, targetA: this.parent, targetB:
                        Find.CurrentMap.listerThings.AllThings.Where(x => x.def.defName == "TableMachining").MinBy(y => y.Position.DistanceTo(selPawn.Position)))   {count = 1 }
                        , JobCondition.InterruptForced); });
                }
            }
            

            if (!capExt.BarrelSwapRequiresTools | this.parent.Position.GetThingList(this.parent.Map).Any(x => x.def.defName == "TableMachining"))
            {
                foreach (Thing barrel in Find.CurrentMap.listerThings.AllThings.FindAll(x => x.def.HasModExtension<HeatCapExt>() && x.def.Verbs.NullOrEmpty()))
                {
                    yield return new FloatMenuOption("swap barrel to " + barrel.Label, swapBarrel(barrel, selPawn));
                }
               
            }

            if (this.parent.Position.GetThingList(this.parent.Map).Any(x => x.def.defName == "TableMachining") && (!curMalf?.fieldClearable ?? true))
            {
                yield return new FloatMenuOption("Fix catastrophic malfunction", 
                    delegate
                    {
                        var Job = JobMaker.MakeJob( StatDefOfHeat.FixCatMalfDef, this.parent) ;

                        Job.count = 1;

                        selPawn.jobs.StartJob(Job, JobCondition.InterruptForced);
                    }
                    );
            }


        }

        public bool SpawnBool = false;

        public override void Notify_Equipped(Pawn pawn)
        {
            if (pawn.Faction != Faction.OfPlayer && !SpawnBool)
            {
                if (this.replacedBarrel == null)
                {
                    if (this.parent.def.GetModExtension<FamilyModExt>() != null)
                    {
                        replacedBarrel = this.parent.def.GetModExtension<FamilyModExt>().familydef.curBarrels.FindAll(x => (x?.GetModExtension<HeatCapExt>()?.GeneratedCaliber ?? null) == user.Props.ammoSet).RandomElement();

                        extInt = replacedBarrel.GetModExtension<HeatCapExt>();
                    }
                }

            }
            SpawnBool = true;
            base.Notify_Equipped(pawn);
        }

        public override void PostPostMake()
        {
            if (this.replacedBarrel == null)
            {
                if (this.parent.def.GetModExtension<FamilyModExt>() != null)
                {
                    replacedBarrel = this.parent.def.GetModExtension<FamilyModExt>().familydef.curBarrels.FindAll(x => (x?.GetModExtension<HeatCapExt>()?.GeneratedCaliber ?? null) == user.Props.ammoSet
                    && (x?.GetModExtension<HeatCapExt>()?.barrelType ?? null) == BarrelType.standard
                    ).RandomElement();

                    extInt = replacedBarrel.GetModExtension<HeatCapExt>();
                }


            }

            base.PostPostMake();
        }

    }

    public class GizmoHeat : Gizmo
    {
        private static bool initialized;
        private static Texture2D FullTex;
        private static Texture2D EmptyTex;
        private static Texture2D BGTex;

        public HeatComp heatComp;

        public GizmoHeat()
        {
            if (!initialized)
            {
                InitializeTextures();
                initialized = true;
            }
        }
        public override float GetWidth(float maxWidth)
        {
            return 120f;
        }

        public Texture2D textD
        {
            get
            {
                var result = SolidColorMaterials.NewSolidColorTexture(Color.yellow);

                if (heatComp.curHeat > (heatComp.maxHeat / 4f))
                {
                    result = SolidColorMaterials.NewSolidColorTexture(new Color(90, 90, 0));
                }

                if (heatComp.curHeat > (heatComp.maxHeat / 3f))
                {
                    result = SolidColorMaterials.NewSolidColorTexture(new Color(130, 90, 0));
                }

                if (heatComp.curHeat > (heatComp.maxHeat / 2f))
                {
                    result = SolidColorMaterials.NewSolidColorTexture(Color.red);
                }


                return result;
            }
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect backgroundRect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), Height);

            Rect inRect = backgroundRect.ContractedBy(6);
            GUI.DrawTexture(backgroundRect, BGTex);

            Text.Font = GameFont.Tiny;
            Rect textRect = inRect.TopHalf();
            Widgets.Label(textRect, "Current heat: ");

            Rect barRect = inRect.BottomHalf();
            Widgets.FillableBar(barRect, heatComp.curHeat / heatComp.maxHeat, textD);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(barRect, (int)heatComp.curHeat + " / " + (int)heatComp.maxHeat);
            Text.Anchor = TextAnchor.UpperLeft;

            return new GizmoResult(GizmoState.Clear);
        }

        private void InitializeTextures()
        {
            if (FullTex == null)
                FullTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.2f, 0.24f));
            if (EmptyTex == null)
                EmptyTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);
            if (BGTex == null)
                BGTex = ContentFinder<Texture2D>.Get("UI/Widgets/DesButBG", true);
        }
    }
    #endregion

    public class SwitchBarrel : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            if (TargetB.Thing.Position != TargetA.Thing.Position)
            {
                yield return Toils_Goto.Goto(TargetIndex.A, PathEndMode.ClosestTouch);
                yield return Toils_Haul.StartCarryThing(TargetIndex.A);
                yield return Toils_Haul.CarryHauledThingToCell(TargetIndex.B);
            }

            yield return Toils_General.Wait(this.TargetB.Thing.TryGetComp<HeatComp>().capExt.timeToSwap * 60).WithProgressBarToilDelay(TargetIndex.B);

            yield return Toils_General.Do(delegate
            {
                if (this.TargetB.Thing.TryGetComp<HeatComp>().replacedBarrel != null)
                {
                    Thing thing = ThingMaker.MakeThing(this.TargetB.Thing.TryGetComp<HeatComp>().replacedBarrel);
                    thing.stackCount = 1;
                    if (thing.TryGetComp<HeatOther>() != null)
                    {
                        thing.TryGetComp<HeatOther>().heat = this.TargetB.Thing.TryGetComp<HeatComp>().curHeat;
                    }
                    this.GetActor().inventory.TryAddItemNotForSale(thing);
                }
                this.TargetB.Thing.TryGetComp<HeatComp>().extInt = TargetA.Thing.TryGetComp<HeatOther>().ext;
                this.TargetB.Thing.TryGetComp<HeatComp>().curHeat = TargetA.Thing.TryGetComp<HeatOther>().heat;
                this.TargetA.Thing.DecreaseOrDestroy(1);
                this.TargetB.Thing.TryGetComp<HeatComp>().replacedBarrel = this.TargetA.Thing.def;
            });
        }
    }

    public class FixCatMalf : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.Goto(TargetIndex.A, PathEndMode.ClosestTouch);

            var Toil2 = Toils_General.Wait(480);

            Toil2.AddFinishAction
                (
                delegate
                {
                    TargetA.Thing.TryGetComp<HeatComp>().curMalf = null;
                }
                );

            yield return Toil2;
        }
    }

    #region jamming

    public class AdditionalJamInfo : DefModExtension
    {
        public float clearMult = 1f;
    }

    [HarmonyPatch(typeof(Pawn), "Tick")]
    static class PostFixTickStuffThing
    {
        public static void Postfix(Pawn __instance)
        {
            if (__instance == null | (!__instance.def?.race?.Humanlike ?? true) | !(__instance.ParentHolder is Map) | __instance.Faction != Faction.OfPlayer)
            {
                return;
            }

            if ((__instance.equipment?.Primary?.TryGetComp<HeatComp>() ?? null) != null)
            {
                __instance.equipment.Primary.TryGetComp<HeatComp>().CompTick();
            }


        }
    }

    public class FixJam : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        public CompAmmoUser user
        {
            get
            {
                return TargetA.Thing.TryGetComp<CompAmmoUser>();
            }
        }

        public HeatComp heatcomp
        {
            get
            {
                return TargetA.Thing.TryGetComp<HeatComp>();
            }
        }

        public MalfunctionTypeDef malfDef
        {
            get
            {
                return TargetA.Thing.TryGetComp<HeatComp>().curMalf;
            }
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            //yield return Toils_General.Do(delegate { ); } );

            bool unloaded = false;

            if (malfDef.canNeedUnload && Rand.Chance(malfDef.unloadChance))
            {
                yield return Toils_General.Do(delegate
                {
                    user.TryUnload();
                    unloaded = true;
                });
            }

          

            int timeToClear = (int)(malfDef.timeToClear * 30);

            if (TargetA.Thing.def.HasModExtension<AdditionalJamInfo>())
            {
                timeToClear = (int)(timeToClear * TargetA.Thing.def.GetModExtension<AdditionalJamInfo>().clearMult);
            }

            if (GetActor().skills.GetSkill(SkillDefOf.Shooting).Level != 0)
            {
                timeToClear = (int)Math.Max( (GetActor().skills.GetSkill(SkillDefOf.Shooting).Level / 10f) * timeToClear, 1 );
            }


            var waitToil = Toils_General.Wait(timeToClear).WithProgressBarToilDelay(TargetIndex.A);

            yield return waitToil;

            if (unloaded)
            {
                user.LoadAmmo(GetActor().TryGetComp<CompInventory>().ammoList.RandomElement());
            }

            yield return Toils_General.Do(delegate { heatcomp.curMalf = null; });
        }
    }

    public class AccuracyShift : StatPart
    {
        public HeatComp source(StatRequest req)
        {
            return req.Thing?.TryGetComp<HeatComp>() ?? null;
        }

        public float valueMult(StatRequest req)
        {
            return Math.Max((float)Math.Round((source(req).capExt.shitf * ((source(req).curHeat * 1.35f) / (source(req).maxHeat))), 2), 1f);
        }

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (source(req) != null)
            {
                val *= valueMult(req);
            }
        }
        public override string ExplanationPart(StatRequest req)
        {
            if (source(req) != null)
            {
                return "Innacuray multiplier due to heat: " + valueMult(req).ToString();
            }
            else
            {
                return "";
            }
        }

    }

    public class JamChance : StatPart
    {
        public HeatComp HeatComp(StatRequest reg)
        {
            HeatComp result = null;

            if (reg.Thing != null && reg.Thing.TryGetComp<HeatComp>() != null)
            {
                return reg.Thing.TryGetComp<HeatComp>();
            }

            return result;
        }

        public float mult(StatRequest req)
        {
            var comp = (HeatComp(req));
            return (float)(Math.Round(comp.curHeat / comp.maxHeat));
        }

        public float oneorzero(MalfunctionTypeDef fed, float curHeat)
        {
            if (curHeat >= fed.minHeat)
            {
                return 1f;
            }
            return 0f;
        }

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (HeatComp(req) != null)
            {
                var comp = (HeatComp(req));
                var heatChance = mult(req);


                val *= heatChance;

                Func<MalfunctionTypeDef, float> func1 = delegate (MalfunctionTypeDef x)
                {
                    var result = ((x.baseChance) / (Math.Abs(x.optimalHeat.Average - comp.curHeat))) * oneorzero(x, comp.curHeat);

                    if (x.optimalHeat.Includes(comp.curHeat))
                    {
                        result *= Rand.Range(1.5f, 2f);
                    }

                    return result;
                };

                val += (DefDatabase<MalfunctionTypeDef>.AllDefs.Max(x => 1 / (Math.Abs(x.optimalHeat.Average - HeatComp(req).curHeat))) / 100f);

              

                if(comp.curHeat > comp.maxHeat)
                {
                    val += (comp.curHeat - comp.maxHeat) * 0.05f;
                }
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (HeatComp(req) != null)
            {
                return "Overheating: " + mult(req) + "\n" + " most possible malfunction: " + DefDatabase<MalfunctionTypeDef>.AllDefs.MaxBy(x => 1 / (Math.Abs(x.optimalHeat.Average - HeatComp(req).curHeat))).label.Colorize(Color.cyan);
            }
            return null;
        }
    }

    public class ShootWithJam : Verb_ShootCE
    {
        public HeatComp jamsource
        {
            get
            {
                return this.EquipmentSource.TryGetComp<HeatComp>();
            }
        }
        public override bool TryCastShot()
        {
            if (jamsource.curMalf != null)
            {
                if (!jamsource.curMalf.fieldClearable)
                {
                    MoteMaker.ThrowText(CasterPawn.Position.ToVector3(), CasterPawn.Map, "Fuck, my gun is dead");
                }
                jamsource.DoJamAction(true);
                return false;
            }

            return base.TryCastShot();
        }
    }

    public class BarrelWeight : StatPart
    {
        public HeatComp HeatComp(StatRequest reg)
        {
            HeatComp result = null;

            if (reg.Thing != null && reg.Thing.TryGetComp<HeatComp>() != null)
            {
                return reg.Thing.TryGetComp<HeatComp>();
            }

            return result;
        }

        public override void TransformValue(StatRequest req, ref float val)
        {

            var comp = HeatComp(req);
            if (comp != null && comp.replacedBarrel != null)
            {
                val /= 1.5f;
                val += value(req);
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            var comp = HeatComp(req);
            if (comp != null && comp.replacedBarrel != null)
            {
                //Log.Message(.ToString().Colorize(Color.red));
                //return "Barrel mass: " + comp.replacedBarrel.statBases.GetValue(StatDefOf.Mass).ToString().Colorize(Color.green) + "\n" + "Barrel: " + comp.replacedBarrel.label;
                return "Barrel mass: " + value(req) + "\n" + "Barrel: " + comp.replacedBarrel.label;
            }
            return null;
        }

        public float value (StatRequest req)
        {
            var comp = HeatComp(req);
            return ThingMaker.MakeThing(comp.replacedBarrel).GetStatValue(StatDefOf.Mass);
        }
    }

    #endregion
}
