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
2. Open the solution NEONWebService_wof11.sln in Visual Studio
3. In the genericODws project create a new file ConnectionStrings.config with content: 

```xml
<connectionStrings>
  <clear />
  <add name="ODDB" connectionString="Data Source=MY_SERVER;Initial Catalog=MY_DB;User Id=MY_USER;Password=MY_PASSWORD;"
   providerName="System.Data.SqlClient" />
</connectionStrings>
```

3. Edit the file ConnectionStrings.config: fill in the correct database server, database name, database user and database password for the ODM database.
- NOTE: For improved security we recommend setting up a separate MSSQL database user account with read-only data access permission to be used by NEONWebService
4. Build the solution
5. Copy the whole content of the "NEONWebService" folder to your a folder on IIS Web server where you want to publish the web service.

## Documentation - Supported NEON Products and Variables
The NEON web service transducer supports a subset of NEON sites, products, variables and attributes which are in the hydrology domain of CUAHSI. These product belong to the Atmosphere and Ecohydrology categories. Each NEON data product consists of several data tables with multiple attributes. A lookup table which associates NEON data products, tables and attributes to a CUAHSI variables and methods is available at https://github.com/CUAHSI/GHCN-Transducer/blob/master/NEON/NEONHarvester/settings/neon_variables_lookup.xlsx.

The data made available through the WaterOneFlow web service interface have following limitations:
* Only aggregate 30-minute average data are supported. Original measurements with higher temporal resolution (2-minute, 30 seconds) and other statistical summaries (30-minute maximum, minimum, median and standard deviation) are not made available via the web service due to large volume of downloaded data.
* Only NEON data from sensors with published sensor positions are available.
* A NEON site consists of multiple sensors with distinct geographical coordinates. In CUAHSI, a CUAHSI site corresponds to a NEON sensor. Therefore, a NEON Site is represented as a set of of multiple CUAHSI sites.
* NEON geospatial datasets (such as LIDAR scans) are not published through the CUAHSI web service. Only point-based 1d time-series data are available.
