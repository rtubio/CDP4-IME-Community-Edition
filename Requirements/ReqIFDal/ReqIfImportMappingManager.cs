﻿// -------------------------------------------------------------------------------------------------
// <copyright file="ReqIfImportMappingManager.cs" company="RHEA System S.A.">
//   Copyright (c) 2015 RHEA System S.A.
// </copyright>
// -------------------------------------------------------------------------------------------------

namespace CDP4Requirements.ReqIFDal
{
    using System.Collections.Generic;
    using System.Linq;
    using CDP4Common.CommonData;
    using CDP4Common.EngineeringModelData;
    using CDP4Common.SiteDirectoryData;
    using CDP4Composition.Navigation;
    using CDP4Composition.Navigation.Interfaces;
    using CDP4Dal;
    using CDP4Requirements.ViewModels;
    using ReqIFSharp;

    /// <summary>
    /// The import <see cref="ReqIF"/> to <see cref="Thing"/> mapping class that handle the different mapping steps
    /// </summary>
    public class ReqIfImportMappingManager
    {
        /// <summary>
        /// The <see cref="IDialogNavigationService"/>
        /// </summary>
        private readonly IDialogNavigationService dialogNavigationService;

        /// <summary>
        /// The <see cref="IThingDialogNavigationService"/>
        /// </summary>
        private readonly IThingDialogNavigationService thingDialogNavigationService;

        /// <summary>
        /// The active <see cref="DomainOfExpertise"/>
        /// </summary>
        private readonly DomainOfExpertise currentDomain;

        /// <summary>
        /// The <see cref="ReqIF"/> object to map
        /// </summary>
        private readonly ReqIF reqIf;

        /// <summary>
        /// The <see cref="ISession"/> in which are information shall be written
        /// </summary>
        private readonly ISession session;

        /// <summary>
        /// The <see cref="Iteration"/> in which the information shall be written
        /// </summary>
        private readonly Iteration iteration;

        /// <summary>
        /// The <see cref="ParameterType"/> Mapping
        /// </summary>
        private Dictionary<DatatypeDefinition, DatatypeDefinitionMap> datatypeDefinitionMap; 

        /// <summary>
        /// The <see cref="SpecObjectType"/> map
        /// </summary>
        private Dictionary<SpecObjectType, SpecObjectTypeMap> specObjectTypeMap;

        /// <summary>
        /// The <see cref="SpecRelationType"/> map
        /// </summary>
        private Dictionary<SpecRelationType, SpecRelationTypeMap> specRelationTypeMap;

        /// <summary>
        /// The <see cref="RelationGroupType"/> map
        /// </summary>
        private Dictionary<RelationGroupType, RelationGroupTypeMap> relationGroupTypeMap;

        /// <summary>
        /// The <see cref="SpecificationType"/> map
        /// </summary>
        private Dictionary<SpecificationType, SpecTypeMap> specificationTypeMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReqIfImportMappingManager"/> class
        /// </summary>
        /// <param name="reqif">The <see cref="ReqIF"/> object to map</param>
        /// <param name="session">The <see cref="ISession"/> in which are information shall be written</param>
        /// <param name="iteration">The <see cref="Iteration"/> in which the information shall be written</param>
        /// <param name="domain">The active <see cref="DomainOfExpertise"/></param>
        /// <param name="dialogNavigationService">The <see cref="IDialogNavigationService"/></param>
        public ReqIfImportMappingManager(ReqIF reqif, ISession session, Iteration iteration, DomainOfExpertise domain, IDialogNavigationService dialogNavigationService, IThingDialogNavigationService thingDialogNavigationService)
        {
            this.reqIf = reqif;
            this.session = session;
            this.iteration = iteration.Clone(false);
            this.dialogNavigationService = dialogNavigationService;
            this.thingDialogNavigationService = thingDialogNavigationService;
            this.currentDomain = domain;
        }

        /// <summary>
        /// Start the mapping process
        /// </summary>
        public void StartMapping()
        {
            this.NavigateToParameterTypeMappingDialog();
        }

        /// <summary>
        /// NavigateModal to the <see cref="ParameterType"/> mapping dialog
        /// </summary>
        private void NavigateToParameterTypeMappingDialog()
        {
            var dialog = new ParameterTypeMappingDialogViewModel(this.reqIf.Lang, this.reqIf.CoreContent.First().DataTypes, this.datatypeDefinitionMap, this.iteration, this.session, this.thingDialogNavigationService);
            var res = (ParameterTypeMappingDialogResult)this.dialogNavigationService.NavigateModal(dialog);

            if (res == null || !res.Result.HasValue || !res.Result.Value)
            {
                return;
            }

            // set the result of the mapping
            this.datatypeDefinitionMap = res.Map.ToDictionary(x => x.Key, x => x.Value);
            this.NavigateToSpecificationTypeMappingDialog();
        }

        /// <summary>
        /// Navigates to <see cref="SpecificationType"/> mapping dialog
        /// </summary>
        private void NavigateToSpecificationTypeMappingDialog()
        {
            var dialog = new SpecificationTypeMappingDialogViewModel(this.reqIf.CoreContent.First().SpecTypes, this.datatypeDefinitionMap, this.specificationTypeMap, this.iteration, this.session, this.thingDialogNavigationService, this.reqIf.Lang);
            var res = (SpecificationTypeMappingDialogResult) this.dialogNavigationService.NavigateModal(dialog);

            if (res == null || !res.Result.HasValue || !res.Result.Value)
            {
                return;
            }

            this.specificationTypeMap = res.SpecificationTypeMap.ToDictionary(x => x.Key, x => x.Value);
            if (res.GoNext.HasValue && res.GoNext.Value)
            {
                // go next to requirement specification mapping
                this.NavigateToRequirementObjectTypeMappingDialog();
            }
            else if (res.GoNext.HasValue && !res.GoNext.Value)
            {
                // go back to parameter type mapping
                this.NavigateToParameterTypeMappingDialog();
            }
        }

