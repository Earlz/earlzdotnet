using System;
using MongoDB.Driver.Builders;
using Earlz.BarelyMVC;
using Earlz.BarelyMVC.ViewEngine;
using Earlz.LastYearsWishes.Models;
using MongoDB.Bson;

namespace Earlz.LastYearsWishes
{
	public class AboutMeHandler : HttpHandler
	{
		public AboutMeHandler ()
		{
		}
		public IBarelyView Get ()
		{
			var v=new BlogEntryView();
			v.Layout.Title="About Me - Earlz.Net";
			v.Layout.Active="aboutme";
			v.Entry=BlogEntryData.All().FindOneAs<BlogEntryData>(Query.In("Tags",new BsonValue[]{"page-aboutme"}));

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

