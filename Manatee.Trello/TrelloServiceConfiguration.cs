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
 
	File Name:		Options.cs
	Namespace:		Manatee.Trello
	Class Name:		Options
	Purpose:		Exposes a set of run-time options for Manatee.Trello.

***************************************************************************************/
using System;
using Manatee.Trello.Contracts;
using Manatee.Trello.Internal;
using Manatee.Trello.Json;
using Manatee.Trello.Json.Manatee;
using Manatee.Trello.Json.Newtonsoft;
using Manatee.Trello.Rest;

namespace Manatee.Trello
{
	/// <summary>
	/// Exposes a set of run-time options for all automatically-refreshing objects.
	/// </summary>
	public class TrelloServiceConfiguration : ITrelloServiceConfiguration
	{
		private static ManateeSerializer _manateeSerializer;
		private static NewtonsoftSerializer _newtonsoftSerializer;
		private ILog _log;
		private ISerializer _serializer;
		private IDeserializer _deserializer;
		private IRestClientProvider _restClientProvider;
		private IRequestQueue _requestQueue;

		private static ISerializer ManateeSerializer { get { return _manateeSerializer ?? (_manateeSerializer = new ManateeSerializer()); } }
		private static IDeserializer ManateeDeserializer { get { return _manateeSerializer ?? (_manateeSerializer = new ManateeSerializer()); } }
		private static ISerializer NewtonsoftSerializer { get { return _newtonsoftSerializer ?? (_newtonsoftSerializer = new NewtonsoftSerializer()); } }
		private static IDeserializer NewtonsoftDeserializer { get { return _newtonsoftSerializer ?? (_newtonsoftSerializer = new NewtonsoftSerializer()); } }

		/// <summary>
		/// Provides a default configuration.  Modifying the default configuration will
		/// change the operation of any ITrelloService instance which relies upon it.
		/// </summary>
		public static ITrelloServiceConfiguration Default { get; private set; }
		/// <summary>
		/// Provides a default logging solution.  New ITrelloService instances will use
		/// this unless overridden in an ITrelloServiceConfiguration instance.
		/// </summary>
		public static ILog GlobalLog { get; set; }
		/// <summary>
		/// Provides a default caching solution.  New ITrelloService instances will use
		/// this unless overridden in an ITrelloServiceConfiguration instance.
		/// </summary>
		public static ICache GlobalCache { get; set; }
		/// <summary>
		/// Gets and sets the global duration setting for all auto-refreshing objects.
		/// </summary>
		public TimeSpan ItemDuration { get; set; }
		/// <summary>
		/// Enables/disables auto-refreshing for all auto-refreshing objects.
		/// </summary>
		public bool AutoRefresh { get; set; }
		/// <summary>
		/// Specifies the serializer which is used the first time a request is made from
		/// a given instance of the TrelloService class.
		/// </summary>
		public ISerializer Serializer
		{
			get
			{
				if (_serializer == null)
					CreateDefaultSerializer();
				return _serializer;
			}
			set
			{
				if (value == null)
					Log.Error(new ArgumentNullException("value"));
				_serializer = value;
			}
		}
		/// <summary>
		/// Specifies the deserializer which is used the first time a request is made from
		/// a given instance of the TrelloService class.
		/// </summary>
		public IDeserializer Deserializer
		{
			get
			{
				if (_deserializer == null)
					CreateDefaultSerializer();
				return _deserializer;
			}
			set
			{
				if (value == null)
					Log.Error(new ArgumentNullException("value"));
				_deserializer = value;
			}
		}
		/// <summary>
		/// Specifies the REST client provider which is used the first time a request is made from
		/// a given instance of the TrelloService class.
		/// </summary>
		public IRestClientProvider RestClientProvider
		{
			get { return _restClientProvider ?? (_restClientProvider = new RestSharpClientProvider(this)); }
			set
			{
				if (value == null)
					Log.Error(new ArgumentNullException("value"));
				_restClientProvider = value;
			}
		}
		/// <summary>
		/// Provides a cache for TrelloService.  Defaults to TrelloServiceConfiguration.GlobalCache.
		/// </summary>
		public ICache Cache { get; set; }
		/// <summary>
		/// Provides logging for all of Manatee.Trello.  The default log only writes to the Debug window.
		/// </summary>
		public ILog Log { get { return _log ?? (_log = new DebugLog()); } set { _log = value; } }
		/// <summary>
		/// Gets and sets the request queue used by the service.  Can be used to persist requests at shutdown.  Defaults to an internal, in-memory implementation.
		/// </summary>
		public IRequestQueue RequestQueue { get { return _requestQueue ?? (_requestQueue = new RequestQueue()); } set { _requestQueue = value; } }

		static TrelloServiceConfiguration()
		{
			GlobalCache = new ThreadSafeCache(new SimpleCache());
			GlobalLog = new DebugLog();
			Default = new TrelloServiceConfiguration();
		}
		/// <summary>
		/// Creates a new instance of the TrelloServiceConfiguration class.
		/// </summary>
		public TrelloServiceConfiguration()
		{
			ItemDuration = TimeSpan.FromSeconds(60);
			AutoRefresh = true;
			Cache = GlobalCache;
		}

		/// <summary>
		/// Sets Manatee.Json as the serializer/deserializer (default).
		/// </summary>
		public void UseManateeJson()
		{
			_serializer = ManateeSerializer;
			_deserializer = ManateeDeserializer;
		}
		/// <summary>
		/// Sets Newtonsoft's Json.Net as the serializer/deserializer.
		/// </summary>
		public void UseNewtonsoftJson()
		{
			_serializer = NewtonsoftSerializer;
			_deserializer = NewtonsoftDeserializer;
		}

		private void CreateDefaultSerializer()
		{
			UseManateeJson();
		}
		private void CreateDefaultRestClientProvider()
		{
			_restClientProvider = new RestSharpClientProvider(this);
		}
	}
}
