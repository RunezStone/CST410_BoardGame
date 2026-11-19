using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Menu: Convergence > Minions > Build Minion Canvas
// Copies the Warlock's player canvas (same layout as every other player) into a shared "Minion Canvas":
//   - the card panel that Use Deck opens (the Warlock's "ActionCards") is copied too, as "Minion Card Panel",
//     so minions never see the Warlock's cards: its slots show the active minion's cards (only Bite)
//   - the Use Deck / Next / Close buttons open, page and close that copy
//   - End Turn ends the active minion's turn (MinionManager.EndActiveMinionTurn)
// It also makes sure the scene has a MinionManager, with the Minion prefab and this canvas assigned.
// Run it again any time to rebuild (it replaces the previous copies). Undo (Ctrl+Z) reverts everything it did.
public static class MinionCanvasBuilder
{
    const string MinionPrefabPath = "Assets/Ysabelle/Player/Warlock/Minion/Minion.prefab";
    const string CanvasName = "Minion Canvas";
    const string PanelName = "Minion Card Panel";

    [MenuItem("Convergence/Minions/Build Minion Canvas")]
    public static void Build()
    {
        PlayerManager villain = Object.FindObjectsByType<PlayerManager>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(p => p.team == Team.VILLAIN && !p.isMinion && p.playerUICanvas != null);
        if (villain == null)
        {
            Debug.LogError("Build Minion Canvas: no Villain (Warlock) PlayerManager with a Player UI Canvas in the open scene.");
            return;
        }

        CardUIManager villainCards = villain.GetComponent<CardUIManager>();
        Canvas template = villain.playerUICanvas;

        Undo.SetCurrentGroupName("Build Minion Canvas");
        int undoGroup = Undo.GetCurrentGroup();

        MinionManager mm = FindOrCreateMinionManager();
        RemoveOldCopies(mm);

        // original GameObject -> its copy (used to retarget button events like SetActive)
        var copies = new Dictionary<GameObject, GameObject>();

        // ---- Copy the Warlock's canvas (Instantiate remaps references inside the copy to the copy) ----
        GameObject clone = CopyObject(template.gameObject, CanvasName);
        copies[template.gameObject] = clone;

        // ---- Copy the Warlock's card panel(s): any object outside the canvas that holds the Warlock's card slots ----
        foreach (GameObject panel in FindCardPanels(villainCards, template.transform))
        {
            GameObject panelCopy = CopyObject(panel, PanelName);
            panelCopy.SetActive(false);   // opened by Use Deck
            copies[panel] = panelCopy;
        }

        // ---- Card UI that follows the active minion ----
        CardUIManager minionCards = Undo.AddComponent<CardUIManager>(clone);
        var cardsSO = new SerializedObject(minionCards);
        cardsSO.FindProperty("useActiveMinion").boolValue = true;
        CopyCardDisplays(villainCards, copies, cardsSO.FindProperty("cardDisplays"));
        cardsSO.ApplyModifiedProperties();

        // ---- Point the copied buttons at the minion versions ----
        int rewired = 0;
        foreach (GameObject copy in copies.Values)
            foreach (Button button in copy.GetComponentsInChildren<Button>(true))
                rewired += RewireButton(button, villain, villainCards, minionCards, mm, copies);

        // ---- Hook the canvas up to MinionManager ----
        var mmSO = new SerializedObject(mm);
        mmSO.FindProperty("minionUICanvas").objectReferenceValue = clone.GetComponent<Canvas>();
        SerializedProperty prefab = mmSO.FindProperty("minionPrefab");
        if (prefab.objectReferenceValue == null)
            prefab.objectReferenceValue = AssetDatabase.LoadAssetAtPath<PlayerManager>(MinionPrefabPath);
        mmSO.ApplyModifiedProperties();

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(clone.scene);
        Selection.activeGameObject = clone;
        EditorGUIUtility.PingObject(clone);

        Debug.Log($"Built '{CanvasName}' from {template.name} ({copies.Count - 1} card panel(s) copied, " +
                  $"{rewired} button event(s) rewired) and assigned it to {mm.name}. Save the scene to keep it.", clone);
    }

    static GameObject CopyObject(GameObject original, string newName)
    {
        GameObject copy = Object.Instantiate(original, original.transform.parent);
        Undo.RegisterCreatedObjectUndo(copy, "Build Minion Canvas");
        copy.name = newName;
        copy.transform.SetSiblingIndex(original.transform.GetSiblingIndex() + 1);
        return copy;
    }

    // The objects the Warlock's Use Deck opens: the top-most canvas around each card slot that isn't inside the player canvas.
    static List<GameObject> FindCardPanels(CardUIManager villainCards, Transform templateRoot)
    {
        var panels = new List<GameObject>();
        if (villainCards == null) return panels;

        var src = new SerializedObject(villainCards).FindProperty("cardDisplays");
        for (int i = 0; i < src.arraySize; i++)
        {
            var display = src.GetArrayElementAtIndex(i).objectReferenceValue as CardDisplay;
            if (display == null || display.transform.IsChildOf(templateRoot)) continue;

            Canvas canvas = display.GetComponentInParent<Canvas>(true);
            if (canvas == null) continue;
            // Walk up to the outermost canvas that still doesn't contain the player canvas.
            Transform root = canvas.transform;
            for (Transform t = root.parent; t != null; t = t.parent)
                if (t.GetComponent<Canvas>() != null && !templateRoot.IsChildOf(t)) root = t;

            if (!panels.Contains(root.gameObject)) panels.Add(root.gameObject);
        }
        return panels;
    }

