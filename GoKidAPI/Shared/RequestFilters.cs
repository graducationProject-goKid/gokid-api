using GoKidAPI.Enums.Shared;

namespace GoKidAPI.Shared
{
    public class RequestFilters<TSortColumn>
    where TSortColumn : struct, Enum
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public TSortColumn? SortColumn { get; set; }
        public SortDirection? SortDirection { get; set; } = Enums.Shared.SortDirection.ASC;
    }
}
