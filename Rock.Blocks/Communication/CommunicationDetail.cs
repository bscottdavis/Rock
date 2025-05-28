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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;

using Rock.Attribute;
using Rock.Constants;
using Rock.Model;
using Rock.Obsidian.UI;
using Rock.Security;
using Rock.Utility;
using Rock.ViewModels.Blocks.Communication.CommunicationDetail;
using Rock.ViewModels.Controls;
using Rock.ViewModels.Reporting;
using Rock.Web.Cache;

using CommunicationType = Rock.Enums.Communication.CommunicationType;

namespace Rock.Blocks.Communication
{
    /// <summary>
    /// Used for displaying details of an existing communication that has already been created.
    /// </summary>

    [DisplayName( "Communication Detail" )]
    [Category( "Communication" )]
    [Description( "Used for displaying details of an existing communication that has already been created." )]
    // [SupportedSiteTypes( Model.SiteType.Web )]

    #region Block Attributes

    [SecurityAction( Authorization.APPROVE, "The roles and/or users that have access to approve new communications." )]

    [BooleanField( "Enable Personal Templates",
        Key = AttributeKey.EnablePersonalTemplates,
        Description = "Should support for personal templates be enabled? These are templates that a user can create and are personal to them. If enabled, they will be able to create a new template based on the current communication.",
        DefaultBooleanValue = false,
        Order = 0,
        IsRequired = false )]

    #endregion Block Attributes

    [Rock.SystemGuid.EntityTypeGuid( "32838848-2423-4BD9-B5EF-5F7E6AC7F5F4" )]
    [Rock.SystemGuid.BlockTypeGuid( "2B63C6ED-20D5-467E-9A6A-C608E1D953E5" )]
    public class CommunicationDetail : RockBlockType
    {
        #region Keys & Constants

        private static class AttributeKey
        {
            public const string EnablePersonalTemplates = "EnablePersonalTemplates";
        }

        private static class PageParameterKey
        {
            // "Communication" allows Communication Id, Guid, or IdKey values,
            // while the older "CommunicationId" only supports Id.
            public const string Communication = "Communication";
            public const string CommunicationId = "CommunicationId";

            public const string Edit = "Edit";
        }

        private static class PersonPreferenceKey
        {
            public const string RecipientGridSettings = "recipient-list-settings";
        }

        private static class PersonPropertyColumn
        {
            public static string Age = "Age";
            public static string AgeClassification = "AgeClassification";
            public static string Birthdate = "BirthDate";
            public static string Email = "Email";
            public static string Gender = "Gender";
            public static string Grade = "Grade";
            public static string IsActive = "IsActive";
            public static string IsDeceased = "IsDeceased";
        }

        private static class InteractionOperation
        {
            public const string Opened = "Opened";
            public const string Click = "Click";
        }

        private static class SankeyNode
        {
            public static SankeyDiagramNodeBag Sent => new SankeyDiagramNodeBag
            {
                Id = 1,
                Name = "Sent",
                Color = "#6B46C1"
            };

            public static SankeyDiagramNodeBag Delivered => new SankeyDiagramNodeBag
            {
                Id = 2,
                Name = "Delivered",
                Color = "#D7F4DE"
            };

            public static SankeyDiagramNodeBag Failed => new SankeyDiagramNodeBag
            {
                Id = 3,
                Name = "Failed",
                Color = "#FED7D7"
            };

            public static SankeyDiagramNodeBag Pending => new SankeyDiagramNodeBag
            {
                Id = 4,
                Name = "Pending",
                Color = "#8B8BA7"
            };

            public static SankeyDiagramNodeBag Cancelled => new SankeyDiagramNodeBag
            {
                Id = 5,
                Name = "Cancelled",
                Color = "#FFECD1"
            };

            public static SankeyDiagramNodeBag Opened => new SankeyDiagramNodeBag
            {
                Id = 6,
                Name = "Opened",
                Color = "#3182CE"
            };

            public static SankeyDiagramNodeBag Clicked => new SankeyDiagramNodeBag
            {
                Id = 7,
                Name = "Clicked",
                Color = "#2F855A"
            };

            public static SankeyDiagramNodeBag MarkedAsSpam => new SankeyDiagramNodeBag
            {
                Id = 8,
                Name = "Marked As Spam",
                Color = "#DD6B20"
            };

            public static SankeyDiagramNodeBag Unsubscribed => new SankeyDiagramNodeBag
            {
                Id = 9,
                Name = "Unsubscribed",
                Color = "#C53030"
            };
        }

        #endregion Keys & Constants

        #region Fields

        /// <summary>
        /// The backing field for the <see cref="CommunicationTypeByMediumEntityTypeId"/> property.
        /// </summary>
        private static readonly Lazy<Dictionary<int, CommunicationType>> _communicationTypeByMediumEntityTypeId = new Lazy<Dictionary<int, CommunicationType>>( () =>
        {
            var dict = new Dictionary<int, CommunicationType>();

            if ( EmailMediumEntityTypeId > 0 )
            {
                dict.Add( EmailMediumEntityTypeId, CommunicationType.Email );
            }

            if ( SmsMediumEntityTypeId > 0 )
            {
                dict.Add( SmsMediumEntityTypeId, CommunicationType.SMS );
            }

            if ( PushNotificationMediumEntityTypeId > 0 )
            {
                dict.Add( PushNotificationMediumEntityTypeId, CommunicationType.PushNotification );
            }

            return dict;
        } );

        /// <summary>
        /// The backing field for the <see cref="RecipientGridSettings"/> property.
        /// </summary>
        private CommunicationRecipientGridSettingsBag _recipientGridSettings;

        /// <summary>
        /// The backing field for the <see cref="RecipientGridAttributeColumns"/> property.
        /// </summary>
        private List<AttributeCache> _recipientGridAttributeColumns;

        #endregion Fields

        #region Properties

        /// <summary>
        /// A lazy-loaded dictionary of <see cref="CommunicationType"/>s by medium <see cref="EntityType"/> identifiers.
        /// </summary>
        private static Dictionary<int, CommunicationType> CommunicationTypeByMediumEntityTypeId = _communicationTypeByMediumEntityTypeId.Value;

        /// <summary>
        /// Gets the block person preferences.
        /// </summary>
        private PersonPreferenceCollection BlockPersonPreferences => this.GetBlockPersonPreferences();

        /// <summary>
        /// Gets the <see cref="CommunicationRecipientGridSettingsBag"/> from block person preferences.
        /// </summary>
        public CommunicationRecipientGridSettingsBag RecipientGridSettings
        {
            get
            {
                if ( _recipientGridSettings == null )
                {
                    _recipientGridSettings = BlockPersonPreferences
                        .GetValue( PersonPreferenceKey.RecipientGridSettings )
                        .FromJsonOrNull<CommunicationRecipientGridSettingsBag>() ?? new CommunicationRecipientGridSettingsBag();
                }

                return _recipientGridSettings;
            }
        }

        /// <summary>
        /// Gets the <see cref="Person"/> attributes to add as columns to the recipient grid.
        /// </summary>
        public List<AttributeCache> RecipientGridAttributeColumns
        {
            get
            {
                if ( _recipientGridAttributeColumns == null )
                {
                    if ( RecipientGridSettings.SelectedAttributes?.Any() != true )
                    {
                        // No attributes selected.
                        _recipientGridAttributeColumns = new List<AttributeCache>();
                    }
                    else
                    {
                        // Get the selected attributes from cache.
                        var personEntityTypeId = EntityTypeCache.Get<Person>().Id;

                        _recipientGridAttributeColumns = AttributeCache
                            .GetByEntityType( personEntityTypeId )
                            .Where( a => RecipientGridSettings.SelectedAttributes.Contains( a.Guid ) )
                            .OrderBy( a => a.Order )
                            .ThenBy( a => a.Name )
                            .ThenBy( a => a.Id )
                            .ToList();
                    }
                }

                return _recipientGridAttributeColumns;
            }
        }

        /// <summary>
        /// Gets the Communication entity key passed to the "Communication" or "CommunicationId" page parameter.
        /// </summary>
        private string CommunicationOrCommunicationIdPageParameter
        {
            get
            {
                var communicationPageParameter = PageParameter( PageParameterKey.Communication );

                if ( communicationPageParameter.IsNotNullOrWhiteSpace() )
                {
                    return communicationPageParameter;
                }
                else
                {
                    // Only allow the CommunicationId to contain an ID, but return it as a string so it can be used as an entity key.
                    return PageParameter( PageParameterKey.CommunicationId ).AsIntegerOrNull()?.ToString();
                }
            }
        }

        /// <summary>
        /// Gets the email medium <see cref="EntityType"/> identifier.
        /// </summary>
        private static int EmailMediumEntityTypeId => EntityTypeCache.Get( Rock.SystemGuid.EntityType.COMMUNICATION_MEDIUM_EMAIL.AsGuid() )?.Id ?? 0;

