using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrainingSessionManagement.Model
{
    public class TrackViewModel
    {
        public TrackViewModel()
        {
            Sessions = new List<Session>();
        }
        public int TrackId { get; set; }
        public string TrackName { get; set; }
        public List<Session> Sessions { get; set; }
    }

    public class Session
    {
        public string SessionName { get; set; }
        public string Duration { get; set; }
        public string StartTimeText { get; set; }
        public int StartTime { get; set; }
    }
}
