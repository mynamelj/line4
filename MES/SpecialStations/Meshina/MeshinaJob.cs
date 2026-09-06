using MES.MesModel.Request;

namespace MES.SpecialStations.Meshina
{
    public enum MeshinaStage { FeedingCheckSending, FeedingCheckRejected, WaitingForMdb,
        ReadyForCheckOut, CheckOutSending, CheckOutRejected, Completed }

    public sealed class MeshinaJob
    {
        public string SN { get; set; }
        public DateTime ScanTimeUtc { get; set; }
        public List<string> BaselineFiles { get; set; }
        public MeshinaStage Stage { get; set; }
        public string MdbPath { get; set; }
        public MeshinaMeasurement Measurement { get; set; }
        public FeedingCheckModel FeedingCheckRequest { get; set; }
        public SNCheckoutModel CheckOutRequest { get; set; }
        public string Message { get; set; }
    }
}
