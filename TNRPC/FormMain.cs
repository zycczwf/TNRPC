using System;
using System.Configuration;
using System.Windows.Forms;
using System.Threading;
using System.IO.Ports;
using System.Globalization;
using HslCommunication;
using HslCommunication.Core;
using HslCommunication.BasicFramework;
using HslCommunication.Serial;
using HslCommunication.ModBus;
using HslCommunication.Profinet.Siemens;
using HslCommunication.Profinet.Melsec;
using MySql.Data.MySqlClient;
using log4net;

namespace TNRPC {
    public partial class FormMain : Form {
        ILog log = log4net.LogManager.GetLogger("TNRPC.Logging");
        Warn warn = Warn.getInstance();
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
            used = ConfigurationManager.AppSettings["sbzndb2"];
            if (used != null && used.Length > 0) {
                string[] coms = used.Split(',');
                foreach (string com in coms) {
                    Thread worker = new Thread(new ParameterizedThreadStart(sbzndb2));
                    worker.IsBackground = true;
                    worker.Start(ConfigurationManager.AppSettings[com]);
                    log.Info(DateTime.Now.ToString() + "_start sbzndb2 thread_" + com);
                }
            }
            used = ConfigurationManager.AppSettings["sbgh"];
            if (used != null && used.Length > 0) {
                Thread worker = new Thread(new ParameterizedThreadStart(sbgh));
                worker.IsBackground = true;
                worker.Start(used);
                log.Info(DateTime.Now.ToString() + "_start sbgh thread.");
            }
            used = ConfigurationManager.AppSettings["sbhg"];
            if (used != null && used.Length > 0) {
                Thread worker = new Thread(new ParameterizedThreadStart(sbhg));
                worker.IsBackground = true;
                worker.Start(used);
                log.Info(DateTime.Now.ToString() + "_start sbhg thread.");
            }
            used = ConfigurationManager.AppSettings["sbqm"];
            if (used != null && used.Length > 0) {
                Thread worker = new Thread(new ParameterizedThreadStart(sbqm));
                worker.IsBackground = true;
                worker.Start(used);
                log.Info(DateTime.Now.ToString() + "_start sbqm thread.");
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
            used = ConfigurationManager.AppSettings["ybgh"];
            if (used != null && used.Length > 0) {
                Thread worker = new Thread(new ParameterizedThreadStart(ybgh));
                worker.IsBackground = true;
                worker.Start(used);
                log.Info(DateTime.Now.ToString() + "_start ybgh thread.");
            }
            used = ConfigurationManager.AppSettings["gybdz97"];
            if (used != null && used.Length > 0) {
                string[] coms = used.Split(',');
                foreach (string com in coms) {
                    Thread worker = new Thread(new ParameterizedThreadStart(gybdz97));
                    worker.IsBackground = true;
                    worker.Start(ConfigurationManager.AppSettings[com]);
                    log.Info(DateTime.Now.ToString() + "_start gybdz97 thread." + com);
                }
            }
            used = ConfigurationManager.AppSettings["gybdz07"];
            if (used != null && used.Length > 0) {
                string[] coms = used.Split(',');
                foreach (string com in coms) {
                    Thread worker = new Thread(new ParameterizedThreadStart(gybdz07));
                    worker.IsBackground = true;
                    worker.Start(ConfigurationManager.AppSettings[com]);
                    log.Info(DateTime.Now.ToString() + "_start gybdz07 thread." + com);
                }
            }
            used = ConfigurationManager.AppSettings["hbfj"];
            if (used != null && used.Length > 0) {
                Thread worker = new Thread(new ParameterizedThreadStart(hbfj));
                worker.IsBackground = true;
                worker.Start(used);
                log.Info(DateTime.Now.ToString() + "_start hbfj thread.");
            }
        }
        private void sbcdsc(Object com) {
            string sendTextBox = "textBox4";
            string recvTextBox = "textBox3";
            string[] parameters = com.ToString().Split(',');
            ModbusRtu rtu = new ModbusRtu();
            rtu.SerialPortInni(parameters[0], Convert.ToInt32(parameters[1]), Convert.ToInt32(parameters[2]), (StopBits)Convert.ToInt32(parameters[4]), (Parity)Convert.ToInt32(parameters[3]));
            while (true) {
                try {
                    if (!rtu.IsOpen()) {
                        rtu.Open();
                    }
                    for (int i = 1; i <= Convert.ToInt32(parameters[6]); i++) {
                        int equipmentID = Convert.ToInt32(parameters[5]) + i;
                        SetText(sendTextBox, parameters[0] + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>查询" + equipmentID + "温度\n");
                        double data = rtu.ReadInt16("s=" + i + ";4097").Content / 10.0;
                        if (data >= 100 || data <= 0) {
                            SetText(recvTextBox, equipmentID + "<=返回温度:ERROR!\n");
                            continue;
                        }
                        SetText(recvTextBox, equipmentID + "<=返回温度:" + data.ToString("0.0") + "℃\n");
                        using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MYSQL"].ConnectionString)) {
                            conn.Open();
                            string status = "2";
                            using (MySqlCommand cmd = new MySqlCommand("select max, min FROM tb_parameterinfo where id = '50001'", conn)) {
                                using (MySqlDataReader reader = cmd.ExecuteReader()) {
                                    if (reader.Read()) {
                                        if (data > reader.GetFloat("max")) {
                                            status = "3";
                                        } else if (data < reader.GetFloat("min")) {
                                            status = "1";
                                        }
                                    }
                                }
                                cmd.CommandText = "insert into tb_equipmentparamrecord_3 (id,equipmentid,paramID,recordTime,value,recorder,equipmentTypeID,status) values('" + Guid.NewGuid().ToString("N") + "','" + equipmentID + "','50001','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + data + "','仪表采集','3','" + status + "')";
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    Thread.Sleep(3600000);
                } catch (Exception e) {
                    rtu.Close();
                    log.Error(DateTime.Now.ToString() + e.Message);
                    Thread.Sleep(10000);
                }
            }
        }

        private void sbzndb(Object com) {
            string sendTextBox = "textBox26";
            string recvTextBox = "textBox25";
            string[] parameters = com.ToString().Split(',');
            SerialPort serialPort = new SerialPort(parameters[0], Convert.ToInt32(parameters[1]), (Parity)Convert.ToInt32(parameters[3]), Convert.ToInt32(parameters[2]), (StopBits)Convert.ToInt32(parameters[4]));
            int startNo = Convert.ToInt32(parameters[5]);
            int num = Convert.ToInt32(parameters[6]);
            string[] queryTimes = { "0:00", "1:00", "2:00", "3:00", "4:00", "5:00", "6:00", "7:00", "8:00", "9:00", "10:00", "11:00", "12:00", "13:00", "14:00", "15:00", "16:00", "17:00", "18:00", "19:00", "20:00", "21:00", "22:00", "23:00" };
            string[] columnNames = { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen", "twenty", "twentyone", "twentytwo", "twentythree" };
            while (true) {
                try {
                    if (!serialPort.IsOpen) serialPort.Open();
                    string now = DateTime.Now.ToShortTimeString();
                    int index = Array.IndexOf(queryTimes, now);
                    if (index > -1) {
                        if (index == 0) {
                            using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MYSQL"].ConnectionString)) {
                                conn.Open();
                                using (MySqlCommand cmd = new MySqlCommand("", conn)) {
                                    for (int i = 1; i <= num; i++) {
                                        int equipmentID = startNo + i;
                                        cmd.CommandText = "insert into tb_electricitymeterparametersacquisition_1003 (id,equipmentid,dayTime,remark,status) values('" + Guid.NewGuid().ToString("N") + "','" + equipmentID + "','" + DateTime.Now.ToString("yyyy-MM-dd") + "','仪表采集','1')";
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                        }
                        for (int i = 1; i <= num; i++) {
                            int equipmentID = startNo + i;
                            string orderWithoutCrc = string.Format("{0:X2}", i) + "03004a0002";
                            byte[] bufferS = SoftCRC16.CRC16(SoftBasic.HexStringToBytes(orderWithoutCrc));
                            for (int j = 0; j <= 2; j++) {
                                serialPort.Write(bufferS, 0, bufferS.Length);
                                SetText(sendTextBox, parameters[0] + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>" + SoftBasic.ByteToHexString(bufferS) + "\n");
                                Thread.Sleep(1000);
                                byte[] bufferR = null;
                                if (serialPort.BytesToRead > 0) {
                                    bufferR = new byte[serialPort.BytesToRead];
                                    serialPort.Read(bufferR, 0, bufferR.Length);
                                }
                                SetText(recvTextBox, parameters[0] + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=" + ((bufferR is null) ? "N/A" : SoftBasic.ByteToHexString(bufferR)) + "\n");
                                if (bufferR is null || !SoftCRC16.CheckCRC16(bufferR)) {
                                    Thread.Sleep(10000);
                                    continue;
                                } else {
                                    ReverseBytesTransform transform = new ReverseBytesTransform();
                                    transform.DataFormat = DataFormat.BADC;
                                    double data = (double)transform.TransUInt32(bufferR, 3) / (double)10.0;
                                    SetText(recvTextBox, "返回数据:" + data.ToString("0") + "\n");
                                    using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MYSQL"].ConnectionString)) {
                                        conn.Open();
                                        using (MySqlCommand cmd = new MySqlCommand("", conn)) {
                                            cmd.CommandText = "update tb_electricitymeterparametersacquisition_1003 set " + columnNames[index] + "=" + data.ToString("0") + " where equipmentid='" + equipmentID + "' and  dayTime='" + DateTime.Now.ToString("yyyy-MM-dd") + "'";
                                            cmd.ExecuteNonQuery();
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                        Thread.Sleep(2000000);
                    } else {
                        Thread.Sleep(40000);
                    }
                } catch (Exception e) {
                    log.Error(DateTime.Now.ToString() + e.Message);
                    Thread.Sleep(10000);
                }
            }
        }

        private void sbzndb2(Object com) {
            string sendTextBox = "textBox26";
            string recvTextBox = "textBox25";
            string[] parameters = com.ToString().Split(',');
            SerialPort serialPort = new SerialPort(parameters[0], Convert.ToInt32(parameters[1]), (Parity)Convert.ToInt32(parameters[3]), Convert.ToInt32(parameters[2]), (StopBits)Convert.ToInt32(parameters[4]));
            int startNo = Convert.ToInt32(parameters[5]);
            int num = Convert.ToInt32(parameters[6]);
            string[] queryTimes = { "0:00", "1:00", "2:00", "3:00", "4:00", "5:00", "6:00", "7:00", "8:00", "9:00", "10:00", "11:00", "12:00", "13:00", "14:00", "15:00", "16:00", "17:00", "18:00", "19:00", "20:00", "21:00", "22:00", "23:00" };
            string[] columnNames = { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen", "twenty", "twentyone", "twentytwo", "twentythree" };
            while (true) {
                try {
                    if (!serialPort.IsOpen) serialPort.Open();
                    string now = DateTime.Now.ToShortTimeString();
                    int index = Array.IndexOf(queryTimes, now);
                    if (index > -1) {
                        if (index == 0) {
                            using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MYSQL"].ConnectionString)) {
                                conn.Open();
                                using (MySqlCommand cmd = new MySqlCommand("", conn)) {
                                    for (int i = 1; i <= num; i++) {
                                        int equipmentID = startNo + i;
                                        cmd.CommandText = "insert into tb_electricitymeterparametersacquisition_1003 (id,equipmentid,dayTime,remark,status) values('" + Guid.NewGuid().ToString("N") + "','" + equipmentID + "','" + DateTime.Now.ToString("yyyy-MM-dd") + "','仪表采集','1')";
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                        }
                        for (int i = 1; i <= num; i++) {
                            int equipmentID = startNo + i;
                            string orderWithoutCrc = string.Format("{0:X2}", i) + "0400000002";
                            byte[] bufferS = SoftCRC16.CRC16(SoftBasic.HexStringToBytes(orderWithoutCrc));
                            for (int j = 0; j <= 2; j++) {
                                serialPort.Write(bufferS, 0, bufferS.Length);
                                SetText(sendTextBox, parameters[0] + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>" + SoftBasic.ByteToHexString(bufferS) + "\n");
                                Thread.Sleep(1000);
                                byte[] bufferR = null;
                                if (serialPort.BytesToRead > 0) {
                                    bufferR = new byte[serialPort.BytesToRead];
                                    serialPort.Read(bufferR, 0, bufferR.Length);
                                }
                                SetText(recvTextBox, parameters[0] + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=" + ((bufferR is null) ? "N/A" : SoftBasic.ByteToHexString(bufferR)) + "\n");
                                if (bufferR is null || !SoftCRC16.CheckCRC16(bufferR)) {
                                    Thread.Sleep(10000);
                                    continue;
                                } else {
                                    ReverseBytesTransform transform = new ReverseBytesTransform();
                                    double data = (double)transform.TransUInt32(bufferR, 3) / (double)100.0;
                                    SetText(recvTextBox, "返回数据:" + data.ToString() + "\n");
                                    using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MYSQL"].ConnectionString)) {
                                        conn.Open();
                                        using (MySqlCommand cmd = new MySqlCommand("", conn)) {
                                            cmd.CommandText = "update tb_electricitymeterparametersacquisition_1003 set " + columnNames[index] + "=" + data.ToString() + " where equipmentid='" + equipmentID + "' and  dayTime='" + DateTime.Now.ToString("yyyy-MM-dd") + "'";
                                            cmd.ExecuteNonQuery();
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                        Thread.Sleep(2000000);
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
            string sendTextBox = "textBox14";
            string recvTextBox = "textBox13";
            string[] plcs = com.ToString().Split(',');
            while (true) {
                foreach (string plc in plcs) {
                    SiemensS7Net siemens = null;
                    try {
                        string[] parameters = ConfigurationManager.AppSettings[plc].Split(',');
                        siemens = new SiemensS7Net(SiemensPLCS.S200, parameters[1]) { ConnectTimeOut = 5000 };
                        OperateResult connect = siemens.ConnectServer();
                        if (connect.IsSuccess) {
                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Temperat\n");
                            double douWendu = siemens.ReadInt16("DB1.1200").Content / 10.0;
                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query TempSet\n");
                            double douWenduSet = siemens.ReadInt16("DB1.1202").Content / 10.0;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=温度:" + douWendu.ToString("0.0") + "℃\n");
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=设置温度:" + douWenduSet.ToString("0.0") + "℃\n");
                            double douShidu = siemens.ReadInt16("DB1.1204").Content / 10.0;
                            double douShiduSet = siemens.ReadInt16("DB1.1206").Content / 10.0;
                            if (!plc.Contains("_3")) {
                                SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Humidity\n");
                                SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=湿度:" + douShidu.ToString("0.0") + "%\n");
                                SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query HumiSet\n");
                                SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=设置湿度:" + douShiduSet.ToString("0.0") + "%\n");
                            }
                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Hour\n");
                            int intHour = siemens.ReadInt32("DB1.322").Content;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=固化小时:" + intHour + "\n");
                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Minute\n");
                            int intMinute = siemens.ReadInt32("DB1.326").Content;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=固化分钟:" + intMinute + "\n");
                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Second\n");
                            int intSecond = siemens.ReadInt32("DB1.330").Content;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=固化秒:" + intSecond + "\n");
                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Status\n");
                            int intStatus = siemens.ReadInt16("DB1.220").Content;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=运行状态:" + intStatus + "\n");
                            using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MYSQL"].ConnectionString)) {
                                conn.Open();
                                using (MySqlCommand cmd = new MySqlCommand()) {
                                    cmd.Connection = conn;
                                    if (!plc.Contains("_3")) {
                                        cmd.CommandText = "insert into tb_solidificationoperatingparametersacquisition_1003 (id,equipmentID,acquisitionTime,remark,status,realtimeTemperature,settingTemperature,realtimeHumidity,settingHumidity,solidificationHour,solidificationMinute,solidificationSecond,runningStatus) values('"
                                            + Guid.NewGuid().ToString("N") + "','" + parameters[0] + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','仪表采集',1,'" + douWendu.ToString("0.0") + "','" + douWenduSet.ToString("0.0") + "','" + douShidu.ToString("0.0") + "','" + douShiduSet.ToString("0.0") + "','" + intHour + "','" + intMinute + "','" + intSecond + "','" + intStatus + "')";
                                    } else {
                                        cmd.CommandText = "insert into tb_solidificationoperatingparametersacquisition_1003 (id,equipmentID,acquisitionTime,remark,status,realtimeTemperature,settingTemperature,solidificationHour,solidificationMinute,solidificationSecond,runningStatus) values('"
                                            + Guid.NewGuid().ToString("N") + "','" + parameters[0] + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','仪表采集',1,'" + douWendu.ToString("0.0") + "','" + douWenduSet.ToString("0.0") + "','" + intHour + "','" + intMinute + "','" + intSecond + "','" + intStatus + "')";
                                    }
                                    cmd.ExecuteNonQuery();
                                    string[] parameterIds = { "70001", "70002", "70003", "70004", "70005", "70006", "70007", "70008" };
                                    double[] parameterValues = { douWendu, douWenduSet, douShidu, douShiduSet, intHour, intMinute, intSecond, intStatus };
                                    for (int i = 0; i < 8; i++) {
                                        string parameterId = parameterIds[i];
                                        double parameterValue = parameterValues[i];
                                        if (warn.maxValue.ContainsKey(parameters[0] + "_" + parameterId) && parameterValue > warn.maxValue[parameters[0] + "_" + parameterId] && warn.notificationType.ContainsKey(parameterId)) {
                                            cmd.CommandText = "insert into tb_warningmessagerecord(id,notificationtypeid,paramid,equipmentid,plantid,processid,status,message,updatetime,updater) values('"
                                                + Guid.NewGuid().ToString("N") + "','" + warn.notificationType[parameterId] + "','" + parameterId + "','" + parameters[0] + "','1003','1004','1','" + warn.equipmentInfo[parameters[0]] + warn.parameterInfo[parameterId] + "超高:" + parameterValue + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','采集程序')";
                                            cmd.ExecuteNonQuery();
                                        }
                                        if (warn.minValue.ContainsKey(parameters[0] + "_" + parameterId) && parameterValue < warn.minValue[parameters[0] + "_" + parameterId] && warn.notificationType.ContainsKey(parameterId)) {
                                            cmd.CommandText = "insert into tb_warningmessagerecord(id,notificationtypeid,paramid,equipmentid,plantid,processid,status,message,updatetime,updater) values('"
                                                + Guid.NewGuid().ToString("N") + "','" + warn.notificationType[parameterId] + "','" + parameterId + "','" + parameters[0] + "','1003','1004','1','" + warn.equipmentInfo[parameters[0]] + warn.parameterInfo[parameterId] + "超低:" + parameterValue + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','采集程序')";
                                            cmd.ExecuteNonQuery();
                                        }
                                    }
                                }
                            }
                        } else {
                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>N/A(" + parameters[1] + ")\n");
                        }
                    } catch (Exception ee) {
                        log.Error(DateTime.Now.ToString() + ee.Message);
                    } finally {
                        if (siemens != null) {
                            siemens.ConnectClose();
                        }
                    }
                }
                Thread.Sleep(300000);
            }
        }

        private void sbhg(Object com) {
            string sendTextBox = "textBox1";
            string recvTextBox = "textBox2";
            string[] plcs = com.ToString().Split(',');
            while (true) {
                foreach (string plc in plcs) {
                    SiemensS7Net siemens = null;
                    try {
                        string[] parameters = ConfigurationManager.AppSettings[plc].Split(',');
                        siemens = new SiemensS7Net(SiemensPLCS.S200, parameters[1]) { ConnectTimeOut = 5000 };
                        OperateResult connect = siemens.ConnectServer();
                        if (connect.IsSuccess) {
                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Parameter1\n");
                            double douSZL = siemens.ReadInt16("DB1.2212").Content / 10.0;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=酸重量:" + douSZL.ToString("0.0") + "\n");

                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Parameter2\n");
                            double douSHZL = siemens.ReadInt16("DB1.2202").Content / 10.0;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=水重量:" + douSHZL.ToString("0.0") + "\n");

                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Parameter3\n");
                            int intQFZL = siemens.ReadInt16("DB1.2200").Content;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=铅粉重量:" + intQFZL + "\n");

                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Parameter4\n");
                            double douLSJKWD = siemens.ReadInt16("DB1.2218").Content / 10.0;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=冷水进口温度:" + douLSJKWD.ToString("0.0") + "\n");

                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Parameter5\n");
                            double douLSCKWD = siemens.ReadInt16("DB1.2216").Content / 10.0;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=冷水出口温度:" + douLSCKWD.ToString("0.0") + "\n");

                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Parameter6\n");
                            double douQY = siemens.ReadInt16("DB1.2208").Content / 100.0;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=气压:" + douQY.ToString("0.00") + "\n");

                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Parameter7\n");
                            double douCGWD = siemens.ReadInt16("DB1.5048").Content / 10.0;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=出膏温度:" + douCGWD.ToString("0.0") + "\n");

                            using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MYSQL"].ConnectionString)) {
                                conn.Open();
                                using (MySqlCommand cmd = new MySqlCommand()) {
                                    cmd.Connection = conn;
                                    cmd.CommandText = "insert into tb_blenderoperatingparametersacquisition_1003 (id,equipmentID,acquisitionTime,remark,status,vitriolWeight,WaterWeight,leadPowderWeight,waterInTemperature,waterOutTemperature,atmosphericPressure,leadPasteOutTemperature) values('"
                                        + Guid.NewGuid().ToString("N") + "','" + parameters[0] + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','仪表采集',1,'" + douSZL.ToString("0.0") + "','" + douSHZL.ToString("0.0") + "','" + intQFZL + "','" + douLSJKWD.ToString("0.0") + "','" + douLSCKWD.ToString("0.0") + "','" + douQY.ToString("0.0") + "','" + douCGWD.ToString("0.0") + "')";
                                    cmd.ExecuteNonQuery();
                                    string[] parameterIds = { "30001", "30002", "30003", "30004", "30005", "30006", "30007" };
                                    double[] parameterValues = { douSZL, douSHZL, intQFZL, douLSJKWD, douLSCKWD, douQY, douCGWD };
                                    for (int i = 0; i < 7; i++) {
                                        string parameterId = parameterIds[i];
                                        double parameterValue = parameterValues[i];
                                        if (warn.maxValue.ContainsKey(parameters[0] + "_" + parameterId) && parameterValue > warn.maxValue[parameters[0] + "_" + parameterId] && warn.notificationType.ContainsKey(parameterId)) {
                                            cmd.CommandText = "insert into tb_warningmessagerecord(id,notificationtypeid,paramid,equipmentid,plantid,processid,status,message,updatetime,updater) values('"
                                                + Guid.NewGuid().ToString("N") + "','" + warn.notificationType[parameterId] + "','" + parameterId + "','" + parameters[0] + "','1003','1002','1','" + warn.equipmentInfo[parameters[0]] + warn.parameterInfo[parameterId] + "超高:" + parameterValue + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','采集程序')";
                                            cmd.ExecuteNonQuery();
                                        }
                                        if (warn.minValue.ContainsKey(parameters[0] + "_" + parameterId) && parameterValue < warn.minValue[parameters[0] + "_" + parameterId] && warn.notificationType.ContainsKey(parameterId)) {
                                            cmd.CommandText = "insert into tb_warningmessagerecord(id,notificationtypeid,paramid,equipmentid,plantid,processid,status,message,updatetime,updater) values('"
                                                + Guid.NewGuid().ToString("N") + "','" + warn.notificationType[parameterId] + "','" + parameterId + "','" + parameters[0] + "','1003','1002','1','" + warn.equipmentInfo[parameters[0]] + warn.parameterInfo[parameterId] + "超低:" + parameterValue + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','采集程序')";
                                            cmd.ExecuteNonQuery();
                                        }
                                    }
                                }
                            }
                        } else {
                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>N/A(" + parameters[1] + ")\n");
                        }
                    } catch (Exception ee) {
                        log.Error(DateTime.Now.ToString() + ee.Message);
                    } finally {
                        if (siemens != null) {
                            siemens.ConnectClose();
                        }
                    }
                }
                Thread.Sleep(300000);
            }
        }

        private void sbqm(Object com) {
            string sendTextBox = "textBox16";
            string recvTextBox = "textBox15";
            string[] plcs = com.ToString().Split(',');
            while (true) {
                foreach (string plc in plcs) {
                    MelsecA1ENet melsec = null;
                    try {
                        string[] parameters = ConfigurationManager.AppSettings[plc].Split(',');
                        melsec = new MelsecA1ENet(parameters[1], 5551) { ConnectTimeOut = 5000 };
                        OperateResult connect = melsec.ConnectServer();
                        if (connect.IsSuccess) {
                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Parameter1\n");
                            double zjqt = melsec.ReadInt16("D40").Content;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=主机启停:" + zjqt.ToString("0") + "\n");

                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Parameter2\n");
                            double zjglsd = melsec.ReadInt16("D230").Content / 10.0;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=主机功率设定:" + zjglsd.ToString("0.0") + "\n");

                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Parameter3\n");
                            double zjglfk = melsec.ReadInt16("D108").Content / 10.0;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=主机功率反馈:" + zjglfk.ToString("0.0") + "\n");

                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Parameter4\n");
                            double jql = melsec.ReadInt16("D113").Content;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=进球量:" + jql.ToString("0") + "\n");

                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Parameter5\n");
                            double zjqdwdfk = melsec.ReadInt16("D100").Content;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=主机前段温度反馈:" + zjqdwdfk.ToString("0") + "\n");

                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Parameter6\n");
                            double zjzdwdfk = melsec.ReadInt16("D101").Content;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=主机中端温度反馈:" + zjzdwdfk.ToString("0") + "\n");

                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Parameter7\n");
                            double zjhdwdfk = melsec.ReadInt16("D102").Content;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=主机后端温度反馈:" + zjhdwdfk.ToString("0") + "\n");

                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Parameter8\n");
                            double bdyc = melsec.ReadInt16("D110").Content;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=布袋压差:" + bdyc.ToString("0") + "\n");

                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Parameter9\n");
                            double bdwd = melsec.ReadInt16("D103").Content;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=布袋温度:" + bdwd.ToString("0") + "\n");

                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Parameter10\n");
                            double gxyc = melsec.ReadInt16("D111").Content;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=高效压差:" + gxyc.ToString("0") + "\n");

                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Parameter11\n");
                            double fyfmycfk = melsec.ReadInt16("D112").Content;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=负压风门压差反馈:" + fyfmycfk.ToString("0") + "\n");

                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Parameter12\n");
                            double zyfmycfk = melsec.ReadInt16("D109").Content;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=正压风门压差反馈:" + zyfmycfk.ToString("0") + "\n");

                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Parameter13\n");
                            double qdzcwd = melsec.ReadInt16("D104").Content;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=前端轴承温度:" + qdzcwd.ToString("0") + "\n");

                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Parameter14\n");
                            double hdzcwd = melsec.ReadInt16("D105").Content;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=后端轴承温度:" + hdzcwd.ToString("0") + "\n");

                            using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MYSQL"].ConnectionString)) {
                                conn.Open();
                                using (MySqlCommand cmd = new MySqlCommand()) {
                                    cmd.Connection = conn;
                                    cmd.CommandText = "insert into tb_qmjparametersacquisition_1003 (id,equipmentID,acquisitionTime,remark,status,zjqt,zjglsd,zjglfk,jql,zjqdwdfk,zjzdwdfk,zjhdwdfk,bdyc,bdwd,gxyc,fyfmycfk,zyfmycfk,qdzcwd,hdzcwd) values('"
                                        + Guid.NewGuid().ToString("N") + "','" + parameters[0] + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','仪表采集',1,'" + zjqt.ToString("0") + "','" + zjglsd.ToString("0.0") + "','" + zjglfk.ToString("0.0") + "','" + jql.ToString("0") + "','" + zjqdwdfk.ToString("0") + "','" + zjzdwdfk.ToString("0") + "','" + zjhdwdfk.ToString("0") + "','" + bdyc.ToString("0") + "','" + bdwd.ToString("0") + "','" + gxyc.ToString("0") + "','" + fyfmycfk.ToString("0") + "','" + zyfmycfk.ToString("0.0") + "','" + qdzcwd.ToString("0") + "','" + hdzcwd.ToString("0") + "')";
                                    cmd.ExecuteNonQuery();
                                    string[] parameterIds = { "10010", "10011", "10012", "10013", "10014", "10015", "10016", "10017", "10018", "10019", "10020", "10021", "10022", "10023" };
                                    double[] parameterValues = { zjqt, zjglsd, zjglfk, jql, zjqdwdfk, zjzdwdfk, zjhdwdfk, bdyc, bdwd, gxyc, fyfmycfk, zyfmycfk, qdzcwd, hdzcwd };
                                    for (int i = 0; i < 14; i++) {
                                        string parameterId = parameterIds[i];
                                        double parameterValue = parameterValues[i];
                                        if (warn.maxValue.ContainsKey(parameters[0] + "_" + parameterId) && parameterValue > warn.maxValue[parameters[0] + "_" + parameterId] && warn.notificationType.ContainsKey(parameterId)) {
                                            cmd.CommandText = "insert into tb_warningmessagerecord(id,notificationtypeid,paramid,equipmentid,plantid,processid,status,message,updatetime,updater) values('"
                                                + Guid.NewGuid().ToString("N") + "','" + warn.notificationType[parameterId] + "','" + parameterId + "','" + parameters[0] + "','1003','1001','1','" + warn.equipmentInfo[parameters[0]] + warn.parameterInfo[parameterId] + "超高:" + parameterValue + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','采集程序')";
                                            cmd.ExecuteNonQuery();
                                        }
                                        if (warn.minValue.ContainsKey(parameters[0] + "_" + parameterId) && parameterValue < warn.minValue[parameters[0] + "_" + parameterId] && warn.notificationType.ContainsKey(parameterId)) {
                                            cmd.CommandText = "insert into tb_warningmessagerecord(id,notificationtypeid,paramid,equipmentid,plantid,processid,status,message,updatetime,updater) values('"
                                                + Guid.NewGuid().ToString("N") + "','" + warn.notificationType[parameterId] + "','" + parameterId + "','" + parameters[0] + "','1003','1001','1','" + warn.equipmentInfo[parameters[0]] + warn.parameterInfo[parameterId] + "超低:" + parameterValue + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','采集程序')";
                                            cmd.ExecuteNonQuery();
                                        }
                                    }
                                }
                            }
                        } else {
                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>N/A(" + parameters[1] + ")\n");
                        }
                    } catch (Exception ee) {
                        log.Error(DateTime.Now.ToString() + ee.Message);
                    } finally {
                        if (melsec != null) {
                            melsec.ConnectClose();
                        }
                    }
                }
                Thread.Sleep(300000);
            }
        }

        private void ebgh(Object com) {
            string sendTextBox = "textBox10";
            string recvTextBox = "textBox9";
            string[] plcs = com.ToString().Split(',');
            while (true) {
                foreach (string plc in plcs) {
                    SiemensS7Net siemens = null;
                    try {
                        string[] parameters = ConfigurationManager.AppSettings[plc].Split(',');
                        siemens = new SiemensS7Net(SiemensPLCS.S200, parameters[1]) { ConnectTimeOut = 5000 };
                        OperateResult connect = siemens.ConnectServer();
                        if (connect.IsSuccess) {
                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Temperat\n");
                            double douWendu = siemens.ReadInt16("DB1.1200").Content / 10.0;
                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query TempSet\n");
                            double douWenduSet = siemens.ReadInt16("DB1.1202").Content / 10.0;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=温度:" + douWendu.ToString("0.0") + "℃\n");
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=设置温度:" + douWenduSet.ToString("0.0") + "℃\n");
                            double douShidu = siemens.ReadInt16("DB1.1204").Content / 10.0;
                            double douShiduSet = siemens.ReadInt16("DB1.1206").Content / 10.0;
                            if (!plc.Contains("_3")) {
                                SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Humidity\n");
                                SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=湿度:" + douShidu.ToString("0.0") + "%\n");
                                SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query HumiSet\n");
                                SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=设置湿度:" + douShiduSet.ToString("0.0") + "%\n");
                            }
                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Hour\n");
                            int intHour = siemens.ReadInt32("DB1.322").Content;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=固化小时:" + intHour + "\n");
                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Minute\n");
                            int intMinute = siemens.ReadInt32("DB1.326").Content;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=固化分钟:" + intMinute + "\n");
                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Second\n");
                            int intSecond = siemens.ReadInt32("DB1.330").Content;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=固化秒:" + intSecond + "\n");
                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Status\n");
                            int intStatus = siemens.ReadInt16("DB1.220").Content;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=运行状态:" + intStatus + "\n");
                            using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MYSQL"].ConnectionString)) {
                                conn.Open();
                                using (MySqlCommand cmd = new MySqlCommand()) {
                                    cmd.Connection = conn;
                                    if (!plc.Contains("_3")) {
                                        cmd.CommandText = "insert into tb_solidificationoperatingparametersacquisition_1002 (id,equipmentID,acquisitionTime,remark,status,realtimeTemperature,settingTemperature,realtimeHumidity,settingHumidity,solidificationHour,solidificationMinute,solidificationSecond,runningStatus) values('"
                                            + Guid.NewGuid().ToString("N") + "','" + parameters[0] + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','仪表采集',1,'" + douWendu.ToString("0.0") + "','" + douWenduSet.ToString("0.0") + "','" + douShidu.ToString("0.0") + "','" + douShiduSet.ToString("0.0") + "','" + intHour + "','" + intMinute + "','" + intSecond + "','" + intStatus + "')";
                                    } else {
                                        cmd.CommandText = "insert into tb_solidificationoperatingparametersacquisition_1002 (id,equipmentID,acquisitionTime,remark,status,realtimeTemperature,settingTemperature,solidificationHour,solidificationMinute,solidificationSecond,runningStatus) values('"
                                            + Guid.NewGuid().ToString("N") + "','" + parameters[0] + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','仪表采集',1,'" + douWendu.ToString("0.0") + "','" + douWenduSet.ToString("0.0") + "','" + intHour + "','" + intMinute + "','" + intSecond + "','" + intStatus + "')";
                                    }
                                    cmd.ExecuteNonQuery();
                                    string[] parameterIds = { "70001", "70002", "70003", "70004", "70005", "70006", "70007", "70008" };
                                    double[] parameterValues = { douWendu, douWenduSet, douShidu, douShiduSet, intHour, intMinute, intSecond, intStatus };
                                    for (int i = 0; i < 8; i++) {
                                        string parameterId = parameterIds[i];
                                        double parameterValue = parameterValues[i];
                                        if (warn.maxValue.ContainsKey(parameters[0] + "_" + parameterId) && parameterValue > warn.maxValue[parameters[0] + "_" + parameterId] && warn.notificationType.ContainsKey(parameterId)) {
                                            cmd.CommandText = "insert into tb_warningmessagerecord(id,notificationtypeid,paramid,equipmentid,plantid,processid,status,message,updatetime,updater) values('"
                                                + Guid.NewGuid().ToString("N") + "','" + warn.notificationType[parameterId] + "','" + parameterId + "','" + parameters[0] + "','1002','1004','1','" + warn.equipmentInfo[parameters[0]] + warn.parameterInfo[parameterId] + "超高:" + parameterValue + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','采集程序')";
                                            cmd.ExecuteNonQuery();
                                        }
                                        if (warn.minValue.ContainsKey(parameters[0] + "_" + parameterId) && parameterValue < warn.minValue[parameters[0] + "_" + parameterId] && warn.notificationType.ContainsKey(parameterId)) {
                                            cmd.CommandText = "insert into tb_warningmessagerecord(id,notificationtypeid,paramid,equipmentid,plantid,processid,status,message,updatetime,updater) values('"
                                                + Guid.NewGuid().ToString("N") + "','" + warn.notificationType[parameterId] + "','" + parameterId + "','" + parameters[0] + "','1002','1004','1','" + warn.equipmentInfo[parameters[0]] + warn.parameterInfo[parameterId] + "超低:" + parameterValue + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','采集程序')";
                                            cmd.ExecuteNonQuery();
                                        }
                                    }
                                }
                            }
                        } else {
                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>N/A(" + parameters[1] + ")\n");
                        }
                    } catch (Exception ee) {
                        log.Error(DateTime.Now.ToString() + ee.Message);
                    } finally {
                        if (siemens != null) {
                            siemens.ConnectClose();
                        }
                    }
                }
                Thread.Sleep(300000);
            }
        }

        private void ebhg(Object com) {
            string sendTextBox = "textBox8";
            string recvTextBox = "textBox7";
            string[] plcs = com.ToString().Split(',');
            while (true) {
                foreach (string plc in plcs) {
                    SiemensS7Net siemens = null;
                    try {
                        string[] parameters = ConfigurationManager.AppSettings[plc].Split(',');
                        siemens = new SiemensS7Net(SiemensPLCS.S200, parameters[1]) { ConnectTimeOut = 5000 };
                        OperateResult connect = siemens.ConnectServer();
                        if (connect.IsSuccess) {
                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Parameter1\n");
                            double douSZL = siemens.ReadInt16("DB1.2212").Content / 10.0;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=酸重量:" + douSZL.ToString("0.0") + "\n");

                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Parameter2\n");
                            double douSHZL = siemens.ReadInt16("DB1.2202").Content / 10.0;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=水重量:" + douSHZL.ToString("0.0") + "\n");

                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Parameter3\n");
                            int intQFZL = siemens.ReadInt16("DB1.2200").Content;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=铅粉重量:" + intQFZL + "\n");

                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Parameter4\n");
                            double douLSJKWD = siemens.ReadInt16("DB1.2218").Content / 10.0;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=冷水进口温度:" + douLSJKWD.ToString("0.0") + "\n");

                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Parameter5\n");
                            double douLSCKWD = siemens.ReadInt16("DB1.2216").Content / 10.0;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=冷水出口温度:" + douLSCKWD.ToString("0.0") + "\n");

                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Parameter6\n");
                            double douQY = siemens.ReadInt16("DB1.2208").Content / 100.0;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=气压:" + douQY.ToString("0.00") + "\n");

                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Parameter7\n");
                            double douCGWD = siemens.ReadInt16("DB1.5048").Content / 10.0;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=出膏温度:" + douCGWD.ToString("0.0") + "\n");

                            using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MYSQL"].ConnectionString)) {
                                conn.Open();
                                using (MySqlCommand cmd = new MySqlCommand()) {
                                    cmd.Connection = conn;
                                    cmd.CommandText = "insert into tb_blenderoperatingparametersacquisition_1002 (id,equipmentID,acquisitionTime,remark,status,vitriolWeight,WaterWeight,leadPowderWeight,waterInTemperature,waterOutTemperature,atmosphericPressure,leadPasteOutTemperature) values('"
                                        + Guid.NewGuid().ToString("N") + "','" + parameters[0] + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','仪表采集',1,'" + douSZL.ToString("0.0") + "','" + douSHZL.ToString("0.0") + "','" + intQFZL + "','" + douLSJKWD.ToString("0.0") + "','" + douLSCKWD.ToString("0.0") + "','" + douQY.ToString("0.0") + "','" + douCGWD.ToString("0.0") + "')";
                                    cmd.ExecuteNonQuery();
                                    string[] parameterIds = { "30001", "30002", "30003", "30004", "30005", "30006", "30007" };
                                    double[] parameterValues = { douSZL, douSHZL, intQFZL, douLSJKWD, douLSCKWD, douQY, douCGWD };
                                    for (int i = 0; i < 7; i++) {
                                        string parameterId = parameterIds[i];
                                        double parameterValue = parameterValues[i];
                                        if (warn.maxValue.ContainsKey(parameters[0] + "_" + parameterId) && parameterValue > warn.maxValue[parameters[0] + "_" + parameterId] && warn.notificationType.ContainsKey(parameterId)) {
                                            cmd.CommandText = "insert into tb_warningmessagerecord(id,notificationtypeid,paramid,equipmentid,plantid,processid,status,message,updatetime,updater) values('"
                                                + Guid.NewGuid().ToString("N") + "','" + warn.notificationType[parameterId] + "','" + parameterId + "','" + parameters[0] + "','1002','1002','1','" + warn.equipmentInfo[parameters[0]] + warn.parameterInfo[parameterId] + "超高:" + parameterValue + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','采集程序')";
                                            cmd.ExecuteNonQuery();
                                        }
                                        if (warn.minValue.ContainsKey(parameters[0] + "_" + parameterId) && parameterValue < warn.minValue[parameters[0] + "_" + parameterId] && warn.notificationType.ContainsKey(parameterId)) {
                                            cmd.CommandText = "insert into tb_warningmessagerecord(id,notificationtypeid,paramid,equipmentid,plantid,processid,status,message,updatetime,updater) values('"
                                                + Guid.NewGuid().ToString("N") + "','" + warn.notificationType[parameterId] + "','" + parameterId + "','" + parameters[0] + "','1002','1002','1','" + warn.equipmentInfo[parameters[0]] + warn.parameterInfo[parameterId] + "超低:" + parameterValue + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','采集程序')";
                                            cmd.ExecuteNonQuery();
                                        }
                                    }
                                }
                            }
                        } else {
                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>N/A(" + parameters[1] + ")\n");
                        }
                    } catch (Exception ee) {
                        log.Error(DateTime.Now.ToString() + ee.Message);
                    } finally {
                        if (siemens != null) {
                            siemens.ConnectClose();
                        }
                    }
                }
                Thread.Sleep(300000);
            }
        }

        private void ybgh(Object com) {
            string sendTextBox = "textBox22";
            string recvTextBox = "textBox21";
            string[] plcs = com.ToString().Split(',');
            while (true) {
                foreach (string plc in plcs) {
                    SiemensS7Net siemens = null;
                    try {
                        string[] parameters = ConfigurationManager.AppSettings[plc].Split(',');
                        siemens = new SiemensS7Net(SiemensPLCS.S200, parameters[1]) { ConnectTimeOut = 5000 };
                        OperateResult connect = siemens.ConnectServer();
                        if (connect.IsSuccess) {
                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Temperat\n");
                            double douWendu = siemens.ReadInt16("DB1.1200").Content / 10.0;
                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query TempSet\n");
                            double douWenduSet = siemens.ReadInt16("DB1.1202").Content / 10.0;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=温度:" + douWendu.ToString("0.0") + "℃\n");
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=设置温度:" + douWenduSet.ToString("0.0") + "℃\n");
                            double douShidu = siemens.ReadInt16("DB1.1204").Content / 10.0;
                            double douShiduSet = siemens.ReadInt16("DB1.1206").Content / 10.0;
                            if (!plc.Contains("_3")) {
                                SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Humidity\n");
                                SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=湿度:" + douShidu.ToString("0.0") + "%\n");
                                SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query HumiSet\n");
                                SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=设置湿度:" + douShiduSet.ToString("0.0") + "%\n");
                            }
                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Hour\n");
                            int intHour = siemens.ReadInt32("DB1.322").Content;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=固化小时:" + intHour + "\n");
                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Minute\n");
                            int intMinute = siemens.ReadInt32("DB1.326").Content;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=固化分钟:" + intMinute + "\n");
                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Second\n");
                            int intSecond = siemens.ReadInt32("DB1.330").Content;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=固化秒:" + intSecond + "\n");
                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Status\n");
                            int intStatus = siemens.ReadInt16("DB1.220").Content;
                            SetText(recvTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=运动状态:" + intStatus + "\n");
                            using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MYSQL"].ConnectionString)) {
                                conn.Open();
                                using (MySqlCommand cmd = new MySqlCommand()) {
                                    cmd.Connection = conn;
                                    if (!plc.Contains("_3")) {
                                        cmd.CommandText = "insert into tb_solidificationoperatingparametersacquisition_1001 (id,equipmentID,acquisitionTime,remark,status,realtimeTemperature,settingTemperature,realtimeHumidity,settingHumidity,solidificationHour,solidificationMinute,solidificationSecond,runningStatus) values('"
                                            + Guid.NewGuid().ToString("N") + "','" + parameters[0] + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','仪表采集',1,'" + douWendu.ToString("0.0") + "','" + douWenduSet.ToString("0.0") + "','" + douShidu.ToString("0.0") + "','" + douShiduSet.ToString("0.0") + "','" + intHour + "','" + intMinute + "','" + intSecond + "','" + intStatus + "')";
                                    } else {
                                        cmd.CommandText = "insert into tb_solidificationoperatingparametersacquisition_1001 (id,equipmentID,acquisitionTime,remark,status,realtimeTemperature,settingTemperature,solidificationHour,solidificationMinute,solidificationSecond,runningStatus) values('"
                                            + Guid.NewGuid().ToString("N") + "','" + parameters[0] + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','仪表采集',1,'" + douWendu.ToString("0.0") + "','" + douWenduSet.ToString("0.0") + "','" + intHour + "','" + intMinute + "','" + intSecond + "','" + intStatus + "')";
                                    }
                                    cmd.ExecuteNonQuery();
                                    string[] parameterIds = { "70001", "70002", "70003", "70004", "70005", "70006", "70007", "70008" };
                                    double[] parameterValues = { douWendu, douWenduSet, douShidu, douShiduSet, intHour, intMinute, intSecond, intStatus };
                                    for (int i = 0; i < 8; i++) {
                                        string parameterId = parameterIds[i];
                                        double parameterValue = parameterValues[i];
                                        if (warn.maxValue.ContainsKey(parameters[0] + "_" + parameterId) && parameterValue > warn.maxValue[parameters[0] + "_" + parameterId] && warn.notificationType.ContainsKey(parameterId)) {
                                            cmd.CommandText = "insert into tb_warningmessagerecord(id,notificationtypeid,paramid,equipmentid,plantid,processid,status,message,updatetime,updater) values('"
                                                + Guid.NewGuid().ToString("N") + "','" + warn.notificationType[parameterId] + "','" + parameterId + "','" + parameters[0] + "','1001','1004','1','" + warn.equipmentInfo[parameters[0]] + warn.parameterInfo[parameterId] + "超高:" + parameterValue + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','采集程序')";
                                            cmd.ExecuteNonQuery();
                                        }
                                        if (warn.minValue.ContainsKey(parameters[0] + "_" + parameterId) && parameterValue < warn.minValue[parameters[0] + "_" + parameterId] && warn.notificationType.ContainsKey(parameterId)) {
                                            cmd.CommandText = "insert into tb_warningmessagerecord(id,notificationtypeid,paramid,equipmentid,plantid,processid,status,message,updatetime,updater) values('"
                                                + Guid.NewGuid().ToString("N") + "','" + warn.notificationType[parameterId] + "','" + parameterId + "','" + parameters[0] + "','1001','1004','1','" + warn.equipmentInfo[parameters[0]] + warn.parameterInfo[parameterId] + "超低:" + parameterValue + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','采集程序')";
                                            cmd.ExecuteNonQuery();
                                        }
                                    }
                                }
                            }
                        } else {
                            SetText(sendTextBox, plc + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>N/A(" + parameters[1] + ")\n");
                        }
                    } catch (Exception ee) {
                        log.Error(DateTime.Now.ToString() + ee.Message);
                    } finally {
                        if (siemens != null) {
                            siemens.ConnectClose();
                        }
                    }
                }
                Thread.Sleep(300000);
            }
        }

        private void gybdz97(Object com) {
            string sendTextBox = "textBox32";
            string recvTextBox = "textBox31";
            string[] parameters = com.ToString().Split(',');
            SerialPort serialPort = new SerialPort(parameters[0], Convert.ToInt32(parameters[1]), (Parity)Convert.ToInt32(parameters[3]), Convert.ToInt32(parameters[2]), (StopBits)Convert.ToInt32(parameters[4]));
            int num = Convert.ToInt32(parameters[6]);
            string[] queryTimes = { "0:00", "1:00", "2:00", "3:00", "4:00", "5:00", "6:00", "7:00", "8:00", "9:00", "10:00", "11:00", "12:00", "13:00", "14:00", "15:00", "16:00", "17:00", "18:00", "19:00", "20:00", "21:00", "22:00", "23:00" };
            string[] columnNames = { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen", "twenty", "twentyone", "twentytwo", "twentythree" };
            while (true) {
                try {
                    if (!serialPort.IsOpen) serialPort.Open();
                    string now = DateTime.Now.ToShortTimeString();
                    int index = Array.IndexOf(queryTimes, now);
                    if (index > -1) {
                        if (index == 0) {
                            using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MYSQL"].ConnectionString)) {
                                conn.Open();
                                using (MySqlCommand cmd = new MySqlCommand("", conn)) {
                                    for (int i = 1; i <= num; i++) {
                                        string equipment = ConfigurationManager.AppSettings[parameters[6 + i]];
                                        string equipmentID = equipment.Split(',')[0];
                                        cmd.CommandText = "insert into tb_electricitymeterparametersacquisition_3003 (id,equipmentid,dayTime,remark,status) values('" + Guid.NewGuid().ToString("N") + "','" + equipmentID + "','" + DateTime.Now.ToString("yyyy-MM-dd") + "','仪表采集','1')";
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                        }
                        for (int i = 1; i <= num; i++) {
                            string equipment = ConfigurationManager.AppSettings[parameters[6 + i]];
                            string equipmentID = equipment.Split(',')[0];
                            string equipmentAdd = equipment.Split(',')[1];
                            string orderNoCheck = "68" + equipmentAdd + "68010243C3";
                            string order = addCheckCode(orderNoCheck);
                            byte[] bufferS = SoftBasic.HexStringToBytes(order);
                            for (int j = 0; j <= 2; j++) {
                                serialPort.Write(bufferS, 0, bufferS.Length);
                                SetText(sendTextBox, parameters[0] + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>" + SoftBasic.ByteToHexString(bufferS) + "\n");
                                Thread.Sleep(1000);
                                byte[] bufferR = null;
                                if (serialPort.BytesToRead > 0) {
                                    bufferR = new byte[serialPort.BytesToRead];
                                    serialPort.Read(bufferR, 0, bufferR.Length);
                                } else {
                                    SetText(recvTextBox, parameters[0] + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=N/A\n");
                                    Thread.Sleep(20000);
                                    continue;
                                }
                                SetText(recvTextBox, parameters[0] + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=" + SoftBasic.ByteToHexString(bufferR) + "\n");
                                byte[] bufferData = new byte[4] { (byte)(bufferR[15] - 51), (byte)(bufferR[14] - 51), (byte)(bufferR[13] - 51), (byte)(bufferR[12] - 51) };
                                double data = (double)Convert.ToInt32(SoftBasic.ByteToHexString(bufferData), 10) / (double)100.00;
                                SetText(recvTextBox, "返回数据:" + data + "\n");
                                using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MYSQL"].ConnectionString)) {
                                    conn.Open();
                                    using (MySqlCommand cmd = new MySqlCommand("", conn)) {
                                        cmd.CommandText = "update tb_electricitymeterparametersacquisition_3003 set " + columnNames[index] + "=" + data + " where equipmentid='" + equipmentID + "' and  dayTime='" + DateTime.Now.ToString("yyyy-MM-dd") + "'";
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                                break;
                            }
                        }
                        Thread.Sleep(2000000);
                    } else {
                        Thread.Sleep(40000);
                    }
                } catch (Exception e) {
                    log.Error(DateTime.Now.ToString() + e.Message);
                    Thread.Sleep(10000);
                }
            }
        }

        private void gybdz07(Object com) {
            string sendTextBox = "textBox32";
            string recvTextBox = "textBox31";
            string[] parameters = com.ToString().Split(',');
            SerialPort serialPort = new SerialPort(parameters[0], Convert.ToInt32(parameters[1]), (Parity)Convert.ToInt32(parameters[3]), Convert.ToInt32(parameters[2]), (StopBits)Convert.ToInt32(parameters[4]));
            int num = Convert.ToInt32(parameters[6]);
            string[] queryTimes = { "0:00", "1:00", "2:00", "3:00", "4:00", "5:00", "6:00", "7:00", "8:00", "9:00", "10:00", "11:00", "12:00", "13:00", "14:00", "15:00", "16:00", "17:00", "18:00", "19:00", "20:00", "21:00", "22:00", "23:00" };
            string[] columnNames = { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen", "twenty", "twentyone", "twentytwo", "twentythree" };
            while (true) {
                try {
                    if (!serialPort.IsOpen) serialPort.Open();
                    string now = DateTime.Now.ToShortTimeString();
                    int index = Array.IndexOf(queryTimes, now);
                    if (index > -1) {
                        if (index == 0) {
                            using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MYSQL"].ConnectionString)) {
                                conn.Open();
                                using (MySqlCommand cmd = new MySqlCommand("", conn)) {
                                    for (int i = 1; i <= num; i++) {
                                        string equipment = ConfigurationManager.AppSettings[parameters[6 + i]];
                                        string equipmentID = equipment.Split(',')[0];
                                        cmd.CommandText = "insert into tb_electricitymeterparametersacquisition_3003 (id,equipmentid,dayTime,remark,status) values('" + Guid.NewGuid().ToString("N") + "','" + equipmentID + "','" + DateTime.Now.ToString("yyyy-MM-dd") + "','仪表采集','1')";
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                        }
                        for (int i = 1; i <= num; i++) {
                            string equipment = ConfigurationManager.AppSettings[parameters[6 + i]];
                            string equipmentID = equipment.Split(',')[0];
                            string equipmentAdd = equipment.Split(',')[1];
                            string orderNoCheck = "68" + equipmentAdd + "68110433333333";
                            string order = addCheckCode(orderNoCheck);
                            byte[] bufferS = SoftBasic.HexStringToBytes(order);
                            for (int j = 0; j <= 2; j++) {
                                serialPort.Write(bufferS, 0, bufferS.Length);
                                SetText(sendTextBox, parameters[0] + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>" + SoftBasic.ByteToHexString(bufferS) + "\n");
                                Thread.Sleep(1000);
                                byte[] bufferR = null;
                                if (serialPort.BytesToRead > 0) {
                                    bufferR = new byte[serialPort.BytesToRead];
                                    serialPort.Read(bufferR, 0, bufferR.Length);
                                } else {
                                    SetText(recvTextBox, parameters[0] + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=N/A\n");
                                    Thread.Sleep(20000);
                                    continue;
                                }
                                SetText(recvTextBox, parameters[0] + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=" + SoftBasic.ByteToHexString(bufferR) + "\n");
                                byte[] bufferData = new byte[4] { (byte)(bufferR[17] - 51), (byte)(bufferR[16] - 51), (byte)(bufferR[15] - 51), (byte)(bufferR[14] - 51) };
                                double data = (double)Convert.ToInt32(SoftBasic.ByteToHexString(bufferData), 10) / (double)100.00;
                                SetText(recvTextBox, "返回数据:" + data + "\n");
                                using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MYSQL"].ConnectionString)) {
                                    conn.Open();
                                    using (MySqlCommand cmd = new MySqlCommand("", conn)) {
                                        cmd.CommandText = "update tb_electricitymeterparametersacquisition_3003 set " + columnNames[index] + "=" + data + " where equipmentid='" + equipmentID + "' and  dayTime='" + DateTime.Now.ToString("yyyy-MM-dd") + "'";
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                                break;
                            }
                        }
                        Thread.Sleep(2000000);
                    } else {
                        Thread.Sleep(40000);
                    }
                } catch (Exception e) {
                    log.Error(DateTime.Now.ToString() + e.Message);
                    Thread.Sleep(10000);
                }
            }
        }

        private void hbfj(Object com) {
            string sendTextBox = "textBox30";
            string recvTextBox = "textBox29";
            string[] kzgs = com.ToString().Split(',');
            while (true) {
                foreach (string kzg in kzgs) {
                    ModbusTcpNet modbus = null;
                    try {
                        string[] parameters = ConfigurationManager.AppSettings[kzg].Split(',');
                        modbus = new ModbusTcpNet(parameters[0]);
                        OperateResult connect = modbus.ConnectServer();
                        if (connect.IsSuccess) {
                            SetText(sendTextBox, kzg + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Parameter1\n");
                            double zdpl = modbus.ReadFloat("x=4;0").Content;
                            SetText(recvTextBox, kzg + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=" + parameters[1].Substring(4) + "号振动频率:" + zdpl.ToString("0.00") + "\n");

                            SetText(sendTextBox, kzg + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Parameter2\n");
                            double zdplsx = modbus.ReadFloat("x=4;4").Content;
                            SetText(recvTextBox, kzg + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=" + parameters[1].Substring(4) + "号振动频率上限:" + zdplsx.ToString("0") + "\n");

                            SetText(sendTextBox, kzg + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Parameter3\n");
                            double bjyssj = modbus.ReadFloat("x=4;8").Content;
                            SetText(recvTextBox, kzg + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=" + parameters[1].Substring(4) + "号报警延时时间:" + bjyssj.ToString("0") + "\n");

                            SetText(sendTextBox, kzg + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Parameter4\n");
                            double bjcx = modbus.ReadFloat("x=4;12").Content;
                            SetText(recvTextBox, kzg + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=" + parameters[1].Substring(4) + "号报警次数:" + bjcx.ToString("0") + "\n");

                            SetText(sendTextBox, kzg + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Parameter1\n");
                            double zdpl2 = modbus.ReadFloat("x=4;2").Content;
                            SetText(recvTextBox, kzg + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=" + parameters[2].Substring(4) + "号振动频率:" + zdpl2.ToString("0.00") + "\n");

                            SetText(sendTextBox, kzg + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Parameter2\n");
                            double zdplsx2 = modbus.ReadFloat("x=4;6").Content;
                            SetText(recvTextBox, kzg + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=" + parameters[2].Substring(4) + "号振动频率上限:" + zdplsx2.ToString("0") + "\n");

                            SetText(sendTextBox, kzg + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Parameter3\n");
                            double bjyssj2 = modbus.ReadFloat("x=4;10").Content;
                            SetText(recvTextBox, kzg + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=" + parameters[2].Substring(4) + "号报警延时时间:" + bjyssj2.ToString("0") + "\n");

                            SetText(sendTextBox, kzg + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>Query Parameter4\n");
                            double bjcx2 = modbus.ReadFloat("x=4;14").Content;
                            SetText(recvTextBox, kzg + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "<=" + parameters[2].Substring(4) + "号报警次数:" + bjcx2.ToString("0") + "\n");
                        } else {
                            SetText(sendTextBox, kzg + "/" + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "=>N/A(" + parameters[0] + ")\n");
                        }
                    } catch (Exception ee) {
                        log.Error(DateTime.Now.ToString() + ee.Message);
                    } finally {
                        if (modbus != null) {
                            modbus.ConnectClose();
                        }
                    }
                }
                Thread.Sleep(300000);
            }
        }

        private string addCheckCode(string source) {
            int total = 0;
            int len = source.Length;
            for (int i = 0; i < len; i += 2) {
                string part = source.Substring(i, 2);
                total += int.Parse(part, NumberStyles.HexNumber);
            }
            string checkCode = total.ToString("X");
            return source + checkCode.Substring(checkCode.Length - 2, 2) + "16";
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