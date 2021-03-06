﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConstantRowViewModel.cs" company="RHEA System S.A.">
//   Copyright (c) 2015 RHEA System S.A.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace BasicRdl.ViewModels
{
    using System.Linq;
    using CDP4Common.CommonData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Composition.Mvvm;
    using CDP4Dal;
    using CDP4Dal.Events;
    using ReactiveUI;

    /// <summary>
    /// A row view model that represents a <see cref="Constant"/>
    /// </summary>
    public class ConstantRowViewModel : CDP4CommonView.ConstantRowViewModel
    {
        /// <summary>
        /// Backing field for the <see cref="Value"/> property
        /// </summary>
        private string value;

        /// <summary>
        /// Backing field for the <see cref="ContainerRdl"/> property
        /// </summary>
        private string containerRdl;

        /// <summary>
        /// Backing field for the <see cref="SelectedScale"/> property
        /// </summary>
        private MeasurementScale selectedScale;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConstantRowViewModel"/> class. 
        /// </summary>
        /// <param name="constant">The <see cref="Constant"/> that is represented by the current view-model</param>
        /// <param name="session">The session</param>
        /// <param name="containerViewModel">The container <see cref="IViewModelBase{T}"/></param>
        public ConstantRowViewModel(Constant constant, ISession session, IViewModelBase<Thing> containerViewModel)
            : base(constant, session, containerViewModel)
        {
            this.UpdateProperties();
        }

        /// <summary>
        /// Gets or sets the value of the <see cref="Constant"/>
        /// </summary>
        public string Value
        {
            get
            {
                return this.value;
            }

            set
            {
                this.RaiseAndSetIfChanged(ref this.value, value);
            }
        }

        /// <summary>
        /// Gets or sets the selected <see cref="MeasurementScale"/>
        /// </summary>
        public MeasurementScale SelectedScale
        {
            get
            {
                return this.selectedScale;
            }

            set
            {
                this.RaiseAndSetIfChanged(ref this.selectedScale, value);
            }
        }

        /// <summary>
        /// Gets or sets the Container RDL ShortName.
        /// </summary>
        public string ContainerRdl
        {
            get
            {
                return this.containerRdl;
            }

            set
            {
                this.RaiseAndSetIfChanged(ref this.containerRdl, value);
            }
        }

        /// <summary>
        /// Gets the <see cref="ClassKind"/> of the <see cref="Thing"/> that is represented by the current view-model
        /// </summary>
        public string ClassKind { get; private set; }

        /// <summary>
        /// Updates the properties of the view-model
        /// </summary>
        private void UpdateProperties()
        {
            this.Value = this.Thing.Value.Count() == 1 ? this.Thing.Value.First() : "[...]";
            this.ClassKind = this.Thing.ClassKind.ToString();
            var quantityKind = this.Thing.ParameterType as QuantityKind;
            this.SelectedScale = quantityKind != null ? this.Thing.Scale : null;

            var container = this.Thing.Container as ReferenceDataLibrary;  
            this.ContainerRdl = container == null ? string.Empty : container.ShortName;
        }

        /// <summary>
        /// The event-handler that is invoked by the subscription that listens for updates
        /// on the <see cref="Thing"/> that is being represented by the view-model
        /// </summary>
        /// <param name="objectChange">
        /// The payload of the event that is being handled
        /// </param>
        protected override void ObjectChangeEventHandler(ObjectChangedEvent objectChange)
        {
            base.ObjectChangeEventHandler(objectChange);
            this.UpdateProperties();
        }
    }
}
