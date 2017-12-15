// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using Microsoft.IdentityModel.Protocols.WsFederation;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Tokens.Saml;

namespace IdentityServer4.WsFederation
{
    internal class RequestSecurityTokenResponse
    {
        public DateTime CreatedAt { get;set; }
        public DateTime ExpiresAt { get;set; }
        public string AppliesTo { get; set; }
        public string Context { get; set; }
        public string ReplyTo { get; set; }
        public SecurityToken RequestedSecurityToken { get; set; }
        public SecurityTokenHandler SecurityTokenHandler { get; set; }

        public string Serialize()
        {
            var ms = new MemoryStream();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            settings.Encoding = new UTF8Encoding(false);
            XmlWriter writer = XmlWriter.Create(ms, settings);

            // <t:RequestSecurityTokenResponseCollection>
            //TODO: check if collection is required
            writer.WriteStartElement(WsFederationConstants.Prefixes.Trust, WsFederationConstants.Elements.RequestSecurityTokenResponseCollection, WsTrustConstants.Namespaces.WsTrust1_3);
            // <t:RequestSecurityTokenResponse>
            writer.WriteStartElement(WsFederationConstants.Prefixes.Trust, WsTrustConstants.Elements.RequestSecurityTokenResponse, WsTrustConstants.Namespaces.WsTrust1_3);
            // @Context
            writer.WriteAttributeString(WsFederationConstants.Attribures.Context, Context);

            // <t:Lifetime>
            writer.WriteStartElement(WsTrustConstants.Elements.Lifetime, WsTrustConstants.Namespaces.WsTrust1_3);

            // <wsu:Created></wsu:Created>
            writer.WriteElementString(IdentityServer4.WsFederation.WsFederationConstants.Prefixes.Wsu, WsTrustConstants.Elements.Created, WsTrustConstants.Namespaces.Utility, CreatedAt.ToString(SamlConstants.GeneratedDateTimeFormat, DateTimeFormatInfo.InvariantInfo));
            // <wsu:Expires></wsu:Expires>
            writer.WriteElementString(IdentityServer4.WsFederation.WsFederationConstants.Prefixes.Wsu, WsTrustConstants.Elements.Expires, WsTrustConstants.Namespaces.Utility, ExpiresAt.ToString(SamlConstants.GeneratedDateTimeFormat, DateTimeFormatInfo.InvariantInfo));

            // </t:Lifetime>
            writer.WriteEndElement();

            // <wsp:AppliesTo>
            writer.WriteStartElement(IdentityServer4.WsFederation.WsFederationConstants.Prefixes.Wsp, WsTrustConstants.Elements.AppliesTo, WsTrustConstants.Namespaces.WsPolicy);

            // <wsa:EndpointReference>
            writer.WriteStartElement(IdentityServer4.WsFederation.WsFederationConstants.Prefixes.Wsa, WsTrustConstants.Elements.EndpointReference, WsTrustConstants.Namespaces.AddressingNamspace);

            // <wsa:Address></wsa:Address>
            writer.WriteElementString(IdentityServer4.WsFederation.WsFederationConstants.Prefixes.Wsa, WsTrustConstants.Elements.Address, WsTrustConstants.Namespaces.AddressingNamspace, AppliesTo);

            writer.WriteEndElement();
            // </wsa:EndpointReference>

            writer.WriteEndElement();
            // </wsp:AppliesTo>

            // <t:RequestedSecurityToken>
            writer.WriteStartElement(WsTrustConstants.Elements.RequestedSecurityToken, WsTrustConstants.Namespaces.WsTrust1_3);

            // write assertion
            SecurityTokenHandler.WriteToken(writer, RequestedSecurityToken);

            // </t:RequestedSecurityToken>
            writer.WriteEndElement();

            // </t:RequestSecurityTokenResponse>
            writer.WriteEndElement();

            // <t:RequestSecurityTokenResponseCollection>
            writer.WriteEndElement();

            writer.Flush();
            ms.Position = 0;
            var result = Encoding.UTF8.GetString(ms.ToArray());
            return result;
        }
    }
}