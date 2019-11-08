using System;
using System.Configuration;
using System.Windows.Forms;
using System.Threading;
using System.IO.Ports;
using HslCommunication;
using HslCommunication.Serial;
using HslCommunication.BasicFramework;
using HslCommunication.Core;
using HslCommunication.Profinet.Siemens;
using MySql.Data.MySqlClient;
using log4net;
using NDde.Client;

namespace TNRPC {
    public partial class FormMain : Form {
        ILog log = log4net.LogManager.GetLogger("testApp.Logging");
        Random rm = new Random();
        public FormMain() {
            InitializeComponent();
        }
        private void FormMain_FormClosing(object sender, FormClosingEventArgs e) {
            if (MessageBox.Show("退出实时数据采集系统吗?", "退出", MessageBoxButtons.YesNo, MessageBoxIcon.Error, MessageBoxDefaultButton.Button2) == DialogResult.No) e.Cancel = true;
        }
        private void FormMain_Shown(object sender, EventArgs e) {
            string used = ConfigurationManager.AppSettings["sbcdsc"];
            if (used != null && used.Length > 0) {
                string[] coms = used.Split(',');
                foreach (string com in coms) {
                    Thread worker = new Thread(new ParameterizedThreadStart(sbcdsc));
                    worker.IsBackground = true;
                    worker.Start(ConfigurationManager.AppSettings[com]);
                    log.Info(DateTime.Now.ToString() + "_start sbcdsc thread_" + com);
                }
            }
            used = ConfigurationManager.AppSettings["sbzndb"];
            if (used != null && used.Length > 0) {
                string[] coms = used.Split(',');
                foreach (string com in coms) {
                    Thread worker = new Thread(new ParameterizedThreadStart(sbzndb));
                    worker.IsBackground = true;
                    worker.Start(ConfigurationManager.AppSettings[com]);
                    log.Info(DateTime.Now.ToString() + "_start sbzndb thread_" + com);
                }
            }
            used = ConfigurationManager.AppSettings["sbgh"];
            if (used != null && used.Length > 0) {
                Thread worker = new Thread(new ParameterizedThreadStart(sbgh));
                worker.IsBackground = true;
                worker.Start(used);
                log.Info(DateTime.Now.ToString() + "_start sbgh thread.");
            }
            used = ConfigurationManager.AppSettings["ebgh"];
            if (used != null && used.Length > 0) {
                Thread worker = new Thread(new ParameterizedThreadStart(ebgh));
                worker.IsBackground = true;
                worker.Start(used);
                log.Info(DateTime.Now.ToString() + "_start ebgh thread.");
            }
            used = ConfigurationManager.AppSettings["ebhg"];
            if (used != null && used.Length > 0) {
                Thread worker = new Thread(new ParameterizedThreadStart(ebhg));
                worker.IsBackground = true;
                worker.Start(used);
                log.Info(DateTime.Now.ToString() + "_start ebhg thread.");
            }
            used = ConfigurationManager.AppSettings["cs"];
            if (used != null && used.Length > 0) {
                string[] coms = used.Split(',');
                foreach (string com in coms) {
                    Thread worker = new Thread(new ParameterizedThreadStart(cs));
                    worker.IsBackground = true;
                    worker.Start(ConfigurationManager.AppSettings[com]);
                    log.Info(DateTime.Now.ToString() + "_start cs thread_" + com);
                }
            }
        }
        private void sbcdsc(Object com) {
            string[] parameters = com.ToString().Split(',');
            string paramID = "50001";
            string equipmentTypeID = "3";
            string sendTextBox = "textBox16";
            string recvTextBox = "textBox15";
            SerialPort serialPort = new SerialPort(parameters[0], Convert.ToInt32(parameters[1]), (Parity)Convert.ToInt32(parameters[3]), Convert.ToInt32(parameters[2]), (StopBits)Convert.ToInt32(parameters[4]));
            int startNo = Convert.ToInt32(parameters[5]);
            int num = Convert.ToInt32(parameters[6]);
            while (true) {
                try {
                    if (!serialPort.IsOpen) serialPort.Open();
                    for (int i = 1; i <= num; i++) {
                        int equipmentID = startNo + i;
                        string orderWithoutCrc = string.Format("{0:X2}", i) + "0310010001";
                        byte[] bufferS = SoftCRC16.CRC16(SoftBasic.HexStringToBytes(orderWithoutCrc));
                        serialPort.Write(bufferS, 0, bufferS.Length);
                        SetText(sendTextBox, parameters[0] + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>" + SoftBasic.ByteToHexString(bufferS) + "\n");
                        Thread.Sleep(500);
                        byte[] bufferR = null;
                        if (serialPort.BytesToRead > 0) {
                            bufferR = new byte[serialPort.BytesToRead];
                            serialPort.Read(bufferR, 0, bufferR.Length);
                        }
                        SetText(recvTextBox, parameters[0] + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=" + ((bufferR is null) ? "N/A" : SoftBasic.ByteToHexString(bufferR)) + "\n");
                        string showResult = null;
                        if (bufferR is null) {
                            showResult = "N/A";
                        } else if (!SoftCRC16.CheckCRC16(bufferR)) {
                            showResult = "ERROR";
                        } else {
                            ReverseBytesTransform transform = new ReverseBytesTransform();
                            float data = (float)transform.TransInt16(bufferR, 3) / 10;
                            if (data > 100 || data < 0) {
                                showResult = "ERROR";
                            } else {
                                showResult = data.ToString("f1") + "℃";
                                using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MYSQL"].ConnectionString)) {
                                    conn.Open();
                                    string status = "2";
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
                                        cmd.CommandText = "insert into tb_equipmentparamrecord (id,equipmentid,paramID,recordTime,value,recorder,equipmentTypeID,status) values('" + Guid.NewGuid().ToString("N") + "','" + equipmentID + "','" + paramID + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + data + "','仪表采集','" + equipmentTypeID + "','" + status + "')"; ;
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                        }
                        SetText(recvTextBox, "返回数据:" + showResult + "\n");
                    }
                    Thread.Sleep(270000 + rm.Next(60000));
                } catch (Exception e) {
                    log.Error(DateTime.Now.ToString() + e.Message);
                    Thread.Sleep(10000);
                }
            }
        }

        private void sbzndb(Object com) {
            string[] parameters = com.ToString().Split(',');
            SerialPort serialPort = new SerialPort(parameters[0], Convert.ToInt32(parameters[1]), (Parity)Convert.ToInt32(parameters[3]), Convert.ToInt32(parameters[2]), (StopBits)Convert.ToInt32(parameters[4]));
            int startNo = Convert.ToInt32(parameters[5]);
            int num = Convert.ToInt32(parameters[6]);
            string[] queryTimes = { "0:00", "8:00", "12:00", "18:00", "22:00" };
            double[][] queryData = { new double[num], new double[num], new double[num], new double[num], new double[num], new double[num] };
            while (true) {
                try {
                    if (!serialPort.IsOpen) serialPort.Open();
                    string now = DateTime.Now.ToShortTimeString();
                    int index = Array.IndexOf(queryTimes, now);
                    if (index > -1) {
                        for (int i = 1; i <= num; i++) {
                            int equipmentID = startNo + i;
                            string orderWithoutCrc = string.Format("{0:X2}", i) + "03004a0002";
                            byte[] bufferS = SoftCRC16.CRC16(SoftBasic.HexStringToBytes(orderWithoutCrc));
                            for (int j = 0; j < 2; j++) {
                                serialPort.Write(bufferS, 0, bufferS.Length);
                                SetText("textBox4", parameters[0] + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>" + SoftBasic.ByteToHexString(bufferS) + "\n");
                                Thread.Sleep(1000);
                                byte[] bufferR = null;
                                if (serialPort.BytesToRead > 0) {
                                    bufferR = new byte[serialPort.BytesToRead];
                                    serialPort.Read(bufferR, 0, bufferR.Length);
                                }
                                SetText("textBox3", parameters[0] + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=" + ((bufferR is null) ? "N/A" : SoftBasic.ByteToHexString(bufferR)) + "\n");
                                if (bufferR is null || !SoftCRC16.CheckCRC16(bufferR)) {
                                    Thread.Sleep(10000);
                                    continue;
                                } else {
                                    ReverseBytesTransform transform = new ReverseBytesTransform();
                                    transform.DataFormat = DataFormat.BADC;
                                    double data = (double)transform.TransUInt32(bufferR, 3) / (double)10.0;
                                    queryData[index][i - 1] = data;
                                    SetText("textBox3", "返回数据:" + data.ToString("0") + ".\n");
                                    break;
                                }
                            }
                            if (index == 0) {
                                double[] jfpg = { queryData[4][i - 1] - queryData[3][i - 1], queryData[2][i - 1] - queryData[1][i - 1], queryData[3][i - 1] - queryData[2][i - 1] + queryData[0][i - 1] - queryData[4][i - 1], queryData[1][i - 1] - queryData[5][i - 1] };
                                string[] paramCode = { "60003", "60004", "60005", "60006" };
                                string[] labelCode = { "j", "f", "p", "g" };
                                queryData[5][i - 1] = queryData[0][i - 1];
                                queryData[0][i - 1] = 0;
                                queryData[1][i - 1] = 0;
                                queryData[2][i - 1] = 0;
                                queryData[3][i - 1] = 0;
                                queryData[4][i - 1] = 0;
                                using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MYSQL"].ConnectionString)) {
                                    conn.Open();
                                    using (MySqlCommand cmd = new MySqlCommand("", conn)) {
                                        for (int k = 0; k <= 3; k++) {
                                            if (jfpg[k] >= 0 && jfpg[k] < 20000.0) {
                                                cmd.CommandText = "insert into tb_equipmentparamrecord_10012 (id,equipmentid,paramID,recordTime,value,recorder,equipmentTypeID) values('" + Guid.NewGuid().ToString("N") + "', '" + equipmentID + "', '" + paramCode[k] + "', '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "', '" + jfpg[k].ToString("0.0") + "', '仪表采集', '10012')";
                                                cmd.ExecuteNonQuery();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        Thread.Sleep(5000000);
                    } else {
                        Thread.Sleep(40000);
                    }
                } catch (Exception e) {
                    log.Error(DateTime.Now.ToString() + e.Message);
                    Thread.Sleep(10000);
                }
            }
        }

        private void sbgh(Object com) {
            string[] plcs = com.ToString().Split(',');
            while (true) {
                foreach (string plc in plcs) {
                    try {
                        DdeClient client = new DdeClient("PROSERVR", plc + ".PLC1");
                        client.Connect();
                        string[] parameters = ConfigurationManager.AppSettings[plc].Split(',');
                        SetText("textBox14", plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Temperat.\n");
                        string strWendu = client.Request("wendu", 60000);
                        SetText("textBox14", plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Humidity.\n");
                        string strShidu = client.Request("shidu", 60000);
                        double douWendu = Convert.ToDouble(strWendu.Substring(0, strWendu.IndexOf("\r"))) / 10.0;
                        SetText("textBox13", plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=Correct Return.\n");
                        double douShidu = Convert.ToDouble(strShidu.Substring(0, strShidu.IndexOf("\r"))) / 10.0;
                        SetText("textBox13", plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=Correct Return.\n");
                        using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MYSQL"].ConnectionString)) {
                            conn.Open();
                            using (MySqlCommand cmd = new MySqlCommand("insert into tb_equipmentparamrecord_10016 (id,equipmentid,paramID,recordTime,value,recorder,equipmentTypeID) values('" + Guid.NewGuid().ToString("N") + "','" + parameters[0] + "','70001','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + douWendu.ToString("0.0") + "','仪表采集','10016')", conn)) {
                                cmd.ExecuteNonQuery();
                                cmd.CommandText = "insert into tb_equipmentparamrecord_10016 (id,equipmentid,paramID,recordTime,value,recorder,equipmentTypeID) values('" + Guid.NewGuid().ToString("N") + "','" + parameters[0] + "','70002','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + douShidu.ToString("0.0") + "','仪表采集','10016')";
                                cmd.ExecuteNonQuery();
                            }
                        }
                    } catch (Exception ee) {
                        log.Error(DateTime.Now.ToString() + ee.Message);
                        Thread.Sleep(10000);
                    }
                }
                Thread.Sleep(300000);
            }
        }

        private void ebgh(Object com) {
            string[] plcs = com.ToString().Split(',');
            while (true) {
                foreach (string plc in plcs) {
                    try {
                        string[] parameters = ConfigurationManager.AppSettings[plc].Split(',');
                        SiemensS7Net siemens = new SiemensS7Net(SiemensPLCS.S200, parameters[1]) {
                            ConnectTimeOut = 5000
                        };
                        OperateResult connect = siemens.ConnectServer();
                        if (connect.IsSuccess) {
                            SetText("textBox10", plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Temperat.\n");
                            double douWendu = siemens.ReadInt16("DB1.1200").Content / 10.0;
                            SetText("textBox9", plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=返回温度:" + douWendu.ToString("0.0") + "℃.\n");
                            double douShidu = siemens.ReadInt16("DB1.1204").Content / 10.0;
                            if (!plc.Contains("_3")) {
                                SetText("textBox10", plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Humidity.\n");
                                SetText("textBox9", plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=返回湿度:" + douShidu.ToString("0.0") + "%.\n");
                            }
                            using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MYSQL"].ConnectionString)) {
                                conn.Open();
                                using (MySqlCommand cmd = new MySqlCommand("insert into tb_equipmentparamrecord_10016 (id,equipmentid,paramID,recordTime,value,recorder,equipmentTypeID) values('" + Guid.NewGuid().ToString("N") + "','" + parameters[0] + "','70001','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + douWendu.ToString("0.0") + "','仪表采集','10016')", conn)) {
                                    cmd.ExecuteNonQuery();
                                    if (!plc.Contains("_3")) {
                                        cmd.CommandText = "insert into tb_equipmentparamrecord_10016 (id,equipmentid,paramID,recordTime,value,recorder,equipmentTypeID) values('" + Guid.NewGuid().ToString("N") + "','" + parameters[0] + "','70002','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + douShidu.ToString("0.0") + "','仪表采集','10016')";
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                        } else {
                            SetText("textBox10", plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>无法连接" + parameters[1] + ".\n");
                        }
                        siemens.ConnectClose();
                    } catch (Exception ee) {
                        log.Error(DateTime.Now.ToString() + ee.Message);
                    }
                }
                Thread.Sleep(300000);
            }
        }

        private void ebhg(Object com) {
            string[] plcs = com.ToString().Split(',');
            while (true) {
                foreach (string plc in plcs) {
                    try {
                        string[] parameters = ConfigurationManager.AppSettings[plc].Split(',');
                        SiemensS7Net siemens = new SiemensS7Net(SiemensPLCS.S200, parameters[1]) {
                            ConnectTimeOut = 5000
                        };
                        OperateResult connect = siemens.ConnectServer();
                        if (connect.IsSuccess) {
                            SetText("textBox8", plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Temperat.\n");
                            double douWendu = siemens.ReadInt16("DB1.2204").Content / 10.0;
                            SetText("textBox7", plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=合膏温度:" + douWendu.ToString("0.0") + "℃.\n");
                            double douShidu = siemens.ReadInt16("DB1.2216").Content / 10.0;
                            SetText("textBox8", plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Water Temp.\n");
                            SetText("textBox7", plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=冷水水温:" + douShidu.ToString("0.0") + "℃.\n");
                            using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MYSQL"].ConnectionString)) {
                                conn.Open();
                                using (MySqlCommand cmd = new MySqlCommand("insert into tb_equipmentparamrecord_10017 (id,equipmentid,paramID,recordTime,value,recorder,equipmentTypeID) values('" + Guid.NewGuid().ToString("N") + "','" + parameters[0] + "','30002','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + douWendu.ToString("0.0") + "','仪表采集','10017')", conn)) {
                                    cmd.ExecuteNonQuery();
                                    cmd.CommandText = "insert into tb_equipmentparamrecord_10017 (id,equipmentid,paramID,recordTime,value,recorder,equipmentTypeID) values('" + Guid.NewGuid().ToString("N") + "','" + parameters[0] + "','30003','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + douShidu.ToString("0.0") + "','仪表采集','10017')";
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        } else {
                            SetText("textBox8", plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>无法连接" + parameters[1] + ".\n");
                        }
                        siemens.ConnectClose();
                    } catch (Exception ee) {
                        log.Error(DateTime.Now.ToString() + ee.Message);
                    }
                }
                Thread.Sleep(300000);
            }
        }

        private void cs(Object com) {
            string[] parameters = com.ToString().Split(',');
            SerialPort serialPort = new SerialPort(parameters[0], Convert.ToInt32(parameters[1]), (Parity)Convert.ToInt32(parameters[3]), Convert.ToInt32(parameters[2]), (StopBits)Convert.ToInt32(parameters[4]));
            int startNo = Convert.ToInt32(parameters[5]);
            int num = Convert.ToInt32(parameters[6]);
            while (true) {
                try {
                    if (!serialPort.IsOpen) serialPort.Open();
                    for (int i = 1; i <= num; i++) {
                        int equipmentID = startNo + i;
                        string orderWithoutCrc = string.Format("{0:X2}", i) + "03004a0002";
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
                        if (bufferR != null) {
                            ReverseBytesTransform transform = new ReverseBytesTransform();
                            transform.DataFormat = DataFormat.BADC;
                            double data = (double)transform.TransUInt32(bufferR, 3) / (double)10.0;
                            SetText("textBox2", "返回数据:" + data.ToString("0") + "\n");
                        }
                    }
                    Thread.Sleep(30000);
                } catch (Exception e) {
                    log.Error(DateTime.Now.ToString() + e.Message);
                }
            }
        }

        private delegate void SetTextCallback(string name, string text);
        private void SetText(string name, string text) {
            Control c = Controls.Find(name, true)[0];
            if (c.InvokeRequired) {
                SetTextCallback setTextCallback = new SetTextCallback(SetText);
                c.Invoke(setTextCallback, new object[] { name, text });
            } else {
                if (c is TextBox) {
                    TextBox textBox = (TextBox)c;
                    if (textBox.TextLength > 10000) {
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