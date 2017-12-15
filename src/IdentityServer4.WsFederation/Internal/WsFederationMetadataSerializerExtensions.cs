
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

        if (string.IsNullOrEmpty(configuration.Issuer))
            throw XmlUtil.LogWriteException(nameof(configuration.Issuer) + " is null or empty");

        if (string.IsNullOrEmpty(configuration.TokenEndpoint))
            throw XmlUtil.LogWriteException(nameof(configuration.TokenEndpoint) + " is null or empty");

        X509SecurityKey securityKey = configuration.SigningKeys.FirstOrDefault() as X509SecurityKey;
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

        // <EntityDescriptor>
        writer.WriteStartElement(Elements.EntityDescriptor, Namespaces.MetadataNamespace);
        // @entityID
        writer.WriteAttributeString(Attributes.EntityId, configuration.Issuer);
        // @ID
        writer.WriteAttributeString("ID", entityDescriptorId);

        // if (envelopeWriter != null)
        //     envelopeWriter.WriteSignature();

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
        writer.WriteStartElement(Elements.RoleDescriptor);
        writer.WriteAttributeString(IdentityServer4.WsFederation.WsFederationConstants.Xmlns, IdentityServer4.WsFederation.WsFederationConstants.Prefixes.Xsi, null, XmlSignatureConstants.XmlSchemaNamespace);
        writer.WriteAttributeString(IdentityServer4.WsFederation.WsFederationConstants.Xmlns, IdentityServer4.WsFederation.WsFederationConstants.Prefixes.Fed, null, WsFederationConstants.Namespaces.FederationNamespace);
        writer.WriteAttributeString("protocolSupportEnumeration", WsFederationConstants.Namespaces.FederationNamespace);
        writer.WriteStartAttribute(Attributes.Type, XmlSignatureConstants.XmlSchemaNamespace);
        writer.WriteQualifiedName(Types.SecurityTokenServiceType, WsFederationConstants.Namespaces.FederationNamespace);
        writer.WriteEndAttribute();

        WriteKeyDescriptorForSigning(configuration, writer);

        //TODO: rewrite to not using static values, use values from config instead 
        var supportedTokenTypeUris = new[] {
            "http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.1#SAMLV1.1",
            "http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.1#SAMLV2.0"
        };
        writer.WriteStartElement("TokenTypesOffered", WsFederationConstants.Namespaces.FederationNamespace);
        foreach (string tokenTypeUri in supportedTokenTypeUris)
        {
            // <TokenType>
            writer.WriteStartElement("TokenType", WsFederationConstants.Namespaces.FederationNamespace);
            writer.WriteAttributeString("Uri", tokenTypeUri);
            // </TokenType>
            writer.WriteEndElement();
        }
        // </TokenTypesOffered>
        writer.WriteEndElement();

        WriteSecurityTokenEndpoint(configuration, writer);
        WritePassiveRequestorEndpoint(configuration, writer);

        // </RoleDescriptorr>
        writer.WriteEndElement();
    }

    private static void WriteSecurityTokenEndpoint(WsFederationConfiguration configuration, XmlWriter writer)
    {
        // <SecurityTokenServiceEndpoint>
        writer.WriteStartElement(IdentityServer4.WsFederation.WsFederationConstants.Elements.SecurityTokenServiceEndpoint, Namespaces.FederationNamespace);

        // <EndpointReference>
        writer.WriteStartElement(IdentityServer4.WsFederation.WsFederationConstants.Prefixes.Wsa, Elements.EndpointReference, Namespaces.AddressingNamspace);  // EndpointReference

        // <Address>
        writer.WriteStartElement(Elements.Address, Namespaces.AddressingNamspace);
        writer.WriteString(configuration.TokenEndpoint);
        // </Address>
        writer.WriteEndElement();

        // </EndpointReference>
        writer.WriteEndElement();

        // </SecurityTokenServiceEndpoint>
        writer.WriteEndElement();
    }
    
    private static void WritePassiveRequestorEndpoint(WsFederationConfiguration configuration, XmlWriter writer)
    {
        // <PassiveRequestorEndpoint>
        writer.WriteStartElement(IdentityServer4.WsFederation.WsFederationConstants.Elements.PassiveRequestorEndpoint, Namespaces.FederationNamespace);

        // <EndpointReference>
        writer.WriteStartElement(IdentityServer4.WsFederation.WsFederationConstants.Prefixes.Wsa, Elements.EndpointReference, Namespaces.AddressingNamspace);

        // <Address>
        writer.WriteStartElement(Elements.Address, Namespaces.AddressingNamspace);
        writer.WriteString(configuration.TokenEndpoint);
        // </Address>
        writer.WriteEndElement();

        // </EndpointReference>
        writer.WriteEndElement();

        // </PassiveRequestorEndpoint>
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