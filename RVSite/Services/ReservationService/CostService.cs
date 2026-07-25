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
            var fees = await _context.Fees
                .Where(f => f.ReservationID == reservationID)
                .ToListAsync();

            return fees.Sum(f => CalculateFeeAmount(f));
        }

        public decimal CalculateFeeAmount(Fee fee)
        {
            if (fee.NameCode == FeeCodes.Cancellation)
            {
                int daysAccrued = CalculateCancellationFeeDays(fee.EffectiveDate);

                return fee.Amount * daysAccrued;
            }

            return fee.Amount;
        }

        private int CalculateCancellationFeeDays(DateTime effectiveDate)
        {
            var startDate = effectiveDate.Date;
            var today = DateTime.Today;

            if (startDate > today)
            {
                return 0;
            }

            return (today - startDate).Days + 1;
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

            if (reservation.ReservationStatus == ReservationStatus.Cancelled)
            {
                var cancellationFees = await _context.Fees
                    .Where(f =>
                        f.ReservationID == reservationID &&
                        f.NameCode == FeeCodes.Cancellation)
                    .ToListAsync();

                decimal cancellationFeeTotal = cancellationFees
                    .Sum(f => CalculateFeeAmount(f));

                decimal paidCancellationTotal = 0;

                if (cancellationFees.Any())
                {
                    DateTime cancellationFeeStartDate = cancellationFees
                        .Min(f => f.EffectiveDate);

                    paidCancellationTotal = await _context.Payments
                        .Where(p =>
                            p.ReservationID == reservationID &&
                            p.Status == PaymentStatus.Paid &&
                            p.PaymentDate >= cancellationFeeStartDate)
                        .SumAsync(p => p.AmountPaid);
                }

                reservation.BalanceDue = Math.Max(
                    0,
                    cancellationFeeTotal - paidCancellationTotal);

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
