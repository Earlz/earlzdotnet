
using System;
using System.Collections;
using System.ComponentModel;
using System.Web;
using System.Web.SessionState;
using Earlz.BarelyMVC;
using Earlz.BarelyMVC.Authentication;
using Earlz.LastYearsWishes.Models;

namespace Earlz.LastYearsWishes
{


	public class Global : System.Web.HttpApplication
	{

		protected virtual void Application_Start (Object sender, EventArgs e)
		{
			FSCAuth.UserStore=new MongoUserStore();
			FSCAuth.Config.SiteName="Last Year's Wishes";
			FSCAuth.Config.LoginPage="/login";
			FSCAuth.Config.UniqueHash=Config.UniqueHash;
			FSCAuth.Config.CookieHttpOnly=true;
			FSCAuth.Config.CookieSecure=false;
			FSCAuth.Config.CookieUseBase=true;
			FSCAuth.Config.CookieUseBrowserInfo=false;
			FSCAuth.Config.CookieUseIP=true;

			SimplePattern.AddShortcut("dateslug","/{Year}/{Month}/{Day}/{Time}/{*}");

			//blog routes
			Routing.AddRoute("index", HttpMethod.Get, "/",(r,f)=>new BlogHandler().Index() );
			Routing.AddRoute("paged_index", HttpMethod.Get, new SimplePattern("/blog/{page}").Where("page", GroupMatchType.Integer), 
			                 (r,f)=>new BlogHandler().Index(int.Parse(r["page"])));
			Routing.AddRoute("view", HttpMethod.Get, new SimplePattern("/view/{!dateslug!}").Dateify(), (r,f)=>new BlogHandler().View(r.Fill(new BlogRouteModel())));

			Routing.AddRoute("redirect_id", HttpMethod.Get,"/blog/view/{ID}", (r,f)=>new BlogHandler().Redirect(r.Fill(new BlogRouteModel())));
			Routing.AddSecureRoute("edit", HttpMethod.Any, new SimplePattern("/edit/{!dateslug!}").Dateify(), 
			                       (r,f)=>new BlogHandler().Edit(r.Fill(new BlogRouteModel()), f.Fill(new BlogEntryData())));
			Routing.AddSecureRoute("new", HttpMethod.Any, "/new", (r,f)=>new BlogHandler().New(f.Fill(new BlogEntryData())));

			//config routes
			Routing.AddRoute("login", HttpMethod.Any, "/login", (r,f)=>new ConfigHandler().Login(f.Fill(new LoginModel())));
			Routing.AddRoute("logout", HttpMethod.Get, "/logout", (r,f)=>new ConfigHandler().Logout());
			Routing.AddSecureRoute("/config/comments/recalc", (r,f)=>new ConfigHandler().RecalculateCommentCounts());
			Routing.AddRoute("/init", (r,f)=>new ConfigHandler().Init());
			Routing.AddRoute("/whoami", (r,f)=>new ConfigHandler().WhoAmI());
			Routing.AddSecureRoute("/comments/enable", (r,f)=>new ConfigHandler().SetComments(true));
			Routing.AddSecureRoute("/comments/disable", (r,f)=>new ConfigHandler().SetComments(false));

			//fixed page routes
			Routing.AddRoute("/aboutme", (r,f)=>new AboutMeHandler().Get());
			Routing.AddRoute("/projects", (r,f)=>new ProjectHandler().Get());

			//comment routes
			Routing.AddRoute("new", 
			                 HttpMethod.Post,
			                 new SimplePattern("/comment/{EntryID}/new"),
			                 (r,f)=>new CommentHandler().New(r["EntryID"], f.Fill(new CommentData()))
			                 );
			Routing.AddSecureRoute("delete", HttpMethod.Post, "/comment/delete/{id}", (r,f)=> new CommentHandler().Delete(r["id"]));
			Routing.AddRoute("/comments/list", (r,f)=> new CommentHandler().List());

			//tag routes
			Routing.AddSecureRoute("draft tag", HttpMethod.Get, 
			                 new SimplePattern("/tags/private-draft/{page}").Where("page",GroupMatchType.Integer), 
			                 (r,f)=>new TagHandler().Tag("private-draft", int.Parse(r["page"])));
			Routing.AddRoute("tag", HttpMethod.Get,
			                 new SimplePattern("/tags/{tag}/{page}").Where("page",GroupMatchType.Integer), 
			                 (r,f)=>new TagHandler().Tag(r["tag"], int.Parse(r["page"])));
			Routing.AddRoute("/tags/{tag}", (r,f)=>new TagHandler().RedirectZero(r["tag"]));
			Routing.AddRoute("/tags", (r,f)=>new TagHandler().Index());

			//Apparently apache is fucking retarded so I have to block the directory here
			Routing.AddRoute("fuck it", HttpMethod.Any, "/bin/{*}", (r,f) => {throw new HttpException(401, "denied fucker"); return null;});

			//Routing.AddRoute("new", HttpMethod.Post, new SimplePattern("/comment/{!dateslug!}/new").Dateify(), (r,f)=>new CommentHandler().
			//Routing.AddSecureRoute("new", HttpMethod.Any, "/new", 

			//Routing.AddRoute("view", HttpMethod.Get, 
			/*	
			Routing.AddRoute("aboutme","/aboutme",(p) => {return new AboutMeHandler();} );
			//blog routes
			Routing.AddRoute("new","/blog/new",(p)=>{return new BlogHandler();});
			Routing.AddRoute("edit", new SimplePattern("/blog/edit/{id}").Where("id",GroupMatchType.HexString), (p)=>{return new BlogHandler();});
			Routing.AddRoute("view", new SimplePattern("/blog/view/{id}").Where("id",GroupMatchType.HexString), (p)=>{return new BlogHandler();});

			Routing.AddRoute("view_new", new SimplePattern("/blog/view/{year}/{month}/{day}/{tiime}").Dateify(), (prop) => {return new BlogHandler();});

			Routing.AddRoute("delete",new SimplePattern("/blog/delete/{id}").Where("id", GroupMatchType.HexString), (p)=>{return new BlogHandler();});
			Routing.AddRoute("index","/blog",(p)=>{return new BlogHandler();}); 
			Routing.AddRoute("paged_index",new SimplePattern("/blog/{page}").Where("page",GroupMatchType.Integer),(p)=>{return new BlogHandler();}); 
			Routing.AddRoute("view","/page/{id}",(p)=>{return new BlogHandler();}); //wtf is this used for? 
			
			//comment routes
			Routing.AddRoute("new",new SimplePattern("/blog/{entryid}/comment/add").Where("entryid", GroupMatchType.HexString), (p)=>{return new CommentHandler();});
			Routing.AddRoute("delete",new SimplePattern("/comments/delete/{commentid}").Where("commentid", GroupMatchType.HexString), (p)=>{return new CommentHandler();});
			Routing.AddRoute("list","/comments/list",(p)=>{return new CommentHandler();});
			

			//config routes
			Routing.AddRoute("init","/init",(p)=>{return new ConfigHandler();});
			Routing.AddRoute("login","/login",(p)=>{return new ConfigHandler();});
			Routing.AddRoute("logout","/logout",(p)=>{return new ConfigHandler();});
			Routing.AddRoute("whoami","/whoami",(p)=>{return new ConfigHandler();});
			Routing.AddRoute("enable-comments","/comments/enable",(p)=>{return new ConfigHandler();});
			Routing.AddRoute("disable-comments","/comments/disable",(p)=>{return new ConfigHandler();});
			
			//project routes
			Routing.AddRoute("projects","/projects",(p)=>{return new ProjectHandler();});
			
			//tag routes
			Routing.AddRoute("index","/tags",(p)=>{return new TagHandler();});
			Routing.AddRoute("tag",new SimplePattern("/tags/{tag}/{page}").Where("page",GroupMatchType.Integer),(p)=>{return new TagHandler();});
			Routing.AddRoute("tag_zero","/tags/{tag}",(p)=>{return new TagHandler();});
			*/
		}


