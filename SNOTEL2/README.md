# SNOTEL web service (new version)
Transducer and web service to enable integrate NEON data into the CUAHSI catalog

## Setup Instructions for SNOTEL Web Service
1. Setup and run SNOTELHarvester as described above
2. Open the solution SNOTELWebService.sln in Visual Studio
3. Edit the file ConnectionStrings.config: fill in the correct database server, database name, database user and database password for the ODM database.
4. If required, edit the file appsettings.config and uncomment or edit the setting <add key="exclude_durations" value="WATER_YEAR, CALENDAR_YEAR, YEARLY, SEASONAL, MONTHLY, SEMIMONTHLY"/> to exclude any time-aggregated variables from the WaterML response.
5. Build the solution
