using System;
using MongoDB.Bson;
using Earlz.BarelyMVC;
using Earlz.BarelyMVC.ViewEngine;
using Earlz.BarelyMVC.Authentication;
using Earlz.LastYearsWishes.Models;
using MongoDB.Bson.Serialization.Attributes;


namespace Earlz.LastYearsWishes
{
	public class ConfigData{
		[BsonId]
		public ObjectId ID;
		public bool AllowComments;
	}
	public class LoginModel
	{
		public string Username{get;set;}
		public string Password{get;set;}
	}
	public class ConfigHandler : HttpHandler
	{
		public IBarelyView Login(LoginModel model)
		{
			if(Method != HttpMethod.Post)
			{
				return new LoginView();
			}
			else
			{
				if(FSCAuth.Login(model.Username, model.Password, false))
				{
					Response.Redirect("/new");
				}
				else
				{
					return new WrapperView("Fail");
				}
				return null;
			}
		}
		public IBarelyView Logout()
		{
			FSCAuth.Logout();
			return new WrapperView("you've been logged out");
		}
		public IBarelyView RecalculateCommentCounts()
		{
			foreach(var entry in BlogEntryData.All().FindAllAs<BlogEntryData>())
			{
				BlogEntryData.RecalculateComments(entry.ID);
			}
			return new WrapperView("Complete!");
		}
		public IBarelyView WhoAmI()
		{
			if(FSCAuth.IsAuthenticated()){
				return new WrapperView("Is this a test? You're "+CurrentUser.Username);
			}else{
				return new WrapperView("You are not special. You are not a beautiful or unique snowflake");
			}
		}
		public IBarelyView Init()
		{
			/*Replace these with needed values, run /init, and then remove them!
			 *Make sure not to commit to source control ;)
			 */
			/*
			var u=FSCAuth.UserStore.GetUserByName("earlz");
			u.PasswordHash=FSCAuth.ComputePasswordHash(u,"foobar"); 
			u.Update();
			*/ 
			if(!Config.GetDB().CollectionExists("config")){
				var db=Config.GetDB();
				var c=db.GetCollection("config");
				var config=new ConfigData();
				config.AllowComments=true;
				c.Save<ConfigData>(config);
				return new WrapperView("Complete");
			}
			else
			{
				return new WrapperView("fuck you");
			}
		}
		public IBarelyView SetComments(bool enabled)
		{
			FSCAuth.RequiresLogin();
			var c=Config.GetDB().GetCollection("config");
			var config=c.FindOneAs<ConfigData>();
			config.AllowComments=enabled;
			c.Save<ConfigData>(config);
			if(enabled)
			{
				return new WrapperView("Comments enabled");
			}else
			{
				return new WrapperView("Comments disabled");
			}
		}
	}
}

