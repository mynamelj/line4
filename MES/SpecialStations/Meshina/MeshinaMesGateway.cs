using MES.Manager;
using MES.MesModel.Request;
using Newtonsoft.Json.Linq;

namespace MES.SpecialStations.Meshina
{
    public enum MeshinaMesOutcome { Accepted, Rejected, Unknown }
    public sealed class MeshinaMesReply
    {
        public MeshinaMesOutcome Outcome { get; set; }
        public string Message { get; set; }
    }
    public interface IMeshinaMesGateway
    {
        Task<MeshinaMesReply> FeedingCheckAsync(FeedingCheckModel request);
        Task<MeshinaMesReply> CheckOutAsync(SNCheckoutModel request);
    }
    public sealed class MeshinaMesGateway : IMeshinaMesGateway
    {
        public async Task<MeshinaMesReply> FeedingCheckAsync(FeedingCheckModel request)
        {
            var result = SetHelper.resultModel[0];
            result.CheckInSN = request.SN;
            result.Result1 = "处理中";
            result.CheckOutSN = "";
            result.Result3 = "";
            var reply = await SetHelper.mesManager.FeedingCheck(request, 0, retryTransport: false);
            // 直接更新结果数组，不依赖消息页的刷新定时器。
            result.Result1 = reply.Item1 ? "OK" : "NG";
            return Classify(reply.Item1, reply.Item2, reply.Item3);
        }
        public async Task<MeshinaMesReply> CheckOutAsync(SNCheckoutModel request)
        {
            var reply = await SetHelper.mesManager.CheckOut(request, 0, retryTransport: false);
            return Classify(reply.Item1, reply.Item2, reply.Item4);
        }
        private static MeshinaMesReply Classify(bool accepted, string message, string raw)
        {
            var outcome = accepted ? MeshinaMesOutcome.Accepted : MeshinaMesOutcome.Unknown;
            if (!accepted && !string.IsNullOrWhiteSpace(raw))
            {
                try
                {
                    var result = JObject.Parse(raw).GetValue("Result", StringComparison.OrdinalIgnoreCase)?.ToString();
                    if (string.Equals(result, "FAIL", StringComparison.OrdinalIgnoreCase)) outcome = MeshinaMesOutcome.Rejected;
                }
                catch { /* 非业务响应（例如网关HTML）无法证明请求未被接收。 */ }
            }
            return new MeshinaMesReply { Outcome = outcome, Message = message };
        }
    }
}
