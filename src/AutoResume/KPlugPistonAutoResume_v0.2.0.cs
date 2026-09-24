using System;
using System.Collections;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

[BepInPlugin("local.kplug.pistonautoresume", "kPlug Piston Auto Resume", "0.2.0")]
public class KPlugPistonAutoResume : BaseUnityPlugin
{
    private Harmony harmony;
    private static KPlugPistonAutoResume instance;

    private static Type aiMainType;
    private static Type toolHType;
    private static Type coreType;
    private static Type hSonyuType;

    private static MethodInfo mGetInAnswer;
    private static MethodInfo mIsInLoopAction;
    private static MethodInfo mIsInAnalLoopAction;

    private static FieldInfo fCoreHFlag;
    private static FieldInfo fClick;
    private static FieldInfo fSelectAnimationListInfo;

    private static ConfigEntry<bool> cfgEnabled;
    private static ConfigEntry<float> cfgResumeDelay;

    private static bool pendingGo;
    private static bool delayStarted;
    private static float pendingUntil;
    private static float resumeAt;
    private static float activeDelay;
    private static object pendingAiInstance;

    private void Awake()
    {
        instance = this;

        cfgEnabled = Config.Bind(
            "Piston Auto Resume",
            "Enabled",
            true,
            "Automatically resume piston motion after kPlug changes the insertion animation with Numpad 7 / Backspace."
        );

        cfgResumeDelay = Config.Bind(
            "Piston Auto Resume",
            "ResumeDelaySeconds",
            0.8f,
            new ConfigDescription(
                "Delay after the real InsertIdle/A_InsertIdle state is reached before normal Go is injected. 0.0 = fastest. Range: 0.0 to 3.0 seconds.",
                new AcceptableValueRange<float>(0.0f, 3.0f)
            )
        );

        try
        {
            Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();

            aiMainType = FindType(asms, "kPlug.CmpH.AI_Main");
            toolHType = FindType(asms, "kPlug.Tools.ToolH");
            coreType = FindType(asms, "kPlug.Core");
            hSonyuType = FindType(asms, "HSonyu");

            if (object.ReferenceEquals(aiMainType, null))
                throw new Exception("AI_Main not found.");
            if (object.ReferenceEquals(toolHType, null))
                throw new Exception("ToolH not found.");
            if (object.ReferenceEquals(coreType, null))
                throw new Exception("Core not found.");
            if (object.ReferenceEquals(hSonyuType, null))
                throw new Exception("HSonyu not found.");

            BindingFlags stat = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            BindingFlags inst = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            MethodInfo askPistonChange = aiMainType.GetMethod("AskPistonChange", inst);
            MethodInfo hSonyuProc = hSonyuType.GetMethod("Proc", inst);

            mGetInAnswer = aiMainType.GetMethod(
                "get_InAnswer",
                inst,
                null,
                Type.EmptyTypes,
                null
            );

            mIsInLoopAction = toolHType.GetMethod(
                "get_IsInLoopAction",
                stat,
                null,
                Type.EmptyTypes,
                null
            );

            mIsInAnalLoopAction = toolHType.GetMethod(
                "get_IsInAnalLoopAction",
                stat,
                null,
                Type.EmptyTypes,
                null
            );

            fCoreHFlag = coreType.GetField("hFlag", stat);

            if (object.ReferenceEquals(askPistonChange, null))
                throw new Exception("AI_Main.AskPistonChange not found.");
            if (object.ReferenceEquals(hSonyuProc, null))
                throw new Exception("HSonyu.Proc not found.");
            if (object.ReferenceEquals(mGetInAnswer, null))
                throw new Exception("AI_Main.get_InAnswer not found.");
            if (object.ReferenceEquals(mIsInLoopAction, null))
                throw new Exception("ToolH.get_IsInLoopAction not found.");
            if (object.ReferenceEquals(mIsInAnalLoopAction, null))
                throw new Exception("ToolH.get_IsInAnalLoopAction not found.");
            if (object.ReferenceEquals(fCoreHFlag, null))
                throw new Exception("Core.hFlag not found.");

            MethodInfo askPrefix = typeof(KPlugPistonAutoResume).GetMethod(
                "AskPistonChangePrefix",
                BindingFlags.Static | BindingFlags.NonPublic
            );

            MethodInfo askPostfix = typeof(KPlugPistonAutoResume).GetMethod(
                "AskPistonChangePostfix",
                BindingFlags.Static | BindingFlags.NonPublic
            );

            MethodInfo procPrefix = typeof(KPlugPistonAutoResume).GetMethod(
                "HSonyuProcPrefix",
                BindingFlags.Static | BindingFlags.NonPublic
            );

            if (object.ReferenceEquals(askPrefix, null) ||
                object.ReferenceEquals(askPostfix, null) ||
                object.ReferenceEquals(procPrefix, null))
                throw new Exception("Harmony patch methods not found.");

            harmony = new Harmony("local.kplug.pistonautoresume");

            harmony.Patch(
                askPistonChange,
                new HarmonyMethod(askPrefix),
                new HarmonyMethod(askPostfix),
                null
            );

            harmony.Patch(
                hSonyuProc,
                new HarmonyMethod(procPrefix),
                null,
                null
            );

            Logger.LogInfo(
                "[PistonAutoResume] v0.2.0 active. Enabled=" +
                cfgEnabled.Value +
                ", ResumeDelaySeconds=" +
                GetConfiguredDelay().ToString("F2")
            );
        }
        catch (Exception e)
        {
            Logger.LogError("[PistonAutoResume] patch failed: " + e);
        }
    }

