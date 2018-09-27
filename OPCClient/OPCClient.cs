using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OPCAutomation;

namespace OPCClient
{
    class OPCClientClass
    {
        /// <summary>
        /// 定义变量
        /// </summary>
        OPCServer KepServer;
        OPCGroups KepGroups;
        OPCGroup KepGroupDataChange;
        OPCGroup KepGroupWriteData;
        OPCGroup KepGroupReadData;
        OPCItems KepItemsWrite;
        OPCItem KepItemWrite;
        OPCItems KepItemsRead;
        OPCItem KepItemRead;
        OPCBrowser oPCBrowser;
        OPCItems KepItems;
        OPCItem KepItem;

        public int updateTime = 300;
        public bool isConnected2Server = false;
        public static int tagCountsMax = 50;

        string[] tagList = new string[tagCountsMax];
        int tagCounts = 0;

        int itmHandleClientDataChange = 0;
        int itmHandleServerDataChange = 0;

        int itmHandleClientWriteData = 0;
        int itmHandleServerWriteData = 0;

        int itmHandleClientReadData = 0;
        int itmHandleServerReadData = 0;

        public OPCClientClass() { }

        public delegate void TagDataChange(string tag, object itemValue, object quality, object timestamp);
        private TagDataChange _TagDataChange;
        public void SetTagDataUpdateFunc(TagDataChange tdc)
        {
            this._TagDataChange = tdc;
        }

        public delegate void AsyncWriteComplete(string tag, Int64 error);
        private AsyncWriteComplete _AsyncWriteComplete;
        public void SetAsyncWriteCompleteFunc(AsyncWriteComplete awc)
        {
            this._AsyncWriteComplete = awc;
        }


        public string SearchOPCSevers(ref object serverList)
        {
            try
            {
                KepServer = new OPCServer();
                serverList = KepServer.GetOPCServers("");
                return "OK";
            }
            catch (Exception err)
            {
                return "搜索失败："+err.Message;
            }
        }

        /// <summary>
        /// 列出OPC服务器中所有节点
        /// </summary>
        private int RecurBrowse(OPCBrowser oPCBrowser, bool initFlag)
        {
            //展开分支
            oPCBrowser.ShowBranches();
            //展开叶子
            oPCBrowser.ShowLeafs(true);
            byte idx = 0;
            foreach (object turn in oPCBrowser)
            {
                //if (string.Compare(turn.ToString(),"Tags")==0)//
                //if ((turn.ToString().IndexOf(cTagName)) > -1)
                {
                    tagList[idx]=turn.ToString();
                    if(initFlag)
                        BeginUpdate(turn.ToString());
                    ++idx;
                }
            }
            return idx;
        }

        public void InitSomeTags(string[] tags, int count)
        {
            for (int i = 0; i < count; i++)
            {
                BeginUpdate(tags[i]);
            }
            KepGroupDataChange.DataChange += new DIOPCGroupEvent_DataChangeEventHandler(KepGroupDataChange_DataChange);

        }

        public void GetTags(string[] tags)
        {
            for (int i = 0; i < tagCounts; i++)
            {
                tags[i] = tagList[i];
            }
        }

        public int GetTagsCount()
        {
            return tagCounts;
        }

        /// <summary>
        /// 建立连接按钮
        /// </summary>
        public string ConnectToServer(string serverName, bool initAllTagsFlag=false)
        {
            try
            {
                KepServer.Connect(serverName);
                //string str = KepServer.ServerName;
                KepGroups = KepServer.OPCGroups;
                KepServer.OPCGroups.DefaultGroupIsActive = true;
                KepServer.OPCGroups.DefaultGroupDeadband = 0;
                KepServer.OPCGroups.DefaultGroupUpdateRate = updateTime;
                KepGroupDataChange = KepGroups.Add("DATACHANGE");
                KepGroupWriteData = KepGroups.Add("WRITEDATA");
                KepGroupReadData = KepGroups.Add("READDATA");

                KepGroupDataChange.IsActive = true;
                KepGroupDataChange.IsSubscribed = true;

                KepGroupReadData.IsActive = true;
                KepGroupReadData.IsSubscribed = true;

                KepGroupWriteData.IsActive = true;
                KepGroupWriteData.IsSubscribed = true;

                KepItems = KepGroupDataChange.OPCItems;
                KepItemsWrite = KepGroupWriteData.OPCItems;
                KepItemsRead = KepGroupReadData.OPCItems;

                oPCBrowser = KepServer.CreateBrowser();
                tagCounts = RecurBrowse(oPCBrowser, initAllTagsFlag);

                if(initAllTagsFlag)
                    KepGroupDataChange.DataChange += new DIOPCGroupEvent_DataChangeEventHandler(KepGroupDataChange_DataChange);
                KepGroupReadData.AsyncReadComplete += new DIOPCGroupEvent_AsyncReadCompleteEventHandler(KepGroupReadData_AsyncReadComplete);
                KepGroupWriteData.AsyncWriteComplete+=new DIOPCGroupEvent_AsyncWriteCompleteEventHandler(KepGroupWriteData_AsyncWriteComplete);
                isConnected2Server = true;
                return "OK";
            }
            catch (Exception err)
            {
                return "连接失败："+err.Message;
            }
        }

