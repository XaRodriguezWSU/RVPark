namespace RVSite.Models
{
    public enum FeeCodes
    {
        Damages,
        Maintenance,
        // keep late payment and late check out? what are the odds of late payment?
        LatePayment,
        Cancellation,
        EarlyCheckIn,
        LateCheckOut
    }
}