    private void OnDestroy()
    {
        try
        {
            ClearPending();

            if (!object.ReferenceEquals(harmony, null))
                harmony.UnpatchSelf();
        }
        catch { }
    }

    private static void AskPistonChangePrefix(ref bool __state)
    {
        if (pendingGo)
            CancelPending("new piston animation change");

        __state = InvokeStaticBool(mIsInLoopAction) || InvokeStaticBool(mIsInAnalLoopAction);
    }

    private static void AskPistonChangePostfix(object __instance, bool __state)
    {
        if (!__state)
            return;

        if (object.ReferenceEquals(instance, null) ||
            object.ReferenceEquals(__instance, null))
            return;

        if (!IsEnabled())
            return;

        instance.StartCoroutine(instance.ArmGoAfterAskPistonChange(__instance));
    }

    private IEnumerator ArmGoAfterAskPistonChange(object aiInstance)
    {
        float start = Time.realtimeSinceStartup;
        bool sawInAnswer = false;

        while (Time.realtimeSinceStartup - start < 1.0f)
        {
            if (!IsEnabled())
                yield break;

            if (InvokeInstanceBool(mGetInAnswer, aiInstance))
            {
                sawInAnswer = true;
                break;
            }

            yield return null;
        }

        if (!sawInAnswer)
        {
            Logger.LogWarning("[PistonAutoResume] InAnswer did not start; no auto resume.");
            yield break;
        }

        start = Time.realtimeSinceStartup;

        while (Time.realtimeSinceStartup - start < 5.0f)
        {
            if (!IsEnabled())
                yield break;

            if (!InvokeInstanceBool(mGetInAnswer, aiInstance))
                break;

            yield return null;
        }

        if (InvokeInstanceBool(mGetInAnswer, aiInstance))
        {
            Logger.LogWarning("[PistonAutoResume] AskPistonChange did not finish; no auto resume.");
            yield break;
        }

        if (InvokeStaticBool(mIsInLoopAction) || InvokeStaticBool(mIsInAnalLoopAction))
        {
            Logger.LogInfo("[PistonAutoResume] transition already resumed in loop; no action.");
            yield break;
        }

        pendingAiInstance = aiInstance;
        pendingGo = true;
        delayStarted = false;
        activeDelay = 0.0f;
        resumeAt = 0.0f;
        pendingUntil = Time.realtimeSinceStartup + 2.0f;

        Logger.LogInfo("[PistonAutoResume] armed; waiting for actual InsertIdle animator state.");
    }

