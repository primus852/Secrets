﻿using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Object/Primitives/Float")]
[System.Serializable]   // Is this attribute still necessary?
public class FloatValue : ScriptableObject  // Should implement ISerializationCallbackReceiver and make `value = defaultValue;`?
{
    // [SerializeField] private float defaultValue = default;
    public float value;
}
