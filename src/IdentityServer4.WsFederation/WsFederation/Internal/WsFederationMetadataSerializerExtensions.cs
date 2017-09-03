
using System;
using System.Linq;
using System.Xml;
using Microsoft.IdentityModel.Protocols.WsFederation;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Tokens.Saml;
using Microsoft.IdentityModel.Xml;
using static Microsoft.IdentityModel.Protocols.WsFederation.WsFederationConstants;

public static class WsFederationMetadataSerializerExtensions
{
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static void WriteMetadata(this WsFederationMetadataSerializer serializer, XmlWriter writer, WsFederationConfiguration configuration)
    {
        if (writer == null)
            throw new ArgumentNullException(nameof(writer));

        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        var securityKey = configuration.SigningKeys.FirstOrDefault() as X509SecurityKey;
        var entityDescriptorId = "_" + Guid.NewGuid().ToString();
        EnvelopedSignatureWriter envelopeWriter = null;
        if (securityKey != null)
        {
            var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256Signature, SecurityAlgorithms.Sha256Digest);
            envelopeWriter = new EnvelopedSignatureWriter(
                writer, 
                signingCredentials,
                "#" + entityDescriptorId); 
            writer = envelopeWriter;
        }
        writer.WriteStartDocument();

        //TODO: Add Signature
        // <EntityDescriptor>
        writer.WriteStartElement(Elements.EntityDescriptor, Namespaces.MetadataNamespace);
        // @entityID
        writer.WriteAttributeString(Attributes.EntityId, configuration.Issuer);
        // @ID
        writer.WriteAttributeString("ID", entityDescriptorId);

        // if (envelopeWriter != null)
        // {
        //     envelopeWriter.WriteSignature();
        // }

        WriteSecurityTokenServiceTypeRoleDescriptor(configuration, writer);

        // </EntityDescriptor>
        writer.WriteEndElement();

        writer.WriteEndDocument();
    }

    private static void WriteSecurityTokenServiceTypeRoleDescriptor(WsFederationConfiguration configuration, XmlWriter writer)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        // <RoleDescriptorr>
        writer.WriteStartElement("xsi", Elements.RoleDescriptor, XmlSignatureConstants.XmlSchemaNamespace);
        writer.WriteAttributeString("xmlns", "fed", null, WsFederationConstants.Namespaces.FederationNamespace);
        writer.WriteAttributeString("protocolSupportEnumeration", WsFederationConstants.Namespaces.FederationNamespace);
        writer.WriteStartAttribute(Attributes.Type, XmlSignatureConstants.XmlSchemaNamespace);
        writer.WriteQualifiedName(Types.SecurityTokenServiceType, WsFederationConstants.Namespaces.FederationNamespace);
        writer.WriteEndAttribute();

        WriteKeyDescriptorForSigning(configuration, writer);

        WriteSecurityTokenEndpoint(configuration, writer);

        // else if (reader.IsStartElement())
        //     reader.ReadOuterXml();

        // </RoleDescriptorr>
        writer.WriteEndElement();
    }

    private static void WriteSecurityTokenEndpoint(WsFederationConfiguration configuration, XmlWriter writer)
    {
        // <SecurityTokenServiceEndpoint>
        writer.WriteStartElement(Elements.SecurityTokenEndpoint, Namespaces.FederationNamespace);
        // writer.WriteAttributeString(Attributes.Type, XmlSignatureConstants.XmlSchemaNamespace,);
        // var typeQualifiedName = new XmlQualifiedName(Types.SecurityTokenServiceType, Namespaces.FederationNamespace);

        // <EndpointReference>
        writer.WriteStartElement("wsa", Elements.EndpointReference, Namespaces.AddressingNamspace);  // EndpointReference

        // <Address>
        writer.WriteStartElement(Elements.Address, Namespaces.AddressingNamspace);  // Address
        writer.WriteString(configuration.TokenEndpoint);
        // </Address>
        writer.WriteEndElement();

        // </EndpointReference>
        writer.WriteEndElement();

        // </SecurityTokenServiceEndpoint>
        writer.WriteEndElement();
    }

    private static void WriteKeyDescriptorForSigning(WsFederationConfiguration configuration, XmlWriter writer)
    {
        // <KeyDescriptor>
        writer.WriteStartElement(Elements.KeyDescriptor, Namespaces.MetadataNamespace);
        writer.WriteAttributeString(Attributes.Use, keyUse.Signing);

        // <KeyInfo>
        writer.WriteStartElement(XmlSignatureConstants.Elements.KeyInfo, XmlSignatureConstants.Namespace);

        var dsigSerializer = new DSigSerializer();
        foreach (var keyInfo in configuration.KeyInfos)
        {
            dsigSerializer.WriteKeyInfo(writer, keyInfo);
        }
        // </KeyInfo>
        writer.WriteEndElement();

        // </KeyDescriptor>
        writer.WriteEndElement();
    }
}