    private static void HSonyuProcPrefix(object __instance)
    {
        if (!pendingGo)
            return;

        if (!IsEnabled())
        {
            CancelPending("disabled in config");
            return;
        }

        if (object.ReferenceEquals(__instance, null))
            return;

        object hFlag = fCoreHFlag.GetValue(null);
        if (object.ReferenceEquals(hFlag, null))
        {
            CancelPending("HFlag unavailable");
            return;
        }

        BindingFlags inst = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        if (object.ReferenceEquals(fClick, null))
            fClick = hFlag.GetType().GetField("click", inst);

        if (object.ReferenceEquals(fSelectAnimationListInfo, null))
            fSelectAnimationListInfo = hFlag.GetType().GetField("selectAnimationListInfo", inst);

        if (object.ReferenceEquals(fClick, null))
        {
            CancelPending("HFlag.click unavailable");
            return;
        }

        int currentClick = ReadInt(fClick, hFlag, 0);

        if (currentClick != 0)
        {
            CancelPending("another H command was received");
            return;
        }

        if (!object.ReferenceEquals(fSelectAnimationListInfo, null))
        {
            object pendingAnim = fSelectAnimationListInfo.GetValue(hFlag);
            if (!object.ReferenceEquals(pendingAnim, null))
            {
                CancelPending("another animation change became pending");
                return;
            }
        }

        if (!object.ReferenceEquals(pendingAiInstance, null) &&
            InvokeInstanceBool(mGetInAnswer, pendingAiInstance))
        {
            CancelPending("AI_Main entered another answer/action");
            return;
        }

        if (InvokeStaticBool(mIsInLoopAction) || InvokeStaticBool(mIsInAnalLoopAction))
        {
            CancelPending("motion already resumed");
            return;
        }

        bool actualInsertIdle = IsActualInsertIdle(__instance);

        if (!delayStarted)
        {
            if (Time.realtimeSinceStartup > pendingUntil)
            {
                CancelPending("actual InsertIdle was not reached before timeout");
                return;
            }

            if (!actualInsertIdle)
                return;

            activeDelay = GetConfiguredDelay();

            if (activeDelay <= 0.0001f)
            {
                InjectNormalGo(hFlag, "actual InsertIdle reached; injected normal Go immediately.");
                return;
            }

            delayStarted = true;
            resumeAt = Time.realtimeSinceStartup + activeDelay;
            pendingUntil = resumeAt + 1.0f;

            if (!object.ReferenceEquals(instance, null))
            {
                instance.Logger.LogInfo(
                    "[PistonAutoResume] actual InsertIdle reached; waiting " +
                    activeDelay.ToString("F2") +
                    "s before normal Go."
                );
            }

            return;
        }

        if (!actualInsertIdle)
        {
            CancelPending("InsertIdle ended during resume delay");
            return;
        }

        if (Time.realtimeSinceStartup < resumeAt)
            return;

        InjectNormalGo(
            hFlag,
            "resume delay elapsed; injected normal Go (click=5)."
        );
    }

    private static void InjectNormalGo(object hFlag, string message)
    {
        if (object.ReferenceEquals(hFlag, null) ||
            object.ReferenceEquals(fClick, null))
        {
            CancelPending("cannot inject normal Go");
            return;
        }

        if (ReadInt(fClick, hFlag, 0) != 0)
        {
            CancelPending("another H command appeared before Go injection");
            return;
        }

        fClick.SetValue(hFlag, 5);
        ClearPending();

        if (!object.ReferenceEquals(instance, null))
            instance.Logger.LogInfo("[PistonAutoResume] " + message);
    }

