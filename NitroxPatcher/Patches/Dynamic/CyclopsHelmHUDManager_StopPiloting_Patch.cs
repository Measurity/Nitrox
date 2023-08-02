using System.Reflection;
using HarmonyLib;
using NitroxClient.GameLogic;
using NitroxModel.DataStructures;
using NitroxModel.Helper;

namespace NitroxPatcher.Patches.Dynamic
{
    public class CyclopsHelmHUDManager_StopPiloting_Patch : NitroxPatch, IDynamicPatch
    {
        public static readonly MethodInfo TARGET_METHOD = Reflect.Method((CyclopsHelmHUDManager t) => t.StopPiloting());

        public static void Postfix(CyclopsHelmHUDManager __instance)
        {
            __instance.hudActive = true;

            if (__instance.subRoot.TryGetIdOrWarn(out NitroxId id))
            {
                Resolve<Cyclops>().BroadcastMetadataChange(id);
            }
        }

        public override void Patch(Harmony harmony)
        {
            PatchPostfix(harmony, TARGET_METHOD);
        }
    }
}
