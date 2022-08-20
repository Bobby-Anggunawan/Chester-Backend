using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using chesterBackendNet31.Models;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;

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

        //dipanggil untuk login
        [HttpPost]
        [Route("login/{userID}")]
        public JsonResult UserLogin(string userID) {
            try {
                var data = new ChesterDatabase.ChesterMySQL().addUser(userID);
                return new JsonResult(new Dictionary<string, object>() { { "userID", userID }, {"coin", data } });
            }
            catch{
                return new JsonResult(new Dictionary<string, object>() { { "userID", userID }, { "coin", -3 } });
            }
        }

        //dipanggil untuk menambah koin
        [HttpPut]
        [Route("addCoin/{userID}/{coin}")]
        public String TambahKoinUser(string userID, string coin) {
            var data = new ChesterDatabase.ChesterMySQL().addUserCoin(userID, Convert.ToInt32(coin));
            if (data) return "koin ditambah";
            return "koin gagal ditambah";
        }


        [HttpGet]
        public JsonResult PrintSemuaUser() {
            var data = new ChesterDatabase.ChesterMySQL().getUser();
            return new JsonResult(data);
        }
    }

    [ApiController]
    [Route("[controller]")]
    public class CardController : ControllerBase {
        [HttpGet("{DeckID}")]
        public JsonResult GetAllCards(int DeckID) {
            try
            {
                var data = new ChesterDatabase.ChesterMySQL().getAllCards(DeckID);
                return new JsonResult(data);
            }
            catch (Exception e) {
                var ret = new Dictionary<string, string>() {
                    {e.GetType().ToString(), e.Message }
                };
                return new JsonResult(ret);
            }
        }

        [HttpPut]
        public string UpdateDeck([FromBody] object jsonData) {
            int debugPos = 0;
            try
            {
                var data = JsonConvert.DeserializeObject<UpdateDeckData>(jsonData.ToString());
                debugPos = 4;
                var ret = new ChesterDatabase.ChesterMySQL().updateDeck(data.DeckID, data.data);
                debugPos = 5;
                if (ret) return "berhasil";
                return "gagal";
            }
            catch (Exception e) {
                return $"{jsonData.ToString()}\n\nPosisi debug pos: {debugPos}\n{e.GetType().Name}\n{e.Message}\n{e.StackTrace}";
            }
        }
    }


    [ApiController]
    [Route("[controller]")]
    public class DeckController : ControllerBase {
        [HttpGet("{userID}")]
        public JsonResult getDeckList(string userID)
        {
            return new JsonResult(new ChesterDatabase.ChesterMySQL().getAllDeck(userID));
        }

        [HttpPost]
        [Route("create/{UserID}/{NamaDeck}")]
        public string createNewDeck(string UserID, string NamaDeck) {
            try
            {
                bool ret = new ChesterDatabase.ChesterMySQL().addNewDeck(UserID, NamaDeck);
                if (ret) return "sukses";
                return "gagal";
            }
            catch (Exception e) {
                return $"{e.GetType().Name}\n{e.Message}\n{e.StackTrace}";
            }
        }

    }

    [ApiController]
    [Route("[controller]")]
    public class ShopController : ControllerBase {
        [HttpPut]
        public string BuyCard([FromBody] object jsonData) {
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData.ToString());

            var ret = new ChesterDatabase.ChesterMySQL().buyCard(Convert.ToString(data["UserID"]),
                                                                    Convert.ToString(data["CardID"]),
                                                                    Convert.ToDouble(data["discount"]));
            if (ret) return "berhasil";
            else return "gagal";
        }

        [HttpGet]
        public string GetCardList() {
            return JsonConvert.SerializeObject(new ChesterDatabase.ChesterMySQL().getAllCardInShop());
        }

        //untuk ngacak otomatis item yang dijual di shop
        [HttpPut]
        [Route("shuffle")]
        public string ShuffleCard() {
            var res = new ChesterDatabase.ChesterMySQL().ShuffleCard();
            if (res) return "sukses";
            else return "gagal";
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
            return new JsonResult(
                new Dictionary<string, string> { {"Error: ", "404" } }
            );
        }
    }

}
