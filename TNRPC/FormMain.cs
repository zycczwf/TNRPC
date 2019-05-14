using System;
using System.Configuration;
using System.Windows.Forms;
using System.Threading;
using System.IO.Ports;
using HslCommunication.Serial;
using HslCommunication.BasicFramework;
using HslCommunication.Core;
using MySql.Data.MySqlClient;
using log4net;
using NDde.Client;

namespace TNRPC {
    public partial class FormMain : Form {

        ILog log = log4net.LogManager.GetLogger("testApp.Logging");
        Random rm = new Random(1);

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
                    log.Info(DateTime.Now.ToString() + "_start WenDu thread_" + com);
                }
            }
            //电度
            used = ConfigurationManager.AppSettings["DIANDU"];
            if (used != null && used.Length > 0) {
                string[] coms = used.Split(',');
                foreach (string com in coms) {
                    Thread worker = new Thread(new ParameterizedThreadStart(JFPG));
                    //随主线程退出而退出
                    worker.IsBackground = true;
                    worker.Start(ConfigurationManager.AppSettings[com]);
                    log.Info(DateTime.Now.ToString() + "_start JFPG thread_" + com);
                }
            }
            //固化室
            used = ConfigurationManager.AppSettings["GUHUA"];
            if (used != null && used.Length > 0) {
                string[] plcs = used.Split(',');
                foreach (string plc in plcs) {
                    DdeClient client = new DdeClient("PROSERVR", plc + ".PLC1");
                    try {
                        client.Connect();
                    } catch (Exception ee) {
                        log.Error(DateTime.Now.ToString() + ee.Message);
                    }
                    client.Advise += SBGH;
                    client.StartAdvise("wendu", 1, true, 60000);
                    client.StartAdvise("shidu", 1, true, 60000);
                    log.Info(DateTime.Now.ToString() + "_start SBGH thread_" + plc);
                }
            }
        }

        private enum PROCESS {
            CDWD,//充电温度
            GZWD //干燥温度
        }

        //温度
        private void WenDu(Object com) {
            Thread.Sleep(110000);
            string[] parameters = com.ToString().Split(',');
            //根据参数判断是什么工艺参数
            string process = null;
            string paramID = null;//tb_parameterinfo
            string equipmentTypeID = null;//tb_equipmenttype
            switch (parameters[0]) {
                case "COM104":
                case "COM105":
                case "COM106":
                case "COM107":
                case "COM108":
                    process = PROCESS.CDWD.ToString();
                    paramID = "50001";
                    equipmentTypeID = "3";
                    break;
                case "COM109":
                case "COM110":
                case "COM111":
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
                            log.Error(DateTime.Now.ToString() + e.Message);
                            Thread.Sleep(10000);
                        }
                    }
                    //间隔5分钟左右采集一次数据
                    Thread.Sleep(270000 + rm.Next(60000));
                } catch (Exception e) {
                    log.Error(DateTime.Now.ToString() + e.Message);
                    Thread.Sleep(10000);
                }
            }
        }

        //电度
        private void DianDu(Object com) {
            Thread.Sleep(120000);
            string[] parameters = com.ToString().Split(',');
            SerialPort serialPort = new SerialPort();
            serialPort.PortName = parameters[0];
            serialPort.BaudRate = Convert.ToInt32(parameters[1]);
            serialPort.DataBits = Convert.ToInt32(parameters[2]);
            serialPort.Parity = (Parity)Convert.ToInt32(parameters[3]);
            serialPort.StopBits = (StopBits)Convert.ToInt32(parameters[4]);

            int startNo = Convert.ToInt32(parameters[5]);
            int num = Convert.ToInt32(parameters[6]);

            string[] order = { "03004a0002", "03004c0002" };
            string[] postfix = { "yg", "wg" };
            string[] paramID = { "60001", "60002" };

            while (true) {
                try {
                    if (!serialPort.IsOpen) {
                        serialPort.Open();
                    }
                    for (int i = 1; i <= num; i++) {
                        try {
                            int equipmentID = startNo + i;
                            for (int m = 0; m <= 1; m++) {
                                string orderWithoutCrc = string.Format("{0:X2}", i) + order[m];
                                byte[] bufferS = SoftCRC16.CRC16(SoftBasic.HexStringToBytes(orderWithoutCrc));
                                serialPort.Write(bufferS, 0, bufferS.Length);
                                SetText("textBox1", parameters[0] + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>" + SoftBasic.ByteToHexString(bufferS) + "\n");
                                Thread.Sleep(500);
                                byte[] bufferR = null;
                                if (serialPort.BytesToRead > 0) {
                                    bufferR = new byte[serialPort.BytesToRead];
                                    serialPort.Read(bufferR, 0, bufferR.Length);
                                }
                                SetText("textBox2", parameters[0] + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=" + ((bufferR is null) ? "N/A" : SoftBasic.ByteToHexString(bufferR)) + "\n");
                                string showResult = null;
                                if (bufferR is null) {
                                    showResult = "N/A";
                                } else if (!SoftCRC16.CheckCRC16(bufferR)) {
                                    showResult = "ERROR";
                                } else {
                                    ReverseBytesTransform transform = new ReverseBytesTransform();
                                    transform.DataFormat = DataFormat.BADC;
                                    double data = (double)transform.TransUInt32(bufferR, 3) / (double)10.0;
                                    showResult = data.ToString("0.0");
                                    using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MYSQL"].ConnectionString)) {
                                        conn.Open();
                                        using (MySqlCommand cmd = new MySqlCommand("insert into tb_equipmentparamrecord_10012 (id,equipmentid,paramID,recordTime,value,recorder,equipmentTypeID) values('" + Guid.NewGuid().ToString("N") + "','" + equipmentID + "','" + paramID[m] + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + data + "','仪表采集','10012')", conn)) {
                                            cmd.ExecuteNonQuery();
                                        }
                                    }
                                }
                                SetText("label" + equipmentID + postfix[m], showResult);
                            }
                        } catch (Exception e) {
                            log.Error(DateTime.Now.ToString() + e.Message);
                            Thread.Sleep(10000);
                        }
                    }
                    Thread.Sleep(270000 + rm.Next(60000));
                } catch (Exception e) {
                    log.Error(DateTime.Now.ToString() + e.Message);
                    Thread.Sleep(10000);
                }
            }
        }

        //固化
        private void SBGH(object sender, DdeAdviseEventArgs args) {
            string topic = ((DdeClient)sender).Topic;
            string plc = topic.Substring(0, topic.IndexOf("."));
            string[] parameters = ConfigurationManager.AppSettings[plc].Split(',');
            string strData = args.Text.Substring(0, args.Text.IndexOf("\r"));
            Console.WriteLine(strData);
            //double douData = Convert.ToDouble(strData) / 10.0;
            //            if (args.Item.Equals("wendu")) {
            //
            //              SetText("textBox4", plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Temperat.\n");
            //            SetText("textBox3", plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=Correct Return.\n");
            //          SetText("label" + parameters[0] + "wd", swendu);
            //    } else {
            //      string sshidu = args.Text.Substring(0, args.Text.IndexOf("\r"));
            //    SetText("textBox4", plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Humidity.\n");
            //  SetText("textBox3", plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=Correct Return.\n");
            //SetText("label" + parameters[0] + "sd", sshidu);
            // }

            //            using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MYSQL"].ConnectionString)) {
            //                conn.Open();
            //                using (MySqlCommand cmd = new MySqlCommand("insert into tb_equipmentparamrecord_10016 (id,equipmentid,paramID,recordTime,value,recorder,equipmentTypeID) values('" + Guid.NewGuid().ToString("N") + "','" + parameters[1] + "','70001','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + wendu + "','仪表采集','10016')", conn)) {
            //                    cmd.ExecuteNonQuery();
            //                    cmd.CommandText = "insert into tb_equipmentparamrecord_10016 (id,equipmentid,paramID,recordTime,value,recorder,equipmentTypeID) values('" + Guid.NewGuid().ToString("N") + "','" + parameters[1] + "','70002','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + shidu + "','仪表采集','10016')";
            //                   cmd.ExecuteNonQuery();
            //               }
            //          }

        }

        //统计每天尖峰平谷有功电量
        private void JFPG(Object com) {
            Thread.Sleep(130000);
            string[] parameters = com.ToString().Split(',');
            SerialPort serialPort = new SerialPort();
            serialPort.PortName = parameters[0];
            serialPort.BaudRate = Convert.ToInt32(parameters[1]);
            serialPort.DataBits = Convert.ToInt32(parameters[2]);
            serialPort.Parity = (Parity)Convert.ToInt32(parameters[3]);
            serialPort.StopBits = (StopBits)Convert.ToInt32(parameters[4]);

            int startNo = Convert.ToInt32(parameters[5]);
            int num = Convert.ToInt32(parameters[6]);

            //查询时间及查询数据
            string[] queryTimes = { "0:00", "8:00", "12:00", "18:00", "22:00" };
            double[][] queryData = { new double[num], new double[num], new double[num], new double[num], new double[num], new double[num] };

            while (true) {
                try {
                    if (!serialPort.IsOpen) {
                        serialPort.Open();
                    }
                    //当前系统时间
                    string now = DateTime.Now.ToShortTimeString();
                    int index = Array.IndexOf(queryTimes, now);
                    if (index > -1) {
                        for (int i = 1; i <= num; i++) {
                            int equipmentID = startNo + i;
                            string orderWithoutCrc = string.Format("{0:X2}", i) + "03004a0002";
                            byte[] bufferS = SoftCRC16.CRC16(SoftBasic.HexStringToBytes(orderWithoutCrc));

                            //查询仪表数据，共7次机会
                            for (int j = 0; j < 7; j++) {
                                try {
                                    serialPort.Write(bufferS, 0, bufferS.Length);
                                    SetText("textBox1", parameters[0] + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>" + SoftBasic.ByteToHexString(bufferS) + "\n");
                                    Thread.Sleep(500);
                                    byte[] bufferR = null;
                                    if (serialPort.BytesToRead > 0) {
                                        bufferR = new byte[serialPort.BytesToRead];
                                        serialPort.Read(bufferR, 0, bufferR.Length);
                                    }
                                    SetText("textBox2", parameters[0] + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=" + ((bufferR is null) ? "N/A" : SoftBasic.ByteToHexString(bufferR)) + "\n");
                                    if (bufferR is null || !SoftCRC16.CheckCRC16(bufferR)) {
                                        Thread.Sleep(10000);
                                        continue;
                                    } else {
                                        ReverseBytesTransform transform = new ReverseBytesTransform();
                                        transform.DataFormat = DataFormat.BADC;
                                        double data = (double)transform.TransUInt32(bufferR, 3) / (double)10.0;
                                        queryData[index][i - 1] = data;
                                        break;
                                    }
                                } catch (Exception e) {
                                    log.Error(DateTime.Now.ToString() + e.Message);
                                    Thread.Sleep(10000);
                                }
                            }

                            //每天00:00数据更新
                            if (index == 0) {
                                double jian = queryData[4][i - 1] - queryData[3][i - 1];
                                double feng = queryData[2][i - 1] - queryData[1][i - 1];
                                double ping = queryData[3][i - 1] - queryData[2][i - 1] + queryData[0][i - 1] - queryData[4][i - 1];
                                double gu = queryData[1][i - 1] - queryData[5][i - 1];
                                queryData[5][i - 1] = queryData[0][i - 1];
                                using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MYSQL"].ConnectionString)) {
                                    conn.Open();
                                    using (MySqlCommand cmd = new MySqlCommand()) {
                                        cmd.Connection = conn;
                                        if (jian > 0 && jian < 1000000.0) {
                                            cmd.CommandText = "insert into tb_equipmentparamrecord_10012(id, equipmentid, paramID, recordTime, value, recorder, equipmentTypeID) values('" + Guid.NewGuid().ToString("N") + "', '" + equipmentID + "', '60003', '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "', '" + jian.ToString("0.0") + "', '仪表采集', '10012')";
                                            cmd.ExecuteNonQuery();
                                            SetText("label" + equipmentID + "j", jian.ToString("0.0"));
                                        } else {
                                            SetText("label" + equipmentID + "j", "N/A");
                                        }

                                        if (feng > 0 && feng < 1000000.0) {
                                            cmd.CommandText = "insert into tb_equipmentparamrecord_10012 (id,equipmentid,paramID,recordTime,value,recorder,equipmentTypeID) values('" + Guid.NewGuid().ToString("N") + "','" + equipmentID + "','60004','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + feng.ToString("0.0") + "','仪表采集','10012')";
                                            cmd.ExecuteNonQuery();
                                            SetText("label" + equipmentID + "f", feng.ToString("0.0"));
                                        } else {
                                            SetText("label" + equipmentID + "f", "N/A");
                                        }

                                        if (ping > 0 && ping < 1000000.0) {
                                            cmd.CommandText = "insert into tb_equipmentparamrecord_10012 (id,equipmentid,paramID,recordTime,value,recorder,equipmentTypeID) values('" + Guid.NewGuid().ToString("N") + "','" + equipmentID + "','60005','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + ping.ToString("0.0") + "','仪表采集','10012')";
                                            cmd.ExecuteNonQuery();
                                            SetText("label" + equipmentID + "p", ping.ToString("0.0"));
                                        } else {
                                            SetText("label" + equipmentID + "p", "N/A");
                                        }

                                        if (gu > 0 && gu < 1000000.0) {
                                            cmd.CommandText = "insert into tb_equipmentparamrecord_10012 (id,equipmentid,paramID,recordTime,value,recorder,equipmentTypeID) values('" + Guid.NewGuid().ToString("N") + "','" + equipmentID + "','60006','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + gu.ToString("0.0") + "','仪表采集','10012')";
                                            cmd.ExecuteNonQuery();
                                            SetText("label" + equipmentID + "g", gu.ToString("0.0"));
                                        } else {
                                            SetText("label" + equipmentID + "g", "N/A");
                                        }
                                    }
                                }
                            }
                        }
                        //睡一觉
                        Thread.Sleep(5000000);
                    } else {
                        //眯一会儿
                        Thread.Sleep(50000);
                    }
                } catch (Exception e) {
                    log.Error(DateTime.Now.ToString() + e.Message);
                    Thread.Sleep(10000);
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
