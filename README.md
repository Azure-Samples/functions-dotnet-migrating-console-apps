---
services: functions
platforms: dotnet
author: muralivp
---
# Migrating Console Apps To Azure Functions 
Azure functions is a new offering from Microsoft that allows you to create serverless "compute on demand" applications. This is a generic function that can be used to convert **any console application** to an HTTP **webservice**.

# Getting Started - Creating a new Function App
1. Login to - [Azure Portal](https://portal.azure.com)
2. Create a function app by specifying an App Name and storage account

<img src="https://github.com/Azure-Samples/functions-dotnet-migrating-console-apps/blob/master/CreateFunctionApp.PNG?raw=true" alt="Create a Function App"></img> 

3. Go to the function code editor and Create a New Function

<img src="https://github.com/Azure-Samples/functions-dotnet-migrating-console-apps/blob/master/CreateNewFunction.PNG?raw=true" alt="Creating a New Function"></img> 

4. Select **HTTPTrigger - C#** and name your function **ConsoleAppToFunction** with the right **Authorization Level**

<img src="https://github.com/Azure-Samples/functions-dotnet-migrating-console-apps/blob/master/HttpTriggerWithAuthz.PNG?raw=true" alt="Create an Http Trigger"></img> 

# Adding Code

1. Select the run.csx file under **View files**

<img src="https://github.com/Azure-Samples/functions-dotnet-migrating-console-apps/blob/master/AddingCodeRunCsx.PNG?raw=true" alt="Adding code to run.csx"></img>

Replace the code in **Run.csx** with [ConsoleApp Function Code](https://github.com/Azure-Samples/functions-dotnet-migrating-console-apps/blob/master/code/run.csx?raw=true)

Since the code uses Json.Net, create a [Project.Json](https://github.com/Azure-Samples/functions-dotnet-migrating-console-apps/blob/master/code/Project.json?raw=true) file.

We have now created a generic function that can run any console application. The configuration of which console app to run is specified in a new **FunctionConfig.json** in the following example config we specify the function to run **FFMpeg.exe**
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

We needed a way to pass input files to our function. For this we define an **argument** of type **url**, where we expect the user to upload a file to **Onedrive** and provide the link in query string. 

Once these changes are done, the function should have the following files as shown below:

<img src="https://github.com/Azure-Samples/functions-dotnet-migrating-console-apps/blob/master/FinalFunction.PNG?raw=true" alt="Final configuration"></img>

#Interacting with the function
1. Upload all input files to **OneDrive**
2. Get a link to the file using the OneDrive **Share** menu - something like https://1drv.ms/v/<link-to-file>
3. Interact with the function providing the inputs as querystring something like
```
https://consoletofunctions.azurewebsites.net/api/ConsoleAppToFunctions?code=<function-authorization-key>&inputFile=<link-to-onedrive-file>
```
The function will process this request by invoking the specified console app and provide the output as a file download.