        /// <summary>
        /// NavigateModal to the <see cref="Requirement"/> and <see cref="RequirementsGroup"/> mapping dialog
        /// </summary>
        private void NavigateToRequirementObjectTypeMappingDialog()
        {
            var dialog = new SpecObjectTypesMappingDialogViewModel(this.reqIf.CoreContent.First().SpecTypes, this.datatypeDefinitionMap, this.specObjectTypeMap, this.iteration, this.session, this.thingDialogNavigationService, this.reqIf.Lang);
            var res = (RequirementTypeMappingDialogResult)this.dialogNavigationService.NavigateModal(dialog);

            if (res == null || !res.Result.HasValue || !res.Result.Value)
            {
                return;
            }

            this.specObjectTypeMap = res.ReqCategoryMap.ToDictionary(x => x.Key, x => x.Value);
            if (res.GoNext.HasValue && res.GoNext.Value)
            {
                // go next to requirement specification mapping
                this.NavigateToRelationshipGroupDialog();
            }
            else if (res.GoNext.HasValue && !res.GoNext.Value)
            {
                // go back to parameter type mapping
                this.NavigateToSpecificationTypeMappingDialog();
            }
        }

        /// <summary>
        /// Navigate to the <see cref="SpecRelationType"/> mapping dialog
        /// </summary>
        private void NavigateToRelationshipGroupDialog()
        {
            var relationgroups = this.reqIf.CoreContent.First().SpecTypes.OfType<RelationGroupType>();
            var dialog = new RelationGroupTypeMappingDialogViewModel(relationgroups, this.relationGroupTypeMap, this.datatypeDefinitionMap, this.iteration, this.session, this.thingDialogNavigationService, this.reqIf.Lang);
            var res = (RelationshipGroupMappingDialogResult)this.dialogNavigationService.NavigateModal(dialog);

            if (res == null || !res.Result.HasValue || !res.Result.Value)
            {
                return;
            }

            this.relationGroupTypeMap = res.Map.ToDictionary(x => x.Key, y => y.Value);
            if (res.GoNext.HasValue && res.GoNext.Value)
            {
                this.NavigateToRelationshipDialog();
            }
            else if (res.GoNext.HasValue && !res.GoNext.Value)
            {
                this.NavigateToRequirementObjectTypeMappingDialog();
            }
        }

        /// <summary>
        /// NavigateModal to the <see cref="BinaryRelationship"/> mapping dialog
        /// </summary>
        private void NavigateToRelationshipDialog()
        {
            var dialog = new SpecRelationTypeMappingDialogViewModel(this.reqIf.CoreContent.Single().SpecTypes.OfType<SpecRelationType>(), this.specRelationTypeMap, this.datatypeDefinitionMap, this.iteration, this.session, this.thingDialogNavigationService, this.reqIf.Lang);
            var res = (RelationshipMappingDialogResult)this.dialogNavigationService.NavigateModal(dialog);

            // go back or mapping operation over.
            if (res == null || !res.Result.HasValue || !res.Result.Value)
            {
                return;
            }

            this.specRelationTypeMap = res.Map.ToDictionary(x => x.Key, x => x.Value);
            if (res.GoNext.HasValue && res.GoNext.Value)
            {
                this.NavigateToRequirementSpecificationMappingDialog();
            }
            else if (res.GoNext.HasValue && !res.GoNext.Value)
            {
                this.NavigateToRelationshipGroupDialog();
            }
        }

        /// <summary>
        /// NavigateModal to the <see cref="RequirementsSpecification"/> mapping dialog
        /// </summary>
        private void NavigateToRequirementSpecificationMappingDialog()
        {
            var typeMap = new Dictionary<SpecType, SpecTypeMap>();

            foreach (var specTypeMap in this.specificationTypeMap)
            {
                typeMap.Add(specTypeMap.Key, specTypeMap.Value);
            }

            foreach (var relationTypeMap in this.specRelationTypeMap)
            {
                typeMap.Add(relationTypeMap.Key, relationTypeMap.Value);
            }

            foreach (var map in this.specObjectTypeMap)
            {
                typeMap.Add(map.Key, map.Value);
            }

            foreach (var groupTypeMap in this.relationGroupTypeMap)
            {
                typeMap.Add(groupTypeMap.Key, groupTypeMap.Value);
            }

            var thingFactory = new ThingFactory(this.iteration, this.datatypeDefinitionMap, typeMap, this.currentDomain, this.reqIf.Lang);
            thingFactory.ComputeRequirementThings(this.reqIf);

            var dialog = new RequirementSpecificationMappingDialogViewModel(thingFactory, this.iteration, this.session, this.thingDialogNavigationService, this.reqIf.Lang);
            var res = (MappingDialogNavigationResult)this.dialogNavigationService.NavigateModal(dialog);

            if (res == null || !res.Result.HasValue || !res.Result.Value)
            {
                return;
            }

            if (res.Result.Value && res.GoNext.HasValue && !res.GoNext.Value)
            {
                this.NavigateToRelationshipDialog();
            }
        }
    }
}