using System.Globalization;
using System.IO;
using System.Threading;
using MES.MesModel.Request;

namespace MES.SpecialStations.Meshina
{
    /// <summary>单件串行流程；所有网络发送前先持久化，未知响应不自动重发。</summary>
    public sealed class MeshinaStationService
    {
        private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
        private readonly MeshinaSettings settings;
        private readonly MdbPoller poller;
        private readonly IMdbReader reader;
        private readonly IMeshinaMesGateway gateway;
        private readonly MeshinaStateStore store;
        private readonly string contextKey;
        private readonly Action<string> log;
        private readonly MeshinaState state;
        private volatile bool stopped;
        private volatile bool faulted;
        private string status = "等待扫码进站";
        public string Status => status;
        public MeshinaJob Current => state.Current;
        public MeshinaJob LastFinished => state.LastFinished;
        public bool HasPending => faulted || gate.CurrentCount == 0 || state.Current != null;
        public bool CanRetry => !faulted && Current != null &&
            (Current.Stage == MeshinaStage.CheckInRejected || Current.Stage == MeshinaStage.CheckOutRejected);
        public bool NeedsConfirmation => !faulted && Current != null &&
            (Current.Stage == MeshinaStage.CheckInUncertain || Current.Stage == MeshinaStage.CheckOutUncertain);
        public bool CanCancel => !faulted && Current != null &&
            (Current.Stage == MeshinaStage.WaitingForMdb || Current.Stage == MeshinaStage.CheckInRejected
                || Current.Stage == MeshinaStage.CheckOutRejected);

        public MeshinaStationService(MeshinaSettings settings, MdbPoller poller, IMdbReader reader,
            IMeshinaMesGateway gateway, MeshinaStateStore store, string contextKey, Action<string> log)
        {
            this.settings = settings; this.poller = poller; this.reader = reader;
            this.gateway = gateway; this.store = store; this.contextKey = contextKey; this.log = log;
            state = store.Load();
            if (Current != null)
            {
                if (Current.ContextKey != contextKey) throw new InvalidDataException("未完成任务与当前工位/机型/接口/目录配置不一致，请恢复原配置");
                if (Current.Stage == MeshinaStage.CheckInSending) Current.Stage = MeshinaStage.CheckInUncertain;
                if (Current.Stage == MeshinaStage.CheckOutSending) Current.Stage = MeshinaStage.CheckOutUncertain;
                if (Current.Stage == MeshinaStage.ReadyForCheckOut && Current.Measurement == null)
                    throw new InvalidDataException("待出站任务缺少检测数据，禁止自动出站");
                Persist();
                Report($"恢复任务 SN:{Current.SN}，状态:{Current.Stage}。" +
                    (NeedsConfirmation ? "请技术人员核实MES是否已接收，再使用恢复按钮。" : "继续处理原任务。"));
            }
        }

        public async Task ScanAsync(string sn, Func<MeshinaJob> createRequest, CancellationToken token = default)
        {
            if (!await gate.WaitAsync(0, token)) { log("工位正在处理任务，本次扫码未接收"); return; }
            try
            {
                if (stopped || faulted) { log("啮合站未就绪，本次扫码未接收"); return; }
                if (Current != null) { log($"当前SN:{Current.SN}尚未完成，本次扫码{sn}未接收"); return; }
                if (string.IsNullOrWhiteSpace(sn)) { log("扫码为空，未进站"); return; }
                // 在请求MES前采集基线，进站接口响应期间生成的文件不会丢失。
                DateTime scanTime = DateTime.UtcNow;
                var baseline = poller.Snapshot();
                var job = createRequest();
                job.SN = sn.Trim(); job.ScanTimeUtc = scanTime; job.ContextKey = contextKey;
                job.BaselineFiles = baseline.Select(f => f.Path).ToList();
                job.CheckInRequest.SN = job.SN;
                job.Stage = MeshinaStage.CheckInSending;
                state.Current = job;
                poller.Reset();
                await SendCheckInAsync();
            }
            catch (Exception ex) { Report("扫码进站未完成：" + ex.Message); }
            finally { gate.Release(); }
        }

