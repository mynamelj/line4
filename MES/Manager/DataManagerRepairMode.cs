namespace MES.Manager
{
    public partial class DataManager
    {
        public void TriggerRepairMode(bool target, string Number)
        {
            if (!int.TryParse(Number, out int stationNumber)
                || stationNumber <= 0
                || stationNumber > SetHelper.StationNumber.numberGroups.Count
                || SetHelper.resultModel == null
                || stationNumber > SetHelper.resultModel.Length
                || SetHelper.resultModel[stationNumber - 1] == null)
            {
                return;
            }

            SetHelper.resultModel[stationNumber - 1].IsRepairMode = target;
            SetHelper.ListPLCMessage.ShowInfoQueue($"工位[{Number}] 返修模式: {(target ? "开" : "关")}");
        }
    }
}
