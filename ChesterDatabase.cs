using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using chesterBackendNet31.Models;
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

            //kalau return -1 artinya gagal
            //ini return jumlah koin user yang login
            public int addUser(string userID) {

                if (openConnection()) {
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.CommandText = $"SELECT EXISTS(SELECT * FROM user WHERE userID='{userID}')";
                    cmd.Connection = connection;
                    Int64 isExist = (Int64)cmd.ExecuteScalar();

                    if (isExist != 1L)
                    {
                        cmd.CommandText = $"insert into user values('{userID}', 500)";
                        cmd.ExecuteNonQuery();

                        //=============================
                        //kasih kartu ke user baru
                        cmd = new MySqlCommand(@$"insert into CardOwnership values  (NULL, '0001', '{userID}', 3),
                                                                                    (NULL, '0002', '{userID}', 3),
                                                                                    (NULL, '0003', '{userID}', 3),
                                                                                    (NULL, '0004', '{userID}', 3),
                                                                                    (NULL, '0005', '{userID}', 3),
                                                                                    (NULL, '0006', '{userID}', 3),
                                                                                    (NULL, '0007', '{userID}', 20)", connection);
                        cmd.ExecuteNonQuery();
                        //=============================

                        closeConnection();
                        return 500;
                    }
                    else {
                        cmd.CommandText = $"select coin from user where userID='{userID}'";
                        int jlhKoin = Convert.ToInt32(cmd.ExecuteScalar());
                        closeConnection();
                        return jlhKoin;
                    }
                }
                return -1;
            }

            public bool addUserCoin(string userID, int newCoin) {
                if (openConnection()) {
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = connection;
                    cmd.CommandText = $"select * from user where userID='{userID}'";
                    MySqlDataReader dataReader = cmd.ExecuteReader();
                    dataReader.Read();
                    int coin = (int)dataReader["coin"];
                    dataReader.Close();

                    cmd.CommandText = $"update user set coin = {coin+newCoin} where userID='{userID}'";
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

            //dipakai di halaman cards. untuk mengembalikan 2 hasil untuk di scrollview atas dan bawah
            public Dictionary<string, List<Dictionary<string, object>>> getAllCards(int deckID) {
                List<Dictionary<string, object>> retDeck = new List<Dictionary<string, object>>();
                List<Dictionary<string, object>> retSisa = new List<Dictionary<string, object>>();

                if (openConnection()) {

                    var getCardInDeck = $"select CardOwnership.CardID, CardOwnership.OwnershipID, CardInDeck.Count from CardInDeck inner join CardOwnership on CardInDeck.OwnershipID = CardOwnership.OwnershipID where CardInDeck.DeckID = {deckID}";
                    MySqlCommand cmd = new MySqlCommand(getCardInDeck, connection);
                    MySqlDataReader dataReader1 = cmd.ExecuteReader();
                    while (dataReader1.Read()) {
                        retDeck.Add(new Dictionary<string, object> { { "CardID", dataReader1["CardID"] }, { "OwnershipID", dataReader1["OwnershipID"] }, { "Count", dataReader1["Count"] } });
                    }
                    dataReader1.Close();

                    //============================
                    string UserID = Convert.ToString(new MySqlCommand($"select UserID from DeckInfo where DeckID={deckID}", connection).ExecuteScalar());

                    var getSisaKartu = $"select CardOwnership.CardID, CardOwnership.OwnershipID, COALESCE(CardOwnership.Count-bTab.Count,CardOwnership.Count) as Count from CardOwnership left join (select * from CardInDeck where CardInDeck.DeckID = {deckID}) as bTab on CardOwnership.OwnershipID = bTab.OwnershipID where CardOwnership.UserID = '{UserID}' and COALESCE(CardOwnership.Count-bTab.Count,CardOwnership.Count) != 0;";
                    cmd = new MySqlCommand(getSisaKartu, connection);
                    MySqlDataReader dataReader2 = cmd.ExecuteReader();
                    while (dataReader2.Read())
                    {
                        retSisa.Add(new Dictionary<string, object> { { "CardID", dataReader2["CardID"] }, { "OwnershipID", dataReader2["OwnershipID"] }, { "Count", dataReader2["Count"] } });
                    }
                    dataReader2.Close();

                    closeConnection();
                }
                return new Dictionary<string, List<Dictionary<string, object>>>(){ { "retDeck", retDeck }, { "retSisa", retSisa } };
            }

            public bool updateDeck(int DeckID, IList<CardInDeckMap> newCard) {
                if (openConnection())
                {

                    var deletePreviousData = $"delete from CardInDeck where DeckID={DeckID}";
                    MySqlCommand cmd = new MySqlCommand(deletePreviousData, connection);
                    cmd.ExecuteNonQuery();

                    string batchInsert = "INSERT INTO CardInDeck VALUES";
                    bool isFirst = true;
                    foreach (CardInDeckMap data in newCard)
                    {
                        if (isFirst)
                        {
                            isFirst = false;
                            batchInsert += $"({data.OwnershipID}, {DeckID}, {data.Count})";
                        }
                        else
                        {
                            batchInsert += $",({data.OwnershipID}, {DeckID}, {data.Count})";
                        }
                    }
                    cmd = new MySqlCommand(batchInsert, connection);
                    cmd.ExecuteNonQuery();

                    closeConnection();
                    return true;
                }
                return false;
            }

            public bool buyCard(string UserID, string CardID, double discount) {
                int coin;
                int cardPrice;
                if (openConnection()) {

                    MySqlCommand cmd = new MySqlCommand($"select coin from user where userID='{UserID}'", connection);
                    coin = Convert.ToInt32(cmd.ExecuteScalar());

                    cmd = new MySqlCommand($"select MarketPrice from Card where CardID='{CardID}'", connection);
                    cardPrice = Convert.ToInt32(cmd.ExecuteScalar());

                    if (coin >= cardPrice + (cardPrice * discount))
                    {
                        //apakah user sudah punya kartu ini
                        cmd = new MySqlCommand($"select EXISTS(select * from CardOwnership where CardID='{CardID}' and UserID='{UserID}')", connection);
                        bool isExist = Convert.ToBoolean(cmd.ExecuteScalar());

                        //tambah jumlah di koleksi user
                        if (isExist)
                        {
                            cmd = new MySqlCommand($"update CardOwnership set Count=Count+1 where CardID='{CardID}' and UserID='{UserID}'", connection);
                        }
                        //masukkin baru ke koleksi
                        else
                        {
                            cmd = new MySqlCommand($"insert into CardOwnership values(NULL, '{CardID}', '{UserID}', 1)", connection);
                        }
                        cmd.ExecuteNonQuery();

                        //kurangi koin user
                        cmd = new MySqlCommand($"update user set coin=coin-{cardPrice + (cardPrice * discount)} where userID='{UserID}'", connection);
                        cmd.ExecuteNonQuery();
                        closeConnection();

                        return true;
                    }
                    else {
                        closeConnection();
                        return false;
                    }
                }
                return false;
            }

            public List<Dictionary<string, object>> getAllDeck(string UserID) {
                var data = new List<Dictionary<string, object>>();


                if (openConnection()) {

                    MySqlCommand cmd = new MySqlCommand($"select * from DeckInfo where UserID='{UserID}'", connection);
                    MySqlDataReader dataReader = cmd.ExecuteReader();
                    while (dataReader.Read()) {
                        data.Add(new Dictionary<string, object>
                        {
                            { "DeckID", dataReader["DeckID"]},
                            { "UserID", dataReader["UserID"]},
                            { "DeckName", dataReader["DeckName"]}
                        });
                    }
                    closeConnection();
                }

                return data;
            }

            public bool addNewDeck(string UserID, string NamaDeck) {
                if (openConnection())
                {

                    MySqlCommand cmd = new MySqlCommand($"insert into DeckInfo values(NULL, '{UserID}', '{NamaDeck}')", connection);
                    var res = cmd.ExecuteNonQuery();

                    closeConnection();

                    if (res > 0) return true;
                    else return false;
                }
                else return false;
            }

            public List<Dictionary<string, object>> getAllCardInShop()
            {
                List<Dictionary<string, object>> ret = new List<Dictionary<string, object>>();
                if (openConnection())
                {
                    MySqlCommand cmd = new MySqlCommand($"select Shop.CardID, Shop.Discount, Card.MarketPrice from Shop inner join Card on Shop.CardID=Card.CardID", connection);

                    MySqlDataReader dataReader = cmd.ExecuteReader();
                    while (dataReader.Read())
                    {
                        ret.Add(new Dictionary<string, object>
                        {
                            { "CardID", dataReader["CardID"] },
                            { "Discount", dataReader["Discount"] },
                            { "MarketPrice", dataReader["MarketPrice"] },
                        });
                    }

                    closeConnection();
                }
                return ret;
            }

            public bool ShuffleCard() {
                if (openConnection())
                {

                    MySqlCommand cmd = new MySqlCommand($"delete from Shop", connection);
                    cmd.ExecuteNonQuery();

                    cmd = new MySqlCommand($"SELECT * FROM Card ORDER BY RAND()LIMIT 3", connection);
                    MySqlDataReader dataReader = cmd.ExecuteReader();

                    string bulkInsertQuery = "insert into Shop values";
                    bool isFirst = true;
                    while (dataReader.Read()) {
                        if (isFirst) {
                            isFirst = false;
                            bulkInsertQuery+= $"('{dataReader["CardID"]}', 0)";
                        }
                        else bulkInsertQuery += $",('{dataReader["CardID"]}', 0)";
                    }
                    dataReader.Close();
                    cmd = new MySqlCommand(bulkInsertQuery, connection);
                    var res = cmd.ExecuteNonQuery();
                    closeConnection();

                    if (res > 0) return true;

                    return false;
                }
                return false;
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
