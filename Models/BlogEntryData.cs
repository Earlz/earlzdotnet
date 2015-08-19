using System;
using Earlz.BarelyMVC;
using MongoDB.Bson;
using System.Collections.Generic;
using MongoDB.Driver;
using Earlz.BarelyMVC.Converters;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization.Attributes;

namespace Earlz.LastYearsWishes.Models
{
	/// <summary>
	/// Blog entry model used as a model for MongoDB
	/// </summary>
	public class BlogEntryData{
		public BlogEntryData(){}
		[BsonId]
		//[ParameterIgnore]
		public ObjectId ID{get;set;}
		public string Title{get;set;}
		public string Text{get;set;}
		/// <summary>
		/// The creation date of the post
		/// </summary>
		[ParameterIgnore]
		public DateTime Posted{get;set;}
		[ParameterIgnore]
		public DateTime Edited{get;set;}
		[ParameterConverter(typeof(StringsToListConverter))]
		public List<string> Tags{get;set;}
		[BsonDefaultValue(true)]
		[ParameterConverter(typeof(CheckboxToBoolConverter))]
		public bool Publish{get;set;}
		[ParameterIgnore]
		[BsonDefaultValue(0)]
		public int CommentCount{get;set;}
		public SafeModeResult Save()
		{
			return All().Save(this, SafeMode.FSyncTrue);
		}
		public static BlogEntryData Get(DateTime begin)
		{
			var c=Config.GetDB().GetCollection<BlogEntryData>("entries");
			DateTime end=begin.AddMinutes(1);
			var list=c.FindAs<BlogEntryData>(Query.And(Query.GTE ("Posted", begin), Query.LT("Posted", end)));
			if(list.Count()>1)
			{
				//wtf?
			}
			if(list.Count()==0)
			{
				return null;
			}
			var en=list.GetEnumerator(); //god I hate enumerators
			en.MoveNext();
			return (BlogEntryData)en.Current;
		}
		public static BlogEntryData Get(string ID)
		{
			return Get(new ObjectId(ID));
		}
		public static BlogEntryData Get(ObjectId ID)
		{
			return All().FindOneById(ID);
		}
		public static MongoCollection<BlogEntryData> All()
		{
			return Config.GetDB().GetCollection<BlogEntryData>("entries");
		}
		public MongoCursor<CommentData> GetComments()
		{
			return CommentData.ForEntry(this);
		}
		public static void RecalculateComments(ObjectId id)
		{
			var entry=Get(id);
			entry.CommentCount=(int)entry.GetComments().Count();
			entry.Save();
		}
	}
}

