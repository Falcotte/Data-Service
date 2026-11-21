using System;

namespace AngryKoala.Data
{
    /// <summary>
    /// Says how a <see cref="DataContainer"/> class lives. Every container class needs one.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class DataFileAttribute : Attribute
    {
        public DataPersistence Persistence { get; }

        /// <summary>
        /// File name without extension. Defaults to the class name. Set it before renaming a class that has saves.
        /// </summary>
        public string FileName { get; set; }

        public bool Obfuscate { get; set; }

        public DataFileAttribute(DataPersistence persistence)
        {
            Persistence = persistence;
        }
    }
}
