#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public class DataWorkbenchWindow : OdinEditorWindow
{
    [MenuItem("Tools/Data Workbench")]
    private static void Open() => GetWindow<DataWorkbenchWindow>().Show();

    [TabGroup("Skills")] public Data.SkillDefinition[] skills;
    [TabGroup("Items")] public Data.ItemDefinition[] items;
    [TabGroup("Status")] public Data.StatusEffectDefinition[] statuses;

    protected override void OnEnable()
    {
        base.OnEnable();
        skills = Resources.FindObjectsOfTypeAll<Data.SkillDefinition>();
        items = Resources.FindObjectsOfTypeAll<Data.ItemDefinition>();
        statuses = Resources.FindObjectsOfTypeAll<Data.StatusEffectDefinition>();
    }
}
#endif
