using RVSite.Models;
using Microsoft.EntityFrameworkCore;

namespace RVSite.Services
{
    public class CostService
    {
        private readonly AppDbContext _context;

        public CostService(AppDbContext context)
        {
            _context = context;
        }

        public decimal CalculateCost(Reservation reservation)
        {
            if (reservation.Site == null)
            {
                return 0;
            }

            int nights = (reservation.CheckOutDate.Date - reservation.CheckInDate.Date).Days;

            if (nights < 0)
            {
                nights = 0;
            }

            return nights * reservation.Site.BaseRate;
        }

        public async Task<decimal> CalculateFeeTotalAsync(int reservationID)
        {
            return await _context.Fees
                .Where(f => f.ReservationID == reservationID)
                .SumAsync(f => f.Amount);
        }

        public async Task<decimal> CalculatePaidTotalAsync(int reservationID)
        {
            return await _context.Payments
                .Where(p =>
                    p.ReservationID == reservationID &&
                    p.Status == PaymentStatus.Paid)
                .SumAsync(p => p.AmountPaid);
        }

        public async Task UpdateReservationBalanceDueAsync(int reservationID)
        {
            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.ReservationID == reservationID);

            if (reservation == null)
            {
                return;
            }

            decimal feeTotal = await CalculateFeeTotalAsync(reservationID);
            decimal paidTotal = await CalculatePaidTotalAsync(reservationID);

            reservation.BalanceDue = Math.Max(
                0,
                reservation.TotalCost + feeTotal - paidTotal);
        }
    }
}
