﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Manatee.Trello.Contracts;
using Manatee.Trello.Internal;
using Manatee.Trello.Internal.DataAccess;
using Manatee.Trello.Internal.Synchronization;
using Manatee.Trello.Internal.Validation;
using Manatee.Trello.Json;
using IQueryable = Manatee.Trello.Contracts.IQueryable;

namespace Manatee.Trello
{
	/// <summary>
	/// Represents an organization.
	/// </summary>
	public class Organization : ICanWebhook, IQueryable
	{
		/// <summary>
		/// Enumerates the data which can be pulled for organizations (teams).
		/// </summary>
		[Flags]
		public enum Fields
		{
			/// <summary>
			/// Indicates the Description property should be populated.
			/// </summary>
			[Display(Description="desc")]
			Description = 1,
			/// <summary>
			/// Indicates the DisplayName property should be populated.
			/// </summary>
			[Display(Description="displayName")]
			DisplayName = 1 << 1,
			/// <summary>
			/// Indicates the LogoHash property should be populated.
			/// </summary>
			[Display(Description="logoHash")]
			LogoHash = 1 << 2,
			/// <summary>
			/// Indicates the Name property should be populated.
			/// </summary>
			[Display(Description="name")]
			Name = 1 << 3,
			/// <summary>
			/// Indicates the PowerUps property should be populated.
			/// </summary>
			[Display(Description="powerUps")]
			PowerUps = 1 << 5,
			/// <summary>
			/// Indicates the Preferences property should be populated.
			/// </summary>
			[Display(Description="prefs")]
			Preferences = 1 << 6,
			/// <summary>
			/// Indicates the Url property should be populated.
			/// </summary>
			[Display(Description="url")]
			Url = 1 << 7,
			/// <summary>
			/// Indicates the Website property should be populated.
			/// </summary>
			[Display(Description="website")]
			Website = 1 << 8
		}

		private readonly Field<string> _description;
		private readonly Field<string> _displayName;
		private readonly Field<bool> _isBusinessClass;
		private readonly Field<string> _name;
		private readonly Field<string> _url;
		private readonly Field<string> _website;
		private readonly OrganizationContext _context;

		private string _id;
		private DateTime? _creation;

		/// <summary>
		/// Specifies which fields should be downloaded.
		/// </summary>
		public static Fields DownloadedFields { get; set; } = (Fields)Enum.GetValues(typeof(Fields)).Cast<int>().Sum();

		/// <summary>
		/// Gets the collection of actions performed on the organization.
		/// </summary>
		public ReadOnlyActionCollection Actions { get; }
		/// <summary>
		/// Gets the collection of boards owned by the organization.
		/// </summary>
		public BoardCollection Boards { get; }
		/// <summary>
		/// Gets the creation date of the organization.
		/// </summary>
		public DateTime CreationDate
		{
			get
			{
				if (_creation == null)
					_creation = Id.ExtractCreationDate();
				return _creation.Value;
			}
		}
		/// <summary>
		/// Gets or sets the organization's description.
		/// </summary>
		public string Description
		{
			get { return _description.Value; }
			set { _description.Value = value; }
		}
		/// <summary>
		/// Gets or sets the organization's display name.
		/// </summary>
		public string DisplayName
		{
			get { return _displayName.Value; }
			set { _displayName.Value = value; }
		}
		/// <summary>
		/// Gets the organization's ID.
		/// </summary>
		public string Id
		{
			get
			{
				if (!_context.HasValidId)
					_context.Synchronize();
				return _id;
			}
			private set { _id = value; }
		}
		/// <summary>
		/// Gets whether the organization has business class status.
		/// </summary>
		public bool IsBusinessClass => _isBusinessClass.Value;
		/// <summary>
		/// Gets the collection of members who belong to the organization.
		/// </summary>
		public ReadOnlyMemberCollection Members { get; }
		/// <summary>
		/// Gets the collection of members and their priveledges on this organization.
		/// </summary>
		public OrganizationMembershipCollection Memberships { get; }
		/// <summary>
		/// Gets the organization's name.
		/// </summary>
		public string Name
		{
			get { return _name.Value; }
			set { _name.Value = value; }
		}
		/// <summary>
		/// Gets specific data regarding power-ups.
		/// </summary>
		public ReadOnlyPowerUpDataCollection PowerUpData { get; }
		/// <summary>
		/// Gets the set of preferences for the organization.
		/// </summary>
		public OrganizationPreferences Preferences { get; }
		/// <summary>
		/// Gets the organization's URL.
		/// </summary>
		public string Url => _url.Value;
		/// <summary>
		/// Gets or sets the organization's website.
		/// </summary>
		public string Website
		{
			get { return _website.Value; }
			set { _website.Value = value; }
		}

