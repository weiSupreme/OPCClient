using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OPCAutomation;

namespace OPCClient
{
    class OPCClient
    {
        /// <summary>
        /// 定义变量
        /// </summary>
        OPCServer KepServer;
        OPCGroups KepGroups;
        OPCGroup KepGroup;
        OPCBrowser oPCBrowser;
        OPCItems KepItems;
        OPCItem KepItem;
        int updateTime = 250;

        int itmHandleClient = 0;
        int itmHandleServer = 0;

        string[] tagList = new string[50];
        int tagCounts = 0;

        public OPCClient() { }

        public delegate void TagDataChange(string str, string tag);
        public TagDataChange _TagDataChange;
        public void SetTagDataChangeFunc(TagDataChange tdc)
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
                    tagList[idx]=turn.ToString();
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
        public bool ConnectToServer(string serverName, ref int tagNum, string commonTagName="Tag")
        {

            try
            {
                KepServer.Connect(serverName);
                KepGroups = KepServer.OPCGroups;
                KepServer.OPCGroups.DefaultGroupIsActive = true;
                KepServer.OPCGroups.DefaultGroupDeadband = 0;
                KepServer.OPCGroups.DefaultGroupUpdateRate = updateTime;
                KepGroup = KepGroups.Add("OPCDOTNETGROUP");

                KepGroup.IsActive = true;
                KepGroup.IsSubscribed = true;

                oPCBrowser = KepServer.CreateBrowser();

                tagCounts = RecurBrowse(oPCBrowser, commonTagName);
                tagNum = tagCounts;

                KepGroup.DataChange += new DIOPCGroupEvent_DataChangeEventHandler(KepGroup_DataChange);
                KepItems = KepGroup.OPCItems;
                return true;
            }
            catch (Exception err)
            {
                return false;
            }
        }

        public void DisconnectToServer()
        {
            if (KepGroup != null)
            {
                KepGroup.DataChange -= new DIOPCGroupEvent_DataChangeEventHandler(KepGroup_DataChange);
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
        private void KepGroup_DataChange(int TransactionID, int NumItems, ref Array ClientHandles, ref Array ItemValues, ref Array Qualities, ref Array TimeStamps)
        {
            for (int i = 1; i <= NumItems; i++)
            {
                _TagDataChange(ItemValues.GetValue(i).ToString(), tagList[Convert.ToInt16(ClientHandles.GetValue(i).ToString()) - 1]);
            }
        }

        private string[] itemList = new string[50];
        private int itemListIdx = 0;
        public void BeginUpdate(string tag)
        {
            try
            {
                itmHandleClient =Array.IndexOf(tagList,tag) + 1;
                int idx =Array.IndexOf(itemList, tag);
                if (idx == -1)
                {
                    KepItem = KepItems.AddItem(tag, itmHandleClient);
                    itmHandleServer = KepItem.ServerHandle;
                    itemList[itemListIdx++] = tag;
                }
                else
                {
                    OPCItems KepItemsTmp;
                    KepItemsTmp = KepGroup.OPCItems;
                    KepItem = KepItemsTmp.AddItem(tag, itmHandleClient);
                    itmHandleServer = KepItem.ServerHandle;
                }

                //int cnt = KepItems.Count;
            }
            catch (Exception err)
            {
                //没有任何权限的项，都是OPC服务器保留的系统项，此处可不做处理。
                itmHandleClient = 0;
                //MessageBox.Show("此项为系统保留项:" + err.Message, "提示信息");
            }
        }

        public bool AsyncReadTagValue(string tag)
        {
            return true;
        }
        public bool AsyncWriteTagValue(string writeStr)
        {
            OPCItem bItem = KepItems.GetOPCItem(itmHandleServer);
            int[] temp = new int[2] { 0, bItem.ServerHandle };
            Array serverHandles = (Array)temp;
            object[] valueTemp = new object[2] { "",  writeStr};
            Array values = (Array)valueTemp;
            Array Errors;
            int cancelID;
            KepGroup.AsyncWrite(1, ref serverHandles, ref values, out Errors, 2009, out cancelID);
            GC.Collect();
            return true;
        }
    }
}
