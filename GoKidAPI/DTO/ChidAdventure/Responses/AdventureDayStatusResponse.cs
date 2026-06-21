namespace GoKidAPI.DTO.ChidAdventure.Responses
{
    public class AdventureDayStatusResponse
    {
        public int DayNumber { get; set; }
        public AdventureDayStatus Status { get; set; }
    }

    public enum AdventureDayStatus
    {
        Locked = 1,       // اليوم لسه ما جاش
        Unlocked = 2,     // اليوم ده ينفع يعمل Submit
        Completed = 3,    // عمل التاسك وخلص
        Missed = 4        // فات اليوم ومعملش
    }
}
