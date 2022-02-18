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
    public class MalfunctionTypeDef : Def
    {
        public float timeToClear = 1;

        public float baseChance = 0f;

        public FloatRange optimalHeat = new FloatRange(0, 70);

        public float minHeat = 0f;

        public bool canBurnShooter = false;

        public bool fieldClearable = true;

        public bool canNeedUnload = false;

        public float unloadChance = 0;


    }
}
