using PropertyChanged;

namespace MES.SetModel
{
    [AddINotifyPropertyChangedInterface]
    public class ProductTypeModel
    {
        /// <summary>
        /// 产品型号ID 对应PLC中产品型号
        /// </summary>
        public int ProductID { get; set; }
        /// <summary>
        /// 产品型号名称
        /// </summary>
        public string ProductName { get; set; }
    }
}
