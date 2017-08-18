# GHCN-Transducer
Transducer to enable integrate GHCN data into the CUAHSI catalog

The transducer contains two parts:
- GHCN-Harvester.exe: this executable program updates metadata about available sites, variables and time series
- GHCN-service: this ASP.NET website publishes the data in WaterML format

## Original data source
- Global Historical Climatology Network - Daily (GHCN-Daily), Version 3: data.noaa.gov/dataset/global-historical-climatology-network-daily-ghcn-daily-version-3
- List of sites: www1.ncdc.noaa.gov/pub/data/ghcn/daily/ghcnd-stations.txt
- Series catalog: www1.ncdc.noaa.gov/pub/data/ghcn/daily/ghcnd-inventory.txt
- Data files: www1.ncdc.noaa.gov/pub/data/ghcn/daily/gsn/

## Special Configuration Settings
- the GHCN-Daily dataset includes selected sites from the CoCoRaHS and SNOTEL observation networks. By default the transducer is configured to show all GHCN-Daily sites including
CoCoRaHS and SNOTEL. CUAHSI provides complete data from these networks using dedicated CoCoRaHS and SNOTEL web service transducers. To exclude CoCoRaHS and SNOTEL sites from
the GHCN transducer, set the **use_cocorahs** and **use_snotel** to **false** in the *app.config* configuration file of GhcnHarvester.

## Setup Instructions for GHCN-Harvester
1. Install an empty ODM 1.1.1 database on your Microsoft SQL Server. 
- To install an empty ODM 1.1.1 database, create a new database on the database server and execute the script https://github.com/CUAHSI/GHCN-Transducer/blob/master/odm/odm_111.sql
- Alternatively the empty ODM database can be created with the "Attach Database" command using the file: https://github.com/CUAHSI/GHCN-Transducer/blob/master/odm/OD.mdf
2. Open the solution GhcnHarvester.sln in Visual Studio
3. Edit the file app.config: fill in the correct database server, database name, database user and database password for the ODM database.
4. If required, edit the settings use_cocorahs and use_snotel in app.config. Setting the values to false will exclude CoCoRaHS and SNOTEL sites from the web service
5. Build the solution and run the command-line program MetadataHarvester.EXE
6. While executing the GhcnHarvester.EXE connects to the GHCN website and populates the Variables, Sources, Qualifiers, Sites and SeriesCatalog tables in the ODM database. Progress report of the update including errors and exceptions is saved in a log file in the same directory as the EXE.

## Setup Instructions for GHCN-Service
1. Setup and run GHCN-Harvester as described above
2. Open the solution GhcnWebService.sln in Visual Studio
3. Edit the file ConnectionStrings.config: fill in the correct database server, database name, database user and database password for the ODM database.
4. Build the solution
5. Copy the whole content of the "GhcnWebService" folder to your a folder on IIS Web server where you want to publish the web service
