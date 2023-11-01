﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Castle.Components.DictionaryAdapter;
using H.Core.Emissions.Results;
using H.Core.Enumerations;
using H.Core.Models;
using H.Core.Models.Animals;
using H.Core.Models.Animals.Beef;
using H.Core.Models.LandManagement.Fields;
using H.Core.Services.Animals;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace H.Core.Test.Services
{
    [TestClass]
    public class ManureServiceTest : UnitTestBase
    {
        #region Fields

        private ManureService _sut;
        private Mock<IAnimalService> _mockAnimalService;

        #endregion

        #region Initialization

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
        }

        [TestInitialize]
        public void TestInitialize()
        {
            _mockAnimalService = new Mock<IAnimalService>();
            _sut = new ManureService(_mockAnimalService.Object);

            var componentResults = base.GetNonEmptyTestAnimalComponentEmissionsResults();
            _mockAnimalService.Setup(x => x.GetAnimalResults(It.IsAny<AnimalType>(), It.IsAny<Farm>())).Returns(new List<AnimalComponentEmissionsResults>() { componentResults });
        }

        [TestCleanup]
        public void TestCleanup()
        {
        }

        #endregion

        #region Tests

        [TestMethod]
        public void InitializeTankTest()
        {
            var managementPeriod = new ManagementPeriod() {Start = DateTime.Now};

            managementPeriod.NumberOfDays = 365 * 12;
            managementPeriod.End = managementPeriod.Start.AddDays(managementPeriod.NumberOfDays);
            managementPeriod.Duration = managementPeriod.End.Subtract(managementPeriod.Start);

            var group = new AnimalGroup();
            group.ManagementPeriods.Add(managementPeriod);

            var component = new FinishingComponent();
            component.Groups.Add(group);

            var farm = new Farm();
            farm.Components.Add(component);
        }

        [TestMethod]
        public void GetAmountAvailableForExportReturnsZeroWhenNoAnimalsPresentTest()
        {
            var result = _sut.GetAmountAvailableForExport(DateTime.Now.Year, new Farm());

            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void GetAmountAvailableForExportReturnsNonZeroWhenAnimalsArePresentTest()
        {
            var farm = base.GetTestFarm();
            farm.Components.Add(base.GetTestFieldComponent());
            _sut.CalculateResults(farm);

            var result = _sut.GetAmountAvailableForExport(DateTime.Now.Year, farm);

            Assert.IsTrue(result > 0);
        }

        [TestMethod]
        public void GetValidManureTypesReturnsCorrectCountTest()
        {
            var result = _sut.GetValidManureTypes();
            Assert.AreEqual(14, result.Count);
        }

        [TestMethod]
        public void GetAmountAvailableForExportReturnsNonZero()
        {
            var farm = base.GetTestFarm();
            _sut.CalculateResults(farm);

            var result = _sut.GetAmountAvailableForExport(DateTime.Now.Year, farm);

            Assert.AreEqual(200000, result);
        }

        [TestMethod]
        public void GetTotalAmountOfManureExportedReturnsZeroWhenNoExportsCreated()
        {
            var farm = base.GetTestFarm();
            farm.ManureExportViewItems.Clear();

            var result = _sut.GetTotalAmountOfManureExported(DateTime.Now.Year, farm, AnimalType.Beef);

            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void GetTotalAmountOfManureExportedReturnsNonZeroWhenExportsCreated()
        {
            var farm = base.GetTestFarm();

            var result = _sut.GetTotalAmountOfManureExported(DateTime.Now.Year, farm, AnimalType.Dairy);

            Assert.AreEqual(3000, result);
        }

        [TestMethod]
        public void GetValidManureStateTypesForImportedManureReturnsCorrectCount()
        {
            var result = _sut.GetValidManureStateTypes(new Farm(), ManureLocationSourceType.Imported, AnimalType.Beef);

            Assert.AreEqual(5, result.Count);
        }

        [TestMethod]
        public void GetValidManureStateTypesForOnFarmManureReturnsCorrectCount()
        {
            var farm = base.GetTestFarm();

            var result = _sut.GetValidManureStateTypes(farm, ManureLocationSourceType.Livestock, AnimalType.Beef);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(ManureStateType.AnaerobicDigester, result[0]);
        }

        [TestMethod]
        public void GetManureTypesProducedOnFarm()
        {
            var farm = base.GetTestFarm();

            var result = _sut.GetManureTypesProducedOnFarm(farm);

            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void GetYearWithMostManureProducedTest()
        {
            var farm = base.GetTestFarm();
            _sut.CalculateResults(farm);

            var result = _sut.GetYearHighestVolumeRemaining();

            Assert.AreEqual(2023, result);
        }

        [TestMethod]
        public void GetManureCompositionDataReturnsNonNull()
        {
            var result = _sut.GetManureCompositionData(new ManureExportViewItem(), new Farm());

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void GetManureCompositionDataReturnsNonNullWithNullArgument()
        {
            var result = _sut.GetManureCompositionData(null, null);

            Assert.IsNotNull(result);
        }

        #endregion
    }
}