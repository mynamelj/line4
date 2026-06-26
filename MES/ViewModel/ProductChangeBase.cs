using CommunityToolkit.Mvvm.ComponentModel;
using MES.Manager;
using MES.SetModel;
using PropertyChanged;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MES.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public partial class ProductChangeBase: ObservableObject
    {
        public ProductChangeBase()
        {
            productTypes = SetHelper.products;
            ProductType = SetHelper.NowProduct;
            SetHelper.dataManager.ChangeParamsAction += (product) =>
            {
                //productTypes = SetHelper.ReadSys<ObservableCollection<ProductTypeModel>>(SetHelper.productpath);
                SetHelper.NowProduct = product;
                ProductType = productTypes.FirstOrDefault(x => x.ProductID == product.ProductID);
                //ProductID = productTypes.IndexOf(ProductType);
                GetParams(product);
            };
        }
        public ObservableCollection<ProductTypeModel> productTypes { get; set; } = new ObservableCollection<ProductTypeModel>();

        public int ProductID { get; set; } = 0;
        public ProductTypeModel ProductType { get; set; } = new ProductTypeModel();

        public virtual void GetParams(ProductTypeModel productType) { }

        public ICommand ProductTypeChange => new RelayCommand<ProductTypeModel>((product) =>
        {
            //ProductType = product;
            GetParams(product);
        });
    }
}
