namespace RaceDayPOE.Models
{
    //The lifecycle state of an enrolment. Stored in the database as text.
    public enum EnrolmentStatus
    {
        Pending,
        Confirmed,
        Cancelled
    }
}