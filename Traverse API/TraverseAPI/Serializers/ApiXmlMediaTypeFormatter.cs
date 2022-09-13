using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
using TRAVERSE.Web.API.Properties;

namespace TRAVERSE.Web.API
{
    public class ApiXmlMediaTypeFormatter : MediaTypeFormatter
    {
        #region Constructors
        public ApiXmlMediaTypeFormatter()
        {
            var xslt = XDocument.Parse(NamespaceRemover, LoadOptions.PreserveWhitespace);
            _xlstTransformer = new XslCompiledTransform();
            _xlstTransformer.Load(xslt.CreateReader(), new XsltSettings(), new XmlUrlResolver());

            SupportedMediaTypes.Add(new System.Net.Http.Headers.MediaTypeHeaderValue("application/xml"));
            SupportedMediaTypes.Add(new System.Net.Http.Headers.MediaTypeHeaderValue("text/xml"));
        }
        #endregion Constructors

        #region Methods
        public override bool CanWriteType(Type type)
        {
            return true;
        }

        public override bool CanReadType(Type type)
        {
            return true;
        }
        public async override Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger)
        {
            return await Task.Run<object>(() =>
            {
                JToken token = null;
                XDocument doc = XDocument.Load(readStream);

                if (((XElement)doc.FirstNode).Name.LocalName.Equals(EntityListName, StringComparison.OrdinalIgnoreCase))
                {
                    JArray array = new JArray();
                    XNode node = ((XElement)doc.FirstNode).FirstNode;
                    do
                    {
                        array.Add(JObject.Parse(JsonConvert.SerializeXNode(node, Newtonsoft.Json.Formatting.Indented, true)));
                    }
                    while ((node = node.NextNode) != null);

                    token = array;
                }
                else
                {
                    token = JObject.Parse(JsonConvert.SerializeXNode(doc.FirstNode, Newtonsoft.Json.Formatting.Indented, true));
                }

                if (type == typeof(object))
                    return (new ApiJsonConverter()).ReadJson(token.CreateReader(), typeof(ApiEntityModel), null, System.Web.Http.GlobalConfiguration.Configuration.Formatters.JsonFormatter.CreateJsonSerializer());

                return token.ToObject(type, System.Web.Http.GlobalConfiguration.Configuration.Formatters.JsonFormatter.CreateJsonSerializer());
            });
        }

        public async override Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, TransportContext transportContext)
        {
            var xml = JsonConvert.DeserializeXNode(
                string.Format("{{\"{0}\":{1}}}", EntityName, JsonConvert.SerializeObject(value))
                    , typeof(ApiEntityModel).IsAssignableFrom(type) ? string.Empty : EntityListName, false).ToString();

            if (!string.IsNullOrWhiteSpace(xml))
            {
                xml = xml.Replace("<root>", string.Empty).Replace("</root>", string.Empty);
            }

            using (var writer = XmlTextWriter.Create(writeStream, new XmlWriterSettings() { Indent = true, OmitXmlDeclaration = true, Async = true }))
            {
                await writer.WriteRawAsync(xml);
            }

            writeStream.Flush();
        }
        #endregion Methods

        #region Fields
        private readonly XslCompiledTransform _xlstTransformer;
        private readonly string EntityName = Resources.ApiEntityName;
        private readonly string EntityListName = Resources.ApiEntityListName;
        #endregion Fields

        #region XslTransform
        // See http://wiki.tei-c.org/index.php/Remove-Namespaces.xsl
        private const string NamespaceRemover =
              @"<xsl:stylesheet version='1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'>
            <xsl:output method='xml' indent='no'/>
            <xsl:template match='/|comment()|processing-instruction()'>
              <xsl:copy>
                <xsl:apply-templates/>
              </xsl:copy>
            </xsl:template>
            <xsl:template match='*'>
              <xsl:element name='{local-name()}'>
                <xsl:apply-templates select='@*|node()'/>
              </xsl:element>
            </xsl:template>
            <xsl:template match='@*'>
              <xsl:attribute name='{local-name()}'>
                <xsl:value-of select='.'/>
              </xsl:attribute>
            </xsl:template>
          </xsl:stylesheet>";
        #endregion
    }
}