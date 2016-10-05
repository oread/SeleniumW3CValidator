using OpenQA.Selenium.IE;
using OpenQA.Selenium.Remote;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text;
using System.Web;

namespace SeleniumW3CValidator
{
    class Program
    {
        /// <summary>
        /// Parameters 
        /// </summary>        
        private static string w3cMarkupValidatorUrl = @"https://validator.w3.org/nu/?showsource=yes&showoutline=yes&showimagereport=yes&doc={0}";
        private static string w3cCSSValidatorUrl = @"http://jigsaw.w3.org/css-validator/validator?warning=0&profile={0}&uri={1}";
        static string debug = "0";
        static string cssProfile = "css3";
        /// <summary>
        /// Private properties
        /// </summary>
        private static string baseURL = "https://google.com";
        private static string driverPath = AppDomain.CurrentDomain.BaseDirectory;
        private static string logFilePath = "";
        private static string logFolderPath = AppDomain.CurrentDomain.BaseDirectory;
        static InternetExplorerDriver remoteWebDriver = null;
        private static List<string> urlList = new List<string>();
        private static Queue<string> urlRunningList = new Queue<string>();
      
        static void Main(string[] args)
        {
            //ChromeOptions chOptions = new ChromeOptions();
            //chOptions.AddArgument("start-maximized");
            InternetExplorerOptions ieOptions = new InternetExplorerOptions();
            remoteWebDriver = new OpenQA.Selenium.IE.InternetExplorerDriver (driverPath);
            remoteWebDriver.Manage().Window.Maximize();
            string url = baseURL;
            if (args.Length > 0)
                url = args[0];            
            if (args.Length > 1)
                debug = args[1];
            if (args.Length > 2)
                cssProfile = args[2];

            foreach (string site in url.Split(','))
            {
                logFolderPath = AppDomain.CurrentDomain.BaseDirectory + new Uri(site).Host.Replace('.', '_') + DateTime.Now.ToString("yyyMMddhhmmss") + "\\";
                baseURL = site.ToLower();                
                logFilePath = logFolderPath +  "result.csv";
                Directory.CreateDirectory(logFolderPath);
                File.Create(logFilePath).Close();
                crawlPage(baseURL);
            }
        }
        /// <summary>
        /// Open and get all URL in a site
        /// </summary>
        /// <param name="url">URL need to open</param>
        static void crawlPage(string url)
        {
            try
            {

                urlList.Add(url);
                HttpWebResponse response = getHTTPStatusCode(url);                
                remoteWebDriver.Navigate().GoToUrl(url);                
                var uri = new Uri(url);
                if (!uri.IsFile && !remoteWebDriver.FileDetector.IsFile(url))
                {
                    //var width = chrdriver.ExecuteScript("return Math.max(document.body.scrollWidth, document.body.offsetWidth, document.documentElement.clientWidth, document.documentElement.scrollWidth, document.documentElement.offsetWidth);");
                    //var height = chrdriver.ExecuteScript("return Math.max(document.body.scrollHeight, document.body.offsetHeight, document.documentElement.clientHeight, document.documentElement.scrollHeight, document.documentElement.offsetHeight);");
                    //chrdriver.Manage().Window.Size = new System.Drawing.Size(int.Parse(width.ToString()) + 100, int.Parse(height.ToString()) + 100);
                    remoteWebDriver.Manage().Window.Maximize();
                    remoteWebDriver.GetScreenshot().SaveAsFile(logFolderPath + uri.PathAndQuery.Replace('/','_') + ".png", ImageFormat.Png);
                }
                string currentURL = remoteWebDriver.Url;

                //Find all a tags in webpage
                var atags = remoteWebDriver.FindElementsByTagName("a");
                foreach (RemoteWebElement e in atags)
                {
                    try
                    {
                        string eurl = e.GetAttribute("href").ToLower();
                        if (isValidUrl(currentURL,eurl))
                        {
                            if (!urlRunningList.Contains(eurl))
                                urlRunningList.Enqueue(eurl);
                            if (!urlList.Contains(eurl))
                                urlList.Add(eurl);
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
                //Find all a tags in webpage
                var linkTags = remoteWebDriver.FindElementsByTagName("link");
                List<string> linkUrlList = new List<string>();
                foreach (RemoteWebElement l in linkTags)
                {
                    try
                    {
                        string lurl = l.GetAttribute("href").ToLower();
                        if ((isValidUrl("", lurl)) && (l.GetAttribute("rel") == "stylesheet" 
                            || l.GetAttribute("type").ToLower() == "text/css" || lurl.Contains(".css")))
                        {
                            linkUrlList.Add(lurl);
                          
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
                w3cCSSValidate(linkUrlList);
                w3cHtmlValidate(url);
                //var searchBox = chrdriver.FindElementsById("lst-ib");
                //searchBox[0].SendKeys("Nguyen Thanh Son");
                //searchBox[0].Submit();      
                if (response!=null)
                   File.AppendAllText(logFilePath, string.Format("\"{0}\"\t\"{1}\"\t\"{2}\"\t\"{3}\"\t\"{4}\"\r\n", remoteWebDriver.Title, remoteWebDriver.Url, (int)response.StatusCode, response.StatusCode, response.ContentLength), Encoding.Unicode);
                else
                    File.AppendAllText(logFilePath, string.Format("\"{0}\"\t\"{1}\"\r\n", remoteWebDriver.Title, remoteWebDriver.Url), Encoding.Unicode);
                              
            }
            catch (Exception e)
            {
                while (e.InnerException != null)
                    e = e.InnerException;
                if(debug=="1" ||  debug == "2")
                    Console.WriteLine(url + " - CrawlUrl: " + e.ToString());
               
            }


            if(urlRunningList.Count > 0)
                crawlPage(urlRunningList.Dequeue());

        }
        /// <summary>
        /// Get Webresponse
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static HttpWebResponse getHTTPStatusCode(string url)
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            HttpWebResponse response = null;
            try
            {
                // Creates an HttpWebRequest for the specified URL. 
                HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                myHttpWebRequest.ServerCertificateValidationCallback += AcceptAllCertifications;                
                myHttpWebRequest.AllowAutoRedirect = false;
                // Sends the HttpWebRequest and waits for a response.
                HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                response =  myHttpWebResponse;
                myHttpWebResponse.Close();
                return response;
            }
            catch (Exception e)
            {
                while (e.InnerException != null)
                    e = e.InnerException;
                if (debug == "1")
                    Console.WriteLine(url + " - GetHTTPStatusCode: " + e.ToString());
                return null;
            }
        }
        /// <summary>
        /// using w3c to check css file
        /// </summary>
        /// <param name="urls"></param>
        /// <returns></returns>
        private static bool w3cCSSValidate(List<string> urls)
        {
            bool result = true;
            foreach (string u in urls)
            {
                try
                {
                    remoteWebDriver.Navigate().GoToUrl(string.Format(w3cCSSValidatorUrl, cssProfile, HttpUtility.UrlEncode(u)));
                    if (remoteWebDriver.FindElementById("errors") != null)
                    {
                        File.WriteAllText(logFolderPath + new Uri(u).PathAndQuery.Replace('/', '_') + ".html", remoteWebDriver.PageSource, Encoding.Unicode);
                        result = false;
                    }
                }
                catch
                { continue; }
            }
            return result;
        }
        /// <summary>
        /// Using w3c service to check html
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static bool w3cHtmlValidate(string url)
        {
            try
            {
                remoteWebDriver.Navigate().GoToUrl(string.Format(w3cMarkupValidatorUrl, HttpUtility.UrlEncode(url)));
                if (remoteWebDriver.FindElementByCssSelector("li[class=error]") != null)
                {
                    File.WriteAllText(logFolderPath + new Uri(url).PathAndQuery.Replace('/', '_') + ".html", remoteWebDriver.PageSource,Encoding.Unicode);
                    return false;
                }
                return true;
            }
            catch
            { return true; }
        }
        /// <summary>
        /// check if the url is valid
        /// </summary>
        /// <param name="lastNavigateUrl"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        private static bool isValidUrl(string lastNavigateUrl, string url)
        {
            if (!string.IsNullOrEmpty(url)
                            && lastNavigateUrl != url
                            && !url.Contains("#")
                            && url.StartsWith(baseURL)
                            && !urlList.Contains(url)
                            )
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// To Accept unvalid Server Certificate
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certification"></param>
        /// <param name="chain"></param>
        /// <param name="sslPolicyErrors"></param>
        /// <returns></returns>
        public static bool AcceptAllCertifications(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certification, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
        
    }
}
