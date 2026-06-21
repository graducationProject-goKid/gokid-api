using GoKidAPI.Enums;
using GoKidAPI.Shared;

namespace GoKidAPI.DTO.Classes.Requests
{
    public class GetClassesFilters : RequestFilters<ClassSortingColumn>
    {
        public string? SearchName { get; set; }  // optional search by class name
    }
}
