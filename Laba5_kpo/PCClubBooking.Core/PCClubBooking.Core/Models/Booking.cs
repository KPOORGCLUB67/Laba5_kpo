using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PCClubBooking.Core.Models
{
    public class Booking
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int PcId { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public decimal TotalPrice { get; set; }
        public bool IsCancelled { get; set; }

        public double DurationHours => (End - Start).TotalHours;
    }
}
