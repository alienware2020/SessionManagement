using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrainingSessionManagement.Model
{
    public class SessionInputViewModel
    {
        public string SessionName { get; set; }
        public string SessionDuration { get; set; }
        public int DurationMinutes { get; set; }
        public bool IsAllocated { get; set; }
    }
}
