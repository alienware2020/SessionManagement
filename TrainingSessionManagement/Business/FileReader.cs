using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TrainingSessionManagement.Model;

namespace TrainingSessionManagement.Business
{
    public class FileReader : IFileReader
    {
        public string GetFileContent(string fileName)
        {
            try
            {
                var reader = new StreamReader(fileName, Encoding.Default);
                var fileContents = reader.ReadToEnd().Trim();
                return fileContents;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Not a valid file");
                throw ex;
            }
        }

        public List<SessionInputViewModel> GetInput(string fileName)
        {
            var fileContents = GetFileContent(fileName);
            var sessionInput = JsonConvert.DeserializeObject<List<SessionInputViewModel>>(fileContents);
            return sessionInput;
        }
    }
}
