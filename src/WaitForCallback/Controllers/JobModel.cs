namespace WaitForCallback.Controllers
{
    public class JobModel
    {
        public Guid Id { get; set; }

        public DateTime Timestamp { get; set; }

        public string? Status { get; set; }
    }
}
