namespace RaceDayPOE.Models
{
    /// <summary>
    /// The two application roles. A person is EITHER an Organiser OR a Participant -
    /// they live in separate tables, so nobody can hold both roles.
    /// </summary>
    public enum UserRole
    {
        Organiser,
        Participant
    }
}