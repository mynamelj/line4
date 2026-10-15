using System;

namespace MES.Manager
{
    internal static class FoolproofTestRules
    {
        // 内部防呆标识，供进站流程沿用；MES实际返回 PASS + FD。
        public const string MesResult = "FDCSJ";
        public const int CheckInResult = 7;
        private const string TagPrefix = "fd_";

        public static bool IsFoolproofTest(string result)
        {
            return string.Equals(result, MesResult, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsFoolproofTest(string result, string message)
        {
            return string.Equals(result, "PASS", StringComparison.OrdinalIgnoreCase)
                && string.Equals(message, "FD", StringComparison.OrdinalIgnoreCase);
        }

        public static bool ShouldReadCheckOutTag(string tagName, string stationNumber, int checkInResult)
        {
            if (!tagName.EndsWith("_" + stationNumber, StringComparison.Ordinal))
                return false;

            bool hasFoolproofPrefix = tagName.StartsWith(TagPrefix, StringComparison.OrdinalIgnoreCase);
            if (checkInResult == CheckInResult)
            {
                // 防呆件：只读取并上传 fd_ 前缀的标签。
                return hasFoolproofPrefix;
            }

            // 普通件及其他非防呆状态：过滤掉 fd_ 标签，不读取、不上传。
            return !hasFoolproofPrefix;
        }

        public static string GetMesItemName(string tagName)
        {
            string item = tagName.Substring(0, tagName.LastIndexOf('_'));
            return item.StartsWith(TagPrefix, StringComparison.OrdinalIgnoreCase)
                ? item.Substring(TagPrefix.Length)
                : item;
        }
    }
}
