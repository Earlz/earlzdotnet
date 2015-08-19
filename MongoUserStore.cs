using System;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using Earlz.BarelyMVC.Authentication;
using MongoDB.Bson.Serialization.Attributes;

namespace Earlz.LastYearsWishes
{
	public class MongoUserData : UserData{
		[BsonId]
		public ObjectId ID{get;set;}
	}
		
	public class MongoUserStore : IUserStore
	{
		public MongoUserStore ()
		{
		}
		public UserData GetUserByName (string username)
		{
			var db=Config.GetDB();
			var u=db.GetCollection<MongoUserData>("users").FindOneAs<MongoUserData>(Query.EQ("Username",username));
			return u;
		}

		public bool UpdateUserByID(UserData user)
		{
			var userdata=(MongoUserData)user;
			var db=Config.GetDB();
			var u=db.GetCollection<MongoUserData>("users");
			if(u.Save<MongoUserData>(userdata)==null){
				return false;
			}else{
				return true;
			}
		}

		public bool AddUser (UserData user)
		{
			var userdata=(MongoUserData)user;
			var db=Config.GetDB();
			var u=db.GetCollection<MongoUserData>("users");
			if(u.Find(Query.EQ("Username",userdata.Username)).Count()!=0){
				return false;
			}
			u.Insert<MongoUserData>(userdata);
			userdata.UniqueID=userdata.ID.ToString();
			u.Save<MongoUserData>(userdata);
			return userdata!=null;
		}
		public bool DeleteUserByID(UserData user){
			var userdata=(MongoUserData)user;
			var db=Config.GetDB();
			var u=db.GetCollection<MongoUserData>("users");
			u.Remove(Query.EQ("_id",userdata.ID));
			return true;
		}
	}
}

