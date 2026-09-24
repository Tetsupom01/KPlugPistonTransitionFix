using System;
using System.Reflection;
using BepInEx;
using HarmonyLib;

[BepInPlugin("local.kplug.pistonwaitfix", "kPlug Piston Wait Fix", "1.0.0")]
public class KPlugPistonWaitFix : BaseUnityPlugin
{
    private Harmony harmony;

    private static MethodInfo mIsInLoopAction;
    private static MethodInfo mIsInAnalLoopAction;

    private void Awake()
    {
        try
        {
            Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();

            Type aiClosureType = FindType(asms, "kPlug.CmpH.AI_Main+<>c");
            Type toolHType = FindType(asms, "kPlug.Tools.ToolH");

            if (object.ReferenceEquals(aiClosureType, null))
                throw new Exception("AI_Main+<>c not found.");

            if (object.ReferenceEquals(toolHType, null))
                throw new Exception("ToolH not found.");

            BindingFlags stat = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            BindingFlags inst = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            MethodInfo waitPredicate = aiClosureType.GetMethod(
                "<IE_AskPistonChange>b__83_1",
                inst
            );

            if (object.ReferenceEquals(waitPredicate, null))
                throw new Exception("AskPistonChange WaitUntil predicate not found.");

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

            if (object.ReferenceEquals(mIsInLoopAction, null) &&
                object.ReferenceEquals(mIsInAnalLoopAction, null))
                throw new Exception("ToolH loop-state getters not found.");

            MethodInfo postfix = typeof(KPlugPistonWaitFix).GetMethod(
                "WaitPredicatePostfix",
                BindingFlags.Static | BindingFlags.NonPublic
            );

            if (object.ReferenceEquals(postfix, null))
                throw new Exception("Postfix method not found.");

            harmony = new Harmony("local.kplug.pistonwaitfix");
            harmony.Patch(
                waitPredicate,
                null,
                new HarmonyMethod(postfix),
                null
            );

            Logger.LogInfo("[PistonWaitFix] v1.0.0 active.");
        }
        catch (Exception e)
        {
            Logger.LogError("[PistonWaitFix] patch failed: " + e);
        }
    }

    private void OnDestroy()
    {
        try
        {
            if (!object.ReferenceEquals(harmony, null))
                harmony.UnpatchSelf();
        }
        catch { }
    }

    private static void WaitPredicatePostfix(ref bool __result)
    {
        if (__result)
            return;

        bool loop = InvokeBool(mIsInLoopAction);
        bool analLoop = InvokeBool(mIsInAnalLoopAction);

        if (loop || analLoop)
            __result = true;
    }

    private static bool InvokeBool(MethodInfo mi)
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
