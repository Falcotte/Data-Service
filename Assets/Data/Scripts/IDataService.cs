using System.Collections.Generic;
using AngryKoala.Services;

namespace AngryKoala.Data
{
    public interface IDataService : IService
    {
        /// <summary>
        /// The runtime copy of a container. Logs an error and returns null when the type is not in the service's list.
        /// </summary>
        T Get<T>() where T : DataContainer;

        bool TryGet<T>(out T container) where T : DataContainer;

        /// <summary>
        /// Writes the container's file if its values changed since the last write, or if it has no file yet.
        /// </summary>
        void Save<T>() where T : DataContainer;

        void SaveAll();

        /// <summary>
        /// Puts the asset's defaults back into the runtime copy, which stays the same object, and saves.
        /// </summary>
        void Reset<T>() where T : DataContainer;

        void ResetAll();

        void GetContainers(List<DataContainer> results);
    }
}
