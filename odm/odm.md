# ODM 1.1.1. database for Microsoft SQL Server (modified version)
This folder contains a .mdf file and SQL script that can be used to create a default Observations Data Model (ODM) database on your system.

- the original database schema is published at: http://hydroserver.codeplex.com/releases/view/83377
- NOTICE: compared to the original ODM 1.1.1 this modified version of ODM contains an extra field "SourceCode" in the "Sources" table.

## Setup Instructions for ODM
- The ODM database runs on MS SQL Server. Most versions of MSSQL including SQL Server Express are supported.
- To install an empty ODM 1.1.1 database, create a new database on the database server and execute the script https://github.com/CUAHSI/GHCN-Transducer/blob/master/odm/odm_111.sql
- Alternatively the empty ODM database can be created with the "Attach Database" command using the file: https://github.com/CUAHSI/GHCN-Transducer/blob/master/odm/OD.mdf