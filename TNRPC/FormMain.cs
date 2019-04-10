using System;
using System.Configuration;
using System.Windows.Forms;
using System.Threading;
using System.IO.Ports;
using HslCommunication.Serial;
using HslCommunication.BasicFramework;
using HslCommunication.Core;
using MySql.Data.MySqlClient;

namespace TNRPC {
    public partial class FormMain : Form {
        public FormMain() {
            InitializeComponent();
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e) {
            if (MessageBox.Show("退出实时数据采集系统吗?", "退出", MessageBoxButtons.YesNo, MessageBoxIcon.Error, MessageBoxDefaultButton.Button2) == DialogResult.No) {
                e.Cancel = true;
            }
        }

        private void FormMain_Shown(object sender, EventArgs e) {
            //温度
            string used = ConfigurationManager.AppSettings["WENDU"];
            if (used != null && used.Length > 0) {
                string[] coms = used.Split(',');
                foreach (string com in coms) {
                    Thread worker = new Thread(new ParameterizedThreadStart(WenDu));
                    //随主线程退出而退出
                    worker.IsBackground = true;
                    worker.Start(ConfigurationManager.AppSettings[com]);
                }
            }
            //电度
            used = ConfigurationManager.AppSettings["DIANDU"];
            if (used != null && used.Length > 0) {
                string[] coms = used.Split(',');
                foreach (string com in coms) {
                    Thread worker = new Thread(new ParameterizedThreadStart(DianDu));
                    //随主线程退出而退出
                    worker.IsBackground = true;
                    worker.Start(ConfigurationManager.AppSettings[com]);
                }
            }
        }

        private enum PROCESS {
            CDWD,//充电温度
            GZWD //干燥温度
        }

