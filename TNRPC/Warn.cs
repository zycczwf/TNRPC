using System;
using System.Collections.Generic;
using System.Configuration;
using MySql.Data.MySqlClient;
using log4net;

namespace TNRPC {
    class Warn {
        public Dictionary<string, double> maxValue;
        public Dictionary<string, double> minValue;
        public Dictionary<string, string> notificationType;
        public Warn() {
            if (maxValue == null) {
                maxValue = new Dictionary<string, double>();
                minValue = new Dictionary<string, double>();
                notificationType = new Dictionary<string, string>();
                using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["MYSQL"].ConnectionString)) {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand()) {
                        cmd.Connection = conn;
                        cmd.CommandText = "select id,max from tb_parameterinfo where max is not null";
                        using (MySqlDataReader data = cmd.ExecuteReader()) {
                            while (data.Read()) {
                                maxValue.Add(data.GetString("id"), data.GetDouble("max"));
                            }
                        }
                        cmd.CommandText = "select id,min from tb_parameterinfo where min is not null";
                        using (MySqlDataReader data = cmd.ExecuteReader()) {
                            while (data.Read()) {
                                minValue.Add(data.GetString("id"), data.GetDouble("min"));
                            }
                        }
                        cmd.CommandText = "select paraminfoid,notificationtypedetailid from tb_notificationtypeparaminfo where status=1";
                        using (MySqlDataReader data = cmd.ExecuteReader()) {
                            while (data.Read()) {
                                notificationType.Add(data.GetString("paraminfoid"), data.GetString("notificationtypedetailid"));
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
        }
    }
}
