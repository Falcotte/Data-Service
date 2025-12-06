using System.ComponentModel;
using AngryKoala.Data;
using AngryKoala.Services;
using SRDebugger;

public partial class SROptions
{
    private IDataService _dataService;

    private IDataService DataService
    {
        get
        {
            if (_dataService == null)
            {
                _dataService = ServiceLocator.Get<IDataService>();
            }

            return _dataService;
        }
    }
}