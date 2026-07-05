using RVSite.Models;

namespace RVSite.Services
{
    public class CostService
    {
        public decimal CalculateCost(Reservation r)
        {
            int nights = (r.CheckOutDate - r.CheckInDate).Days;
            return nights * r.Site.BaseRate;
        }
    }
}
