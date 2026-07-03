namespace GoKidAPI.Services.LevelProgression
{
    public interface ILevelProgressionService
    {
        Task CheckAndUpdateLevelAsync(string childId, string updatedBy);
    }
}
