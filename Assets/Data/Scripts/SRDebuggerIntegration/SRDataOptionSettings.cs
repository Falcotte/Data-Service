using System;
using UnityEngine;

namespace AngryKoala.Data
{
    [Serializable]
    public class SRDebuggerOptionSettings
    {
        [SerializeField] private bool _enabled;

        [SerializeField] private string _category = "Data";
        [SerializeField] private string _displayName;

        [SerializeField] private int _sort;

        [SerializeField] private SRDataOptionRange _numberRange = new();
        [SerializeField] private double _increment = 1d;

        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        public string Category
        {
            get => _category;
            set => _category = value;
        }

        public string DisplayName
        {
            get => _displayName;
            set => _displayName = value;
        }

        public int Sort
        {
            get => _sort;
            set => _sort = value;
        }

        public SRDataOptionRange NumberRange
        {
            get => _numberRange;
            set => _numberRange = value;
        }

        public double Increment
        {
            get => _increment;
            set => _increment = value;
        }
    }
}