---
services: functions
platforms: dotnet
author: muralivp
---
Best viewed in [GitHub](https://github.com/Azure-Samples/functions-dotnet-migrating-console-apps/blob/master/README.md) 
# Run Console Apps on Azure Functions 
tldr: 
Here is an easy way of getting any .exe running as a web service.  You just specify the input parameters to your exe in a configuration file.  You can use binary files as inputs to the exe by specifying a URL to download it from.

More details:
This sample is a generic function (.csx file) that can be used to convert **any console application** to an HTTP **webservice** in Azure Functions.  All you have to do is edit a configuration file and specify what input parameters will be passed as arguments to the .exe.

Azure functions is an offering from Microsoft that allows you to create serverless "compute on demand" applications. 

# Features
- You can use binary files as input (specify the URL and the binary file will be downloaded to a temporary location on the virtual environment hosting your Azure Function)
- You choose which file is sent back to the user as output
- Here's what the configuration file looks like (more details later)

```
{
    "name": "consoleAppToFunctions",
    "input": {
        "command": "ffmpeg.exe -i {inputFile} {output1}",
        "arguments": {
            "inputFile": {
                "url": "https://1drv.ms/v/<link-to-file>"
                //binary file input
            },
            "output1": {
                "localfile": "out.mp3"
                //stored in a temp folder
            }
        }
    },
    "output": {
        "folder": "outputFolder",
        "binaryFile": {
            "returnFile": "*.mp3",
            "returnFileName": "yourFile.mp3"
        }
    }
}
```

# Getting Started - Creating a new Function App
1. Login to - [Azure Portal](https://portal.azure.com)
2. Create a function app by specifying an App Name and storage account

<img src="https://github.com/Azure-Samples/functions-dotnet-migrating-console-apps/blob/master/CreateFunctionApp.PNG?raw=true" alt="Create a Function App"></img> 

3. Go to the function code editor and Create a New Function

<img src="https://github.com/Azure-Samples/functions-dotnet-migrating-console-apps/blob/master/CreateNewFunction.PNG?raw=true" alt="Creating a New Function"></img> 

4. Select **HTTPTrigger - C#** and name your function **ConsoleAppToFunction** with the right **Authorization Level**

<img src="https://github.com/Azure-Samples/functions-dotnet-migrating-console-apps/blob/master/HttpTriggerWithAuthz.PNG?raw=true" alt="Create an Http Trigger"></img> 

# Adding Code
If you don't care about the details, just go to your function's "Your Files > Upload Files" (top right) and upload [the 3 files in the code sample](https://github.com/Azure-Samples/functions-dotnet-migrating-console-apps/tree/master/code) along with the .exe (and .dll's it requires)
--
The following are steps that you should follow if you want to fimiliarinze yourself with the using Azure functions interface.

1. Select the run.csx file under **View files**

<img src="https://github.com/Azure-Samples/functions-dotnet-migrating-console-apps/blob/master/AddingCodeRunCsx.PNG?raw=true" alt="Adding code to run.csx"></img>

2. Replace the code in **Run.csx** with [ConsoleApp Function Code](https://github.com/Azure-Samples/functions-dotnet-migrating-console-apps/blob/master/code/run.csx?raw=true)

3. Since the code uses Json.Net, create a [Project.Json](https://github.com/Azure-Samples/functions-dotnet-migrating-console-apps/blob/master/code/Project.json?raw=true) file.

# Configuring the Sample
We have now created a generic function that can run any console application. You can specify which console app to run in a file called **FunctionConfig.json** in the following example config we specify the function to run **FFMpeg.exe**

```
{
    "name": "consoleAppToFunctions",
    "input": {
        "command": "ffmpeg.exe -i {inputFile} {output1}",
        "arguments": {
            "inputFile": {
                "url": "https://1drv.ms/v/<link-to-file>"
            },
            "output1": {
                "localfile": "out.mp3"
            }
        }
    },
    "output": {
        "folder": "outputFolder",
        "binaryFile": {
            "returnFile": "*.mp3",
            "returnFileName": "yourFile.mp3"
        }
    }
}
```

We needed a way to pass binary input files to our function. For this we define an **argument** of type **url**, where we expect the user to upload a file to **Onedrive** (or whatever service that hosts a binary file) and provide the link in the query string. 

Once these changes are done, the function should have the following files as shown below:

<img src="https://github.com/Azure-Samples/functions-dotnet-migrating-console-apps/blob/master/FinalFunction.PNG?raw=true" alt="Final configuration"></img>

# Interacting with the function
1. Upload your test input file(s) to **OneDrive**
2. Get a link to the file using the OneDrive **Share** menu - something like https://1drv.ms/v/<link-to-file>
3. Interact with the function providing the inputs as querystring. Note that `inputFile` here is defined in the **FunctionConfig.json** file above

Example URL:

```
https://[the URL when you click 'Get Function URL']&inputFile=<link-to-onedrive-file>
```

The function will process this request by invoking the specified console app and provide the output as a file download.
