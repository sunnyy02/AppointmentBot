using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace AppointmentBot.CognitiveModels
{

    public partial class DoctorBooking : IRecognizerConvert
    {
        public string Text;
        public string AlteredText;
        public enum Intent
        {
            BookAppointment,
            Cancel,
            GetAvailableDoctors,
            None
        };
        public Dictionary<Intent, IntentScore> Intents;

        public class _Entities
        {

            // Built-in entities
            public DateTimeSpec[] datetime;

            // Lists
            public string[][] Doctor;

            // Instance
            public class _Instance
            {
                public InstanceData[] datetime;
                public InstanceData[] Doctor;
                public InstanceData[] AvailableDoctors;
            }
            [JsonProperty("$instance")]
            public _Instance _instance;
        }
        public _Entities Entities;

        [JsonExtensionData(ReadData = true, WriteData = true)]
        public IDictionary<string, object> Properties { get; set; }

        public void Convert(dynamic result)
        {
            var app = JsonConvert.DeserializeObject<DoctorBooking>(JsonConvert.SerializeObject(result, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
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
