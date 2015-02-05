using System;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Diagnostics;
using TagLib;

namespace SoundcloudDownload
{
	class MainClass
	{
		static ManualResetEvent m_mreReady;
		static Stopwatch sw = new Stopwatch(); 

		public static void Main (string[] args)
		{

			bool bolNextisUrl = false;
			
			Console.WriteLine ("Soundcloud Grabber 0.1 Aplha - by Frederic Bultmann");
			Console.WriteLine ("----------------------------------------------------");
			Console.WriteLine ("");

			//Check for Arguments an do the main work

			//DEBUG
			parseUrl ("https://soundcloud.com/digital-zen/tokimonsta-ft-gavin-turek-9");


			/*foreach (string strArg in args) 
			{
				if (bolNextisUrl == true) {
					parseUrl(strArg);
					break;
				}
						
				switch (strArg.ToString()) {
				case "-?":
				case "--help": 
					Console.WriteLine ("This is the help output!") ;
					break;
				case "-url":
					bolNextisUrl = true;
					break;
				default:
					break;
				}
			}*/
		

		}



		/// <summary>
		/// Parses the URL.
		/// </summary>
		/// <param name="strURL">String URL.</param>
		public static void parseUrl(string strURL) 
		{
			HttpWebResponse webResponse = null;

			string strArtistName = "";
			string strTrackName = "";
			string strImage = "";

			Console.WriteLine ("Doin' the magic...\n");

			//Check URL
			if (strURL.ToLower().StartsWith("www.")) {
				strURL = "http://" + strURL;
			}

			//Check if online and get some Infos
			if (checkURL(strURL)) 
			{

				//Soundcloud URL
				Console.WriteLine ("Track URL: " + strURL); 

				// Creates an HttpWebRequest for the specified URL. 
				HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(@strURL); 
				// Set User Agent
				myHttpWebRequest.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";

				try
				{
					String strHTML;

					webResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
					using (var reader = new StreamReader(webResponse.GetResponseStream()))
					{
						//Get HTML Code
						strHTML = reader.ReadToEnd();
					}
					//Split HTML

					//##############
					//Get Artistname, Trackname, ImageLink - RegEx -> Get Characters between < > 
					Regex linkParser = new Regex("(?<=<)(?s)(.*?)(?=s*>)", RegexOptions.IgnoreCase);

					foreach(Match i in linkParser.Matches(strHTML)) {
						//Get Artistname 
						if (i.Value.IndexOf("twitter:audio:artist_name") > 0)
						{
							//RegEx -> Get Characters between content=" " 
							linkParser = new Regex("(?<=content=\")(?s)(.*?)(?=s*\")", RegexOptions.IgnoreCase);

							foreach(Match j in linkParser.Matches(i.Value)) 
							{
								//DEBUG
								Console.WriteLine ("\nArtist: " + WebUtility.HtmlDecode(j.Value));

								strArtistName = WebUtility.HtmlDecode(j.Value);
								break;
						    }

					    }

						//Get Trackname
						if (i.Value.IndexOf("og:title") > 0)
						{
							//RegEx -> Get Characters between content=" " 
							linkParser = new Regex("(?<=content=\")(?s)(.*?)(?=s*\")", RegexOptions.IgnoreCase);

							foreach(Match j in linkParser.Matches(i.Value)) 
							{
								//DEBUG
								Console.WriteLine ("Track: " + WebUtility.HtmlDecode(j.Value));

								strTrackName = WebUtility.HtmlDecode(j.Value);
								break;
							}

						}
						//Get Imagelink
						if (i.Value.IndexOf("og:image") > 0)
						{
							//RegEx -> Get Characters between content=" " 
							linkParser = new Regex("(?<=content=\")(?s)(.*?)(?=s*\")", RegexOptions.IgnoreCase);

							foreach(Match j in linkParser.Matches(i.Value)) 
							{
								//DEBUG
								Console.WriteLine ("ImageLink: " + j.Value );

								strImage = j.Value;
								break;
							}

						}
					}

					//##############
					//Get Link for "Download Page" - RegEx -> Get every URL with https
					linkParser = new Regex("https://([\\w+?\\.\\w+])+([a-zA-Z0-9\\~\\!\\@\\#\\$\\%\\^\\&amp;\\*\\(\\)_\\-\\=\\+\\\\\\/\\?\\.\\:\\;\\'\\,]*)?", RegexOptions.IgnoreCase);

					foreach(Match m in linkParser.Matches(strHTML)) {
						if (m.Value.EndsWith ("/vmap") ) 
						{
						 	//Downloadseite auf Link überprüfen

							//Neuen Link zum auslesen festlegen
							strURL = m.Value;

							myHttpWebRequest = (HttpWebRequest)WebRequest.Create(@strURL);  
							// Set User Agent
							myHttpWebRequest.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";

							//DEBUG -> "Link Page"
							//Console.WriteLine (m.Value ) ;

							webResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
							using (var reader = new StreamReader(webResponse.GetResponseStream()))
							{
								//Get HTML Code
								strHTML = reader.ReadToEnd();
							}

							//Split HTML  - RegEx -> Ger every URL with http
							linkParser = new Regex("http://([\\w+?\\.\\w+])+([a-zA-Z0-9\\~\\!\\@\\#\\$\\%\\^\\&amp;\\*\\(\\)_\\-\\=\\+\\\\\\/\\?\\.\\:\\;\\'\\,]*)?", RegexOptions.IgnoreCase);

							foreach(Match n in linkParser.Matches(strHTML)) {
								if (n.Value.IndexOf(".mp3") >= 0 ) 
								{
									//Playklist URL
									if (n.Value.IndexOf(".m3u") >= 0 ) 
									{
										//DEBUG
										//Console.WriteLine ("\nPlaylist: " + n.Value ) ;
									}
									//MP3 URL
									else
									{
										//DEBUG
										//Console.WriteLine ("\nMP3 Download: " + n.Value ) ;
										//Style Purposes
										Console.WriteLine ("\n");
										Console.WriteLine("Status: Downloading Track...");

										String strTrackPath, strCoverPath;

										string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());

										foreach (char c in invalid)
										{
											strTrackName = strTrackName.Replace(c.ToString(), ""); 
											strArtistName = strArtistName.Replace(c.ToString(), ""); 
										}


										strTrackPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),strArtistName + " - " + strTrackName + ".mp3");
										strCoverPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),strArtistName + " - " + strTrackName + ".jpg");

										//DownloadTrack
										using (WebClient cliThis = new WebClient())
										{
											// bind the events
											cliThis.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(cliThis_DownloadFileCompleted);
											cliThis.DownloadProgressChanged += new DownloadProgressChangedEventHandler(cliThis_DownloadProgressChanged);
											// start the download-progress

											string strTarget = strTrackPath;
											m_mreReady = new ManualResetEvent(false);

											// Start the stopwatch which we will be using to calculate the download speed
											sw.Start();

											cliThis.DownloadFileAsync(new Uri(n.Value), strTarget);
										}
										m_mreReady.WaitOne();

										Console.WriteLine ("\n");
										Console.WriteLine("Status: Downloading Coverart...");

										//Download CoverArt
										using (WebClient cliThis = new WebClient())
										{
											// bind the events
											cliThis.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(cliThis_DownloadFileCompleted);
											cliThis.DownloadProgressChanged += new DownloadProgressChangedEventHandler(cliThis_DownloadProgressChanged);
											// start the download-progress

											string strTarget = strCoverPath;
											m_mreReady = new ManualResetEvent(false);

											// Start the stopwatch which we will be using to calculate the download speed
											sw.Start();

											cliThis.DownloadFileAsync(new Uri(strImage), strTarget);
										}
										m_mreReady.WaitOne();

										Console.WriteLine ("\n");
										Console.WriteLine ("Status: Setting MP3 Tags and Coverart...\n");

										TagLib.File file = TagLib.File.Create(strTrackPath);

										string[] strArtists = new string[1] {strArtistName};

										file.Tag.Performers = strArtists;
										file.Tag.Title = strTrackName;

										IPicture [] pictures = new IPicture[1];
										Picture picture = new Picture(strCoverPath);
										pictures[0] = picture;

										file.Tag.Pictures = pictures;
										file.Save();

										Console.WriteLine ("Status: Almost done!");

									}										
									//Exit Loop
									//break;
								}
							}
							//Exit Loop
							break;
						} 

					}						
				}
				catch (WebException ex)
				{
					Console.WriteLine (ex); 
				}
				finally
				{
					if ((webResponse != null)) 
					{
						webResponse.Close ();
					}
				}
		 }
		}

		static void cliThis_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
		{
			//Console.SetCursorPosition(0, 1);
			Console.Write("\rStatus: {4} {0} MB/{1}MB -> {2}% @ {3} Kb/s", (e.BytesReceived / 1024d / 1024d).ToString("0.00"),
				(e.TotalBytesToReceive / 1024d / 1024d).ToString("0.00"), e.ProgressPercentage,(e.BytesReceived / 1024d / sw.Elapsed.TotalSeconds).ToString("0.00"),
				getProgress(e.ProgressPercentage));

		}

		static void cliThis_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		{
			//Reset Stopwatch
			sw.Reset ();

			m_mreReady.Set();
		}

		/// <summary>
		/// Returns the Progress Bar
		/// </summary>
		/// <returns>String with Progress Bar</returns>
		/// <param name="intProgress">Progress in Percent</param>
		static string getProgress(int intProgress)
		{
			string strBuffer;

			strBuffer = "|";

			decimal decPerc = (decimal)intProgress / (decimal)100;
			int chars = (int)Math.Floor(decPerc / ((decimal)1 / (decimal)20));
			string strProgress = String.Empty, strBack = String.Empty;

			for (int i = 0; i < chars; i++) strProgress += "#";
			for (int i = 0; i < 20 - chars; i++) strBack += "-";

			strBuffer += strProgress + strBack;

			strBuffer += "|";
			return strBuffer;
		}

		/// <summary>
		/// Checks the URL for validity.
		/// </summary>
		/// <returns><c>true</c>, if URL is valid, <c>false</c> otherwise.</returns>
		/// <param name="strURL">URL to check</param>
		static bool checkURL(string strURL)
		{
			HttpWebResponse webResponse = null;

			try {
				HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(@strURL);
				webRequest.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";
				webResponse = (HttpWebResponse)webRequest.GetResponse();
				return true;
			} 
			catch(Exception ex) {
				Console.WriteLine (ex); 
				return false;
			} 
			finally {
				if ((webResponse != null)) 
				{
					webResponse.Close ();
				}
			}
		}














	}


}