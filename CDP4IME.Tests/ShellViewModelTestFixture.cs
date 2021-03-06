﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ShellViewModelTestFixture.cs" company="RHEA System S.A.">
//   Copyright (c) 2015 RHEA System S.A.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace CDP4IME.Tests
{
    using System.Collections.Generic;
    using CDP4Composition.Navigation;
    using CDP4Dal;
    using CDP4Dal.Composition;
    using CDP4Dal.DAL;
    using CDP4IME;
    using CDP4IME.ViewModels;
    using CDP4ShellDialogs.ViewModels;
    using Microsoft.Practices.ServiceLocation;
    using Moq;
    using NLog;
    using NLog.Config;
    using NUnit.Framework;
    using System;

    /// <summary>
    /// TestFixture for the <see cref="ShellViewModel"/>
    /// </summary>
    [TestFixture]
    public class ShellViewModelTestFixture
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// the view-model under test
        /// </summary>
        private ShellViewModel viewModel;

        /// <summary>
        /// mocked <see cref="IDal"/>
        /// </summary>
        private Mock<IDal> mockedDal;

        /// <summary>
        /// mocked <see cref="IDialogNavigationService"/>
        /// </summary>
        private Mock<IDialogNavigationService> navigationService;

        /// <summary>
        /// mocked <see cref="IServiceLocator"/>
        /// </summary>
        private Mock<IServiceLocator> serviceLocator;

        [SetUp]
        public void SetUp()
        {
            LogManager.Configuration = new LoggingConfiguration();

            this.navigationService = new Mock<IDialogNavigationService>();

            this.serviceLocator = new Mock<IServiceLocator>();
            ServiceLocator.SetLocatorProvider(() => this.serviceLocator.Object);

            var dals = new List<Lazy<IDal, IDalMetaData>>();
            var availableDals = new AvailableDals(dals);
            this.serviceLocator.Setup(x => x.GetInstance<AvailableDals>()).Returns(availableDals);

            this.viewModel = new ShellViewModel(this.navigationService.Object);
        }

        [TearDown]
        public void TearDown()
        {
            LogManager.Configuration = null;
            this.serviceLocator = null;
            this.viewModel = null;
        }

        [Test]
        public void VerifyThatArgumentNullExceptionIsThrown()
        {
            Assert.Throws<ArgumentNullException>(() => new ShellViewModel(null));
        }

        [Test]
        public void VerifyThatTheCaptionIsCorrect()
        {
            var title = "CDP4 IME - Community Edition";
            Assert.AreEqual(title, this.viewModel.Title);
        }

        [Test]
        public void VerifyThatLogAreCaught()
        {
            logger.Log(LogLevel.Info, "test");

            var log = this.viewModel.LogEventInfo;
            Assert.AreEqual("test", log.Message);
        }

        [Test]
        public void VerifyThatLogDetailCommandWorks()
        {
            this.navigationService.Setup(x => x.NavigateModal(It.IsAny<LogDetailsViewModel>())).Returns(null as IDialogResult);

            this.viewModel.OpenLogDialogCommand.Execute(null);
            this.navigationService.Verify(x => x.NavigateModal(It.IsAny<LogDetailsViewModel>()));
        }

        [Test]
        public void VerifyThatOpenAboutCommandWorks()
        {
            Assert.DoesNotThrow(() => this.viewModel.OpenAboutCommand.Execute(null));
        }

        [Test]
        public void Verify_that_OpenProxyConfigurationCommand_can_be_executed()
        {
            Assert.DoesNotThrow(() => this.viewModel.OpenProxyConfigurationCommand.Execute(null));
        }

        [Test]
        public void VerifyThatOpenDataSourceCommandExecutesAndIfCancelledNoSessionLoaded()
        {
            this.navigationService.Setup(x => x.NavigateModal(It.IsAny<DataSourceSelectionViewModel>())).Returns(null as DataSourceSelectionResult);
            this.viewModel.OpenDataSourceCommand.Execute(null);
            this.navigationService.Verify(x => x.NavigateModal(It.IsAny<DataSourceSelectionViewModel>()));

            CollectionAssert.IsEmpty(this.viewModel.Sessions);
        }

        [Test]
        public void VerifyThatOpenAPluginManagerCommandNavigatesToPluginWindow()
        {
            this.viewModel.OpenPluginManagerCommand.Execute(null);
            this.navigationService.Verify(x => x.NavigateModal(It.IsAny<PluginManagerViewModel>()));
        }

        [Test]
        public void VerifyThatOpenDataSourceCommandExecutesAndSessionIsLoaded()
        {
            Assert.IsFalse(this.viewModel.HasSessions);

            var mockedSession = new Mock<ISession>();
            var selectionResult = new DataSourceSelectionResult(true, mockedSession.Object);

            this.navigationService.Setup(x => x.NavigateModal(It.IsAny<DataSourceSelectionViewModel>())).Returns(selectionResult);
            this.viewModel.OpenDataSourceCommand.Execute(null);
            this.navigationService.Verify(x => x.NavigateModal(It.IsAny<DataSourceSelectionViewModel>()));

            CollectionAssert.IsNotEmpty(this.viewModel.Sessions);

            Assert.IsTrue(this.viewModel.HasSessions);

            Assert.AreEqual(mockedSession.Object, this.viewModel.SelectedSession.Session);
        }

        [Test]
        public void VerifyThatExecuteOpenAboutRequestNavigatesToAboutWindow()
        {
            this.viewModel.OpenAboutCommand.Execute(null);
            this.navigationService.Verify(x => x.NavigateModal(It.IsAny<AboutViewModel>()));
        }

        [Test]
        public void VerifThatExecuteOpenLogDialogCommandNavigatesToLogWindow()
        {
            this.viewModel.OpenLogDialogCommand.Execute(null);
            this.navigationService.Verify(x => x.NavigateModal(It.IsAny<LogDetailsViewModel>()));
        }

        [Test]
        public void VerifThatSaveSessionCommandNavigatesToDataSourceExportViewModel()
        {
            this.viewModel.SaveSessionCommand.Execute(null);
            this.navigationService.Verify(x => x.NavigateModal(It.IsAny<DataSourceExportViewModel>()));            
        }

        [Test]
        public void VerifyThatAOpenUriCommandCanBeExecuted()
        {
            Assert.IsTrue(this.viewModel.OpenUriManagerCommand.CanExecute(null));

            this.viewModel.OpenUriManagerCommand.Execute(null);
            this.navigationService.Verify(x => x.NavigateModal(It.IsAny<UriManagerViewModel>()));            
        }
    }
}