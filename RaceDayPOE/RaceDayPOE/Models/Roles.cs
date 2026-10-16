namespace RaceDayPOE.Helpers
{
    // Role names as constants so [Authorize(Roles = Roles.Organiser)] never has a typo.
    // These strings must match the UserRole enum names, because they are written into the authentication cookie's role claim.
    
    public static class Roles
    {
        public const string Organiser = "Organiser";
        public const string Participant = "Participant";
    }
}