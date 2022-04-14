using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace ExpressDelivery
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        static ManualLogSource logger;

        private void Awake()
        {
            // Plugin startup logic
            logger = Logger;
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded");
            Logger.LogInfo($"Patching...");
            Harmony.CreateAndPatchAll(typeof(Plugin));
            Logger.LogInfo($"Patched");
        }

        [HarmonyPatch(typeof(DeliveryTruck), "StuckChecker")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> StuckChecker_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions).MatchForward(true,
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(DeliveryTruck), "distanceCounter")),
                new CodeMatch(OpCodes.Ldc_R4, 20f)
            ).SetOperandAndAdvance(-1f)
            
            .MatchForward(false,
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(ConstructionSite), "_truckTeleportLoc")),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(ConstructionSite), "CheckTeleportArea")),
                new CodeMatch(OpCodes.Brtrue),
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(ConstructionSite), "_orders")),
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(OpCodes.Ldc_I4_0),
                new CodeMatch(OpCodes.Callvirt),
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Ldfld),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(ConstructionSite), "TeleportToPoint"))
            )

            .RemoveInstructions(4)

            //Insert stuff before Brtrue
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Plugin), "TeleportStuff"))
                ).SetOpcodeAndAdvance(OpCodes.Br);

            return matcher.InstructionEnumeration();
        }

        static void TeleportStuff(ConstructionSite component)
        {
            if (component._orders._truckObjs[0].GetComponent<DeliveryTruck>().atDestination != 1 && !component.CheckTeleportArea(component._truckTeleportLoc))
            {
                component.TeleportToPoint(component._orders._truckObjs[0], component._truckTeleportLoc);
            }
            else if (component._orders._truckObjs[0].GetComponent<DeliveryTruck>().atDestination != 0 && !component.CheckTeleportArea(component._truckHomeTeleportLoc))
            {
                component.TeleportToPoint(component._orders._truckObjs[0], component._truckHomeTeleportLoc);
            }
        }
    }
}
