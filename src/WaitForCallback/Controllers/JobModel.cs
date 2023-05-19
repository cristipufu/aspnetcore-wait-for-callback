namespace WaitForCallback.Controllers
{
    public class JobModel
    {
        public Guid Id { get; set; }

        public DateTime? Timestamp { get; set; }

        public DateTime? CompletedTimestamp { get; set; }

        public TimeSpan? Duration { get; set; }

        public string? Status { get; set; }
    }
}
