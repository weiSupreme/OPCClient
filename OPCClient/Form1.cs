﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace OPCClient
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        
        object servers;
        OPCClientClass myOPCClient = new OPCClientClass();
        private void btSearch_Click(object sender, EventArgs e)
        {
            string outErrors = myOPCClient.SearchOPCSevers(ref servers);
            if ("OK"==outErrors)
            {
                toolStripStatusLabel1.Text = "已搜索到OPC 服务器";
                cmbServers.Items.Clear();
                foreach (string turn in (Array)servers)
                {
                    cmbServers.Items.Add(turn);
                }
                cmbServers.SelectedIndex = 0;
            }
            else
            {
                toolStripStatusLabel1.Text = outErrors;
            }
        }
        private void TagDataChange(string tag, object itemValue, object quality, object timestamp)
        {
            tbReadValue.Text = tag + ": " + itemValue.ToString() + "\r\n";
            //tbReadValue.Text += "品质: " + quality.ToString();
        }

        private void AsyncWriteComplete(string tag, Int64 error)
        {
            if (0 == error)
                toolStripStatusLabel1.Text = "写入数据成功";
            else
            {
                toolStripStatusLabel1.Text = "写入数据失败";
            }
        }

        int tagNum = 0;
        private void btConnect_Click(object sender, EventArgs e)
        {
            string outErrors = myOPCClient.ConnectToServer();
            if ("OK" == outErrors)
            {
                toolStripStatusLabel1.Text = "已连接到： " + cmbServers.Text;
                tagNum = myOPCClient.GetTagsCount();
                string[] tags = new string[tagNum];
                myOPCClient.GetTags(tags);
                listBox1.Items.Clear();
                foreach (string tag in tags)
                {
                    listBox1.Items.Add(tag);
                }
                //listBox1.SelectedIndex = 0;
                myOPCClient.SetSubscribeDataUpdateFunc(TagDataChange);
                myOPCClient.SetAsyncWriteCompleteFunc(AsyncWriteComplete);
                string[] subscribeTagList = new string[5];
                subscribeTagList[0] = "Device.TagD2";
                subscribeTagList[1] = "Device.TagD6";
                subscribeTagList[2] = "Device.TagD10";
                subscribeTagList[3] = "Device.TagD12";
                subscribeTagList[4] = "Device.TagD14";
                myOPCClient.InitSomeTags(subscribeTagList, 5);
            }
            else
            {
                toolStripStatusLabel1.Text = outErrors;
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //tbReadValue.Text = "正在读取";
            //myOPCClient.BeginUpdate(listBox1.SelectedItem.ToString());
            myOPCClient.AsyncReadTagValue(listBox1.SelectedItem.ToString());
            //Array readValues;
            //myOPCClient.SyncReadTagValue(listBox1.SelectedItem.ToString(), out readValues);
            //tbReadValue.Text = listBox1.SelectedItem.ToString() + ": " + readValues.GetValue(1).ToString();
        }

        private void btWrite_Click(object sender, EventArgs e)
        {
            //DateTime beforDT = System.DateTime.Now;
            if("" != tbWriteTag.Text)
                myOPCClient.AsyncWriteTagValue("Device." + tbWriteTag.Text, tbWriteValue.Text);
            //myOPCClient.SyncWriteTagValue(listBox1.SelectedItem.ToString(), tbWriteValue.Text);
            //DateTime afterDT = System.DateTime.Now;
            //TimeSpan ts = afterDT.Subtract(beforDT);
            //MessageBox.Show(ts.TotalMilliseconds.ToString());
        }

        private void btExit_Click(object sender, EventArgs e)
        {
            myOPCClient.DisconnectToServer();
            toolStripStatusLabel1.Text = "已断开连接";
        }

        private void btRead_Click(object sender, EventArgs e)
        {
            if("" != tbReadTag.Text)
                myOPCClient.AsyncReadTagValue("Device."+tbReadTag.Text);
        }
    }
}
