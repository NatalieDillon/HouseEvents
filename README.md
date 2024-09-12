To run the code</br>
1. Create a new database in SQL Server called HouseEvent
2. Run the scripts in HouseEvents.Data/Scripts CreateTables.sql and SeedData.sql
3. Change the connection string in HouseEventDb in appsettings.json to point to your own instance of sql server
4. You can test calling it from the browser or from Postman

The key classes to look at are:
1. HouseEvents.Api/Program.cs - this class defines the api interface and what each call does
2. HouseEvents.Data/HouseEventsDB.cs - this class makes asynchronous calls to get data from the database

Points to Note:
1. Basic objects known as dtos (data transfer objects) are used to convert to and from JSON. 
2. In reality a webservice would need authentication configured
