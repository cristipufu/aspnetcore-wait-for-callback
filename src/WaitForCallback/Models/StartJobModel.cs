using System.ComponentModel;

namespace WaitForCallback.Models
{
    public class StartJobModel
    {
        [DefaultValue(true)]
        public bool WaitForResponse { get; set; }

        [DefaultValue(10)]
        public int WaitForTimeoutSeconds { get; set; }
    }
}
