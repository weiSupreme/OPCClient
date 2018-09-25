using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OPCAutomation;

namespace OPCClientDLL
{
    public class OPCClientClass
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
        int updateTime = 250;

        int itmHandleClientDataChange = 0;
        int itmHandleServerDataChange = 0;

        int itmHandleClientWriteData = 0;
        int itmHandleServerWriteData = 0;

        int itmHandleClientReadData = 0;
        int itmHandleServerReadData = 0;

        string[] tagList = new string[50];
        int tagCounts = 0;

        public OPCClientClass() { }

        public delegate void TagDataChange(string tag, string str);
        public TagDataChange _TagDataChange;
        public void SetTagDataUpdateFunc(TagDataChange tdc)
        {
            this._TagDataChange = tdc;
        }

        public void SetUpdateTime(int updTime) { updateTime = updTime; }

        public bool SearchOPCSevers(ref object serverList)
        {
            try
            {
                KepServer = new OPCServer();
                serverList = KepServer.GetOPCServers("");
                return true;
            }
            catch (Exception err)
            {
                return false;
            }
        }

        /// <summary>
        /// 列出OPC服务器中所有节点
        /// </summary>
        private int RecurBrowse(OPCBrowser oPCBrowser, string cTagName)
        {
            //展开分支
            oPCBrowser.ShowBranches();
            //展开叶子
            oPCBrowser.ShowLeafs(true);
            byte idx = 0;
            foreach (object turn in oPCBrowser)
            {
                //if (string.Compare(turn.ToString(),"Tags")==0)//
                if ((turn.ToString().IndexOf(cTagName)) > -1)
                {
                    tagList[idx] = turn.ToString();
                    BeginUpdate(turn.ToString());
                    ++idx;
                }

            }
            return idx;
        }

        public void getTags(string[] tags)
        {
            for (int i = 0; i < tagCounts; i++)
            {
                tags[i] = tagList[i];
            }
        }

        /// <summary>
        /// 建立连接按钮
        /// </summary>
        public bool ConnectToServer(string serverName, ref int tagNum, string commonTagName = "Tag", bool initAllTagsFlag = true)
        {

            try
            {
                KepServer.Connect(serverName);
                KepGroups = KepServer.OPCGroups;
                KepServer.OPCGroups.DefaultGroupIsActive = true;
                KepServer.OPCGroups.DefaultGroupDeadband = 0;
                KepServer.OPCGroups.DefaultGroupUpdateRate = updateTime;
                KepGroupDataChange = KepGroups.Add("DATACHANGE");
                KepGroupWriteData = KepGroups.Add("WRITEDATA");
                KepGroupReadData = KepGroups.Add("READDATA");

                KepGroupDataChange.IsActive = true;
                KepGroupDataChange.IsSubscribed = true;

                KepGroupWriteData.IsActive = true;
                KepGroupWriteData.IsSubscribed = true;

                KepGroupReadData.IsActive = true;
                KepGroupReadData.IsSubscribed = true;

                oPCBrowser = KepServer.CreateBrowser();

                KepItems = KepGroupDataChange.OPCItems;
                KepItemsWrite = KepGroupWriteData.OPCItems;
                KepItemsRead = KepGroupReadData.OPCItems;

                tagCounts = RecurBrowse(oPCBrowser, commonTagName);
                tagNum = tagCounts;
                if (initAllTagsFlag)
                    KepGroupDataChange.DataChange += new DIOPCGroupEvent_DataChangeEventHandler(KepGroupDataChange_DataChange);
                KepGroupReadData.AsyncReadComplete += new DIOPCGroupEvent_AsyncReadCompleteEventHandler(KepGroupReadData_AsyncReadComplete);
                return true;
            }
            catch (Exception err)
            {
                return false;
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
            }
        }

        /// <summary>
        /// 每当项数据有变化时执行的事件
        /// </summary>
        /// <param name="TransactionID">处理ID</param>
        /// <param name="NumItems">项个数</param>
        /// <param name="ClientHandles">项客户端句柄</param>
        /// <param name="ItemValues">TAG值</param>
        private bool firstFlag = true;
        private void KepGroupDataChange_DataChange(int TransactionID, int NumItems, ref Array ClientHandles, ref Array ItemValues, ref Array Qualities, ref Array TimeStamps)
        {
            if (firstFlag)
                firstFlag = false;
            else
                for (int i = 1; i <= NumItems; i++)
                {
                    _TagDataChange(tagList[Convert.ToInt16(ClientHandles.GetValue(i).ToString()) - 1], ItemValues.GetValue(i).ToString());
                }
        }

        private string[] itemList = new string[50];
        private int itemListIdx = 0;
        public void BeginUpdate(string tag)
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
                _TagDataChange(tagList[Convert.ToInt16(ClientHandles.GetValue(i).ToString()) - 1], ItemValues.GetValue(i).ToString());
            }
        }

        public bool AsyncReadTagValue(string tag)
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
            return true;
        }

        public bool AsyncWriteTagValue(string tag, string writeStr)
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
            return true;
        }

        public bool SyncReadTagValue(string tag, out Array outValues)
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

            short src = (short)OPCDataSource.OPCDevice;
            object qualities, timeStamps;
            KepGroupReadData.SyncRead(src, 1, ref serverHandles, out outValues, out Errors, out qualities, out timeStamps);
            return true;
        }

        public bool SyncWriteTagValue(string tag, string writeStr)
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
            KepGroupWriteData.SyncWrite(1, ref serverHandles, ref values, out Errors);
            return true;
        }

    }
}
