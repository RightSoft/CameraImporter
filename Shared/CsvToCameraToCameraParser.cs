using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CameraImporter.Model;
using CameraImporter.Model.Genetec;
using CameraImporter.Shared.Interface;
using CsvHelper;
using CsvHelper.Configuration;

namespace CameraImporter.Shared
{
    public class CsvToCameraToCameraParser : ICsvToCameraParser
    {

        public List<GenetecCamera> Parse(string content)
        {
            if (string.IsNullOrEmpty(content))
                throw new Exception(ExceptionMessage.ContentIsNullOrEmpty);
                
            return LoadFromCSV<GenetecCamera>(content).ToList();
        }

        private T[] LoadFromCSV<T>(string content)
        {

            var csvConfiguration = new Configuration()
            {
                Delimiter = ",",
                CultureInfo = new CultureInfo("en-GB"),
                HeaderValidated = null,
                HasHeaderRecord = true,
                MissingFieldFound = null
            };

            TextReader reader = new StringReader(content);
            CsvReader csvReader = new CsvReader(reader, csvConfiguration);
            var result = csvReader.GetRecords<T>();
            return result.ToArray();
        }
    }
}