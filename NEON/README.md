# NEON-Transducer
Transducer to enable integrate NEON data into the CUAHSI catalog

The transducer contains two parts:
- NEONHarvester.exe: this executable program updates metadata about available sites, variables and time series
- NEONWebService: this ASP.NET website publishes the data in WaterML format

## Original data source
- NEON (http://data.neonnscience.org), using the NEON API.

## Setup Instructions for NEON-Harvester
1. Install an empty ODM 1.1.1 database on your Microsoft SQL Server. 
- To install an empty ODM 1.1.1 database, create a new database on the database server and execute the script https://github.com/CUAHSI/GHCN-Transducer/blob/master/odm/odm_111.sql
- Alternatively the empty ODM database can be created with the "Attach Database" command using the file: https://github.com/CUAHSI/GHCN-Transducer/blob/master/odm/OD.mdf
2. (optional) Edit the spreadsheet settings\neon_variables_lookup_example.xlsx. This is a lookup table to link the NEON and CUAHSI controlled vocabulary terms.
2. Open the solution NEONHarvester.sln in Visual Studio
4. Edit the file ConnectionStrings.config: fill in the correct database server, database name, database user and database password for the ODM database.
5. Build the solution and run the command-line program NEONHarvester.EXE
6. While executing the NEONHarvester.EXE connects to the NEON data API website and populates the Variables, Sources, Sites and SeriesCatalog tables in the ODM database. Progress report of the update including errors and exceptions is saved in a log file in the same directory as the EXE.

## Setup Instructions for NEON-WebService
1. Setup and run NEONHarvester.EXE as described above
2. Open the solution NEONWebService.sln in Visual Studio
3. Edit the file ConnectionStrings.config: fill in the correct database server, database name, database user and database password for the ODM database.
- NOTE: For improved security we recommend setting up a separate MSSQL database user account with read-only data access permission to be used by NEONWebService
4. Build the solution
5. Copy the whole content of the "NEONWebService" folder to your a folder on IIS Web server where you want to publish the web service.
