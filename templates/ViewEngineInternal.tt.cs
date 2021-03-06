//<#+
/*
Copyright (c) 2010 - 2012 Jordan "Earlz/hckr83" Earls  <http://lastyearswishes.com>
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions
are met:

1. Redistributions of source code must retain the above copyright
   notice, this list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright
   notice, this list of conditions and the following disclaimer in the
   documentation and/or other materials provided with the distribution.
3. The name of the author may not be used to endorse or promote products
   derived from this software without specific prior written permission.
   
THIS SOFTWARE IS PROVIDED ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY
AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL
THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS;
OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

#if NOT_IN_T4
//Apparently T4 places classes into another class, making namespaces impossible
namespace Earlz.BarelyMVC.ViewEngine.Internal
{
	using System;
	using System.Linq;
	using System.Text;
	using System.Collections.Generic;
	using System.IO;
#endif
	
	
	public class ViewGenerator : ClassGenerator
	{
		bool HasFlash=false;
		string Layout;
		string LayoutField;
		/// <summary>
		/// The stringbuilder for the body of the RenderView method
		/// </summary>
		StringBuilder view=new StringBuilder();
		StringBuilder external=new StringBuilder();
		public override string OtherCode {
			get {
				return external.ToString();
			}
			set {
				external=new StringBuilder(value);
			}
		}
		
		string input;
		public string Input
		{
			get{return input;}
			set{input=value;}
		}
		bool RenderDirectly=false;
		/// <summary>
		/// This should be set in the T4 template
		/// </summary>
		public override string BaseClass {
			get {
				return base.BaseClass ?? DefaultBaseClass;
			}
			set {
				base.BaseClass = value;
			}
		}
		public static string DefaultBaseClass=null;
		public bool DetectNulls=true;
		string DefaultWriter="";
		public ViewGenerator(string file,string name,string namespace_,bool renderdirectly,bool detectnulls,string defaultwriter){
			var f=File.OpenText(file);
			string text=f.ReadToEnd();
			Init (text, name, namespace_, renderdirectly, detectnulls, defaultwriter);
		}
		public ViewGenerator(string name, string namespace_, bool renderdirectly, bool detectnulls, string defaultwriter)
		{
			Init (null, name, namespace_, renderdirectly, detectnulls, defaultwriter);
		}
		void Init(string text,string name,string namespace_,bool renderdirectly,bool detectnulls,string defaultwriter){
			input=text;
			Name=name;
			Namespace=namespace_;
			RenderDirectly=renderdirectly;
			DetectNulls=detectnulls;
			DefaultWriter=defaultwriter;
		}
		
		int DoVariables (int start)
		{
			int end=input.Substring(start+1).IndexOf("@}");
			if(end==-1){
				throw new ApplicationException("Could not find end of variable block");
			}
			string block=input.Substring(start+1,end);
			block=block.Trim();
			var lines=block.Replace("\r"," ").Replace("\n"," ").Split(new char[]{';'}, StringSplitOptions.RemoveEmptyEntries);
			foreach(var l in lines)
			{
				var line=l.Trim();
				var p=new Property();
				if(line[0]=='{')
				{
					//found documentation
					int enddoc=line.IndexOf("}");
					p.PrefixDocs=line.Substring(1, enddoc-1).Trim();
					line=line.Substring(enddoc+1).Trim();
				}
				var words=line.Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries);
				int asword=Array.IndexOf(words,"as");
				if(asword==-1)
				{
					throw new ApplicationException("`as` expected but not found");
				}
				int accessword2=asword-3; //for things like `public virtual`
				int accessword=asword-2;
				int nameword=asword-1;
				if(accessword2>-1)
				{
					p.Accessibility=words[accessword2].Trim();
				}
				else
				{
					p.Accessibility="";
				}
				if(accessword>-1)
				{
					p.Accessibility+=" "+words[accessword].Trim();
				}
				else
				{
					p.Accessibility="public";
				}
				if(nameword<0)
				{
					throw new ApplicationException("Expected a name before `as` but none found");
				}
				p.Name=words[nameword].Trim();
				
				p.Type=line.Substring(line.IndexOf(" as ")+4).Trim();
				if(p.Name=="Flash")
				{
					HasFlash=true;
					if(p.Type.ToLower()!="string" && !p.Accessibility.Contains("public"))
					{
						throw new ApplicationException("Flash variable must be a public string");
					}
				}
				else
				{
					Properties.Add(p);
				}
			}/*
            List<string> words=new List<string>(block.Split(new char[]{' ','\t','\n','\r'},StringSplitOptions.RemoveEmptyEntries));
            while(words.Contains("")){
                words.Remove("");
            }
            int i;
            for(i=0;i<words.Count-2;i+=3){
                var p=new Property();
                p.Accessibility="virtual public";
                p.Name=words[i];
                if(p.Name=="private" || p.Name=="protected" || p.Name=="public"){
                    i++;
                    p.Accessibility=p.Name;
                    p.Name=words[i];
                }
                if(words[i+1]!="as"){
                    throw new ApplicationException("'as' expected. Found: "+words[i+1]);
                }
                p.Type=words[i+2];
                
                if(p.Type[p.Type.Length-1]!=';' && (i<words.Count-3 || words[i+3]!=";")){
                    throw new ApplicationException("';' expected. Found: "+words[i+3]);
                }
                if(i<words.Count-3 && words[i+3]==";"){
                    i++;
                }else{
                    p.Type=p.Type.Substring(0,p.Type.Length-1);
                }
                if(p.Name=="Flash"){
                    HasFlash=true;
                    if(p.Type.ToLower()!="string" && !p.Accessibility.Contains("public")){
                        throw new ApplicationException("Flash variable must be a public string");
                    }
                }else{
                    Properties.Add(p);
                }
            }       
            */
			return end+=3; //+=2 for @} ending
		}
		int WriteVariable(int start){
			int end=input.Substring(start+1).IndexOf("=}");
			if(end==-1){
				throw new ApplicationException("Could not find end of output block");
			}
			string block=input.Substring(start+1,end);
			block=block.Trim();
			
			var p=Properties.Find(x=>x.Name==block);
			string code;
			if(p==null){
				code=block;
				//don't throw an error I guess? The properties could be coming from elsewhere (inheritance, etc)
			}else{
				code=p.Name;
			}
			view.AppendLine(@"{
                object __v;
                ");
			if(DetectNulls){
				view.AppendLine(@"
                try{
                    __v="+code+@";
                }catch(NullReferenceException){
                    __v=null;
                }
                ");
			}else{
				view.AppendLine(@"
                    __v="+code+@";
                ");
			}
			view.AppendLine("__OutputVariable(__v);");
			view.AppendLine("}");
			return end+=3;
		}
		
		int WriteRawOutput(int start){
			int end=input.Substring(start+1).IndexOf("-}");
			if(end==-1){
				throw new ApplicationException("Could not find end of code output block");
			}
			string block=input.Substring(start+1,end);
			block=block.Trim();
			
			view.AppendLine(@"
                __Write("+block+");");
			
			return end+=3;
		}
		
		int WriteCode(int start){
			int end=input.Substring(start+1).IndexOf("#}");
			if(end==-1){
				throw new ApplicationException("Could not find end of code block");
			}
			string block=input.Substring(start+1,end);
			block=block.Trim();
			view.AppendLine(block);
			return end+=3;
		}
		int WriteExternalCode(int start){
			int end=input.Substring(start+1).IndexOf("+}");
			if(end==-1){
				throw new ApplicationException("Could not find end of external code block");
			}
			string block=input.Substring(start+1,end);
			block=block.Trim();
			external.AppendLine(block);
			return end+=3;
		}
		int WriteHelper(int start){
			int end=input.Substring(start+1).IndexOf("?}");
			if(end==-1){
				throw new ApplicationException("Could not find end of helper block");
			}
			string block=input.Substring(start+1,end);
			block.Trim();
			int stop=block.IndexOfAny(new char[]{' ','='});
			string classname;
			classname=block.Substring(0,stop);
			
			block=block.Substring(stop);
			block=block.Trim();
			view.AppendLine(@"
            {
                var __v=new "+classname+"{"+block+@"};
                __v.Layout=null; //HACK
                __Write(__v);
            }
            ");
			return end+=3;
		}
		int ParseKeyword(int start){
			int end=input.Substring(start+1).IndexOf("!}");
			if(end==-1){
				throw new ApplicationException("Could not find end of keyword block");
			}
			string block=input.Substring(start+1,end);
			
			int stop=block.IndexOfAny(new char[]{' ','='});
			string keyword;
			if(stop<0){
				keyword=block.Substring(0,end);
			}else{
				keyword=block.Substring(0,stop);
			}
			switch(keyword){
			case "base":
				BaseClass=block.Substring(stop+1);
				break;
			case "name":
				Name=block.Substring(stop+1);
				break;
			case "namespace":
				Namespace=block.Substring(stop+1);
				break;
			case "layout":
				Layout=block.Substring(stop+1);
				break;
			case "layout_field":
				LayoutField=block.Substring(stop+1);
				break;
			case "if":
				view.Append("if(");
				view.Append(block.Substring(stop+1));
				view.AppendLine("){");
				break;
			case "endif":
				view.AppendLine("}");
				break;
			case "else":
				view.AppendLine("}else{");
				break;
			case "elseif":
				view.Append("}else if(");
				view.Append(block.Substring(stop+1));
				view.AppendLine("){"); 
				break;
			case "foreach":
				DoForeach(block,stop);
				break;
			case "endforeach":
				view.AppendLine("}");
				break;
			case "use_once":
				//DoUseOnce(block,stop);
				break;
			case "render_directly":
				RenderDirectly=true;
				break;
			case "render_tostring":
				RenderDirectly=false;
				break;
			case "detect_nulls":
				DetectNulls=true;
				break;
			case "do_not_detect_nulls":
				DetectNulls=false;
				break;
			default:
				throw new ApplicationException("Unknown keyword used");
			}
			
			return end+=3;
		}
		public void DoForeach(string block,int stop){
			view.Append("foreach(");
			var pieces=block.Substring(stop+1).Split(new char[]{' '});
			if(pieces.Count()<3){
				throw new ApplicationException("Expected format of {!foreach variable in enumerator!}");
			}
			view.Append("var ");
			view.Append(block.Substring(stop+1));
			view.AppendLine("){");
		}
		/*
        public void DoUseOnce(string block,int stop){
            //How to implement this?
            //Intention is to have this create a public variable, and write it out only once. 
            //Is it even needed?
            Variable v=new Variable();
            var pieces=block.Substring(stop+1).Split(new char[]{' '});
            if(pieces.Count()<3){
                throw new ApplicationException("Expected format of {!use_once variablename as variabletype!}");
            }
            v.Name=pieces[0];
            v.VarType=pieces[2];
            Variables.Add(v);
        }
        */
		string GenerateViewBody(){
			char last='\0';
			view.Append("__Write(@\"");
			for(int i=0;i<input.Length;i++){
				if(last=='{' && input[i]=='@'){
					view.AppendLine("\");");
					i+=DoVariables(i);
					view.Append("__Write(@\"");
				}else if (last=='{' && input[i]=='='){
					view.AppendLine("\");");
					i+=WriteVariable(i);
					view.Append("__Write(@\"");
				}else if (last=='{' && input[i]=='#'){
					view.AppendLine("\");");
					i+=WriteCode(i);
					view.Append("__Write(@\"");
				}else if(last=='{' && input[i]=='+'){
					i+=WriteExternalCode(i);
				}else if (last=='{' && input[i]=='!'){
					view.AppendLine("\");");
					i+=ParseKeyword(i);
					view.Append("__Write(@\"");
				}else if(last=='{' && input[i]=='-'){
					view.AppendLine("\");");
					i+=WriteRawOutput(i);
					view.Append("__Write(@\"");
				}else if(last=='{' && input[i]=='?'){
					view.AppendLine("\");");
					i+=WriteHelper(i);
					view.Append("__Write(@\"");
				}else if(last=='\\' && input[i]=='{'){
					view.Append(Escape(input[i]));
					last='\0';
					continue;
				}else{
					view.Append(Escape(last));
					if(i==input.Length-1){
						view.Append(Escape(input[i]));
					}
				}
				if(i<input.Length){
					last=input[i];
				}
			}
			view.AppendLine("\");");
			string s=view.ToString();
			return s;
		}
		
		public override string ToString ()
		{
			string s=base.ToString();
			return s;
		}
		bool GeneratedBefore=false;
		public void Generate()
		{
			if(GeneratedBefore)
			{
				throw new NotSupportedException("The view class can only be generated once for each class. Create a new instance of this class to regenerate the class");
			}
			GeneratedBefore=true;
			string viewbody=GenerateViewBody();
			//add the internals required first
			var f=new Field
			{
				Name="__Output",
				Type="StringBuilder",
				PrefixDocs="For internal use only!"
			};
			Fields.Add(f);
			f=new Field
			{
				Name="__InLayout",
				Type="bool",
				InitialValue="false",
				PrefixDocs="For internal use only!"
			};
			Fields.Add(f);
			f=new Field
			{
				Name="__Writer",
				Type="TextWriter",
				PrefixDocs="For internal use only!"
			};
			Fields.Add(f);
			f=new Field
			{
				Name="__RenderDirectly",
				InitialValue=RenderDirectly.ToString().ToLower(),
				Type="bool",
				PrefixDocs="For internal use only!"
			};
			Fields.Add(f);
			
			var p=new Property
			{
				Name="Layout",
				GetMethod="get;",
				SetMethod="set;",
				PrefixDocs="This is the layout of the given view (master page)",
				Accessibility="public",
				Type=Layout ?? "IBarelyView"
			};
			Properties.Add(p);
			p=new Property
			{
				Name="RenderedDirectly",
				Accessibility="public override",
				Type="bool",
				GetMethod="get{ return __RenderDirectly; }",
				SetMethod=null,
				PrefixDocs="Indicates if this view is set to be rendered directly to the appropriate stream"
					
			};
			Properties.Add(p);
			if(!HasFlash)
			{
				p=new Property();
				p.Accessibility="public override";
				p.Name="Flash";
				p.Type="string";
				p.GetMethod="get{return Layout.Flash;}";
				p.SetMethod="set{Layout.Flash=value;}";
				p.PrefixDocs=@"The ""Flash"" notification text(passes through to the layout";
				Properties.Add(p);
			}
			
			var m=new Method();
			m.Name="BuildOutput";
			m.Body=viewbody;
			Methods.Add(m);
			m=new Method();
			m.Name="__Init";
			m.Params.Add(new MethodParam{Name="writer", Type="TextWriter"});
			m.PrefixDocs="internal use only";
			m.Body="__Writer=writer;";
			if(Layout!=null)
			{
				m.Body+="Layout=new "+Layout+"(); "+"Layout."+LayoutField+"=this;";
			}
			//constructors
			Methods.Add(m);
			m=new Method();
			m.Accessibility="public";
			m.Name=Name;
			m.ReturnType="";
			m.Body="if(__RenderDirectly){ __Init("+DefaultWriter+"); }else{ __Init(null); }";
			m.PrefixDocs="Initialize with default options";
			Methods.Add(m);
			m=new Method();
			m.Accessibility="public";
			m.Name=Name;
			m.Params.Add(new MethodParam{Name="UseHttpResponse", Type="bool"});
			m.ReturnType="";
			m.Body="if(UseHttpResponse){ __RenderDirectly=true; __Init("+DefaultWriter+");}else{ __Init(null);}";
			m.PrefixDocs="Initialize a view to either directly render or not and use the default TextWriter";
			Methods.Add(m);
			m=new Method();
			m.Accessibility="public";
			m.Name=Name;
			m.ReturnType="";
			m.Params.Add(new MethodParam{Name="writer", Type="TextWriter"});
			m.Body="__Init(writer);";
			m.PrefixDocs="Initialize a view with the chosen TextWriter";
			Methods.Add(m);
			//Write methods
			m=new Method();
			m.Name="__Write";
			m.Accessibility="protected virtual";
			m.Params.Add(new MethodParam{Name="s", Type="string"});
			m.Body="if(__Writer!=null){ __Writer.Write(s); } __Output.Append(s);";
			m.PrefixDocs="Writes a string to the output (or adds it to the output if direct rendering)";
			Methods.Add(m);
			m=new Method();
			m.Name="__Write";
			m.Accessibility="protected virtual";
			m.Params.Add(new MethodParam{Name="v", Type="IBarelyView"});
			m.Body="string s=v.RenderView(); __Output.Append(s);";
			m.PrefixDocs="Renders the view and adds it to the output";
			Methods.Add(m);
			//RenderView
			m=new Method();
			m.Accessibility="public override";
			m.ReturnType="string";
			m.Name="RenderView";
			m.Body=
				@"__Output=new StringBuilder();
if(Layout==null){
        BuildOutput();
        return __Output.ToString();
}
if(__InLayout){
        //If we get here, then the layout is currently trying to render itself(and we are being rendered as a partial/sub view)
        __InLayout=false;
        BuildOutput();
        return __Output.ToString();
}else{
        //otherwise, we are here and someone called RenderView on us(and we have a layout to render first)
        __InLayout=true;
        return Layout.RenderView(); 
}
//This should recurse no more than 2 times
//Basically, this will go to hell if there is ever more than 1 partial view with a Layout set.";
			m.PrefixDocs="Renders the view to a string and to the chosen TextWriter string if directly rendered";
			Methods.Add(m);
			
			
			m=new Method();
			m.Accessibility="protected virtual";
			m.ReturnType="void";
			m.Name="__OutputVariable";
			m.Params.Add(new MethodParam{Name="v", Type="object"});
			m.Body=@"
            {
                if(v!=null)
                {
                    var e=v as System.Collections.IEnumerable;
                    if (e!=null)
                    {
                        foreach(var item in e){ 
                            var view=item as Earlz.BarelyMVC.ViewEngine.IBarelyView;
                            if(view!=null){
                                __Write(view);
                            }else{
                                __Write(item.ToString());
                            }
                        }
                    }else{
                        var view=v as Earlz.BarelyMVC.ViewEngine.IBarelyView;
                        if(view!=null){
                            __Write(view);
                        }else{
                            __Write(v.ToString());
                        }
                    }
                }        
            }";
			Methods.Add(m);
			
		}
		
		string Escape(char c){
			if(c=='\"'){
				return "\"\"";
			}
			if(c=='\0'){
				return "";
			}
			return c.ToString();
		}
	}
	
	
	
	//shove this all into one file so we don't force implementers to hand combine this or copy over more than 2 files
	public class ClassGenerator : CodeElement
	{
		virtual public List<Property> Properties
		{
			get;
			private set;
		}
		virtual public List<Method> Methods
		{
			get;
			private set;
		}
		virtual public List<Field> Fields
		{
			get;
			private set;
		}
		virtual public string Namespace
		{
			get;set;
		}
		virtual public string OtherCode
		{
			get;set;
		}
		public virtual string BaseClass
		{
			get;set;
		}
		public ClassGenerator()
		{
			Properties=new List<Property>();
			Methods=new List<Method>();
			Fields=new List<Field>();
			Accessibility="";
		}
		public override string ToString ()
		{
			StringBuilder sb=new StringBuilder();
			sb.Append("namespace "+Namespace);
			sb.AppendLine("{");
			sb.AppendLine(PrefixDocs);
			sb.AppendLine(GetTab(1)+Accessibility+" class "+Name+": "+BaseClass);
			sb.AppendLine(GetTab(1)+"{");
			foreach(var p in Properties)
			{
				sb.AppendLine(p.ToString());
			}
			foreach(var m in Methods)
			{
				sb.AppendLine(m.ToString());
			}
			foreach(var f in Fields)
			{
				sb.AppendLine(f.ToString());
			}
			sb.AppendLine(OtherCode);
			sb.AppendLine(GetTab(1)+"}");
			sb.AppendLine("}");
			return sb.ToString();
		}
		
	}
	abstract public class CodeElement
	{
		public const string Tab="    ";
		public string Name
		{
			get;
			set;
		}
		public string Accessibility
		{
			get;
			set;
		}
		string prefixdocs;
		virtual public string PrefixDocs
		{
			get
			{
				return prefixdocs;
			}
			set
			{
				prefixdocs=GetTab(2)+"///<summary>\n"+GetTab(2)+"///"+value+"\n"+GetTab(2)+"///</summary>";
			}
		}
		public override string ToString ()
		{
			throw new NotImplementedException();
		}
		public static string GetTab(int nest)
		{
			string tmp="";
			for(int i=0;i<nest;i++)
			{
				tmp+=Tab;
			}
			return tmp;
		}
		protected CodeElement()
		{
			Accessibility="";
			PrefixDocs="";
		}
	}
	public class Property : CodeElement
	{
		public string Type
		{
			get;set;
		}
		public string GetMethod
		{
			get;
			set;
		}
		public string SetMethod
		{
			get;
			set;
		}
		public override string ToString ()
		{
			string tmp=GetTab(2)+PrefixDocs+"\n";
			tmp+=GetTab(2)+CodeElement.Tab+Accessibility+" "+Type+" "+Name+"{\n";
			if(GetMethod!=null)
			{
				tmp+=GetTab(2)+GetMethod+"\n";
			}
			if(SetMethod!=null)
			{
				tmp+=GetTab(2)+SetMethod+"\n";
			}
			tmp+=GetTab(2)+"}\n";
			return tmp;
		}
		public Property()
		{
			GetMethod="get;";
			SetMethod="set;";
		}
	}
	public class Field : CodeElement
	{
		public string Type
		{
			get;
			set;
		}
		public string InitialValue
		{
			get;
			set;
		}
		public override string ToString ()
		{
			string tmp=GetTab(2)+PrefixDocs+"\n";
			tmp+=GetTab(2)+Accessibility+" " +Type+" " +Name;
			if(InitialValue!=null)
			{
				tmp+="="+InitialValue+";";
			}else{
				tmp+=";";
			}
			return tmp;
		}
	}
	public class Method : CodeElement
	{
		public string ReturnType
		{
			get;
			set;
		}
		public List<MethodParam> Params
		{
			get;set;
		}
		public string Body
		{
			get;set;
		}
		public Method()
		{
			Params=new List<MethodParam>();
			Body="";
			ReturnType="void";
		}
		public override string ToString ()
		{
			string tmp=GetTab(2)+PrefixDocs+"\n";
			tmp=GetTab(2)+Accessibility+" "+ReturnType+" "+Name+"(";
			for(int i=0;i<Params.Count;i++)
			{
				tmp+=Params[i].ToString();
				if(i==Params.Count-1)
				{
					tmp+=")";
				}
				else
				{
					tmp+=", ";
				}
			}
			if(Params.Count==0)
			{
				tmp+=")";
			}
			tmp+="\n"+GetTab(2)+"{\n";
			tmp+=Body;
			tmp+="\n"+GetTab(2)+"}";
			return tmp;
		}
	}
	public class MethodParam
	{
		public string Name{get;set;}
		public string Type{get;set;}
		public override string ToString ()
		{
			return Type+" "+Name;
		}
	}
	
#if NOT_IN_T4
}
#endif
//#>