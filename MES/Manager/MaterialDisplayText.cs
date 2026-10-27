using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace MES.Manager
{
    /// <summary>
    /// 仅转换界面文案，不修改接口、条码或保存的数据。
    /// </summary>
    public sealed class MaterialDisplayText : INotifyPropertyChanged
    {
        public static MaterialDisplayText Instance { get; } = new MaterialDisplayText();

        public bool UseOilBottle { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Refresh(IEnumerable<string> stationNames)
        {
            bool useOilBottle = stationNames?.Any(name => name?.Contains("5050") == true) == true;
            if (UseOilBottle == useOilBottle) return;

            UseOilBottle = useOilBottle;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UseOilBottle)));
        }

        public string Format(string text) => Format(text, UseOilBottle);

        public static string Format(string text, bool useOilBottle)
        {
            if (!useOilBottle || text == null) return text;
            return text.Replace("胶水", "油品")
                .Replace("单胶", "单油品")
                .Replace("双胶", "双油品")
                .Replace("涂胶", "油品")
                .Replace("缺胶", "缺油品");
        }
    }
}
