namespace GoKidAPI.DTO.Notifications
{
    public class NotificationListResponse
    {
        public List<NotificationResponse> Notifications { get; set; } = [];

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public bool HasNextPage { get; set; }
    }
}
