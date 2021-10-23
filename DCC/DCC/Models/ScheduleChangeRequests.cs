

namespace DCC.Models
{
    public class ScheduleChangeRequest
    {
        public string requestor { get; set; }
        public string requestedTime { get; set; }
        public string provider { get; set; }
        public string client { get; set; }
        public string svc { get; set; }
        public string changeRequest { get; set; }
       

        public int scheduleId { get; set; }

    }
}