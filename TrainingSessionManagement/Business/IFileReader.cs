using System.Collections.Generic;
using TrainingSessionManagement.Model;

namespace TrainingSessionManagement.Business
{
    public interface IFileReader
    {
        string GetFileContent(string fileName);
        List<SessionInputViewModel> GetInput(string fileName);
    }
}
