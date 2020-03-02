using System.Collections.Generic;
using TrainingSessionManagement.Model;

namespace TrainingSessionManagement.Business
{
    public interface ISessionManagerBusiness
    {
        List<TrackViewModel> GetTracks();
    }
}