        /// <summary>
        /// Gets the SMS medium <see cref="EntityType"/> identifier.
        /// </summary>
        private static int SmsMediumEntityTypeId => EntityTypeCache.Get( Rock.SystemGuid.EntityType.COMMUNICATION_MEDIUM_SMS.AsGuid() )?.Id ?? 0;

        /// <summary>
        /// Gets the push notification medium <see cref="EntityType"/> identifier.
        /// </summary>
        private static int PushNotificationMediumEntityTypeId => EntityTypeCache.Get( Rock.SystemGuid.EntityType.COMMUNICATION_MEDIUM_PUSH_NOTIFICATION.AsGuid() )?.Id ?? 0;

        #endregion Properties

        #region RockBlockType Implementation

        /// <inheritdoc/>
        public override object GetObsidianBlockInitialization()
        {
            var communicationWithDeliveryBreakdown = LoadCommunicationFromPageParameter();

            var box = new CommunicationDetailInitializationBox
            {
                IsHidden = GetIsBlockHidden( communicationWithDeliveryBreakdown?.Communication )
            };

            if ( box.IsHidden )
            {
                // Return early if the block should be hidden.
                return box;
            }

            if ( !GetIsAuthorizedToView( communicationWithDeliveryBreakdown.Communication ) )
            {
                // Return early if the current person is not authorized to view the communication.
                box.ErrorMessage = EditModeMessage.NotAuthorizedToView( Rock.Model.Communication.FriendlyTypeName );
                return box;
            }

            box.CommunicationDetail = GetCommunicationDetailBag( communicationWithDeliveryBreakdown );
            box.IsPersonalTemplateEnabled = GetAttributeValue( AttributeKey.EnablePersonalTemplates ).AsBoolean();

            box.RecipientGridDefinition = GetRecipientGridBuilder().BuildDefinition();

            return box;
        }

        #endregion RockBlockType Implementation

        #region Block Actions

        /// <summary>
        /// Gets the communication analytics.
        /// </summary>
        /// <param name="bag">The information needed to get communication analytics.</param>
        /// <returns>An object containing information about the communication analytics.</returns>
        [BlockAction]
        public BlockActionResult GetCommunicationAnalytics( CommunicationAnalyticsRequestBag bag )
        {
            if ( bag == null )
            {
                return ActionBadRequest( "Unable to load analytics data." );
            }

            var response = new CommunicationAnalyticsResponseBag();

            // Get the communication and recipient info [for the filtered medium entity type].
            var communicationWithDeliveryBreakdown = LoadCommunicationFromPageParameter( bag.Type );

            if ( !GetIsAuthorizedToView( communicationWithDeliveryBreakdown?.Communication ) )
            {
                return ActionUnauthorized( EditModeMessage.NotAuthorizedToView( Rock.Model.Communication.FriendlyTypeName ) );
            }

            var deliveryBreakdown = GetCommunicationDetailBag( communicationWithDeliveryBreakdown ).DeliveryBreakdown;
            response.DeliveryBreakdown = deliveryBreakdown;

            if ( deliveryBreakdown == null || deliveryBreakdown.CommunicationType == CommunicationType.SMS )
            {
                // Either we don't have a delivery breakdown (should never happen), OR the client is requesting
                // analytics data for SMS (which we don't track); exit early.
                return ActionOk( response );
            }

            // Get the common interaction data that will be used to drive most of the remaining visuals.
            var interactions = GetInteractions(
                communicationWithDeliveryBreakdown.Communication.Id,
                communicationWithDeliveryBreakdown.MediumEntityTypeFilterId
            );

            if ( !interactions.Any() )
            {
                response.ShowNoActivityMessage = true;
                return ActionOk( response );
            }

            // Group the interactions by operation (e.g. "Opened", "Click").
            var groupedInteractions = new GroupedInteractions( interactions );

            response.Kpis = GetCommunicationKpis(
                deliveryBreakdown.DeliveredCount,
                groupedInteractions,
                communicationWithDeliveryBreakdown.UnsubscribeEvents
            );

            response.UniqueInteractionsOverTime = GetUniqueInteractionsOverTime(
                communicationWithDeliveryBreakdown,
                groupedInteractions,
                deliveryBreakdown.CommunicationType
            );

            response.ActivityFlow = GetActivityFlow( communicationWithDeliveryBreakdown, groupedInteractions );

            response.UniqueOpensByGender = GetUniqueOpensByGender( groupedInteractions.UniqueOpens );
            response.UniqueOpensByAgeRange = GetUniqueOpensByAgeRange( groupedInteractions.UniqueOpens );

            var clients = GetClients( communicationWithDeliveryBreakdown.Communication.Id );

            response.TopClients = clients.top;
            response.AllClients = clients.all;

            if ( deliveryBreakdown.CommunicationType != CommunicationType.Email )
            {
                // The remaining analytics are only relevant for email communications; exit early.
                return ActionOk( response );
            }

            response.AllLinksAnalytics = GetAllLinksAnalytics( deliveryBreakdown.DeliveredCount, groupedInteractions );

            return ActionOk( response );
        }

        /// <summary>
        /// Gets the recipient log grid data.
        /// </summary>
        /// <returns>A bag containing the recipient log grid data.</returns>
        [BlockAction]
        public BlockActionResult GetRecipientGridData()
        {
            // Check page parameter for existing communication.
            var communicationKey = this.CommunicationOrCommunicationIdPageParameter;
            if ( communicationKey.IsNullOrWhiteSpace() )
            {
                return ActionBadRequest();
            }

            // Eager-load related entities needed for authorization checks.
            var communicationWithRecipientRows = new CommunicationService( RockContext )
                .GetQueryableByKey( communicationKey, !this.PageCache.Layout.Site.DisablePredictableIds )
                .Include( c => c.CommunicationTemplate )
                .Include( c => c.SystemCommunication )
                .AsNoTracking()
                .Select( c => new
                {
                    Communication = c,
                    RecipientRows = c.Recipients
                        .Where( r => r.PersonAlias != null )
                        .Select( r => new CommunicationRecipientRow
                        {
                            CommunicationRecipientId = r.Id,
                            Person = r.PersonAlias.Person,
                            MediumEntityTypeId = r.MediumEntityTypeId,
                            WasDelivered = r.Status == CommunicationRecipientStatus.Delivered,
                            WasFailedDelivery = r.Status == CommunicationRecipientStatus.Failed,
                            LastActivityDateTime = r.ModifiedDateTime, // This will likely be overwritten below.
                            UnsubscribedDateTime = r.UnsubscribeDateTime,
                            MarkedAsSpamDateTime = null // TODO (Jason): Need to add this column to [CommunicationRecipient]
                        } )
                } )
                .AsEnumerable() // Materialize the query.
                .Select( a => new CommunicationWithRecipientRows
                {
                    Communication = a.Communication,
                    RecipientRows = a.RecipientRows.ToList()
                } )
                .FirstOrDefault();

            if ( !GetIsAuthorizedToView( communicationWithRecipientRows?.Communication ) )
            {
                return ActionUnauthorized( EditModeMessage.NotAuthorizedToView( Rock.Model.Communication.FriendlyTypeName ) );
            }

            // Load interactions so we can supplement each row with this data.
            var groupedInteractions = new GroupedInteractions( GetInteractions( communicationWithRecipientRows.Communication.Id ) );

            foreach ( var row in communicationWithRecipientRows.RecipientRows )
            {
                if ( row.MediumEntityTypeId.HasValue && CommunicationTypeByMediumEntityTypeId.TryGetValue( row.MediumEntityTypeId.Value, out var communicationType ) )
                {
                    row.CommunicationType = communicationType;
                }

                if ( groupedInteractions.AllOpensByRecipientId.TryGetValue( row.CommunicationRecipientId, out var opens ) && opens.Any() )
                {
                    row.OpensCount = opens.Count;
                    row.OpenedDateTime = opens.Last().InteractionDateTime;
                    if ( !row.LastActivityDateTime.HasValue || row.OpenedDateTime > row.LastActivityDateTime )
                    {
                        row.LastActivityDateTime = row.OpenedDateTime;
                    }
                }

                if ( groupedInteractions.AllClicksByRecipientId.TryGetValue( row.CommunicationRecipientId, out var clicks ) && clicks.Any() )
                {
                    row.ClicksCount = clicks.Count;
                    row.ClickedDateTime = clicks.Last().InteractionDateTime;
                    if ( !row.LastActivityDateTime.HasValue || row.ClickedDateTime > row.LastActivityDateTime )
                    {
                        row.LastActivityDateTime = row.ClickedDateTime;
                    }
                }
            }

            var builder = GetRecipientGridBuilder();
            var gridDataBag = builder.Build( communicationWithRecipientRows.RecipientRows.OrderByDescending( r => r.LastActivityDateTime ) );

            return ActionOk( gridDataBag );
        }

        #endregion Block Actions

        #region Private Methods

