using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.IO.Ports;
using HslCommunication.Serial;
using HslCommunication.BasicFramework;
using HslCommunication.Core;

namespace TNRPC
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show("退出实时数据采集系统吗?", "退出", MessageBoxButtons.YesNo, MessageBoxIcon.Error, MessageBoxDefaultButton.Button2) == DialogResult.No)
            {
                e.Cancel = true;
            }
        }

        private void FormMain_Shown(object sender, EventArgs e)
        {
            string used = ConfigurationManager.AppSettings["USED"];
            string[] coms = used.Split(',');
            foreach (string com in coms)
            {
                Thread worker = new Thread(new ParameterizedThreadStart(Transceiver));
                //随主线程退出而退出
                worker.IsBackground = true;
                worker.Start(ConfigurationManager.AppSettings[com]);
            }
        }

        private void Transceiver(Object com)
        {
            string[] parameters = com.ToString().Split(',');
            SerialPort serialPort = new SerialPort();
            serialPort.PortName = parameters[0];
            serialPort.BaudRate = Convert.ToInt32(parameters[1]);
            serialPort.DataBits = Convert.ToInt32(parameters[2]);
            serialPort.Parity = (Parity)Convert.ToInt32(parameters[3]);
            serialPort.StopBits = (StopBits)Convert.ToInt32(parameters[4]);
            int startNo = Convert.ToInt32(parameters[5]);
            int num = Convert.ToInt32(parameters[6]);
            while (true)
            {
                //保证循环不退出
                try
                {
                    if (!serialPort.IsOpen)
                    {
                        serialPort.Open();
                    }
                    for (int i = 1; i <= num; i++)
                    {
                        /////////////////////////////////////////发送数据（modbusRTU协议）
                        string orderWithoutCrc = string.Format("{0:X2}", i) + "0310010001";
                        byte[] bufferS = SoftCRC16.CRC16(SoftBasic.HexStringToBytes(orderWithoutCrc));
                        serialPort.Write(bufferS, 0, bufferS.Length);
                        //将发送的数据显示在窗体相应的位置
                        switch (parameters[0])
                        {
                            case "COM4":
                            case "COM5":
                            case "COM6":
                            case "COM7":
                            case "COM8":
                                SetText("textBox16", parameters[0] + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>" + SoftBasic.ByteToHexString(bufferS) + "\n");
                                break;
                            case "COM9":
                                SetText("textBox14", parameters[0] + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>" + SoftBasic.ByteToHexString(bufferS) + "\n");
                                break;
                        }
                        //////////////////////////////////////////等待数据
                        Thread.Sleep(500);
                        /////////////////////////////////////////接收数据。
                        byte[] bufferR = null;
                        if (serialPort.BytesToRead > 0)
                        {
                            bufferR = new byte[serialPort.BytesToRead];
                            serialPort.Read(bufferR, 0, bufferR.Length);
                        }
                        //将接收的数据显示在窗体相应的位置，没有回应数据显示N/A。
                        switch (parameters[0])
                        {
                            case "COM4":
                            case "COM5":
                            case "COM6":
                            case "COM7":
                            case "COM8":
                                SetText("textBox15", parameters[0] + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=" + ((bufferR is null) ? "N/A" : SoftBasic.ByteToHexString(bufferR)) + "\n");
                                break;
                            case "COM9":
                                SetText("textBox13", parameters[0] + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=" + ((bufferR is null) ? "N/A" : SoftBasic.ByteToHexString(bufferR)) + "\n");
                                break;
                        }
                        //////////////////////////////////////数据解析，在界面上显示结果，并存储到数据库
                        string showResult = null;//界面显示的结果
                        ReverseBytesTransform transform = new ReverseBytesTransform();//数据转换工具
                        if (bufferR is null)
                        {
                            showResult = "N/A";//设备不可用
                        }
                        else if (!SoftCRC16.CheckCRC16(bufferR))
                        {
                            showResult = "ERROR";//返回数据错误
                        }
                        else
                        {
                            showResult = ((float)transform.TransInt16(bufferR, 3) / 10).ToString() + "℃";
                            //保存到数据库

                        }
                        //设置界面
                        SetText("label" + (startNo + i ), showResult);
                    }
                    //间隔5分钟采集一次数据
                    Thread.Sleep(300000);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        /*
         * 子线程设置主界面控件
         **/
        private delegate void SetTextCallback(string name, string text);
        private void SetText(string name, string text)
        {
            Control c = Controls.Find(name, true)[0];
            if (c.InvokeRequired)
            {
                SetTextCallback setTextCallback = new SetTextCallback(SetText);
                c.Invoke(setTextCallback, new object[] { name, text });
            }
            else
            {
                if (c is TextBox)
                {
                    TextBox textBox = (TextBox)c;
                    if (textBox.TextLength > 5000)
                    {
                        textBox.Clear();
                    }
                    textBox.AppendText(text);
                    textBox.Refresh();
                }
                if (c is Label)
                {
                    ((Label)c).Text = text;
                }
            }
        }
    }
}
