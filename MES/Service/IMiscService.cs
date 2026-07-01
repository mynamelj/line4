using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MES.Service
{
    public interface IMiscService
    {
        ObservableCollection<SNPrefix> SNPrefixes { get; set; }

        void ReloadSettings();

        void SaveSettings();

    }
}
