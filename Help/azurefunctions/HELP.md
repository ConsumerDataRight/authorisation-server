<h2>Azure Functions</h2>
<div style="margin-left:18px;">
The Authorisation Server solution contains an azure function project.<br />
The GetDataRecipients function is used to get the list of Data Recipients<br />
from the Mock Register and update the Authorisation Server repository.<br />
</div>

<h2>To Run and Debug Azure Functions</h2>
<div style="margin-left:18px;">
	The following procedures can be used to run the functions in a local development environment for evaluation of the functions.
<br />

<div style="margin-top:6px;margin-bottom:6px;">
1) Start the <a href="https://github.com/ConsumerDataRight/mock-register " title="Mock Register" alt="Mock Register">Mock Register</a> and the Authorisation Server solutions.
</div>

<div style="margin-top:6px;">
2) Start the Azure Storage Emulator (Azurite):
</div>
<div style="margin-left:18px;margin-bottom:6px;">
	using a MS Windows command prompt:<br />
</div>

```
md C:\azurite
cd "C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\Extensions\Microsoft\Azure Storage Emulator"
azurite --silent --location c:\azurite --debug c:\azurite\debug.log
```

<div style="margin-left:18px;">
	Noting this is only required to be performed once, it will then be listening on ports - 10000, 10001 and 10002<br />
	when debugging is started from MS Visual Studio by selecting CdrAuthServer.GetDataRecipients as the startup project<br />
	(by starting a debug instance using F5 or Debug > Start Debugging)
	<br />
</div>
<div style="margin-left:18px;margin-bottom:6px;">
	or by using a MS Windows command prompt:<br />
</div>

```
navigate to .\authorisation-server\Source\CdrAuthServer.GetDataRecipients
func host start --verbose
```

<p>3) Open the Authorisation Server in MS Visual Studio, select CdrAuthServer.GetDataRecipients as the startup project.</p>

<p>4) Start each debug instances (F5 or Debug > Start Debugging), this will simulate the discovery of Data Recipients and the</p>
<div style="margin-left:18px;margin-top:-12px;">
	updating of the data in the Authorisation Server repositories.
</div>

<div style="margin-left:18px;margin-top:12px;margin-bottom:6px;">
	Noting the below sql scripts are used to observe the results.<br />
</div>

```
SELECT * FROM [cdr-auth-server].[dbo].[ClientClaims]
SELECT * FROM [cdr-auth-server].[dbo].[Clients]
SELECT * FROM [cdr-auth-server].[dbo].[SoftwareProducts]
SELECT * FROM [cdr-auth-server].[dbo].[LogEvents-DrService]
```

<h2>To Build Azure Functions</h2>
<div style="margin-left:18px;">
	dotnet SDK 8.0.10x or higher is required. Latest SDK can be found from the link https://microsoft.com/net
<br />