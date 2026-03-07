using HarmonyLib;
using System.Reflection;
using Verse;

namespace StellarisDaughter
{
    // ✨ 沐雪写的哦~
    [StaticConstructorOnStartup]
    public class SD_Mod : Mod
    {
        public SD_Mod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("tourswen.stellarisdaughter");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Message("[StellarisDaughter] 地砖的女儿模组已加载");
        }
    }
}