        public void DisconnectToServer()
        {
            if (KepGroupDataChange != null)
            {
                KepGroupDataChange.DataChange -= new DIOPCGroupEvent_DataChangeEventHandler(KepGroupDataChange_DataChange);
            }

            if (KepServer != null)
            {
                KepServer.Disconnect();
                KepServer = null;
                isConnected2Server = false;
            }
        }

        /// <summary>
        /// 每当项数据有变化时执行的事件
        /// </summary>
        /// <param name="TransactionID">处理ID</param>
        /// <param name="NumItems">项个数</param>
        /// <param name="ClientHandles">项客户端句柄</param>
        /// <param name="ItemValues">TAG值</param>
        bool initializeFlag = true;
        private void KepGroupDataChange_DataChange(int TransactionID, int NumItems, ref Array ClientHandles, ref Array ItemValues, ref Array Qualities, ref Array TimeStamps)
        {
            if (!initializeFlag)
                for (int i = 1; i <= NumItems; i++)
                {
                    _TagDataChange(tagList[Convert.ToInt16(ClientHandles.GetValue(i).ToString()) - 1], ItemValues.GetValue(i), Qualities.GetValue(i), TimeStamps.GetValue(i));
                }
            else
            {
                initializeFlag = false;
            }
        }

        private string[] itemList = new string[tagCountsMax];
        private int itemListIdx = 0;
        private void BeginUpdate(string tag)
        {
            try
            {
                if (Array.IndexOf(itemList, tag) == -1)
                {
                    itmHandleClientDataChange = Array.IndexOf(tagList, tag) + 1;
                    KepItem = KepItems.AddItem(tag, itmHandleClientDataChange);
                    itmHandleServerDataChange = KepItem.ServerHandle;
                    itemList[itemListIdx++] = tag;
                }
            }
            catch (Exception err)
            {
                //没有任何权限的项，都是OPC服务器保留的系统项，此处可不做处理。
                itmHandleClientDataChange = 0;
                //MessageBox.Show("此项为系统保留项:" + err.Message, "提示信息");
            }
        }

        private void KepGroupReadData_AsyncReadComplete(int TransactionID, int NumItems, ref Array ClientHandles, ref Array ItemValues, ref Array Qualities, ref Array TimeStamps, ref Array Errors)
        {
            for (int i = 1; i <= NumItems; i++)
            {
                _TagDataChange(tagList[Convert.ToInt16(ClientHandles.GetValue(i).ToString()) - 1], ItemValues.GetValue(i), Qualities.GetValue(i), TimeStamps.GetValue(i));
            }
        }

        public string AsyncReadTagValue(string tag)
        {
            try
            {
                Array Errors;
                if (itmHandleClientReadData != 0)
                {
                    OPCItem bItem = KepItemsRead.GetOPCItem(itmHandleServerReadData);
                    //注：OPC中以1为数组的基数
                    int[] temp = new int[2] { 0, bItem.ServerHandle };
                    Array serverHandle = (Array)temp;
                    //移除上一次选择的项
                    KepItemsRead.Remove(KepItemsRead.Count, ref serverHandle, out Errors);
                }
                itmHandleClientReadData = Array.IndexOf(tagList, tag) + 1;
                KepItemRead = KepItemsRead.AddItem(tag, itmHandleClientReadData);
                itmHandleServerReadData = KepItemRead.ServerHandle;

                OPCItem bItem_ = KepItemsRead.GetOPCItem(itmHandleServerReadData);
                int[] temp_ = new int[2] { 0, bItem_.ServerHandle };
                Array serverHandles = (Array)temp_;
                int cancelID;
                //KepGroupReadData.AsyncWrite(1, ref serverHandles, ref values, out Errors, 2009, out cancelID);
                KepGroupReadData.AsyncRead(1, ref serverHandles, out Errors, 2018, out cancelID);
                GC.Collect();
                return "OK";
            }
            catch (Exception err)
            {
                return err.Message;
            }
        }

