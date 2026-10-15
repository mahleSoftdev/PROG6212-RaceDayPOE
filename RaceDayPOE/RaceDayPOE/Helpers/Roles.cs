namespace RaceDayPOE.Helpers
{
    /// <summary>
    /// Role names as constants so [Authorize(Roles = Roles.Organiser)] never has a typo.
    /// These strings must match the UserRole enum names, because they are written into the JWT "role" claim.
    /// </summary>
    public static class Roles
    {
        public const string Organiser = "Organiser";
        public const string Participant = "Participant";
    }
}