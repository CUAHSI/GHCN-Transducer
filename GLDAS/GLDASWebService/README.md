NEONWebService_wof11
========================

Solution based on http://github.com/CUAHSI/CUAHSI-GenericWOF_vs2013 with following changes:

* solution upgraded to Visual Studion 2017
* WaterOneFlow version is limited to 1.1 (1.0 is not fully supported)
* The service is configured to support HTTP GET calls (for example cuahsi_1_1.asmx/GetSitesObject?site=&authToken=)
* GetValues method does not pull data values from the ODM database, but it connects to the NEON data API.