        private void KepGroupWriteData_AsyncWriteComplete(int TransactionID, int NumItems, ref Array ClientHandles, ref Array Errors)
        {
            for (int i = 1; i <= NumItems; i++)
            {
                _AsyncWriteComplete(tagList[Convert.ToInt16(ClientHandles.GetValue(i).ToString()) - 1], Convert.ToInt64(Errors.GetValue(i)));
            }
        }

        public string AsyncWriteTagValue(string tag, string writeStr)
        {
            try
            {
                Array Errors;
                if (itmHandleClientWriteData != 0)
                {
                    OPCItem bItem = KepItemsWrite.GetOPCItem(itmHandleServerWriteData);
                    //注：OPC中以1为数组的基数
                    int[] temp = new int[2] { 0, bItem.ServerHandle };
                    Array serverHandle = (Array)temp;
                    //移除上一次选择的项
                    KepItemsWrite.Remove(KepItemsWrite.Count, ref serverHandle, out Errors);
                }
                itmHandleClientWriteData = Array.IndexOf(tagList, tag) + 1;
                KepItemWrite = KepItemsWrite.AddItem(tag, itmHandleClientWriteData);
                itmHandleServerWriteData = KepItemWrite.ServerHandle;

                OPCItem bItem_ = KepItemsWrite.GetOPCItem(itmHandleServerWriteData);
                int[] temp_ = new int[2] { 0, bItem_.ServerHandle };
                Array serverHandles = (Array)temp_;
                object[] valueTemp = new object[2] { "", writeStr };
                Array values = (Array)valueTemp;
                int cancelID;
                KepGroupWriteData.AsyncWrite(1, ref serverHandles, ref values, out Errors, 2009, out cancelID);
                GC.Collect();
                return "OK";
            }
            catch (Exception err)
            {
                return err.Message;
            }
        }

        public string SyncReadTagValue(string tag, out Array outValues, out object qualities, out object timeStamps)
        {
            try{
                Array Errors;
                if (itmHandleClientReadData != 0)
                {
                    OPCItem bItem = KepItemsRead.GetOPCItem(itmHandleServerReadData);
                    //注：OPC中以1为数组的基数
                    int[] temp = new int[2] { 0, bItem.ServerHandle };
                    Array serverHandle = (Array)temp;
                    //移除上一次选择的项
                    KepItemsRead.Remove(KepItemsRead.Count, ref serverHandle, out Errors);
                }
                itmHandleClientReadData = Array.IndexOf(tagList, tag) + 1;
                KepItemRead = KepItemsRead.AddItem(tag, itmHandleClientReadData);
                itmHandleServerReadData = KepItemRead.ServerHandle;

                OPCItem bItem_ = KepItemsRead.GetOPCItem(itmHandleServerReadData);
                int[] temp_ = new int[2] { 0, bItem_.ServerHandle };
                Array serverHandles = (Array)temp_;

                short src = (short)OPCDataSource.OPCDevice;
                KepGroupReadData.SyncRead(src, 1, ref serverHandles, out outValues, out Errors, out qualities, out timeStamps);
                return "OK";
            }
            catch (Exception err)
            {
                outValues = null;
                qualities = null;
                timeStamps = null;
                return err.Message;
            }
        }

        public string SyncWriteTagValue(string tag, string writeStr)
        {
            try{
                Array Errors;
                if (itmHandleClientWriteData != 0)
                {
                    OPCItem bItem = KepItemsWrite.GetOPCItem(itmHandleServerWriteData);
                    //注：OPC中以1为数组的基数
                    int[] temp = new int[2] { 0, bItem.ServerHandle };
                    Array serverHandle = (Array)temp;
                    //移除上一次选择的项
                    KepItemsWrite.Remove(KepItemsWrite.Count, ref serverHandle, out Errors);
                }
                itmHandleClientWriteData = Array.IndexOf(tagList, tag) + 1;
                KepItemWrite = KepItemsWrite.AddItem(tag, itmHandleClientWriteData);
                itmHandleServerWriteData = KepItemWrite.ServerHandle;

                OPCItem bItem_ = KepItemsWrite.GetOPCItem(itmHandleServerWriteData);
                int[] temp_ = new int[2] { 0, bItem_.ServerHandle };
                Array serverHandles = (Array)temp_;
                object[] valueTemp = new object[2] { "", writeStr };
                Array values = (Array)valueTemp;
                KepGroupWriteData.SyncWrite(1, ref serverHandles, ref values, out Errors);
                return "OK";
            }
            catch (Exception err)
            {
                return err.Message;
            }
        }
    }
}
