using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.Firestore;

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

        //===============================================
        static string projectID = "chester-game";
        FirestoreDb db = FirestoreDb.Create(projectID);
        //===============================================

        private readonly ILogger<MatchmakingController> _logger;

        public MatchmakingController(ILogger<MatchmakingController> logger)
        {
            _logger = logger;
        }

        [HttpGet("{userID}")]
        public JsonResult Get(string userID) {
            CollectionReference usersRef = db.Collection("duel");
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
                DocumentReference docRef = db.Collection("duel").Document(ret);
                docRef.UpdateAsync(new Dictionary<string, object> { { "redPlayerID", userID } });
                isBlue = false;
            }
            else
            {
                DocumentReference docRef = db.Collection("duel").Document();
                ret = docRef.Id;
                docRef.SetAsync(new Dictionary<string, object>{
                    { "bluePlayerID", userID}
                });
            }

            return new JsonResult(new Dictionary<string, object> { {"roomID", ret }, {"isBlue", isBlue} });
        }
    }
}
