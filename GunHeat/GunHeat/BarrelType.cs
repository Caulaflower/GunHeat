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
    public enum BarrelType : byte
    {
        standard,
        pencil,
        heavy,
        high_tech_pencil,
        high_techh_heavy
    }
}
