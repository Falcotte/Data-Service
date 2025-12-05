using System;
using UnityEngine;

namespace AngryKoala.Data
{
    [Serializable]
    public class SRDebuggerOptionEntry
    {
        [SerializeField] private string _propertyName;

        [SerializeField] private SRDebuggerOptionSettings _settings = new();

        public string PropertyName
        {
            get => _propertyName;
            set => _propertyName = value;
        }

        public SRDebuggerOptionSettings Settings
        {
            get => _settings;
            set => _settings = value;
        }
    }
}