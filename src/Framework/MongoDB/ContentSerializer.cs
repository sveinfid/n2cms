﻿using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Builders;
using N2.Collections;
using N2.Persistence.Proxying;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace N2.Persistence.MongoDB
{
	public class ContentSerializer : IBsonSerializer, IBsonIdProvider, IBsonDocumentSerializer
	{
		private BsonClassMap classMap;
		private BsonClassMapSerializer serializer;
		private MongoDatabaseProvider database;
		private IProxyFactory proxies;

		public ContentSerializer(Type type, MongoDatabaseProvider database, IProxyFactory proxies)
		{
			this.database = database;
			classMap = BsonClassMap.LookupClassMap(type);
			serializer = new BsonClassMapSerializer(classMap);
			this.proxies = proxies;
		}

		public object Deserialize(BsonReader bsonReader, Type nominalType, Type actualType, IBsonSerializationOptions options)
		{
			return Inject(serializer.Deserialize(bsonReader, nominalType, actualType, options));
		}

		private object Inject(object value)
		{
			var item = (ContentItem)value;
			var parentId = item.AncestralTrail.Split('/').Where(id => !string.IsNullOrEmpty(id)).LastOrDefault();
			item.ParentRelation = new ContentRelation
			{
				ID = string.IsNullOrEmpty(parentId) ? null : (int?)int.Parse(parentId),
				ValueAccessor = (relationId) =>
				{
					var ids = item.AncestralTrail.Split('/').Where(id => !string.IsNullOrEmpty(id)).Select(id => BsonInt32.Create(int.Parse(id)));
					var parents = database.GetCollection<ContentItem>().Find(Query.In("_id", ids)).OrderByDescending(p => p.AncestralTrail.Length).ToList();
					for (int i = 1; i < parents.Count; i++)
					{
						parents[i - 1].Parent = parents[i];
					}
					return parents[0];
				}
			};

			item.Children = new ItemList(() => database.GetCollection<ContentItem>().Find(Query.EQ("AncestralTrail", item.GetTrail())));
			proxies.OnLoaded(item);
			return item;
		}

		public object Deserialize(BsonReader bsonReader, Type nominalType, IBsonSerializationOptions options)
		{
			return Inject(serializer.Deserialize(bsonReader, nominalType, options));
		}

		public IBsonSerializationOptions GetDefaultSerializationOptions()
		{
			return serializer.GetDefaultSerializationOptions();
		}

		public void Serialize(BsonWriter bsonWriter, Type nominalType, object value, IBsonSerializationOptions options)
		{
			proxies.OnSaving(value);
			serializer.Serialize(bsonWriter, nominalType, value, options);
		}

		public bool GetDocumentId(object document, out object id, out Type idNominalType, out IIdGenerator idGenerator)
		{
			return serializer.GetDocumentId(document, out id, out idNominalType, out idGenerator);
		}

		public void SetDocumentId(object document, object id)
		{
			serializer.SetDocumentId(document, id);
		}

		public BsonSerializationInfo GetMemberSerializationInfo(string memberName)
		{
			return serializer.GetMemberSerializationInfo(memberName);
		}
	}
}
