﻿using UnityEditor;
using UnityEngine;

namespace SchwerEditor {
    public static class PrefabMenu {
        // Reference: https://docs.unity3d.com/ScriptReference/MenuItem.html

        // Editor seems to need restarting in order to reflect changes in menu item priority.
        // Reference for built-in menu item priorities: https://blog.redbluegames.com/guide-to-extending-unity-editors-menus-b2de47a746db

        // `0` is the priority for `Create Empty`.
        // Items with the same priority seem to be unpredictably sorted
        // Intended for `Instantiate Prefab` to be below `Create Empty`, but can't use priority `1` since `3D Object` uses that.

        [MenuItem("GameObject/Instantiate Prefab/Item Chest", false, 0)]
        public static void InstantiateChestPrefab(MenuCommand command) => InstantiatePrefab(command,
            "Assets/Prefabs/Objects/Chests/ItemChest.prefab");

        private static void InstantiatePrefab(MenuCommand command, string path) {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) {
                var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                GameObjectUtility.SetParentAndAlign(instance, (command.context as GameObject));
                Undo.RegisterCreatedObjectUndo(instance, "Instantiate Prefab '" + prefab.name + "'");
                Selection.activeObject = instance;
            }
            else {
                Debug.LogWarning("No prefab exists at: " + path);
            }
        }
    }
}
