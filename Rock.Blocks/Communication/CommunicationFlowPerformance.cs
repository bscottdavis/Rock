// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//

using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.ViewModels.Blocks;
using Rock.ViewModels.Blocks.Communication.CommunicationFlowPerformance;
using Rock.ViewModels.Utility;
using Rock.Web.Cache;

namespace Rock.Blocks.Communication
{
    /// <summary>
    /// Displays the performance of a particular communication flow.
    /// </summary>

    [DisplayName( "Communication Flow Performance" )]
    [Category( "Communication" )]
    [Description( "Displays the performance of a particular communication flow." )]
    [IconCssClass( "fa fa-line-chart" )]
    // [SupportedSiteTypes( Model.SiteType.Web )]

    #region Block Attributes

    #endregion
    
    [Rock.SystemGuid.EntityTypeGuid( "53FFF4C5-1F30-415C-BE65-53FCA7F2CD64" )]
    [Rock.SystemGuid.BlockTypeGuid( "72B92AA4-2AA7-4FDD-9CD7-7DD84018B21E" )]
    public class CommunicationFlowPerformance : RockBlockType
    {
        #region Keys

        private static class PageParameterKey
        {
            public const string CommunicationFlow = "CommunicationFlow";
        }

        private static class NavigationUrlKey
        {
            public const string ParentPage = "ParentPage";
        }

        #endregion Keys

        #region Methods

        /// <inheritdoc/>
        public override object GetObsidianBlockInitialization()
        {
            var box = new CommunicationFlowPerformanceInitializationBox();
          
            box.NavigationUrls = GetBoxNavigationUrls();
            box.Flow = GetFlow( this.RockContext );

            return box;
        }

        /// <summary>
        /// Gets the box navigation URLs required for the page to operate.
        /// </summary>
        /// <returns>A dictionary of key names and URL values.</returns>
        private Dictionary<string, string> GetBoxNavigationUrls()
        {
            return new Dictionary<string, string>
            {
                [NavigationUrlKey.ParentPage] = this.GetParentPageUrl()
            };
        }

        private CommunicationFlowPerformanceFlowBag GetFlow( RockContext rockContext )
        {
            var flowKey = PageParameter( PageParameterKey.CommunicationFlow );
            if ( flowKey.IsNullOrWhiteSpace() )
            {
                return null;
            }

            var flow = new CommunicationFlowService( rockContext )
                .GetQueryableByKey( flowKey, !this.PageCache.Layout.Site.DisablePredictableIds )
                // TBD: Add Includes.
                .FirstOrDefault();

            var bag = new CommunicationFlowPerformanceFlowBag
            {
                ConversionGoalType = flow.ConversionGoalType,
                // Load the flow data here, e.g. from the database
                // This is a placeholder; actual implementation will depend on your data model
                ConversionGoalSettings = GetConversionGoalSettingsBag( flow ),
                Instances = GetCommunicationFlowInstanceBags( flow.CommunicationFlowInstances )
            };

            return bag;
        }

        private List<CommunicationFlowPerformanceFlowInstanceBag> GetCommunicationFlowInstanceBags( IEnumerable<CommunicationFlowInstance> communicationFlowInstances )
        {
            var communicationFlowInstanceBags = new List<CommunicationFlowPerformanceFlowInstanceBag>();

            // Process instance data.
            foreach ( var communicationFlowInstance in communicationFlowInstances )
            {
                var instanceBag = new CommunicationFlowPerformanceFlowInstanceBag
                {
                    StartDate = communicationFlowInstance.StartDate,
                    // TBD: Process info needed from conversions.
                    ConversionHistories = GetCommunicationFlowInstanceConversionHistoryBags( communicationFlowInstance.CommunicationFlowInstanceConversionHistories ),
                    // TBD: Process info needed from communications.
                    Communications = communicationFlowInstance.CommunicationFlowInstanceCommunications,
                    // TBD: Process info needed from this instance's set or subset of recipients (recipients will fall off the flow as they unsubscribe).
                    Recipients = communicationFlowInstance.CommunicationFlowInstanceRecipients,
                    communicationFlowInstance.CreatedDateTime,
                    communicationFlowInstance.ModifiedDateTime,
                };

                bag.Instances.Add( instanceBag );
            }

            return communicationFlowInstanceBags;
        }

