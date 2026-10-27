using MES.Manager;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace MES.Comm
{
    /// <summary>
    /// 固定文案和动态提示都在绑定输出时转换，切型后自动刷新。
    /// </summary>
    public sealed class MaterialTextExtension : MarkupExtension
    {
        public MaterialTextExtension() { }

        public MaterialTextExtension(string text) => Text = text;

        public string Text { get; set; }
        public string Path { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var binding = new MultiBinding
            {
                Mode = BindingMode.OneWay,
                Converter = Converter.Instance
            };
            binding.Bindings.Add(Path == null
                ? new Binding { Source = Text, Mode = BindingMode.OneWay }
                : new Binding(Path) { Mode = BindingMode.OneWay });
            binding.Bindings.Add(new Binding(nameof(MaterialDisplayText.UseOilBottle))
            {
                Source = MaterialDisplayText.Instance,
                Mode = BindingMode.OneWay
            });
            return binding.ProvideValue(serviceProvider);
        }

        private sealed class Converter : IMultiValueConverter
        {
            public static Converter Instance { get; } = new Converter();

            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                if (values[0] == DependencyProperty.UnsetValue) return "";
                return MaterialDisplayText.Format(values[0] as string, values[1] is bool useOilBottle && useOilBottle);
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
                => throw new NotSupportedException();
        }
    }
}
