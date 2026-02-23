using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Verse;

namespace EndfieldPerlica
{
    [StaticConstructorOnStartup]
    public class EFPerlicaMod : Mod
    {
        public EFPerlicaMod(ModContentPack content) : base(content)
        {
            // 初始化Harmony
            var harmony = new Harmony("tourswen.EndfieldPerlica");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
