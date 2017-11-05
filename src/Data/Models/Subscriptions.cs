namespace PokeFilterBot.Data.Models
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    [XmlRoot("subscriptions")]
    public class Subscriptions : List<Subscription>
    {
        public Subscription this[ulong userId]
        {
            get { return Find(x => x.UserId == userId); }
        }

        public bool ContainsKey(ulong userId)
        {
            var sub = this[userId];
            return sub != null;
        }

        public bool Remove(ulong userId)
        {
            if (ContainsKey(userId))
            {
                return Remove(this[userId]);
            }

            return false;
        }
    }
}