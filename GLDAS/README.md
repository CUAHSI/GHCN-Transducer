# GLDAS2.1-Transducer
Transducer to enable integrate GLDAS2.1 data into the CUAHSI catalog

The transducer contains two parts:
- GldasHarvester.exe: this executable program updates metadata about available sites, variables and time series
- GldasWebService: this ASP.NET website publishes the data in WaterML format

## Original data source
- GLDAS2.1 (https://disc.gsfc.nasa.gov/datasets/GLDAS_NOAH025_M_2.1/summary), using the Data Rods or Giovanni API.

## Setup Instructions for GLDAS-Harvester
1. Install an empty ODM 1.1.1 database on your Microsoft SQL Server. 
- To install an empty ODM 1.1.1 database, create a new database on the database server and execute the script https://github.com/CUAHSI/GHCN-Transducer/blob/master/odm/odm_111.sql
- Alternatively the empty ODM database can be created with the "Attach Database" command using the file: https://github.com/CUAHSI/GHCN-Transducer/blob/master/odm/OD.mdf
2. (optional) Edit the spreadsheet settings\variables.xlsx. This is a lookup table to link the GLDAS and CUAHSI controlled vocabulary terms.
2. Open the solution GldasHarvester.sln in Visual Studio
4. Edit the file App.config: fill in the correct database server, database name, database user and database password for the ODM database.
5. Build the solution and run the command-line program GldasHarvester.EXE
6. While executing the GldasHarvester.EXE populates grid point locations and variables from entries in the settings folder. It also generates entries for the Sources, Methods and SeriesCatalog tables in the ODM database. Progress report of the update including errors and exceptions is saved in a log file in the same directory as the EXE.

## Setup Instructions for GLDAS-WebService
1. Setup and run GldasHarvester.EXE as described above
2. Open the solution GldasWebService_wof11.sln in Visual Studio
3. In the genericODws project create a new file ConnectionStrings.config with content similar as below, replacing MY_SERVER, MY_DB, MY_USER and MY_PASSWORD with the actual database server URL or IP, actual database name, actual DB user name and actual password: 

```xml
<connectionStrings>
  <clear />
  <add name="ODDB" connectionString="Data Source=MY_SERVER;Initial Catalog=MY_DB;User Id=MY_USER;Password=MY_PASSWORD;"
   providerName="System.Data.SqlClient" />
</connectionStrings>
```

- NOTE: The file ConnectionStrings.config is not committed to the GitHub repository for security reasons.
4. Build the solution
5. Copy the whole content of the "GldasWebService" folder to your a folder on IIS Web server where you want to publish the web service.
