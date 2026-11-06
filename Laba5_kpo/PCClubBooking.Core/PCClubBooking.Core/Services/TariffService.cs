using PCClubBooking.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PCClubBooking.Core.Services
{
    public class TariffService
    {
        private readonly Dictionary<PcCategory, Tariff> _tariffs;

        public TariffService(IEnumerable<Tariff> tariffs)
            => _tariffs = tariffs.ToDictionary(t => t.Category);

        public decimal Calculate(PcCategory category, double hours)
        {
            if (!_tariffs.TryGetValue(category, out var t))
                throw new InvalidOperationException($"Тариф для {category} не задан");
            var basePrice = t.PricePerHour * (decimal)hours;
            return Math.Round(basePrice * (1 - t.DiscountPercent / 100m), 2);
        }
    }
}
