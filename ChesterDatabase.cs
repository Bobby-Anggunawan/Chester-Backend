using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using MySql.Data.MySqlClient;

namespace chesterBackendNet31
{
    public class ChesterDatabase
    {
        //===============================================
        static string projectID = "chester-game";
        static public FirestoreDb firestoreInstance = FirestoreDb.Create(projectID);
        //===============================================

        public class ChesterMySQL {

            private MySqlConnection _connection;
            MySqlConnection connection {
                get {
                    if(_connection == null) _connection = new MySqlConnection(getConnectionString());

                    return _connection;
                }
            }

            //========================================================================
            private bool openConnection() {
                try {
                    connection.Open();
                    return true;
                }
                catch {
                    return false;
                }
            }
            private void closeConnection() {
                connection.Close();
            }

            //kalau return true artinya sukses
            public bool addUser(string userID) {
                string query = $"insert into user values('{userID}', 0)";

                if (openConnection()) {
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.CommandText = query;
                    cmd.Connection = connection;
                    cmd.ExecuteNonQuery();

                    closeConnection();

                    return true;
                }
                return false;
            }

            public List<Dictionary<string, object>> getUser() {
                string query = "SELECT * FROM user";

                List<Dictionary<string, object>> ret = new List<Dictionary<string, object>>();

                if (openConnection())
                {
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    MySqlDataReader dataReader = cmd.ExecuteReader();
                    while (dataReader.Read())
                    {
                        ret.Add(new Dictionary<string, object> { { "userID", dataReader["userID"] }, { "coin", dataReader["coin"] } });
                    }
                    dataReader.Close();

                    closeConnection();
                }
                else {
                    ret.Add(new Dictionary<string, object> { { "isinya", "kosong" } });
                }
                return ret;
            }
            //========================================================================

            static public string getConnectionString()
            {
                // Equivalent connection string:
                // "Server=<dbSocketDir>/<INSTANCE_CONNECTION_NAME>;Uid=<DB_USER>;Pwd=<DB_PASS>;Database=<DB_NAME>;Protocol=unix"
                String dbSocketDir = Environment.GetEnvironmentVariable("DB_SOCKET_PATH") ?? "/cloudsql";
                String instanceConnectionName = Environment.GetEnvironmentVariable("INSTANCE_CONNECTION_NAME");
                var connectionString = new MySqlConnectionStringBuilder()
                {
                    // The Cloud SQL proxy provides encryption between the proxy and instance.
                    SslMode = MySqlSslMode.Disabled,
                    // Remember - storing secrets in plain text is potentially unsafe. Consider using
                    // something like https://cloud.google.com/secret-manager/docs/overview to help keep
                    // secrets secret.
                    Server = String.Format("{0}/{1}", dbSocketDir, instanceConnectionName),
                    UserID = Environment.GetEnvironmentVariable("DB_USER"),   // e.g. 'my-db-user
                    Password = Environment.GetEnvironmentVariable("DB_PASS"), // e.g. 'my-db-password'
                    Database = Environment.GetEnvironmentVariable("DB_NAME"), // e.g. 'my-database'
                    ConnectionProtocol = MySqlConnectionProtocol.UnixSocket
                };
                connectionString.Pooling = true;
                // Specify additional properties here.
                return connectionString.ConnectionString;
            }
        }
    }
}
