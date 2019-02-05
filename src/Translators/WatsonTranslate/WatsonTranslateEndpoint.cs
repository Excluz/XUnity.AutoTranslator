﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using SimpleJSON;
using UnityEngine;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Configuration;
using XUnity.AutoTranslator.Plugin.Core.Constants;
using XUnity.AutoTranslator.Plugin.Core.Endpoints;
using XUnity.AutoTranslator.Plugin.Core.Endpoints.Www;
using XUnity.AutoTranslator.Plugin.Core.Extensions;
using XUnity.AutoTranslator.Plugin.Core.Utilities;
using XUnity.AutoTranslator.Plugin.Core.Web;

namespace WatsonTranslate
{
   internal class WatsonTranslateEndpoint : WwwEndpoint
   {
      private static readonly string RequestTemplate = "{{\"text\":[\"{2}\"],\"model_id\":\"{0}-{1}\"}}";

      private string _fullUrl;
      private string _url;
      private string _key;

      public WatsonTranslateEndpoint()
      {
      }

      public override string Id => "WatsonTranslate";

      public override string FriendlyName => "Watson Language Translator";

      public override void Initialize( IInitializationContext context )
      {
         _url = context.GetOrCreateSetting( "Watson", "Url", "" );
         _key = context.GetOrCreateSetting( "Watson", "Key", "" );
         if( string.IsNullOrEmpty( _url ) ) throw new Exception( "The WatsonTranslate endpoint requires a url which has not been provided." );
         if( string.IsNullOrEmpty( _key ) ) throw new Exception( "The WatsonTranslate endpoint requires a key which has not been provided." );

         _fullUrl = _url.TrimEnd( '/' ) + "/v3/translate?version=2018-05-01";

         if( context.SourceLanguage != "ja" ) throw new Exception( "Current implementation only supports japanese-to-english." );
         if( context.DestinationLanguage != "en" ) throw new Exception( "Current implementation only supports japanese-to-english." );
      }

      public override void OnCreateRequest( IWwwRequestCreationContext context )
      {
         var request = new WwwRequestInfo(
            _fullUrl,
            string.Format(
               RequestTemplate,
               context.SourceLanguage,
               context.DestinationLanguage,
               TextHelper.EscapeJson( context.UntranslatedText ) ) );

         request.Headers[ "User-Agent" ] = string.IsNullOrEmpty( AutoTranslatorSettings.UserAgent ) ? "curl/7.55.1" : AutoTranslatorSettings.UserAgent;
         request.Headers[ "Accept" ] = "application/json";
         request.Headers[ "Content-Type" ] = "application/json";
         request.Headers[ "Authorization" ] = "Basic " + Convert.ToBase64String( Encoding.ASCII.GetBytes( "apikey:" + _key ) );

         context.Complete( request );
      }

      public override void OnExtractTranslation( IWwwTranslationExtractionContext context )
      {
         var data = context.ResponseData;
         var obj = JSON.Parse( data );
         var lineBuilder = new StringBuilder( data.Length );

         foreach( JSONNode entry in obj.AsObject[ "translations" ].AsArray )
         {
            var token = entry.AsObject[ "translation" ].ToString();
            token = token.Substring( 1, token.Length - 2 ).UnescapeJson();

            if( !lineBuilder.EndsWithWhitespaceOrNewline() ) lineBuilder.Append( "\n" );

            lineBuilder.Append( token );
         }
         var translated = lineBuilder.ToString();

         context.Complete( translated );
      }
   }
}