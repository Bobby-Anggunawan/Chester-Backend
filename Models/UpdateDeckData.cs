using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chesterBackendNet31.Models
{

    public class CardInDeckMap {
        public int OwnershipID { get; set; }
        public int Count { get; set; }
    }

    public class UpdateDeckData
    {
        public int DeckID { get; set; }
        public IList<CardInDeckMap> data { get; set; }
    }
}
