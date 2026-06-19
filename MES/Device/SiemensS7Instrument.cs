using MES.Comm;
using MES.Manager;
using MES.SetModel;
using S7.Net;
using S7.Net.Types;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Windows.Documents;

namespace DAL
{
    public class SiemensS7Instrument
    {
        private CpuType _cpuType = CpuType.S71500;
        private string? _ip = "127.0.0.1";
        private string Name;
        private bool IsConnected;
        private bool isRun = false;
        private string Message;

        private List<PLCGroup> PLCGroups = new List<PLCGroup>();

        /// <summary>
        /// Dictionary<PLCGroupType,Dictionray<TagName,DataItemp[]>>
        /// </summary>
        public Dictionary<string, Dictionary<string, DataItem>> DicDataItems = new Dictionary<string, Dictionary<string, DataItem>>();

        public Dictionary<string, int> FormulaData = new Dictionary<string, int>();
        public Dictionary<string, DataItem> DataCollections = new Dictionary<string, DataItem>();
        public Dictionary<string, object> DataInitVal = new Dictionary<string, object>();

        public Plc _commClient;

        public Plc CommClient
        {
            get => _commClient;
        }

        /// <summary>
        /// 是否监控所有数据变化
        /// </summary>
        public bool ChangeAll { get; set; } = false;

        public bool InitailzePLC()
        {
            isRun = false;
            DicDataItems.Clear();
            if (IsConnected)
            {
                _commClient.Close();
                IsConnected = false;
            }

            FormulaData.Clear();
            //DataInitVal.Clear();
            bool PLCRun = Init();
            PLCRun = InitPara();
            Thread.Sleep(1000);
            PLCRun = IsConnected || Open();
            StartPLC();
            return PLCRun;
        }

        public bool Init()
        {
            try
            {
                Name = "西门子S7PLC";

                _cpuType = CpuType.S71500;

                _ip = SetHelper.PLCSetting.IPAddress;

                PLCGroups = new List<PLCGroup>(SetHelper.PLCSetting.ListGroup);

                return true;
            }
            catch (Exception ex)
            {
                SetHelper.ListPLCMessage.ShowInfoQueue(ex.ToString());
                return false;
            }
        }

