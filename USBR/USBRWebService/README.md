SNOTELWebService_wof11
========================

Solution based on http://github.com/CUAHSI/CUAHSI-GenericWOF_vs2013 with following changes:

* solution upgraded to Visual Studio 2017
* WaterOneFlow version is limited to 1.1 (1.0 is not fully supported)
* The service is configured to support HTTP GET calls (for example cuahsi_1_1.asmx/GetSitesObject?site=&authToken=)
* GetValues method does not pull data values from the ODM database, but it connects to the AWDB SNOTEL web service API.

Customized files in the web service are:
========================================
* genericODws/App_Code/GetValuesSNOTEL.cs (customized data values retrieval class)
* genericODws/App_Code/ODService_v1_1.cs (instead of the default GetValuesOD.GetValues() method, it calls the customized GetValuesSnotel.GetValues() method)
* genericODws/AppSettings.config (customized network code=SNOTEL and vocabulary prefix=SNOTEL)
* genericODws/App_WebReferences/Awdb (web reference to the Awdb SOAP web service. The Awdb SOAP web service is used for retrieving the actual data values.)
