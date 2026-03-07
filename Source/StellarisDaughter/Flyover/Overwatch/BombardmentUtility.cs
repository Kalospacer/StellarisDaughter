using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace StellarisDaughter
{
    public static class BombardmentUtility
    {
        public static string ExecuteCircularBombardment(Map map, IntVec3 targetCell, AbilityDef def, CompProperties_AbilityCircularBombardment props, Dictionary<string, object> parsed = null)
        {
            if (props.skyfallerDef == null)
            {
                return $"Error: '{def.defName}' has no skyfallerDef.";
            }

            bool filter = true;
            if (TryGetBool(parsed, "filterFriendlyFire", out bool filterFriendlyFire))
            {
                filter = filterFriendlyFire;
            }

            List<IntVec3> selectedTargets = SelectTargetCells(map, targetCell, props, filter);
            if (selectedTargets.Count == 0)
            {
                return $"Error: No valid target cells near {targetCell}.";
            }

            bool isPaused = Find.TickManager != null && Find.TickManager.Paused;
            int totalLaunches = ScheduleBombardment(map, selectedTargets, props, isPaused);
            return $"Success: Scheduled Circular Bombardment '{def.defName}' at {targetCell}. Launches: {totalLaunches}/{props.maxLaunches}.";
        }

        public static string ExecuteStrafeBombardment(Map map, IntVec3 targetCell, AbilityDef def, CompProperties_AbilityBombardment props, Dictionary<string, object> parsed = null)
        {
            if (props.skyfallerDef == null)
            {
                return $"Error: '{def.defName}' has no skyfallerDef.";
            }

            ParseDirectionInfo(parsed, targetCell, props.bombardmentLength, true, out Vector3 direction, out _);
            List<IntVec3> targetCells = CalculateBombardmentAreaCells(map, targetCell, direction, props.bombardmentWidth, props.bombardmentLength);
            if (targetCells.Count == 0)
            {
                return $"Error: No valid targets found for strafe at {targetCell}.";
            }

            List<IntVec3> selectedCells = new List<IntVec3>();
            List<IntVec3> missedCells = new List<IntVec3>();
            foreach (IntVec3 cell in targetCells)
            {
                if (Rand.Value <= props.targetSelectionChance)
                {
                    selectedCells.Add(cell);
                }
                else
                {
                    missedCells.Add(cell);
                }
            }

            if (selectedCells.Count < props.minTargetCells && missedCells.Count > 0)
            {
                int needed = props.minTargetCells - selectedCells.Count;
                selectedCells.AddRange(missedCells.InRandomOrder().Take(Math.Min(needed, missedCells.Count)));
            }
            else if (selectedCells.Count > props.maxTargetCells)
            {
                selectedCells = selectedCells.InRandomOrder().Take(props.maxTargetCells).ToList();
            }

            if (selectedCells.Count == 0)
            {
                return $"Error: No cells selected for strafe after chance filter.";
            }

            Dictionary<int, List<IntVec3>> rows = OrganizeIntoRows(targetCell, direction, selectedCells);
            MapComponent_SkyfallerDelayed delayed = map.GetComponent<MapComponent_SkyfallerDelayed>();
            if (delayed == null)
            {
                delayed = new MapComponent_SkyfallerDelayed(map);
                map.components.Add(delayed);
            }

            int now = Find.TickManager.TicksGame;
            int startTick = now + props.warmupTicks;
            int totalScheduled = 0;

            foreach (KeyValuePair<int, List<IntVec3>> row in rows)
            {
                int rowStartTick = startTick + row.Key * props.rowDelayTicks;
                for (int i = 0; i < row.Value.Count; i++)
                {
                    int hitTick = rowStartTick + i * props.impactDelayTicks;
                    int delay = hitTick - now;
                    if (delay <= 0)
                    {
                        Skyfaller skyfaller = SkyfallerMaker.MakeSkyfaller(props.skyfallerDef);
                        GenSpawn.Spawn(skyfaller, row.Value[i], map);
                    }
                    else
                    {
                        delayed.ScheduleSkyfaller(props.skyfallerDef, row.Value[i], delay);
                    }

                    totalScheduled++;
                }
            }

            return $"Success: Scheduled Strafe Bombardment '{def.defName}' at {targetCell}. Direction: {direction}. Targets: {totalScheduled}.";
        }

        public static string ExecuteStrafeBombardmentDirect(Map map, IntVec3 targetCell, AbilityDef def, CompProperties_AbilityBombardment props, float angle)
        {
            Dictionary<string, object> dict = new Dictionary<string, object> { { "angle", angle } };
            return ExecuteStrafeBombardment(map, targetCell, def, props, dict);
        }

        public static string ExecuteEnergyLance(Map map, IntVec3 targetCell, AbilityDef def, CompProperties_AbilityEnergyLance props, Dictionary<string, object> parsed = null)
        {
            ThingDef lanceDef = props.energyLanceDef ?? DefDatabase<ThingDef>.GetNamedSilentFail("SD_EnergyLance_Base");
            if (lanceDef == null)
            {
                return $"Error: Could not resolve EnergyLance ThingDef for '{def.defName}'.";
            }

            ParseDirectionInfo(parsed, targetCell, props.moveDistance, props.useFixedDistance, out _, out IntVec3 endPos);
            try
            {
                EnergyLance.MakeEnergyLance(lanceDef, targetCell, endPos, map, props.moveDistance, props.useFixedDistance, props.durationTicks, null);
                return $"Success: Triggered Energy Lance '{def.defName}' from {targetCell} towards {endPos}. Type: {lanceDef.defName}.";
            }
            catch (Exception ex)
            {
                return $"Error: Failed to spawn EnergyLance: {ex.Message}";
            }
        }

        public static string ExecuteEnergyLanceDirect(Map map, IntVec3 targetCell, AbilityDef def, CompProperties_AbilityEnergyLance props, float angle)
        {
            Dictionary<string, object> dict = new Dictionary<string, object> { { "angle", angle } };
            return ExecuteEnergyLance(map, targetCell, def, props, dict);
        }

        public static string ExecuteCallSkyfaller(Map map, IntVec3 targetCell, AbilityDef def, CompProperties_AbilityCallSkyfaller props)
        {
            if (props.skyfallerDef == null)
            {
                return $"Error: '{def.defName}' has no skyfallerDef.";
            }

            MapComponent_SkyfallerDelayed delayed = map.GetComponent<MapComponent_SkyfallerDelayed>();
            if (delayed == null)
            {
                delayed = new MapComponent_SkyfallerDelayed(map);
                map.components.Add(delayed);
            }

            int delay = props.delayTicks;
            if (delay <= 0)
            {
                Skyfaller skyfaller = SkyfallerMaker.MakeSkyfaller(props.skyfallerDef);
                GenSpawn.Spawn(skyfaller, targetCell, map);
                return $"Success: Spawned Skyfaller '{def.defName}' immediately at {targetCell}.";
            }

            delayed.ScheduleSkyfaller(props.skyfallerDef, targetCell, delay);
            return $"Success: Scheduled Skyfaller '{def.defName}' at {targetCell} in {delay} ticks.";
        }

        private static void ParseDirectionInfo(Dictionary<string, object> parsed, IntVec3 startPos, float moveDistance, bool useFixedDistance, out Vector3 direction, out IntVec3 endPos)
        {
            direction = Vector3.forward;
            endPos = startPos;

            if (parsed == null)
            {
                endPos = (startPos.ToVector3() + Vector3.forward * moveDistance).ToIntVec3();
                return;
            }

            if (TryGetFloat(parsed, "angle", out float angle))
            {
                direction = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward;
                endPos = (startPos.ToVector3() + direction * moveDistance).ToIntVec3();
            }
            else if (TryParseDirectionCell(parsed, out IntVec3 dirCell))
            {
                direction = (dirCell.ToVector3() - startPos.ToVector3()).normalized;
                if (direction == Vector3.zero)
                {
                    direction = Vector3.forward;
                }

                endPos = useFixedDistance ? (startPos.ToVector3() + direction * moveDistance).ToIntVec3() : dirCell;
            }
            else
            {
                endPos = (startPos.ToVector3() + Vector3.forward * moveDistance).ToIntVec3();
            }
        }

        private static bool TryParseDirectionCell(Dictionary<string, object> parsed, out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (parsed == null)
            {
                return false;
            }

            if (TryGetInt(parsed, "dirX", out int x) && TryGetInt(parsed, "dirZ", out int z))
            {
                cell = new IntVec3(x, 0, z);
                return true;
            }

            if (TryGetString(parsed, "direction", out string dirStr) && !string.IsNullOrWhiteSpace(dirStr))
            {
                string[] parts = dirStr.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && int.TryParse(parts[0], out int dx) && int.TryParse(parts[1], out int dz))
                {
                    cell = new IntVec3(dx, 0, dz);
                    return true;
                }
            }

            return false;
        }

        private static List<IntVec3> SelectTargetCells(Map map, IntVec3 center, CompProperties_AbilityCircularBombardment props, bool filterFriendlyFire)
        {
            List<IntVec3> candidates = GenRadial.RadialCellsAround(center, props.radius, true)
                .Where(c => c.InBounds(map))
                .Where(c => IsValidTargetCell(map, c, center, props, filterFriendlyFire))
                .ToList();

            if (candidates.Count == 0)
            {
                return new List<IntVec3>();
            }

            List<IntVec3> selected = new List<IntVec3>();
            foreach (IntVec3 cell in candidates.InRandomOrder())
            {
                if (Rand.Value <= props.targetSelectionChance)
                {
                    selected.Add(cell);
                }

                if (selected.Count >= props.maxTargets)
                {
                    break;
                }
            }

            if (selected.Count < props.minTargets)
            {
                List<IntVec3> missedCells = candidates.Except(selected).InRandomOrder().ToList();
                int needed = props.minTargets - selected.Count;
                if (needed > 0 && missedCells.Count > 0)
                {
                    selected.AddRange(missedCells.Take(Math.Min(needed, missedCells.Count)));
                }
            }
            else if (selected.Count > props.maxTargets)
            {
                selected = selected.InRandomOrder().Take(props.maxTargets).ToList();
            }

            return selected;
        }

        private static bool IsValidTargetCell(Map map, IntVec3 cell, IntVec3 center, CompProperties_AbilityCircularBombardment props, bool filterFriendlyFire)
        {
            if (props.minDistanceFromCenter > 0f)
            {
                float distance = Vector3.Distance(cell.ToVector3(), center.ToVector3());
                if (distance < props.minDistanceFromCenter)
                {
                    return false;
                }
            }

            if (props.avoidBuildings && cell.GetEdifice(map) != null)
            {
                return false;
            }

            if (filterFriendlyFire && props.avoidFriendlyFire)
            {
                List<Thing> things = map.thingGrid.ThingsListAt(cell);
                if (things != null)
                {
                    for (int i = 0; i < things.Count; i++)
                    {
                        if (things[i] is Pawn pawn && pawn.Faction == Faction.OfPlayer)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static int ScheduleBombardment(Map map, List<IntVec3> targets, CompProperties_AbilityCircularBombardment props, bool spawnImmediately)
        {
            int now = Find.TickManager?.TicksGame ?? 0;
            int startTick = now + props.warmupTicks;
            int launchesCompleted = 0;
            int groupIndex = 0;
            List<IntVec3> remainingTargets = new List<IntVec3>(targets);

            MapComponent_SkyfallerDelayed delayed = null;
            if (!spawnImmediately)
            {
                delayed = map.GetComponent<MapComponent_SkyfallerDelayed>();
                if (delayed == null)
                {
                    delayed = new MapComponent_SkyfallerDelayed(map);
                    map.components.Add(delayed);
                }
            }

            while (remainingTargets.Count > 0 && launchesCompleted < props.maxLaunches)
            {
                int groupSize = Math.Min(props.simultaneousLaunches, remainingTargets.Count);
                List<IntVec3> groupTargets = remainingTargets.Take(groupSize).ToList();
                remainingTargets.RemoveRange(0, groupSize);

                if (props.useIndependentIntervals)
                {
                    for (int i = 0; i < groupTargets.Count && launchesCompleted < props.maxLaunches; i++)
                    {
                        int scheduledTick = startTick + groupIndex * props.launchIntervalTicks + i * props.innerLaunchIntervalTicks;
                        SpawnOrSchedule(map, delayed, props.skyfallerDef, groupTargets[i], spawnImmediately, scheduledTick - now);
                        launchesCompleted++;
                    }
                }
                else
                {
                    int scheduledTick = startTick + groupIndex * props.launchIntervalTicks;
                    for (int i = 0; i < groupTargets.Count && launchesCompleted < props.maxLaunches; i++)
                    {
                        SpawnOrSchedule(map, delayed, props.skyfallerDef, groupTargets[i], spawnImmediately, scheduledTick - now);
                        launchesCompleted++;
                    }
                }

                groupIndex++;
            }

            return launchesCompleted;
        }

        private static void SpawnOrSchedule(Map map, MapComponent_SkyfallerDelayed delayed, ThingDef skyfallerDef, IntVec3 cell, bool spawnImmediately, int delayTicks)
        {
            if (!cell.IsValid || !cell.InBounds(map))
            {
                return;
            }

            if (spawnImmediately || delayTicks <= 0)
            {
                Skyfaller skyfaller = SkyfallerMaker.MakeSkyfaller(skyfallerDef);
                GenSpawn.Spawn(skyfaller, cell, map);
                return;
            }

            delayed?.ScheduleSkyfaller(skyfallerDef, cell, delayTicks);
        }

        private static List<IntVec3> CalculateBombardmentAreaCells(Map map, IntVec3 startCell, Vector3 direction, int width, int length)
        {
            List<IntVec3> areaCells = new List<IntVec3>();
            Vector3 start = startCell.ToVector3();
            Vector3 perpendicularDirection = new Vector3(-direction.z, 0f, direction.x).normalized;
            float halfWidth = width * 0.5f;
            int widthSteps = Math.Max(1, width);
            int lengthSteps = Math.Max(1, length);

            for (int l = 0; l <= lengthSteps; l++)
            {
                float lengthProgress = (float)l / lengthSteps;
                float lengthOffset = Mathf.Lerp(0f, length, lengthProgress);

                for (int w = 0; w <= widthSteps; w++)
                {
                    float widthProgress = (float)w / widthSteps;
                    float widthOffset = Mathf.Lerp(-halfWidth, halfWidth, widthProgress);
                    Vector3 cellPos = start + direction * lengthOffset + perpendicularDirection * widthOffset;
                    IntVec3 cell = new IntVec3(Mathf.RoundToInt(cellPos.x), Mathf.RoundToInt(cellPos.y), Mathf.RoundToInt(cellPos.z));
                    if (cell.InBounds(map) && !areaCells.Contains(cell))
                    {
                        areaCells.Add(cell);
                    }
                }
            }

            return areaCells;
        }

        private static Dictionary<int, List<IntVec3>> OrganizeIntoRows(IntVec3 startCell, Vector3 direction, List<IntVec3> cells)
        {
            Dictionary<int, List<IntVec3>> rows = new Dictionary<int, List<IntVec3>>();
            Vector3 perpendicularDirection = new Vector3(-direction.z, 0f, direction.x).normalized;

            foreach (IntVec3 cell in cells)
            {
                Vector3 cellVector = cell.ToVector3() - startCell.ToVector3();
                int rowIndex = Mathf.RoundToInt(Vector3.Dot(cellVector, direction));
                if (!rows.ContainsKey(rowIndex))
                {
                    rows[rowIndex] = new List<IntVec3>();
                }

                rows[rowIndex].Add(cell);
            }

            Dictionary<int, List<IntVec3>> sortedRows = rows.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
            foreach (int key in sortedRows.Keys.ToList())
            {
                sortedRows[key] = sortedRows[key]
                    .OrderBy(c => Vector3.Dot(c.ToVector3() - startCell.ToVector3(), perpendicularDirection))
                    .ToList();
            }

            return sortedRows;
        }

        private static bool TryGetString(Dictionary<string, object> parsed, string key, out string value)
        {
            value = null;
            if (parsed == null || string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            if (!parsed.TryGetValue(key, out object raw) || raw == null)
            {
                return false;
            }

            value = Convert.ToString(raw, CultureInfo.InvariantCulture);
            return !string.IsNullOrWhiteSpace(value);
        }

        private static bool TryGetInt(Dictionary<string, object> parsed, string key, out int value)
        {
            value = 0;
            if (!TryGetNumber(parsed, key, out double number))
            {
                return false;
            }

            value = (int)Math.Round(number);
            return true;
        }

        private static bool TryGetFloat(Dictionary<string, object> parsed, string key, out float value)
        {
            value = 0f;
            if (!TryGetNumber(parsed, key, out double number))
            {
                return false;
            }

            value = (float)number;
            return true;
        }

        private static bool TryGetBool(Dictionary<string, object> parsed, string key, out bool value)
        {
            value = false;
            if (parsed == null || string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            if (!parsed.TryGetValue(key, out object raw) || raw == null)
            {
                return false;
            }

            if (raw is bool boolValue)
            {
                value = boolValue;
                return true;
            }

            if (raw is string stringValue && bool.TryParse(stringValue, out bool parsedBool))
            {
                value = parsedBool;
                return true;
            }

            if (raw is long longValue)
            {
                value = longValue != 0;
                return true;
            }

            if (raw is double doubleValue)
            {
                value = Math.Abs(doubleValue) > 0.0001;
                return true;
            }

            return false;
        }

        private static bool TryGetNumber(Dictionary<string, object> parsed, string key, out double value)
        {
            value = 0d;
            if (parsed == null || string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            if (!parsed.TryGetValue(key, out object raw) || raw == null)
            {
                return false;
            }

            switch (raw)
            {
                case double doubleValue:
                    value = doubleValue;
                    return true;
                case float floatValue:
                    value = floatValue;
                    return true;
                case int intValue:
                    value = intValue;
                    return true;
                case long longValue:
                    value = longValue;
                    return true;
                case string stringValue when double.TryParse(stringValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedNum):
                    value = parsedNum;
                    return true;
                default:
                    return false;
            }
        }
    }
}