        public bool InitPara()
        {
            Dictionary<string, DataItem> dataItems = new Dictionary<string, DataItem>();
            foreach (var group in PLCGroups)
            {
                dataItems = new Dictionary<string, DataItem>();
                foreach (var tag in group.ListTag)
                {
                    if (tag.TagName == "") continue;
                    try
                    {
                        int startAdr = 0;
                        int count = 1;
                        byte bitAdr = 0;

                        if (tag.DataType != "bool" && tag.DataType != "string" && tag.DataType != "intArray" && tag.DataType != "boolArray")
                        {
                            startAdr = Convert.ToInt32(tag.TagAddress);
                        }
                        else
                        {
                            string[] tmpAry = tag.TagAddress.Split('.');
                            if (tag.DataType == "bool")
                            {
                                startAdr = Convert.ToInt32(tmpAry[0]);
                                bitAdr = Convert.ToByte(tmpAry[1]);
                            }
                            else if (tag.DataType == "boolArray")
                            {
                                startAdr = Convert.ToInt32(tmpAry[0]);
                                bitAdr = Convert.ToByte(tmpAry[1]);
                                count = Convert.ToInt32(tmpAry[2]);
                            }
                            else
                            {
                                startAdr = Convert.ToInt32(tmpAry[0]);
                                count = Convert.ToInt32(tmpAry[1]);
                            }
                        }

                        DataItem dataItem = new DataItem()
                        {
                            DataType = DataType.DataBlock,
                            VarType = TypeToS7NetType(tag.DataType),
                            DB = tag.TagDbArea,
                            BitAdr = bitAdr,
                            Count = count,
                            StartByteAdr = startAdr,
                            Value = tag.TagValue
                        };
                        if (ChangeAll)
                        {
                            if (!DataInitVal.ContainsKey(tag.TagName))
                                DataInitVal.Add(tag.TagName, new object());
                        }
                        else
                        {
                            if (group.GroupType == PLCGroupName.TriggerGroup.ToString())
                            {
                                if (!DataInitVal.ContainsKey(tag.TagName))
                                    DataInitVal.Add(tag.TagName, new object());
                            }
                        }

                        //注：所有交互变量中，若名称后未添加_1等数字，则默认添加上，为了单工位避免配置每个变量
                        string TagName;
                        int index = tag.TagName.LastIndexOf("_");
                        if (index > 0)
                        {//如果有下划线
                            try
                            {
                                //如果最后一位不符合规范
                                if (!(tag.TagName[index + 1] == '1' || tag.TagName[index + 1] == '2' || tag.TagName[index + 1] == '3' || tag.TagName[index + 1] == '4'
                                    || tag.TagName[index + 1] == '5' || tag.TagName[index + 1] == '6' || tag.TagName[index + 1] == '7' || tag.TagName[index + 1] == '8'
                                        || tag.TagName[index + 1] == '9'))
                                {
                                    TagName = tag.TagName + "_1";
                                }
                                else
                                {
                                    TagName = tag.TagName;
                                }
                            }
                            catch
                            {
                                TagName = tag.TagName + "_1";
                            }
                        }
                        else
                        {//如果没下划线直接加_1
                            TagName = tag.TagName + "_1";
                        }

                        //if (tag.TagName.Contains('_'))
                        //{
                        //    TagName = tag.TagName;
                        //}
                        //else
                        //{
                        //    TagName = tag.TagName + "_1";
                        //}
                        if (group.GroupType == PLCGroupName.FormulaGroup.ToString())
                        {
                            if (!FormulaData.ContainsKey(TagName))
                            {
                                FormulaData.Add(TagName, tag.TagValue.Obj2Int());
                            }
                            else
                            {
                                FormulaData[TagName] = tag.TagValue.Obj2Int();
                            }
                        }
                        if (dataItems.ContainsKey(TagName))
                        {
                            SetHelper.ListPLCMessage.ShowInfoQueue($"{TagName}重复");
                            dataItems[TagName] = dataItem;
                        }
                        else
                        {
                            dataItems.Add(TagName, dataItem);
                        }
                    }
                    catch (Exception ex)
                    {
                        SetHelper.ListPLCMessage.ShowInfoQueue(ex.ToString());
                        continue;
                    }
                }
                if (DicDataItems.ContainsKey(group.GroupType))
                {
                    DicDataItems[group.GroupType] = dataItems;
                }
                else
                {
                    DicDataItems.Add(group.GroupType, dataItems);
                }
            }
            return true;
        }

        private VarType TypeToS7NetType(object type)
        {
            VarType itemType = 0;
            switch (type.ToString())
            {
                case "bool":
                    itemType = VarType.Bit;
                    break;

                case "boolArray":
                    itemType = VarType.Bit;
                    break;

                case "int":
                    itemType = VarType.Int;
                    break;

                case "intArray":
                    itemType = VarType.Int;
                    break;

                case "string":
                    itemType = VarType.S7String;
                    break;

                case "real":
                    itemType = VarType.Real;
                    break;
            }

            return itemType;
        }

        public bool Open()
        {
            try
            {
                if (!PingIp())
                {
                    return false;
                }

                _commClient = new Plc(_cpuType, _ip, 0, 0);
                _commClient.Open();
                IsConnected = true;
            }
            catch (Exception ex)
            {
                IsConnected = false;

                Message = _ip + "设备连接失败=>" + ex.Message;

                return false;
            }

            return true;
        }

        /// <summary>
        /// ping ip,测试能否ping通
        /// </summary>
        /// <returns></returns>
        private bool PingIp()
        {
            bool bRet = false;
            try
            {
                Ping pingSend = new Ping();
                PingReply reply = pingSend.Send(_ip, 1000);
                if (reply.Status == IPStatus.Success)
                {
                    bRet = true;
                }
                else
                {
                    Message = _ip + "ping不通，请检查网络！";
                    bRet = false;
                }
            }
            catch (Exception ex)
            {
                Message = _ip + "ping不通=>" + ex.Message;
                bRet = false;
            }

            return bRet;
        }

        public bool Reset()
        {
            try
            {
                if (_commClient != null)
                {
                    _commClient.Close();
                }
                IsConnected = false;

                return this.Open();
            }
            catch (Exception ex)
            {
                Message = _ip + "设备复位失败=>" + ex.Message;
                IsConnected = false;
            }
            return false;
        }

