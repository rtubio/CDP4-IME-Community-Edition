﻿// ------------------------------------------------------------------------------------------------
// <copyright file="ElementUsageRowViewModelTestFixture.cs" company="RHEA System S.A.">
//   Copyright (c) 2015 RHEA System S.A.
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ProductTree.Tests.ProductTreeRows
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Common.Types;
    using CDP4Composition.DragDrop;
    using CDP4Composition.Navigation;
    using CDP4Composition.Navigation.Interfaces;
    using CDP4Dal;
    using CDP4Dal.Events;
    using CDP4Dal.Operations;
    using CDP4Dal.Permission;
    using CDP4ProductTree.ViewModels;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    internal class ElementUsageRowViewModelTestFixture
    {
        private Mock<IPermissionService> permissionService;
        private Mock<IPanelNavigationService> panelNavigationService;
        private Mock<IThingDialogNavigationService> thingDialogNavigationService;
        private Mock<ISession> session;
        private readonly Uri uri = new Uri("http://www.rheagroup.com");

        private SiteDirectory siteDir;
        private EngineeringModel model;
        private Iteration iteration;
        private EngineeringModelSetup modelSetup;
        private IterationSetup iterationSetup;
        private Person person;
        private Participant participant;
        private Option option;
        private ElementDefinition elementDef;
        private ElementDefinition elementDef2;
        private ElementDefinition elementDef3;
        private DomainOfExpertise domain;
        private ElementUsage elementUsage;
        private ParameterValueSet valueSet;
        private ParameterOverrideValueSet valueSetOverride;

        private ConcurrentDictionary<CacheKey, Lazy<Thing>> cache = new ConcurrentDictionary<CacheKey, Lazy<Thing>>();

        [SetUp]
        public void Setup()
        {
            this.permissionService = new Mock<IPermissionService>();
            this.panelNavigationService = new Mock<IPanelNavigationService>();
            this.thingDialogNavigationService = new Mock<IThingDialogNavigationService>();
            this.session = new Mock<ISession>();

            this.domain = new DomainOfExpertise(Guid.NewGuid(), this.cache, this.uri) { Name = "domain" , ShortName = "dom" };
            this.siteDir = new SiteDirectory(Guid.NewGuid(), this.cache, this.uri);
            this.person = new Person(Guid.NewGuid(), this.cache, this.uri);
            this.model = new EngineeringModel(Guid.NewGuid(), this.cache, this.uri);
            this.modelSetup = new EngineeringModelSetup(Guid.NewGuid(), this.cache, this.uri);
            this.iteration = new Iteration(Guid.NewGuid(), this.cache, this.uri);
            this.iterationSetup = new IterationSetup(Guid.NewGuid(), this.cache, this.uri);
            this.participant = new Participant(Guid.NewGuid(), this.cache, this.uri);
            this.option = new Option(Guid.NewGuid(), this.cache, this.uri);
            this.elementDef = new ElementDefinition(Guid.NewGuid(), this.cache, this.uri) { Owner = this.domain};
            this.elementDef3 = new ElementDefinition(Guid.NewGuid(), this.cache, this.uri) { Owner = this.domain};

            this.elementDef2 = new ElementDefinition(Guid.NewGuid(), this.cache, this.uri) { Owner = this.domain, Name = "Element definition 1", ShortName = "ED1" };
            this.elementUsage = new ElementUsage(Guid.NewGuid(), this.cache, this.uri) 
            { ElementDefinition = this.elementDef2, Owner = this.domain, Name = "Element usage 1" ,ShortName = "EU1"};

            this.valueSet = new ParameterValueSet(Guid.NewGuid(), this.cache, this.uri);
            this.valueSet.Published = new ValueArray<string>(new List<string> { "1" });
            this.valueSet.Manual = new ValueArray<string>(new List<string> { "1" });
            this.valueSet.ValueSwitch = ParameterSwitchKind.MANUAL;

            this.valueSetOverride = new ParameterOverrideValueSet(Guid.NewGuid(), this.cache, this.uri);
            this.valueSetOverride.Published = new ValueArray<string>(new List<string> { "1" });
            this.valueSetOverride.Manual = new ValueArray<string>(new List<string> { "1" });
            this.valueSetOverride.ValueSwitch = ParameterSwitchKind.MANUAL;

            this.siteDir.Person.Add(this.person);
            this.siteDir.Model.Add(this.modelSetup);
            this.modelSetup.IterationSetup.Add(this.iterationSetup);
            this.modelSetup.Participant.Add(this.participant);
            this.participant.Person = this.person;

            this.model.Iteration.Add(this.iteration);
            this.model.EngineeringModelSetup = this.modelSetup;
            this.iteration.IterationSetup = this.iterationSetup;
            this.iteration.Option.Add(this.option);
            this.iteration.TopElement = this.elementDef;
            this.iteration.Element.Add(this.elementDef);
            this.iteration.Element.Add(this.elementDef3);
            this.iteration.Element.Add(this.elementDef2);
            this.elementDef.ContainedElement.Add(this.elementUsage);

            this.session.Setup(x => x.ActivePerson).Returns(this.person);
            this.session.Setup(x => x.DataSourceUri).Returns(this.uri.ToString);
            this.session.Setup(x => x.OpenIterations).Returns(new Dictionary<Iteration, Tuple<DomainOfExpertise, Participant>>());
            this.session.Setup(x => x.PermissionService).Returns(this.permissionService.Object);
            this.session.Setup(x => x.QuerySelectedDomainOfExpertise(this.iteration)).Returns(this.domain);
        }

        [TearDown]
        public void TearDown()
        {
            CDPMessageBus.Current.ClearSubscriptions();
        }

        [Test]
        public void VerifyThatPropertiesAreSet()
        {
            var vm = new ElementUsageRowViewModel(this.elementUsage, this.option, this.session.Object, null);

            Assert.IsNotNull(vm.ElementDefinition);
            Assert.That(vm.Name, Is.Not.Null.Or.Empty);
            Assert.That(vm.ShortName, Is.Not.Null.Or.Empty);
            Assert.AreEqual("Element usage 1 : Element definition 1", vm.Name);
            Assert.AreEqual("EU1 : ED1", vm.ShortName);
            Assert.AreSame(this.elementDef2, vm.ElementDefinition);
            Assert.AreEqual("domain", vm.OwnerName);
            Assert.AreEqual("dom", vm.OwnerShortName);
        }

        [Test]
        public void VerifyThatNullElementDefThrows()
        {
            this.elementUsage.ElementDefinition = null;
            Assert.Throws<NullReferenceException>(() =>  new ElementUsageRowViewModel(this.elementUsage, this.option, this.session.Object, null));
        }

        [Test]
        public void VerifyThatPopulateGroupWorks()
        {
            var revisionProperty = typeof(ElementUsage).GetProperty("RevisionNumber");

            var group1 = new ParameterGroup(Guid.NewGuid(), this.cache, this.uri);
            var group11 = new ParameterGroup(Guid.NewGuid(), this.cache, this.uri) { ContainingGroup = group1 };

            this.elementDef.ParameterGroup.Add(group1);
            this.elementDef.ParameterGroup.Add(group11);

            this.elementUsage.ElementDefinition = this.elementDef;
            this.elementDef.ContainedElement.Clear();

            var vm = new ElementUsageRowViewModel(this.elementUsage, this.option, this.session.Object, null);

            var group1row = vm.ContainedRows.OfType<ParameterGroupRowViewModel>().Single();
            Assert.AreSame(group1, group1row.Thing);

            var group11row = group1row.ContainedRows.OfType<ParameterGroupRowViewModel>().Single();
            Assert.AreSame(group11, group11row.Thing);

            // move group11
            group11.ContainingGroup = null;
            revisionProperty.SetValue(group11, 10);

            CDPMessageBus.Current.SendObjectChangeEvent(group11, EventKind.Updated);
            Assert.AreEqual(2, vm.ContainedRows.OfType<ParameterGroupRowViewModel>().Count());
            Assert.AreEqual(0, group1row.ContainedRows.OfType<ParameterGroupRowViewModel>().Count());

            // move group11 under group1
            group11.ContainingGroup = group1;
            revisionProperty.SetValue(group11, 20);

            CDPMessageBus.Current.SendObjectChangeEvent(group11, EventKind.Updated);
            Assert.AreEqual(1, vm.ContainedRows.OfType<ParameterGroupRowViewModel>().Count());
            Assert.AreSame(group11, group1row.ContainedRows.OfType<ParameterGroupRowViewModel>().Single().Thing);

            // add group2 and move group11 under group2
            var group2 = new ParameterGroup(Guid.NewGuid(), this.cache, this.uri);
            group11.ContainingGroup = group2;
            this.elementDef.ParameterGroup.Add(group2);
            revisionProperty.SetValue(this.elementDef, 30);
            revisionProperty.SetValue(group11, 30);

            CDPMessageBus.Current.SendObjectChangeEvent(this.elementDef, EventKind.Updated);
            CDPMessageBus.Current.SendObjectChangeEvent(group11, EventKind.Updated);

            Assert.AreEqual(2, vm.ContainedRows.OfType<ParameterGroupRowViewModel>().Count());
            Assert.AreEqual(0, group1row.ContainedRows.OfType<ParameterGroupRowViewModel>().Count());

            var group2row = vm.ContainedRows.OfType<ParameterGroupRowViewModel>().Single(x => x.Thing == group2);
            Assert.AreEqual(1, group2row.ContainedRows.OfType<ParameterGroupRowViewModel>().Count());

            // remove group11
            this.elementDef.ParameterGroup.Remove(group11);
            revisionProperty.SetValue(this.elementDef, 40);

            CDPMessageBus.Current.SendObjectChangeEvent(this.elementDef, EventKind.Updated);
            Assert.AreEqual(2, vm.ContainedRows.OfType<ParameterGroupRowViewModel>().Count());
            Assert.AreEqual(0, group1row.ContainedRows.OfType<ParameterGroupRowViewModel>().Count());
            Assert.AreEqual(0, group2row.ContainedRows.OfType<ParameterGroupRowViewModel>().Count());
        }

        [Test]
        public void VerifyThatPopulateParameterOrOverrideWorks()
        {
            var revisionProperty = typeof(ElementUsage).GetProperty("RevisionNumber");

            // TEST DATA
            var group1 = new ParameterGroup(Guid.NewGuid(), this.cache, this.uri);
            var group11 = new ParameterGroup(Guid.NewGuid(), this.cache, this.uri) { ContainingGroup = group1 };

            this.elementDef.ParameterGroup.Add(group1);
            this.elementDef.ParameterGroup.Add(group11);

            this.elementUsage.ElementDefinition = this.elementDef;
            this.elementDef.ContainedElement.Clear();

            var type1 = new EnumerationParameterType(Guid.NewGuid(), this.cache, this.uri) { Name = "type1" };
            var parameter1 = new Parameter(Guid.NewGuid(), this.cache, this.uri) { ParameterType = type1, Owner = this.domain};
            parameter1.ValueSet.Add(this.valueSet);
            var parameter2 = new Parameter(Guid.NewGuid(), this.cache, this.uri) { ParameterType = type1, Owner = this.domain};
            parameter2.ValueSet.Add(this.valueSet);

            this.elementDef.Parameter.Add(parameter2);
            this.elementDef.Parameter.Add(parameter1);

            var override1 = new ParameterOverride(Guid.NewGuid(), this.cache, this.uri) {Parameter = parameter1, Owner = this.domain};
            override1.ValueSet.Add(this.valueSetOverride);
            this.elementUsage.ParameterOverride.Add(override1);

            var vm = new ElementUsageRowViewModel(this.elementUsage, this.option, this.session.Object, null);
            // **************************************************************************************

            // check added parameter
            Assert.AreEqual(2, vm.ContainedRows.OfType<ParameterOrOverrideBaseRowViewModel>().Count());
            var param2row = vm.ContainedRows.OfType<ParameterRowViewModel>().Single();
            var override1row = vm.ContainedRows.OfType<ParameterOverrideRowViewModel>().Single();

            Assert.AreSame(param2row.Thing, parameter2);
            Assert.AreSame(override1, override1row.Thing);

            // move parameter1, check update is correct
            parameter1.Group = group11;
            revisionProperty.SetValue(parameter1, 10);

            CDPMessageBus.Current.SendObjectChangeEvent(parameter1, EventKind.Updated);
            Assert.AreEqual(1, vm.ContainedRows.OfType<ParameterOrOverrideBaseRowViewModel>().Count());

            var group11row = vm.ContainedRows.OfType<ParameterGroupRowViewModel>().Single().ContainedRows.OfType<ParameterGroupRowViewModel>().Single();
            Assert.AreSame(override1, group11row.ContainedRows.OfType<ParameterOrOverrideBaseRowViewModel>().Single().Thing);

            // move parameter1 under group1, check update
            parameter1.Group = group1;
            revisionProperty.SetValue(parameter1, 20);

            CDPMessageBus.Current.SendObjectChangeEvent(parameter1, EventKind.Updated);
            Assert.AreEqual(1, vm.ContainedRows.OfType<ParameterOrOverrideBaseRowViewModel>().Count());

            var group1row = vm.ContainedRows.OfType<ParameterGroupRowViewModel>().Single();
            Assert.AreSame(override1, group1row.ContainedRows.OfType<ParameterOrOverrideBaseRowViewModel>().Single().Thing);

            // move parameter1 back to top, check update is correct
            parameter1.Group = null;
            revisionProperty.SetValue(parameter1, 30);

            CDPMessageBus.Current.SendObjectChangeEvent(parameter1, EventKind.Updated);
            Assert.AreEqual(2, vm.ContainedRows.OfType<ParameterOrOverrideBaseRowViewModel>().Count());
            Assert.AreEqual(0, group11row.ContainedRows.OfType<ParameterOrOverrideBaseRowViewModel>().Count());

            // remove override1
            this.elementUsage.ParameterOverride.Clear();
            revisionProperty.SetValue(this.elementUsage, 40);

            CDPMessageBus.Current.SendObjectChangeEvent(this.elementUsage, EventKind.Updated);
            Assert.AreEqual(2, vm.ContainedRows.OfType<ParameterOrOverrideBaseRowViewModel>().Count());
            var param1row = vm.ContainedRows.OfType<ParameterRowViewModel>().SingleOrDefault(x => x.Thing == parameter1);
            Assert.IsNotNull(param1row);

        }

        [Test]
        public void VerifyThatOptionDependencyIsHandled()
        {
            var newDef = new ElementDefinition(Guid.NewGuid(), this.cache, this.uri);
            var newUsage = new ElementUsage(Guid.NewGuid(), this.cache, this.uri) {ElementDefinition = newDef};

            this.elementDef2.ContainedElement.Add(newUsage);

            var vm = new ElementUsageRowViewModel(this.elementUsage, this.option, this.session.Object, null);

            Assert.IsTrue(vm.ContainedRows.Select(x => x.Thing).Contains(newUsage));

            var revisionProperty = typeof(ElementDefinition).GetProperty("RevisionNumber");
            revisionProperty.SetValue(newUsage, 20);

            newUsage.ExcludeOption.Add(this.option);

            CDPMessageBus.Current.SendObjectChangeEvent(newUsage, EventKind.Updated);
            Assert.IsFalse(vm.ContainedRows.Select(x => x.Thing).Contains(newUsage));

            revisionProperty.SetValue(newUsage, 30);
            newUsage.ExcludeOption.Clear();

            CDPMessageBus.Current.SendObjectChangeEvent(newUsage, EventKind.Updated);
            Assert.IsTrue(vm.ContainedRows.Select(x => x.Thing).Contains(newUsage));
        }

        [Test]
        public void VerifyThatDragOverWorks()
        {
            this.permissionService.Setup(x => x.CanWrite(It.IsAny<ClassKind>(), It.IsAny<Thing>())).Returns(true);
            var vm = new ElementUsageRowViewModel(this.elementUsage, this.option, this.session.Object, null);

            var dropinfo = new Mock<IDropInfo>();
            dropinfo.Setup(x => x.Payload).Returns(this.elementDef3);

            dropinfo.SetupProperty(x => x.Effects);
            vm.DragOver(dropinfo.Object);

            Assert.AreEqual(DragDropEffects.Copy, dropinfo.Object.Effects);
        }

        [Test]
        public void VerifyThatDragOverWorks2()
        {
            this.permissionService.Setup(x => x.CanWrite(It.IsAny<ClassKind>(), It.IsAny<Thing>())).Returns(true);
            var vm = new ElementUsageRowViewModel(this.elementUsage, this.option, this.session.Object, null);

            var dropinfo = new Mock<IDropInfo>();
            dropinfo.Setup(x => x.Payload).Returns(this.elementDef2);

            dropinfo.SetupProperty(x => x.Effects);
            vm.DragOver(dropinfo.Object);

            Assert.AreEqual(DragDropEffects.None, dropinfo.Object.Effects);
        }


        [Test]
        public void VerifyThatDropWorks()
        {
            this.permissionService.Setup(x => x.CanWrite(It.IsAny<ClassKind>(), It.IsAny<Thing>())).Returns(true);
            var vm = new ElementUsageRowViewModel(this.elementUsage, this.option, this.session.Object, null);

            var dropinfo = new Mock<IDropInfo>();
            dropinfo.Setup(x => x.Payload).Returns(this.elementDef3);
            dropinfo.Setup(x => x.Effects).Returns(DragDropEffects.Copy);

            dropinfo.SetupProperty(x => x.Effects);
            vm.Drop(dropinfo.Object).Wait();
            this.session.Verify(x => x.Write(It.IsAny<OperationContainer>()));
        }
    }
}