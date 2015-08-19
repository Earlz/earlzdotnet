using System;
using MongoDB.Driver.Builders;
using Earlz.BarelyMVC;
using Earlz.BarelyMVC.ViewEngine;
using Earlz.LastYearsWishes.Models;
using MongoDB.Bson;


namespace Earlz.LastYearsWishes
{
	public class ProjectHandler : HttpHandler
	{
		public ProjectHandler ()
		{
		}
		public IBarelyView Get ()
		{
			var v=new BlogEntryView();
			v.Layout.Title="Programming Projects - Earlz.Net";
			v.Layout.Active="projects";
			v.Entry=BlogEntryData.All().FindOneAs<BlogEntryData>(Query.In("Tags",new BsonArray(new string[]{"page-projects"})));

			string text=BetterCache.Get<string>(v.Entry.ID.ToString());
			if(text==null)
			{
				v.Entry.Text=Config.GetMarkdown().Transform(v.Entry.Text);
				BetterCache.Add(v.Entry.ID.ToString(), v.Entry.Text);
			}
			else
			{
				v.Entry.Text=text;
			}
			v.ShowComments=true;
			v.Summary=false;
			return v;
		}
	}
}