        /// <summary>
        /// Loads an existing <see cref="Rock.Model.Communication"/> - along with its recipient info - based on the
        /// page parameter.
        /// </summary>
        /// <param name="communicationTypeOverride">
        /// The optional <see cref="CommunicationType"/> for which to get the <see cref="RecipientCountBreakdown"/>.
        /// </param>
        /// <returns>
        /// A <see cref="CommunicationWithDeliveryBreakdown"/> or <see langword="null"/> if unable to find the
        /// <see cref="Rock.Model.Communication"/>.
        /// </returns>
        private CommunicationWithDeliveryBreakdown LoadCommunicationFromPageParameter( CommunicationType? communicationTypeOverride = null )
        {
            // Check page parameter for existing communication.
            var communicationKey = this.CommunicationOrCommunicationIdPageParameter;
            if ( communicationKey.IsNullOrWhiteSpace() )
            {
                return null;
            }

            // Eager-load related entities needed for authorization checks.
            return new CommunicationService( RockContext )
                .GetQueryableByKey( communicationKey, !this.PageCache.Layout.Site.DisablePredictableIds )
                .Include( c => c.CommunicationTemplate )
                .Include( c => c.SystemCommunication )
                .AsNoTracking()
                .Select( c => new
                {
                    Communication = c,
                    TotalRecipientCount = c.Recipients.Count(),
                    Counts = c.Recipients
                        .GroupBy( r => new { r.MediumEntityTypeId, r.Status } )
                        .Select( g => new
                        {
                            g.Key.MediumEntityTypeId,
                            g.Key.Status,
                            Count = g.Count()
                        } ),
                    UnsubscribeEvents = c.Recipients
                        // TODO (Jason): "Marked as SPAM" is separate from an unsubscribe.
                        .Where( r => r.CausedUnsubscribe == true )
                        .Select( r => new RecipientUnsubscribeEvent
                        {
                            CommunicationRecipientId = r.Id,
                            MediumEntityTypeId = r.MediumEntityTypeId,
                            UnsubscribeDateTime = r.UnsubscribeDateTime,
                            IsSpamComplaint = false
                        } )
                } )
                .AsEnumerable() // Materialize the query; we'll perform the remaining aggregations in-memory.
                .Select( a =>
                {
                    var communication = a.Communication;

                    var countsByMediumAndStatus = a.Counts
                        .ToDictionary( k => (k.MediumEntityTypeId, k.Status), v => v.Count );

                    var communicationType = communicationTypeOverride ?? ( CommunicationType ) communication.CommunicationType;

                    // If recipient preference, show only email counts; the individual can choose to show SMS later.
                    if ( communicationType == CommunicationType.RecipientPreference )
                    {
                        communicationType = CommunicationType.Email;
                    }

                    // Translate the communication type to the corresponding medium entity type filter ID.
                    int? mediumEntityTypeFilterId = null;
                    if ( communicationType == CommunicationType.Email )
                    {
                        mediumEntityTypeFilterId = EmailMediumEntityTypeId;
                    }
                    else if ( communicationType == CommunicationType.SMS )
                    {
                        mediumEntityTypeFilterId = SmsMediumEntityTypeId;
                    }
                    else
                    {
                        // No need to filter other communication/medium entity types, as all analytics data will already
                        // relate to only those types (e.g. push notifications).
                    }

                    // A local function to get the count of recipients for a given status.
                    int GetCount( CommunicationRecipientStatus s )
                    {
                        if ( mediumEntityTypeFilterId.HasValue )
                        {
                            return countsByMediumAndStatus.TryGetValue( (mediumEntityTypeFilterId.Value, s), out var count ) ? count : 0;
                        }

                        return countsByMediumAndStatus
                            .Where( kvp => kvp.Key.Status == s )
                            .Sum( kvp => kvp.Value );
                    }

                    var recipientCount = mediumEntityTypeFilterId.HasValue
                        ? countsByMediumAndStatus.Where( kvp => kvp.Key.MediumEntityTypeId == mediumEntityTypeFilterId.Value ).Sum( kvp => kvp.Value )
                        : a.TotalRecipientCount;

                    var recipientCountBreakdown = new RecipientCountBreakdown
                    {
                        CommunicationType = communicationType,
                        RecipientCount = recipientCount,
                        PendingCount = GetCount( CommunicationRecipientStatus.Pending ) + GetCount( CommunicationRecipientStatus.Sending ),
                        DeliveredCount = GetCount( CommunicationRecipientStatus.Delivered ) + GetCount( CommunicationRecipientStatus.Opened ),
                        FailedCount = GetCount( CommunicationRecipientStatus.Failed ),
                        CancelledCount = GetCount( CommunicationRecipientStatus.Cancelled )
                    };

                    var filteredUnsubscribeEvents = a.UnsubscribeEvents
                        .Where( e =>
                            !mediumEntityTypeFilterId.HasValue
                            || e.MediumEntityTypeId == mediumEntityTypeFilterId
                        )
                        .ToList();

                    return new CommunicationWithDeliveryBreakdown
                    {
                        Communication = communication,
                        TotalRecipientCount = a.TotalRecipientCount,
                        RecipientCountBreakdown = recipientCountBreakdown,
                        UnsubscribeEvents = filteredUnsubscribeEvents,
                        MediumEntityTypeFilterId = mediumEntityTypeFilterId
                    };
                } )
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets a value indicating whether the block should be hidden.
        /// </summary>
        /// <param name="communication">
        /// The <see cref="Rock.Model.Communication"/> whose <see cref="CommunicationStatus"/> will dictate whether to
        /// hide the block.
        /// </param>
        /// <returns>Whether the block should be hidden.</returns>
        private bool GetIsBlockHidden( Rock.Model.Communication communication )
        {
            return communication == null
                || communication.Status == CommunicationStatus.Transient
                || communication.Status == CommunicationStatus.Draft
                || communication.Status == CommunicationStatus.Denied
                || (
                    communication.Status == CommunicationStatus.PendingApproval
                    && PageParameter( PageParameterKey.Edit ).AsBoolean()
                    && this.BlockCache.IsAuthorized( Authorization.APPROVE, GetCurrentPerson() )
                );
        }

        /// <summary>
        /// Gets whether the current person is authorized to view the communication.
        /// </summary>
        /// <param name="communication">The <see cref="Rock.Model.Communication"/> for which to check authorization.</param>
        /// <returns>Whether the current person is authorized to view the communication.</returns>
        private bool GetIsAuthorizedToView( Rock.Model.Communication communication )
        {
            return communication?.IsAuthorized( Authorization.VIEW, GetCurrentPerson() ) ?? false;
        }

        /// <summary>
        /// Gets a <see cref="CommunicationDetailBag"/> that contains the details of the communication.
        /// </summary>
        /// <param name="communicationWithDeliveryBreakdown">
        /// The <see cref="CommunicationWithDeliveryBreakdown"/> from which to get the <see cref="CommunicationDetailBag"/>.
        /// </param>
        /// <returns>A <see cref="CommunicationDetailBag"/> that contains the details of the communication.</returns>
        private CommunicationDetailBag GetCommunicationDetailBag( CommunicationWithDeliveryBreakdown communicationWithDeliveryBreakdown )
        {
            var communication = communicationWithDeliveryBreakdown?.Communication;
            if ( communication == null )
            {
                return null;
            }

            // TODO (Jason): Pick one of the following name approaches.

            // This is the way the Legacy Communication List block chose the value to display in the grid:
            //// Prefer - in this order - the Subject, PushTitle, or Name of the communication.
            //var name = !string.IsNullOrWhiteSpace( communication.Subject ) ? communication.Subject
            //    : !string.IsNullOrWhiteSpace( communication.PushTitle ) ? communication.PushTitle : communication.Name;

            // This is the way the Legacy Communication Detail block chose the title for the block:
            // Prefer - in this order - Name, Subject, Push Title.
            var name = !string.IsNullOrWhiteSpace( communication.Name ) ? communication.Name
                : !string.IsNullOrWhiteSpace( communication.Subject ) ? communication.Subject : communication.PushTitle;

            CommunicationDeliveryBreakdownBag deliveryBreakdownBag = null;

            var recipientCountBreakdown = communicationWithDeliveryBreakdown.RecipientCountBreakdown;
            if ( recipientCountBreakdown != null )
            {
                decimal GetPercentage( int count )
                {
                    return Math.Round( count / ( decimal ) recipientCountBreakdown.RecipientCount * 100, 1, MidpointRounding.AwayFromZero );
                }

                deliveryBreakdownBag = new CommunicationDeliveryBreakdownBag
                {
                    CommunicationType = recipientCountBreakdown.CommunicationType,
                    RecipientCount = recipientCountBreakdown.RecipientCount,
                    PendingCount = recipientCountBreakdown.PendingCount,
                    PendingPercentage = GetPercentage( recipientCountBreakdown.PendingCount ),
                    DeliveredCount = recipientCountBreakdown.DeliveredCount,
                    DeliveredPercentage = GetPercentage( recipientCountBreakdown.DeliveredCount ),
                    FailedCount = recipientCountBreakdown.FailedCount,
                    FailedPercentage = GetPercentage( recipientCountBreakdown.FailedCount ),
                    CancelledCount = recipientCountBreakdown.CancelledCount,
                    CancelledPercentage = GetPercentage( recipientCountBreakdown.CancelledCount )
                };
            }

            return new CommunicationDetailBag
            {
                Name = name,
                Type = ( CommunicationType ) communication.CommunicationType,
                Status = communication.Status,
                SendDateTime = communication.SendDateTime,
                TotalRecipientCount = communicationWithDeliveryBreakdown.TotalRecipientCount,
                Topic = DefinedValueCache.GetValue( communication.CommunicationTopicValueId ),
                IsBulk = communication.IsBulkCommunication,
                DeliveryBreakdown = deliveryBreakdownBag,
            };
        }

        /// <summary>
        /// Gets a list of <see cref="InteractionInfo"/>s for the given communication.
        /// </summary>
        /// <param name="communicationId">The <see cref="Rock.Model.Communication"/> identifier.</param>
        /// <param name="mediumEntityTypeFilterId">
        /// The medium <see cref="EntityType"/> identifier filter, if interactions should be restricted to a specific medium.
        /// </param>
        /// <returns>A list of <see cref="InteractionInfo"/>s.</returns>
        private List<InteractionInfo> GetInteractions( int communicationId, int? mediumEntityTypeFilterId = null )
        {
            var recipientQuery = new CommunicationRecipientService( RockContext )
                .Queryable()
                .AsNoTracking()
                .Where( r =>
                    r.CommunicationId == communicationId
                    && (
                        !mediumEntityTypeFilterId.HasValue
                        || r.MediumEntityTypeId == mediumEntityTypeFilterId
                    )
                );

            return GetInteractionsQuery( communicationId )
                .Join(
                    recipientQuery,
                    i => i.EntityId,
                    r => r.Id,
                    ( i, r ) => new InteractionInfo
                    {
                        InteractionDateTime = i.InteractionDateTime,
                        Operation = i.Operation,
                        InteractionData = i.InteractionData,
                        CommunicationRecipientId = i.EntityId,
                        PersonGender = r.PersonAlias != null ? r.PersonAlias.Person.Gender : Gender.Unknown,
                        PersonAge = r.PersonAlias != null ? r.PersonAlias.Person.Age : ( int? ) null,
                    }
                )
                .ToList();
        }

        /// <summary>
        /// Gets a Queryable of <see cref="Interaction"/>s for the given communication.
        /// </summary>
        /// <param name="communicationId">The <see cref="Rock.Model.Communication"/> identifier.</param>
        /// <returns>a Queryable of <see cref="Interaction"/>s.</returns>
        private IQueryable<Interaction> GetInteractionsQuery( int communicationId )
        {
            var interactionChannelId = InteractionChannelCache.Get( Rock.SystemGuid.InteractionChannel.COMMUNICATION.AsGuid() )?.Id;

            return new InteractionService( RockContext )
                .Queryable()
                .AsNoTracking()
                .Where( i =>
                    i.InteractionComponent.InteractionChannelId == interactionChannelId
                    && i.InteractionComponent.EntityId == communicationId
                );
        }

        /// <summary>
        /// Gets the KPIs for this communication.
        /// </summary>
        /// <param name="deliveredRecipientCount">The count of recipients to whom this communication was successfully delivered.</param>
        /// <param name="groupedInteractions">The grouped interaction data from which to derive KPIs.</param> 
        /// <param name="unsubscribeEvents">The list of unsubscribe events from which to derive KPIs.</param>
        /// <returns>A <see cref="CommunicationKpisBag"/> containing the KPIs for this communication.</returns>
        private CommunicationKpisBag GetCommunicationKpis( int deliveredRecipientCount, GroupedInteractions groupedInteractions, List<RecipientUnsubscribeEvent> unsubscribeEvents )
        {
            var markedAsSpamCount = unsubscribeEvents.Where( e => e.IsSpamComplaint ).Count();
            var unsubscribedCount = unsubscribeEvents.Count;

            var kpis = new CommunicationKpisBag
            {
                TotalOpensCount = groupedInteractions.TotalOpensCount,
                UniqueOpensCount = groupedInteractions.UniqueOpensCount,
                TotalClicksCount = groupedInteractions.TotalClicksCount,
                UniqueClicksCount = groupedInteractions.UniqueClicksCount,
                TotalMarkedAsSpamCount = markedAsSpamCount,
                TotalUnsubscribesCount = unsubscribedCount,
            };

            decimal GetPercentage( int count, int divisor )
            {
                return Math.Round( count / ( decimal ) divisor * 100, 0, MidpointRounding.AwayFromZero );
            }

            if ( deliveredRecipientCount > 0 )
            {
                kpis.OpenRate = GetPercentage( groupedInteractions.UniqueOpensCount, deliveredRecipientCount );
                kpis.MarkedAsSpamRate = GetPercentage( markedAsSpamCount, deliveredRecipientCount );
                kpis.UnsubscribeRate = GetPercentage( unsubscribedCount, deliveredRecipientCount );
            }

            if ( groupedInteractions.UniqueOpensCount > 0 )
            {
                kpis.ClickThroughRate = GetPercentage( groupedInteractions.UniqueClicksCount, groupedInteractions.UniqueOpensCount );
            }

            return kpis;
        }

        /// <summary>
        /// Gets the unique interactions over time for this communication.
        /// </summary>
        /// <param name="communicationWithDeliveryBreakdown">The <see cref="CommunicationWithDeliveryBreakdown"/>.</param>
        /// <param name="groupedInteractions">The <see cref="GroupedInteractions"/>.</param>
        /// <param name="analyticsCommunicationType">The <see cref="CommunicationType"/> for which we're collecting analytics data.</param>
        /// <returns>A list of <see cref="ChartNumericDataPointBag"/>s representing the unique interactions over time.
        private List<ChartNumericDataPointBag> GetUniqueInteractionsOverTime(
            CommunicationWithDeliveryBreakdown communicationWithDeliveryBreakdown,
            GroupedInteractions groupedInteractions,
            CommunicationType analyticsCommunicationType )
        {
            var uniqueInteractionsOverTime = new List<ChartNumericDataPointBag>();

            var sendDateTime = communicationWithDeliveryBreakdown.Communication.SendDateTime;
            if ( !sendDateTime.HasValue )
            {
                // This communication hasn't been sent yet.
                return uniqueInteractionsOverTime;
            }

            var isEmail = analyticsCommunicationType == CommunicationType.Email;

            var openCountsByDate = groupedInteractions.UniqueOpens
                .GroupBy( o => o.InteractionDateTime.Date )
                .ToDictionary( g => g.Key, g => g.Count() );

            var clickCountsByDate = isEmail
                ? groupedInteractions.UniqueClicks
                    .GroupBy( o => o.InteractionDateTime.Date )
                    .ToDictionary( g => g.Key, g => g.Count() )
                : new Dictionary<DateTime, int>();

            // TODO (Jason): Figure out how we're determining the "marked as SPAM" date.

            var unsubscribeCountsByDate = isEmail
                ? communicationWithDeliveryBreakdown.UnsubscribeEvents
                    .Where( e => e.UnsubscribeDateTime.HasValue )
                    .GroupBy( e => e.UnsubscribeDateTime.Value.Date )
                    .ToDictionary( g => g.Key, g => g.Count() )
                : new Dictionary<DateTime, int>();

            // The start date for this data should always be the date the communication was sent.
            var startDate = sendDateTime.Value.Date;

            // The end date - however - should be driven by the last interaction (or unsubscribe / marked as SPAM event).
            var endDates = openCountsByDate.Keys
                .Union( clickCountsByDate.Keys )
                .Union( unsubscribeCountsByDate.Keys )
                .ToList();

            if ( !endDates.Any() )
            {
                // We have no interactions to report.
                return uniqueInteractionsOverTime;
            }

            var endDate = endDates.Max().Date;

            var deliveredRecipientCount = communicationWithDeliveryBreakdown.RecipientCountBreakdown?.DeliveredCount ?? 0;
            var uniqueOpensCount = groupedInteractions.UniqueOpensCount;

            if ( endDate < startDate || deliveredRecipientCount <= 0 )
            {
                // Should never happen.
                return uniqueInteractionsOverTime;
            }

            // We have everything we need; time to aggregate events into chart data.
            // Each data point is cumulative, not individual, so we'll keep a running total for each event type.
            var currentDate = startDate;
            var openCount = 0;
            var clickCount = 0;
            var markedAsSpamCount = 0;
            var unsubscribeCount = 0;

            decimal GetPercentage( int count, int divisor )
            {
                if ( divisor == 0 )
                {
                    return 0;
                }

                return Math.Round( count / ( decimal ) divisor * 100, 0, MidpointRounding.AwayFromZero );
            }

            while ( currentDate <= endDate )
            {
                var label = currentDate.ToISO8601DateString();

                openCount += openCountsByDate.GetValueOrDefault( currentDate, 0 );
                uniqueInteractionsOverTime.Add( new ChartNumericDataPointBag
                {
                    SeriesName = "Open Rate",
                    Label = label,
                    Value = GetPercentage( openCount, deliveredRecipientCount ),
                    Color = "#2B6CB0"
                } );

                if ( isEmail )
                {
                    clickCount += clickCountsByDate.GetValueOrDefault( currentDate, 0 );
                    uniqueInteractionsOverTime.Add( new ChartNumericDataPointBag
                    {
                        SeriesName = "Click-Through Rate",
                        Label = label,
                        Value = GetPercentage( clickCount, uniqueOpensCount ),
                        Color = "#2F855A"
                    } );

                    // TODO (Jason): Figure out how we're determining the "marked as SPAM" rate.
                    uniqueInteractionsOverTime.Add( new ChartNumericDataPointBag
                    {
                        SeriesName = "Spam Rate",
                        Label = label,
                        Value = 0,
                        Color = "#DD6B20"
                    } );

                    unsubscribeCount += unsubscribeCountsByDate.GetValueOrDefault( currentDate, 0 );
                    uniqueInteractionsOverTime.Add( new ChartNumericDataPointBag
                    {
                        SeriesName = "Unsubscribe Rate",
                        Label = label,
                        Value = GetPercentage( unsubscribeCount, deliveredRecipientCount ),
                        Color = "#C53030"
                    } );
                }

                currentDate = currentDate.AddDays( 1 );
            }

            return uniqueInteractionsOverTime;
        }

        /// <summary>
        /// Gets the activity flow of interactions, Etc. for this communication.
        /// </summary>
        /// <param name="communicationWithDeliveryBreakdown">The <see cref="CommunicationWithDeliveryBreakdown"/>.</param>
        /// <param name="groupedInteractions">The <see cref="GroupedInteractions"/>.</param>
        /// <returns>The activity flow of interactions, Etc. for this communication.</returns>
        private CommunicationActivityFlowBag GetActivityFlow( CommunicationWithDeliveryBreakdown communicationWithDeliveryBreakdown, GroupedInteractions groupedInteractions )
        {
            var recipientCountBreakdown = communicationWithDeliveryBreakdown.RecipientCountBreakdown;

            var level = 1;
            var nodeOrder = 0;

            var sentNode = SankeyNode.Sent;
            sentNode.Order = ++nodeOrder;

            // Start by adding the "Sent" node and edge, as these will always be present.
            var nodes = new List<SankeyDiagramNodeBag> { sentNode };
            var edges = new List<SankeyDiagramEdgeBag>
            {
                new SankeyDiagramEdgeBag
                {
                    TargetId = sentNode.Id,
                    Level = level,
                    Units = recipientCountBreakdown.RecipientCount
                }
            };

            // A local function to assist with edge tooltips.
            string BuildTooltip( string sourceName, string targetName, int units )
            {
                return $@"<strong>{sourceName} > {targetName}:</strong> {units:N0}";
            }

            // Add the "Delivered" node flowing from the "Sent" node only if any communications were actually delivered.
            // Interaction, Etc., nodes will flow from this node, so we'll keep a handle on it for later use.
            level = 2;
            SankeyDiagramNodeBag deliveredNode = null;
            var unitCount = recipientCountBreakdown.DeliveredCount;
            if ( unitCount > 0 )
            {
                deliveredNode = SankeyNode.Delivered;
                deliveredNode.Order = ++nodeOrder;
                nodes.Add( deliveredNode );

                edges.Add( new SankeyDiagramEdgeBag
                {
                    SourceId = sentNode.Id,
                    TargetId = deliveredNode.Id,
                    Level = level,
                    Units = unitCount,
                    Tooltip = BuildTooltip( sentNode.Name, deliveredNode.Name, unitCount )
                } );
            }

            // Add the "Failed", "Pending" and "Cancelled" nodes flowing from the "Sent" node only if we have sends that
            // resulted in each respective outcome.
            unitCount = recipientCountBreakdown.FailedCount;
            if ( unitCount > 0 )
            {
                var failedNode = SankeyNode.Failed;
                failedNode.Order = ++nodeOrder;
                nodes.Add( failedNode );

                edges.Add( new SankeyDiagramEdgeBag
                {
                    SourceId = sentNode.Id,
                    TargetId = failedNode.Id,
                    Level = level,
                    Units = unitCount,
                    Tooltip = BuildTooltip( sentNode.Name, failedNode.Name, unitCount )
                } );
            }

            unitCount = recipientCountBreakdown.PendingCount;
            if ( unitCount > 0 )
            {
                var pendingNode = SankeyNode.Pending;
                pendingNode.Order = ++nodeOrder;
                nodes.Add( pendingNode );

                edges.Add( new SankeyDiagramEdgeBag
                {
                    SourceId = sentNode.Id,
                    TargetId = pendingNode.Id,
                    Level = level,
                    Units = unitCount,
                    Tooltip = BuildTooltip( sentNode.Name, pendingNode.Name, unitCount )
                } );
            }

            unitCount = recipientCountBreakdown.CancelledCount;
            if ( unitCount > 0 )
            {
                var cancelledNode = SankeyNode.Cancelled;
                cancelledNode.Order = ++nodeOrder;
                nodes.Add( cancelledNode );

                edges.Add( new SankeyDiagramEdgeBag
                {
                    SourceId = sentNode.Id,
                    TargetId = cancelledNode.Id,
                    Level = level,
                    Units = unitCount,
                    Tooltip = BuildTooltip( sentNode.Name, cancelledNode.Name, unitCount )
                } );
            }

            if ( deliveredNode != null )
            {
                // Add Interaction [Etc.] nodes flowing from the "Delivered" node, according to this rank:
                // 
                //  [Action]            [Rank]      [Final Bucket]
                // ---------------------------------------------
                //  Unsubscribed        4           Unsubscribed
                //  Marked As Spam      3           Marked As Spam
                //  Clicked             2           Clicked
                //  Opened              1           Opened
                //  Delivered           0           Delivered (no flow)

                level = 3;

                // Keep track of assigned recipient IDs, so we only assign them to a single bucket.
                var level3AssignedRecipientIds = new HashSet<int>();

                // Gather recipients who unsubscribed.
                var unsubscribedRecipientIds = new HashSet<int>(
                    communicationWithDeliveryBreakdown.UnsubscribeEvents
                        .Select( e => e.CommunicationRecipientId )
                        .Distinct()
                );

                level3AssignedRecipientIds.UnionWith( unsubscribedRecipientIds );

                // Gather recipients who marked as spam.
                // TODO (Jason): Figure out how we're determining the "marked as SPAM" recipients.
                var markedAsSpamRecipientIds = new HashSet<int>();

                level3AssignedRecipientIds.UnionWith( markedAsSpamRecipientIds );

                // Gather recipients who clicked.
                var clickedRecipientIds = new HashSet<int>(
                    groupedInteractions.UniqueClicks
                        .Where( c =>
                            c.CommunicationRecipientId.HasValue
                            && !level3AssignedRecipientIds.Contains( c.CommunicationRecipientId.Value )
                        )
                        .Select( c => c.CommunicationRecipientId.Value )
                );

                level3AssignedRecipientIds.UnionWith( clickedRecipientIds );

                // Gather recipients who opened.
                var openedRecipientIds = new HashSet<int>(
                    groupedInteractions.UniqueOpens
                        .Where( c =>
                            c.CommunicationRecipientId.HasValue
                            && !level3AssignedRecipientIds.Contains( c.CommunicationRecipientId.Value )
                        )
                        .Select( c => c.CommunicationRecipientId.Value )
                );

                level3AssignedRecipientIds.UnionWith( openedRecipientIds );

                // Now add them to the sankey in the reverse order.
                // Starting with recipients who opened.
                unitCount = openedRecipientIds.Count;
                if ( unitCount > 0 )
                {
                    var openedNode = SankeyNode.Opened;
                    openedNode.Order = ++nodeOrder;
                    nodes.Add( openedNode );

                    edges.Add( new SankeyDiagramEdgeBag
                    {
                        SourceId = deliveredNode.Id,
                        TargetId = openedNode.Id,
                        Level = level,
                        Units = unitCount,
                        Tooltip = BuildTooltip( deliveredNode.Name, openedNode.Name, unitCount )
                    } );
                }

                // Followed by those who clicked.
                unitCount = clickedRecipientIds.Count;
                if ( unitCount > 0 )
                {
                    var clickedNode = SankeyNode.Clicked;
                    clickedNode.Order = ++nodeOrder;
                    nodes.Add( clickedNode );

                    edges.Add( new SankeyDiagramEdgeBag
                    {
                        SourceId = deliveredNode.Id,
                        TargetId = clickedNode.Id,
                        Level = level,
                        Units = unitCount,
                        Tooltip = BuildTooltip( deliveredNode.Name, clickedNode.Name, unitCount )
                    } );
                }

                // Followed by those who marked as spam.
                unitCount = markedAsSpamRecipientIds.Count;
                if ( unitCount > 0 )
                {
                    var markedAsSpamNode = SankeyNode.MarkedAsSpam;
                    markedAsSpamNode.Order = ++nodeOrder;
                    nodes.Add( markedAsSpamNode );

                    edges.Add( new SankeyDiagramEdgeBag
                    {
                        SourceId = deliveredNode.Id,
                        TargetId = markedAsSpamNode.Id,
                        Level = level,
                        Units = unitCount,
                        Tooltip = BuildTooltip( deliveredNode.Name, markedAsSpamNode.Name, unitCount )
                    } );
                }

                // Followed by those who unsubscribed.
                unitCount = unsubscribedRecipientIds.Count;
                if ( unitCount > 0 )
                {
                    var unsubscribedNode = SankeyNode.Unsubscribed;
                    unsubscribedNode.Order = ++nodeOrder;
                    nodes.Add( unsubscribedNode );

                    edges.Add( new SankeyDiagramEdgeBag
                    {
                        SourceId = deliveredNode.Id,
                        TargetId = unsubscribedNode.Id,
                        Level = level,
                        Units = unitCount,
                        Tooltip = BuildTooltip( deliveredNode.Name, unsubscribedNode.Name, unitCount )
                    } );
                }
            }

            return new CommunicationActivityFlowBag
            {
                Nodes = nodes,
                Edges = edges
            };
        }

        /// <summary>
        /// Gets the unique opens by gender for this communication.
        /// </summary>
        /// <param name="uniqueOpens">The list of <see cref="InteractionInfo"/>s that represent unique opens.</param>
        /// <returns>A list of <see cref="ChartNumericDataPointBag"/>s representing the unique opens by gender.</returns>
        private List<ChartNumericDataPointBag> GetUniqueOpensByGender( List<InteractionInfo> uniqueOpens )
        {
            var uniqueOpensByGender = new List<ChartNumericDataPointBag>();

            var genderGroups = uniqueOpens
                .GroupBy( o => o.PersonGender )
                .ToDictionary( g => g.Key, g => g.Count() );

            var uniqueOpensCount = uniqueOpens.Count();
            decimal GetPercentage( int count )
            {
                if ( uniqueOpensCount == 0 )
                {
                    return 0;
                }

                return Math.Round( count / ( decimal ) uniqueOpensCount * 100, 0, MidpointRounding.AwayFromZero );
            }

            if ( genderGroups.TryGetValue( Gender.Male, out var maleCount ) && maleCount > 0 )
            {
                uniqueOpensByGender.Add( new ChartNumericDataPointBag
                {
                    Label = Gender.Male.ConvertToString(),
                    Value = GetPercentage( maleCount ),
                    Color = "#B2D7FF"
                } );
            }

            if ( genderGroups.TryGetValue( Gender.Female, out var femaleCount ) && femaleCount > 0 )
            {
                uniqueOpensByGender.Add( new ChartNumericDataPointBag
                {
                    Label = Gender.Female.ConvertToString(),
                    Value = GetPercentage( femaleCount ),
                    Color = "#FFB2C1"
                } );
            }

            if ( genderGroups.TryGetValue( Gender.Unknown, out var unknownCount ) && unknownCount > 0 )
            {
                uniqueOpensByGender.Add( new ChartNumericDataPointBag
                {
                    Label = Gender.Unknown.ConvertToString(),
                    Value = GetPercentage( unknownCount ),
                    Color = "#D8D8DA"
                } );
            }

            return uniqueOpensByGender;
        }

        /// <summary>
        /// Gets the unique opens by age range for this communication.
        /// </summary>
        /// <param name="uniqueOpens">The list of <see cref="InteractionInfo"/>s that represent unique opens.</param>
        /// <returns>A list of <see cref="ChartNumericDataPointBag"/>s representing the unique opens by age range.</returns>
        private List<ChartNumericDataPointBag> GetUniqueOpensByAgeRange( List<InteractionInfo> uniqueOpens )
        {
            var labels = new string[] { "18-29", "30-39", "40-49", "50-59", "60-69", "70-79", "80+", "Unknown" };

            var green = "#4CD964";
            var gray = "#D8D8DA";
            var colors = new string[] { green, green, green, green, green, green, green, gray };

            var counts = new int[labels.Length];

            foreach ( var o in uniqueOpens )
            {
                int bucket;
                var age = o.PersonAge.GetValueOrDefault();

                if ( age < 18 )
                {
                    bucket = 7;
                }
                else if ( age < 30 )
                {
                    bucket = 0;
                }
                else if ( age < 40 )
                {
                    bucket = 1;
                }
                else if ( age < 50 )
                {
                    bucket = 2;
                }
                else if ( age < 60 )
                {
                    bucket = 3;
                }
                else if ( age < 70 )
                {
                    bucket = 4;
                }
                else if ( age < 80 )
                {
                    bucket = 5;
                }
                else
                {
                    bucket = 6;
                }

                counts[bucket]++;
            }

            var uniqueOpensByAgeRange = new List<ChartNumericDataPointBag>();

            for ( var i = 0; i < labels.Length; i++ )
            {
                uniqueOpensByAgeRange.Add( new ChartNumericDataPointBag
                {
                    SeriesName = "Age Ranges",
                    Label = labels[i],
                    Value = counts[i],
                    Color = colors[i]
                } );
            }

            return uniqueOpensByAgeRange;
        }

        /// <summary>
        /// Gets analytics data about the clients used to open this communication.
        /// </summary>
        /// <param name="communicationId">The <see cref="Rock.Model.Communication"/> identifier.</param>
        /// <returns>Analytics data about the clients used to open this communication.</returns>
        private (List<ChartNumericDataPointBag> top, List<ChartNumericDataPointBag> all) GetClients( int communicationId )
        {
            var clients = (
                top: ( List<ChartNumericDataPointBag> ) null,
                all: ( List<ChartNumericDataPointBag> ) null
            );

            var seriesName = "Clients";

            var unknownLabel = "Unknown";
            var unknownColor = "#D8D8DA";

            var othersLabel = "Others";
            var othersColor = "#676766";

            // Start by getting the interaction counts by each distinct client (excluding those from robots).
            var clientCounts = GetInteractionsQuery( communicationId )
                .Where( i =>
                    i.InteractionSession == null
                    || i.InteractionSession.DeviceType == null
                    || !i.InteractionSession.DeviceType.ClientType.Equals( "robot", StringComparison.OrdinalIgnoreCase )
                )
                .GroupBy( i =>
                    i.InteractionSession == null
                    || i.InteractionSession.DeviceType == null
                    || i.InteractionSession.DeviceType.Application == null
                        ? unknownLabel
                        : i.InteractionSession.DeviceType.Application
                )
                .Select( g => new
                {
                    ClientName = g.Key,
                    Count = g.Count()
                } )
                .OrderByDescending( a => a.Count )
                .ToList();

            var totalInteractionsCount = clientCounts.Sum( c => c.Count );
            if ( totalInteractionsCount == 0 )
            {
                return clients;
            }

            // Determine each client's percentage of the whole while adding them to the "all" collection.
            clients.all = clientCounts
                .Select( c => new ChartNumericDataPointBag
                {
                    SeriesName = seriesName,
                    Label = c.ClientName,
                    Value = c.Count / ( decimal ) totalInteractionsCount * 100
                } )
                .ToList();

            // Rules for displaying "Top" email clients:
            //  1. If client count < 5, show a "Top" entry for each client.
            //  2. Otherwise, show the top 3 + "Others" where "Others" = sum of all remaining clients' percentages.
            clients.top = new List<ChartNumericDataPointBag>();

            // Ensure we've defined enough colors, up to the max number of bars.
            var maxNumberOfBars = 4;
            var colorQueue = new Queue<string>( new[] { "#5DA5DA", "#60BD68", "#FFBF2F", "#F36F13" } );

            string GetColor( string label ) =>
                label.Equals( unknownLabel )
                    ? unknownColor
                    : colorQueue.Count > 0 ? colorQueue.Dequeue() : "#999999";

            decimal GetRoundedValue( decimal value ) => Math.Round( value, 1, MidpointRounding.AwayFromZero );

            // Add a bar for each client, up to the max minus one.
            clients.top.AddRange(
                clients.all.Take( maxNumberOfBars - 1 ).Select( dataPoint =>
                {
                    dataPoint.Color = GetColor( dataPoint.Label );
                    dataPoint.Value = GetRoundedValue( dataPoint.Value );

                    return dataPoint;
                } )
            );

            // Then decide whether the remainder of the clients need to be aggregated into "Others".
            var remainder = clients.all.Skip( maxNumberOfBars - 1 ).ToList();
            if ( remainder.Count < 2 )
            {
                // Add the remaining 1 [or 0] to the "Top" collection.
                clients.top.AddRange(
                    remainder.Select( dataPoint =>
                    {
                        dataPoint.Color = GetColor( dataPoint.Label );
                        dataPoint.Value = GetRoundedValue( dataPoint.Value );

                        return dataPoint;
                    } )
                );
            }
            else
            {
                // Aggregate into "Others" and add it to the "Top" collection.
                clients.top.Add( new ChartNumericDataPointBag
                {
                    SeriesName = seriesName,
                    Label = othersLabel,
                    Color = othersColor,
                    Value = GetRoundedValue( remainder.Sum( r => r.Value ) )
                } );

                // Finish by rounding and assigning a color to the individual remainders.
                remainder.ForEach( dataPoint =>
                {
                    dataPoint.Color = othersColor;
                    dataPoint.Value = GetRoundedValue( dataPoint.Value );
                } );
            }

            return clients;
        }

        /// <summary>
        /// Gets analytics data for all links contained within this communication.
        /// </summary>
        /// <param name="deliveredRecipientCount">The count of recipients to whom this communication was successfully delivered.</param>
        /// <param name="groupedInteractions">The grouped interaction data from which to derive links analytics.</param>
        /// <returns>Analytics data for all links contained within this communication.</returns>
        private List<CommunicationLinkAnalyticsBag> GetAllLinksAnalytics( int deliveredRecipientCount, GroupedInteractions groupedInteractions )
        {
            // The top performing link is the one that has the highest count of total clicks (EXCLUDING unsubscribe clicks).
            // Start by grouping the interactions by URL and assigning the click counts & click-through rates.
            var uniqueClicksCountByUrl = groupedInteractions.UniqueClicks
                .Where( c =>
                    c.InteractionData.IsNotNullOrWhiteSpace()
                    && !c.InteractionData.Contains( "/Unsubscribe/" )
                )
                .GroupBy( c => c.InteractionData )
                .ToDictionary( g => g.Key, g => g.Count() );

            var topLinkTotalClicksCount = 0;

            var allLinksAnalytics = groupedInteractions.AllClicks
                .Where( c => uniqueClicksCountByUrl.ContainsKey( c.InteractionData ) )
                .GroupBy( c => c.InteractionData )
                .Select( g =>
                {
                    var uniqueClicksCount = uniqueClicksCountByUrl[g.Key];
                    var clickThroughRate = deliveredRecipientCount > 0
                        ? Math.Round( uniqueClicksCount / ( decimal ) deliveredRecipientCount * 100, 1, MidpointRounding.AwayFromZero )
                        : 0;

                    var analytics = new CommunicationLinkAnalyticsBag
                    {
                        Url = g.Key,
                        TotalClicksCount = g.Count(),
                        UniqueClicksCount = uniqueClicksCount,
                        ClickThroughRate = clickThroughRate
                    };

                    return analytics;
                } )
                .OrderByDescending( a => a.TotalClicksCount )
                .Select( ( a, index ) =>
                {
                    if ( index == 0 )
                    {
                        topLinkTotalClicksCount = a.TotalClicksCount;
                        a.PercentOfTopLink = 100;
                    }
                    else
                    {
                        // Set the percentage of this link's total clicks count relative to that of the top performing link.
                        a.PercentOfTopLink = topLinkTotalClicksCount > 0
                            ? Math.Round( a.TotalClicksCount / ( decimal ) topLinkTotalClicksCount * 100, 1, MidpointRounding.AwayFromZero )
                            : 0;
                    }

                    return a;
                } )
                .ToList();

            return allLinksAnalytics;
        }

        /// <summary>
        /// Gets the grid builder for the recipient log grid.
        /// </summary>
        /// <returns>The grid builder for the recipient log grid.</returns>
        private GridBuilder<CommunicationRecipientRow> GetRecipientGridBuilder()
        {
            var builder = new GridBuilder<CommunicationRecipientRow>()
                .WithBlock( this )
                .AddTextField( "idKey", r => r.Person.IdKey )
                .AddPersonField( "person", r => r.Person )
                .AddDateTimeField( "lastActivityDateTime", r => r.LastActivityDateTime )
                .AddField( "opensCount", r => r.OpensCount )
                .AddField( "clicksCount", r => r.ClicksCount )
                .AddField( "communicationType", r => r.CommunicationType )
                .AddField( "wasDelivered", r => r.WasDelivered )
                .AddField( "wasFailedDelivery", r => r.WasFailedDelivery )
                .AddField( "openedDateTime", r => r.OpenedDateTime )
                .AddField( "clickedDateTime", r => r.ClickedDateTime )
                .AddField( "unsubscribedDateTime", r => r.UnsubscribedDateTime )
                .AddField( "markedAsSpamDateTime", r => r.MarkedAsSpamDateTime );

            if ( RecipientGridAttributeColumns.Any() )
            {
                builder.AddAttributeFieldsFrom( r => r.Person, RecipientGridAttributeColumns );
            }

            return builder;
        }

        #endregion Private Methods

        #region Supporting Classes

        /// <summary>
        /// A POCO to represent a <see cref="Rock.Model.Communication"/> along with the minimum
        /// <see cref="CommunicationRecipient"/> data needed for common block data and delivery breakdown counts.
        /// </summary>
        private class CommunicationWithDeliveryBreakdown
        {
            /// <summary>
            /// Gets or sets the <see cref="Rock.Model.Communication"/>.
            /// </summary>
            public Rock.Model.Communication Communication { get; set; }

            /// <summary>
            /// Gets or sets the total count of all recipients tied to this communication.
            /// </summary>
            public int TotalRecipientCount { get; set; }

            /// <summary>
            /// Gets or sets the the breakdown of recipients counts for this communication.
            /// </summary>
            public RecipientCountBreakdown RecipientCountBreakdown { get; set; }

            /// <summary>
            /// Gets or sets the list of <see cref="RecipientUnsubscribeEvent"/>s tied to this communication.
            /// </summary>
            public List<RecipientUnsubscribeEvent> UnsubscribeEvents { get; set; }

            /// <summary>
            /// Gets or sets the identifier of the medium <see cref="EntityType"/> filter that should be applied to
            /// analytics data (e.g. interactions).
            /// </summary>
            public int? MediumEntityTypeFilterId { get; set; }
        }

        /// <summary>
        /// A POCO to represent the breakdown of <see cref="CommunicationRecipient"/> counts.
        /// </summary>
        private class RecipientCountBreakdown
        {
            /// <summary>
            /// Gets or sets the <see cref="Rock.Enums.Communication.CommunicationType"/> represented within these count values.
            /// </summary>
            public CommunicationType CommunicationType { get; set; }

            /// <summary>
            /// Gets or sets the total count of recipients tied to this communication and type.
            /// </summary>
            public int RecipientCount { get; set; }

            /// <summary>
            /// Gets or sets the count of recipients for whom communications of this type are still pending (not yet sent).
            /// </summary>
            public int PendingCount { get; set; }

            /// <summary>
            /// Gets or sets the count of recipients for whom communications of this type were delivered.
            /// </summary>
            public int DeliveredCount { get; set; }

            /// <summary>
            /// Gets or sets the count of recipients for whom communications of this type failed to be delivered.
            /// </summary>
            public int FailedCount { get; set; }

            /// <summary>
            /// Gets or sets the count of recipients for whom communications of this type were cancelled.
            /// </summary>
            public int CancelledCount { get; set; }
        }

        /// <summary>
        /// A POCO to represent a <see cref="CommunicationRecipient"/> who unsubscribed as a result of receiving this
        /// <see cref="Rock.Model.Communication"/>.
        /// </summary>
        private class RecipientUnsubscribeEvent
        {
            /// <summary>
            /// Gets or sets the identifier of the <see cref="Rock.Model.CommunicationRecipient"/> who was unsubscribed.
            /// </summary>
            public int CommunicationRecipientId { get; set; }

            /// <inheritdoc cref="CommunicationRecipient.MediumEntityTypeId"/>
            public int? MediumEntityTypeId { get; set; }

            /// <inheritdoc cref="CommunicationRecipient.UnsubscribeLevel"/>
            public DateTime? UnsubscribeDateTime { get; set; }

            /// <summary>
            /// TODO (Jason): inheritdoc CommunicationRecipient.IsSpamComplaint
            /// </summary>
            public bool IsSpamComplaint { get; set; }
        }

        /// <summary>
        /// A POCO to represent the minimum <see cref="Interaction"/> data needed for analytics visuals.
        /// </summary>
        private class InteractionInfo
        {
            /// <inheritdoc cref="Interaction.InteractionDateTime"/>
            public DateTime InteractionDateTime { get; set; }

            /// <inheritdoc cref="Interaction.Operation"/>
            public string Operation { get; set; }

            /// <inheritdoc cref="Interaction.InteractionData"/>
            public string InteractionData { get; set; }

            /// <summary>
            /// Gets or sets the identifier of the <see cref="CommunicationRecipient"/> to whom this interaction relates.
            /// </summary>
            public int? CommunicationRecipientId { get; set; }

            /// <inheritdoc cref="Person.Gender"/>
            public Gender PersonGender { get; set; }

            /// <inheritdoc cref="Person.Age"/>
            public int? PersonAge { get; set; }
        }

        /// <summary>
        /// A POCO to represent grouped <see cref="InteractionInfo"/>s, which will be used throughout this block.
        /// </summary>
        private class GroupedInteractions
        {
            /// <summary>
            /// Gets or sets the complete list of <see cref="InteractionInfo"/>s that represent "Opened" interactions.
            /// </summary>
            public List<InteractionInfo> AllOpens { get; private set; }

            /// <summary>
            /// Gets or sets the complete list of <see cref="InteractionInfo"/>s that represent "Opened" interactions,
            /// grouped by <see cref="CommunicationRecipient"/> identifier.
            /// </summary>
            public Dictionary<int, List<InteractionInfo>> AllOpensByRecipientId { get; private set; }

            /// <summary>
            /// Gets or sets the complete list of <see cref="InteractionInfo"/>s that represent "Click" interactions.
            /// </summary>
            public List<InteractionInfo> AllClicks { get; private set; }

            /// <summary>
            /// Gets or sets the complete list of <see cref="InteractionInfo"/>s that represent "Click" interactions,
            /// grouped by <see cref="CommunicationRecipient"/> identifier.
            /// </summary>
            public Dictionary<int, List<InteractionInfo>> AllClicksByRecipientId { get; private set; }

            /// <summary>
            /// Gets or sets the list of unique, first "Opened" <see cref="InteractionInfo"/>s (at most one per recipient).
            /// </summary>
            public List<InteractionInfo> UniqueOpens { get; private set; }

            /// <summary>
            /// Gets or sets the list of unique, first "Click" <see cref="InteractionInfo"/>s (at most one per recipient).
            /// </summary>
            public List<InteractionInfo> UniqueClicks { get; private set; }

            /// <summary>
            /// Gets the total count of "Opened" interactions.
            /// </summary>
            public int TotalOpensCount => AllOpens.Count;

            /// <summary>
            /// Gets the total count of "Click" interactions.
            /// </summary>
            public int TotalClicksCount => AllClicks.Count;

            /// <summary>
            /// Gets the count of recipients who had at least one "Opened" interaction.
            /// </summary>
            public int UniqueOpensCount => UniqueOpens.Count;

            /// <summary>
            /// Gets the count of recipients who had at least one "Click" interaction.
            /// </summary>
            public int UniqueClicksCount => UniqueClicks.Count;

            /// <summary>
            /// Initializes a new instance of the <see cref="GroupedInteractions"/> class.
            /// </summary>
            /// <param name="interactions">The list of <see cref="InteractionInfo"/>s to group.</param>
            public GroupedInteractions( List<InteractionInfo> interactions )
            {
                interactions = ( interactions ?? new List<InteractionInfo>() )
                    .Where( i => i?.CommunicationRecipientId != null )
                    .ToList();

                var allOpens = interactions.Where( i => i.Operation == InteractionOperation.Opened );
                var allClicks = interactions.Where( i => i.Operation == InteractionOperation.Click );

                var uniqueOpensByRecipient = allOpens
                    .GroupBy( o => o.CommunicationRecipientId.Value )
                    .ToDictionary(
                        g => g.Key,                                             // [CommunicationRecipient].[Id]
                        g => g.OrderBy( i => i.InteractionDateTime ).First()    // The first "Opened" interaction for this recipient.
                    );

                var uniqueClicksByRecipient = allClicks
                    .GroupBy( o => o.CommunicationRecipientId.Value )
                    .ToDictionary(
                        g => g.Key,                                             // [CommunicationRecipient].[Id]
                        g => g.OrderBy( i => i.InteractionDateTime ).First()    // The first "Click" interaction for this recipient.
                    );

                var recipientIdsWithOpens = uniqueOpensByRecipient.Keys;
                var recipientIdsWithClicks = uniqueClicksByRecipient.Keys;

                // When grouping by opens, include unique click interactions whose recipient does NOT have a corresponding
                // open interaction. This is to capture the scenario where an email is viewed without loading the image
                // links that are required to trigger the open event.
                var recipientIdsHavingClicksWithoutOpens = recipientIdsWithClicks.Except( recipientIdsWithOpens );
                var inferredOpens = uniqueClicksByRecipient
                    .Where( kvp => recipientIdsHavingClicksWithoutOpens.Contains( kvp.Key ) )
                    .Select( kvp => kvp.Value )
                    .ToList();

                // Merge [and reorder] the inferred opens in with all actual opens.
                AllOpens = allOpens
                    .Union( inferredOpens )
                    .OrderBy( i => i.InteractionDateTime )
                    .ToList();

                AllOpensByRecipientId = AllOpens
                    .GroupBy( i => i.CommunicationRecipientId.Value )
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderBy( i => i.InteractionDateTime ).ToList()
                    );

                AllClicks = allClicks
                    .OrderBy( i => i.InteractionDateTime )
                    .ToList();

                AllClicksByRecipientId = AllClicks
                    .GroupBy( i => i.CommunicationRecipientId.Value )
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderBy( i => i.InteractionDateTime ).ToList()
                    );

                // Merge [and reorder] the inferred opens in with the unique actual opens.
                UniqueOpens = uniqueOpensByRecipient
                    .Select( kvp => kvp.Value )
                    .Union( inferredOpens )
                    .OrderBy( i => i.InteractionDateTime )
                    .ToList();

                UniqueClicks = uniqueClicksByRecipient
                    .Select( kvp => kvp.Value )
                    .OrderBy( i => i.InteractionDateTime )
                    .ToList();
            }
        }

