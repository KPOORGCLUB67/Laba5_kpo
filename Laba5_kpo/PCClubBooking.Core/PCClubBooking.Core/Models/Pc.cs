using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PCClubBooking.Core.Models
{
    public enum PcStatus { Free, Busy, Booked, Broken }
    public enum PcCategory { Standard, Gaming, Vip }

    public class Pc
    {
        public int Id { get; set; }
        public string Number { get; set; } = "";          // номер ПК
        public string Cpu { get; set; } = "";
        public string Gpu { get; set; } = "";
        public int RamGb { get; set; }
        public string Monitor { get; set; } = "";         // диагональ + разрешение
        public string Periphery { get; set; } = "";
        public PcCategory Category { get; set; }
        public PcStatus Status { get; set; } = PcStatus.Free;
    }
}
