﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using H.Content;
using H.Core.Converters;
using H.Core.Enumerations;
using H.Infrastructure;

namespace H.Core.Providers.Energy
{
    public class FuelEnergyEstimatesProvider
    {
        #region Fields

        private readonly ProvinceStringConverter _provinceStringConverter;
        private readonly SoilFunctionalCategoryStringConverter _soilFunctionalCategoryStringConverter;
        private readonly TillageTypeStringConverter _tillageTypeStringConverter;
        private readonly CropTypeStringConverter _cropTypeStringConverter;

        // Sets the default value for when no fuel energy estimate value is available for a cell
        private const double DefaultValue = 0.0;

        #endregion

        #region Constructors
        
        public FuelEnergyEstimatesProvider()
        {
            _provinceStringConverter = new ProvinceStringConverter();
            _soilFunctionalCategoryStringConverter = new SoilFunctionalCategoryStringConverter();
            _tillageTypeStringConverter = new TillageTypeStringConverter();
            _cropTypeStringConverter = new CropTypeStringConverter();

            this.Data = this.ReadFile();
        }

        #endregion

        #region Properties
        // List that stores all instances of FuelEnergyData. Each instance corresponds to a given province, soil category, tillage type and crop.
        private List<FuelEnergyEstimatesData> Data { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// This method takes in a set of characteristics and finds the fuel energy estimate given those characteristics.
        /// </summary>
        /// <param name="province">The province for which fuel energy estimate data is required</param>
        /// <param name="soilCategory">The functional soil category for the province and crop</param>
        /// <param name="tillageType">The tillage type used for the crop</param>
        /// <param name="cropType">The type of crop for which fuel energy estimate data is required</param>
        /// <returns> The method returns an instance of FuelEnergyEstimateData based on the characteristics in the parameters. Returns null if nothing found
        ///  Unit of measurement of fuel energy estimate value = GJ ha-1</returns>
        public FuelEnergyEstimatesData GetFuelEnergyEstimatesDataInstance(Province province, SoilFunctionalCategory soilCategory, TillageType tillageType, CropType cropType)
        {
            var soilLookupType = soilCategory.GetSimplifiedSoilCategory();

            FuelEnergyEstimatesData data = this.Data.Find(x => (x.Province == province) && (x.SoilFunctionalCategory == soilLookupType)
                                                            && (x.TillageType == tillageType) && (x.CropType == cropType));
            
            // If instance is found return the instance
            if (data != null)
            {
                return data;
            }

            // If instance is not found, we do a few searches to help trace which input parameter was wrong.
            data = this.Data.Find(x => (x.Province == province) && (x.SoilFunctionalCategory == soilLookupType) && (x.TillageType == tillageType));

            // The specified crop type was wrong
            if (data != null)
            {
                Trace.TraceError($"{nameof(FuelEnergyEstimatesProvider)}.{nameof(FuelEnergyEstimatesProvider.GetFuelEnergyEstimatesDataInstance)}" +
                                 $" unable to find Crop: {cropType} in the available crop type data. Returning null");
                return null;
            }

            data = this.Data.Find(x => (x.Province == province) && (x.TillageType == tillageType) && (x.CropType == cropType));

            // The specified soil category was wrong
            if (data != null)
            {
                Trace.TraceError($"{nameof(FuelEnergyEstimatesProvider)}.{nameof(FuelEnergyEstimatesProvider.GetFuelEnergyEstimatesDataInstance)}" +
                                 $" unable to find Soil Category: {soilLookupType} in the available soil data. Returning null");
            }

            // The specified province type was wrong
            else
            {
                Trace.TraceError($"{nameof(FuelEnergyEstimatesProvider)}.{nameof(FuelEnergyEstimatesProvider.GetFuelEnergyEstimatesDataInstance)}" +
                                 $" unable to find Province: {province} in the available province data. Returning null");
            }

            return null;
        }


        #endregion

        #region Private Methods

        /// <summary>
        /// Reads the Comma Separated Value (.csv) file containing the required data. 
        /// </summary>
        /// <returns>Returns a List of FuelEnergyEstimatesData instances. Each instance contains properties that are read from the csv file.</returns>
        private List<FuelEnergyEstimatesData> ReadFile()
        {
            var results = new List<FuelEnergyEstimatesData>();

            IEnumerable<string[]> fileLines = CsvResourceReader.GetFileLines(CsvResourceNames.FuelEnergyEstimates);

            // A new dictionary is created to store the column headers of the file.
            // Key = Cell location of an entry
            // Value = A custom class containing characteristics read from given file.
            Dictionary<int, FuelEnergyDataCharacteristics> dataLocation = GetDataLocation(fileLines);


            // We skip over the first three lines as they are already read above. We then move to each cell
            // and extract data from that cell. If a cell does not contain any data the value of DefaultValue is stored in the instance.
            foreach (string[] line in fileLines.Skip(3))
            {
                foreach (KeyValuePair<int, FuelEnergyDataCharacteristics> item in dataLocation)
                {
                    int columnNumber = item.Key;
                    double fuelEnergyEstimate = CheckCellValue(line, columnNumber);

                    Province provinceName = item.Value.Province;
                    SoilFunctionalCategory soilFunctionalCategory = item.Value.SoilFunctionalCategory;
                    TillageType tillageType = item.Value.TillageType;
                    CropType cropType = _cropTypeStringConverter.Convert(line[0]);


                    results.Add(new FuelEnergyEstimatesData
                    {
                        FuelEstimate = fuelEnergyEstimate,
                        Province = provinceName,
                        SoilFunctionalCategory = soilFunctionalCategory,
                        TillageType = tillageType,
                        CropType = cropType,
                    });
                }
            }
            return results;
        }

        /// <summary>
        /// Reads the header columns of the csv file and stores the results in a dictionary.
        /// </summary>
        /// <param name="fileLines">The collection of string arrays denoting each line in the csv file.</param>
        /// <param name="dataLocation">A dictionary that stores the location of a fuel energy estimate given a column. The key is the column number
        /// and the value is a custom class that stores the header rows (characteristics).</param>
        private Dictionary<int, FuelEnergyDataCharacteristics> GetDataLocation (IEnumerable<string[]> fileLines)
        {
            int numberOfColumns = fileLines.First().Length;
            var dataLocation = new Dictionary<int, FuelEnergyDataCharacteristics>();

            // Each string array denotes a row in the column. Is row is taken from the string array collection of the file's lines.
            string[] provinceNames = fileLines.ElementAt(0);
            string[] soilFunctionalCategory = fileLines.ElementAt(1);
            string[] tillageTypes = fileLines.ElementAt(2);

            // Iterate over each column and store the information in the rows into the custom class. This custom class is the dictionary key value.
            for (int columnNumber = 1; columnNumber < numberOfColumns; columnNumber++)
            {
                FuelEnergyDataCharacteristics energyCharacteristics = new FuelEnergyDataCharacteristics();

                energyCharacteristics.Province = _provinceStringConverter.Convert(provinceNames[columnNumber]);
                energyCharacteristics.SoilFunctionalCategory = _soilFunctionalCategoryStringConverter.Convert(soilFunctionalCategory[columnNumber]);
                energyCharacteristics.TillageType = _tillageTypeStringConverter.Convert(tillageTypes[columnNumber]);

                dataLocation.Add(columnNumber, energyCharacteristics);
            }

            return dataLocation;
        }

        /// <summary>
        /// Checks a given cell in the file to see if it contains a valid value.
        /// </summary>
        /// <param name="line">The line (row) in which the cell is located.</param>
        /// <param name="columnNumber">The column number in which the cell is located.</param>
        /// <returns>Returns the value of the cell. If the cell is empty, the default value of a cell is returned..</returns>
        private double CheckCellValue(string[] line, int columnNumber)
        {
            double value = DefaultValue;
            var cultureInfo = InfrastructureConstants.EnglishCultureInfo;

            if (!string.IsNullOrEmpty(line[columnNumber]))
            {
                value = double.Parse(line[columnNumber], cultureInfo);
            }

            return value;
        }

        #endregion

    }
}
