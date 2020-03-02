using System;
using TrainingSessionManagement.Business;

namespace TrainingSessionManagement
{
    class Program
    {
        static void Main(string[] args)
        {
            var fileReader = new FileReader();
            var sessionManagerBusiness = new SessionManagerBusiness(fileReader);
            var tracks = sessionManagerBusiness.GetTracks();

            foreach (var track in tracks)
            {
                Console.WriteLine();
                Console.WriteLine(track.TrackName);
                Console.WriteLine("|{0,15}|{1,60}|{2,10}|", "Time", "Session Name", "Duration");
                Console.WriteLine("| _____________ | __________________________________________________________ | __________");
                foreach (var session in track.Sessions)
                {
                    var item = string.Format("|{0,15}|{1,60}|{2,10}|", session.StartTimeText, session.SessionName, session.Duration);
                    Console.WriteLine(item);
                }
            }
        }
    }
}
