namespace RaceDayPOE.Models
{
    /// <summary>
    /// The lifecycle state of an enrolment. Stored in the database as text.
    /// </summary>
    public enum EnrolmentStatus
    {
        Pending,
        Confirmed,
        Cancelled
    }
}