        public bool ReadItems(object address, ref object data)
        {
            try
            {
                if (!IsConnected)
                {
                    data = new object();
                    return false;
                }

                if (address != null)
                {
                    if (address is DataItem)
                    {
                        DataItem dataItem = (DataItem)address;
                        object tmpData = _commClient.Read(DataType.DataBlock, dataItem.DB, dataItem.StartByteAdr, dataItem.VarType, dataItem.Count, dataItem.BitAdr);
                        data = tmpData;
                    }
                    else if (address is DataItem[])
                    {
                        DataItem[] dataItems = (DataItem[])address;
                        object[] tmpData = new object[dataItems.Length];
                        _commClient.ReadMultipleVars(new List<DataItem>(dataItems));
                        for (int i = 0; i != dataItems.Length; i++)
                        {
                            tmpData[i] = dataItems[i].Value;
                        }
                        data = tmpData;
                    }

                    return true;
                }

                Message = _ip + "数据读取失败！";
            }
            catch (Exception ex)
            {
                IsConnected = false;

                Message = _ip + "数据读取失败=>" + ex.Message;
                return false;
            }

            return false;
        }

        public bool WriteItems(object address, object data)
        {
            try
            {
                if (!IsConnected)
                {
                    data = new object();
                    return false;
                }
                if (address != null && data != null)
                {
                    if (address is DataItem)
                    {
                        DataItem dataItem = (DataItem)address;
                        if (data is int)
                        {
                            _commClient.Write(DataType.DataBlock, dataItem.DB, dataItem.StartByteAdr, Convert.ToInt16(data));
                        }
                        else if (data is string)
                        {
                            _commClient.Write(DataType.DataBlock, dataItem.DB, dataItem.StartByteAdr, GetPlcStringByteArray(data.ToString()));
                        }
                        else if (data is byte)
                        {
                            _commClient.Write(DataType.DataBlock, dataItem.DB, dataItem.StartByteAdr, Convert.ToByte(data));
                        }
                        else if (data is float || data is double || data is decimal)
                        {
                            _commClient.Write(DataType.DataBlock, dataItem.DB, dataItem.StartByteAdr, Convert.ToSingle(data));
                        }
                        else if (data is bool)
                        {
                            _commClient.Write(DataType.DataBlock, dataItem.DB, dataItem.StartByteAdr, Convert.ToBoolean(data), Convert.ToInt16(dataItem.BitAdr));
                        }
                        else
                        {
                            _commClient.Write(DataType.DataBlock, dataItem.DB, dataItem.StartByteAdr, data);
                        }
                    }
                    else if (address is DataItem[])
                    {
                        DataItem[] dataItems = (DataItem[])address;
                        object[] objects = (object[])data;
                        int count = objects.Length / 20;
                        for (int j = 0; j < count + 1; j++)
                        {
                            List<DataItem> datasSend = new List<DataItem>();
                            int maxCount = j == count ? objects.Length : (j + 1) * 20;
                            for (int i = j * 20; i != maxCount; i++)
                            {
                                if (objects[i] is int)
                                {
                                    dataItems[i].Value = Convert.ToInt16(objects[i]);
                                }
                                else if (objects[i] is string)
                                {
                                    dataItems[i].Value = GetPlcStringByteArray(objects[i].ToString());
                                }
                                else if (objects[i] is byte)
                                {
                                    dataItems[i].Value = Convert.ToByte(objects[i]);
                                }
                                else if (objects[i] is float || objects[i] is double || objects[i] is decimal)
                                {
                                    dataItems[i].Value = Convert.ToSingle(objects[i]);
                                }
                                else if (objects[i] is bool)
                                {
                                    dataItems[i].Value = Convert.ToBoolean(objects[i]);
                                }
                                else
                                {
                                    dataItems[i].Value = objects[i];
                                }
                                datasSend.Add(dataItems[i]);
                            }
                            _commClient.Write(datasSend.ToArray());
                        }
                    }

                    return true;
                }

                Message = _ip + "数据写入失败！";
            }
            catch (Exception ex)
            {
                IsConnected = false;
                SetHelper.ListPLCMessage.ShowInfoQueue(ex.ToString());
                Message = _ip + "数据写入失败=>" + ex.Message;
                return false;
            }
            return false;
        }

