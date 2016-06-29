using System.Net;
using System;
using System.Web;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
namespace wpad
{
	partial class MainForm
	{
		HttpListener HTTPserver;
		bool flag = true;
		/// <summary>
		/// Designer variable used to keep track of non-visual components.
		/// </summary>
		private System.ComponentModel.IContainer components = null;
		
		/// <summary>
		/// Disposes resources used by the form.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				if (components != null) {
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}
		
		/// <summary>
		/// This method is required for Windows Forms designer support.
		/// Do not change the method contents inside the source code editor. The Forms designer might
		/// not be able to load this method if it was changed manually.
		/// </summary>
		private void InitializeComponent()
		{
			this.listBox1 = new System.Windows.Forms.ListBox();
			this.SuspendLayout();
			// 
			// listBox1
			// 
			this.listBox1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.listBox1.FormattingEnabled = true;
			this.listBox1.Location = new System.Drawing.Point(0, 0);
			this.listBox1.Name = "listBox1";
			this.listBox1.Size = new System.Drawing.Size(534, 460);
			this.listBox1.TabIndex = 0;
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(534, 460);
			this.Controls.Add(this.listBox1);
			this.Name = "MainForm";
			this.Text = "wpad";
			this.Load += new System.EventHandler(this.MainFormLoad);
			this.ResumeLayout(false);
		}
		private System.Windows.Forms.ListBox listBox1;
		
		void MainFormLoad(object sender, System.EventArgs e)
		{
			listBox1.Items.Add("Start");
			listBox1.Items.Add("Start HTTP Server");
			string uri = @"http://10.80.4.220/wpad/";
        	StartServer(uri);
		}
        private void StartServer(string prefix)
	    {
	        HTTPserver = new HttpListener();
	
	        // текущая ос не поддерживается
	        if (!HttpListener.IsSupported) return;
	
	        //добавление префикса (say/)
	        //обязательно в конце должна быть косая черта
	        if (string.IsNullOrEmpty(prefix))
	           throw new ArgumentException("prefix");
	   
	        HTTPserver.Prefixes.Add(prefix);
	
	        //запускаем север
	        HTTPserver.Start();  
	 
	        this.Text = "Сервер запущен!";
	
	        //сервер запущен? Тогда слушаем входящие соединения
	        while (HTTPserver.IsListening)
	        {
	           //ожидаем входящие запросы
	           HttpListenerContext context = HTTPserver.GetContext();
	
	            //получаем входящий запрос
	            HttpListenerRequest request = context.Request;
	
	            //обрабатываем POST запрос
	
	            //запрос получен методом POST (пришли данные формы)
	            if (request.HttpMethod == "POST")
	            {
	                //показать, что пришло от клиента
	                ShowRequestData(request);
	
	                //завершаем работу сервера
	                if (!flag) return;
	            }
	 
	            //формируем ответ сервера:
	
	            //динамически создаём страницу
	            string responseString = @"<!DOCTYPE HTML>
	                    <html><head></head><body>
	                    <form method=""post"" action=""say"">
	                    <p><b>Name: </b><br>
	                    <input type=""text"" name=""myname"" size=""40""></p>
	                    <p><input type=""submit"" value=""send""></p>
	                    </form></body></html>";
	
	            //отправка данных клиенту
	            HttpListenerResponse response = context.Response;
	            response.ContentType = "text/html; charset=UTF-8";
	            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
	            response.ContentLength64 = buffer.Length;
	
	            using (Stream output = response.OutputStream)
	            {
	                output.Write(buffer, 0, buffer.Length);
	            }
	        }
	   } 
        
        
        //http://csharpprogramming.ru/web/kak-sozdat-veb-server-s-pomoshhyu-klassa-httplistener
        //https://habrahabr.ru/post/120157/
		private void ShowRequestData(HttpListenerRequest request)
	    {
	        //есть данные от клиента?
	        if (!request.HasEntityBody) return;
			
	        //смотрим, что пришло
	        using (Stream body = request.InputStream)
	        {
	            using (StreamReader reader = new StreamReader(body))
	            {
	               string text = reader.ReadToEnd();
	                
	               //оставляем только имя
	               text = text.Remove(0, 7);
	
	               //преобразуем %CC%E0%EA%F1 -> Макс
	               //text = System.Web.HttpUtility.UrlDecode(text, Encoding.UTF8);
						              
	               
	               //выводим имя
	               MessageBox.Show("Ваше имя: " + text);
	
	              flag = true;
	
	                //останавливаем сервер
	                if (text == "stop")
	                {
	                   HTTPserver.Stop();
	                   this.Text = "Сервер остановлен!";
	                   flag = false;
	                }
	             }
	          }
	       }
	   }        
	}

