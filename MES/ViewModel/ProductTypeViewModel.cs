using MES.Manager;
using MES.SetModel;
using PropertyChanged;
using S7.Net;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace MES.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class ProductTypeViewModel
    {
        public ProductTypeViewModel()
        {
            productTypeViewModels = SetHelper.products;
        }

        public string productName { get; set; } = "";
        public int productID { get; set; } = 0;
        public ProductTypeModel SelectProduct { get; set; } = new ProductTypeModel();
        public ProductTypeModel SelectComboProductFrom { get; set; } = new ProductTypeModel();
        public ProductTypeModel SelectComboProductTo { get; set; } = new ProductTypeModel();

        public ProductTypeModel SelectComboFormulaFrom { get; set; } = new ProductTypeModel();
        public ProductTypeModel SelectComboFormulaTo { get; set; } = new ProductTypeModel();

        public ObservableCollection<ProductTypeModel> productTypeViewModels { get; set; } = new ObservableCollection<ProductTypeModel>();

        public ICommand ProductTypeSelectionChange => new RelayCommand(() =>
        {
            productID = SelectProduct.ProductID;
            productName = SelectProduct.ProductName;
        });

        public ICommand ModifyCommand => new RelayCommand(() =>
        {
            if (SelectProduct.ProductName != productName)
            {
                string directoyOld = Path.Combine(SetHelper.mainpath, SelectProduct.ProductName);
                string directoyNew = Path.Combine(SetHelper.mainpath, productName);
                if (Directory.Exists(directoyNew))
                    Directory.Delete(directoyNew);
                Directory.Move(directoyOld, directoyNew);
            }


            SelectProduct.ProductID = productID;
            SelectProduct.ProductName = productName;

            SetHelper.SaveSys(productTypeViewModels, SetHelper.productpath, "产品型号");

            SetHelper.products = productTypeViewModels;
        });

        public ICommand FormulaSengCommand => new RelayCommand(() =>
        {
            if (!SetHelper.IsAdmin)
            {
                MessageBox.Show("当前权限不足");
                return;
            }
            bool res = SetHelper.dataManager.FormulaSend();
            string msg = $"配方下发{(res ? "成功" : "失败")}";
            MessageBox.Show(msg);
        });


        public ICommand AddCommand => new RelayCommand(() =>
        {
            if (productID <= 0 || string.IsNullOrEmpty(productName))
            {
                MessageBox.Show("编号或产品类型为空!");
                return;
            }
            if (productTypeViewModels.FirstOrDefault(x => x.ProductID == productID) != null || productTypeViewModels.FirstOrDefault(x => x.ProductName == productName) != null)
            {
                MessageBox.Show("该编号或产品型号已存在!");
                return;
            }
            ProductTypeModel model = new ProductTypeModel()
            {
                ProductID = productID,
                ProductName = productName
            };
            productTypeViewModels.Add(model);

            SetHelper.SaveSys(productTypeViewModels, SetHelper.productpath, "产品型号");
            SetHelper.products = productTypeViewModels;

        });

        public ICommand DeleteCommand => new RelayCommand(() =>
        {
            productTypeViewModels.Remove(SelectProduct);

            SetHelper.SaveSys(productTypeViewModels, SetHelper.productpath, "产品型号");

            SetHelper.products = productTypeViewModels;
        });
        //更改D02配置到其他型号不生效的问题
        public ICommand CopyD02Setting => new RelayCommand(() =>
        {
            var products = productTypeViewModels.Where(x =>!string.IsNullOrEmpty(x.ProductName) &&
                                                      (x.ProductName.StartsWith("D")  || 
                                                      x.ProductName.StartsWith("DJ")  ||
                                                      x.ProductName.StartsWith("DK")) &&
                                                      x.ProductName != "D02");
            var d02Product = productTypeViewModels.FirstOrDefault(x => x.ProductName == "D02");
            foreach (var item in products)
            {
                var Mes = SetHelper.LoadConfig<MesSettingModel>(SetHelper.mes, d02Product);
                var Api = SetHelper.LoadConfig<ApiSettingModel>(SetHelper.api, d02Product);
                var scan = SetHelper.LoadConfig<List<ScanSettingModel>>(SetHelper.scan, d02Product);
                var plc = SetHelper.LoadConfig<PLCSettingModel>(SetHelper.plc, d02Product);

                var alarm = SetHelper.LoadConfig<List<AlarmData>>(SetHelper.alarm, d02Product);
                //var glue = SetHelper.LoadConfig<List<MaterailOnOffModel>>(SetHelper.glue, d02Product);
                var stationNumber = SetHelper.LoadConfig<StationNumberModel>(SetHelper.stationNumber, d02Product);
                var opid = SetHelper.LoadConfig<List<OPID>>(SetHelper.opid, d02Product);
                var openBoxCode = SetHelper.LoadConfig<List<List<OpenBox>>>(SetHelper.openBoxCode, d02Product);
                var lightConfig = SetHelper.LoadConfig<ObservableCollection<LightConfig>>(SetHelper.lightConfig, d02Product);

                //AddTags(plc, Api);

                SetHelper.SaveConfig(Mes, SetHelper.mes, item);
                SetHelper.SaveConfig(Api, SetHelper.api, item);
                SetHelper.SaveConfig(scan, SetHelper.scan, item);
                SetHelper.SaveConfig(plc, SetHelper.plc, item);
                SetHelper.SaveConfig(alarm, SetHelper.alarm, item);
                // SetHelper.SaveConfig(glue, SetHelper.glue, item);
                SetHelper.SaveConfig(stationNumber, SetHelper.stationNumber, item);
                SetHelper.SaveConfig(opid, SetHelper.opid, item);
                SetHelper.SaveConfig(openBoxCode, SetHelper.openBoxCode, item);
                SetHelper.SaveConfig(lightConfig, SetHelper.lightConfig, item);
            }

            foreach (var item in SetHelper.products)
            {
                var plc = SetHelper.LoadConfig<PLCSettingModel>(SetHelper.plc, item);

                var group = plc.ListGroup.FirstOrDefault(x => x.GroupType == PLCGroupName.WriteGroup.ToString());
                var db = group.ListTag.FirstOrDefault(x => !x.TagName.Contains("_")).TagDbArea;
                var tag = group.ListTag.FirstOrDefault(x => x.TagName == "操作权限");
                if (tag == null)
                {
                    group.ListTag.Add(new PLCTag()
                    {
                        TagAddress = "0.7",
                        TagDbArea = db,
                        DataType = "bool",
                        LowLimit = -100000,
                        UpLimit = 100000,
                        TagDesc = "",
                        TagName = "操作权限"
                    });
                }
                SetHelper.SaveConfig(plc, SetHelper.plc, item);
            }
            MessageBox.Show($"复制成功");
        });

        public ICommand CopyCommand => new RelayCommand(() =>
        {
            try
            {
                if (!SetHelper.IsAdmin)
                {
                    MessageBox.Show("当前权限不足");
                    return;
                }

                var Mes = SetHelper.LoadConfig<MesSettingModel>(SetHelper.mes, SelectComboProductFrom);
                var Api = SetHelper.LoadConfig<ApiSettingModel>(SetHelper.api, SelectComboProductFrom);
                var scan = SetHelper.LoadConfig<List<ScanSettingModel>>(SetHelper.scan, SelectComboProductFrom);
                var plc = SetHelper.LoadConfig<PLCSettingModel>(SetHelper.plc, SelectComboProductFrom);

                var alarm = SetHelper.LoadConfig<List<AlarmData>>(SetHelper.alarm, SelectComboProductFrom);
                //var glue = SetHelper.LoadConfig<List<MaterailOnOffModel>>(SetHelper.glue, SelectComboProductFrom);
                var stationNumber = SetHelper.LoadConfig<StationNumberModel>(SetHelper.stationNumber, SelectComboProductFrom);
                var opid = SetHelper.LoadConfig<List<OPID>>(SetHelper.opid, SelectComboProductFrom);
                var openBoxCode = SetHelper.LoadConfig<List<List<OpenBox>>>(SetHelper.openBoxCode, SelectComboProductFrom);
                var lightConfig = SetHelper.LoadConfig<ObservableCollection<LightConfig>>(SetHelper.lightConfig, SelectComboProductFrom);

                AddTags(plc, Api);

                SetHelper.SaveConfig(Mes, SetHelper.mes, SelectComboProductTo);
                SetHelper.SaveConfig(Api, SetHelper.api, SelectComboProductTo);
                SetHelper.SaveConfig(scan, SetHelper.scan, SelectComboProductTo);
                SetHelper.SaveConfig(plc, SetHelper.plc, SelectComboProductTo);
                SetHelper.SaveConfig(alarm, SetHelper.alarm, SelectComboProductTo);
                //SetHelper.SaveConfig(glue, SetHelper.glue, SelectComboProductTo);
                SetHelper.SaveConfig(stationNumber, SetHelper.stationNumber, SelectComboProductTo);
                SetHelper.SaveConfig(opid, SetHelper.opid, SelectComboProductTo);
                SetHelper.SaveConfig(openBoxCode, SetHelper.openBoxCode, SelectComboProductTo);
                SetHelper.SaveConfig(lightConfig, SetHelper.lightConfig, SelectComboProductTo);
                MessageBox.Show($"{SelectComboProductFrom.ProductName}→{SelectComboProductTo.ProductName} 复制成功");
            }
            catch (Exception ex)
            {
                SetHelper.ListMesMessage.ShowInfoQueue(ex.ToString());
            }

        });

        public ICommand CopyOriginalCommand => new RelayCommand(() =>
        {
            try
            {
                if (!SetHelper.IsAdmin)
                {
                    MessageBox.Show("当前权限不足");
                    return;
                }

                if (productTypeViewModels.Count <= 0)
                {
                    MessageBox.Show("请先配置产品型号");
                    return;
                }

                var Mes = SetHelper.LoadConfig<MesSettingModel>(SetHelper.mes, null);
                var Api = SetHelper.LoadConfig<ApiSettingModel>(SetHelper.api, null);
                var scan = SetHelper.LoadConfig<List<ScanSettingModel>>(SetHelper.scan, null);
                var plc = SetHelper.LoadConfig<PLCSettingModel>(SetHelper.plc, null);
                var alarm = SetHelper.LoadConfig<List<AlarmData>>(SetHelper.alarm, null);
                //var glue = SetHelper.LoadConfig<List<MaterailOnOffModel>>(SetHelper.glue, null);
                var stationNumber = SetHelper.LoadConfig<StationNumberModel>(SetHelper.stationNumber, null);
                var opid = SetHelper.LoadConfig<List<OPID>>(SetHelper.opid, null);
                var openBoxCode = SetHelper.LoadConfig<List<List<OpenBox>>>(SetHelper.openBoxCode, null);
                var lightConfig = SetHelper.LoadConfig<ObservableCollection<LightConfig>>(SetHelper.lightConfig, null);

                AddTags(plc, Api);

                foreach (var item in productTypeViewModels)
                {
                    SetHelper.SaveConfig(Mes, SetHelper.mes, item);
                    SetHelper.SaveConfig(Api, SetHelper.api, item);
                    SetHelper.SaveConfig(scan, SetHelper.scan, item);
                    SetHelper.SaveConfig(plc, SetHelper.plc, item);

                    SetHelper.SaveConfig(alarm, SetHelper.alarm, item);
                    //SetHelper.SaveConfig(glue, SetHelper.glue, item);
                    SetHelper.SaveConfig(stationNumber, SetHelper.stationNumber, item);
                    SetHelper.SaveConfig(opid, SetHelper.opid, item);
                    SetHelper.SaveConfig(openBoxCode, SetHelper.openBoxCode, item);
                    SetHelper.SaveConfig(lightConfig, SetHelper.lightConfig, item);

                }
                MessageBox.Show("原始配置复制到所有型号成功");
            }
            catch (Exception ex)
            {
                SetHelper.ListMesMessage.ShowInfoQueue(ex.ToString());
            }
        });

        public ICommand AddOEETag => new RelayCommand(() =>
        {
            foreach (var item in SetHelper.products)
            {
                var plc = SetHelper.LoadConfig<PLCSettingModel>(SetHelper.plc, item);
                var Api = SetHelper.LoadConfig<ApiSettingModel>(SetHelper.api, item);
                AddTags1(plc, Api);
                SetHelper.SaveConfig(plc, SetHelper.plc, item);
                SetHelper.SaveConfig(Api, SetHelper.api, item);
            }
            MessageBox.Show("添加成功");
        });


        public ICommand CopyFormulaCommand => new RelayCommand(() =>
        {
            try
            {
                if (!SetHelper.IsAdmin)
                {
                    MessageBox.Show("当前权限不足");
                    return;
                }

                if (string.IsNullOrEmpty(SelectComboFormulaFrom.ProductName) || string.IsNullOrEmpty(SelectComboFormulaTo.ProductName))
                {
                    MessageBox.Show("配方数据源未选择");
                    return;
                }

                var plcFrom = SetHelper.LoadConfig<PLCSettingModel>(SetHelper.plc, SelectComboFormulaFrom);
                var plcTo = SetHelper.LoadConfig<PLCSettingModel>(SetHelper.plc, SelectComboFormulaTo);

                var tags = plcFrom.ListGroup.FirstOrDefault(x => x.GroupType == PLCGroupName.FormulaGroup.ToString())?.ListTag;
                if (tags == null || tags.Count <= 0)
                {
                    MessageBox.Show("配方未设置");
                    return;
                }
                plcTo.ListGroup.FirstOrDefault(x => x.GroupType == PLCGroupName.FormulaGroup.ToString()).ListTag = tags;

                SetHelper.SaveConfig(plcTo, SetHelper.plc, SelectComboFormulaTo);

                MessageBox.Show($"{SelectComboFormulaFrom.ProductName}→{SelectComboFormulaTo.ProductName} 配方复制成功");
            }
            catch (Exception ex)
            {
                SetHelper.ListMesMessage.ShowInfoQueue(ex.ToString());
            }

        });

        public ICommand CopyAllFormulaCommand => new RelayCommand(() =>
        {
            try
            {
                if (!SetHelper.IsAdmin)
                {
                    MessageBox.Show("当前权限不足");
                    return;
                }

                if (productTypeViewModels.Count <= 0)
                {
                    MessageBox.Show("请先配置产品型号");
                    return;
                }

                if (string.IsNullOrEmpty(SelectComboFormulaFrom.ProductName))
                {
                    MessageBox.Show("配方数据源未选择");
                    return;
                }

                var plcFrom = SetHelper.LoadConfig<PLCSettingModel>(SetHelper.plc, SelectComboFormulaFrom);
                var tags = plcFrom.ListGroup.FirstOrDefault(x => x.GroupType == PLCGroupName.FormulaGroup.ToString())?.ListTag;
                if (tags == null || tags.Count <= 0)
                {
                    MessageBox.Show("配方未设置");
                    return;
                }

                foreach (var item in productTypeViewModels)
                {
                    var plcTo = SetHelper.LoadConfig<PLCSettingModel>(SetHelper.plc, item);

                    plcTo.ListGroup.FirstOrDefault(x => x.GroupType == PLCGroupName.FormulaGroup.ToString()).ListTag = tags;

                    SetHelper.SaveConfig(plcTo, SetHelper.plc, item);
                }
                MessageBox.Show($"{SelectComboFormulaFrom.ProductName}配方复制到所有型号成功");
            }
            catch (Exception ex)
            {
                SetHelper.ListMesMessage.ShowInfoQueue(ex.ToString());
            }
        });

        public void AddTags1(PLCSettingModel plc, ApiSettingModel api)
        {
            var group = plc.ListGroup.FirstOrDefault(x => x.GroupType == PLCGroupName.ReadGroup.ToString());
            var db = group.ListTag.FirstOrDefault(x => !x.TagName.Contains("_")).TagDbArea;
            var tag = group.ListTag.FirstOrDefault(x => x.TagName == "设备报警");
            if (tag == null)
            {
                group.ListTag.Add(new PLCTag()
                {
                    TagAddress = "1280",
                    TagDbArea = db,
                    DataType = "int",
                    LowLimit = -100000,
                    UpLimit = 100000,
                    TagDesc = "",
                    TagName = "设备报警"
                });
            }

            //group = plc.ListGroup.FirstOrDefault(x => x.GroupType == PLCGroupName.OeeGroup.ToString());
            ////db = group.ListTag.FirstOrDefault(x => !x.TagName.Contains("_")).TagDbArea;
            //tag = group.ListTag.FirstOrDefault(x => x.TagName == "Takt_Time");
            //if (tag == null)
            //{
            //    group.ListTag.Add(new PLCTag()
            //    {
            //        TagAddress = "552",
            //        TagDbArea = db,
            //        DataType = "real",
            //        LowLimit = -100000,
            //        UpLimit = 100000,
            //        TagDesc = "实际CT",
            //        TagName = "Takt_Time"
            //    });
            //}

            //tag = group.ListTag.FirstOrDefault(x => x.TagName == "Cycle_Time");
            //if (tag == null)
            //{
            //    group.ListTag.Add(new PLCTag()
            //    {
            //        TagAddress = "556",
            //        TagDbArea = db,
            //        DataType = "real",
            //        LowLimit = -100000,
            //        UpLimit = 100000,
            //        TagDesc = "设定CT",
            //        TagName = "Cycle_Time"
            //    });
            //}

            //tag = group.ListTag.FirstOrDefault(x => x.TagName == "Eq_Active_Rate");
            //if (tag == null)
            //{
            //    group.ListTag.Add(new PLCTag()
            //    {
            //        TagAddress = "560",
            //        TagDbArea = db,
            //        DataType = "real",
            //        LowLimit = -100000,
            //        UpLimit = 100000,
            //        TagDesc = "机台稼动率",
            //        TagName = "Eq_Active_Rate"
            //    });
            //}
        }




        public void AddTags(PLCSettingModel plc, ApiSettingModel Api)
        {
            var group = plc.ListGroup.FirstOrDefault(x => x.GroupType == PLCGroupName.TriggerGroup.ToString());
            var db = group.ListTag.FirstOrDefault(x => !x.TagName.Contains("_")).TagDbArea;
            var tag = group.ListTag.FirstOrDefault(x => x.TagName == "产品型号获取");
            if (tag == null)
            {
                group.ListTag.Add(new PLCTag()
                {
                    TagAddress = "297.1",
                    TagDbArea = db,
                    DataType = "bool",
                    LowLimit = -100000,
                    UpLimit = 100000,
                    TagDesc = "",
                    TagName = "产品型号获取"
                });
            }

            group = plc.ListGroup.FirstOrDefault(x => x.GroupType == PLCGroupName.TriggerGroup.ToString());
            db = group.ListTag.FirstOrDefault(x => !x.TagName.Contains("_")).TagDbArea;
            tag = group.ListTag.FirstOrDefault(x => x.TagName == "产品型号切换");
            if (tag == null)
            {
                group.ListTag.Add(new PLCTag()
                {
                    TagAddress = "470",
                    TagDbArea = db,
                    DataType = "int",
                    LowLimit = -100000,
                    UpLimit = 100000,
                    TagDesc = "",
                    TagName = "产品型号切换"
                });
            }


            group = plc.ListGroup.FirstOrDefault(x => x.GroupType == PLCGroupName.WriteGroup.ToString());
            db = group.ListTag.FirstOrDefault(x => !x.TagName.Contains("_")).TagDbArea;
            tag = group.ListTag.FirstOrDefault(x => x.TagName == "产品型号");
            if (tag == null)
            {
                group.ListTag.Add(new PLCTag()
                {
                    TagAddress = "128",
                    TagDbArea = db,
                    DataType = "int",
                    LowLimit = -100000,
                    UpLimit = 100000,
                    TagDesc = "",
                    TagName = "产品型号"
                });
            }

            tag = group.ListTag.FirstOrDefault(x => x.TagName == "产品型号结果");
            if (tag == null)
            {
                group.ListTag.Add(new PLCTag()
                {
                    TagAddress = "0.6",
                    TagDbArea = db,
                    DataType = "bool",
                    LowLimit = -100000,
                    UpLimit = 100000,
                    TagDesc = "",
                    TagName = "产品型号结果"
                });
            }



            group = plc.ListGroup.FirstOrDefault(x => x.GroupType == PLCGroupName.FormulaGroup.ToString());
            if (group == null)
            {
                plc.ListGroup.Add(new PLCGroup()
                {
                    GroupDesc = "",
                    GroupName = "配方写入",
                    GroupType = PLCGroupName.FormulaGroup.ToString(),
                    ListTag = new ObservableCollection<PLCTag>(),
                    RefreshTime = 200
                });
            }

            #region ReadGroup

            #endregion

            foreach (var item in Api.ListGroup)
            {
                item.OEEApi = "StandAlone/OEEDataCollection";
                item.ChangeProductTypeApi = "StandAlone/Query_WO";
            }
        }

        public int StartAddr { get; set; } = 3500;
        public int Count { get; set; } = 10;
        private List<string> tagNames1 = new List<string>() { "精追扫描数量", "相机程序号", "气密程序号", "助力臂程序号", "旋转前拧紧螺丝数量", "旋转后拧紧螺丝数量", "旋转位置", "供钉机1备钉总数", "供钉机2备钉总数", "供钉机3备钉总数" };
        private List<string> tagNames2 = new List<string>() { "枪号", "程序号", "批头号", "供钉机号", };
        public ICommand AddFormulaDataCommand => new RelayCommand(() =>
        {
            if (!SetHelper.IsAdmin)
            {
                MessageBox.Show("当前权限不足");
                return;
            }
            if (SelectComboFormulaFrom.ProductName == null)
            {
                MessageBox.Show("请选择型号");
                return;
            }
            var plc = SetHelper.LoadConfig<PLCSettingModel>(SetHelper.plc, SelectComboFormulaFrom);
            var group = plc.ListGroup.FirstOrDefault(x => x.GroupType == PLCGroupName.FormulaGroup.ToString());
            var db = plc.ListGroup.FirstOrDefault(x => x.GroupType == PLCGroupName.WriteGroup.ToString())?.ListTag.FirstOrDefault().TagDbArea.Obj2Int();
            group.ListTag?.Clear();

            for (int i = 0; i < tagNames1.Count; i++)
            {
                var tag = group.ListTag?.FirstOrDefault(x => x.TagName == tagNames1[i]);
                if (tag == null)
                {
                    group.ListTag.Add(new PLCTag()
                    {
                        TagAddress = (StartAddr + i * 2).ToString(),
                        TagDbArea = db.Obj2Int(),
                        DataType = "int",
                        LowLimit = -100000,
                        UpLimit = 100000,
                        TagDesc = "",
                        TagValue = 1,
                        TagName = tagNames1[i]
                    });
                }
            }
            for (int j = 0; j < Count; j++)
            {
                for (int i = 0; i < tagNames2.Count; i++)
                {
                    var tag = group.ListTag.FirstOrDefault(x => x.TagName == tagNames2[i]);
                    if (tag == null)
                    {
                        group.ListTag.Add(new PLCTag()
                        {
                            TagAddress = (StartAddr + 40 + 20 * j + i * 2).ToString(),
                            TagDbArea = db.Obj2Int(),
                            DataType = "int",
                            LowLimit = -100000,
                            UpLimit = 100000,
                            TagDesc = "",
                            TagValue = 1,
                            TagName = "螺丝" + (j + 1) + tagNames2[i]
                        });
                    }
                }
            }
            SetHelper.SaveConfig(plc, SetHelper.plc, SelectComboFormulaFrom);
            MessageBox.Show($"配方添加到{SelectComboFormulaFrom.ProductName}成功");
        });

        public bool TestID { get; set; } = true;
        public int TestType { get; set; } = 1;
        public ICommand TestCommand => new RelayCommand(() =>
        {
            SetHelper.dataManager.Siemens_OnDataChange("产品型号获取", 2345, 1, TestID);
        });
        public ICommand ChangeCommand => new RelayCommand(() =>
        {
            SetHelper.dataManager.Siemens_OnDataChange("产品型号切换", 2345, 1, TestType);
        });
    }
}
