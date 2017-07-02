# GHCN-Transducer
Transducer to enable integrate GHCN data into thr CUAHSI catalog

The transducer contains two parts:
- GHCN-Harvester.exe: this executable program updates metadata about available sites, variables and time series
- GHCN-service: this ASP.NET website publishes the data in WaterML format

## Original data source
- Global Historical Climatology Network - Daily (GHCN-Daily), Version 3: data.noaa.gov/dataset/global-historical-climatology-network-daily-ghcn-daily-version-3
- List of sites: www1.ncdc.noaa.gov/pub/data/ghcn/daily/ghcnd-stations.txt
- Series catalog: www1.ncdc.noaa.gov/pub/data/ghcn/daily/ghcnd-inventory.txt
- Data files: www1.ncdc.noaa.gov/pub/data/ghcn/daily/gsn/

## Setup Instructions
- Install an empty ODM 1.1.1 database on your Microsoft SQL Server. There are two ways for installing an empty ODM 1.1.1 database:
-- Attach the database file OD.mdf
-- Create an empty database and execute the SQL script ODM_mssql_111.sql
