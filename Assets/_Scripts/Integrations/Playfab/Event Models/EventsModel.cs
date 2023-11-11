using System.Collections.Generic;
using PlayFab.EventsModels;

namespace CosmicShore._Core.Playfab.Event_Models
{
    public class EventsModel
    {
        public List<EventContents> EventContents { get; set; }
        public Dictionary<string, string> CustomTags { get; set; }
    }
}