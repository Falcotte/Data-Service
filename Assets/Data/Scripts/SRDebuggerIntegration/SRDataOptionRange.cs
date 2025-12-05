using System;
using UnityEngine;

namespace AngryKoala.Data
{
    [Serializable]
    public class SRDataOptionRange
    {
        [SerializeField] private bool _enabled;
        
        [SerializeField] private double _min;
        [SerializeField] private double _max = 1d;

        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        public double Min
        {
            get => _min;
            set => _min = value;
        }

        public double Max
        {
            get => _max;
            set => _max = value;
        }
    }
}
