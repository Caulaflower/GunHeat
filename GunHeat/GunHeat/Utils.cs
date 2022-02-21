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
    public static class Utils
    {
        public static void AddModExt(this Def def, DefModExtension ext)
        {
            if (def.modExtensions == null)
            {
                def.modExtensions = new List<DefModExtension>();
            }

            def.modExtensions.Add(ext);
        }

        public static ThingDef CloneDef(this ThingDef def)
        {
            ThingDef result = new ThingDef();

            FieldInfo[] infos = typeof(ThingDef).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (FieldInfo aids in infos)
            {
                aids.SetValue(result, aids.GetValue(def));
            }

            return result;
        }

        public static void DecreaseOrDestroy(this Thing f, int amount)
        {
            if (f.stackCount <= amount)
            {
                f.Destroy();
            }
            else
            {
                f.stackCount -= amount;
            }
        }

        public static void MakeLMGBarrel()
        {
            var lmgBarrel = ThingDefOf.Steel.CloneDef();

            lmgBarrel.defName = "LMGbarrel";

            lmgBarrel.label = "lmg barrel";

            lmgBarrel.shortHash = (ushort)(DefDatabase<ThingDef>.AllDefs.Last().shortHash + 900);

            lmgBarrel.statBases = new List<StatModifier>();

            lmgBarrel.description = "LMG quick change barrel";

            lmgBarrel.modExtensions = new List<DefModExtension>();

            lmgBarrel.comps = new List<CompProperties>() { new CompProperties { compClass = typeof(HeatOther) } };

            lmgBarrel.AddModExt(new HeatCapExt { BarrelSwapRequiresTools = false, heatDis = 0.15f, heatMax = 600f, shitf = 1.5f, timeToSwap = 2 });

            lmgBarrel.stackLimit = 1;

            lmgBarrel.tickerType = TickerType.Normal;

            lmgBarrel.stuffProps = new StuffProperties();

            lmgBarrel.stuffCategories = new List<StuffCategoryDef>();

            lmgBarrel.mineable = false;

            lmgBarrel.deepCommonality = 0f;

            DefGenerator.AddImpliedDef<ThingDef>(lmgBarrel);

            DefDatabase<ThingDef>.AllDefs.AddItem(lmgBarrel);
        }

        public static ThingDef MakeNewBarrel(this WeaponFamilyDef fed, AmmoSetDef def/*, List<ThingDef> barrelS*/)
        {
            var result = new ThingDef();

            result = ThingDefOf.Steel.CloneDef();

            result.tickerType = TickerType.Normal;

            result.stackLimit = 1;

            result.comps = new List<CompProperties>();

            result.modExtensions = new List<DefModExtension>();

            result.statBases = new List<StatModifier>();

            result.description = "barrel";

            result.defName = "barrel" + fed.defName + def.defName;

            result.label = fed.label + " " + def.label + " barrel";

            result.shortHash = (ushort)(DefDatabase<ThingDef>.AllDefs.Last().shortHash + 900);

            result.comps = new List<CompProperties>() { new CompProperties { compClass = typeof(HeatOther) } };

            result.AddModExt(new HeatCapExt { BarrelSwapRequiresTools = fed.barrelChangeRequiresTools, heatDis = fed.heatdis, heatMax = fed.heatmax, shitf = fed.shift, timeToSwap = fed.BarrelSwapTime/*, barrels = barrelS */});

            DefGenerator.AddImpliedDef<ThingDef>(result);

            DefDatabase<ThingDef>.AllDefs.AddItem(result);

            return result;
        }

        public static float GetValue(this List<StatModifier> me, StatDef stat)
        {
            return me.Find(x => (x?.stat ?? null) == stat)?.value ?? 0f;
        }

        public static void AddOrChangeStat(this List<StatModifier> me, StatModifier stat)
        {
            if (me == null)
            {
                Log.Message("User AddorChangeStat on null list");
                return;
            }

            if (me.Any(x => x.stat == stat.stat))
            {
                me.RemoveAll(x => x.stat == stat.stat);
            }
            me.Add(stat);
        }

        public static ThingDef barrelbase
        {
            get
            {
                return DefDatabase<ThingDef>.AllDefsListForReading.Find(x => x.defName == "LMGbarrel");
            }
        }

        public static StatDef JamStat
        {
            get
            {
                return StatDefOfHeat.reliabilityStatDEf_HEat;
            }
        }
    }
}