        /// <summary>
        /// A POCO to represent a <see cref="Rock.Model.Communication"/> along with its list of
        /// <see cref="CommunicationRecipientRow"/>s.
        /// </summary>
        private class CommunicationWithRecipientRows
        {
            /// <summary>
            /// Gets or sets the <see cref="Rock.Model.Communication"/>.
            /// </summary>
            public Rock.Model.Communication Communication { get; set; }

            /// <summary>
            /// Gets or sets the list of <see cref="CommunicationRecipientRow"/>s tied to this communication.
            /// </summary>
            public List<CommunicationRecipientRow> RecipientRows { get; set; }
        }

        /// <summary>
        /// A POCO to represent a <see cref="CommunicationRecipient"/> SQL projection for the recipient log.
        /// </summary>
        private class CommunicationRecipientRow
        {
            /// <summary>
            /// Gets or sets the identifier of the <see cref="CommunicationRecipient"/> represented by this row.
            /// </summary>
            public int CommunicationRecipientId { get; set; }

            /// <summary>
            /// Gets or sets the <see cref="Rock.Model.Person"/> represented by this row.
            /// </summary>
            public Person Person { get; set; }

            /// <inheritdoc cref="CommunicationRecipient.MediumEntityTypeId"/>
            public int? MediumEntityTypeId { get; set; }

