using MES.Manager;
using MES.SetModel;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace MES.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class TestViewModel
    {
        // 下拉框选项：组名称
        public List<string> GroupList { get; set; } = new List<string> { "写入组", "读取组", "触发组","出站读取组" };
        public string SelectedGroup { get; set; } = "写入组";

        // 输入框：标签名
        public string TagName { get; set; }

        // 下拉框选项：数据类型
        public List<string> TypeList { get; set; } = new List<string> { "string", "int", "bool" ,"float"};
        public string SelectedType { get; set; } = "string";

        // 输入框：写入的数值
        public string WriteValue { get; set; }
        public ICommand ReadCommand => new RelayCommand(() =>
        {
            if (string.IsNullOrEmpty(TagName))
            {
                MessageBox.Show("请输入标签名");
                return;
            }
            // 映射 UI 选中的组到程序内的枚举 PLCGroupName
            PLCGroupName group;
            switch (SelectedGroup)
            {
                case "触发组": group = PLCGroupName.TriggerGroup; break;
                case "读取组": group = PLCGroupName.ReadGroup; break;
                case "出站读取组": group = PLCGroupName.CheckOutGroup; break;
                default: group = PLCGroupName.WriteGroup; break;
            }

            object data = new object();
            bool result = SetHelper.siemens.ReadItem(group, TagName, ref data);

            if (!result)
            {
                MessageBox.Show("读取失败");
                return;
            }

            try
            {
                string displayValue;
                switch (SelectedType)
                {
                    case "int":
                        displayValue = data.Obj2Int().ToString();
                        break;
                    case "float":
                        // 使用 Convert.ToSingle 或 ToString 直接显示
                        if (data is float || data is double)
                            displayValue = data.ToString();
                        else
                            displayValue = Convert.ToSingle(data).ToString();
                        break;
                    case "bool":
                        // PLC 的 bool 可能是 true/false 或 0/1
                        if (data is bool b)
                            displayValue = b.ToString();
                        else
                            displayValue = (data.Obj2Int() != 0).ToString();
                        break;
                    default:
                        // string 类型
                        displayValue = data.Obj2String();
                        break;
                }
                MessageBox.Show($"类型: {SelectedType}\r\n值: {displayValue}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"数据转换失败: {ex.Message}\r\n原始值类型: {data?.GetType().FullName}\r\n原始值: {data}");
            }

        });

        // 写入按钮命令
        public ICommand WriteCommand => new RelayCommand(() =>
        {

            if (string.IsNullOrEmpty(TagName))
            {
                MessageBox.Show("请输入标签名");
                return;
            }

            // 映射 UI 选中的组到程序内的枚举 PLCGroupName
            PLCGroupName group;
            switch (SelectedGroup)
            {
                case "触发组": group = PLCGroupName.TriggerGroup; break;
                case "读取组": group = PLCGroupName.ReadGroup; break;
                case "出站读取组": group = PLCGroupName.CheckOutGroup; break;
                default: group = PLCGroupName.WriteGroup; break;
            }

            // 数据转换处理
            object data;
            try
            {
                switch (SelectedType)
                {
                    case "int":
                        data = int.Parse(WriteValue);
                        break;
                    case "float":
                        data = float.Parse(WriteValue);
                        break;
                    case "bool":
                        // 支持 true/false 或 1/0
                        if (WriteValue == "1") data = true;
                        else if (WriteValue == "0") data = false;
                        else data = bool.Parse(WriteValue);
                        break;
                    default:
                        data = WriteValue;
                        break;
                }
            }
            catch (Exception)
            {
                MessageBox.Show($"数据格式错误：无法将 '{WriteValue}' 转换为 {SelectedType} 类型");
                return;
            }

            // 执行写入操作
            // 注意：如果配置中标签名带 _1 后缀，输入时也需要带上，或者在此处代码逻辑中根据工位号补充
            bool result = SetHelper.siemens.WriteItem(group, TagName, data);

            if (result)
            {
                MessageBox.Show("写入成功");
            }
            else
            {
                MessageBox.Show("写入失败，请检查 PLC 连接或标签名是否存在于该组中");
            }
        });
    }

}
