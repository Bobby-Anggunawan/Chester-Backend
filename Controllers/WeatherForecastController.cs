using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using MySql.Data.MySqlClient;

namespace chesterBackendNet31.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }

    [ApiController]
    [Route("[controller]")]
    public class MatchmakingController : ControllerBase {

        private readonly ILogger<MatchmakingController> _logger;

        public MatchmakingController(ILogger<MatchmakingController> logger)
        {
            _logger = logger;
        }

        [HttpGet("{userID}")]
        public JsonResult Get(string userID) {
            CollectionReference usersRef = ChesterDatabase.firestoreInstance.Collection("duel");
            QuerySnapshot snapshot = usersRef.GetSnapshotAsync().Result;

            string ret = "";
            bool isBlue = true;

            bool selesaiCari = false;
            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                Dictionary<string, object> documentDictionary = document.ToDictionary();

                if (!documentDictionary.ContainsKey("redPlayerID"))
                {
                    selesaiCari = true;
                    ret = document.Id;
                    break;
                }
            }

            if (selesaiCari)
            {
                DocumentReference docRef = ChesterDatabase.firestoreInstance.Collection("duel").Document(ret);
                docRef.UpdateAsync(new Dictionary<string, object> { { "redPlayerID", userID } });
                isBlue = false;
            }
            else
            {
                DocumentReference docRef = ChesterDatabase.firestoreInstance.Collection("duel").Document();
                ret = docRef.Id;
                docRef.SetAsync(new Dictionary<string, object>{
                    { "bluePlayerID", userID}
                });
            }

            return new JsonResult(new Dictionary<string, object> { {"roomID", ret }, {"isBlue", isBlue} });
        }
    }

    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase {
        private readonly ILogger<UserController> _logger;

        public UserController(ILogger<UserController> logger)
        {
            _logger = logger;
        }

        [HttpPost("{userID}")]
        public String Post(string userID) {
            var data = new ChesterDatabase.ChesterMySQL().addUser(userID);
            if (data) return "Berhasil dirambah";
            return "Gagal dittambah";
        }

        [HttpGet]
        public JsonResult Get() {
            var data = new ChesterDatabase.ChesterMySQL().getUser();
            return new JsonResult(data);
        }
    }

    [ApiController]
    [Route("/")]
    public class TestController : ControllerBase {
        private readonly ILogger<TestController> _logger;

        public TestController(ILogger<TestController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public JsonResult Get() {
            
            string dbSocketDir = Environment.GetEnvironmentVariable("DB_SOCKET_PATH") ?? "/cloudsql";
            string instanceConnectionName = Environment.GetEnvironmentVariable("INSTANCE_CONNECTION_NAME");
            string UserID = Environment.GetEnvironmentVariable("DB_USER");   // e.g. 'my-db-user
            string Password = Environment.GetEnvironmentVariable("DB_PASS");
            string Database = Environment.GetEnvironmentVariable("DB_NAME");
            return new JsonResult(
                new Dictionary<string, string> { { "dbSocketDir", dbSocketDir }, { "instanceConnectionName", instanceConnectionName }, { "UserID", UserID }, { "Password", Password }, { "Database", Database }, {"Connection String", ChesterDatabase.ChesterMySQL.getConnectionString() } }
            );
            /*
            string ret = "";
            var connection = new MySqlConnection(ChesterDatabase.ChesterMySQL.getConnectionString());

            try
            {
                connection.Open();
                ret = "berhasil";
            }
            catch(Exception e)
            {
                ret = e.StackTrace;
            }

            ret += "\n\n"+Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
            ret += "\n\n" + "Connection string = "+ ChesterDatabase.ChesterMySQL.getConnectionString();

            return ret;*/
        }
    }

}
