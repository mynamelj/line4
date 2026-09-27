using System;

namespace MES.SetModel
{
    /// <summary>
    /// 进站结果类，用于绑定产品SN与进站结果
    /// </summary>
    public class CheckInResult
    {
        /// <summary>
        /// 产品SN
        /// </summary>
        public string SN { get; set; } = string.Empty;

        /// <summary>
        /// 进站结果 (例如: 1=OK, 2=NG, 3=空载具, 5=返修打散/返修件, 6=返修合装)
        /// </summary>
        public int Result { get; set; }

        public CheckInResult()
        {
        }

        public CheckInResult(string sn, int result)
        {
            SN = sn ?? string.Empty;
            Result = result;
        }

        // 兼容字段名
        public string sn
        {
            get => SN;
            set => SN = value;
        }

        public int 进站结果
        {
            get => Result;
            set => Result = value;
        }

        public override string ToString() => $"[SN={SN}, Result={Result}]";
    }
}
