using MES.Comm;
using MES.MesModel.Request;
using MES.MesModel.Response;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Net.Http;
using System.Text;

namespace MES.Manager
{
    public class MesManager
    {
        private IHttpClientFactory httpClientFactory = null;

        public MesManager()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddHttpClient();
            var provice = services.BuildServiceProvider();

            httpClientFactory = provice.GetRequiredService<IHttpClientFactory>();
        }




        #region 加热管控


        //  carrierID 参数，因为 MES 接口需要知道是哪个托盘加热结束
        public async Task<(bool, string, string)> HeatingFinished(int number, string SN)
        {
            (bool, string,string) result = (false, "" , "");
            DateTime dtStart = DateTime.Now;
            string responseString = "";
            string msg = "";

            // 1. 构建匿名对象（请求参数）
            var heatingFinish = new
            {
                EventID = "HeatingFinished",
                Line = SetHelper.MesSetting.ListGroup[number].Line,
                MachineID = SetHelper.MesSetting.ListGroup[number].MachineID,
                StationID = SetHelper.MesSetting.ListGroup[number].StationID,
                FixSN = "", // 如果有产品SN，这里也要传
                Token = SetHelper.MesSetting.ListGroup[number].Token,
                OPID = SetHelper.Opid[number].Id,
                CarrierID = SN, // 使用传入的托盘号
                SendTime = dtStart.ToString("yyyy/MM/dd HH:mm:ss"), // 建议格式化时间
            };

            try
            {
                using HttpClient client = httpClientFactory.CreateClient();
                string jsonString = heatingFinish.ToJson(); // 转换为JSON字符串

                // 记录发送日志
                SetHelper.ListOEEMessage.ShowInfoQueue($"[发送] {jsonString}");

                StringContent stringContent = new(jsonString, Encoding.UTF8, "application/json");

                //  调用接口
                HttpResponseMessage resultData = await client.PollyPostJsonAsync(
                    SetHelper.ApiSetting.ListGroup[number].BaseUrl + SetHelper.ApiSetting.ListGroup[number].CarrierCheck,
                    jsonString);

                if (resultData.IsSuccessStatusCode)
                {
                    responseString = await resultData.Content.ReadAsStringAsync().ConfigureAwait(false);
                    SetHelper.ListOEEMessage.ShowInfoQueue($"[接收] {responseString}");

                    // 直接解析 JSON
                    // 使用 Newtonsoft.Json 
                    var jo = Newtonsoft.Json.Linq.JObject.Parse(responseString);
                    string apiResult = jo["Result"]?.ToString();
                    string apiMsg = jo["Msg"]?.ToString();

                    if (apiResult == "PASS")
                    {
                        result = (true, apiResult, apiMsg);
                    }
                    else
                    {
                        result = (false, apiResult, apiMsg);
                    }
                }
                else
                {
                    result = (false, "",$"网络请求失败: {resultData.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                msg = $"程序异常: {ex.Message}";
                SetHelper.ListPLCMessage.ShowInfoQueue(ex.ToString(), false, "ex");
                result = (false, "", msg);
            }

            return result;
        }

        public async Task<(bool, string,string)> HeatingCheck(int number, string carrierID, string sn)
        {
            (bool, string, string) result = (false, "","");
            DateTime dtStart = DateTime.Now;
            string responseString = "";

            // 1. 构建请求参数（包含嵌套数组）
            var heatingCheckObj = new
            {
                EventID = "HeatingCheck",
                Token = SetHelper.MesSetting.ListGroup[number].Token,
                Mold = "",
                FixSN = "",
                // 即使只有一个SN，也要放进匿名对象的数组里
                SNInfo = new[]
                { new { SN = sn, Result = "PASS" }},
                CarrierID = carrierID,
                Qty = "",
                Line = SetHelper.MesSetting.ListGroup[number].Line,
                StationID = SetHelper.MesSetting.ListGroup[number].StationID,
                MachineID = SetHelper.MesSetting.ListGroup[number].MachineID,
                OPID = SetHelper.Opid[number].Id,
                SendTime = dtStart.ToString("yyyy/MM/dd HH:mm:ss")
            };

            try
            {
                using HttpClient client = httpClientFactory.CreateClient();
                string jsonString = heatingCheckObj.ToJson(); // 转换为JSON字符串
                SetHelper.ListOEEMessage.ShowInfoQueue($"[发送核对] {jsonString}");

                StringContent stringContent = new(jsonString, Encoding.UTF8, "application/json");

                // 2. 调用接口 (注意地址是 SN_Checkout)
                HttpResponseMessage resultData = await client.PollyPostJsonAsync(
                    SetHelper.ApiSetting.ListGroup[number].BaseUrl + SetHelper.ApiSetting.ListGroup[number].CheckOutApi,
                    jsonString);

                if (resultData.IsSuccessStatusCode)
                {
                    responseString = await resultData.Content.ReadAsStringAsync().ConfigureAwait(false);
                    SetHelper.ListOEEMessage.ShowInfoQueue($"[接收核对] {responseString}");

                    // 动态解析响应结果
                    var jo = Newtonsoft.Json.Linq.JObject.Parse(responseString);

                    // 获取全局结果
                    string apiResult = jo["Result"]?.ToString();
                    string apiMsg = jo["Msg"]?.ToString();

                    // 如果全局失败，直接返回
                    if (apiResult == "PASS")
                    {
                        result = (true, apiResult, apiMsg);
                    }
                    else
                    {
                        result = (false, apiResult, apiMsg );
                    }
                }
                else
                {
                    result = (false, "",$"{resultData.StatusCode}");
                }
            }
            catch (Exception ex)
            { 
            
                result = (false, "", ex.Message);
                SetHelper.ListPLCMessage.ShowInfoQueue(ex.ToString(), false, "ex");
            }

            return result;
        }

        #endregion 



        #region DataCollection
        public async Task<(bool, string)> DataCollection(DataCollectionModel dataCollection, int number)
        {
            (bool, string) result = (false, "");
            DateTime dtStart = DateTime.Now;
            string @string = "";
            string msg = "";
            DataCollectionResponse response = new DataCollectionResponse();
            try
            {
                using HttpClient client = httpClientFactory.CreateClient();

                string jsonString = dataCollection.ToJson();
                SetHelper.ListOEEMessage.ShowInfoQueue(jsonString);

                StringContent stringContent = new(jsonString, Encoding.UTF8, "application/json");

                HttpResponseMessage resultData = await client.PollyPostJsonAsync(SetHelper.ApiSetting.ListGroup[number].BaseUrl + SetHelper.ApiSetting.ListGroup[number].DataCollectionApi, jsonString);

                @string = await resultData.Content.ReadAsStringAsync().ConfigureAwait(false);
                SetHelper.ListOEEMessage.ShowInfoQueue(@string);

                response = typeof(string) == typeof(DataCollectionResponse) ? (DataCollectionResponse)(object)@string : @string.FromJson<DataCollectionResponse>();

                if (response != null)
                {
                    msg = response.MSG_ID.Obj2String();
                    if (response.Result.ToUpper() == "PASS")
                    {
                        result = (true, msg);
                    }
                }
            }
            catch (Exception ex)
            {
                msg = ex.ToString();
            }
            result.Item2 = msg;
            WriteMesLog(dataCollection.EventID, result.Item1, dtStart, dataCollection?.ToJson(), @string ?? msg);
            return result;
        }
        #endregion


        #region 载具绑定/解绑
        public async Task<(bool, string)> CarrierBind(CarrierBindModel carrierBind, int number)
        {
            (bool, string) result = (false, "");
            DateTime dtStart = DateTime.Now;
            string @string = "";
            string msg = "";
            CarrierBindResponse response = new CarrierBindResponse();
            try
            {
                using HttpClient client = httpClientFactory.CreateClient();

                string jsonString = carrierBind.ToJson();
                SetHelper.ListOEEMessage.ShowInfoQueue(jsonString);

                StringContent stringContent = new(jsonString, Encoding.UTF8, "application/json");

                HttpResponseMessage resultData = await client.PollyPostJsonAsync(SetHelper.ApiSetting.ListGroup[number].BaseUrl + SetHelper.ApiSetting.ListGroup[number].CarrierBind, jsonString);

                @string = await resultData.Content.ReadAsStringAsync().ConfigureAwait(false);

                response = typeof(string) == typeof(CarrierBindResponse) ? (CarrierBindResponse)(object)@string : @string.FromJson<CarrierBindResponse>();

                if (response != null)
                {
                    SetHelper.ListOEEMessage.ShowInfoQueue(@string);

                    msg = response.Msg.Obj2String();
                    if (response.Result.ToUpper() == "PASS")
                    {
                        result = (true, msg);
                    }
                }
            }
            catch (Exception ex)
            {
                msg = ex.ToString();
            }
            result.Item2 = msg;
            WriteMesLog(carrierBind.EventID, result.Item1, dtStart, carrierBind?.ToJson(), @string ?? msg);
            return result;
        }
        #endregion

        #region 切型
        /// <summary>
        /// (返回结果，产品型号，返回信息)
        /// </summary>
        /// <param name="changeProduct"></param>
        /// <param name="number"></param>
        /// <returns></returns>
        public async Task<(bool, string, string)> ChangeProductType(ChangeProductTypeModel changeProduct, int number)
        {
            (bool, string, string) result = (false, "", "");
            DateTime dtStart = DateTime.Now;
            string @string = "";
            string msg = "";
            ChangeProductTypeResponse response = new ChangeProductTypeResponse();
            try
            {
                using HttpClient client = httpClientFactory.CreateClient();

                string jsonString = changeProduct.ToJson();
                SetHelper.ListOEEMessage.ShowInfoQueue(jsonString);

                StringContent stringContent = new(jsonString, Encoding.UTF8, "application/json");
                if (SetHelper.ApiSetting.ListGroup[number].ChangeProductTypeApi == "") return result;
                HttpResponseMessage resultData = await client.PollyPostJsonAsync(SetHelper.ApiSetting.ListGroup[number].BaseUrl + SetHelper.ApiSetting.ListGroup[number].ChangeProductTypeApi, jsonString);

                @string = await resultData.Content.ReadAsStringAsync().ConfigureAwait(false);

                response = typeof(string) == typeof(ChangeProductTypeResponse) ? (ChangeProductTypeResponse)(object)@string : @string.FromJson<ChangeProductTypeResponse>();

                if (response != null)
                {
                    SetHelper.ListOEEMessage.ShowInfoQueue(@string);

                    msg = response.MSG.Obj2String();
                    if (response.Result?.ToUpper() == "PASS")
                    {
                        result = (true, response.WO, msg);
                    }
                }
            }
            catch (Exception ex)
            {
                msg = ex.ToString();
            }
            result.Item3 = msg;
            WriteMesLog(changeProduct.EventID, result.Item1, dtStart, changeProduct?.ToJson(), @string ?? msg);
            return result;
        }
        #endregion

        #region 图片上传
        public async Task<(bool, string)> FileUpLoad(FileUploadModel fileUpload, int number)
        {
            (bool, string) result = (false, "");
            DateTime dtStart = DateTime.Now;
            string @string = "";
            string msg = "";
            FileUploadResponse response = new FileUploadResponse();
            try
            {
                using HttpClient client = httpClientFactory.CreateClient();

                string jsonString = fileUpload.ToJson();

                StringContent stringContent = new(jsonString, Encoding.UTF8, "application/json");

                HttpResponseMessage resultData = await client.PollyPostJsonAsync(SetHelper.ApiSetting.ListGroup[number].BaseUrl + SetHelper.ApiSetting.ListGroup[number].FileUploadApi, jsonString);

                @string = await resultData.Content.ReadAsStringAsync().ConfigureAwait(false);

                response = typeof(string) == typeof(FileUploadResponse) ? (FileUploadResponse)(object)@string : @string.FromJson<FileUploadResponse>();

                if (response != null)
                {
                    msg = response.ErrorMessage.Obj2String();
                    if (response.StatusCode.ToUpper() == "200")
                    {
                        result = (true, msg);
                    }
                }
            }
            catch (Exception ex)
            {
                msg = ex.ToString();
            }
            result.Item2 = msg;
            WriteMesLog(fileUpload.EventID, result.Item1, dtStart, fileUpload?.ToJson(), @string ?? msg);
            return result;
        }
        #endregion

        #region 进站
        /// <summary>
        /// 进站
        /// </summary>
        /// <param name="checkINModel"></param>
        /// <returns></returns>
        public async Task<(bool, string, string, string, string)> CheckIn(SNCheckINModel checkINModel, int number)
        {
            (bool, string, string, string, string) result = (false, "", "", "", "");
            DateTime dtStart = DateTime.Now;
            SNCheckINResponse response = new SNCheckINResponse();
            string @string = "";
            string msg = "";
            string sn = "";
            try
            {
                using HttpClient client = httpClientFactory.CreateClient();

                string jsonString = checkINModel.ToJson();
                SetHelper.ListOEEMessage.ShowInfoQueue(jsonString);

                StringContent stringContent = new(jsonString, Encoding.UTF8, "application/json");

                HttpResponseMessage resultData = await client.PollyPostJsonAsync(SetHelper.ApiSetting.ListGroup[number].BaseUrl + SetHelper.ApiSetting.ListGroup[number].CheckINApi, jsonString);

                @string = await resultData.Content.ReadAsStringAsync().ConfigureAwait(false);

                response = typeof(string) == typeof(SNCheckINResponse) ? (SNCheckINResponse)(object)@string : @string.FromJson<SNCheckINResponse>();

                if (response != null)
                {
                    SetHelper.ListOEEMessage.ShowInfoQueue(@string);

                    sn = response?.SN;
                    msg = response.MSG.Obj2String();
                    if (response.Result.ToUpper() == "PASS")
                    {
                        result = (true, msg, sn, @string, "");
                    }
                    if (response.Result.ToUpper() == "REDO")
                    {
                        result = (true, msg, sn, @string, "返修件");
                    }
                    if (response.Result.ToUpper() == "REPAIR")
                    {
                        result = (true, msg, sn, @string, "返修特殊工位");
                    }
                    //增加上料口转盘逻辑
                    if (checkINModel.StationID.Contains("1Turntable")&& msg.Contains("PASS->Status"))
                    {
                        result = (true, msg, sn, @string, "返修件");
                    }
                }
            }
            catch (Exception ex)
            {
                msg = ex.ToString();
            }
            result.Item2 = msg;
            //NG
            result.Item3 = response?.SN;
            result.Item4 = @string;
            WriteMesLog(checkINModel.EventID, result.Item1, dtStart, checkINModel?.ToJson(), @string ?? msg);
            return result;
        }
        #endregion

        #region 进站
        /// <summary>
        /// 进站
        /// </summary>
        /// <param name="checkINModel"></param>
        /// <returns></returns>
        public async Task<(bool, string)> QuerySN(QuerySNModel querySNModel, int number)
        {
            (bool, string) result = (false, "");
            DateTime dtStart = DateTime.Now;
            QuerySNResponse response = new QuerySNResponse();
            string @string = "";
            string msg = "";
            try
            {
                using HttpClient client = httpClientFactory.CreateClient();

                string jsonString = querySNModel.ToJson();
                SetHelper.ListOEEMessage.ShowInfoQueue(jsonString);

                StringContent stringContent = new(jsonString, Encoding.UTF8, "application/json");

                HttpResponseMessage resultData = await client.PollyPostJsonAsync(SetHelper.ApiSetting.ListGroup[number].BaseUrl + SetHelper.ApiSetting.ListGroup[number].QuerySNApi, jsonString);

                @string = await resultData.Content.ReadAsStringAsync().ConfigureAwait(false);

                response = typeof(string) == typeof(QuerySNResponse) ? (QuerySNResponse)(object)@string : @string.FromJson<QuerySNResponse>();

                if (response != null)
                {
                    SetHelper.ListOEEMessage.ShowInfoQueue(@string);
                    msg = response.MSG.Obj2String();
                    if (response.Result.ToUpper() == "PASS")
                    {
                        result = (true, msg);
                    }
                }
            }
            catch (Exception ex)
            {
                msg = ex.ToString();
            }
            result.Item2 = msg;
            WriteMesLog(querySNModel.EventID, result.Item1, dtStart, querySNModel?.ToJson(), @string ?? msg);
            return result;
        }
        #endregion

        #region 材料校验
        public async Task<(bool, string)> LinkComp(LinkCompModel linkCompModel, int number)
        {
            (bool, string) result = (false, "");
            DateTime dtStart = DateTime.Now;
            string msg = "";
            string @string = "";
            LinkCompResponse response = new LinkCompResponse();

            try
            {
                using HttpClient client = httpClientFactory.CreateClient();

                string jsonString = linkCompModel.ToJson();
                SetHelper.ListOEEMessage.ShowInfoQueue(jsonString);

                StringContent stringContent = new(jsonString, Encoding.UTF8, "application/json");

                HttpResponseMessage resultData = await client.PollyPostJsonAsync(SetHelper.ApiSetting.ListGroup[number].BaseUrl + SetHelper.ApiSetting.ListGroup[number].LinkCompApi, jsonString);

                @string = await resultData.Content.ReadAsStringAsync().ConfigureAwait(false);

                response = typeof(string) == typeof(LinkCompResponse) ? (LinkCompResponse)(object)@string : @string.FromJson<LinkCompResponse>();

                if (response != null)
                {
                    SetHelper.ListOEEMessage.ShowInfoQueue(@string);

                    msg = response.MSG.Obj2String();
                    if (response.Result.ToUpper() == "PASS")
                    {
                        result = (true, msg);
                    }
                }
            }
            catch (Exception ex)
            {
                msg = ex.ToString();
            }
            result.Item2 = msg;
            WriteMesLog(linkCompModel.EventID, result.Item1, dtStart, linkCompModel?.ToJson(), @string ?? msg);
            return result;
        }
        #endregion

        #region 出站
        public async Task<(bool, string, SN_InfoItem[], string)> CheckOut(SNCheckoutModel checkoutModel, int number)
        {
            (bool, string, SN_InfoItem[], string) result = (false, "", null, "");
            DateTime dtStart = DateTime.Now;
            string msg = "";
            string @string = "";
            SN_InfoItem[] sN_Infos;
            SNCheckOutResponse response = new SNCheckOutResponse();

            try
            {
                using HttpClient client = httpClientFactory.CreateClient();

                string jsonString = checkoutModel.ToJson();
                SetHelper.ListOEEMessage.ShowInfoQueue(jsonString);



                StringContent stringContent = new(jsonString, Encoding.UTF8, "application/json");

                HttpResponseMessage resultData = await client.PollyPostJsonAsync(SetHelper.ApiSetting.ListGroup[number].BaseUrl + SetHelper.ApiSetting.ListGroup[number].CheckOutApi, jsonString);

                @string = await resultData.Content.ReadAsStringAsync().ConfigureAwait(false);

                //@string = "{ \"EventID\": \"SN_CHECKOUT\", \"Result\": \"FAIL\", \"Msg\": \"M3*10机台上料数量不够扣料,请检查;\", \"Need_Work\": \"STOP\", \"SN_Info\": [ { \"SN\": \"123123\", \"SNResult\": \"FAIL\", \"Msg_ID\": \"M3*10机台上料数量不够扣料,请检查;\" } ] }\r\n";//测试用
                response = typeof(string) == typeof(SNCheckOutResponse) ? (SNCheckOutResponse)(object)@string : @string.FromJson<SNCheckOutResponse>();

                if (response != null)
                {
                    SetHelper.ListOEEMessage.ShowInfoQueue(@string);

                    msg = response.Msg.Obj2String();
                    sN_Infos = response.SN_Info;
                    if (response.Result.ToUpper() == "PASS")
                    {
                        result = (true, msg, sN_Infos, @string);
                    }
                    else
                    {
                        result = (false, msg, sN_Infos, @string);
                    }
                }
            }
            catch (Exception ex)
            {
                msg = ex.ToString();
            }
            result.Item2 = msg;
            result.Item4 = @string;

            WriteMesLog(checkoutModel.EventID, result.Item1, dtStart, checkoutModel?.ToJson(), @string ?? msg);
            return result;
        }
        #endregion

        #region OEE参数上传新版
        public async Task<(bool, string, string)> OEEDataCollect(OEEModel oeeModel, int number)
        {
            (bool, string, string) result = (false, "", "");
            DateTime dtStart = DateTime.Now;
            string msg = "";
            string @string = "";
            SN_InfoItem[] sN_Infos;
            OEEResponse response = new OEEResponse();

            try
            {
                using HttpClient client = httpClientFactory.CreateClient();

                string jsonString = oeeModel.ToJson();
                SetHelper.ListOtherMessage.ShowInfoQueue(jsonString);



                StringContent stringContent = new(jsonString, Encoding.UTF8, "application/json");

                HttpResponseMessage resultData = await client.PollyPostJsonAsync(SetHelper.ApiSetting.ListGroup[number].BaseUrl + SetHelper.ApiSetting.ListGroup[number].OEEApi, jsonString);

                @string = await resultData.Content.ReadAsStringAsync().ConfigureAwait(false);

                //@string = "{ \"EventID\": \"SN_CHECKOUT\", \"Result\": \"FAIL\", \"Msg\": \"M3*10机台上料数量不够扣料,请检查;\", \"Need_Work\": \"STOP\", \"SN_Info\": [ { \"SN\": \"123123\", \"SNResult\": \"FAIL\", \"Msg_ID\": \"M3*10机台上料数量不够扣料,请检查;\" } ] }\r\n";//测试用
                response = typeof(string) == typeof(OEEResponse) ? (OEEResponse)(object)@string : @string.FromJson<OEEResponse>();

                if (response != null)
                {
                    SetHelper.ListOtherMessage.ShowInfoQueue(@string);

                    msg = response.MSG.Obj2String();
                    if (response.Result.ToUpper() == "PASS")
                    {
                        result = (true, msg, @string);
                    }
                    else
                    {
                        result = (false, msg, @string);
                    }
                }
            }
            catch (Exception ex)
            {
                msg = ex.ToString();
            }
            result.Item2 = msg;
            result.Item3 = @string;

            WriteMesLog(oeeModel.EventID, result.Item1, dtStart, oeeModel?.ToJson(), @string ?? msg);
            return result;
        }
        #endregion

        #region 设定参数校验
        public async Task<(bool, string, string)> GetParaCheck(GetParaModel getParaModel, int number)
        {
            (bool, string, string) result = (false, "", "");
            DateTime dtStart = DateTime.Now;
            string msg = "";
            string @string = "";
            GetParaResponse response = new GetParaResponse();

            try
            {
                using HttpClient client = httpClientFactory.CreateClient();

                string jsonString = getParaModel.ToJson();
                SetHelper.ListOEEMessage.ShowInfoQueue(jsonString);

                StringContent stringContent = new(jsonString, Encoding.UTF8, "application/json");

                HttpResponseMessage resultData = await client.PollyPostJsonAsync(SetHelper.ApiSetting.ListGroup[number].BaseUrl + SetHelper.ApiSetting.ListGroup[number].GetPara, jsonString);

                @string = await resultData.Content.ReadAsStringAsync().ConfigureAwait(false);

                response = typeof(string) == typeof(GetParaResponse) ? (GetParaResponse)(object)@string : @string.FromJson<GetParaResponse>();

                if (response != null)
                {
                    SetHelper.ListOEEMessage.ShowInfoQueue(@string);

                    msg = response.MSG_ID.Obj2String();
                    if (response.Result.ToUpper() == "PASS")
                    {
                        result = (true, msg, @string);
                    }
                }
            }
            catch (Exception ex)
            {
                msg = ex.ToString();
            }
            result.Item2 = msg;
            result.Item3 = @string;

            WriteMesLog(getParaModel.EventID, result.Item1, dtStart, getParaModel?.ToJson(), @string ?? msg);
            return result;
        }
        #endregion

        #region 上料校验
        public async Task<(bool, string, string)> FeedingCheck(FeedingCheckModel feedingCheckModel, int number)
        {
            (bool, string, string) result = (false, "", "");
            DateTime dtStart = DateTime.Now;
            string msg = "";
            string @string = "";
            FeedingCheckResponse response = new FeedingCheckResponse();

            try
            {
                using HttpClient client = httpClientFactory.CreateClient();

                string jsonString = feedingCheckModel.ToJson();
                SetHelper.ListOEEMessage.ShowInfoQueue(jsonString);

                StringContent stringContent = new(jsonString, Encoding.UTF8, "application/json");

                HttpResponseMessage resultData = await client.PollyPostJsonAsync(SetHelper.ApiSetting.ListGroup[number].BaseUrl + SetHelper.ApiSetting.ListGroup[number].FeedingCheck, jsonString);

                @string = await resultData.Content.ReadAsStringAsync().ConfigureAwait(false);

                response = typeof(string) == typeof(FeedingCheckResponse) ? (FeedingCheckResponse)(object)@string : @string.FromJson<FeedingCheckResponse>();

                if (response != null)
                {
                    SetHelper.ListOEEMessage.ShowInfoQueue(@string);

                    msg = response.MSG.Obj2String();
                    if (response.Result.ToUpper() == "PASS")
                    {
                        result = (true, msg, @string);
                    }
                }
            }
            catch (Exception ex)
            {
                msg = ex.ToString();
            }
            result.Item2 = msg;
            result.Item3 = @string;

            WriteMesLog(feedingCheckModel.EventID, result.Item1, dtStart, feedingCheckModel?.ToJson(), @string ?? msg);
            return result;
        }
        #endregion
        #region 上料校验2025/3/28新版
        public async Task<(bool, string)> CompSNCheckout(CompSNCheckoutModel compSNCheckoutModel, int number)
        {
            (bool, string) result = (false, "");
            DateTime dtStart = DateTime.Now;
            string msg = "";
            string @string = "";
            CompSNCheckoutResponse response = new CompSNCheckoutResponse();

            try
            {
                using HttpClient client = httpClientFactory.CreateClient();

                string jsonString = compSNCheckoutModel.ToJson();
                SetHelper.ListOEEMessage.ShowInfoQueue(jsonString);



                StringContent stringContent = new(jsonString, Encoding.UTF8, "application/json");

                HttpResponseMessage resultData = await client.PollyPostJsonAsync(SetHelper.ApiSetting.ListGroup[number].BaseUrl + SetHelper.ApiSetting.ListGroup[number].CompSNCheckout, jsonString);

                @string = await resultData.Content.ReadAsStringAsync().ConfigureAwait(false);

                response = typeof(string) == typeof(CompSNCheckoutResponse) ? (CompSNCheckoutResponse)(object)@string : @string.FromJson<CompSNCheckoutResponse>();

                if (response != null)
                {
                    SetHelper.ListOEEMessage.ShowInfoQueue(@string);
                    msg = response.MSG == null ? @string : response.MSG.Obj2String();
                    if (response.Result.ToUpper() == "PASS")
                    {
                        result = (true, msg);
                    }
                }
            }
            catch (Exception ex)
            {
                msg = @string + "\r\n\r\n" + ex.ToString();
            }
            result.Item2 = msg;

            WriteMesLog(compSNCheckoutModel.EventID, result.Item1, dtStart, compSNCheckoutModel?.ToJson(), @string ?? msg);
            return result;
        }
        #endregion

        #region 检查物料是否可用
        public async Task<(int, string)> CompSNChange(CompSNChangeModel compSNChangeModel, int number)
        {
            (int, string) result = (0, "");
            DateTime dtStart = DateTime.Now;
            string msg = "";
            string @string = "";
            CompSNChangeResponse response = new CompSNChangeResponse();

            try
            {
                using HttpClient client = httpClientFactory.CreateClient();

                string jsonString = compSNChangeModel.ToJson();
                SetHelper.ListOEEMessage.ShowInfoQueue(jsonString);



                StringContent stringContent = new(jsonString, Encoding.UTF8, "application/json");

                HttpResponseMessage resultData = await client.PollyPostJsonAsync(SetHelper.ApiSetting.ListGroup[number].BaseUrl + SetHelper.ApiSetting.ListGroup[number].CompSNChange, jsonString);

                @string = await resultData.Content.ReadAsStringAsync().ConfigureAwait(false);

                response = typeof(string) == typeof(CompSNChangeResponse) ? (CompSNChangeResponse)(object)@string : @string.FromJson<CompSNChangeResponse>();

                if (response != null)
                {
                    SetHelper.ListOEEMessage.ShowInfoQueue(@string);
                    msg = response.Msg == null ? @string : response.Msg.Obj2String();
                    if (response.Result.ToUpper() == "PASS")
                    {
                        result = (1, msg);
                    }
                    else
                    {
                        result = (2, msg);
                    }
                }
            }
            catch (Exception ex)
            {
                msg = @string + "\r\n\r\n" + ex.ToString();
            }
            result.Item2 = msg;

            WriteMesLog(compSNChangeModel.EventID, result.Item1 == 1 ? true : false, dtStart, compSNChangeModel?.ToJson(), @string ?? msg);
            return result;
        }
        #endregion

        #region 下料校验2025/3/28新版
        public async Task<(bool, string)> CompSNOffline(CompSNOfflineModel compSNOfflineModel, int number)
        {
            (bool, string) result = (false, "");
            DateTime dtStart = DateTime.Now;
            string msg = "";
            string @string = "";
            CompSNOfflineResponse response = new CompSNOfflineResponse();

            try
            {
                using HttpClient client = httpClientFactory.CreateClient();

                string jsonString = compSNOfflineModel.ToJson();
                SetHelper.ListOEEMessage.ShowInfoQueue(jsonString);



                StringContent stringContent = new(jsonString, Encoding.UTF8, "application/json");

                HttpResponseMessage resultData = await client.PollyPostJsonAsync(SetHelper.ApiSetting.ListGroup[number].BaseUrl + SetHelper.ApiSetting.ListGroup[number].CompSNOffline, jsonString);

                @string = await resultData.Content.ReadAsStringAsync().ConfigureAwait(false);

                response = typeof(string) == typeof(CompSNOfflineResponse) ? (CompSNOfflineResponse)(object)@string : @string.FromJson<CompSNOfflineResponse>();

                if (response != null)
                {
                    SetHelper.ListOEEMessage.ShowInfoQueue(@string);

                    msg = response.MSG.Obj2String();
                    if (response.Result.ToUpper() == "PASS")
                    {
                        result = (true, msg);
                    }
                }
            }
            catch (Exception ex)
            {
                msg = @string + "\r\n\r\n" + ex.ToString();
            }
            result.Item2 = msg;

            WriteMesLog(compSNOfflineModel.EventID, result.Item1, dtStart, compSNOfflineModel?.ToJson(), @string ?? msg);
            return result;
        }
        #endregion
        #region 载具校验
        public async Task<(bool, string, string)> CarrierCheck(CarrierCheckModel carrierCheckModel, int number)
        {
            (bool, string, string) result = (false, "", "");
            DateTime dtStart = DateTime.Now;
            string msg = "";
            string @string = "";
            CarrierCheckResponse response = new CarrierCheckResponse();

            try
            {
                using HttpClient client = httpClientFactory.CreateClient();

                string jsonString = carrierCheckModel.ToJson();
                SetHelper.ListOEEMessage.ShowInfoQueue(jsonString);

                StringContent stringContent = new(jsonString, Encoding.UTF8, "application/json");

                HttpResponseMessage resultData = await client.PollyPostJsonAsync(SetHelper.ApiSetting.ListGroup[number].BaseUrl + SetHelper.ApiSetting.ListGroup[number].CarrierCheck, jsonString);

                @string = await resultData.Content.ReadAsStringAsync().ConfigureAwait(false);

                response = typeof(string) == typeof(CarrierCheckResponse) ? (CarrierCheckResponse)(object)@string : @string.FromJson<CarrierCheckResponse>();

                if (response != null)
                {
                    SetHelper.ListOEEMessage.ShowInfoQueue(@string);

                    msg = response.MSG.Obj2String();
                    if (response.Result.ToUpper() == "PASS")
                    {
                        if (response.DC_Info != null && response.DC_Info.Length > 0)
                        {
                            string weight = response.DC_Info.FirstOrDefault(x => x.Item.ToUpper().Contains("OLD"))?.Value;
                            result = (true, msg, weight);
                        }
                        else
                        {
                            result = (true, msg, @string);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                msg = ex.ToString();
            }
            result.Item2 = msg;
            result.Item3 = @string;

            WriteMesLog(carrierCheckModel.EventID, result.Item1, dtStart, carrierCheckModel?.ToJson(), @string ?? msg);
            return result;
        }
        #endregion

        #region 胶水上下料通用接口
        public async Task<(bool, string)> GlueOnOrOffLine(GlueCheckOutModel GlueModel, int iNumber)
        {
            (bool, string) result = (false, "");
            DateTime dtStart = DateTime.Now;
            string msg = "";
            string @string = "";
            GlueResponse response = new GlueResponse();

            try
            {
                using HttpClient client = httpClientFactory.CreateClient();

                string jsonString = GlueModel.ToJson();
                SetHelper.ListOEEMessage.ShowInfoQueue(jsonString);

                StringContent stringContent = new(jsonString, Encoding.UTF8, "application/json");

                HttpResponseMessage resultData = await client.PollyPostJsonAsync(SetHelper.ApiSetting.ListGroup[iNumber].BaseUrl + SetHelper.ApiSetting.ListGroup[iNumber].GlueCheckOut, jsonString);

                @string = await resultData.Content.ReadAsStringAsync().ConfigureAwait(false);
                // @string = "{\"EventID\":\"Glue_OffLine\",\"RESULT\":\"PASS\",\"MSG\":\"\"}";
                SetHelper.ListOEEMessage.ShowInfoQueue(@string.Obj2String());

                response = typeof(string) == typeof(GlueResponse) ? (GlueResponse)(object)@string : @string.FromJson<GlueResponse>();

                if (response != null)
                {

                    msg = response.MSG.Obj2String();
                    if (response.Result.ToUpper() == "PASS")
                    {
                        result = (true, msg);
                    }
                }
            }
            catch (Exception ex)
            {
                msg = $"MES返回的信息:{@string} \r\n\r\n{ex}";
            }
            result.Item2 = msg;

            WriteMesLog(GlueModel.EventID, result.Item1, dtStart, GlueModel?.ToJson(), @string ?? msg);
            return result;
        }
        #endregion

        #region 下料（已弃用）

        //public async Task<(bool, string)> GlueOffLine(GlueOffLineModel GlueModel)
        //{
        //    (bool, string) result = (false, "");
        //    DateTime dtStart = DateTime.Now;
        //    string msg = "";
        //    string @string = "";
        //    GlueResponse response = new GlueResponse();

        //    try
        //    {
        //        using HttpClient client = httpClientFactory.CreateClient();

        //        string jsonString = GlueModel.ToJson();
        //        SetHelper.ListOEEMessage.ShowInfoQueue(jsonString);


        //        StringContent stringContent = new(jsonString, Encoding.UTF8, "application/json");

        //        HttpResponseMessage resultData = await client.PollyPostJsonAsync(SetHelper.ApiSetting.ListGroup[0].BaseUrl + SetHelper.ApiSetting.ListGroup[0].GlueOffLine, stringContent);

        //        @string = await resultData.Content.ReadAsStringAsync().ConfigureAwait(false);

        //        // @string = "{\"EventID\":\"Glue_OffLine\",\"RESULT\":\"pass\",\"MSG\":\"\"}";
        //        SetHelper.ListOEEMessage.ShowInfoQueue(@string.Obj2String());


        //        response = typeof(string) == typeof(GlueResponse) ? (GlueResponse)(object)@string : @string.FromJson<GlueResponse>();
        //        if (response != null)
        //        {
        //            msg = response.MSG.Obj2String();
        //            if (response.Result.ToUpper() == "PASS")
        //            {
        //                result = (true, msg);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        msg = ex.ToString();
        //    }
        //    result.Item2 = msg;

        //    WriteMesLog(GlueModel.EventID, result.Item1, dtStart, GlueModel?.ToJson(), @string ?? msg);
        //    return result;
        //}
        #endregion



        #region 报警上传
        public async Task<(bool, string)> EQAlarm(EQAlarmModel alarmModel, int number)
        {
            (bool, string) result = (false, "");
            DateTime dtStart = DateTime.Now;
            string msg = "";
            string @string = "";
            EQAlarmResponse response = new EQAlarmResponse();

            try
            {
                using HttpClient client = httpClientFactory.CreateClient();

                string jsonString = alarmModel.ToJson();
                SetHelper.ListOtherMessage.ShowInfoQueue(jsonString, true, "alarm");

                StringContent stringContent = new(jsonString, Encoding.UTF8, "application/json");

                HttpResponseMessage resultData = await client.PollyPostJsonAsync(SetHelper.ApiSetting.ListGroup[number].BaseUrl + SetHelper.ApiSetting.ListGroup[number].AlarmApi, jsonString);

                @string = await resultData.Content.ReadAsStringAsync().ConfigureAwait(false);

                response = typeof(string) == typeof(EQAlarmResponse) ? (EQAlarmResponse)(object)@string : @string.FromJson<EQAlarmResponse>();

                if (response != null)
                {
                    SetHelper.ListOtherMessage.ShowInfoQueue(@string, true, "alarm");

                    msg = response.Msg.Obj2String();
                    if (response.Result.ToUpper() == "PASS")
                    {
                        result = (true, msg);
                    }
                }
            }
            catch (Exception ex)
            {
                msg = ex.ToString();
            }
            result.Item2 = msg;
            WriteMesLog(alarmModel.EventID, result.Item1, dtStart, alarmModel?.ToJson(), @string ?? msg);
            return result;
        }
        #endregion

        #region 状态上传
        public async Task<(bool, string)> EQStatus(EQStatusModel statusModel, int number)
        {
            (bool, string) result = (false, "");
            DateTime dtStart = DateTime.Now;
            string msg = "";
            string @string = "";
            EQStatusResponse response = new EQStatusResponse();

            try
            {

                using HttpClient client = httpClientFactory.CreateClient();

                string jsonString = statusModel.ToJson();
                SetHelper.ListOtherMessage.ShowInfoQueue(jsonString, true, "status");

                StringContent stringContent = new(jsonString, Encoding.UTF8, "application/json");

                HttpResponseMessage resultData = await client.PollyPostJsonAsync(SetHelper.ApiSetting.ListGroup[number].BaseUrl + SetHelper.ApiSetting.ListGroup[number].StatusApi, jsonString);

                @string = await resultData.Content.ReadAsStringAsync().ConfigureAwait(false);

                response = typeof(string) == typeof(EQStatusResponse) ? (EQStatusResponse)(object)@string : @string.FromJson<EQStatusResponse>();

                if (response != null)
                {
                    SetHelper.ListOtherMessage.ShowInfoQueue(@string, true, "status");

                    msg = response.Msg.Obj2String();
                    if (response.Result.ToUpper() == "PASS")
                    {
                        result = (true, msg);
                    }
                }
            }
            catch (Exception ex)
            {
                msg = "设备状态上传失败" + " " + ex.Message + " " + ex.InnerException.Message;
            }
            result.Item2 = msg;
            WriteMesLog(statusModel.EventID, result.Item1, dtStart, statusModel?.ToJson(), @string ?? msg);
            return result;

        }
        #endregion

        #region 打印机信号发送
        public async Task<(bool, string, string)> CodeSoftPrint(CodeSoftPrintModel printModel, int number)
        {
            (bool, string, string) result = (false, "", "");
            DateTime dtStart = DateTime.Now;
            string msg = "";
            string @string = "";
            CodeSoftPrintResponse response = new CodeSoftPrintResponse();

            try
            {
                using HttpClient client = httpClientFactory.CreateClient();

                string jsonString = printModel.ToJson();
                SetHelper.ListOEEMessage.ShowInfoQueue(jsonString);

                StringContent stringContent = new(jsonString, Encoding.UTF8, "application/json");

                HttpResponseMessage resultData = await client.PollyPostJsonAsync(SetHelper.ApiSetting.ListGroup[number].BaseUrl + SetHelper.ApiSetting.ListGroup[number].CodeSoftPrint, jsonString);

                @string = await resultData.Content.ReadAsStringAsync().ConfigureAwait(false);

                response = typeof(string) == typeof(CodeSoftPrintResponse) ? (CodeSoftPrintResponse)(object)@string : @string.FromJson<CodeSoftPrintResponse>();

                if (response != null)
                {
                    SetHelper.ListOEEMessage.ShowInfoQueue(@string);

                    msg = response.ErrorMessage.Obj2String();
                    if (response.IsSuccess.ToUpper() == "TRUE")
                    {
                        result = (true, msg, @string);
                    }
                }
            }
            catch (Exception ex)
            {
                msg = ex.ToString();
            }
            result.Item2 = msg;
            result.Item3 = @string;

            WriteMesLog("CodeSoftPrint", result.Item1, dtStart, printModel?.ToJson(), @string ?? msg);
            return result;

        }
        #endregion

        #region OEE 已弃用
        //public async Task<(bool, string)> EQOEE(OEEModel oeeModel)
        //{
        //    (bool, string) result = (false, "");
        //    DateTime dtStart = DateTime.Now;
        //    string msg = "";
        //    string @string = "";
        //    OEEResponse response = new OEEResponse();

        //    try
        //    {
        //        using HttpClient client = httpClientFactory.CreateClient();

        //        string jsonString = oeeModel.ToJson();
        //        SetHelper.ListOEEMessage.ShowInfoQueue(jsonString);


        //        StringContent stringContent = new(jsonString, Encoding.UTF8, "application/json");

        //        HttpResponseMessage resultData = await client.PollyPostJsonAsync(SetHelper.ApiSetting.BaseUrl + SetHelper.ApiSetting.OEEApi, stringContent);

        //        @string = await resultData.Content.ReadAsStringAsync().ConfigureAwait(false);

        //        response = typeof(string) == typeof(OEEResponse) ? (OEEResponse)(object)@string : @string.FromJson<OEEResponse>();

        //        if (response != null)
        //        {
        //            SetHelper.ListOEEMessage.ShowInfoQueue(@string);

        //            msg = response.MSG_ID.Obj2String();
        //            if (response.Result.ToUpper() == "PASS")
        //            {
        //                result = (true, msg);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        msg = ex.ToString();
        //    }
        //    result.Item2 = msg;

        //    WriteMesLog(oeeModel.EventID, result.Item1, dtStart, oeeModel?.ToJson(), @string ?? msg);
        //    return result;
        //}
        #endregion

        #region 上料NG校验
        public async Task<(bool, string)> MaterialOn(CarrierCheckModel carrierCheckModel, int number)
        {
            (bool, string) result = (false, "");
            DateTime dtStart = DateTime.Now;
            string msg = "";
            string @string = "";
            CarrierCheckResponse response = new CarrierCheckResponse();

            try
            {
                using HttpClient client = httpClientFactory.CreateClient();

                string jsonString = carrierCheckModel.ToJson();
                SetHelper.ListOEEMessage.ShowInfoQueue(jsonString);

                StringContent stringContent = new(jsonString, Encoding.UTF8, "application/json");

                HttpResponseMessage resultData = await client.PollyPostJsonAsync(SetHelper.ApiSetting.ListGroup[number].BaseUrl + SetHelper.ApiSetting.ListGroup[number].CarrierCheck, jsonString);

                @string = await resultData.Content.ReadAsStringAsync().ConfigureAwait(false);
                //测试
                //@string = "{\"CarrierID\":\"123\",\"EventID\":\"Carrier_Check\",\"Result\":\"PASS\",\"Msg\":\"123\",\"DC_Info\":[]}";

                response = typeof(string) == typeof(CarrierCheckResponse) ? (CarrierCheckResponse)(object)@string : @string.FromJson<CarrierCheckResponse>();

                if (response != null)
                {
                    SetHelper.ListOEEMessage.ShowInfoQueue(@string);

                    msg = response.MSG.Obj2String();
                    if (response.Result.ToUpper() == "PASS")
                    {
                        result = (true, msg);
                    }
                }
            }
            catch (Exception ex)
            {
                msg = @string + "\r\n\r\n" + ex.ToString();
            }

            result.Item2 = msg;

            WriteMesLog(carrierCheckModel.EventID, result.Item1, dtStart, carrierCheckModel?.ToJson(), @string ?? msg);
            return result;
        }
        #endregion

        public static object obj = new object();

        public void WriteMesLog(string filename, bool result, DateTime dtStart, string jsonBody, string jsonResponse)
        {
            try
            {
                DateTime dtNow = DateTime.Now;
                string path = $"D:\\MESLOG\\{dtNow.Year}\\{dtNow.Month}\\{dtNow.Day}\\{filename}\\";

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string file = $"{path}{dtNow.ToString("yyyy-MM-dd")}.csv";
                StringBuilder stringBuilder = new StringBuilder();

                if (!File.Exists(file))
                {
                    stringBuilder.Append("调用时间\t");
                    stringBuilder.Append("接口名\t");
                    stringBuilder.Append("结果\t");
                    stringBuilder.Append("耗时(ms)\t");
                    stringBuilder.Append("数据上传\t");
                    stringBuilder.Append("数据获取\t");
                    stringBuilder.Append(Environment.NewLine);
                }
                double timespan = (DateTime.Now - dtNow).TotalMilliseconds;
                stringBuilder.Append($"{DateTime.Now}\t");
                stringBuilder.Append($"{filename}\t");
                stringBuilder.Append($"{result}\t");
                stringBuilder.Append($"{timespan}\t");
                stringBuilder.Append($"{jsonBody}\t");
                stringBuilder.Append($"{jsonResponse.Replace(" ", "\r").Replace(" ", "\n")}\t");

                lock (obj)
                {
                    using (FileStream stream = new FileStream(file, FileMode.Append))
                    {
                        using (StreamWriter str = new StreamWriter(stream, Encoding.Unicode))
                        {
                            str.WriteLine(stringBuilder.ToString());
                        }
                    }
                    SetHelper.dataManager.DeleteLocalMesLog();
                }
            }
            catch (Exception ex)
            {
                SetHelper.ListMesMessage.ShowInfoQueue(ex.ToString());
            }
        }
    }
}
