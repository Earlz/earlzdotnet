using System;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using System.Web;
using System.Collections.Generic;
using Earlz.BarelyMVC;
using Earlz.BarelyMVC.ViewEngine;
using Earlz.BarelyMVC.Authentication;
using System.Linq;
using Earlz.LastYearsWishes.Models;


namespace Earlz.LastYearsWishes
{
	public class CommentHandler : HttpHandler
	{
		public CommentHandler ()
		{
		}
		public IBarelyView List ()
		{
			var c=CommentData.Collection()
				.FindAllAs<CommentData>()
				.SetSortOrder(SortBy.Descending("Posted"));
			var v=new CommentListView();
			v.Comments=c;
			return v;
		}
		public IBarelyView New(string entryid, CommentData post)
		{
			if(Form["Link"]!=""){
				return null;
			}
			post.Name=post.Name.Substring(0,Math.Min(post.Name.Length,32));
			post.Text=post.Text.Substring(0,Math.Min(post.Text.Length,1024));
			post.IPAddress=Request.UserHostAddress;
			post.UserAgent=Request.UserAgent;
			post.Text=CheckLines(post.Text);
			if(post.Text=="OOONNNNYYYYTTTTIIIII")
			{
				throw new HttpException(500, "Fuck you.");
			}
			BlogEntryData entry=BlogEntryData.Get(entryid);
			if(entry==null)
			{
				throw new HttpException(404, "Blog entry not found");
			}
			post.EntryID=entry.ID;
			post.Posted=DateTime.Now;
			post.Save();
			BlogEntryData.RecalculateComments(entry.ID);
			Response.Redirect(BlogHandler.GetUrl(entry));
			return null;
		}
		public IBarelyView Delete(string id)
		{
			var comment=CommentData.Get(new ObjectId(id));
			CommentData.Delete(id);
			BlogEntryData.RecalculateComments(comment.EntryID);
			return new WrapperView("Deleted comment");
		}

		static string CheckLines(string s)
		{
			s=s.Replace("\r","");
			int count=0;
			int i;
			for(i=0;i<s.Length;i++){
				var c=s[i];
				if(c=='\n'){
					count++;
					if(count>20){
						s=s.Remove(i,1);
						i--;
					}
				}
			}
			return s;
			
		}
	}
}

