using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MES.Manager
{
    public static class GlueStationRules
    {
        public const string FirstType = "第一种";
        public const string SecondType = "第二种";

        public static bool IsGlueStation(string setting) => setting == "1" || setting == "2";

        public static IEnumerable<string> GetLocations(string stationName, string setting)
        {
            if (IsGlueStation(setting)) yield return stationName;
        }

        // 单胶只匹配工位；双胶额外匹配MesMsg中的种类。
        public static bool ShouldReplace(string location, string mesMsg,
            string stationName, string setting, string glueType)
            => MatchesStation(location, stationName)
                && (setting == "1" || (setting == "2" && mesMsg == glueType));

        public static bool MatchesStation(string location, string stationName)
            => !string.IsNullOrEmpty(location) && location == stationName;

        // 上下料均使用真实工位索引；下拉框索引不等于MES工位索引。
        public static int ResolveStationIndex(string location, IEnumerable<string> stationNames)
        {
            if (string.IsNullOrEmpty(location)) return -1;
            return stationNames.ToList().IndexOf(location);
        }

        /// <summary>
        /// 逐一校验已上料的胶水，全部完成后汇总；任何一次失败都不能被后续成功覆盖。
        /// </summary>
        public static async Task<(bool Success, string Message)> CheckAsync(
            IEnumerable<string> glueCodes, Func<string, Task<(bool Success, string Message)>> check)
        {
            var codes = glueCodes.Where(code => !string.IsNullOrWhiteSpace(code)).ToList();
            if (codes.Count == 0)
                return (false, "胶水未上料，请扫描胶水码上料后重新进站");

            var failures = new List<string>();
            foreach (var code in codes)
            {
                try
                {
                    var response = await check(code);
                    if (!response.Success) failures.Add($"胶水码:{code}，{response.Message}");
                }
                catch (Exception ex)
                {
                    failures.Add($"胶水码:{code}，校验异常:{ex.Message}");
                }
            }
            return (failures.Count == 0, string.Join("\r\n", failures));
        }
    }
}
