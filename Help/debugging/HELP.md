<h2>To get started, clone the source code</h2>
<div style="margin-left:18px;">
1. Create a folder called CDR<br />
2. Navigate to this folder<br />
3. Clone the repo as a subfolder of this folder using the following command;<br />
<div style="margin-left:18px;">
git clone https://github.com/ConsumerDataRight/authorisation-server.git<br />
</div>
4. Install the required certificates. See certificate details <a href="../../CertificateManagement/README.md" title="Certificate Management" alt="Certificate Management - CertificateManagement/README.md"> here</a>.<br />
5. Start the projects in the solution, can be done in multiple ways, examples below are from .Net command line and using MS Visual Studio<br />
</div>

<h2>.Net command line</h2>
<div style="margin-left:18px;">
<p>1. Download and install the free <a href="https://docs.microsoft.com/en-us/windows/terminal/get-started" title="Download the free Windows Terminal here" alt="Download the free MS Windows Terminal here">MS Windows Terminal</a>
<br />
2. Use the <a href="../../Source/Start-Auth-Server.bat" title="Use the Start-Auth-Server .Net CLI batch file here" alt="Use the Start-Auth-Server .Net CLI batch file here">Start-Auth-Server</a> batch file to build and run the required projects to start the Mock Data Holder.
</p>

[<img src="./images/DotNet-CLI-Running.png"  width='600' alt="Start projects from .Net CLI"/>](./images/DotNet-CLI-Running.png)
<br />
This will create the LocalDB database by default and seed the database with the supplied sample data.
<p>LocalDB is installed as part of MS Visual Studio. If using MS VSCode, the MS SQL extension will need to be installed.</p>
<p>You can connect to the database from MS Visual Studio using the SQL Explorer, or from MS SQL Server Management Studio (SSMS) using
	the following settings; <br />
	Server type: Database Engine <br />
	Server name: (LocalDB)\MSSQLLocalDB <br />
	Authentication: Windows Authentication<br />
</p>
</div>

<h2>MS Visual Studio</h2>
<div style="margin-left:18px;">
<p>To launch the application using MS Visual Studio, the following projects need to be started:</p>
<p>	CdrAuthServer <br />
	CdrAuthServer.mTLS.Gateway <br />
	CdrAuthServer.TLS.Gateway<br />
</p>

<p>1. Navigate to the solution properties and select a "Start" action for the required projects.</p>

[<img src="./images/MS-Visual-Studio-Select-projects.png" width='600' alt="Project selected to be started"/>](./images/MS-Visual-Studio-Select-projects.png)
<br />
<p>2. Click "Start" to start the Authorisation Server solution.</p>

[<img src="./images/MS-Visual-Studio-Start-No-Debug.png" width='600' alt="Start the project"/>](./images/MS-Visual-Studio-Start-No-Debug.png)
<br />
Output windows will be launched for each of the projects set to start.   <br />
These will show the logging messages as sent to the console in each of the running projects. E.g.<br />
[<img src="./images/MS-Visual-Studio-Running.png" width='600' alt="Project running"/>](./images/MS-Visual-Studio-Running.png)
<br />
To run the solution in debug mode, simply follow the steps outlined above and click on the "Start" button as shown in the image below: <br />
[<img src="./images/MS-Visual-Studio-Start.png" width='600' alt="Start the project"/>](./images/MS-Visual-Studio-Start.png)

</div>