﻿//  Copyright 2015 Stefan Negritoiu (FreeBusy). See LICENSE file for more information.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AlexaSkillsKit.Helpers;
using AlexaSkillsKit.Speechlet;
using AlexaSkillsKit.Slu;

namespace AlexaSkillsKit.Json
{
    public class SpeechletRequestEnvelope
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static SpeechletRequestEnvelope FromJson(string content) {
            if (String.IsNullOrEmpty(content)) {
                throw new SpeechletException("Request content is empty");
            }

            JObject json = JsonConvert.DeserializeObject<JObject>(content, Sdk.DeserializationSettings);
            return FromJson(json);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static SpeechletRequestEnvelope FromJson(JObject json) {
            if (json["version"] != null && json["version"].Value<string>() != Sdk.VERSION) {
                throw new SpeechletException("Request must conform to 1.0 schema.");
            }

            SpeechletRequest request;
            var requestJson = json.Value<JObject>("request");
            var requestType = requestJson?.Value<string>("type");
            var requestId = requestJson?.Value<string>("requestId");
            var timestamp = DateTimeHelpers.FromAlexaTimestamp(requestJson);
            var locale = requestJson?.Value<string>("locale");
            switch (requestType) {
                case "LaunchRequest":
                    request = new LaunchRequest(requestId, timestamp, locale);
                    break;
                case "IntentRequest":
                    IntentRequest.DialogStateEnum dialogState = IntentRequest.DialogStateEnum.NONE;
                    Enum.TryParse(requestJson.Value<string>("dialogState"), out dialogState);
                    var intent = Intent.FromJson(requestJson.Value<JObject>("intent"));
                    request = new IntentRequest(requestId, timestamp, locale, intent, dialogState);
                    break;
                case "SessionStartedRequest":
                    request = new SessionStartedRequest(requestId, timestamp, locale);
                    break;
                case "SessionEndedRequest":
                    SessionEndedRequest.ReasonEnum reason = SessionEndedRequest.ReasonEnum.NONE;
                    Enum.TryParse(requestJson.Value<string>("reason"), out reason);
                    request = new SessionEndedRequest(requestId, timestamp, locale, reason);
                    break;
                default:
                    if (requestType.StartsWith("PlaybackController") || requestType.StartsWith("AudioPlayer")) {
                        var token = requestJson?.Value<string>("token");
                        var offset = requestJson?.Value<long>("offsetInMilliseconds") ?? 0;
                        request = new AudioPlayerRequest(requestId, timestamp, locale, token, offset, requestType);
                    }
                    else if (requestType == "System.ExceptionEncountered")
                        request = null;
                    else
                        throw new ArgumentException("json");
                    break;
            }

            return new SpeechletRequestEnvelope {
                Request = request,
                Session = Session.FromJson(json.Value<JObject>("session")),
                Version = json.Value<string>("version"),
                Context = Context.FromJson(json.Value<JObject>("context"))
            };
        }


        public virtual SpeechletRequest Request {
            get;
            set;
        }

        public virtual Session Session {
            get;
            set;
        }

        public virtual string Version {
            get;
            set;
        }

        public virtual Context Context {
            get;
            set;
        }
    }
}