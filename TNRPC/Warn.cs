using System.Collections.Generic;
using System.Configuration;
using MySql.Data.MySqlClient;
using log4net;

namespace TNRPC {
    class Warn {
        public Dictionary<string, double> maxValue;
        public Dictionary<string, double> minValue;
        public Dictionary<string, string> notificationType;
        public Dictionary<string, string> equipmentInfo;
        public Dictionary<string, string> parameterInfo; 
        public Warn() {
            if (maxValue == null) {
                maxValue = new Dictionary<string, double>();
                minValue = new Dictionary<string, double>();
                notificationType = new Dictionary<string, string>();
                equipmentInfo = new Dictionary<string, string>();
                parameterInfo = new Dictionary<string, string>();
                using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MYSQL"].ConnectionString)) {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand()) {
                        cmd.Connection = conn;
                        cmd.CommandText = "select equipmentid,parameterid,maxlimit from tb_equipmentparameterlimit where maxlimit is not null";
                        using (MySqlDataReader data = cmd.ExecuteReader()) {
                            while (data.Read()) {
                                maxValue.Add(data.GetString("equipmentid") + "_" + data.GetString("parameterid"), data.GetDouble("maxlimit"));
                            }
                        }
                        cmd.CommandText = "select equipmentid,parameterid,minlimit from tb_equipmentparameterlimit where minlimit is not null";
                        using (MySqlDataReader data = cmd.ExecuteReader()) {
                            while (data.Read()) {
                                minValue.Add(data.GetString("equipmentid") + "_" + data.GetString("parameterid"), data.GetDouble("minlimit"));
                            }
                        }
                        cmd.CommandText = "select paraminfoid,notificationtypedetailid from tb_notificationtypeparaminfo where status=1";
                        using (MySqlDataReader data = cmd.ExecuteReader()) {
                            while (data.Read()) {
                                notificationType.Add(data.GetString("paraminfoid"), data.GetString("notificationtypedetailid"));
                            }
                        }
                        cmd.CommandText = "select id,name from tb_equipmentinfo where typeid in('10016','10017','10018')";
                        using (MySqlDataReader data = cmd.ExecuteReader()) {
                            while (data.Read()) {
                                equipmentInfo.Add(data.GetString("id"), data.GetString("name"));
                            }
                        }
                        cmd.CommandText = "select id,name from tb_parameterinfo";
                        using (MySqlDataReader data = cmd.ExecuteReader()) {
                            while (data.Read()) {
                                parameterInfo.Add(data.GetString("id"), data.GetString("name"));
                            }
                        }
                    }
                }
            }
            ILog log = log4net.LogManager.GetLogger("testApp.Logging");
            foreach (string key in maxValue.Keys) {
                log.Info(key + ".MAX=" + maxValue[key]);
            }
            foreach (string key in minValue.Keys) {
                log.Info(key + ".MIN=" + minValue[key]);
            }
            foreach (string key in notificationType.Keys) {
                log.Info(key + ".TYPE=" + notificationType[key]);
            }
            foreach (string key in equipmentInfo.Keys) {
                log.Info(key + ".NAME=" + equipmentInfo[key]);
            }
            foreach (string key in parameterInfo.Keys) {
                log.Info(key + ".NAME=" + parameterInfo[key]);
            }
        }
    }
}
