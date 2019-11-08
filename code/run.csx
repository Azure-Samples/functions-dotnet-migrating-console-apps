#r "Newtonsoft.Json" 
using System.Net;
using Newtonsoft.Json;
using System.IO;
using System.Diagnostics;

//Code from https://github.com/Azure-Samples/functions-dotnet-migrating-console-apps/edit/master/code/run.csx
//Written by Ambrose http://github.com/efficientHacks and Murali http://github.com/muralivp

public class ExeArg
{
    public string Name { get; set; }
    public string Type { get; set; }
    public string Value { get; set; }
}

//NOTE: Must use runtime v1 to use HttpRequestMessage / for this example to still work in Azure Functions
//Similarly, TraceWriter should not be used in v2.
public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info("C# HTTP trigger function processed a request.");

    string localPath = req.RequestUri.LocalPath;
    string functionName = localPath.Substring(localPath.LastIndexOf('/')+1);

    var json = File.ReadAllText(string.Format(@"D:\home\site\wwwroot\{0}\FunctionConfig.json",functionName));

    var config = JsonConvert.DeserializeObject<dynamic>(json);

    var functionArguments = config.input.arguments;
    var localOutputFolder = Path.Combine(@"d:\home\data", config.output.folder.Value, Path.GetFileNameWithoutExtension(Path.GetTempFileName()));
    Directory.CreateDirectory(localOutputFolder);
    log.Info("directory Created=" + localOutputFolder);
    var workingDirectory = Path.Combine(@"d:\home\site\wwwroot", functionName + "");
    Directory.SetCurrentDirectory(workingDirectory);//fun fact - the default working directory is d:\windows\system32

    var command = config.input.command.Value;

    var argList = new List<ExeArg>();

    //Parse the config file's arguments
    //handle file system, local file etc. and construct the input params for the actual calling of the .exe
    foreach (var arg in functionArguments)
    {
        var exeArg = new ExeArg();
        exeArg.Name = arg.Name;
        var value = (Newtonsoft.Json.Linq.JObject)arg.Value;
        var property = (Newtonsoft.Json.Linq.JProperty)value.First;
        exeArg.Type = property.Name;
        exeArg.Value = property.Value.ToString();

        var valueFromQueryString = await getValueFromQuery(req, exeArg.Name);

        log.Info("valueFromQueryString name=" + exeArg.Name);
        log.Info("valueFromQueryString val=" + valueFromQueryString);
        if(!string.IsNullOrEmpty(valueFromQueryString))
        {
            exeArg.Value = valueFromQueryString;
            log.Info(exeArg.Name + " " + valueFromQueryString);
        }

        if(exeArg.Type.ToLower() == "localfile" || exeArg.Type.ToLower() == "localfolder")
        {
            exeArg.Value = Path.Combine(localOutputFolder, exeArg.Value);
            exeArg.Type = "string";
        }
        if(string.IsNullOrEmpty(exeArg.Value))
        {
            //throw exception here
        }
        argList.Add(exeArg);
    }
    //call the exe
    Dictionary<string, string> paramList = ProcessParameters(argList, localOutputFolder, log);
    foreach (string parameter in paramList.Keys)
    {
        command = command.Replace(parameter, paramList[parameter]);
    }

    string commandName = command.Split(' ')[0];
    string arguments = command.Split(new char[] { ' ' }, 2)[1];
    log.Info("the command is " + command);
    log.Info("the working dir is " + workingDirectory);
    Process p = new Process();
    p.StartInfo.UseShellExecute = false;
    p.StartInfo.RedirectStandardOutput = true;
    p.StartInfo.FileName = commandName; 
    p.StartInfo.Arguments = arguments;
    p.Start();
    string output = p.StandardOutput.ReadToEnd();
    p.WaitForExit();
    File.WriteAllText(localOutputFolder+"\\out.txt",output);

    //handle return file
    log.Info("handling return file localOutputFolder=" + localOutputFolder);
    string outputFile = config.output.binaryFile.returnFile.Value;
    string outputFileName = config.output.binaryFile.returnFileName.Value;
    var path = Directory.GetFiles(localOutputFolder, outputFile)[0];
    
    log.Info("returning this file " + path);
    var result = new FileHttpResponseMessage(localOutputFolder);
    var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
    result.Content = new StreamContent(stream);
    //Replace 'application/octet-stream' with, for example, audio/mpeg if you want to return a mp3 file rather than
    //raw binary, or video/mp4 to return a mp4 file
    result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
    result.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
    {
        FileName = outputFileName
    };

    return result;
}

private static Dictionary<string, string> ProcessParameters(List<ExeArg> arguments, string outputFolder, TraceWriter log)
{
    Dictionary<string, string> paramList = new Dictionary<string, string>();
    foreach (var arg in arguments)
    {
        switch (arg.Type)
        {
            case "url":
                log.Info("arg Value " + (string)arg.Value);    
                string downloadedFileName = ProcessUrlType((string)arg.Value, outputFolder, log);
                paramList.Add("{" + arg.Name + "}", downloadedFileName);
                break;
            case "string":
                paramList.Add("{" + arg.Name + "}", arg.Value.ToString());
                break;
            default:
                break;
        }
    }
    log.Info("paramList " + paramList);
    return paramList;
}

//you can modify this method to handle different URLs if necessary
//NOTE: IS Modified from original github example to handle non-OneDrive / 'normal' URLs
private static string ProcessUrlType(string url, string outputFolder, TraceWriter log)
{
    string downloadedFile = Path.Combine(outputFolder, Path.GetFileName(Path.GetTempFileName()));
    log.Info("downloadedFile " + downloadedFile);
    
    HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(url);
    webRequest.AllowAutoRedirect = false;

    WebResponse webResp = webRequest.GetResponse();
    log.Info("webResp Headers" + webResp.Headers);

    WebClient webClient = new WebClient();
    log.Info("url downloading " + url);
    webClient.DownloadFile(url, downloadedFile);
    
    return downloadedFile;
}

private async static Task<string> getValueFromQuery(HttpRequestMessage req, string name)
{
    // parse query parameter
    string value = req.GetQueryNameValuePairs()
        .FirstOrDefault(q => string.Compare(q.Key, name, true) == 0)
        .Value;
    
    //if not found in query string, look for it in the body (json)
    // Get request body
    dynamic data = await req.Content.ReadAsAsync<object>();

    // Set name to query string or body data
    value = value ?? data?[name];
    return value;
}

//this is to delete the folder after the response
//thanks to: https://stackoverflow.com/a/30522890/2205372
public class FileHttpResponseMessage : HttpResponseMessage
{
    private string filePath;

    public FileHttpResponseMessage(string filePath)
    {
        this.filePath = filePath;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Content.Dispose();
        Directory.Delete(filePath,true);
    }
}
