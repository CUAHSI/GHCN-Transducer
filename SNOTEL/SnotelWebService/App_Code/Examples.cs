using System;
using System.Collections.Generic;
using System.Data;
using System.Configuration;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;

/// <summary>
/// Summary description for Examples
/// </summary>
public class Examples
{
    private const string CodeFormat = "{0}:{1}";

    private String connectionString;

    public string ConnectionString
    {
        get { return connectionString; }
        set { connectionString = value; }
    }

    private String networkCode;
    private String vocabularyCode;

    public string NetworkCode
    {
        get { return networkCode; }
        set { networkCode = value; }
    }

    public string VocabularyCode
    {
        get { return vocabularyCode; }
        set { vocabularyCode = value; }
    }

    public Examples()
    {
    }

    public Examples(String connection)
    {
        this.connectionString = connection;
    }

    public List<String> GetSites()
    {
        using (DbConnection connection= new SqlConnection( connectionString   ))
        {
            List<String> sitesSamples = new List<String>();
            
            connection.Open();
            string sitesQ = " Select TOP 3 st_id from stations ";
            DbCommand command = connection.CreateCommand();
            command.CommandText = sitesQ;
            using (DbDataReader reader = command.ExecuteReader() )
            {
            
            

            if (reader.HasRows)
            {
                if (reader.Read())
                {
                    sitesSamples.Add(String.Format(CodeFormat, NetworkCode, reader.GetInt16(0).ToString()));
                }
                string siteList = "In SOAP send ARRAY OF String[] (";
                while (reader.Read())
                {

                    siteList += String.Format(" '{0}' ",
                                              string.Format(CodeFormat, NetworkCode, reader.GetInt16(0).ToString())
                        )
                        ;
                }
                siteList += ") ";
                sitesSamples.Add(siteList);
            }
        }
        //BOX((W S, E N))
           string sitesBoundQ = @" SELECT  CONVERT(varchar,MIN(lon)) +   
                     ' '+  CONVERT(varchar,MIN(lat))+ ', '+ 
                    CONVERT(varchar,Max(lon))+ ' '+
                   CONVERT(varchar,Max(lat)) AS Box FROM locations";
            command = connection.CreateCommand();
            command.CommandText = sitesBoundQ;
             String sitesBounds= (string) command.ExecuteScalar();
             sitesSamples.Add("GEOM:BOX(" + sitesBounds +")");
            connection.Close();
            return sitesSamples;

            
        }
    }

    public List<String> GetSiteInfo()
    {
        using (DbConnection connection = new SqlConnection(connectionString))
        {
           connection.Open();
           string sitesQ = " Select DISTINCT TOP 3 st_id from stationsvariables WHERE var_id=8 ";
            DbCommand command = connection.CreateCommand();
            command.CommandText = sitesQ;
            DbDataReader reader = command.ExecuteReader();
               List<String> sitesSamples = new List<String>();
            if (reader.HasRows)
            {
                while(reader.Read())
                {
                    
                    sitesSamples.Add(String.Format(CodeFormat, NetworkCode, reader[0].ToString()));
                }

            }
            connection.Close();
            return sitesSamples;

          
        }
    }

    public List<String> GetVariableSimple()
    {
        using (DbConnection connection = new SqlConnection(connectionString))
        {
           connection.Open();
            string sitesQ = " Select TOP 3 var_id from variables ";
            DbCommand command = connection.CreateCommand();
            command.CommandText = sitesQ;
            DbDataReader reader = command.ExecuteReader();
                List<String> samples = new List<String>();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    
                    samples.Add(String.Format(CodeFormat, VocabularyCode, reader[0].ToString()));
                }

            }
            connection.Close();
            return samples;

           
        }
    }

    public List<String> GetVariableDetailed()
    {
        using (DbConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();
            string sitesQ = " Select TOP 3 * from variables ";
            DbCommand command = connection.CreateCommand();
            command.CommandText = sitesQ;
            DbDataReader reader = command.ExecuteReader();
                List<String> samples = new List<String>();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    ;
                    DetailedVariable variable = new DetailedVariable();
                    variable.VariableVocabulary = VocabularyCode;
                    variable.VariableCode = ValueFromReader(reader, "var_id");
                    variable.SampleMedium = "snow" ;
                    variable.DataType = "Average";
                    variable.ValueType = "Field Observation";
                    samples.Add(variable.ToString());
                }

            }
            connection.Close();
            return samples;

           
        }
    }
    private string ValueFromReader(DbDataReader reader, String field)
    {
        string val = null;
        int col = reader.GetOrdinal(field);
        if (col >=0)
        {
            if (!reader.IsDBNull(col)) 
                val = reader[col].ToString();
        }
        return val;
    }
    private  class DetailedVariable
    {
        private string variableVocabulary;
        private string variableCode;
        private string sampleMedium;
        private string dataType;
        private string valueType;
        private string methodId;
        private string sourceId;
        private string qualityControlLevelId;

        public string VariableVocabulary
        {
            get { return variableVocabulary; }
            set { variableVocabulary = value; }
        }

        public string VariableCode
        {
            get { return variableCode; }
            set { variableCode = value; }
        }

        public string SampleMedium
        {
            get { return sampleMedium; }
            set { sampleMedium = value; }
        }

        public string DataType
        {
            get { return dataType; }
            set { dataType = value; }
        }

        public string ValueType
        {
            get { return valueType; }
            set { valueType = value; }
        }

        public string MethodId
        {
            get { return methodId; }
            set { methodId = value; }
        }

        public string SourceId
        {
            get { return sourceId; }
            set { sourceId = value; }
        }

        public string QualityControlLevelId
        {
            get { return qualityControlLevelId; }
            set { qualityControlLevelId = value; }
        }

        public string ToString()
        {
            StringBuilder variable = new StringBuilder();

            if (!String.IsNullOrEmpty(variableCode) && ! String.IsNullOrEmpty(VariableVocabulary))
            {
                variable.AppendFormat("{0}:{1}", VariableVocabulary, VariableCode);
            } else
            {
                // no variableCode or vocabulary
                return "INTERNAL ERROR missing variable vocabulary or variable code.";
            }

            const string keypair = "/{0}={1}";

            if (!string.IsNullOrEmpty(sampleMedium)) variable.AppendFormat(keypair, "SampleMedium", sampleMedium);
            if (!string.IsNullOrEmpty(valueType)) variable.AppendFormat(keypair, "valueType", valueType);
            if (!string.IsNullOrEmpty(dataType)) variable.AppendFormat(keypair, "dataType", dataType);
            if (!string.IsNullOrEmpty(QualityControlLevelId)) variable.AppendFormat(keypair, "QualityControlLevelId", QualityControlLevelId);

            if (!string.IsNullOrEmpty(sourceId)) variable.AppendFormat(keypair, "sourceId", sourceId);
            if (!string.IsNullOrEmpty(MethodId)) variable.AppendFormat(keypair, "MethodId", MethodId);

            return variable.ToString();
        }
    }
  
}
