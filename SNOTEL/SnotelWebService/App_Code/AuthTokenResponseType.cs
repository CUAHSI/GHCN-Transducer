using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace WaterOneFlow.Schema.v1_1
{
    [Serializable]
    [XmlType(Namespace = "http://www.cuahsi.org/waterML/1.1/")]
    [DebuggerStepThrough]
    [XmlRoot("sitesResponse", Namespace = "http://www.cuahsi.org/waterML/1.1/", IsNullable = false)]
    [GeneratedCode("xsd", "4.0.30319.1")]
    [DesignerCategory("code")]
    public class AuthTokenResponseType
    {
        public AuthTokenResponseType() { }

        public bool IsValid { get; set; }

        public string Token { get; set; }

        public string Expires { get; set; }

        public string Message { get; set; }
    }
}
