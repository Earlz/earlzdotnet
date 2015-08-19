using System;
using MongoDB.Bson;
using System.Linq;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Earlz.BarelyMVC;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace Earlz.LastYearsWishes.Models
{
	public class CommentData 
	{
		[BsonId]
		[ParameterIgnore]
		public ObjectId ID{get;set;}
		[ParameterIgnore]
		public ObjectId EntryID{get;set;}
		public string Name{get;set;}
		public string Text{get;set;}
		public string IPAddress{get;set;}
		public string UserAgent{get;set;}
		[ParameterIgnore]
		public DateTime Posted{get;set;}

		public SafeModeResult Save()
		{
			return Collection().Save(this,SafeMode.FSyncTrue);
		}
		public static CommentData Get(ObjectId id)
		{
			return Collection().FindOneByIdAs<CommentData>(id);
		}
		public BlogEntryData GetOwner()
		{
			//todo: can cache!
			return BlogEntryData.Get(EntryID);
		}
		public static MongoCursor<CommentData> ForEntry(BlogEntryData entry)
		{
			var comments=Collection().FindAs<CommentData>(Query.EQ("EntryID",entry.ID))
			.SetSortOrder(SortBy.Ascending("Posted"));
			return comments;
		}
		public static int CountForEntry(BlogEntryData entry)
		{
			return (int)ForEntry(entry).Count();
		}
		public static MongoCollection<CommentData> Collection()
		{
			return Config.GetDB().GetCollection<CommentData>("comments");
		}
		public static SafeModeResult Delete(string id)
		{
			return Collection().Remove(Query.EQ("_id",new ObjectId(id)));
		}
	}
}

