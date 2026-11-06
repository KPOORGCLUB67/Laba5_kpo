using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PCClubBooking.Core.Models
{
    public class Tariff
    {
        public int Id { get; set; }
        public PcCategory Category { get; set; }
        public decimal PricePerHour { get; set; }
        public decimal DiscountPercent { get; set; } // акции/скидки
    }
}
