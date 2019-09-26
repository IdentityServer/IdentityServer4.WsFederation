﻿// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


namespace IdentityServer4.WsFederation
{
    public static class WsFederationConstants
    {
        public static class SamlNameIdentifierFormats
        {
            public const string EmailAddressString = "urn:oasis:names:tc:SAML:1.1:nameid-format:emailAddress";
            public const string EncryptedString = "urn:oasis:names:tc:SAML:2.0:nameid-format:encrypted";
            public const string EntityString = "urn:oasis:names:tc:SAML:2.0:nameid-format:entity";
            public const string KerberosString = "urn:oasis:names:tc:SAML:2.0:nameid-format:kerberos";
            public const string PersistentString = "urn:oasis:names:tc:SAML:2.0:nameid-format:persistent";
            public const string TransientString = "urn:oasis:names:tc:SAML:2.0:nameid-format:transient";
            public const string UnspecifiedString = "urn:oasis:names:tc:SAML:1.1:nameid-format:unspecified";
            public const string WindowsDomainQualifiedNameString = "urn:oasis:names:tc:SAML:1.1:nameid-format:WindowsDomainQualifiedName";
            public const string X509SubjectNameString = "urn:oasis:names:tc:SAML:1.1:nameid-format:X509SubjectName";
        }

        public static class TokenTypes
        {
            public const string JsonWebToken = "urn:ietf:params:oauth:token-type:jwt";
            public const string Kerberos = "http://schemas.microsoft.com/ws/2006/05/identitymodel/tokens/Kerberos";
            public const string OasisWssSaml11TokenProfile11 = "http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.1#SAMLV1.1";
            public const string OasisWssSaml2TokenProfile11 = "http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.1#SAMLV2.0";
            public const string Rsa = "http://schemas.microsoft.com/ws/2006/05/identitymodel/tokens/Rsa";
            public const string Saml11TokenProfile11 = "urn:oasis:names:tc:SAML:1.0:assertion";
            public const string Saml2TokenProfile11 = "urn:oasis:names:tc:SAML:2.0:assertion";
            public const string SimpleWebToken = "http://schemas.xmlsoap.org/ws/2009/11/swt-token-profile-1.0";
            public const string UserName = "http://schemas.microsoft.com/ws/2006/05/identitymodel/tokens/UserName";
            public const string X509Certificate = "http://schemas.microsoft.com/ws/2006/05/identitymodel/tokens/X509Certificate";
        }

        public static class Attributes 
        {
            public const string ProtocolSupportEnumeration = "protocolSupportEnumeration";
            public const string Context = "Context";
            public const string TokenTypesOffered = "TokenTypesOffered";
            public const string TokenType = "TokenType";
            public const string Uri = "Uri";
        }

        public static class Elements 
        {
            public const string SecurityTokenServiceEndpoint = "SecurityTokenServiceEndpoint";
            public const string RequestSecurityTokenResponseCollection = "RequestSecurityTokenResponseCollection";
            public const string PassiveRequestorEndpoint = "PassiveRequestorEndpoint";
        }

        public const string Xmlns = "xmlns";

        //TODO: remove this when https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/issues/1119 is fixed and merged
        public static class WsUtility 
        {
            public const string Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
        }

        public static class Prefixes
        {
            public const string Xsi = "xsi";
        }
    }
}