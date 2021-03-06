﻿// -------------------------------------------------------------------------------------------------
// <copyright file="QuantityKindFactorDialogViewModelTestFixture.cs" company="RHEA System S.A.">
//   Copyright (c) 2015 RHEA System S.A.
// </copyright>
// -------------------------------------------------------------------------------------------------

namespace BasicRdl.Tests.ViewModels
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Reactive.Concurrency;
    using System.Threading.Tasks;
    using BasicRdl.ViewModels.Dialogs;
    using CDP4Common.CommonData;
    using CDP4Common.MetaInfo;
    using CDP4Common.Types;
    using CDP4Dal.Operations;
    using CDP4Common.SiteDirectoryData;
    using CDP4Composition.Navigation;
    using CDP4Composition.Navigation.Interfaces;
    using CDP4Dal;
    using CDP4Dal.DAL;
    using Microsoft.Practices.ServiceLocation;
    using Moq;
    using NUnit.Framework;
    using ReactiveUI;

    [TestFixture]
    internal class QuantityKindFactorDialogViewModelTestFixture
    {
        private QuantityKindFactorDialogViewModel viewmodel;
        private QuantityKindFactor quantityKindFactor;
        private SiteDirectory siteDir;
        private ThingTransaction transaction;
        private Mock<ISession> session;
        private Mock<IServiceLocator> serviceLocator;
        private Mock<IThingDialogNavigationService> navigation;

        private ConcurrentDictionary<CacheKey, Lazy<Thing>> cache;

        [SetUp]
        public void Setup()
        {
            RxApp.MainThreadScheduler = Scheduler.CurrentThread;
            this.cache = new ConcurrentDictionary<CacheKey, Lazy<Thing>>();
            this.serviceLocator = new Mock<IServiceLocator>();
            this.navigation = new Mock<IThingDialogNavigationService>();
            ServiceLocator.SetLocatorProvider(() => this.serviceLocator.Object);
            this.serviceLocator.Setup(x => x.GetInstance<IThingDialogNavigationService>()).Returns(this.navigation.Object);

            this.session = new Mock<ISession>();
            var person = new Person(Guid.NewGuid(), this.cache, null) { Container = this.siteDir };
            this.session.Setup(x => x.ActivePerson).Returns(person);

            this.siteDir = new SiteDirectory(Guid.NewGuid(), this.cache, null);
            this.siteDir.Person.Add(person);
            var rdl = new SiteReferenceDataLibrary(Guid.NewGuid(), this.cache, null) { Name = "testRDL", ShortName = "test" };
            this.quantityKindFactor = new QuantityKindFactor();
            var testDerivedQuantityKind = new DerivedQuantityKind(Guid.NewGuid(), this.cache, null) { Name = "Test Derived QK", ShortName = "tdqk" };
            var simpleQuantityKind = new SimpleQuantityKind();
            rdl.ParameterType.Add(simpleQuantityKind);
            rdl.ParameterType.Add(testDerivedQuantityKind);
            this.siteDir.SiteReferenceDataLibrary.Add(rdl);
            this.session.Setup(x => x.RetrieveSiteDirectory()).Returns(this.siteDir);
            var chainOfContainers = new[] { rdl };


            this.cache.TryAdd(new CacheKey(testDerivedQuantityKind.Iid, null), new Lazy<Thing>(() => testDerivedQuantityKind));
            var clone = testDerivedQuantityKind.Clone(false);

            var transactionContext = TransactionContextResolver.ResolveContext(this.siteDir);
            this.transaction = new ThingTransaction(transactionContext, clone);

            var dal = new Mock<IDal>();
            this.session.Setup(x => x.DalVersion).Returns(new Version(1, 1, 0));
            this.session.Setup(x => x.Dal).Returns(dal.Object);
            dal.Setup(x => x.MetaDataProvider).Returns(new MetaDataProvider());

            this.viewmodel = new QuantityKindFactorDialogViewModel(this.quantityKindFactor, this.transaction, this.session.Object, true, ThingDialogKind.Create, null, clone, chainOfContainers);
        }

        [Test]
        public void VerifyThatPropertiesAreSet()
        {
            Assert.AreEqual(this.viewmodel.Exponent, this.quantityKindFactor.Exponent);
            Assert.AreEqual(this.viewmodel.SelectedQuantityKind, this.quantityKindFactor.QuantityKind);
            Assert.IsNotEmpty(this.viewmodel.PossibleQuantityKind);
        }

        [Test]
        public async Task VerifyThatOkCommandWorks()
        {
            this.viewmodel.OkCommand.Execute(null);

            this.session.Verify(x => x.Write(It.IsAny<OperationContainer>()));
            Assert.IsNull(this.viewmodel.WriteException);
            Assert.IsTrue(this.viewmodel.DialogResult.Value);
        }

        [Test]
        public async Task VerifyThatExceptionAreCaught()
        {
            this.session.Setup(x => x.Write(It.IsAny<OperationContainer>())).Throws(new Exception("test"));

            this.viewmodel.OkCommand.Execute(null);
            this.session.Verify(x => x.Write(It.IsAny<OperationContainer>()));

            Assert.IsNotNull(this.viewmodel.WriteException);
            Assert.AreEqual("test", this.viewmodel.WriteException.Message);
        }

        [Test]
        public void VerifyUpdateOkCanExecute()
        {
            Assert.IsFalse(this.viewmodel.OkCommand.CanExecute(null));
            Assert.That(this.viewmodel.Exponent, Is.Null.Or.Empty);
            Assert.IsNotEmpty(this.viewmodel.PossibleQuantityKind);
            Assert.IsNull(this.viewmodel.SelectedQuantityKind);
            this.viewmodel.Exponent = "2";
            Assert.IsFalse(this.viewmodel.OkCommand.CanExecute(null));
            this.viewmodel.SelectedQuantityKind = this.viewmodel.PossibleQuantityKind.First();
            Assert.IsTrue(this.viewmodel.OkCommand.CanExecute(null));
        }

        [Test]
        public void VerifyThatParameterlessContructorExists()
        {
            Assert.DoesNotThrow(() => new QuantityKindFactorDialogViewModel());
        }
    }
}