    static MinionManager FindOrCreateMinionManager()
    {
        MinionManager mm = Object.FindAnyObjectByType<MinionManager>(FindObjectsInactive.Include);
        if (mm != null) return mm;

        var go = new GameObject("MinionManager");
        Undo.RegisterCreatedObjectUndo(go, "Build Minion Canvas");
        Debug.Log("Build Minion Canvas: the scene had no MinionManager, so one was added.", go);
        return Undo.AddComponent<MinionManager>(go);
    }

    // Running the tool again replaces the previous minion canvas and card panel instead of stacking copies.
    static void RemoveOldCopies(MinionManager mm)
    {
        var mmSO = new SerializedObject(mm);
        var old = mmSO.FindProperty("minionUICanvas").objectReferenceValue as Canvas;
        if (old != null && old.gameObject.name == CanvasName)
            Undo.DestroyObjectImmediate(old.gameObject);

        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t != null && t.name == PanelName)
                Undo.DestroyObjectImmediate(t.gameObject);
    }

    // Same slots, same order as the Warlock's CardUIManager, but the copies (in the copied canvas or card panel).
    static void CopyCardDisplays(CardUIManager villainCards, Dictionary<GameObject, GameObject> copies, SerializedProperty list)
    {
        list.ClearArray();

        if (villainCards != null)
        {
            var src = new SerializedObject(villainCards).FindProperty("cardDisplays");
            for (int i = 0; i < src.arraySize; i++)
            {
                var display = src.GetArrayElementAtIndex(i).objectReferenceValue as CardDisplay;
                if (display == null) continue;

                CardDisplay copied = FindCopy(display.transform, copies)?.GetComponent<CardDisplay>();
                if (copied == null) continue;

                list.InsertArrayElementAtIndex(list.arraySize);
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = copied;
            }
        }

        // Fallback: every CardDisplay in the copies, in hierarchy order.
        if (list.arraySize == 0)
        {
            foreach (GameObject copy in copies.Values)
                foreach (CardDisplay d in copy.GetComponentsInChildren<CardDisplay>(true))
                {
                    list.InsertArrayElementAtIndex(list.arraySize);
                    list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = d;
                }
        }
    }

    // The copy of `original` (or null if it isn't inside anything that was copied).
    static Transform FindCopy(Transform original, Dictionary<GameObject, GameObject> copies)
    {
        foreach (var pair in copies)
        {
            Transform root = pair.Key.transform;
            if (original == root) return pair.Value.transform;
            if (!original.IsChildOf(root)) continue;
            return pair.Value.transform.Find(AnimationUtility.CalculateTransformPath(original, root));
        }
        return null;
    }

    // Warlock's CardUIManager -> the minion CardUIManager (same method, e.g. ShowPlayerCards / NextCards).
    // Warlock's PlayerManager.EndTurn -> MinionManager.EndActiveMinionTurn.
    // Anything inside the Warlock's canvas / card panel (e.g. Use Deck's SetActive) -> the same thing in the copy.
    static int RewireButton(Button button, PlayerManager villain, CardUIManager villainCards,
                            CardUIManager minionCards, MinionManager mm, Dictionary<GameObject, GameObject> copies)
    {
        var so = new SerializedObject(button);
        SerializedProperty calls = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
        int changed = 0;

        for (int i = 0; i < calls.arraySize; i++)
        {
            SerializedProperty call = calls.GetArrayElementAtIndex(i);
            SerializedProperty target = call.FindPropertyRelative("m_Target");
            SerializedProperty method = call.FindPropertyRelative("m_MethodName");
            SerializedProperty typeName = call.FindPropertyRelative("m_TargetAssemblyTypeName");
            Object obj = target.objectReferenceValue;

            if (villainCards != null && obj == villainCards)
            {
                target.objectReferenceValue = minionCards;
                changed++;
            }
            else if (obj == villain && method.stringValue == nameof(PlayerManager.EndTurn))
            {
                target.objectReferenceValue = mm;
                method.stringValue = nameof(MinionManager.EndActiveMinionTurn);
                typeName.stringValue = $"{typeof(MinionManager).FullName}, {typeof(MinionManager).Assembly.GetName().Name}";
                changed++;
            }
            else if (obj == villain || obj == villain.gameObject)
            {
                Debug.LogWarning($"Build Minion Canvas: '{button.name}' still calls {method.stringValue} on " +
                                 $"{villain.name}. Rewire it by hand if minions shouldn't trigger that.", button);
            }
            else if (RetargetToCopy(obj, copies) is Object copyTarget && copyTarget != obj)
            {
                target.objectReferenceValue = copyTarget;
                changed++;
            }
        }

        so.ApplyModifiedProperties();
        return changed;
    }

    // A GameObject or component in an original -> the same GameObject/component in its copy.
    static Object RetargetToCopy(Object obj, Dictionary<GameObject, GameObject> copies)
    {
        GameObject go = obj as GameObject;
        Component comp = obj as Component;
        if (go == null && comp == null) return null;

        Transform copy = FindCopy(go != null ? go.transform : comp.transform, copies);
        if (copy == null) return null;
        if (go != null) return copy.gameObject;
        return copy.GetComponent(comp.GetType());
    }
}
