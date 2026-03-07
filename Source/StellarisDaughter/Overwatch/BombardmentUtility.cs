using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace StellarisDaughter
{
    public static class BombardmentUtility
    {
        public static void ExecuteCircularBombardment(Map map, IntVec3 center, AbilityDef ability, CompProperties_AbilityCircularBombardment props)
        {
            if (map == null || props?.skyfallerDef == null)
            {
                return;
            }

            List<IntVec3> cells = GenRadial.RadialCellsAround(center, props.radius, true).InRandomOrder().ToList();
            int launches = 0;
            foreach (IntVec3 cell in cells)
            {
                if (!cell.InBounds(map) || Rand.Value > props.targetSelectionChance)
                {
                    continue;
                }

                Skyfaller skyfaller = SkyfallerMaker.MakeSkyfaller(props.skyfallerDef);
                GenSpawn.Spawn(skyfaller, cell, map);
                launches++;
                if (launches >= props.maxLaunches)
                {
                    break;
                }
            }
        }

        public static void ExecuteStrafeBombardmentDirect(Map map, IntVec3 target, AbilityDef ability, CompProperties_AbilityBombardment props, float angle)
        {
            if (map == null || props?.skyfallerDef == null)
            {
                return;
            }

            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            Vector3 lateral = new Vector3(-direction.z, 0f, direction.x);
            List<IntVec3> cells = new List<IntVec3>();

            for (int l = 0; l < props.bombardmentLength; l++)
            {
                Vector3 rowCenter = target.ToVector3() + direction * l;
                for (int w = -props.bombardmentWidth / 2; w <= props.bombardmentWidth / 2; w++)
                {
                    Vector3 pos = rowCenter + lateral * w;
                    IntVec3 cell = new IntVec3(Mathf.RoundToInt(pos.x), 0, Mathf.RoundToInt(pos.z));
                    if (cell.InBounds(map) && Rand.Value <= props.targetSelectionChance)
                    {
                        cells.Add(cell);
                    }
                }
            }

            cells = cells.InRandomOrder().Take(props.maxTargetCells).ToList();
            foreach (IntVec3 cell in cells)
            {
                Skyfaller skyfaller = SkyfallerMaker.MakeSkyfaller(props.skyfallerDef);
                GenSpawn.Spawn(skyfaller, cell, map);
            }
        }

        public static void ExecuteEnergyLanceDirect(Map map, IntVec3 target, AbilityDef ability, CompProperties_AbilityEnergyLance props, float angle)
        {
            if (map == null)
            {
                return;
            }

            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            Vector3 end = target.ToVector3() + direction * props.moveDistance;
            IntVec3 endCell = new IntVec3(Mathf.RoundToInt(end.x), 0, Mathf.RoundToInt(end.z));
            ThingDef lanceDef = props.energyLanceDef ?? ThingDef.Named("SD_EnergyLance_Base");
            EnergyLance.MakeEnergyLance(lanceDef, target, endCell, map, props.moveDistance, props.useFixedDistance, props.durationTicks, null);
        }
    }
}
