namespace RaceDayPOE.Models
{
    //The two application roles. A person is EITHER an Organiser OR a Participant -
    //they live in separate tables, so nobody can hold both roles.
   
    public enum UserRole
    {
        Organiser,
        Participant
    }
}