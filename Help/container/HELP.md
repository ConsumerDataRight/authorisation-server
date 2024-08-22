<h2>Use the pre-built image for this solution</h2>

<br />
<p>1. Pull the latest image from <a href="https://hub.docker.com/r/consumerdataright/authorisation-server" title="Download the container from docker hub here" alt="Download the container from docker hub here">Docker Hub</a></p>

<span style="display:inline-block;margin-left:1em;">
	docker pull consumerdataright/authorisation-server
</span>

<br />
<p>2. Run the Authorisation Server container</p>

<span style="display:inline-block;margin-left:1em;">
	docker run -d -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Pa{}w0rd2019" -p 1433:1433 --name mssql -h sql1 -d mcr.microsoft.com/mssql/server:2022-latest
	docker run -d -h authorisation-server -p 8001:8001 -p 3000:3000 --add-host=mssql:host-gateway --name authorisation-server consumerdataright/authorisation-server<br \>
	<br \><br \>
	Please note - This docker compose file utilises the Microsoft SQL Server Image from Docker Hub.<br \>
	The Microsoft EULA for the Microsoft SQL Server Image must be accepted to continue.<br \>
	See the Microsoft SQL Server Image on Docker Hub for more information.<br \>
	Using the above command from a MS Windows command prompt will run the database.<br \>
</span>

<br />

<span style="display:inline-block;margin-left:1em;margin-top:10px;margin-bottom:10px;">
	How to build your own image instead of downloading it from docker hub.<br \>
	navigate to .\authorisation-server\Source<br \>
	open a command prompt and execute the following;<br \>
	docker build -f Dockerfile.standalone -t authorisation-server .<br \>
	Please note - By default, the container above will be using a MS SQL database container, using this command from a MS Windows command prompt will run the database,<br \> 
	docker run -d -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Pa{}w0rd2019" -p 1433:1433 --name mssql -h sql1 -d mcr.microsoft.com/mssql/server:2022-latest
	docker run -d -h authorisation-server -p 8001:8001 -p 3000:3000 --add-host=mssql:host-gateway --name authorisation-server authorisation-server<br \><br \>	
</span>

<span style="display:inline-block;margin-left:1em;margin-top:10px;margin-bottom:10px;">
	You can connect to the MS SQL database container from MS Sql Server Management Studio (SSMS) using
	the following settings; <br />
	Server type: Database Engine <br />
	Server name: localhost <br />
	Authentication: SQL Server Authentication <br />
	Login: sa <br />
	Password: Pa{}w0rd2019 <br />
</span>
<br />

[<img src="./images/ssms-login-error.png" height='300' width='400' alt="SSMS Login Error"/>](./images/ssms-login-error.png)

<p>
	(NB: if the above error occurs whilst trying to connect to the MS SQL container, the SQL Server Service MUST BE STOPPED, you can do this from SQL Server Manager)
</p>