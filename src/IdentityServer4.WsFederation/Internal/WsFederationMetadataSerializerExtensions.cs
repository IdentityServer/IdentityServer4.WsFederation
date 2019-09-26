
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
            envelopeWriter = new EnvelopedSignatureWriter(
                writer, 
                configuration.SigningCredentials,
                "#" + entityDescriptorId); 
            writer = envelopeWriter;
        }

        writer.WriteStartDocument();

        // <EntityDescriptor>
        writer.WriteStartElement(Elements.EntityDescriptor, WsFederationConstants.MetadataNamespace);
        // @entityID
        writer.WriteAttributeString(Attributes.EntityId, configuration.Issuer);
        // @ID
        writer.WriteAttributeString(Attributes.Id, entityDescriptorId);

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
        writer.WriteAttributeString(IdentityServer4.WsFederation.WsFederationConstants.Xmlns, WsFederationConstants.PreferredPrefix, null, WsFederationConstants.Namespace);
        writer.WriteAttributeString(IdentityServer4.WsFederation.WsFederationConstants.Attributes.ProtocolSupportEnumeration, WsFederationConstants.Namespace);
        writer.WriteStartAttribute(Attributes.Type, XmlSignatureConstants.XmlSchemaNamespace);
        writer.WriteQualifiedName(Types.SecurityTokenServiceType, WsFederationConstants.Namespace);
        writer.WriteEndAttribute();

        WriteKeyDescriptorForSigning(configuration, writer);

        //TODO: rewrite to not using static values, use values from config instead 
        var supportedTokenTypeUris = new[] {
            "http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.1#SAMLV1.1",
            "http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.1#SAMLV2.0"
        };
        writer.WriteStartElement(IdentityServer4.WsFederation.WsFederationConstants.Attributes.TokenTypesOffered, WsFederationConstants.Namespace);
        foreach (string tokenTypeUri in supportedTokenTypeUris)
        {
            // <TokenType>
            writer.WriteStartElement(IdentityServer4.WsFederation.WsFederationConstants.Attributes.TokenType, WsFederationConstants.Namespace);
            writer.WriteAttributeString(IdentityServer4.WsFederation.WsFederationConstants.Attributes.Uri, tokenTypeUri);
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
        writer.WriteStartElement(IdentityServer4.WsFederation.WsFederationConstants.Elements.SecurityTokenServiceEndpoint, WsFederationConstants.Namespace);

        // <EndpointReference>
        writer.WriteStartElement(WsAddressing.PreferredPrefix, WsAddressing.Elements.EndpointReference, WsAddressing.Namespace);  // EndpointReference

        // <Address>
        writer.WriteStartElement(WsAddressing.Elements.Address, WsAddressing.Namespace);
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
        writer.WriteStartElement(IdentityServer4.WsFederation.WsFederationConstants.Elements.PassiveRequestorEndpoint, WsFederationConstants.Namespace);

        // <EndpointReference>
        writer.WriteStartElement(WsAddressing.PreferredPrefix, WsAddressing.Elements.EndpointReference, WsAddressing.Namespace);

        // <Address>
        writer.WriteStartElement(WsAddressing.Elements.Address, WsAddressing.Namespace);
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
        writer.WriteStartElement(Elements.KeyDescriptor, WsFederationConstants.MetadataNamespace);
        writer.WriteAttributeString(Attributes.Use, WsFederationConstants.KeyUse.Signing);

        var dsigSerializer = new DSigSerializer();
        foreach (var keyInfo in configuration.KeyInfos)
        {
            dsigSerializer.WriteKeyInfo(writer, keyInfo);
        }

        // </KeyDescriptor>
        writer.WriteEndElement();
    }
}