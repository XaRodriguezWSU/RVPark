using Microsoft.EntityFrameworkCore;
using RVSite.Models;

namespace RVSite.Services
{
    public class ReservationPolicyValidationResult
    {
        public List<string> Errors { get; set; } = new();

        public bool IsValid
        {
            get { return !Errors.Any(); }
        }
    }

    public class ReservationPolicyService
    {
        private readonly AppDbContext _context;

        public ReservationPolicyService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ReservationPolicyValidationResult> ValidateReservationAsync(
            DateTime checkInDate,
            DateTime checkOutDate,
            int userId,
            int siteId,
            int? reservationIdToExclude = null)
        {
            var result = new ReservationPolicyValidationResult();

            checkInDate = checkInDate.Date;
            checkOutDate = checkOutDate.Date;

            var policy = await _context.ReservationPolicies.FirstOrDefaultAsync()
                         ?? new ReservationPolicy();

            var site = await _context.Sites
                .Include(s => s.SiteType)
                .FirstOrDefaultAsync(s => s.SiteID == siteId);

            if (checkInDate < DateTime.Today)
            {
                result.Errors.Add("The check-in date cannot be in the past.");
            }

            if (checkOutDate <= checkInDate)
            {
                result.Errors.Add("The check-out date must be after the check-in date.");
                return result;
            }

            if (checkInDate > DateTime.Today.AddDays(policy.MaximumAdvanceBookingDays))
            {
                result.Errors.Add(
                    $"Reservations cannot be made more than {policy.MaximumAdvanceBookingMonths} month(s) in advance.");
            }

            int stayNights = (checkOutDate - checkInDate).Days;

            var specialEventPolicy = await GetMatchingSpecialEventPolicyAsync(
                checkInDate,
                checkOutDate,
                site?.SiteTypeID);

            int maximumStayNights = specialEventPolicy?.MaximumStayNights
                                    ?? policy.PeakSeasonMaximumStayNights;

            if (stayNights > maximumStayNights)
            {
                if (specialEventPolicy != null)
                {
                    result.Errors.Add(
                        $"This reservation overlaps the special event policy '{specialEventPolicy.EventName}', which allows a maximum stay of {maximumStayNights} night(s).");
                }
                else
                {
                    result.Errors.Add(
                        $"The maximum stay is {maximumStayNights} night(s).");
                }
            }

            if (policy.RequiredDaysAwayBeforeReturn > 0)
            {
                DateTime gapStart = checkInDate.AddDays(-policy.RequiredDaysAwayBeforeReturn);
                DateTime gapEnd = checkOutDate.AddDays(policy.RequiredDaysAwayBeforeReturn);

                var userReservations = _context.Reservations
                    .Where(r =>
                        r.UserID == userId &&
                        r.ReservationStatus != ReservationStatus.Cancelled);

                if (reservationIdToExclude.HasValue)
                {
                    userReservations = userReservations
                        .Where(r => r.ReservationID != reservationIdToExclude.Value);
                }

                bool violatesReturnRule = await userReservations.AnyAsync(r =>
                    r.CheckOutDate > gapStart &&
                    r.CheckInDate < gapEnd);

                if (violatesReturnRule)
                {
                    result.Errors.Add(
                        $"This customer must wait {policy.RequiredDaysAwayBeforeReturn} day(s) between each reservation.");
                }
            }

            return result;
        }

        private async Task<SpecialEventPolicy?> GetMatchingSpecialEventPolicyAsync(
            DateTime checkInDate,
            DateTime checkOutDate,
            int? siteTypeId)
        {
            return await _context.SpecialEventPolicies
                .Include(p => p.SiteType)
                .Where(p =>
                    p.IsActive &&
                    p.StartDate.Date < checkOutDate.Date &&
                    checkInDate.Date <= p.EndDate.Date &&
                    (p.SiteTypeID == null || p.SiteTypeID == siteTypeId))
                .OrderByDescending(p => p.SiteTypeID != null)
                .ThenBy(p => p.StartDate)
                .FirstOrDefaultAsync();
        }
    }
}
