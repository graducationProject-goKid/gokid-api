using GoKidAPI.Enums;
using GoKidAPI.Shared;

namespace GoKidAPI.DTO.InstitutionAdmin.Supervisor.Requests
{
    public class GetSupervisorsFilters : RequestFilters<SupervisorSortingColumn>
    {
        public string? SearchTerm { get; set; }
        public string? ClassId { get; set; } // optional

    }
}
