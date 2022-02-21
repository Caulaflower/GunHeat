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
    [DefOf]
    public class StatDefOfHeat : DefOf
    {
        public static StatDef reliabilityStatDEf_HEat;

        public static ThingDef TableMachining;

        public static JobDef FixCatMalfDef;
    }
}
