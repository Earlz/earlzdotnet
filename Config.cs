using System;
using MongoDB.Driver;
using MarkdownSharp;
namespace Earlz.LastYearsWishes
{
	public static class Config
	{
		public static string ConnectionString = "mongodb://localhost";
		public static string UniqueHash="meh"; //changes between dev and production
		public static string Database="lastyearswishes";
		public static string HostName=""; //changes between dev and production
		public static int PageSize=10;
		public static MongoDB.Driver.MongoDatabase GetDB(){
			MongoServer server = MongoServer.Create(Config.ConnectionString);
		    MongoDatabase db = server.GetDatabase(Config.Database);
			return db;
		}
		public static bool CommentsEnabled{
			get{
				var c=Config.GetDB().GetCollection("config");
				var config=c.FindOneAs<ConfigData>();
				return config.AllowComments;
			}
		}
		public static Markdown GetMarkdown(){
			var options=new MarkdownOptions(){
				AutoHyperlink=true,
				AutoNewlines=false,
				EmptyElementSuffix=" />",
				LinkEmails=false,
				StrictBoldItalic=true
			};
			var m=new Markdown(options);
			return m;
			
		}
	}
}

