# CoCoRaHS Transducer
Transducer to enable integrate CoCoRaHS data into the CUAHSI catalog

The transducer contains two parts:
- CoCoHarvester.exe: this executable program updates metadata about available sites, variables and time series
- CoCoService: this ASP.NET website publishes the data in WaterML format

## Original data source
- Community Cooperative Rain, Hail & Snow Network (CoCoRaHS): www.cocorahs.org

## Setup Instructions for CoCoRaHS Harvester
1. Install an empty ODM 1.1.1 database on your Microsoft SQL Server. 
- To install an empty ODM 1.1.1 database, create a new database on the database server and execute the script https://github.com/CUAHSI/GHCN-Transducer/blob/master/odm/odm_111.sql
- Alternatively the empty ODM database can be created with the "Attach Database" command using the file: https://github.com/CUAHSI/GHCN-Transducer/blob/master/odm/OD.mdf
2. Open the solution CoCoHarvester.sln in Visual Studio
3. Edit the file app.config: fill in the correct database server, database name, database user and database password for the ODM database.
4. Build the solution and run the command-line program CoCoHarvester.EXE
5. While executing the CoCoHarvester.EXE connects to the CoCoRaHS website and populates the Variables, Sources, Qualifiers, Sites and SeriesCatalog tables in the ODM database. Progress report of the update including errors and exceptions is saved in a log file in the same directory as the EXE.

## Setup Instructions for CoCoRaHS Web Service
1. Setup and run CoCoHarvester as described above
2. Open the solution CoCoWebService_wof11.sln in Visual Studio
3. In the genericODws project, create a file ConnectionStrings.config: fill in the correct database server, database name, database user and database password for the ODM database. You can use the provided file ConnectionStrings_Example.config as an example.
4. Build the solution
5. Copy the whole content of the "CoCoWebService" folder to your a folder on IIS Web server where you want to publish the web service
