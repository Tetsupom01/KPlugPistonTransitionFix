using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

[BepInPlugin("local.kplug.nullguardsfix", "kPlug Null Guards Fix", "0.1.0")]
public sealed class KPlugNullGuardsFix : BaseUnityPlugin
{
    private static Harmony _harmony;
    private static ManualLogSource _log;

    private static FieldInfo _coreHProc;
    private static FieldInfo _coreInH;

    private static FieldInfo _kokanIsMainGirl;
    private static FieldInfo _kokanIsSecondGirl;

    private static FieldInfo _voiceSourceField;

    private static MethodInfo _voiceReadyMethod;

    private void Awake()
    {
        _log = Logger;

        try
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            Type coreType = FindType(assemblies, "kPlug.Core");
            Type kokanType = FindType(assemblies, "kPlug.CmpChara.KokanBehavior");
            Type menuCornerType = FindType(assemblies, "kPlug.Menu.MenuCorner");
            Type voiceStateType = FindType(
                assemblies,
                "kPlug.CmpChara.FaceCtrl+<IE_PlayVoice>d__88"
            );

            RequireType(coreType, "kPlug.Core");
            RequireType(kokanType, "kPlug.CmpChara.KokanBehavior");
            RequireType(menuCornerType, "kPlug.Menu.MenuCorner");
            RequireType(
                voiceStateType,
                "kPlug.CmpChara.FaceCtrl+<IE_PlayVoice>d__88"
            );

            _coreHProc = coreType.GetField(
                "hProc",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
            );
            _coreInH = coreType.GetField(
                "inH",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
            );

            _kokanIsMainGirl = kokanType.GetField(
                "isMainGirl",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
            _kokanIsSecondGirl = kokanType.GetField(
                "isSecondGirl",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            _voiceSourceField = voiceStateType.GetField(
                "<vSrc>5__2",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            RequireField(_coreHProc, "kPlug.Core.hProc");
            RequireField(_coreInH, "kPlug.Core.inH");
            RequireField(_kokanIsMainGirl, "KokanBehavior.isMainGirl");
            RequireField(_kokanIsSecondGirl, "KokanBehavior.isSecondGirl");
            RequireField(
                _voiceSourceField,
                "FaceCtrl.<IE_PlayVoice>d__88.<vSrc>5__2"
            );

            _voiceReadyMethod = typeof(KPlugNullGuardsFix).GetMethod(
                "VoiceSourceReady",
                BindingFlags.Static | BindingFlags.NonPublic
            );
            RequireMethod(_voiceReadyMethod, "VoiceSourceReady");

            _harmony = new Harmony("local.kplug.nullguardsfix");

            MethodInfo hProcPrefix = GetOwnMethod("HProcPrefix");
            MethodInfo kokanPrefix = GetOwnMethod("KokanPrefix");
            MethodInfo menuCornerPrefix = GetOwnMethod("MenuCornerPrefix");
            MethodInfo voiceTranspiler = GetOwnMethod("VoiceTranspiler");

            int prefixCount = 0;

            prefixCount += PatchHProcGuard(
                assemblies,
                "kPlug.CmpH.AI_Main",
                "Update",
                hProcPrefix
            );
            prefixCount += PatchHProcGuard(
                assemblies,
                "kPlug.CmpH.AI_Main",
                "LateUpdate",
                hProcPrefix
            );
            prefixCount += PatchHProcGuard(
                assemblies,
                "kPlug.CmpH.AnimSelector",
                "LateUpdate",
                hProcPrefix
            );
            prefixCount += PatchHProcGuard(
                assemblies,
                "kPlug.CmpH.HComboCtrl",
                "Update",
                hProcPrefix
            );
            prefixCount += PatchHProcGuard(
                assemblies,
                "kPlug.CmpH.HComboCtrl",
                "LateUpdate",
                hProcPrefix
            );
            prefixCount += PatchHProcGuard(
                assemblies,
                "kPlug.CmpH.HCtrl",
                "Update",
                hProcPrefix
            );
            prefixCount += PatchHProcGuard(
                assemblies,
                "kPlug.CmpH.HCtrl",
                "LateUpdate",
                hProcPrefix
            );
            prefixCount += PatchHProcGuard(
                assemblies,
                "kPlug.CmpH.InviteUI",
                "Update",
                hProcPrefix
            );
            prefixCount += PatchHProcGuard(
                assemblies,
                "kPlug.CmpH.InviteUI",
                "OnGUI",
                hProcPrefix
            );

            PatchMethod(
                kokanType,
                "Update",
                new HarmonyMethod(kokanPrefix),
                null
            );
            prefixCount++;

            PatchMethod(
                kokanType,
                "LateUpdate",
                new HarmonyMethod(kokanPrefix),
                null
            );
            prefixCount++;

            PatchMethod(
                menuCornerType,
                "LateUpdate",
                new HarmonyMethod(menuCornerPrefix),
                null
            );
            prefixCount++;

            MethodInfo voiceMoveNext = GetNoArgMethod(
                voiceStateType,
                "MoveNext"
            );
            RequireMethod(
                voiceMoveNext,
                "FaceCtrl.<IE_PlayVoice>d__88.MoveNext"
            );

            _harmony.Patch(
                voiceMoveNext,
                null,
                null,
                new HarmonyMethod(voiceTranspiler)
            );

            Logger.LogInfo(
                "[NullGuardsFix] v0.1.0 active. " +
                prefixCount.ToString() +
                " Prefix guards + Voice Transpiler."
            );
        }
        catch (Exception e)
        {
            Logger.LogError("[NullGuardsFix] patch failed: " + e);

            try
            {
                if (!object.ReferenceEquals(_harmony, null))
                    _harmony.UnpatchSelf();
            }
            catch
            {
            }
        }
    }

    private void OnDestroy()
    {
        try
        {
            if (!object.ReferenceEquals(_harmony, null))
                _harmony.UnpatchSelf();
        }
        catch
        {
        }
    }

    // Reproduces the v2 guard used by:
    // AI_Main.Update/LateUpdate
    // AnimSelector.LateUpdate
    // HComboCtrl.Update/LateUpdate
    // HCtrl.Update/LateUpdate
    // InviteUI.Update/OnGUI
    private static bool HProcPrefix()
    {
        return IsHProcAlive();
    }

    // Reproduces:
    // if ((isMainGirl || isSecondGirl) && !Core.hProc) return;
    private static bool KokanPrefix(object __instance)
    {
        try
        {
            if (object.ReferenceEquals(__instance, null))
                return true;

            bool isMainGirl = Convert.ToBoolean(
                _kokanIsMainGirl.GetValue(__instance)
            );
            bool isSecondGirl = Convert.ToBoolean(
                _kokanIsSecondGirl.GetValue(__instance)
            );

            if (!isMainGirl && !isSecondGirl)
                return true;

            return IsHProcAlive();
        }
        catch (Exception e)
        {
            LogWarningOnce(
                "KokanPrefix failed open: " + e.GetType().FullName
            );
            return true;
        }
    }

    // v2 semantics:
    // if (!Core.inH) { Destroy(this); return; }
    private static bool MenuCornerPrefix(object __instance)
    {
        try
        {
            object value = _coreInH.GetValue(null);
            bool inH = Convert.ToBoolean(value);

            if (inH)
                return true;

            UnityEngine.Object unityObject =
                __instance as UnityEngine.Object;

            if (!object.ReferenceEquals(unityObject, null))
                UnityEngine.Object.Destroy(unityObject);

            return false;
        }
        catch (Exception e)
        {
            LogWarningOnce(
                "MenuCornerPrefix failed open: " + e.GetType().FullName
            );
            return true;
        }
    }

    private static bool IsHProcAlive()
    {
        try
        {
            object value = _coreHProc.GetValue(null);

            if (object.ReferenceEquals(value, null))
                return false;

            UnityEngine.Object unityObject =
                value as UnityEngine.Object;

            if (object.ReferenceEquals(unityObject, null))
                return true;

            return unityObject;
        }
        catch (Exception e)
        {
            LogWarningOnce(
                "IsHProcAlive failed open: " + e.GetType().FullName
            );
            return true;
        }
    }

    // Equivalent to v2's two checks:
    // if (!vSrc) return false;
    // if (!vSrc.clip) return false;
    private static bool VoiceSourceReady(AudioSource source)
    {
        if (!source)
            return false;

        AudioClip clip = source.clip;

        if (!clip)
            return false;

        return true;
    }

    private static IEnumerable<CodeInstruction> VoiceTranspiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator
    )
    {
        List<CodeInstruction> codes =
            new List<CodeInstruction>(instructions);

        int anchor = -1;
        int matches = 0;

        for (int i = 3; i < codes.Count - 1; i++)
        {
            if (!IsAudioSourceGetClip(codes[i]))
                continue;

            if (!IsAudioClipGetLength(codes[i + 1]))
                continue;

            if (codes[i - 1].opcode != OpCodes.Ldfld)
                continue;

            FieldInfo operandField =
                codes[i - 1].operand as FieldInfo;

            if (object.ReferenceEquals(operandField, null))
                continue;

            if (operandField.Name != "<vSrc>5__2")
                continue;

            if (codes[i - 2].opcode != OpCodes.Ldarg_0)
                continue;

            if (codes[i - 3].opcode != OpCodes.Ldloc_1)
                continue;

            anchor = i - 3;
            matches++;
        }

        if (matches != 1 || anchor < 0)
        {
            throw new Exception(
                "Voice transpiler anchor count was " +
                matches.ToString() +
                ", expected exactly 1."
            );
        }

        Label continueLabel = generator.DefineLabel();

        CodeInstruction first =
            new CodeInstruction(OpCodes.Ldarg_0);

        // Branches that originally targeted ldloc.1 must hit the new guard.
        if (codes[anchor].labels.Count > 0)
        {
            first.labels.AddRange(codes[anchor].labels);
            codes[anchor].labels.Clear();
        }

        // Preserve exception-block boundaries if Harmony attached any here.
        if (codes[anchor].blocks.Count > 0)
        {
            first.blocks.AddRange(codes[anchor].blocks);
            codes[anchor].blocks.Clear();
        }

        codes[anchor].labels.Add(continueLabel);

        List<CodeInstruction> guard =
            new List<CodeInstruction>();

        guard.Add(first);
        guard.Add(
            new CodeInstruction(
                OpCodes.Ldfld,
                _voiceSourceField
            )
        );
        guard.Add(
            new CodeInstruction(
                OpCodes.Call,
                _voiceReadyMethod
            )
        );
        guard.Add(
            new CodeInstruction(
                OpCodes.Brtrue_S,
                continueLabel
            )
        );
        guard.Add(
            new CodeInstruction(OpCodes.Ldc_I4_0)
        );
        guard.Add(
            new CodeInstruction(OpCodes.Ret)
        );

        codes.InsertRange(anchor, guard);

        if (!object.ReferenceEquals(_log, null))
        {
            _log.LogInfo(
                "[NullGuardsFix] Voice guard injected at " +
                "AudioSource.clip -> AudioClip.length path."
            );
        }

        return codes;
    }

    private static bool IsAudioSourceGetClip(
        CodeInstruction instruction
    )
    {
        if (instruction.opcode != OpCodes.Callvirt)
            return false;

        MethodInfo method =
            instruction.operand as MethodInfo;

        if (object.ReferenceEquals(method, null))
            return false;

        Type declaringType = method.DeclaringType;

        return method.Name == "get_clip" &&
            !object.ReferenceEquals(declaringType, null) &&
            declaringType.FullName == "UnityEngine.AudioSource";
    }

    private static bool IsAudioClipGetLength(
        CodeInstruction instruction
    )
    {
        if (instruction.opcode != OpCodes.Callvirt)
            return false;

        MethodInfo method =
            instruction.operand as MethodInfo;

        if (object.ReferenceEquals(method, null))
            return false;

        Type declaringType = method.DeclaringType;

        return method.Name == "get_length" &&
            !object.ReferenceEquals(declaringType, null) &&
            declaringType.FullName == "UnityEngine.AudioClip";
    }

    private static int PatchHProcGuard(
        Assembly[] assemblies,
        string typeName,
        string methodName,
        MethodInfo prefix
    )
    {
        Type type = FindType(assemblies, typeName);
        RequireType(type, typeName);

        PatchMethod(
            type,
            methodName,
            new HarmonyMethod(prefix),
            null
        );

        return 1;
    }

    private static void PatchMethod(
        Type type,
        string methodName,
        HarmonyMethod prefix,
        HarmonyMethod transpiler
    )
    {
        MethodInfo target =
            GetNoArgMethod(type, methodName);

        RequireMethod(
            target,
            type.FullName + "." + methodName
        );

        _harmony.Patch(
            target,
            prefix,
            null,
            transpiler
        );
    }

    private static MethodInfo GetNoArgMethod(
        Type type,
        string methodName
    )
    {
        if (object.ReferenceEquals(type, null))
            return null;

        return type.GetMethod(
            methodName,
            BindingFlags.Instance |
            BindingFlags.Static |
            BindingFlags.Public |
            BindingFlags.NonPublic,
            null,
            Type.EmptyTypes,
            null
        );
    }

    private static MethodInfo GetOwnMethod(
        string methodName
    )
    {
        MethodInfo method =
            typeof(KPlugNullGuardsFix).GetMethod(
                methodName,
                BindingFlags.Static |
                BindingFlags.NonPublic
            );

        RequireMethod(method, methodName);

        return method;
    }

    private static Type FindType(
        Assembly[] assemblies,
        string fullName
    )
    {
        for (int i = 0; i < assemblies.Length; i++)
        {
            Type type =
                assemblies[i].GetType(fullName, false);

            if (!object.ReferenceEquals(type, null))
                return type;
        }

        return null;
    }

    private static void RequireType(
        Type type,
        string name
    )
    {
        if (object.ReferenceEquals(type, null))
            throw new Exception("Type not found: " + name);
    }

    private static void RequireField(
        FieldInfo field,
        string name
    )
    {
        if (object.ReferenceEquals(field, null))
            throw new Exception("Field not found: " + name);
    }

    private static void RequireMethod(
        MethodInfo method,
        string name
    )
    {
        if (object.ReferenceEquals(method, null))
            throw new Exception("Method not found: " + name);
    }

    private static bool _warningLogged;

    private static void LogWarningOnce(string message)
    {
        if (_warningLogged)
            return;

        _warningLogged = true;

        if (!object.ReferenceEquals(_log, null))
            _log.LogWarning("[NullGuardsFix] " + message);
    }
}
