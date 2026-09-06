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
        Task<MeshinaMesReply> CheckInAsync(SNCheckINModel request);
        Task<MeshinaMesReply> CheckOutAsync(SNCheckoutModel request);
    }
    public sealed class MeshinaMesGateway : IMeshinaMesGateway
    {
        private readonly int index;
        public MeshinaMesGateway(int index) { this.index = index; }
        public async Task<MeshinaMesReply> CheckInAsync(SNCheckINModel request)
        {
            var reply = await SetHelper.mesManager.CheckIn(request, index, retryTransport: false);
            return Classify(reply.Item1, reply.Item2, reply.Item4);
        }
        public async Task<MeshinaMesReply> CheckOutAsync(SNCheckoutModel request)
        {
            var reply = await SetHelper.mesManager.CheckOut(request, index, retryTransport: false);
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
