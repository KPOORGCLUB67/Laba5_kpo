using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PCClubBooking.Core.Models
{
    public class Service
    {
        public int Id { get; set; }
        public string Name { get; set; } = "напиток, снек";   // напиток, снек
        public decimal Price { get; set; }
    }
}
