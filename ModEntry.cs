using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using StardewModdingAPI;

namespace DialogueDisplayFrameworkChinese
{
    public class ModEntry : Mod
    {
        private Harmony? _harmony;

        private static readonly Dictionary<string, string> Translations = new()
        {
            ["Mod Enabled"] = "启用模组",
            ["Dialogue Width Offset"] = "对话框宽度偏移",
            ["Size offset to the dialogue box's width. Negative input shrinks size.\nDon't forget to adjust the x offset."] = "对话框宽度的尺寸偏移。负数会缩小尺寸。\n别忘了调整 X 偏移。",
            ["Dialogue Height Offset"] = "对话框高度偏移",
            ["Size offset to the dialogue box's height. Negative input shrinks size.\nDon't forget to adjust the y offset."] = "对话框高度的尺寸偏移。负数会缩小尺寸。\n别忘了调整 Y 偏移。",
            ["Dialogue X Offset"] = "对话框 X 偏移",
            ["Position offset to the dialogue box's x position. Negative input moves the box to the left."] = "对话框 X 坐标的位置偏移。负数将对话框向左移动。",
            ["Dialogue Y Offset"] = "对话框 Y 偏移",
            ["Position offset to the dialogue box's y position. Negative input moves the box up."] = "对话框 Y 坐标的位置偏移。负数将对话框向上移动。"
        };

        public override void Entry(IModHelper helper)
        {
            _harmony = new Harmony(ModManifest.UniqueID);

            Assembly? targetMod = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name?.Contains("DialogueDisplayFramework", StringComparison.OrdinalIgnoreCase) == true);

            if (targetMod == null)
            {
                Monitor.Log("未找到 DialogueDisplayFramework 程序集，跳过汉化。", LogLevel.Warn);
                return;
            }

            MethodInfo transpiler = typeof(ModEntry).GetMethod(nameof(Transpiler), BindingFlags.Static | BindingFlags.NonPublic)!;
            var harmonyTranspiler = new HarmonyMethod(transpiler);
            int patchedCount = 0;

            foreach (Type type in targetMod.GetTypes())
            {
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    if (!ShouldPatch(method)) continue;
                    try
                    {
                        _harmony.Patch(method, transpiler: harmonyTranspiler);
                        patchedCount++;
                    }
                    catch { }
                }
            }
            Monitor.Log($"DialogueDisplayFramework 汉化补丁已加载，共修补 {patchedCount} 个方法。", LogLevel.Info);
        }

        private static bool ShouldPatch(MethodBase method)
        {
            string name = method.Name;
            if (!name.Contains("BuildConfigMenu") && 
                !name.Contains("RegisterConfig") && 
                !name.Contains("OnGameLaunched") &&
                !name.Contains("AddBoolOption") && 
                !name.Contains("AddNumberOption") && 
                !name.Contains("AddSectionTitle") && 
                !name.Contains("AddParagraph")) return false;
            try { return method.GetMethodBody() != null; } catch { return false; }
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldstr && instruction.operand is string original && Translations.TryGetValue(original, out string? translated))
                {
                    instruction.operand = translated;
                }
                yield return instruction;
            }
        }
    }
}
