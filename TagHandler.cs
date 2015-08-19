using System;
using MongoDB.Driver.Builders;
using System.Web;
using System.Collections.Generic;
using Earlz.BarelyMVC;
using Earlz.BarelyMVC.ViewEngine;
using Earlz.LastYearsWishes.Models;
using MongoDB.Driver;
using MongoDB.Bson;

namespace Earlz.LastYearsWishes
{
	public class TagHandler : HttpHandler
	{
		public TagHandler ()
		{
		}
		public IBarelyView Index()
		{
			var v=new TagIndexView();
			v.Layout.Active="tags";
			v.Layout.Title="Tag List - Earlz.Net";
			var c=BlogEntryData.All();
			var l=new List<string>(100);
			foreach(var i in c.FindAll()){
				if(i.Tags==null) continue;
				foreach(var j in i.Tags){
					if(!l.Contains(j)){
						l.Add(j);
					}
				}
			}
			l.Sort();
			v.Tags=l;
			return v;
		}
		public IBarelyView RedirectZero(string tag)
		{
			Response.Redirect("/tags/"+HttpUtility.UrlEncode(tag)+"/1");
			return null;
		}
		public IBarelyView Tag(string tag, int page)
		{
			var v=new BlogIndexView();
			v.ShowPaging=true;
			v.Tag=tag;
			v.Layout.Active="tags";

			MongoCursor<BlogEntryData> c;
			if(tag=="private-draft")
			{
				c=BlogEntryData.All().Find(Query.In("Tags",new BsonArray(new string[]{tag}))).SetSortOrder(SortBy.Descending(new string[]{"Posted"}));
			}
			else
			{
				//otherwise, filter out the private-draft results
				c=BlogEntryData.All().Find(Query.And(Query.NotIn("Tags", new BsonArray(new string[]{"private-draft"})),
				                                     Query.In("Tags",new BsonArray(new string[]{tag})))).SetSortOrder(SortBy.Descending(new String[]{"Posted"}));
			}
			page-=1; //so that routes start with /blog/1 than /blog/0
			c=c.SetSkip(page*Config.PageSize).SetLimit(Config.PageSize);
			int pagecount=((int)c.Count())/Config.PageSize+1; //this takes a total count on the collection, not the query.

			var entries=new List<BlogEntryData>();
			foreach(var e in c){
				e.Text=Config.GetMarkdown().Transform(e.Text);
				entries.Add(e);
			}
			if(entries.Count==0){
				throw new HttpException(404,"not found");
			}
			page+=1;
			v.Page=page;
			v.PageMax=pagecount;
			v.Entries=entries;
			v.Layout.Active="tags";
			v.Layout.Title="Posts Tagged '"+tag+"' - Page "+(page)+" of "+(v.PageMax)+" - Earlz.Net";
			return v;
		}
	}
}

