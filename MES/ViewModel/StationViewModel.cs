using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using MES.Comm;
using MES.Manager;
using MES.SetModel;
using PropertyChanged;

namespace MES.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class StationViewModel : ProductChangeBase
    {

        public StationViewModel()
        {
            try
            {
                StationList = new ObservableCollection<NumberGroup>(SetHelper.StationNumber.numberGroups);
            }
            catch (Exception ex)
            {
                SetHelper.ListPLCMessage.ShowInfoQueue(ex.ToString(), false, "ex");
            }
        }


        public override void GetParams(ProductTypeModel product)
        {
            stationNumberModel = SetHelper.LoadConfig<StationNumberModel>(SetHelper.stationNumber, product);
            StationList = stationNumberModel.numberGroups;
        }
        public int Number { get; set; }
        public string Name { get; set; }
        public NumberGroup SelectUser { get; set; } = new NumberGroup();

        public ObservableCollection<NumberGroup> StationList { get; set; } = new ObservableCollection<NumberGroup>();
        public StationNumberModel stationNumberModel { get; set; } = new StationNumberModel();

        //public ICommand PasswordChangeCommand => new RelayCommand<PasswordBox>((p) =>
        //{
        //    Number = p.Password;

        //});

        public ICommand SelectionChange => new RelayCommand(() =>
        {
            try
            {
                if (SelectUser != null)
                {
                    Name = SelectUser.Name;
                    Number = SelectUser.Number;
                }
            }
            catch (Exception ex)
            {
                SetHelper.ListPLCMessage.ShowInfoQueue(ex.ToString(), false, "ex");
            }
        });


        public ICommand AddCommand => new RelayCommand(() =>
        {
            try
            {
                if (SetHelper.IsAdmin)
                {
                    if (StationList.Any(x => x.Name == Name))
                    {
                        MessageBox.Show($"工位{Name}已存在");
                        return;
                    }

                    StationList.Add(new NumberGroup()
                    {
                        Name = Name,
                        Number = Number
                    });
                    SaveSys(StationList);
                }
                else
                {
                    MessageBox.Show("权限不足");
                }
            }
            catch (Exception ex)
            {
                SetHelper.ListPLCMessage.ShowInfoQueue(ex.ToString(), false, "ex");
            }
        });

        public ICommand EditCommand => new RelayCommand<NumberGroup>((user) =>
        {
            try
            {
                if (SetHelper.IsAdmin)
                {
                    user.Number = Number;
                    user.Name = Name;
                    StationList = StationList;
                    SaveSys(StationList);
                }
                else
                {
                    MessageBox.Show("权限不足");
                }
            }
            catch (Exception ex)
            {
                SetHelper.ListPLCMessage.ShowInfoQueue(ex.ToString(), false, "ex");
            }
        });

        public ICommand DeleteCommand => new RelayCommand<NumberGroup>((user) =>
        {
            try
            {
                if (SetHelper.IsAdmin)
                {
                    StationList.Remove(user);
                    SaveSys(StationList);
                }
                else
                {
                    MessageBox.Show("权限不足");
                }
            }
            catch (Exception ex)
            {
                SetHelper.ListPLCMessage.ShowInfoQueue(ex.ToString(), false, "ex");
            }

        });

        private void SaveSys(ObservableCollection<NumberGroup> GlueOnLineList)
        {
            StationNumberModel model = new StationNumberModel();
            model.numberGroups = GlueOnLineList;
            //string materialjson = JSON.ToJsonFormat(model);
            //File.WriteAllText(SetHelper.stationNumberPath, materialjson);
            SetHelper.SaveConfig(model, SetHelper.stationNumber, ProductType);
            SetHelper.StationNumber.numberGroups = GlueOnLineList;

        }
    }
}