        //温度
        private void WenDu(Object com) {
            string[] parameters = com.ToString().Split(',');
            //根据参数判断是什么工艺参数
            string process = null;
            string paramID = null;//tb_parameterinfo
            string equipmentTypeID = null;//tb_equipmenttype
            switch (parameters[0]) {
                case "COM4":
                case "COM5":
                case "COM6":
                case "COM7":
                case "COM8":
                    process = PROCESS.CDWD.ToString();
                    paramID = "50001";
                    equipmentTypeID = "3";
                    break;
                case "COM9":
                case "COM10":
                case "COM11":
                    process = PROCESS.GZWD.ToString();
                    paramID = "40001";
                    equipmentTypeID = "4";
                    break;
            }

            SerialPort serialPort = new SerialPort();
            serialPort.PortName = parameters[0];
            serialPort.BaudRate = Convert.ToInt32(parameters[1]);
            serialPort.DataBits = Convert.ToInt32(parameters[2]);
            serialPort.Parity = (Parity)Convert.ToInt32(parameters[3]);
            serialPort.StopBits = (StopBits)Convert.ToInt32(parameters[4]);

            int startNo = Convert.ToInt32(parameters[5]);
            int num = Convert.ToInt32(parameters[6]);

            while (true) {
                //保证循环不退出
                try {
                    if (!serialPort.IsOpen) {
                        serialPort.Open();
                    }
                    for (int i = 1; i <= num; i++) {
                        try {
                            //设备ID，数据库中设置好的数值
                            int equipmentID = startNo + i;

                            /////////////////////////////////////////发送数据（modbusRTU协议）
                            string orderWithoutCrc = string.Format("{0:X2}", i) + "0310010001";
                            byte[] bufferS = SoftCRC16.CRC16(SoftBasic.HexStringToBytes(orderWithoutCrc));
                            serialPort.Write(bufferS, 0, bufferS.Length);
                            //将发送的数据显示在窗体相应的位置
                            if (process.Equals(PROCESS.CDWD.ToString())) {
                                SetText("textBox16", parameters[0] + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>" + SoftBasic.ByteToHexString(bufferS) + "\n");
                            }
                            if (process.Equals(PROCESS.GZWD.ToString())) {
                                SetText("textBox14", parameters[0] + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>" + SoftBasic.ByteToHexString(bufferS) + "\n");
                            }

                            //////////////////////////////////////////等待数据
                            Thread.Sleep(500);

                            /////////////////////////////////////////接收数据。
                            byte[] bufferR = null;
                            if (serialPort.BytesToRead > 0) {
                                bufferR = new byte[serialPort.BytesToRead];
                                serialPort.Read(bufferR, 0, bufferR.Length);
                            }
                            //将接收的数据显示在窗体相应的位置，没有回应数据显示N/A。
                            if (process.Equals(PROCESS.CDWD.ToString())) {
                                SetText("textBox15", parameters[0] + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=" + ((bufferR is null) ? "N/A" : SoftBasic.ByteToHexString(bufferR)) + "\n");
                            }
                            if (process.Equals(PROCESS.GZWD.ToString())) {
                                SetText("textBox13", parameters[0] + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=" + ((bufferR is null) ? "N/A" : SoftBasic.ByteToHexString(bufferR)) + "\n");
                            }

                            //////////////////////////////////////数据解析，在界面上显示结果，并存储到数据库
                            string showResult = null;//界面显示的结果
                            if (bufferR is null) {
                                showResult = "N/A";//设备不可用
                            } else if (!SoftCRC16.CheckCRC16(bufferR)) {
                                showResult = "ERROR";//返回数据错误
                            } else {
                                //解析数据
                                ReverseBytesTransform transform = new ReverseBytesTransform();//数据转换工具
                                float data = (float)transform.TransInt16(bufferR, 3) / 10;
                                //水温值超出正常范围
                                if (data > 100 || data < 0) {
                                    showResult = "ERROR";
                                } else {
                                    showResult = data.ToString("f1") + "℃";//显示温度值

                                    //保存到数据库
                                    using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MYSQL"].ConnectionString)) {
                                        //打开数据库连接
                                        conn.Open();

                                        //判断当前值是否超上限或超下限
                                        string status = "2";//2表示在范围之内
                                        using (MySqlCommand cmd = new MySqlCommand("select max, min FROM tb_parameterinfo where id = '" + paramID + "'", conn)) {
                                            using (MySqlDataReader reader = cmd.ExecuteReader()) {
                                                if (reader.Read()) {
                                                    if (data > reader.GetFloat("max")) {
                                                        status = "3";
                                                    } else if (data < reader.GetFloat("min")) {
                                                        status = "1";
                                                    }
                                                }
                                            }

                                            //准备插入一条数据
                                            cmd.CommandText = "insert into tb_equipmentparamrecord (id,equipmentid,paramID,recordTime,value,recorder,equipmentTypeID,status) values('" + Guid.NewGuid().ToString("N") + "','" + equipmentID + "','" + paramID + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + data + "','仪表采集','" + equipmentTypeID + "','" + status + "')"; ;
                                            cmd.ExecuteNonQuery();
                                        }
                                    }
                                }
                            }
                            //设置界面
                            SetText("label" + equipmentID, showResult);
                        } catch (Exception e) {
                            Console.WriteLine(e.Message);
                        }
                    }
                    //间隔5分钟采集一次数据
                    Thread.Sleep(300000);
                } catch (Exception e) {
                    Console.WriteLine(e.Message);
                }
            }
        }

        //电度
        private void DianDu(Object com) {
            string[] parameters = com.ToString().Split(',');
            SerialPort serialPort = new SerialPort();
            serialPort.PortName = parameters[0];
            serialPort.BaudRate = Convert.ToInt32(parameters[1]);
            serialPort.DataBits = Convert.ToInt32(parameters[2]);
            serialPort.Parity = (Parity)Convert.ToInt32(parameters[3]);
            serialPort.StopBits = (StopBits)Convert.ToInt32(parameters[4]);

            int startNo = Convert.ToInt32(parameters[5]);
            int num = Convert.ToInt32(parameters[6]);

            while (true) {
                //保证循环不退出
                try {
                    if (!serialPort.IsOpen) {
                        serialPort.Open();
                    }
                    for (int i = 1; i <= num; i++) {
                        try {
                            //设备ID，数据库中设置好的数值
                            int equipmentID = startNo + i;
                            //查询有功电度
                            string orderWithoutCrc = string.Format("{0:X2}", i) + "03004a0002";
                            byte[] bufferS = SoftCRC16.CRC16(SoftBasic.HexStringToBytes(orderWithoutCrc));
                            serialPort.Write(bufferS, 0, bufferS.Length);
                            //将发送的数据显示在窗体相应的位置
                            SetText("textBox1", parameters[0] + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>" + SoftBasic.ByteToHexString(bufferS) + "\n");
                            //等待数据
                            Thread.Sleep(500);
                            //接收数据
                            byte[] bufferR = null;
                            if (serialPort.BytesToRead > 0) {
                                bufferR = new byte[serialPort.BytesToRead];
                                serialPort.Read(bufferR, 0, bufferR.Length);
                            }
                            //将接收的数据显示在窗体相应的位置，没有回应数据显示N/A。
                            SetText("textBox2", parameters[0] + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=" + ((bufferR is null) ? "N/A" : SoftBasic.ByteToHexString(bufferR)) + "\n");
                            string showResult = null;//界面显示的结果
                            if (bufferR is null) {
                                showResult = "N/A";//设备不可用
                            } else if (!SoftCRC16.CheckCRC16(bufferR)) {
                                showResult = "ERROR";//返回数据错误
                            } else {
                                //解析数据
                                ReverseBytesTransform transform = new ReverseBytesTransform();//数据转换工具
                                transform.DataFormat = DataFormat.BADC;//低位在前
                                double data = (double)transform.TransUInt32(bufferR, 3)/(double)10.0;
                                showResult = data.ToString("0.0");//显示有功电度
                                //保存到数据库
                                using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MYSQL"].ConnectionString)) {
                                    conn.Open();
                                    using (MySqlCommand cmd = new MySqlCommand("insert into tb_equipmentparamrecord_10012 (id,equipmentid,paramID,recordTime,value,recorder,equipmentTypeID) values('" + Guid.NewGuid().ToString("N") + "','" + equipmentID + "','60001','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + data + "','仪表采集','10012')", conn)) {
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                            //设置界面
                            SetText("label" + equipmentID+"yg", showResult);

                            //查询无功电度
                            orderWithoutCrc = string.Format("{0:X2}", i) + "03004c0002";
                            bufferS = SoftCRC16.CRC16(SoftBasic.HexStringToBytes(orderWithoutCrc));
                            serialPort.Write(bufferS, 0, bufferS.Length);
                            //将发送的数据显示在窗体相应的位置
                            SetText("textBox1", parameters[0] + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>" + SoftBasic.ByteToHexString(bufferS) + "\n");
                            //等待数据
                            Thread.Sleep(500);
                            //接收数据。
                            bufferR = null;
                            if (serialPort.BytesToRead > 0) {
                                bufferR = new byte[serialPort.BytesToRead];
                                serialPort.Read(bufferR, 0, bufferR.Length);
                            }
                            //将接收的数据显示在窗体相应的位置，没有回应数据显示N/A。
                            SetText("textBox2", parameters[0] + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=" + ((bufferR is null) ? "N/A" : SoftBasic.ByteToHexString(bufferR)) + "\n");
                            //数据解析，在界面上显示结果，并存储到数据库
                            showResult = null;//界面显示的结果
                            if (bufferR is null) {
                                showResult = "N/A";//设备不可用
                            } else if (!SoftCRC16.CheckCRC16(bufferR)) {
                                showResult = "ERROR";//返回数据错误
                            } else {
                                //解析数据
                                ReverseBytesTransform transform = new ReverseBytesTransform();//数据转换工具
                                transform.DataFormat = DataFormat.BADC;
                                double data = (double)transform.TransUInt32(bufferR, 3) / (double)10.0;
                                showResult = data.ToString("0.0");//显示无功电度
                                //保存到数据库
                                using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MYSQL"].ConnectionString)) {
                                    conn.Open();
                                    using (MySqlCommand cmd = new MySqlCommand("insert into tb_equipmentparamrecord_10012 (id,equipmentid,paramID,recordTime,value,recorder,equipmentTypeID) values('" + Guid.NewGuid().ToString("N") + "','" + equipmentID + "','60002','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + data + "','仪表采集','10012')", conn)) {
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                            //设置界面
                            SetText("label" + equipmentID+"wg", showResult);
                        } catch (Exception e) {
                            Console.WriteLine(e.Message);
                        }
                    }
                    //间隔5分钟采集一次数据
                    Thread.Sleep(300000);
                } catch (Exception e) {
                    Console.WriteLine(e.Message);
                }
            }
        }

        /*
         * 子线程设置主界面控件
         **/
        private delegate void SetTextCallback(string name, string text);
        private void SetText(string name, string text) {
            Control c = Controls.Find(name, true)[0];
            if (c.InvokeRequired) {
                SetTextCallback setTextCallback = new SetTextCallback(SetText);
                c.Invoke(setTextCallback, new object[] { name, text });
            } else {
                if (c is TextBox) {
                    TextBox textBox = (TextBox)c;
                    if (textBox.TextLength > 5000) {
                        textBox.Clear();
                    }
                    textBox.AppendText(text);
                    textBox.Refresh();
                }
                if (c is Label) {
                    ((Label)c).Text = text;
                }
            }
        }
    }
}