        public bool WriteItem(PLCGroupName groupName, string tagItem, object data)
        {
            try
            {
                if (!IsConnected)//测试用
                {
                    return false;
                }

                DataItem address = GetDataItem(groupName, tagItem);

                if (address != null && data != null)
                {
                    if (address is DataItem)
                    {
                        DataItem dataItem = (DataItem)address;
                        if (data is int)
                        {
                            _commClient.Write(DataType.DataBlock, dataItem.DB, dataItem.StartByteAdr, Convert.ToInt16(data));
                        }
                        else if (data is string)
                        {
                            _commClient.Write(DataType.DataBlock, dataItem.DB, dataItem.StartByteAdr, GetPlcStringByteArray(data.ToString()));
                        }
                        else if (data is byte)
                        {
                            _commClient.Write(DataType.DataBlock, dataItem.DB, dataItem.StartByteAdr, Convert.ToByte(data));
                        }
                        else if (data is float || data is double || data is decimal)
                        {
                            _commClient.Write(DataType.DataBlock, dataItem.DB, dataItem.StartByteAdr, Convert.ToSingle(data));
                        }
                        else if (data is bool)
                        {
                            _commClient.Write(DataType.DataBlock, dataItem.DB, dataItem.StartByteAdr, Convert.ToBoolean(data), Convert.ToInt16(dataItem.BitAdr));
                        }
                        else
                        {
                            _commClient.Write(DataType.DataBlock, dataItem.DB, dataItem.StartByteAdr, data);
                        }
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                IsConnected = false;

                Message = _ip + "数据写入失败=>" + ex.Message;
                return false;
            }
            return false;
        }

        public bool ReadItem(PLCGroupName groupName, PLCTagItem tagItem, ref object data)
        {
            try
            {
                if (!IsConnected)
                {
                    data = new object();
                    return false;
                }
                DataItem address = GetDataItem(groupName, tagItem);

                if (address != null)
                {
                    if (address is DataItem)
                    {
                        DataItem dataItem = (DataItem)address;
                        object tmpData = _commClient.Read(DataType.DataBlock, dataItem.DB, dataItem.StartByteAdr, dataItem.VarType, dataItem.Count, dataItem.BitAdr);
                        data = tmpData;
                    }
                    //else if (address is DataItem[])
                    //{
                    //    DataItem[] dataItems = (DataItem[])address;
                    //    object[] tmpData = new object[dataItems.Length];
                    //    _commClient.ReadMultipleVars(new List<DataItem>(dataItems));
                    //    for (int i = 0; i != dataItems.Length; i++)
                    //    {
                    //        tmpData[i] = dataItems[i].Value;
                    //    }
                    //    data = tmpData;
                    //}

                    return true;
                }

                Message = _ip + "数据读取失败！";
            }
            catch (Exception ex)
            {
                IsConnected = false;

                Message = _ip + "数据读取失败=>" + ex.Message;
                return false;
            }

            return false;
        }

        public bool ReadItem(PLCGroupName groupName, string tagItem, ref object data)
        {
            try
            {
                if (!IsConnected)
                {
                    data = new object();
                    return false;
                }
                DataItem address = GetDataItem(groupName, tagItem);

                if (address != null)
                {
                    if (address is DataItem)
                    {
                        DataItem dataItem = (DataItem)address;
                        object tmpData = _commClient.Read(DataType.DataBlock, dataItem.DB, dataItem.StartByteAdr, dataItem.VarType, dataItem.Count, dataItem.BitAdr);
                        if (dataItem.VarType == VarType.S7String)
                        {
                            var str = tmpData.ToString().Replace("\0", "");
                            tmpData = str;
                        }
                        data = tmpData;
                    }
                    //else if (address is DataItem[])
                    //{
                    //    DataItem[] dataItems = (DataItem[])address;
                    //    object[] tmpData = new object[dataItems.Length];
                    //    _commClient.ReadMultipleVars(new List<DataItem>(dataItems));
                    //    for (int i = 0; i != dataItems.Length; i++)
                    //    {
                    //        tmpData[i] = dataItems[i].Value;
                    //    }
                    //    data = tmpData;
                    //}

                    return true;
                }

                Message = _ip + "数据读取失败！";
            }
            catch (Exception ex)
            {
                IsConnected = false;

                Message = _ip + "数据读取失败=>" + ex.Message;
                return false;
            }

            return false;
        }

        public DataItem GetDataItem(PLCGroupName groupName, string tagItem)
        {
            try
            {
                DataItem item = DicDataItems[groupName.ToString()][tagItem.ToString()];
                if (item != null)
                    return item;
            }
            catch (Exception ex)
            {
                //SetHelper.ListPLCMessage.ShowInfoQueue(ex.ToString());
                SetHelper.ListPLCMessage.ShowInfoQueue($"未找到标签--{groupName}--{tagItem}");
            }

            return null;
        }

        public DataItem GetDataItem(PLCGroupName groupName, PLCTagItem tagItem)
        {
            try
            {
                DataItem item = DicDataItems[groupName.ToString()][tagItem.ToString()];
                if (item != null)
                    return item;
            }
            catch (Exception ex)
            {
                SetHelper.ListPLCMessage.ShowInfoQueue(ex.ToString());
            }
            SetHelper.ListPLCMessage.ShowInfoQueue($"未找到标签--{groupName}--{tagItem}");
            return null;
        }

        /// <summary>
        /// 获取西门子PLC字符串数组--String
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private byte[] GetPlcStringByteArray(string str)
        {
            byte[] value = Encoding.Default.GetBytes(str);
            byte[] head = new byte[2];
            head[0] = Convert.ToByte(254);
            head[1] = Convert.ToByte(str.Length);
            value = head.Concat(value).ToArray();
            return value;
        }

        public delegate void DataChangeEvent(string TagName, int Address, int Bit, object TagValue);

        public event DataChangeEvent OnDataChange;

        /// <summary>
        /// 开启连接
        /// </summary>
        /// <returns></returns>
        public void StartPLC()
        {
            isRun = true;
            new Thread(() =>
            {
                while (isRun)
                {
                    try
                    {
                        if (!IsConnected)
                        {
                            Open();
                            Thread.Sleep(1000);
                            continue;
                        }
                        Thread.Sleep(200);
                        //只循环读取触发组数据
                        var monitorGroup = DicDataItems[PLCGroupName.TriggerGroup.ToString()];
                        if (ChangeAll)
                        {
                            foreach (var DicDataItem in DicDataItems.Values)
                            {
                                foreach (var item in DicDataItem)
                                {
                                    if (!monitorGroup.ContainsKey(item.Key))
                                    {
                                        monitorGroup.Add(item.Key, item.Value);
                                    }
                                }
                            }
                        }
                        foreach (var tagItem in monitorGroup)
                        {
                            //就去读数据
                            object objNew = null;
                            Stopwatch sw = new Stopwatch();
                            sw.Start();

                            if (ReadItems(tagItem.Value, ref objNew))
                            {
                                object InitVal = new object();
                                DataInitVal.TryGetValue(tagItem.Key, out InitVal);

                                sw.Stop();
                                if (ChangeAll)
                                {
                                    if (OnDataChange != null)
                                    {
                                        OnDataChange.BeginInvoke(tagItem.Key, tagItem.Value.StartByteAdr, tagItem.Value.BitAdr, objNew, null, null);
                                    }
                                }
                                else
                                {
                                    if (!DataCompare(objNew, InitVal))
                                    {
                                        if (OnDataChange != null)
                                        {
                                            OnDataChange.BeginInvoke(tagItem.Key, tagItem.Value.StartByteAdr, tagItem.Value.BitAdr, objNew, null, null);
                                        }
                                    }
                                }
                                DataInitVal[tagItem.Key] = objNew;
                            }
                            else
                            {
                                //重连
                                Reset();
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        SetHelper.ListPLCMessage.ShowInfoQueue(ex.ToString());
                        Thread.Sleep(2000);
                    }
                }
            })
            { IsBackground = true }.Start();
        }

        private bool DataCompare(object obj, object obj1)
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                if (obj == null || obj1 == null)
                {
                    if (obj == null && obj1 == null)
                        return true;
                    else
                        return false;
                }

                BinaryFormatter binaryFormatter = new BinaryFormatter(null, new StreamingContext(StreamingContextStates.Clone));
                binaryFormatter.Serialize(memStream, obj);
                byte[] b1 = memStream.ToArray();
                memStream.SetLength(0);

                binaryFormatter.Serialize(memStream, obj1);
                byte[] b2 = memStream.ToArray();

                if (b1.Length != b2.Length)
                    return false;

                for (int i = 0; i < b1.Length; i++)
                {
                    if (b1[i] != b2[i])
                        return false;
                }

                return true;
            }
        }
    }
}