            /// <summary>
            /// The type of communication sent to this recipient.
            /// </summary>
            public CommunicationType? CommunicationType { get; set; }

            /// <summary>
            /// Gets or sets whether this communication was successfully delivered to this recipient.
            /// </summary>
            public bool WasDelivered { get; set; }

            /// <summary>
            /// Gets or sets whether delivery of this communication to this recipient failed.
            /// </summary>
            public bool WasFailedDelivery { get; set; }

            /// <summary>
            /// Gets or sets the date time of this recipient's last activity for this communication.
            /// </summary>
            public DateTime? LastActivityDateTime { get; set; }

            /// <summary>
            /// Gets or sets the count of times this recipient has opened this communication.
            /// </summary>
            public int? OpensCount { get; set; }

            /// <summary>
            /// Gets or sets the count of times this recipient has clicked any link within this communication.
            /// </summary>
            public int? ClicksCount { get; set; }

            /// <summary>
            /// Gets or sets the date time this communication was delivered to this recipient.
            /// </summary>
            public DateTime? DeliveredDateTime { get; set; }

            /// <summary>
            /// Gets or sets the last date time this recipient opened this communication.
            /// </summary>
            public DateTime? OpenedDateTime { get; set; }

            /// <summary>
            /// Gets or sets the last date time this recipient clicked this communication.
            /// </summary>
            public DateTime? ClickedDateTime { get; set; }

            ///// <summary>
            /// Gets or sets the date time this recipient unsubscribed from this communication.
            /// </summary>
            public DateTime? UnsubscribedDateTime { get; set; }

            /// <summary>
            /// Gets or sets the date time this recipient marked this communication as SPAM.
            /// </summary>
            public DateTime? MarkedAsSpamDateTime { get; set; }
        }

        #endregion Supporting Classes
    }
}
