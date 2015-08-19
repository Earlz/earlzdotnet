using System;
using MongoDB.Bson;
using System.Collections.Generic;
using System.Web;
using MongoDB.Driver.Builders;
using Earlz.BarelyMVC;
using Earlz.BarelyMVC.ViewEngine;
using Earlz.BarelyMVC.Authentication;
using Earlz.BarelyMVC.Converters;
using MongoDB.Driver;
using Earlz.LastYearsWishes.Models;

namespace Earlz.LastYearsWishes
{
	public class BlogRouteModel
	{
		[ParameterIgnore]
		public ObjectId ID
		{
			get
			{
				return new ObjectId(StringID);
			}
		}
		[ParameterMap("ID")]
		public string StringID{get;set;}
		public int Year{get;set;}
		public int Month{get;set;}
		public int Day{get;set;}
		public string Time{get;set;}
		[ParameterIgnore]
		public DateTime Date
		{
			get
			{
				int hour=Int32.Parse(Time.Substring(0,2));
				int minute=Int32.Parse(Time.Substring(2,2));
				return new DateTime(Year, Month, Day, hour, minute, 0, DateTimeKind.Utc);
			}
		}
		[ParameterMap("*")]
		public string Slug{get;set;}

	}
	public class BlogHandler : HttpHandler
	{
		public BlogHandler ()
		{
		}

		public IBarelyView Index()
		{
			var c=BlogEntryData.All().Find(Query.EQ("Publish",true)).SetSortOrder(SortBy.Descending("Posted"));
			c=c.SetLimit(Config.PageSize);
			var entries=new List<BlogEntryData>();
			foreach(var e in c){
				string text=BetterCache.Get<string>(e.ID.ToString());
				if(text==null)
				{
					e.Text=Config.GetMarkdown().Transform(e.Text);
					BetterCache.Add(e.ID.ToString(), e.Text);
				}
				else
				{
					e.Text=text;
				}

				entries.Add(e);
			}
			var v=new BlogIndexView();
			v.ShowPaging=true;
			v.Page=1;
			v.PageMax=((int)c.Count())/Config.PageSize+1;
			v.Entries=entries;
			
			v.Layout.Active="home";
			v.Layout.Title="Earlz.Net - Programming, Electronics, Hacking, Oh My!";
			return v;
		}
		public IBarelyView View(BlogRouteModel route)
		{
			BlogEntryData entry;
			try{
				entry=BlogEntryData.Get(route.Date);
			}catch{
				throw new HttpException(404,"Blog entry not found");
			}
			if(entry==null){
				throw new HttpException(404,"Blog entry not found");
			}
			if(GetUrl(entry)!=Request.Url.AbsolutePath)
			{
				PermanentRedirect(GetUrl(entry));
			}
			string text=BetterCache.Get<string>(entry.ID.ToString());
			if(text==null)
			{
				entry.Text=Config.GetMarkdown().Transform(entry.Text);
				BetterCache.Add(entry.ID.ToString(), entry.Text);
			}
			else
			{
				entry.Text=text;
			}
			var v=new BlogEntryView();
			v.Layout.Title="";
			if(entry.Tags!=null)
			{
				v.Layout.Title=entry.Tags[0]+" - ";
			}
			v.Layout.Title+=entry.Title+" - Earlz.Net";
#if DEBUG
			v.Layout.Title+="-"+entry.ID.ToString();
#endif
			v.ShowComments=true;
			v.Entry=entry;
			v.Summary=false;
			return v;
		}

