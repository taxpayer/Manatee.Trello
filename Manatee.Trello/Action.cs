﻿/***************************************************************************************

	Copyright 2013 Little Crab Solutions

	   Licensed under the Apache License, Version 2.0 (the "License");
	   you may not use this file except in compliance with the License.
	   You may obtain a copy of the License at

		 http://www.apache.org/licenses/LICENSE-2.0

	   Unless required by applicable law or agreed to in writing, software
	   distributed under the License is distributed on an "AS IS" BASIS,
	   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	   See the License for the specific language governing permissions and
	   limitations under the License.
 
	File Name:		Action.cs
	Namespace:		Manatee.Trello
	Class Name:		Action
	Purpose:		Represents an action on Trello.com.

***************************************************************************************/
using System;
using System.Linq;
using Manatee.Trello.Contracts;
using Manatee.Trello.Internal;
using Manatee.Trello.Internal.DataAccess;
using Manatee.Trello.Internal.Json;
using Manatee.Trello.Json;

namespace Manatee.Trello
{
	/// <summary>
	/// Actions are generated by Trello to record what users do.
	/// </summary>
	public class Action : ExpiringObject, IEquatable<Action>, IComparable<Action>
	{
		private static readonly OneToOneMap<ActionType, string> _typeMap;

		private IJsonAction _jsonAction;
		private Member _memberCreator;
		private ActionType _type = ActionType.Unknown;
		/// <summary>
		/// 
		/// </summary>
		protected bool _isDeleted;

		/// <summary>
		/// The member who performed the action.
		/// </summary>
		public Member MemberCreator
		{
			get
			{
				if (_isDeleted) return null;
				if (_jsonAction == null) return null;
				return UpdateById(ref _memberCreator, EntityRequestType.Member_Read_Refresh, _jsonAction.IdMemberCreator);
			}
		}
		/// <summary>
		/// Data associated with the action.  Contents depend upon the action's type.
		/// </summary>
		internal IJsonActionData Data
		{
			get { return (_isDeleted || (_jsonAction == null)) ? null : _jsonAction.Data; }
			set
			{
				if (_jsonAction == null) return;
				_jsonAction.Data = value;
			}
		}
		/// <summary>
		/// Gets a unique identifier (not necessarily a GUID).
		/// </summary>
		public sealed override string Id
		{
			get { return _jsonAction != null ? _jsonAction.Id : base.Id; }
			internal set
			{
				if (_jsonAction != null)
					_jsonAction.Id = value;
				base.Id = value;
			}
		}
		/// <summary>
		/// The type of action performed.
		/// </summary>
		public ActionType Type
		{
			get { return _isDeleted ? ActionType.Unknown : _type; }
			internal set { _type = value; }
		}
		/// <summary>
		/// When the action was performed.
		/// </summary>
		public DateTime? Date
		{
			get { return (_jsonAction == null) ? null : _jsonAction.Date; }
		}

