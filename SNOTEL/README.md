# SNOTEL Transducer
Transducer to enable integrate the USDA NRCS Snow Telemetry (SNOTEL) data into the CUAHSI catalog

The transducer contains two parts:
- SNOTELHarvester.exe: this executable program updates metadata about available sites, variables and time series
- SNOTELWebService: this ASP.NET website publishes the data in WaterML format

## Original data source
- USDA NRCS Snow Telemetry (SNOTEL): www.wcc.nrcs.usda.gov/snow/
- the data is retrieved from Air-Water Database (AWDB) Web Service: https://www.wcc.nrcs.usda.gov/web_service/awdb_web_service_landing.htm 

## SNOTEL Variable Names 
- This table documents the mapping of original SNOTEL Element names to [CUAHSI Controlled Vocabulary](http://his.cuahsi.org/mastercvreg/edit_cv11.aspx?tbl=VariableNameCV) variable names:

| SCAN Code | SCAN Element Name                         | Units         | CUAHSI Variable Name     | CUAHSI DataType   |
| --------- | ------------------------------------------| --------------| -------------------------| ------------------|
| BATN      | BATTERY                                   | volt          | Battery voltage          | Minimum           |
| BATT      | BATTERY                                   | volt          | Battery voltage          | Average           |
| BATX      | BATTERY                                   | volt          | Battery voltage          | Maximum           |
| DIAG      | DIAGNOSTICS                               | unitless      | ?                        | ?                 |
| DPTP      | DEW POINT TEMPERATURE                     | degF          | Temperature, dew point   | Continuous        |
| LRADT     | SOLAR RADIATION/LANGLEY TOTAL             | langley       | Radiation, total incoming| Continuous        |
| NTRDV     | NET SOLAR RADIATION AVERAGE               | watt/m2       | Radiation, net           | Average           |
| PARV      | PHOTOSYNTHETICALLY ACTIVE RADIATION (PAR) | micromole/m2/s| Radiation, incoming PAR  | Continuous        |
| PRCP      | PRECIPITATION INCREMENT                   | inches        | Precipitation            | Incremental       |
| PRCPSA    | PRECIPITATION INCREMENT – SNOW-ADJUSTED   | inches        | Precipitation            | Incremental       |
| PREC      | PRECIPITATION ACCUMULATION                | inches        | Precipitation            | Cumulative        |
| PRES      | BAROMETRIC PRESSURE                       | inch_Hg       | Barometric pressure      | Continuous        |
| PVPV      | VAPOR PRESSURE - PARTIAL                  | kPa           | Vapor pressure           | Continuous        |
| RDC       | REAL DIELECTRIC CONSTANT                  | unitless      | Real dielectric constant | Continuous        |
| RHENC     | RELATIVE HUMIDITY ENCLOSURE               | pct           | Relative humidity        | Continuous        |
| RHUM      | RELATIVE HUMIDITY                         | pct           | Relative humidity        | Continuous        |
| RHUMN     | RELATIVE HUMIDITY                         | pct           | Relative humidity        | Minimum           |
| RHUMV     | RELATIVE HUMIDITY                         | pct           | Relative humidity        | Average           |
| RHUMX     | RELATIVE HUMIDITY                         | pct           | Relative humidity        | Maximum           |
| SAL       | SALINITY                                  | gram/l        | Salinity                 | Continuous        |
| SMS       | SOIL MOISTURE PERCENT                     | pct           | Volumetric water content | Continuous        |
| SNWD      | SNOW DEPTH                                | inches        | Snow depth               | Continuous        |
| SRADV     | SOLAR RADIATION AVERAGE                   | watt/m2       | Radiation, incoming      | Average           |
| STN       | SOIL TEMPERATURE MINIMUM                  | degF          | Temperature              | Minimum           |
| STO       | SOIL TEMPERATURE OBSERVED                 | degF          | Temperature              | Continuous        |
| STV       | SOIL TEMPERATURE AVERAGE                  | degF          | Temperature              | Average           |
| STX       | SOIL TEMPERATURE MAXIMUM                  | degF          | Temperature              | Maximum           |
| SVPV      | VAPOR PRESSURE - SATURATED                | kPa           | Vapor pressure           | Continuous        |
| TAVG      | AIR TEMPERATURE AVERAGE                   | degF          | Temperature              | Average           |
| TMAX      | AIR TEMPERATURE MAXIMUM                   | degF          | Temperature              | Maximum           |
| TMIN      | AIR TEMPERATURE MINIMUM                   | degF          | Temperature              | Minimum           |
| TOBS      | AIR TEMPERATURE OBSERVED                  | degF          | Temperature, sensor      | Continuous        |
| WDIRV     | WIND DIRECTION AVERAGE                    | degree        | Wind direction           | Average           |
| WDIRZ     | WIND DIRECTION STANDARD DEVIATION         | degree        | Wind direction           | StandardDeviation |           
| WDMVT     | WIND MOVEMENT TOTAL                       | mile          | Wind Run                 | Continuous        |
| WSPDV     | WIND SPEED AVERAGE                        | mph           | Wind speed               | Average           |
| WSPDX     | WIND SPEED MAXIMUM                        | mph           | Wind speed               | Maximum           |
| WTEQ      | SNOW WATER EQUIVALENT                     | inches        | Snow water equivalent    | Continuous        |
| ZDUM      | DUMMY LABEL                               | volt          | ?                        | ?                 |

- See also http://www.fondriest.com/environmental-measurements/parameters/weather/photosynthetically-active-radiation/

## SNOTEL Time Support 
- For each available combination of SNOTEL element and duration, the WaterOneFlow service uses a separate variable code with duration attached to the element code. For example Daily average soil temperature is coded as *STV_D*, Hourly soil temperature as *STO_H*. The SCAN time support is also encoded by the *TimeSupport* element of the WaterML *Variable* object:

| SNOTEL duration | CUAHSI variable code part | CUAHSI time unit | CUAHSI time support value |
| --------------- | --------------------------| -----------------| --------------------------|
| HOURLY          | H                         | hour             | 1                         |
| DAILY           | D                         | day              | 1                         |
| SEMIMONTHLY     | sm                        | month            | 0.5                       |
| MONTHLY         | m                         | month            | 1                         |
| SEASONAL        | season                    | month            | 3                         |
| ANNUAL          | a                         | year             | 1                         |
| WATER_YEAR      | wy                        | year             | 1                         |
| CALENDAR_YEAR   | y                         | year             | 1                         |

## SNOTEL Heights and Depths 
- SNOTEL observations are recorded at different heights above ground or depths below ground surface. The height or depth value is always in inches. Negative value indicates a subsurface sensor (in the soil profile). Positive values are used for wind sensors at different heights above ground. The WaterOneFlow service represents the heights and depths using the VariableCode, Method and Offset. For example the CUAHSI variable code **SMS_H_D2** means "Volumetric water content measured hourly at 2 inches depth" or the CUAHSI variable code **WSPDV_D_H40** means "Average wind speed (daily) at 40 inches height".

## Resulting CUAHSI variable code
- The resulting CUAHSI variable code consists of two or three parts separated by the **underscore** character. 
- The first part is the SNOTEL element code (for example WTEQ, SNWD, PRCP ...)
- The second part is the time support abbreviation (H, D, sm, m, season, a, wy, y) - see table above
- The third part is only included if the variable can be measured at multiple heights or depths (for example D40, D2, H60, H120 ...) The "D" means depth below ground surface and the "H" means height above ground surface.
- For example the CUAHSI variable code **SMS_D_D40** means "Volumetric water content (daily) at depth of 40 inches
- another example: The CUAHSI variable code **TMAX_m** means "Maximum air temperature (monthly) with no specified height or depth.


## Setup Instructions for SNOTEL Harvester
1. Install an empty ODM 1.1.1 database on your Microsoft SQL Server. 
- To install an empty ODM 1.1.1 database, create a new database on the database server and execute the script https://github.com/CUAHSI/GHCN-Transducer/blob/master/odm/odm_111.sql
- Alternatively the empty ODM database can be created with the "Attach Database" command using the file: https://github.com/CUAHSI/GHCN-Transducer/blob/master/odm/OD.mdf
2. Open the solution SNOTELharvester.sln in Visual Studio
3. Edit the file app.config: fill in the correct database server, database name, database user and database password for the ODM database.
4. Build the solution and run the command-line program SCANharvester.EXE
5. While executing the SNOTELharvester.EXE connects to the AWDB web service and populates the Variables, Sources, Qualifiers, Sites and SeriesCatalog tables in the ODM database. Progress report of the update including errors and exceptions is saved in a log file in the same directory as the EXE.

## Setup Instructions for SNOTEL Web Service
1. Setup and run SNOTELHarvester as described above
2. Open the solution SNOTELWebService.sln in Visual Studio
3. Edit the file ConnectionStrings.config: fill in the correct database server, database name, database user and database password for the ODM database.
4. Build the solution
5. Copy the whole content of the "SNOTELWebService" folder to your a folder on IIS Web server where you want to publish the web service
