//-> отслеживать изменения WPAD.DAT




/*
 * Created by SharpDevelop.
 * Date: 27.07.2017
 * Time: 10:39
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Mime;
using System.Net.NetworkInformation;
using System.Threading;
using System.Text;
using System.Collections;
using System.IO;
using Microsoft.Win32;

//using System.Deployment.Application;

namespace Router
{
	//*********************************************************
	class ProxyControl
	//*********************************************************		
	{
		public string Path="c:\\Program Files\\3proxy\\bin64";
		public string Programm="3proxy";
		private string FileParentProxy="ParentProxy.cfg";
		public Int32 PID=0;
		public bool IsRun=false;
		public HTTPProxy ParentProxy=null;
		
	public int Start(HTTPProxy newParentProxy=null)
		{
		if (this.ParentProxy!=newParentProxy)this.ParentProxy=newParentProxy;
		#if DEBUG
			Debug.Print("Start proxy");			
		#endif
		if(IsProxyRun(PID))Stop();
		if (ParentProxy!=null)
			{
			StreamWriter sw = new StreamWriter(Path+"\\"+FileParentProxy);
			sw.WriteLine("parent 1000 http {0} {1}",ParentProxy.IP,ParentProxy.Port);
			sw.Close();
			}
		Process myProcess = new Process();

        try
        {
            myProcess.StartInfo.UseShellExecute = false;
            // You can start any process, HelloWorld is a do-nothing example.
            myProcess.StartInfo.FileName  =this.Path+"\\"+this.Programm;
            myProcess.StartInfo.WorkingDirectory=this.Path;
            	
            //myProcess.StartInfo.CreateNoWindow = true;
            IsRun=myProcess.Start();
            if (IsRun) 
            	{
            	Console.WriteLine("Proxy started sucessfuly");
            	this.PID=myProcess.Id;
            	}
            else Console.WriteLine("ERR: Proxy didn't started");
            // This code assumes the process you are starting will terminate itself. 
            // Given that is is started without a window so you cannot terminate it 
            // on the desktop, it must terminate itself or you can do it programmatically
            // from this application using the Kill method.
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }		
	return 0;
		}
	
	public int Reconfig()
		{
		#if DEBUG
			Debug.Print("Reconfig proxy");			
		#endif	
		Stop();
		Start();
		return 0;
		}
	protected bool IsProxyRun(Int32 PID=0)//https://msdn.microsoft.com/ru-ru/library/x8b2hzk8(v=vs.110).aspx
		{
		bool IsProxyRun=false;		
		//Process currentProcess = Process.GetCurrentProcess();
		// Get all processes on a remote computer.
		//Process[] remoteAll = Process.GetProcesses("myComputer");
		Process ProcessProxy;
		if (PID==0)
			{
			Process[] localByName = Process.GetProcessesByName(Programm);			
			IsProxyRun=localByName.Length!=0;
			#if DEBUG
			Debug.Print("ProcessProxy count {0}",localByName.Length);			
			#endif
			}
		else	
			{
			ProcessProxy = Process.GetProcessById(PID);
			#if DEBUG
			Debug.Print("ProcessProxy PID {0} {1} {2}",ProcessProxy.Id,ProcessProxy.ProcessName,PID);			
			#endif
			if (PID==ProcessProxy.Id && ProcessProxy.ProcessName==this.Programm)
				IsProxyRun=true;
			}
		return (IsProxyRun);
		}
	public void Stop(Int32 PID=0)
		{
		#if DEBUG
			Debug.Print("Stop proxy");			
		#endif
		if (PID==0)
			{
			Process[] localByName = Process.GetProcessesByName(Programm);
			foreach(Process p in localByName)
				{
				#if DEBUG
				Debug.Print("Kill PID {0}",p.Id);			
				#endif
				p.Kill();
				p.WaitForExit();
				}
			}
		}	
	}
	//*********************************************************
	class HTTPProxy
	//*********************************************************		
	{
		public string Host="localhost";
		public IPAddress IP=IPAddress.Parse("127.0.0.1");
		public int Port=3128;
		public string User="";
		public string Password="";		
		public int StatusPing=0;//0-down, 1-up
		private bool GetStatusPing =false;
		public DateTime LastStatusPingDateTime=DateTime.Now;
		public int StatusHTTP=0;//0-down, 1-up
		private bool GetStatusHTTP =false;
		public DateTime LastStatusHTTPDateTime=DateTime.Now;
		public int Priority=0;
		public string URLtestPage="http://ya.ru";
	public HTTPProxy(string Host="127.0.0.1", int  Port=3128,int Priority=0,string URLtestPage=null)		
		{
		this.Host=Host;		
		this.Port=Port;
		this.Priority=Priority;		
		if (URLtestPage==null)URLtestPage="http://127.0.0.1:80";
		this.URLtestPage=URLtestPage;
		try 
			{
			this.IP=IPAddress.Parse(Host);
			}
		catch (Exception e)
			{			
			#if DEBUG
			Debug.Print(e.Message);
			Debug.Print("Request to DNS for {0}",Host);
			#endif
			try 
				{
				IPHostEntry entry = Dns.GetHostEntry(hostNameOrAddress: "www.google.com");
				foreach (IPAddress addr in entry.AddressList)
					{
					this.IP = addr;
					#if DEBUG
					Debug.Print("{0}",addr);
					#else
					break;
					#endif					
					}
				}
			catch (Exception e2)
				{			
				#if DEBUG
				Debug.Print(e2.Message);
				#endif
				//-> Find ip over DNS
				}
			}
		GetStatus();
		
		}	
	public void GetStatus()
		{
		if (!this.GetStatusPing)
			{			
			Thread thread = new Thread(HTTPProxy.GettingStatus);   
			thread.Start(this);
			}		
		}
	private static void GettingStatus(object refHTTPProxy)
		{		
		HTTPProxy p = ((HTTPProxy)refHTTPProxy);
		if (!p.GetStatusPing)
			{
			p.GetStatusPing=true;
			int LastStatusPing=p.StatusPing;			
			Ping pingSender = new Ping ();//https://msdn.microsoft.com/ru-ru/library/system.net.networkinformation.ping(v=vs.110).aspx	
			PingReply reply = pingSender.Send (p.IP);
			if (reply.Status == IPStatus.Success)p.StatusPing=1;
			else p.StatusPing=0;			
		if(LastStatusPing!=p.StatusPing)
			p.LastStatusPingDateTime=DateTime.Now;
			
			p.GetStatusPing=false;
			}
		if (!p.GetStatusHTTP)
			{		
			p.GetStatusHTTP=true;
			int LastStatusHTTP=p.StatusHTTP;			
			WebProxy proxyObject = new WebProxy(string.Format("http://{0}:{1}/",p.IP,p.Port),true);			
			try 
				{				
				//->HttpWebRequest https://msdn.microsoft.com/ru-ru/library/system.net.httpwebrequest(v=vs.110).aspx
				WebRequest req = WebRequest.Create(p.URLtestPage); //https://msdn.microsoft.com/ru-ru/library/system.net.webrequest(v=vs.90).aspx				
				req.Timeout=10000;
				req.Proxy = proxyObject;
				HttpWebResponse response=null;
				try
					{
					
					response = (HttpWebResponse)req.GetResponse ();
					#if DEBUG			
					Debug.Print("{2}:{3} {0} {1}",response.StatusDescription,response.StatusCode,p.IP,p.Port);
					#endif
					if (response.StatusCode==HttpStatusCode.OK)p.StatusHTTP=1;
					}
				catch(WebException e) 
					{
					p.StatusHTTP=0;					
					#if DEBUG	
					Debug.Print("10  {0} {1}:{2} - {3} {4}",e.Status,p.IP,p.Port,req.RequestUri,e.Message);
					#endif
					}
				catch (Exception e)
					{					
					p.StatusHTTP=0;
					#if DEBUG			
					Debug.Print("20 {0} {1}:{2}",e.Message,p.IP,p.Port);
					#endif
					}				
				}
			catch (Exception e)
				{
				p.StatusHTTP=0;				
				#if DEBUG			
				Debug.Print("30 {0} {1}:{2} {3}",e.Message,p.IP,p.Port,p.URLtestPage);
				#endif
				}
			if(LastStatusHTTP!=p.StatusHTTP)p.LastStatusHTTPDateTime=DateTime.Now;
				p.GetStatusHTTP=false;			
			}		
		}	
	}
	//*********************************************************
	class TargetHTTPProxy
	//*********************************************************		
	{
	public HTTPProxy ParentProxy;//TargetHTTPProxy return ip:port
	public ArrayList Way = new ArrayList();//Arsenal Proxy	
	public void AddWay(string Host, int  Port,int Priority,string URLtestPage=null)
        {
		HTTPProxy P=new HTTPProxy(Host,Port,Priority,URLtestPage);
		Way.Add(P);
        }
	public void GetStatus() {foreach ( HTTPProxy obj in this.Way )obj.GetStatus();}
	public void SelectProxy()
		{		
		int Priority=2147483647;
		HTTPProxy CandidateParentProxy=null;
		foreach ( HTTPProxy obj in this.Way )
			{
			if (obj.StatusHTTP!=1) continue;
			if (obj.Priority<Priority)
				{
				Priority=obj.Priority;
				CandidateParentProxy=obj;
				}
				
			}
		if (CandidateParentProxy!=null)
			{
			this.ParentProxy=CandidateParentProxy;			
			#if DEBUG
			Debug.Print("Selected proxy {0}:{1}",ParentProxy.Host,ParentProxy.Port);
			#endif
			}
		}
	}
	struct RegistryNode
	{
		public RegistryKey RegKey ;//= Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\\");
		public string NodePath;
		//public List Parameter;
		 
	}
	//*********************************************************
	class Program
	//*********************************************************
	{
		static TargetHTTPProxy PROXY;
		#if DEBUG
		const int TimeOutFrom=10;
		const int TimeOutTo=30;
		const int maxRepeat=4;
		#else
		const int TimeOutFrom=30;
		const int TimeOutTo=300;
		const int maxRepeat=4;		 
		#endif
		
		public static void Main(string[] args)
		{			
			string ver=System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
			DateTime verDateTime = new DateTime(2000, 1, 1);
			verDateTime=verDateTime.AddDays(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Build);
			//Время неверное verDateTime=verDateTime.AddSeconds(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Revision+9*3600+1060);
			//После того, как такое изменение сделано, третья цифра, цифра сборки (build), будет равна числу дней начиная с 1 января 2000 года по местному времени. 
			//Четвертая цифра ревизии (revision) будет установлена в количество секунд от полуночи по местному времени.
			//Это очень удобно, поскольку версия будет автоматически увеличиваться каждый раз при перекомпилировании проекта.
			//Если нужно контролировать эти цифры вручную (например, при публикации официального релиза), в должны их установить в нужное значение, например 1.4.7.6.		
			Console.Title=" ProxyRouter "+ver+" "+verDateTime;
			Console.WriteLine("*****************************************************\n{0}\n*****************************************************\n",
			                  /*ApplicationDeployment.CurrentDeployment.CurrentVersion*/
			                 Console.Title
			                 );		
			Get_IE_Proxy();
					
			PROXY = new TargetHTTPProxy();
			PROXY.AddWay("10.80.4.144",14253,1,"http://192.168.56.1/");
			PROXY.AddWay("192.168.168.100",3128,4,"http://192.168.168.1");
			PROXY.AddWay("178.74.153.80",8080,2,"http://ya.ru");
			//PROXY.AddWay("172.19.11.5",9090,3,"http://ya.ru");
			
			ProxyControl PRG = new ProxyControl();
			PRG.Start();
			Random RND = new Random();			
			int stimeout=1000;
			int srepeat=1;
			while (true)
				{
				PROXY.GetStatus();
				Thread.Sleep(stimeout);
				PROXY.SelectProxy();
				if (PRG.ParentProxy!=PROXY.ParentProxy)
					PRG.Start(PROXY.ParentProxy);
				ShowStatus();						  
				//http://zennolab.com/discussion/threads/c-kod-proverki-proksi.15272/
							
				srepeat--;
				if (srepeat==0)
					{
					stimeout=RND.Next(TimeOutFrom,TimeOutTo)*1000;
					srepeat=(int)Math.Truncate(Convert.ToDouble((maxRepeat-1)/(TimeOutTo-TimeOutFrom)*(stimeout-TimeOutFrom)))+1;
					}
				}
			//-> Перехватывать выход
			PRG.Stop();
			}
		public static void ShowStatus()
		{
			Console.WriteLine("{0:HH:mm:ss}",DateTime.Now);
			if (PROXY.ParentProxy==null)
				Console.WriteLine("No parent proxy");
			else	
				Console.WriteLine("Parent proxy {0}:{1}",PROXY.ParentProxy.Host,PROXY.ParentProxy.Port);
			
			Console.WriteLine("Ways count {0}",PROXY.Way.Count);				
			int i=1;
			foreach ( HTTPProxy obj in PROXY.Way )
				{         		
				Console.WriteLine("{0}.{1}:{2}\t{6:hh:mm:ss} {3}\t{7:hh:mm:ss} {4}\t{5}",i,obj.IP,obj.Port,obj.StatusPing,obj.StatusHTTP,obj.Host,obj.LastStatusPingDateTime,obj.LastStatusHTTPDateTime);
				i++;
				}	
		}
		public static void Get_IE_Proxy()
		{
		RegistryKey RegKey = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\\");	
		if (RegKey==null)
			{
			#if DEBUG
			Debug.Print("ERR.No Reg found");
			#endif	
			}
		else
			{
			#if DEBUG
			Debug.Print("Read registry");
			#endif	
			/*[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Internet Settings]
			 "ProxyEnable"=dword:00000000
				"ProxyServer"=""
				"ProxyOverride"=""
			[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Internet Settings\Connections]
			"DefaultConnectionSettings"=hex:46,00,00,00,bb,00,00,00,09,00,00,00,0e,00,00,\	
			 
			 */
			RegistryValueKind rvk = RegKey.GetValueKind("ProxyEnable");
				if (rvk==RegistryValueKind.DWord)
				{
					//https://msdn.microsoft.com/ru-ru/library/microsoft.win32.registry.getvalue(v=vs.110).aspx
					int PE =	(int) RegKey.GetValue("ProxyEnable");
				#if DEBUG
				Debug.Print("\"ProxyEnable\"=dword:{0}",PE);
				#endif		
				}
				else
				{
					
				}
			}
		}
		
	}
}




/*
 *******************************
 
            MY FAQ

 *******************************
 
 Добавить файлы в EXE 
 https://habrahabr.ru/post/85480/
 
 Реестр
 Класс Registry  https://msdn.microsoft.com/ru-ru/library/microsoft.win32.registry(v=vs.110).aspx
 Формат reg файла https://support.microsoft.com/ru-kz/help/310516/how-to-add--modify--or-delete-registry-subkeys-and-values-by-using-a 
https://metanit.com/sharp/tutorial/20.3.php
  
 */