		public IBarelyView Redirect(BlogRouteModel route)
		{
			var c=Config.GetDB().GetCollection<BlogEntryData>("entries");
			switch(RouteID)
			{
			case "redirect_id":
				BlogEntryData entry;
				try{
					entry=c.FindOneById(route.ID);
				}catch{
					throw new HttpException(404, "Blog entry not found");
				}
				if(entry==null)
				{
					throw new HttpException(404, "Blog entry not found");
				}
				PermanentRedirect(GetUrl(entry));
				break;
			}
			return null;
		}
		public IBarelyView Index(int page)
		{
			var c=BlogEntryData.All().FindAs<BlogEntryData>(Query.EQ("Publish",true)).SetSortOrder(SortBy.Descending("Posted"));
			page-=1; //so that routes start with /blog/1 than /blog/0
			if(page==0){
				PermanentRedirect("/");
			}
			c=c.SetSkip(page*Config.PageSize).SetLimit(Config.PageSize);
			int pagecount=((int)c.Count())/Config.PageSize+1; //this takes a total count on the collection, not the query.
			
			var entries=new List<BlogEntryData>();

			foreach(var e in c){
				string text=BetterCache.Get<string>(e.ID.ToString());
				if(text==null)
				{
					e.Text=Config.GetMarkdown().Transform(e.Text);
					BetterCache.Add(e.ID.ToString(), e.Text);
				}
				else
				{
					e.Text=text;
				}

				entries.Add(e);
			}
			if(entries.Count==0){
				throw new HttpException(404,"not found");
			}
			var v=new BlogIndexView();
			v.ShowPaging=true;
			page+=1;
			v.Page=page;
			v.PageMax=pagecount;
			v.Entries=entries;
			v.Layout.Active="home";
			v.Layout.Title="Archive - Page "+(page)+" of "+(v.PageMax)+" - Earlz.Net";
			return v;
		}
		public IBarelyView Edit(BlogRouteModel route, BlogEntryData data)
		{
			var entry=BlogEntryData.Get(route.Date);
			if(Method!=HttpMethod.Post)
			{
				//first diplay the page
				var v=new ModifyBlogView();
				v.Entry=entry;
				v.Layout.Title="Editing '"+entry.Title+"'";
				return v;
			}
			else
			{
				//then receive the form and store it
				data.ID=entry.ID; //set ID so mongodb can find it (and update it instead of create)
				data.Posted=entry.Posted; 
				if(!entry.Publish && data.Publish){ //if not previously published, update the time so it's "bumped"
					data.Posted=DateTime.Now; //this shouldn't have any SEO implications unless I unpublish and then republish. 
				}
				data.Edited=DateTime.Now;
				BetterCache.Remove<string>(data.ID.ToString());
				data.Save();
				Response.Redirect(GetUrl(data));
				return null; //never actually reaches here
			}
		}
		public IBarelyView New(BlogEntryData data)
		{
			if(Method!=HttpMethod.Post)
			{
				var v=new ModifyBlogView();
				v.Layout.Active="home";
				return v;
			}
			else
			{
				data.Posted=DateTime.Now;
				data.Edited=DateTime.Now;
				data.Save();
				Response.Redirect(GetUrl(data));
				return null;
			}
		}
		public static string GetFullUrl(BlogEntryData entry)
		{
			string host=Config.HostName;
			if(string.IsNullOrEmpty(host))
			{
				host=Request.Url.Host;
			}
			return "http://"+host+GetUrl(entry);
		}
		public static string GetUrl(BlogEntryData entry, string action)
		{
			//return "/view/"+entry.ID.ToString();
			DateTime tmp=entry.Posted.ToUniversalTime();
			string date=tmp.Year+"/"+tmp.Month.ToString().PadLeft(2,'0')+"/"+tmp.Day.ToString().PadLeft(2,'0')+"/"
				+ tmp.Hour.ToString().PadLeft(2,'0') + tmp.Minute.ToString().PadLeft(2,'0');

			return "/"+action+"/"+date+"/"+Routing.Slugify(entry.Title);
		}
		public static string GetUrl(BlogEntryData entry)
		{
			return GetUrl(entry, "view");
		}
		/// <summary>
		/// Takes a relative location and sends a 301 permanent redirect from this location to that location
		/// Do NOT include the domain. location should look like `/foo/bar`
		/// </summary>
		/// <param name='location'>
		/// Location.
		/// </param>
		public static void PermanentRedirect(string location)
		{
			Response.Status = "301 Moved Permanently";
			Response.AddHeader("Location",location);
			HttpContext.Current.ApplicationInstance.CompleteRequest();
		}

		public static string GetTweet(BlogEntryData entry)
		{
			return "data-url=\""+HttpUtility.HtmlAttributeEncode(GetFullUrl(entry))+"\" data-text=\""+HttpUtility.HtmlAttributeEncode(entry.Title)+"\" data-via=\"earlzdotnet\"";
		}
	}
}