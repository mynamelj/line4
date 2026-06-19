using MES.Manager;
using MES.SetModel;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MES.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class MesInfoViewModel
    {
        public MesInfoViewModel()
        {
            for (int i = 1; i <= SetHelper.MesSetting.MaterialCount; i++)
            {
                MesInfos.MaterialCode.Add(new MaterialInfo() { MaterialName = "材料" + i });
            }
            SetHelper.dataManager.RecItemEvent += DataManager_RecItemEvent;
        }

        private void DataManager_RecItemEvent(MesInfo data)
        {
            MesInfos = data;
        }

        public MesInfo MesInfos { get; set; } = new MesInfo();
    }
}
