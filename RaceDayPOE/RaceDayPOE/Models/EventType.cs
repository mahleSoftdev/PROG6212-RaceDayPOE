namespace RaceDayPOE.Models
{
    /// <summary>
    /// The kind of race an event is. Stored in the database as text ("Run", "Walk", "Cycle").
    /// </summary>
    public enum EventType
    {
        Run,
        Walk,
        Cycle
    }
}