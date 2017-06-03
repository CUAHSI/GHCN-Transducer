# GHCN-Transducer
Transducer to enable integrate GHCN data into thr CUAHSI catalog

The transducer contains two parts:
- GHCN-Harvester.exe: this executable program updates metadata about available sites, variables and time series
- GHCN-service: this ASP.NET website publishes the data in WaterML format

## Original data sources
- List of sites: ftp://ftp.ncdc.noaa.gov/pub/data/ghcn/daily/ghcnd-stations.txt
- Series catalog: ftp://ftp.ncdc.noaa.gov/pub/data/ghcn/daily/ghcnd-inventory.txt
- Data files: ftp://ftp.ncdc.noaa.gov/pub/data/ghcn/daily/gsn/
