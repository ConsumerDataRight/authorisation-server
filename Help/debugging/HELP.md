# Getting Started
To get started, clone the source code from the GitHub repositories by following the steps below:

1. Create a folder called CDR.
2. Navigate to this folder.
3. Clone the repo as a subfolder of this folder using the following command:
```
git clone https://github.com/ConsumerDataRight/authorisation-server.git
```
4. Install the required certificates. See certificate details [here](../../CertificateManagement/README.md "Certificate Management").  
5. Start the projects in the solution. This can be done in multiple ways. This guide explains how to do this using .Net command line and using MS Visual Studio.

## .Net command line

1. Download and install the free [MS Windows Terminal](https://docs.microsoft.com/en-us/windows/terminal/get-started "Download the free Windows Terminal here").
2. Use the [Start-Auth-Server](../../Source/Start-Auth-Server.bat "Use the Start-Auth-Server.bat .Net CLI batch file here") batch file to build and run the required projects to start the Mock Data Holder.


[<img src="./images/DotNet-CLI-Running.png"  width='600' alt="Start projects from .Net CLI"/>](./images/DotNet-CLI-Running.png)

This will create the LocalDB instance by default and seed the database with the supplied sample data.

LocalDB is installed as part of MS Visual Studio. If using MS VSCode, the MS SQL extension will need to be installed.

You can connect to the database from MS Visual Studio using the SQL Explorer, or from MS SQL Server Management Studio (SSMS) using the following settings:
```
Server type: Database Engine  
Server name: (LocalDB)\\MSSQLLocalDB  
Authentication: Windows Authentication  
```
## MS Visual Studio

### Start the Authorisation Server

There are two Visual Studio solution files that are available for use:
- CdrAuthServer.sln - This is the default solution file that is used primarily for running and debugging the Authorisation Server projects.
- CdrAuthServer_Shared.sln - In addition to the Authorisation Server projects, this solution file also opens the [Mock Solutions Test Automation](https://github.com/ConsumerDataRight/mock-solution-test-automation) project. This is useful when wanting to debug or step through source code used in Mock Solution Test Automation project. Further information can be found in [Authorisation Server Test Automation Execution Guide](../testing/HELP.md)

To launch the application using MS Visual Studio, the following projects need to be started:
```
CdrAuthServer 
CdrAuthServer.mTLS.Gateway
CdrAuthServer.TLS.Gateway
```
	
1. Navigate to the solution properties and select a "Start" action for the required projects.

[<img src="./images/MS-Visual-Studio-Select-projects.png" width='600' alt="Project selected to be started"/>](./images/MS-Visual-Studio-Select-projects.png)

2. Click "Start" to start the Authorisation Server solution.

[<img src="./images/MS-Visual-Studio-Start-No-Debug.png" width='600' alt="Start the project"/>](./images/MS-Visual-Studio-Start-No-Debug.png)

3. Output windows will be launched for each of the projects set to start. \
   These will show the logging messages as sent to the console in each of the running projects. E.g.

[<img src="./images/MS-Visual-Studio-Running.png" width='600' alt="Project running"/>](./images/MS-Visual-Studio-Running.png)

4. To run the solution in debug mode, simply follow the steps outlined above and click on the "Start" \
   button as shown in the image below: 

[<img src="./images/MS-Visual-Studio-Start.png" width='600' alt="Start the project"/>](./images/MS-Visual-Studio-Start.png)