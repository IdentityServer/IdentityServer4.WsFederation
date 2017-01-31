using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Metadata;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace IdentityServer4.WsFederation
{
    public class MetadataResult : IActionResult
    {
        private readonly EntityDescriptor _entity;

        public MetadataResult(EntityDescriptor entity)
        {
            _entity = entity;
        }

        public Task ExecuteResultAsync(ActionContext context)
        {
            var ser = new MetadataSerializer();
            var sb = new StringBuilder(512);

            ser.WriteMetadata(XmlWriter.Create(new StringWriter(sb), new XmlWriterSettings { OmitXmlDeclaration = true }), _entity);

            context.HttpContext.Response.ContentType = "application/xml";
            return context.HttpContext.Response.WriteAsync(sb.ToString());
        }
    }
}