		internal IJsonOrganization Json
		{
			get { return _context.Data; }
			set { _context.Merge(value); }
		}

		/// <summary>
		/// Raised when data on the organization is updated.
		/// </summary>
		public event Action<Organization, IEnumerable<string>> Updated;

		/// <summary>
		/// Creates a new instance of the <see cref="Organization"/> object.
		/// </summary>
		/// <param name="id">The organization's ID.</param>
		/// <param name="auth">(Optional) Custom authorization parameters. When not provided, <see cref="TrelloAuthorization.Default"/> will be used.</param>
		/// <remarks>
		/// The supplied ID can be either the full ID or the organization's name.
		/// </remarks>
		public Organization(string id, TrelloAuthorization auth = null)
		{
			Id = id;
			_context = new OrganizationContext(id, auth);
			_context.Synchronized += Synchronized;

			Actions = new ReadOnlyActionCollection(typeof(Organization), () => Id, auth);
			Boards = new BoardCollection(typeof(Organization), () => Id, auth);
			_description = new Field<string>(_context, nameof(Description));
			_displayName = new Field<string>(_context, nameof(DisplayName));
			_isBusinessClass = new Field<bool>(_context, nameof(IsBusinessClass));
			Members = new ReadOnlyMemberCollection(EntityRequestType.Organization_Read_Members, () => Id, auth);
			Memberships = new OrganizationMembershipCollection(() => Id, auth);
			_name = new Field<string>(_context, nameof(Name));
			_name.AddRule(OrganizationNameRule.Instance);
			PowerUpData = new ReadOnlyPowerUpDataCollection(EntityRequestType.Organization_Read_PowerUpData, () => Id, auth);
			Preferences = new OrganizationPreferences(_context.OrganizationPreferencesContext);
			_url = new Field<string>(_context, nameof(Url));
			_website = new Field<string>(_context, nameof(Website));
			_website.AddRule(UriRule.Instance);

			TrelloConfiguration.Cache.Add(this);
		}
		internal Organization(IJsonOrganization json, TrelloAuthorization auth)
			: this(json.Id, auth)
		{
			_context.Merge(json);
		}

		/// <summary>
		/// Applies the changes an action represents.
		/// </summary>
		/// <param name="action">The action.</param>
		public void ApplyAction(Action action)
		{
			if (action.Type != ActionType.UpdateOrganization || action.Data.Organization == null || action.Data.Organization.Id != Id)
				return;
			_context.Merge(action.Data.Organization.Json);
		}
		/// <summary>
		/// Permanently deletes the organization from Trello.
		/// </summary>
		/// <remarks>
		/// This instance will remain in memory and all properties will remain accessible.
		/// </remarks>
		public void Delete()
		{
			_context.Delete();
			TrelloConfiguration.Cache.Remove(this);
		}
		/// <summary>
		/// Marks the organization to be refreshed the next time data is accessed.
		/// </summary>
		public void Refresh()
		{
			_context.Expire();
		}
		/// <summary>
		/// Returns the <see cref="DisplayName"/>.
		/// </summary>
		/// <returns>
		/// A string that represents the current object.
		/// </returns>
		public override string ToString()
		{
			return DisplayName;
		}

		private void Synchronized(IEnumerable<string> properties)
		{
			Id = _context.Data.Id;
			var handler = Updated;
			handler?.Invoke(this, properties);
		}
	}
}