using System;
using System.Reflection;

using BepInEx;

using HarmonyLib;

using MonoMod.Cil;
using Mono.Cecil.Cil;

using UnityEngine;

namespace OverpassFailFilterRemoval
{
    [BepInPlugin(ModGUID, ModName, ModVer)]
    public class RemovalCore : BaseUnityPlugin
    {
        public const string ModGUID = "autumnrivers.failfilterremoval";
        public const string ModName = "Remove The Fail Filter Please";
        public const string ModVer = "1.0.0";

        Harmony harmony = new Harmony("FailFilterRemovalHarmonyInstance");
        
        private void Awake()
        {
            harmony.PatchAll(typeof(RemovalCore).Assembly);

            Logger.LogInfo("Please remove the fail filter I beg of you please God");
        }
    }

    [HarmonyPatch]
    public class FilterRemoval
    {
        // Are ALL of these necessary? Maybe not...
        // ...but it also took me this long to actually get it working.
        // Let's not poke the bear. Or, um... the stack.
        
        [HarmonyILManipulator]
        [HarmonyPatch(typeof(StatueBar), "Update")]
        public static void CancelFailFilter(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            Func<Instruction, bool>[] findFilterCode = { x => x.MatchLdarg(0),
                x => x.MatchLdfld(out var _),
                x => x.MatchBrfalse(out var _),
                x => x.MatchCall(out var _),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld(out var _),
                x => x.MatchSub(),
                x => x.MatchLdcR4(1),
                x => x.MatchDiv(),
                x => x.MatchStloc(0),
                x => x.MatchLdarg(0),
                x => x.MatchCall(out var _),
                x => x.MatchLdcR4(1),
                x => x.MatchLdloc(0),
                x => x.MatchSub(),
                x => x.MatchCallvirt(out var _),
            };
            c.GotoNext(MoveType.After, findFilterCode);
            c.Index -= 3; // Goes to Ldloc
            c.RemoveRange(5); // Removes setting the alpha entirely

            for(var i = 0; i < 5; i++)
            {
                c.Emit(OpCodes.Nop); // Empty instructions to please the Stack God
            }
        }

        [HarmonyILManipulator]
        [HarmonyPatch(typeof(StatueBar),nameof(StatueBar.fail))]
        public static void DontPlayFailureScene(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            Func<Instruction, bool>[] findScenePlayInst =
            {
                x => x.MatchLdarg(0),
                x => x.MatchLdfld(out var _),
                x => x.MatchBrtrue(out var _),
                x => x.MatchLdarg(0),
                x => x.MatchLdcI4(1),
                x => x.MatchStfld(out var _),
                x => x.MatchLdarg(0),
                x => x.MatchCall(out var _),
                x => x.MatchStfld(out var _)
            };
            c.GotoNext(MoveType.After, findScenePlayInst);
            c.RemoveRange(6); // Removes calling the SceneData

            for (var i = 0; i < 6; i++)
            {
                c.Emit(OpCodes.Nop); // Empty instructions to please the Stack God
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(overmaker),nameof(overmaker.statueMiss))]
        public static bool DontFilterOnMiss(overmaker __instance)
        {
            if (__instance.statueHUD == null) return true;
            if (__instance.statueHUD.amt >= 8) return true;

            __instance.statueHUD.failed = true;
            var timer = __instance.statueHUD.GetType().GetField("timer", BindingFlags.NonPublic | BindingFlags.Instance);
            timer.SetValue(__instance.statueHUD, Time.time);

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CanvasCode),"updateHUD")]
        public static bool AlwaysDeactivateRedTime(CanvasCode __instance)
        {
            var redTime = __instance.GetType().GetField("redTime", BindingFlags.NonPublic | BindingFlags.Instance);
            redTime.SetValue(__instance, 0f);
            return true;
        }

        [HarmonyILManipulator]
        [HarmonyPatch(typeof(overmaker), nameof(overmaker.judge))]
        public static void DontUseRedderLUT(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            Func<Instruction, bool>[] applyRedderLutInstr =
            {
                x => x.MatchLdarg(0),
                x => x.MatchLdfld(out var _),
                x => x.MatchLdstr("LUT/LUT_redder"),
                x => x.MatchCall(out var _),
                x => x.MatchIsinst(out var _),
                x => x.MatchStfld(out var _),
                x => x.MatchLdarg(0)
            };

            c.GotoNext(MoveType.Before, applyRedderLutInstr);
            c.RemoveRange(13); // Removes all of the code that applies the redder LUT.
            // This doesn't touch the SFX modifier.

            for (var i = 0; i < 13; i++)
            {
                c.Emit(OpCodes.Nop); // Empty instructions to please the Stack God
            }
        }
    }
}