    private static bool IsActualInsertIdle(object hSonyu)
    {
        try
        {
            BindingFlags inst = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            Type t = hSonyu.GetType();

            FieldInfo femaleField = FindFieldUp(t, "female", inst);
            if (object.ReferenceEquals(femaleField, null))
                return false;

            object female = femaleField.GetValue(hSonyu);
            if (object.ReferenceEquals(female, null))
                return false;

            MethodInfo getState = female.GetType().GetMethod(
                "getAnimatorStateInfo",
                inst,
                null,
                new Type[] { typeof(int) },
                null
            );
            if (object.ReferenceEquals(getState, null))
                return false;

            object state = getState.Invoke(female, new object[] { 0 });
            if (object.ReferenceEquals(state, null))
                return false;

            MethodInfo isName = state.GetType().GetMethod(
                "IsName",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new Type[] { typeof(string) },
                null
            );
            if (object.ReferenceEquals(isName, null))
                return false;

            bool insertIdle = Convert.ToBoolean(
                isName.Invoke(state, new object[] { "InsertIdle" })
            );
            if (insertIdle)
                return true;

            return Convert.ToBoolean(
                isName.Invoke(state, new object[] { "A_InsertIdle" })
            );
        }
        catch
        {
            return false;
        }
    }

    private static float GetConfiguredDelay()
    {
        try
        {
            if (object.ReferenceEquals(cfgResumeDelay, null))
                return 0.8f;

            float value = cfgResumeDelay.Value;
            if (value < 0.0f) return 0.0f;
            if (value > 3.0f) return 3.0f;
            return value;
        }
        catch
        {
            return 0.8f;
        }
    }

    private static bool IsEnabled()
    {
        try
        {
            if (object.ReferenceEquals(cfgEnabled, null))
                return true;

            return cfgEnabled.Value;
        }
        catch
        {
            return true;
        }
    }

    private static void CancelPending(string reason)
    {
        bool wasPending = pendingGo;
        ClearPending();

        if (wasPending && !object.ReferenceEquals(instance, null))
            instance.Logger.LogInfo("[PistonAutoResume] cancelled: " + reason + ".");
    }

    private static void ClearPending()
    {
        pendingGo = false;
        delayStarted = false;
        pendingUntil = 0.0f;
        resumeAt = 0.0f;
        activeDelay = 0.0f;
        pendingAiInstance = null;
    }

    private static int ReadInt(FieldInfo fi, object target, int fallback)
    {
        try
        {
            if (object.ReferenceEquals(fi, null) ||
                object.ReferenceEquals(target, null))
                return fallback;

            object v = fi.GetValue(target);
            if (object.ReferenceEquals(v, null))
                return fallback;

            return Convert.ToInt32(v);
        }
        catch
        {
            return fallback;
        }
    }

    private static FieldInfo FindFieldUp(Type t, string name, BindingFlags flags)
    {
        Type cur = t;
        while (!object.ReferenceEquals(cur, null))
        {
            FieldInfo f = cur.GetField(name, flags);
            if (!object.ReferenceEquals(f, null))
                return f;

            cur = cur.BaseType;
        }

        return null;
    }

    private static bool InvokeStaticBool(MethodInfo mi)
    {
        try
        {
            if (object.ReferenceEquals(mi, null))
                return false;

            object v = mi.Invoke(null, null);
            return Convert.ToBoolean(v);
        }
        catch
        {
            return false;
        }
    }

    private static bool InvokeInstanceBool(MethodInfo mi, object target)
    {
        try
        {
            if (object.ReferenceEquals(mi, null) ||
                object.ReferenceEquals(target, null))
                return false;

            object v = mi.Invoke(target, null);
            return Convert.ToBoolean(v);
        }
        catch
        {
            return false;
        }
    }

    private static Type FindType(Assembly[] asms, string fullName)
    {
        for (int i = 0; i < asms.Length; i++)
        {
            Type t = asms[i].GetType(fullName, false);
            if (!object.ReferenceEquals(t, null))
                return t;
        }

        return null;
    }
}
