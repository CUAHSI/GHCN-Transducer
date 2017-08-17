# SCAN Transducer
Transducer to enable integrate the Soil Climate Analysis Network (SCAN) data into the CUAHSI catalog

The transducer contains two parts:
- SCANharvester.exe: this executable program updates metadata about available sites, variables and time series
- SCANservice: this ASP.NET website publishes the data in WaterML format

## Original data source
- USDA NRCS Soil Climate Analysis Network (SCAN): www.wcc.nrcs.usda.gov/scan/
- the data is retrieved from Air-Water Database (AWDB) Web Service: https://www.wcc.nrcs.usda.gov/web_service/awdb_web_service_landing.htm 

## Setup Instructions for SCAN Harvester
1. Install an empty ODM 1.1.1 database on your Microsoft SQL Server. 
- To install an empty ODM 1.1.1 database, create a new database on the database server and execute the script https://github.com/CUAHSI/GHCN-Transducer/blob/master/odm/odm_111.sql
- Alternatively the empty ODM database can be created with the "Attach Database" command using the file: https://github.com/CUAHSI/GHCN-Transducer/blob/master/odm/OD.mdf
2. Open the solution SCANharvester.sln in Visual Studio
3. Edit the file app.config: fill in the correct database server, database name, database user and database password for the ODM database.
4. Build the solution and run the command-line program SCANharvester.EXE
5. While executing the SCANharvester.EXE connects to the AWDB web service and populates the Variables, Sources, Qualifiers, Sites and SeriesCatalog tables in the ODM database. Progress report of the update including errors and exceptions is saved in a log file in the same directory as the EXE.

## Setup Instructions for SCAN Web Service
1. Setup and run SCANharvester as described above
2. Open the solution SCANWebService.sln in Visual Studio
3. Edit the file ConnectionStrings.config: fill in the correct database server, database name, database user and database password for the ODM database.
4. Build the solution
5. Copy the whole content of the "SCANWebService" folder to your a folder on IIS Web server where you want to publish the web service
