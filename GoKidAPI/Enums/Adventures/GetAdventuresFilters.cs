using GoKidAPI.Shared;

namespace GoKidAPI.Enums.Adventures
{
    public class GetAdventuresFilters : RequestFilters<AdventureSortingColumn>
    {
        public AdventureStatus? Status { get; set; }
        public string? SearchTitle { get; set; }
    }
}