        private List<CommunicationFlowPerformanceConversionHistoryBag> GetCommunicationFlowInstanceConversionHistoryBags( IEnumerable<CommunicationFlowInstanceConversionHistory> conversionHistories )
        {
            var conversionHistoryBags = new List<CommunicationFlowPerformanceConversionHistoryBag>();

            foreach ( var conversionHistory in conversionHistories )
            {
                var conversionHistoryBag = new CommunicationFlowPerformanceConversionHistoryBag
                {
                    ConversionDateTime = conversionHistory.,
                    ConversionType = conversionHistory.ConversionType,
                    // TBD: Add any additional properties needed from the conversion history.
                    // e.g. conversionHistory.SomeProperty
                };
                conversionHistoryBags.Add( conversionHistoryBag );
            }

            return conversionHistoryBags;
        }

        private CommunicationFlowPerformanceConversionGoalSettingsBag GetConversionGoalSettingsBag( CommunicationFlow entity )
        {
            var settings = entity.GetConversionGoalSettings();

            if ( settings == null )
            {
                return null;
            }

            var bag = new CommunicationFlowPerformanceConversionGoalSettingsBag();

            if ( settings.CompletedFormSettings != null )
            {
                bag.CompletedFormSettings = new CommunicationFlowPerformanceCompletedFormSettingsBag
                {
                    WorkflowType = WorkflowTypeCache.Get( settings.CompletedFormSettings.WorkflowTypeGuid ).ToListItemBag(),
                };
            }

            if ( settings.JoinedGroupTypeSettings != null )
            {
                bag.JoinedGroupTypeSettings = new CommunicationFlowPerformanceJoinedGroupTypeSettingsBag
                {
                    GroupType = GroupTypeCache.Get( settings.JoinedGroupTypeSettings.GroupTypeGuid ).ToListItemBag()
                };
            }

            if ( settings.JoinedGroupSettings != null )
            {
                bag.JoinedGroupSettings = new CommunicationFlowPerformanceJoinedGroupSettingsBag
                {
                    Group = GroupCache.Get( settings.JoinedGroupSettings.GroupGuid ).ToListItemBag()
                };
            }

            if ( settings.RegisteredSettings != null )
            {
                var registrationInstance = new RegistrationInstanceService( this.RockContext )
                    .Queryable()
                    .Where( ri => ri.Guid == settings.RegisteredSettings.RegistrationInstanceGuid )
                    .Select( ri => new ListItemBag
                    {
                        Text = ri.Name,
                        Value = ri.Guid.ToString()
                    } )
                    .FirstOrDefault();

                bag.RegisteredSettings = new CommunicationFlowPerformanceRegisteredSettingsBag
                {
                    RegistrationInstance = registrationInstance
                };
            }

            if ( settings.TookStepSettings != null )
            {
                bag.TookStepSettings = new CommunicationFlowPerformanceTookStepSettingsBag
                {
                    StepType = StepTypeCache.Get( settings.TookStepSettings.StepTypeGuid ).ToListItemBag()
                };
            }

            if ( settings.EnteredDataViewSettings != null )
            {
                bag.EnteredDataViewSettings = new CommunicationFlowPerformanceEnteredDataViewSettingsBag
                {
                    DataView = DataViewCache.Get( settings.EnteredDataViewSettings.DataViewGuid ).ToListItemBag()
                };
            }

            return bag;
        }
        

        #endregion

        #region Block Actions

        #endregion
    }
}
