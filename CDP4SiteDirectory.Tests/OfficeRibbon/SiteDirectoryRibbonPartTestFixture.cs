﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SiteDirectoryRibbonPartTestFixture.cs" company="RHEA System S.A.">
//   Copyright (c) 2015 RHEA System S.A.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace CDP4SiteDirectory.Tests.OfficeRibbon
{
    using System;
    using System.IO.Packaging;
    using System.Reactive.Concurrency;
    using System.Threading;
    using System.Windows;
    using CDP4SiteDirectory.ViewModels;
    using CDP4Common.CommonData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Composition;
    using CDP4Composition.Navigation;
    using CDP4Composition.Navigation.Interfaces;
    using CDP4Dal;
    using CDP4Dal.Events;
    using CDP4Dal.Permission;
    using Microsoft.Practices.ServiceLocation;
    using Moq;
    using NUnit.Framework;
    using ReactiveUI;

    /// <summary>
    /// Suite of tests for the <see cref="SiteDirectoryRibbonPart"/> class
    /// </summary>
    [TestFixture, Apartment(ApartmentState.STA)]
    public class SiteDirectoryRibbonPartTestFixture
    {
        private Uri uri;
        private int amountOfRibbonControls;
        private int order;
        private string ribbonxmlname;
        private Mock<IServiceLocator> serviceLocator;
        private Mock<IPanelNavigationService> panelNavigationService;
        private Mock<IThingDialogNavigationService> dialogNavigationService;
        private Mock<IPermissionService> permittingPermissionService;
        private Mock<ISession> session;
        private Person person;
        private SiteDirectoryRibbonPart ribbonPart;

        [SetUp]
        public void SetUp()
        {
            RxApp.MainThreadScheduler = Scheduler.CurrentThread;
            this.uri = new Uri("http://www.rheageoup.com");
            this.SetupRecognizePackUir();

            this.session = new Mock<ISession>();
            var siteDirectory = new SiteDirectory(Guid.NewGuid(), null, this.uri);
            this.person = new Person(Guid.NewGuid(), null, this.uri) { GivenName = "John", Surname = "Doe" };

            this.session.Setup(x => x.DataSourceUri).Returns("test");
            this.session.Setup(x => x.RetrieveSiteDirectory()).Returns(siteDirectory);
            this.session.Setup(x => x.ActivePerson).Returns(this.person);
            this.panelNavigationService = new Mock<IPanelNavigationService>();
            this.dialogNavigationService = new Mock<IThingDialogNavigationService>();
            this.serviceLocator = new Mock<IServiceLocator>();

            this.permittingPermissionService = new Mock<IPermissionService>();
            this.permittingPermissionService.Setup(x => x.CanRead(It.IsAny<Thing>())).Returns(true);
            this.permittingPermissionService.Setup(x => x.CanWrite(It.IsAny<Thing>())).Returns(true);
            this.permittingPermissionService.Setup(x => x.CanWrite(It.IsAny<ClassKind>(), It.IsAny<Thing>())).Returns(true);

            this.session.Setup(x => x.PermissionService).Returns(this.permittingPermissionService.Object);

            this.amountOfRibbonControls = 8;
            this.order = 1;

            this.ribbonPart = new SiteDirectoryRibbonPart(this.order, this.panelNavigationService.Object, null, null, null);

            ServiceLocator.SetLocatorProvider(() => this.serviceLocator.Object);
            this.serviceLocator.Setup(x => x.GetInstance<IThingDialogNavigationService>())
                .Returns(this.dialogNavigationService.Object);
        }

        public void TearDown()
        {
            CDPMessageBus.Current.ClearSubscriptions();
        }

        /// <summary>
        /// Pack Uri's are not recognized untill they have been registered in the appl domain
        /// This method makes sure pack Uri's do no throw an exception regarding incorrect port.
        /// </summary>
        private void SetupRecognizePackUir()
        {
            PackUriHelper.Create(new Uri("reliable://0"));
            new FrameworkElement();

            try
            {
                Application.ResourceAssembly = typeof(SiteDirectoryRibbonPart).Assembly;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex);
            }
        }

        [Test]
        public void VerifyThatTheOrderAndXmlAreLoaded()
        {
            Assert.AreEqual(this.order, this.ribbonPart.Order);
            Assert.AreEqual(this.amountOfRibbonControls, this.ribbonPart.ControlIdentifiers.Count);
        }

        [Test]
        public void VerifyThatIfFluentRibbonIsNotActiveTheSessionEventHasNoEffect()
        {
            var fluentRibbonManager = new FluentRibbonManager();
            fluentRibbonManager.IsActive = false;
            fluentRibbonManager.RegisterRibbonPart(this.ribbonPart);

            var sessionEvent = new SessionEvent(this.session.Object, SessionStatus.Open);
            CDPMessageBus.Current.SendMessage(sessionEvent);
            Assert.IsNull(this.ribbonPart.Session);
        }

        [Test]
        public void VerifyThatIfFluentRibbonIsNullTheSessionEventHasNoEffect()
        {
            var sessionEvent = new SessionEvent(this.session.Object, SessionStatus.Open);
            CDPMessageBus.Current.SendMessage(sessionEvent);
            Assert.IsNull(this.ribbonPart.Session);
        }

        [Test]
        public void VerifyThatRibbonPartHandlesSessionOpenAndCloseEvent()
        {
            var fluentRibbonManager = new FluentRibbonManager();
            fluentRibbonManager.IsActive = true;
            fluentRibbonManager.RegisterRibbonPart(this.ribbonPart);

            var openSessionEvent = new SessionEvent(this.session.Object, SessionStatus.Open);
            CDPMessageBus.Current.SendMessage(openSessionEvent);

            Assert.AreEqual(this.session.Object, this.ribbonPart.Session);

            var closeSessionEvent = new SessionEvent(this.session.Object, SessionStatus.Closed);
            CDPMessageBus.Current.SendMessage(closeSessionEvent);

            Assert.IsNull(this.ribbonPart.Session);
        }

        [Test]
        public void VerifyThatOnActionInvokesTheNavigationServiceOpenAndCloseMethod()
        {
            var fluentRibbonManager = new FluentRibbonManager();
            fluentRibbonManager.IsActive = true;
            fluentRibbonManager.RegisterRibbonPart(this.ribbonPart);

            // open viemodels
            var openSessionEvent = new SessionEvent(this.session.Object, SessionStatus.Open);
            CDPMessageBus.Current.SendMessage(openSessionEvent);

            this.ribbonPart.OnAction("ShowDomainsOfExpertise");
            this.panelNavigationService.Verify(x => x.Open(It.IsAny<DomainOfExpertiseBrowserViewModel>(), false));

            this.ribbonPart.OnAction("ShowModels");
            this.panelNavigationService.Verify(x => x.Open(It.IsAny<ModelBrowserViewModel>(), false));

            this.ribbonPart.OnAction("ShowLanguages");
            this.panelNavigationService.Verify(x => x.Open(It.IsAny<NaturalLanguageBrowserViewModel>(), false));

            this.ribbonPart.OnAction("ShowOrganizations");
            this.panelNavigationService.Verify(x => x.Open(It.IsAny<OrganizationBrowserViewModel>(), false));

            this.ribbonPart.OnAction("ShowRoles");
            this.panelNavigationService.Verify(x => x.Open(It.IsAny<RoleBrowserViewModel>(), false));

            this.ribbonPart.OnAction("ShowPersons");
            this.panelNavigationService.Verify(x => x.Open(It.IsAny<PersonBrowserViewModel>(), false));

            this.ribbonPart.OnAction("ShowSiteRDLs");
            this.panelNavigationService.Verify(x => x.Open(It.IsAny<SiteRdlBrowserViewModel>(), false));

            // close viewmodels
            var closeSessionEvent = new SessionEvent(this.session.Object, SessionStatus.Closed);
            CDPMessageBus.Current.SendMessage(closeSessionEvent);

            this.panelNavigationService.Verify(x => x.Close(It.IsAny<DomainOfExpertiseBrowserViewModel>(), false));
            this.panelNavigationService.Verify(x => x.Close(It.IsAny<ModelBrowserViewModel>(), false));
            this.panelNavigationService.Verify(x => x.Close(It.IsAny<NaturalLanguageBrowserViewModel>(), false));
            this.panelNavigationService.Verify(x => x.Close(It.IsAny<OrganizationBrowserViewModel>(), false));
            this.panelNavigationService.Verify(x => x.Close(It.IsAny<RoleBrowserViewModel>(), false));
            this.panelNavigationService.Verify(x => x.Close(It.IsAny<PersonBrowserViewModel>(), false));
            this.panelNavigationService.Verify(x => x.Close(It.IsAny<SiteRdlBrowserViewModel>(), false));
        }

        [Test]
        public void VerifyThatOnActionDoesNotInvokePanelNavigationWhenSessionIsNull()
        {
            this.ribbonPart.OnAction("ShowDomainsOfExpertise");
            this.panelNavigationService.Verify(x => x.Open(It.IsAny<DomainOfExpertiseBrowserViewModel>(), false), Times.Never);
        }

        [Test]
        public void VerifyThatGetEnabledReturnsExpectedResult()
        {
            var fluentRibbonManager = new FluentRibbonManager();
            fluentRibbonManager.IsActive = true;
            fluentRibbonManager.RegisterRibbonPart(this.ribbonPart);

            Assert.IsFalse(this.ribbonPart.GetEnabled("ShowDomainsOfExpertise"));
            Assert.IsFalse(this.ribbonPart.GetEnabled("ShowModels"));
            Assert.IsFalse(this.ribbonPart.GetEnabled("ShowLanguages"));
            Assert.IsFalse(this.ribbonPart.GetEnabled("ShowOrganizations"));
            Assert.IsFalse(this.ribbonPart.GetEnabled("ShowPersons"));
            Assert.IsFalse(this.ribbonPart.GetEnabled("ShowRoles"));
            Assert.IsFalse(this.ribbonPart.GetEnabled("ShowSiteRDLs"));
            Assert.IsFalse(this.ribbonPart.GetEnabled("unknownRibbonControlId"));

            var openSessionEvent = new SessionEvent(this.session.Object, SessionStatus.Open);
            CDPMessageBus.Current.SendMessage(openSessionEvent);

            Assert.IsTrue(this.ribbonPart.GetEnabled("ShowDomainsOfExpertise"));
            Assert.IsTrue(this.ribbonPart.GetEnabled("ShowModels"));
            Assert.IsTrue(this.ribbonPart.GetEnabled("ShowLanguages"));
            Assert.IsTrue(this.ribbonPart.GetEnabled("ShowOrganizations"));
            Assert.IsTrue(this.ribbonPart.GetEnabled("ShowPersons"));
            Assert.IsTrue(this.ribbonPart.GetEnabled("ShowRoles"));
            Assert.IsTrue(this.ribbonPart.GetEnabled("ShowSiteRDLs"));
            Assert.IsFalse(this.ribbonPart.GetEnabled("unknownRibbonControlId"));

            var closeSessionEvent = new SessionEvent(this.session.Object, SessionStatus.Closed);
            CDPMessageBus.Current.SendMessage(closeSessionEvent);

            Assert.IsFalse(this.ribbonPart.GetEnabled("ShowDomainsOfExpertise"));
            Assert.IsFalse(this.ribbonPart.GetEnabled("ShowModels"));
            Assert.IsFalse(this.ribbonPart.GetEnabled("ShowLanguages"));
            Assert.IsFalse(this.ribbonPart.GetEnabled("ShowOrganizations"));
            Assert.IsFalse(this.ribbonPart.GetEnabled("ShowPersons"));
            Assert.IsFalse(this.ribbonPart.GetEnabled("ShowRoles"));
            Assert.IsFalse(this.ribbonPart.GetEnabled("ShowSiteRDLs"));
            Assert.IsFalse(this.ribbonPart.GetEnabled("unknownRibbonControlId"));
        }

        [Test]
        public void VerifyThatImageIsReturned()
        {
            var domainsOfExpertiseImage = this.ribbonPart.GetImage("ShowDomainsOfExpertise");
            Assert.IsNotNull(domainsOfExpertiseImage);

            var modelsImage = this.ribbonPart.GetImage("ShowModels");
            Assert.IsNotNull(modelsImage);

            var languagesImage = this.ribbonPart.GetImage("ShowLanguages");
            Assert.IsNotNull(languagesImage);

            var organizationsImage = this.ribbonPart.GetImage("ShowOrganizations");
            Assert.IsNotNull(organizationsImage);

            var personsImage = this.ribbonPart.GetImage("ShowPersons");
            Assert.IsNotNull(personsImage);

            var rolesImage = this.ribbonPart.GetImage("ShowRoles");
            Assert.IsNotNull(rolesImage);

            var siteRDLImage = this.ribbonPart.GetImage("ShowSiteRDLs");
            Assert.IsNotNull(siteRDLImage);

            var unknownImage = this.ribbonPart.GetImage("unknownRibbonControlId");
            Assert.IsNull(unknownImage);
        }
    }
}