        public async Task RunAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && !stopped)
                {
                    await Task.Delay(settings.PollIntervalMilliseconds, token);
                    await TickAsync();
                }
            }
            catch (OperationCanceledException) { }
        }

        public void Stop() { stopped = true; }

        public bool TryStopForConfiguration()
        {
            if (!gate.Wait(0)) return false;
            try
            {
                if (faulted || Current != null) return false;
                stopped = true;
                return true;
            }
            finally { gate.Release(); }
        }

        public async Task TickAsync()
        {
            if (!await gate.WaitAsync(0)) return;
            try
            {
                if (stopped || faulted || Current == null) return;
                if (Current.Stage == MeshinaStage.WaitingForMdb)
                {
                    var files = poller.Candidates(Current, state.ProcessedFiles);
                    if (files.Count != 1)
                    {
                        poller.Reset();
                        Report(files.Count == 0 ? $"SN:{Current.SN} 已进站，等待本次检测MDB"
                            : $"SN:{Current.SN} 检测到{files.Count}个候选MDB，暂停自动匹配，请技术人员排除额外文件");
                        return;
                    }
                    var file = files[0];
                    if (!poller.IsStable(file, settings.StablePollCount)) return;
                    var measurement = reader.Read(file.Path);
                    var after = poller.Candidates(Current, state.ProcessedFiles);
                    if (after.Count != 1 || after[0].Path != file.Path || after[0].Stamp != file.Stamp)
                    { poller.Reset(); return; }
                    Current.MdbPath = file.Path;
                    Current.Measurement = measurement;
                    Current.CheckOutRequest.SendTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                    Current.CheckOutRequest.SNInfo = new[] { new SNInfo
                    {
                        SN = Current.SN,
                        Result = "PASS", // 本期不使用MDB.Result，沿用数值采集的PASS规则。
                        CompList = Array.Empty<CompList>(),
                        DC_Info = measurement.Values.Select(pair => new DC_Info
                        {
                            Item = settings.ItemNames.TryGetValue(pair.Key, out string name) && !string.IsNullOrWhiteSpace(name) ? name : pair.Key,
                            Value = pair.Value.ToString(CultureInfo.InvariantCulture), Result = "PASS"
                        }).ToArray()
                    } };
                    Current.Stage = MeshinaStage.ReadyForCheckOut;
                    Persist();
                    Report($"SN:{Current.SN} 已绑定MDB:{Path.GetFileName(file.Path)}，准备自动出站");
                }
                if (Current?.Stage == MeshinaStage.ReadyForCheckOut && !stopped) await SendCheckOutAsync();
            }
            catch (Exception ex) { Report($"SN:{Current?.SN} 暂停处理：{ex.Message}"); }
            finally { gate.Release(); }
        }

        public async Task RetryAsync()
        {
            await gate.WaitAsync();
            try
            {
                if (stopped || !CanRetry) return;
                if (Current.Stage == MeshinaStage.CheckInRejected) await SendCheckInAsync();
                else await SendCheckOutAsync();
            }
            catch (Exception ex) { Report("重试未完成：" + ex.Message); }
            finally { gate.Release(); }
        }

        // 必须由技术人员核实MES实际记录后调用，不能凭超时推断失败。
        public async Task ResolveUnknownAsync(bool accepted, string expectedJobId)
        {
            await gate.WaitAsync();
            try
            {
                if (stopped || !NeedsConfirmation || Current.Id != expectedJobId) return;
                bool checkIn = Current.Stage == MeshinaStage.CheckInUncertain;
                Report($"人工核实 SN:{Current.SN} {(checkIn ? "进站" : "出站")} MES{(accepted ? "已接收" : "未接收")}");
                if (checkIn) Current.Stage = accepted ? MeshinaStage.WaitingForMdb : MeshinaStage.CheckInRejected;
                else if (accepted) { Finish(MeshinaStage.Completed); return; }
                else Current.Stage = MeshinaStage.CheckOutRejected;
                Persist();
                Report(accepted ? $"SN:{Current.SN} 已确认进站，等待检测MDB" : "已确认MES未接收，可点击重试原任务");
            }
            catch (Exception ex) { Report("恢复失败：" + ex.Message); }
            finally { gate.Release(); }
        }

        public async Task CancelAsync(string expectedJobId)
        {
            await gate.WaitAsync();
            try
            {
                if (stopped || !CanCancel || Current.Id != expectedJobId) return;
                // 放弃前把当前目录文件全部纳入排除列表，禁止留给下一件。
                state.ProcessedFiles.AddRange(poller.Snapshot().Select(f => f.Path));
                Finish(MeshinaStage.Cancelled);
            }
            catch (Exception ex) { Report("结束任务失败：" + ex.Message); }
            finally { gate.Release(); }
        }

        private async Task SendCheckInAsync()
        {
            Current.Stage = MeshinaStage.CheckInSending;
            Persist();
            Report($"SN:{Current.SN} 正在请求MES进站，请等待进站OK后测量");
            MeshinaMesReply reply;
            try { reply = await gateway.CheckInAsync(Current.CheckInRequest); }
            catch (Exception ex) { reply = new MeshinaMesReply { Outcome = MeshinaMesOutcome.Unknown, Message = ex.Message }; }
            Current.Stage = reply.Outcome == MeshinaMesOutcome.Accepted ? MeshinaStage.WaitingForMdb
                : reply.Outcome == MeshinaMesOutcome.Rejected ? MeshinaStage.CheckInRejected : MeshinaStage.CheckInUncertain;
            Current.Message = reply.Message;
            Persist();
            Report($"SN:{Current.SN} " + (reply.Outcome == MeshinaMesOutcome.Accepted ? "进站OK，可以开始啮合，等待MDB。"
                : reply.Outcome == MeshinaMesOutcome.Rejected ? "MES进站失败，可重试原任务。" : "进站结果未知，请技术人员核实MES后恢复。") + reply.Message);
        }

        private async Task SendCheckOutAsync()
        {
            Current.Stage = MeshinaStage.CheckOutSending;
            Persist();
            Report($"SN:{Current.SN} 正在自动出站");
            MeshinaMesReply reply;
            try { reply = await gateway.CheckOutAsync(Current.CheckOutRequest); }
            catch (Exception ex) { reply = new MeshinaMesReply { Outcome = MeshinaMesOutcome.Unknown, Message = ex.Message }; }
            Current.Message = reply.Message;
            if (reply.Outcome == MeshinaMesOutcome.Accepted) { Finish(MeshinaStage.Completed); return; }
            Current.Stage = reply.Outcome == MeshinaMesOutcome.Rejected ? MeshinaStage.CheckOutRejected : MeshinaStage.CheckOutUncertain;
            Persist();
            Report($"SN:{Current.SN} " + (reply.Outcome == MeshinaMesOutcome.Rejected
                ? "MES出站失败，数据已保留，可重试原任务。" : "出站结果未知，请技术人员核实MES后恢复。") + reply.Message);
        }

        private void Finish(MeshinaStage stage)
        {
            string sn = Current.SN;
            if (!string.IsNullOrEmpty(Current.MdbPath)) state.ProcessedFiles.Add(Current.MdbPath);
            state.ProcessedFiles = state.ProcessedFiles.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            Current.Stage = stage;
            state.LastFinished = Current;
            // 每件保存独立记录，包含原始Result和固定的SN/MDB绑定。
            var archive = new MeshinaStateStore(Path.Combine(ArchiveDirectory, Current.Id + ".json"));
            try { archive.Save(new MeshinaState { LastFinished = Current }); }
            catch { faulted = true; throw; }
            state.Current = null;
            Persist();
            poller.Reset();
            Report($"SN:{sn} " + (stage == MeshinaStage.Completed ? "出站OK，可以扫描下一件" : "任务已人工结束，确认设备无延迟保存后再扫下一件"));
        }
        public string ArchiveDirectory { get; set; }

        private void Persist()
        {
            try { store.Save(state); }
            catch (Exception ex)
            {
                faulted = true;
                throw new IOException("任务保存失败，工位已锁定；请修复存储并重启后核实MES状态。" + ex.Message, ex);
            }
        }
        private void Report(string message)
        {
            if (status == message) return;
            status = message;
            log(message);
        }
    }
}
