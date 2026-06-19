using MES.Comm;
using MES.Manager;
using MES.SetModel;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Effects;
using System.Windows.Media.Media3D;

namespace MES.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class CopyToViewModel
    {
        public CopyToViewModel()
        {
            productTypes = SetHelper.products;
            //LocationList = SetHelper.LoadConfig<StationNumberModel>(SetHelper.stationNumber, ProductType)?.numberGroups;
        }
        public ObservableCollection<ProductTypeModel> productTypes { get; set; } = new ObservableCollection<ProductTypeModel>();
        public ObservableCollection<NumberGroup> LocationList { get; set; } = new ObservableCollection<NumberGroup>();
        public ProductTypeModel ProductType { get; set; } = new ProductTypeModel();
        public NumberGroup LocationItem { get; set; } = new NumberGroup();
        public MaterailModel Material { get; set; } = new MaterailModel();

        public ICommand ProductTypeChange => new RelayCommand<ProductTypeModel>((product) =>
        {
            try
            {
                LocationList = SetHelper.LoadConfig<StationNumberModel>(SetHelper.stationNumber, product)?.numberGroups;
            }
            catch (Exception ex)
            {
                SetHelper.ListMesMessage.ShowInfoQueue(ex.ToString());
            }
        });

        public ICommand CopyCommand => new RelayCommand(() =>
        {
            try
            {
                if (ProductType.ProductID <= 0 || LocationItem.Number <= 0)
                {
                    MessageBox.Show("请选择产品型号和工位号");
                    return;
                }
                var materials = SetHelper.ReadSys<ObservableCollection<MaterailModel>>(SetHelper.materialpath);
                var mate = JSON.FromJson<MaterailModel>(JSON.ToJson(Material));
                var nowMaterial = materials.FirstOrDefault(x => x.GlueCode == Material.GlueCode);
                if (nowMaterial == null)
                {
                    mate.LocationNo = LocationItem.Name;
                    materials.Add(mate);
                    SetHelper.SaveSys(materials, SetHelper.materialpath);
                    MessageBox.Show("复制完成");
                }
                else
                {
                    //mate.LocationNo = LocationItem.Name;
                    //materials.Add(mate);
                    //SetHelper.SaveConfig(materials, SetHelper.material, ProductType);
                    MessageBox.Show("记录已存在");
                }
            }
            catch (Exception EX)
            {
                SetHelper.ListMesMessage.ShowInfoQueue(EX.ToString());
            }

        });
    }
}
