using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

[BepInPlugin("tetsupom.kplug.gaugeswapfix", "kPlug Gauge Swap Fix", "0.2.0")]
public class KPlugGaugeSwapFix : BaseUnityPlugin
{
    internal static ManualLogSource Log;
    private Harmony _harmony;

    private static Type TCore;
    private static Type THCtrl;
    private static Type THFlag;
    private static Type THSceneProc;
    private static Type TToolUI;

    private static FieldInfo FCoreHCtrl;
    private static FieldInfo FCoreHFlag;
    private static FieldInfo FFaceOverwrite;
    private static FieldInfo FGaugeFemale;
    private static FieldInfo FGaugeMale;
    private static MethodInfo MReplaceFaceImage;

    public sealed class SwapState
    {
        public bool Active;
        public object HCtrl;
        public object Face;
        public float GaugeFemale;
        public float GaugeMale;
    }

    private void Awake()
    {
        Log = Logger;

        try
        {
            TCore = AccessTools.TypeByName("kPlug.Core");
            THCtrl = AccessTools.TypeByName("kPlug.CmpH.HCtrl");
            THFlag = AccessTools.TypeByName("HFlag");
            THSceneProc = AccessTools.TypeByName("HSceneProc");
            TToolUI = AccessTools.TypeByName("kPlug.Tools.ToolUI");

            RequireType(TCore, "kPlug.Core");
            RequireType(THCtrl, "kPlug.CmpH.HCtrl");
            RequireType(THFlag, "HFlag");
            RequireType(THSceneProc, "HSceneProc");
            RequireType(TToolUI, "kPlug.Tools.ToolUI");

            FCoreHCtrl = RequireField(AccessTools.Field(TCore, "hCtrl"), "kPlug.Core.hCtrl");
            FCoreHFlag = RequireField(AccessTools.Field(TCore, "hFlag"), "kPlug.Core.hFlag");
            FFaceOverwrite = RequireField(AccessTools.Field(THCtrl, "faceOverwrite"), "HCtl.faceOverwrite");
            FGaugeFemale = RequireField(AccessTools.Field(THFlag, "gaugeFemale"), "HFlag.gaugeFemale");
            FGaugeMale = RequireField(AccessTools.Field(THFlag, "gaugeMale"), "HFlag.gaugeMale");
            MReplaceFaceImage = RequireMethod(AccessTools.Method(TToolUI, "ReplaceFaceImage"), "ToolUI.ReplaceFaceImage");

            MethodInfo changeAnimator = RequireMethod(AccessTools.Method(THSceneProc, "ChangeAnimator"), "HSceneProc.ChangeAnimator");

            _harmony = new Harmony("tetsupom.kplug.gaugeswapfix");
            _harmony.Patch(
                changeAnimator,
                new HarmonyMethod(typeof(KPlugGaugeSwapFix), "ChangeAnimatorPrefix"),
                new HarmonyMethod(typeof(KPlugGaugeSwapFix), "ChangeAnimatorPostfix"));

            Log.LogInfo("[GaugeSwapFix] v0.2.0 active. Target=HSceneProc::ChangeAnimator. Main-girl swap only.");
            Log.LogInfo("[GaugeSwapFix] Behavior: preserve current gauges across vanilla animator re-init and reapply existing kPlug faceOverwrite afterward.");
        }
        catch (Exception ex)
        {
            Log.LogError("[GaugeSwapFix] patch failed: " + ex);
        }
    }

    public static void ChangeAnimatorPrefix(out SwapState __state)
    {
        __state = new SwapState();

        try
        {
            object hctrl = FCoreHCtrl.GetValue(null);
            if (hctrl == null) return;

            object face = FFaceOverwrite.GetValue(hctrl);
            if (face == null) return;

            object hflag = FCoreHFlag.GetValue(null);
            if (hflag == null) return;

            object femaleObj = FGaugeFemale.GetValue(hflag);
            object maleObj = FGaugeMale.GetValue(hflag);
            if (femaleObj == null || maleObj == null) return;

            __state.Active = true;
            __state.HCtrl = hctrl;
            __state.Face = face;
            __state.GaugeFemale = Convert.ToSingle(femaleObj);
            __state.GaugeMale = Convert.ToSingle(maleObj);
        }
        catch (Exception ex)
        {
            __state.Active = false;
            if (Log != null)
                Log.LogWarning("[GaugeSwapFix] Prefix observation failed; original ChangeAnimator will continue unchanged. " + ex.Message);
        }
    }

    public static void ChangeAnimatorPostfix(SwapState __state)
    {
        if (__state == null || !__state.Active) return;

        try
        {
            object hctrl = FCoreHCtrl.GetValue(null);
            if (!object.ReferenceEquals(hctrl, __state.HCtrl)) return;

            object currentFace = FFaceOverwrite.GetValue(hctrl);
            if (!object.ReferenceEquals(currentFace, __state.Face)) return;

            object hflag = FCoreHFlag.GetValue(null);
            if (hflag == null) return;

            float resetFemale = Convert.ToSingle(FGaugeFemale.GetValue(hflag));
            float resetMale = Convert.ToSingle(FGaugeMale.GetValue(hflag));

            FGaugeFemale.SetValue(hflag, __state.GaugeFemale);
            FGaugeMale.SetValue(hflag, __state.GaugeMale);

            MReplaceFaceImage.Invoke(null, new object[] { __state.Face });

            if (Log != null)
            {
                Log.LogInfo(
                    "[GaugeSwapFix] Rebound swapped main-girl Face/Gauge after ChangeAnimator. " +
                    "female " + resetFemale.ToString("0.###") + " -> " + __state.GaugeFemale.ToString("0.###") +
                    ", male " + resetMale.ToString("0.###") + " -> " + __state.GaugeMale.ToString("0.###"));
            }
        }
        catch (Exception ex)
        {
            if (Log != null)
                Log.LogWarning("[GaugeSwapFix] Postfix rebind failed. Original ChangeAnimator already completed. " + ex.Message);
        }
    }

    private static void RequireType(Type t, string name)
    {
        if (object.ReferenceEquals(t, null))
            throw new TypeLoadException("Required type not found: " + name);
    }

    private static FieldInfo RequireField(FieldInfo f, string name)
    {
        if (object.ReferenceEquals(f, null))
            throw new MissingFieldException("Required field not found: " + name);
        return f;
    }

    private static MethodInfo RequireMethod(MethodInfo m, string name)
    {
        if (object.ReferenceEquals(m, null))
            throw new MissingMethodException("Required method not found: " + name);
        return m;
    }
}
