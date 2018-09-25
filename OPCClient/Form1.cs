using System;
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
        OPCClient myOPCClient = new OPCClient();
        private void btSearch_Click(object sender, EventArgs e)
        {
            if (myOPCClient.SearchOPCSevers(ref servers))
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
                toolStripStatusLabel1.Text = "没有搜索到任何OPC 服务器";
            }
        }
        private void TagDataChange(string tag, string str)
        {
            tbReadValue.Text = tag + ": " + str;
        }

        int tagNum = 0;
        private void btConnect_Click(object sender, EventArgs e)
        {
            if (myOPCClient.ConnectToServer(cmbServers.Text, ref tagNum))
            {
                toolStripStatusLabel1.Text = "已连接到： " + cmbServers.Text;
                string[] tags = new string[tagNum];
                myOPCClient.getTags(tags);
                listBox1.Items.Clear();
                foreach (string tag in tags)
                {
                    listBox1.Items.Add(tag);
                }
                //listBox1.SelectedIndex = 0;
                myOPCClient.SetTagDataUpdateFunc(TagDataChange);
            }
            else
            {
                toolStripStatusLabel1.Text = "连接" + cmbServers.Text + "失败";
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //tbReadValue.Text = "正在读取";
            //myOPCClient.BeginUpdate(listBox1.SelectedItem.ToString());
            //myOPCClient.AsyncReadTagValue(listBox1.SelectedItem.ToString());
            Array readValues;
            myOPCClient.SyncReadTagValue(listBox1.SelectedItem.ToString(), out readValues);
            tbReadValue.Text = listBox1.SelectedItem.ToString() + ": " + readValues.GetValue(1).ToString();
        }

        private void btWrite_Click(object sender, EventArgs e)
        {
            //myOPCClient.AsyncWriteTagValue(listBox1.SelectedItem.ToString(), tbWriteValue.Text);
            myOPCClient.SyncWriteTagValue(listBox1.SelectedItem.ToString(), tbWriteValue.Text);
        }

        private void btExit_Click(object sender, EventArgs e)
        {
            myOPCClient.DisconnectToServer();
        }
    }
}
