// <auto-generated>
// Code generated by LUISGen FAConnect.json -cs Luis.FAConnect -o 
// Tool github: https://github.com/microsoft/botbuilder-tools
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
namespace Luis
{
    public partial class FAConnect: IRecognizerConvert
    {
        [JsonProperty("text")]
        public string Text;

        [JsonProperty("alteredText")]
        public string AlteredText;

        public enum Intent {
            Details, 
            None, 
            Summary
        };
        [JsonProperty("intents")]
        public Dictionary<Intent, IntentScore> Intents;

        public class _Entities
        {
            // Simple entities
            public string[] PortfolioValue;

            public string[] RetirementAge;

            public string[] clientName;

            public string[] clientage;

            public string[] criticalalerts;

            public string[] planName;

            public string[] planstatus;

            // Built-in entities
            public Age[] age;
            public Age[] current_age;

            public DateTimeSpec[] datetime;

            public string[] email;

            public double[] ordinal;

            public string[] personName;

            public string[] phonenumber;

            // Lists
            public string[][] Accounts;

            public string[][] status;

            // Instance
            public class _Instance
            {
                public InstanceData[] Accounts;
                public InstanceData[] PortfolioValue;
                public InstanceData[] RetirementAge;
                public InstanceData[] age;
                public InstanceData[] current_age;
                public InstanceData[] clientName;
                public InstanceData[] clientage;
                public InstanceData[] criticalalerts;
                public InstanceData[] datetime;
                public InstanceData[] email;
                public InstanceData[] ordinal;
                public InstanceData[] personName;
                public InstanceData[] phonenumber;
                public InstanceData[] planName;
                public InstanceData[] planstatus;
                public InstanceData[] status;
            }
            [JsonProperty("$instance")]
            public _Instance _instance;
        }
        [JsonProperty("entities")]
        public _Entities Entities;

        [JsonExtensionData(ReadData = true, WriteData = true)]
        public IDictionary<string, object> Properties {get; set; }

        public void Convert(dynamic result)
        {
            var app = JsonConvert.DeserializeObject<FAConnect>(JsonConvert.SerializeObject(result, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            Text = app.Text;
            AlteredText = app.AlteredText;
            Intents = app.Intents;
            Entities = app.Entities;
            Properties = app.Properties;
        }

        public (Intent intent, double score) TopIntent()
        {
            Intent maxIntent = Intent.None;
            var max = 0.0;
            foreach (var entry in Intents)
            {
                if (entry.Value.Score > max)
                {
                    maxIntent = entry.Key;
                    max = entry.Value.Score.Value;
                }
            }
            return (maxIntent, max);
        }
    }
}
