﻿using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Float Meter")]
public class FloatMeter : ScriptableObject
{
    public event Action OnMinChanged;
    public event Action OnMaxChanged;
    public event Action OnCurrentChanged;

    [SerializeField] private float _min = 0;
    public float min {
        get => _min;
        set {
            _min = value;

            if (_min > _max) {
                Debug.Log("Warning: `min` (" + _min + ") is greater than `max` (" + _max + ")!");
            }

            OnMinChanged?.Invoke();
        }
    }

    [SerializeField] private float _max = 6;
    public float max {
        get => _max;
        set {
            _max = value;

            if (_max < _min) {
                Debug.Log("Warning: `max` (" + _max + ") is less than `min` (" + _min + ")!");
            }

            OnMaxChanged?.Invoke();
        }
    }

    [SerializeField] private float _current;
    public float current {
        get => _current;
        set {
            _current = value;

            if (_current > _max) {
                _current = _max;
            }
            else if (_current < _min) {
                _current = _min;
            }

            OnCurrentChanged?.Invoke();
        }
    }
}