		static Action()
		{
			_typeMap = new OneToOneMap<ActionType, string>
			           	{
			           		{ActionType.AddAttachmentToCard, "addAttachmentToCard"},
			           		{ActionType.AddChecklistToCard, "addChecklistToCard"},
			           		{ActionType.AddMemberToBoard, "addMemberToBoard"},
			           		{ActionType.AddMemberToCard, "addMemberToCard"},
			           		{ActionType.AddMemberToOrganization, "addMemberToOrganization"},
			           		{ActionType.AddToOrganizationBoard, "addToOrganizationBoard"},
			           		{ActionType.CommentCard, "commentCard"},
			           		{ActionType.CopyCommentCard, "copyCommentCard"},
			           		{ActionType.ConvertToCardFromCheckItem, "convertToCardFromCheckItem"},
			           		{ActionType.CopyBoard, "copyBoard"},
			           		{ActionType.CreateBoard, "createBoard"},
			           		{ActionType.CreateCard, "createCard"},
			           		{ActionType.CopyCard, "copyCard"},
			           		{ActionType.CreateList, "createList"},
			           		{ActionType.CreateOrganization, "createOrganization"},
			           		{ActionType.DeleteAttachmentFromCard, "deleteAttachmentFromCard"},
			           		{ActionType.DeleteBoardInvitation, "deleteBoardInvitation"},
							{ActionType.DeleteCard, "deleteCard"},
			           		{ActionType.DeleteOrganizationInvitation, "deleteOrganizationInvitation"},
			           		{ActionType.MakeAdminOfBoard, "makeAdminOfBoard"},
			           		{ActionType.MakeNormalMemberOfBoard, "makeNormalMemberOfBoard"},
			           		{ActionType.MakeNormalMemberOfOrganization, "makeNormalMemberOfOrganization"},
			           		{ActionType.MakeObserverOfBoard, "makeObserverOfBoard"},
			           		{ActionType.MemberJoinedTrello, "memberJoinedTrello"},
			           		{ActionType.MoveCardFromBoard, "moveCardFromBoard"},
			           		{ActionType.MoveListFromBoard, "moveListFromBoard"},
			           		{ActionType.MoveCardToBoard, "moveCardToBoard"},
			           		{ActionType.MoveListToBoard, "moveListToBoard"},
			           		{ActionType.RemoveAdminFromBoard, "removeAdminFromBoard"},
			           		{ActionType.RemoveAdminFromOrganization, "removeAdminFromOrganization"},
			           		{ActionType.RemoveChecklistFromCard, "removeChecklistFromCard"},
			           		{ActionType.RemoveFromOrganizationBoard, "removeFromOrganizationBoard"},
			           		{ActionType.RemoveMemberFromCard, "removeMemberFromCard"},
			           		{ActionType.UnconfirmedBoardInvitation, "unconfirmedBoardInvitation"},
			           		{ActionType.UnconfirmedOrganizationInvitation, "unconfirmedOrganizationInvitation"},
			           		{ActionType.UpdateBoard, "updateBoard"},
			           		{ActionType.UpdateCard, "updateCard"},
			           		{ActionType.UpdateCheckItemStateOnCard, "updateCheckItemStateOnCard"},
			           		{ActionType.UpdateChecklist, "updateChecklist"},
			           		{ActionType.UpdateMember, "updateMember"},
			           		{ActionType.UpdateOrganization, "updateOrganization"},
			           		{ActionType.UpdateCardIdList, "updateCard:idList"},
			           		{ActionType.UpdateCardClosed, "updateCard:closed"},
			           		{ActionType.UpdateCardDesc, "updateCard:desc"},
			           		{ActionType.UpdateCardName, "updateCard:name"},
			           	};
		}
		/// <summary>
		/// Creates a new instance of the Action class.
		/// </summary>
		public Action()
		{
			_jsonAction = new InnerJsonAction();
		}

		/// <summary>
		/// Deletes this action.  This cannot be undone.
		/// </summary>
		public void Delete()
		{
			if (_isDeleted) return;
			Validator.Writable();
			Parameters.Add("_id", Id);
			EntityRepository.Upload(EntityRequestType.Action_Write_Delete, Parameters);
			_isDeleted = true;
		}
		/// <summary>
		/// Indicates whether the current object is equal to another object of the same type.
		/// </summary>
		/// <returns>
		/// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
		/// </returns>
		/// <param name="other">An object to compare with this object.</param>
		public bool Equals(Action other)
		{
			return Id == other.Id;
		}
		/// <summary>
		/// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
		/// </summary>
		/// <returns>
		/// true if the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>; otherwise, false.
		/// </returns>
		/// <param name="obj">The object to compare with the current object. </param><filterpriority>2</filterpriority>
		public override bool Equals(object obj)
		{
			if (!(obj is Action)) return false;
			return Equals((Action) obj);
		}
		/// <summary>
		/// Serves as a hash function for a particular type. 
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:System.Object"/>.
		/// </returns>
		/// <filterpriority>2</filterpriority>
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
		/// <summary>
		/// Compares the current object with another object of the same type.
		/// </summary>
		/// <returns>
		/// A value that indicates the relative order of the objects being compared. The return value has the following meanings: Value Meaning Less than zero This object is less than the <paramref name="other"/> parameter.Zero This object is equal to <paramref name="other"/>. Greater than zero This object is greater than <paramref name="other"/>. 
		/// </returns>
		/// <param name="other">An object to compare with this object.</param>
		public int CompareTo(Action other)
		{
			var diff = Date - other.Date;
			return diff.HasValue ? (int) diff.Value.TotalMilliseconds : 0;
		}
		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>
		/// A string that represents the current object.
		/// </returns>
		/// <filterpriority>2</filterpriority>
		public override string ToString()
		{
			return string.Format("{0} on {1}", Type, Date);
		}
		/// <summary>
		/// Retrieves updated data from the service instance and refreshes the object.
		/// </summary>
		public sealed override bool Refresh()
		{
			if (_isDeleted) return false;
			Parameters.Add("_id", Id);
			AddDefaultParameters();
			EntityRepository.Refresh(this, EntityRequestType.Action_Read_Refresh);
			return true;
		}

		internal override void ApplyJson(object obj)
		{
			_jsonAction = (IJsonAction) obj;
			UpdateType();
		}

		private void UpdateType()
		{
			_type = _typeMap.Any(kvp => kvp.Value == _jsonAction.Type) ? _typeMap[_jsonAction.Type] : ActionType.Unknown;
		}
	}
}