		protected virtual void Session_Start (Object sender, EventArgs e)
		{
		}

		protected virtual void Application_BeginRequest (Object sender, EventArgs e)
		{
			if(!string.IsNullOrEmpty(Config.HostName) && Request.Url.Host!=Config.HostName)
			{ 
				Response.Status = "301 Moved Permanently";
				Response.AddHeader("Location","http://"+Config.HostName+Request.Url.PathAndQuery);
				CompleteRequest();
				return;
			}

			Routing.DoRequest(Context,this);
		}

		protected virtual void Application_EndRequest (Object sender, EventArgs e)
		{
		}

		protected virtual void Application_AuthenticateRequest (Object sender, EventArgs e)
		{
		}

		protected virtual void Application_Error (Object sender, EventArgs e)
		{
			//CustomErrorsFixer.HandleErrors(Context);
		}

		protected virtual void Session_End (Object sender, EventArgs e)
		{
		}

		protected virtual void Application_End (Object sender, EventArgs e)
		{
		}
	}
	public static class RoutingExtensions
	{
		public static SimplePattern Dateify(this SimplePattern p)
		{
			return p.Where("Year", GroupMatchType.Integer).Where("Month", GroupMatchType.Integer).Where("Day", GroupMatchType.Integer)
					.Where("Time", GroupMatchType.Integer);

		}
	}
}

