using System;
using UnityEngine;

namespace AngryKoala.Data
{
    [Serializable]
    public class SRDataOptionEntry
    {
        [SerializeField] private string _propertyName;

        [SerializeField] private SRDataOptionSettings _settings = new();

        public string PropertyName
        {
            get => _propertyName;
            set => _propertyName = value;
        }

        public SRDataOptionSettings Settings
        {
            get => _settings;
            set => _settings = value;
        }
    }
}