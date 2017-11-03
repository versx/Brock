namespace PokeFilterBot
{
    using System;
    using System.Xml.Serialization;

    [XmlRoot("pokemon")]
    public class Pokemon
    {
        [XmlAttribute("index")]
        public uint Index { get; set; }

        [XmlAttribute("name")]
        public string Name { get; set; }


        public Pokemon()
        {
        }

        public Pokemon(uint index, string name)
        {
            Index = index;
            Name = name;
        }
    }
}