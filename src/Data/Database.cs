﻿namespace BrockBot.Data
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml.Serialization;

    using Newtonsoft.Json;

    using BrockBot.Data.Models;
    using BrockBot.Serialization;

    [XmlRoot("database")]
    [JsonObject("database")]
    public class Database
    {
        /// <summary>
        /// The default config file name with extension.
        /// </summary>
        public const string DefaultDatabaseFileName = "database.json";//"Database.xml";

        #region Properties

        //[XmlElement("subscriptions")]
        //public Subscriptions Subscriptions { get; }

        //[XmlArrayItem("lobby")]
        //[XmlArray("lobbies")]
        //public List<RaidLobby> Lobbies { get; }

        [XmlArrayItem("server")]
        [XmlArray("servers")]
        [JsonProperty("servers")]
        public List<Server> Servers { get; set; }

        [XmlIgnore]
        [JsonIgnore]
        public List<Pokemon> Pokemon { get; }

        /// <summary>
        /// Gets the config full config file path.
        /// </summary>
        [XmlIgnore]
        [JsonIgnore]
        public static string ConfigFilePath
        {
            get
            {
                return Path.Combine
                (
                    Directory.GetCurrentDirectory(),
                    DefaultDatabaseFileName
                );
            }
        }

        public Server this[ulong guildId]
        {
            get
            {
                if (ContainsKey(guildId))
                {
                    return Servers.Find(x => x.GuildId == guildId);
                }

                return null;
            }
        }

        #endregion

        #region Constructor

        public Database()
        {
            //Lobbies = new List<RaidLobby>();
            //Subscriptions = new Subscriptions();
            Servers = new List<Server>();
            Pokemon = new List<Pokemon>
            {
new Pokemon(001, "Bulbasaur"),
new Pokemon(002, "Ivysaur"),
new Pokemon(003, "Venusaur"),
new Pokemon(004, "Charmander"),
new Pokemon(005, "Charmeleon"),
new Pokemon(006, "Charizard"),
new Pokemon(007, "Squirtle"),
new Pokemon(008, "Wartortle"),
new Pokemon(009, "Blastoise"),
new Pokemon(010, "Caterpie"),
new Pokemon(011, "Metapod"),
new Pokemon(012, "Butterfree"),
new Pokemon(013, "Weedle"),
new Pokemon(014, "Kakuna"),
new Pokemon(015, "Beedrill"),
new Pokemon(016, "Pidgey"),
new Pokemon(017, "Pidgeotto"),
new Pokemon(018, "Pidgeot"),
new Pokemon(019, "Rattata"),
new Pokemon(020, "Raticate"),
new Pokemon(021, "Spearow"),
new Pokemon(022, "Fearow"),
new Pokemon(023, "Ekans"),
new Pokemon(024, "Arbok"),
new Pokemon(025, "Pikachu"),
new Pokemon(026, "Raichu"),
new Pokemon(027, "Sandshrew"),
new Pokemon(028, "Sandslash"),
new Pokemon(029, "Nidoran♀"),
new Pokemon(030, "Nidorina"),
new Pokemon(031, "Nidoqueen"),
new Pokemon(032, "Nidoran♂"),
new Pokemon(033, "Nidorino"),
new Pokemon(034, "Nidoking"),
new Pokemon(035, "Clefairy"),
new Pokemon(036, "Clefable"),
new Pokemon(037, "Vulpix"),
new Pokemon(038, "Ninetales"),
new Pokemon(039, "Jigglypuff"),
new Pokemon(040, "Wigglytuff"),
new Pokemon(041, "Zubat"),
new Pokemon(042, "Golbat"),
new Pokemon(043, "Oddish"),
new Pokemon(044, "Gloom"),
new Pokemon(045, "Vileplume"),
new Pokemon(046, "Paras"),
new Pokemon(047, "Parasect"),
new Pokemon(048, "Venonat"),
new Pokemon(049, "Venomoth"),
new Pokemon(050, "Diglett"),
new Pokemon(051, "Dugtrio"),
new Pokemon(052, "Meowth"),
new Pokemon(053, "Persian"),
new Pokemon(054, "Psyduck"),
new Pokemon(055, "Golduck"),
new Pokemon(056, "Mankey"),
new Pokemon(057, "Primeape"),
new Pokemon(058, "Growlithe"),
new Pokemon(059, "Arcanine"),
new Pokemon(060, "Poliwag"),
new Pokemon(061, "Poliwhirl"),
new Pokemon(062, "Poliwrath"),
new Pokemon(063, "Abra"),
new Pokemon(064, "Kadabra"),
new Pokemon(065, "Alakazam"),
new Pokemon(066, "Machop"),
new Pokemon(067, "Machoke"),
new Pokemon(068, "Machamp"),
new Pokemon(069, "Bellsprout"),
new Pokemon(070, "Weepinbell"),
new Pokemon(071, "Victreebel"),
new Pokemon(072, "Tentacool"),
new Pokemon(073, "Tentacruel"),
new Pokemon(074, "Geodude"),
new Pokemon(075, "Graveler"),
new Pokemon(076, "Golem"),
new Pokemon(077, "Ponyta"),
new Pokemon(078, "Rapidash"),
new Pokemon(079, "Slowpoke"),
new Pokemon(080, "Slowbro"),
new Pokemon(081, "Magnemite"),
new Pokemon(082, "Magneton"),
new Pokemon(083, "Farfetch'd"),
new Pokemon(084, "Doduo"),
new Pokemon(085, "Dodrio"),
new Pokemon(086, "Seel"),
new Pokemon(087, "Dewgong"),
new Pokemon(088, "Grimer"),
new Pokemon(089, "Muk"),
new Pokemon(090, "Shellder"),
new Pokemon(091, "Cloyster"),
new Pokemon(092, "Gastly"),
new Pokemon(093, "Haunter"),
new Pokemon(094, "Gengar"),
new Pokemon(095, "Onix"),
new Pokemon(096, "Drowzee"),
new Pokemon(097, "Hypno"),
new Pokemon(098, "Krabby"),
new Pokemon(099, "Kingler"),
new Pokemon(100, "Voltorb"),
new Pokemon(101, "Electrode"),
new Pokemon(102, "Exeggcute"),
new Pokemon(103, "Exeggutor"),
new Pokemon(104, "Cubone"),
new Pokemon(105, "Marowak"),
new Pokemon(106, "Hitmonlee"),
new Pokemon(107, "Hitmonchan"),
new Pokemon(108, "Lickitung"),
new Pokemon(109, "Koffing"),
new Pokemon(110, "Weezing"),
new Pokemon(111, "Rhyhorn"),
new Pokemon(112, "Rhydon"),
new Pokemon(113, "Chansey"),
new Pokemon(114, "Tangela"),
new Pokemon(115, "Kangaskhan"),
new Pokemon(116, "Horsea"),
new Pokemon(117, "Seadra"),
new Pokemon(118, "Goldeen"),
new Pokemon(119, "Seaking"),
new Pokemon(120, "Staryu"),
new Pokemon(121, "Starmie"),
new Pokemon(122, "Mr. Mime"),
new Pokemon(123, "Scyther"),
new Pokemon(124, "Jynx"),
new Pokemon(125, "Electabuzz"),
new Pokemon(126, "Magmar"),
new Pokemon(127, "Pinsir"),
new Pokemon(128, "Tauros"),
new Pokemon(129, "Magikarp"),
new Pokemon(130, "Gyarados"),
new Pokemon(131, "Lapras"),
new Pokemon(132, "Ditto"),
new Pokemon(133, "Eevee"),
new Pokemon(134, "Vaporeon"),
new Pokemon(135, "Jolteon"),
new Pokemon(136, "Flareon"),
new Pokemon(137, "Porygon"),
new Pokemon(138, "Omanyte"),
new Pokemon(139, "Omastar"),
new Pokemon(140, "Kabuto"),
new Pokemon(141, "Kabutops"),
new Pokemon(142, "Aerodactyl"),
new Pokemon(143, "Snorlax"),
new Pokemon(144, "Articuno"),
new Pokemon(145, "Zapdos"),
new Pokemon(146, "Moltres"),
new Pokemon(147, "Dratini"),
new Pokemon(148, "Dragonair"),
new Pokemon(149, "Dragonite"),
new Pokemon(150, "Mewtwo"),
new Pokemon(151, "Mew"),
new Pokemon(152, "Chikorita"),
new Pokemon(153, "Bayleef"),
new Pokemon(154, "Meganium"),
new Pokemon(155, "Cyndaquil"),
new Pokemon(156, "Quilava"),
new Pokemon(157, "Typhlosion"),
new Pokemon(158, "Totodile"),
new Pokemon(159, "Croconaw"),
new Pokemon(160, "Feraligatr"),
new Pokemon(161, "Sentret"),
new Pokemon(162, "Furret"),
new Pokemon(163, "Hoothoot"),
new Pokemon(164, "Noctowl"),
new Pokemon(165, "Ledyba"),
new Pokemon(166, "Ledian"),
new Pokemon(167, "Spinarak"),
new Pokemon(168, "Ariados"),
new Pokemon(169, "Crobat"),
new Pokemon(170, "Chinchou"),
new Pokemon(171, "Lanturn"),
new Pokemon(172, "Pichu"),
new Pokemon(173, "Cleffa"),
new Pokemon(174, "Igglybuff"),
new Pokemon(175, "Togepi"),
new Pokemon(176, "Togetic"),
new Pokemon(177, "Natu"),
new Pokemon(178, "Xatu"),
new Pokemon(179, "Mareep"),
new Pokemon(180, "Flaaffy"),
new Pokemon(181, "Ampharos"),
new Pokemon(182, "Bellossom"),
new Pokemon(183, "Marill"),
new Pokemon(184, "Azumarill"),
new Pokemon(185, "Sudowoodo"),
new Pokemon(186, "Politoed"),
new Pokemon(187, "Hoppip"),
new Pokemon(188, "Skiploom"),
new Pokemon(189, "Jumpluff"),
new Pokemon(190, "Aipom"),
new Pokemon(191, "Sunkern"),
new Pokemon(192, "Sunflora"),
new Pokemon(193, "Yanma"),
new Pokemon(194, "Wooper"),
new Pokemon(195, "Quagsire"),
new Pokemon(196, "Espeon"),
new Pokemon(197, "Umbreon"),
new Pokemon(198, "Murkrow"),
new Pokemon(199, "Slowking"),
new Pokemon(200, "Misdreavus"),
new Pokemon(201, "Unown"),
new Pokemon(202, "Wobbuffet"),
new Pokemon(203, "Girafarig"),
new Pokemon(204, "Pineco"),
new Pokemon(205, "Forretress"),
new Pokemon(206, "Dunsparce"),
new Pokemon(207, "Gligar"),
new Pokemon(208, "Steelix"),
new Pokemon(209, "Snubbull"),
new Pokemon(210, "Granbull"),
new Pokemon(211, "Qwilfish"),
new Pokemon(212, "Scizor"),
new Pokemon(213, "Shuckle"),
new Pokemon(214, "Heracross"),
new Pokemon(215, "Sneasel"),
new Pokemon(216, "Teddiursa"),
new Pokemon(217, "Ursaring"),
new Pokemon(218, "Slugma"),
new Pokemon(219, "Magcargo"),
new Pokemon(220, "Swinub"),
new Pokemon(221, "Piloswine"),
new Pokemon(222, "Corsola"),
new Pokemon(223, "Remoraid"),
new Pokemon(224, "Octillery"),
new Pokemon(225, "Delibird"),
new Pokemon(226, "Mantine"),
new Pokemon(227, "Skarmory"),
new Pokemon(228, "Houndour"),
new Pokemon(229, "Houndoom"),
new Pokemon(230, "Kingdra"),
new Pokemon(231, "Phanpy"),
new Pokemon(232, "Donphan"),
new Pokemon(233, "Porygon2"),
new Pokemon(234, "Stantler"),
new Pokemon(235, "Smeargle"),
new Pokemon(236, "Tyrogue"),
new Pokemon(237, "Hitmontop"),
new Pokemon(238, "Smoochum"),
new Pokemon(239, "Elekid"),
new Pokemon(240, "Magby"),
new Pokemon(241, "Miltank"),
new Pokemon(242, "Blissey"),
new Pokemon(243, "Raikou"),
new Pokemon(244, "Entei"),
new Pokemon(245, "Suicune"),
new Pokemon(246, "Larvitar"),
new Pokemon(247, "Pupitar"),
new Pokemon(248, "Tyranitar"),
new Pokemon(249, "Lugia"),
new Pokemon(250, "Ho-Oh"),
new Pokemon(251, "Celebi"),
new Pokemon(252, "Treecko"),
new Pokemon(253, "Grovyle"),
new Pokemon(254, "Sceptile"),
new Pokemon(255, "Torchic"),
new Pokemon(256, "Combusken"),
new Pokemon(257, "Blaziken"),
new Pokemon(258, "Mudkip"),
new Pokemon(259, "Marshtomp"),
new Pokemon(260, "Swampert"),
new Pokemon(261, "Poochyena"),
new Pokemon(262, "Mightyena"),
new Pokemon(263, "Zigzagoon"),
new Pokemon(264, "Linoone"),
new Pokemon(265, "Wurmple"),
new Pokemon(266, "Silcoon"),
new Pokemon(267, "Beautifly"),
new Pokemon(268, "Cascoon"),
new Pokemon(269, "Dustox"),
new Pokemon(270, "Lotad"),
new Pokemon(271, "Lombre"),
new Pokemon(272, "Ludicolo"),
new Pokemon(273, "Seedot"),
new Pokemon(274, "Nuzleaf"),
new Pokemon(275, "Shiftry"),
new Pokemon(276, "Taillow"),
new Pokemon(277, "Swellow"),
new Pokemon(278, "Wingull"),
new Pokemon(279, "Pelipper"),
new Pokemon(280, "Ralts"),
new Pokemon(281, "Kirlia"),
new Pokemon(282, "Gardevoir"),
new Pokemon(283, "Surskit"),
new Pokemon(284, "Masquerain"),
new Pokemon(285, "Shroomish"),
new Pokemon(286, "Breloom"),
new Pokemon(287, "Slakoth"),
new Pokemon(288, "Vigoroth"),
new Pokemon(289, "Slaking"),
new Pokemon(290, "Nincada"),
new Pokemon(291, "Ninjask"),
new Pokemon(292, "Shedinja"),
new Pokemon(293, "Whismur"),
new Pokemon(294, "Loudred"),
new Pokemon(295, "Exploud"),
new Pokemon(296, "Makuhita"),
new Pokemon(297, "Hariyama"),
new Pokemon(298, "Azurill"),
new Pokemon(299, "Nosepass"),
new Pokemon(300, "Skitty"),
new Pokemon(301, "Delcatty"),
new Pokemon(302, "Sableye"),
new Pokemon(303, "Mawile"),
new Pokemon(304, "Aron"),
new Pokemon(305, "Lairon"),
new Pokemon(306, "Aggron"),
new Pokemon(307, "Meditite"),
new Pokemon(308, "Medicham"),
new Pokemon(309, "Electrike"),
new Pokemon(310, "Manectric"),
new Pokemon(311, "Plusle"),
new Pokemon(312, "Minun"),
new Pokemon(313, "Volbeat"),
new Pokemon(314, "Illumise"),
new Pokemon(315, "Roselia"),
new Pokemon(316, "Gulpin"),
new Pokemon(317, "Swalot"),
new Pokemon(318, "Carvanha"),
new Pokemon(319, "Sharpedo"),
new Pokemon(320, "Wailmer"),
new Pokemon(321, "Wailord"),
new Pokemon(322, "Numel"),
new Pokemon(323, "Camerupt"),
new Pokemon(324, "Torkoal"),
new Pokemon(325, "Spoink"),
new Pokemon(326, "Grumpig"),
new Pokemon(327, "Spinda"),
new Pokemon(328, "Trapinch"),
new Pokemon(329, "Vibrava"),
new Pokemon(330, "Flygon"),
new Pokemon(331, "Cacnea"),
new Pokemon(332, "Cacturne"),
new Pokemon(333, "Swablu"),
new Pokemon(334, "Altaria"),
new Pokemon(335, "Zangoose"),
new Pokemon(336, "Seviper"),
new Pokemon(337, "Lunatone"),
new Pokemon(338, "Solrock"),
new Pokemon(339, "Barboach"),
new Pokemon(340, "Whiscash"),
new Pokemon(341, "Corphish"),
new Pokemon(342, "Crawdaunt"),
new Pokemon(343, "Baltoy"),
new Pokemon(344, "Claydol"),
new Pokemon(345, "Lileep"),
new Pokemon(346, "Cradily"),
new Pokemon(347, "Anorith"),
new Pokemon(348, "Armaldo"),
new Pokemon(349, "Feebas"),
new Pokemon(350, "Milotic"),
new Pokemon(351, "Castform"),
new Pokemon(352, "Kecleon"),
new Pokemon(353, "Shuppet"),
new Pokemon(354, "Banette"),
new Pokemon(355, "Duskull"),
new Pokemon(356, "Dusclops"),
new Pokemon(357, "Tropius"),
new Pokemon(358, "Chimecho"),
new Pokemon(359, "Absol"),
new Pokemon(360, "Wynaut"),
new Pokemon(361, "Snorunt"),
new Pokemon(362, "Glalie"),
new Pokemon(363, "Spheal"),
new Pokemon(364, "Sealeo"),
new Pokemon(365, "Walrein"),
new Pokemon(366, "Clamperl"),
new Pokemon(367, "Huntail"),
new Pokemon(368, "Gorebyss"),
new Pokemon(369, "Relicanth"),
new Pokemon(370, "Luvdisc"),
new Pokemon(371, "Bagon"),
new Pokemon(372, "Shelgon"),
new Pokemon(373, "Salamence"),
new Pokemon(374, "Beldum"),
new Pokemon(375, "Metang"),
new Pokemon(376, "Metagross"),
new Pokemon(377, "Regirock"),
new Pokemon(378, "Regice"),
new Pokemon(379, "Registeel"),
new Pokemon(380, "Latias"),
new Pokemon(381, "Latios"),
new Pokemon(382, "Kyogre"),
new Pokemon(383, "Groudon"),
new Pokemon(384, "Rayquaza"),
new Pokemon(385, "Jirachi"),
new Pokemon(386, "Deoxys"),
new Pokemon(387, "Turtwig"),
new Pokemon(388, "Grotle"),
new Pokemon(389, "Torterra"),
new Pokemon(390, "Chimchar"),
new Pokemon(391, "Monferno"),
new Pokemon(392, "Infernape"),
new Pokemon(393, "Piplup"),
new Pokemon(394, "Prinplup"),
new Pokemon(395, "Empoleon"),
new Pokemon(396, "Starly"),
new Pokemon(397, "Staravia"),
new Pokemon(398, "Staraptor"),
new Pokemon(399, "Bidoof"),
new Pokemon(400, "Bibarel"),
new Pokemon(401, "Kricketot"),
new Pokemon(402, "Kricketune"),
new Pokemon(403, "Shinx"),
new Pokemon(404, "Luxio"),
new Pokemon(405, "Luxray"),
new Pokemon(406, "Budew"),
new Pokemon(407, "Roserade"),
new Pokemon(408, "Cranidos"),
new Pokemon(409, "Rampardos"),
new Pokemon(410, "Shieldon"),
new Pokemon(411, "Bastiodon"),
new Pokemon(412, "Burmy"),
new Pokemon(413, "Wormadam"),
new Pokemon(414, "Mothim"),
new Pokemon(415, "Combee"),
new Pokemon(416, "Vespiquen"),
new Pokemon(417, "Pachirisu"),
new Pokemon(418, "Buizel"),
new Pokemon(419, "Floatzel"),
new Pokemon(420, "Cherubi"),
new Pokemon(421, "Cherrim"),
new Pokemon(422, "Shellos"),
new Pokemon(423, "Gastrodon"),
new Pokemon(424, "Ambipom"),
new Pokemon(425, "Drifloon"),
new Pokemon(426, "Drifblim"),
new Pokemon(427, "Buneary"),
new Pokemon(428, "Lopunny"),
new Pokemon(429, "Mismagius"),
new Pokemon(430, "Honchkrow"),
new Pokemon(431, "Glameow"),
new Pokemon(432, "Purugly"),
new Pokemon(433, "Chingling"),
new Pokemon(434, "Stunky"),
new Pokemon(435, "Skuntank"),
new Pokemon(436, "Bronzor"),
new Pokemon(437, "Bronzong"),
new Pokemon(438, "Bonsly"),
new Pokemon(439, "Mime Jr."),
new Pokemon(440, "Happiny"),
new Pokemon(441, "Chatot"),
new Pokemon(442, "Spiritomb"),
new Pokemon(443, "Gible"),
new Pokemon(444, "Gabite"),
new Pokemon(445, "Garchomp"),
new Pokemon(446, "Munchlax"),
new Pokemon(447, "Riolu"),
new Pokemon(448, "Lucario"),
new Pokemon(449, "Hippopotas"),
new Pokemon(450, "Hippowdon"),
new Pokemon(451, "Skorupi"),
new Pokemon(452, "Drapion"),
new Pokemon(453, "Croagunk"),
new Pokemon(454, "Toxicroak"),
new Pokemon(455, "Carnivine"),
new Pokemon(456, "Finneon"),
new Pokemon(457, "Lumineon"),
new Pokemon(458, "Mantyke"),
new Pokemon(459, "Snover"),
new Pokemon(460, "Abomasnow"),
new Pokemon(461, "Weavile"),
new Pokemon(462, "Magnezone"),
new Pokemon(463, "Lickilicky"),
new Pokemon(464, "Rhyperior"),
new Pokemon(465, "Tangrowth"),
new Pokemon(466, "Electivire"),
new Pokemon(467, "Magmortar"),
new Pokemon(468, "Togekiss"),
new Pokemon(469, "Yanmega"),
new Pokemon(470, "Leafeon"),
new Pokemon(471, "Glaceon"),
new Pokemon(472, "Gliscor"),
new Pokemon(473, "Mamoswine"),
new Pokemon(474, "Porygon-Z"),
new Pokemon(475, "Gallade"),
new Pokemon(476, "Probopass"),
new Pokemon(477, "Dusknoir"),
new Pokemon(478, "Froslass"),
new Pokemon(479, "Rotom"),
new Pokemon(480, "Uxie"),
new Pokemon(481, "Mesprit"),
new Pokemon(482, "Azelf"),
new Pokemon(483, "Dialga"),
new Pokemon(484, "Palkia"),
new Pokemon(485, "Heatran"),
new Pokemon(486, "Regigigas"),
new Pokemon(487, "Giratina"),
new Pokemon(488, "Cresselia"),
new Pokemon(489, "Phione"),
new Pokemon(490, "Manaphy"),
new Pokemon(491, "Darkrai"),
new Pokemon(492, "Shaymin"),
new Pokemon(493, "Arceus"),
new Pokemon(494, "Victini"),
new Pokemon(495, "Snivy"),
new Pokemon(496, "Servine"),
new Pokemon(497, "Serperior"),
new Pokemon(498, "Tepig"),
new Pokemon(499, "Pignite"),
new Pokemon(500, "Emboar"),
new Pokemon(501, "Oshawott"),
new Pokemon(502, "Dewott"),
new Pokemon(503, "Samurott"),
new Pokemon(504, "Patrat"),
new Pokemon(505, "Watchog"),
new Pokemon(506, "Lillipup"),
new Pokemon(507, "Herdier"),
new Pokemon(508, "Stoutland"),
new Pokemon(509, "Purrloin"),
new Pokemon(510, "Liepard"),
new Pokemon(511, "Pansage"),
new Pokemon(512, "Simisage"),
new Pokemon(513, "Pansear"),
new Pokemon(514, "Simisear"),
new Pokemon(515, "Panpour"),
new Pokemon(516, "Simipour"),
new Pokemon(517, "Munna"),
new Pokemon(518, "Musharna"),
new Pokemon(519, "Pidove"),
new Pokemon(520, "Tranquill"),
new Pokemon(521, "Unfezant"),
new Pokemon(522, "Blitzle"),
new Pokemon(523, "Zebstrika"),
new Pokemon(524, "Roggenrola"),
new Pokemon(525, "Boldore"),
new Pokemon(526, "Gigalith"),
new Pokemon(527, "Woobat"),
new Pokemon(528, "Swoobat"),
new Pokemon(529, "Drilbur"),
new Pokemon(530, "Excadrill"),
new Pokemon(531, "Audino"),
new Pokemon(532, "Timburr"),
new Pokemon(533, "Gurdurr"),
new Pokemon(534, "Conkeldurr"),
new Pokemon(535, "Tympole"),
new Pokemon(536, "Palpitoad"),
new Pokemon(537, "Seismitoad"),
new Pokemon(538, "Throh"),
new Pokemon(539, "Sawk"),
new Pokemon(540, "Sewaddle"),
new Pokemon(541, "Swadloon"),
new Pokemon(542, "Leavanny"),
new Pokemon(543, "Venipede"),
new Pokemon(544, "Whirlipede"),
new Pokemon(545, "Scolipede"),
new Pokemon(546, "Cottonee"),
new Pokemon(547, "Whimsicott"),
new Pokemon(548, "Petilil"),
new Pokemon(549, "Lilligant"),
new Pokemon(550, "Basculin"),
new Pokemon(551, "Sandile"),
new Pokemon(552, "Krokorok"),
new Pokemon(553, "Krookodile"),
new Pokemon(554, "Darumaka"),
new Pokemon(555, "Darmanitan"),
new Pokemon(556, "Maractus"),
new Pokemon(557, "Dwebble"),
new Pokemon(558, "Crustle"),
new Pokemon(559, "Scraggy"),
new Pokemon(560, "Scrafty"),
new Pokemon(561, "Sigilyph"),
new Pokemon(562, "Yamask"),
new Pokemon(563, "Cofagrigus"),
new Pokemon(564, "Tirtouga"),
new Pokemon(565, "Carracosta"),
new Pokemon(566, "Archen"),
new Pokemon(567, "Archeops"),
new Pokemon(568, "Trubbish"),
new Pokemon(569, "Garbodor"),
new Pokemon(570, "Zorua"),
new Pokemon(571, "Zoroark"),
new Pokemon(572, "Minccino"),
new Pokemon(573, "Cinccino"),
new Pokemon(574, "Gothita"),
new Pokemon(575, "Gothorita"),
new Pokemon(576, "Gothitelle"),
new Pokemon(577, "Solosis"),
new Pokemon(578, "Duosion"),
new Pokemon(579, "Reuniclus"),
new Pokemon(580, "Ducklett"),
new Pokemon(581, "Swanna"),
new Pokemon(582, "Vanillite"),
new Pokemon(583, "Vanillish"),
new Pokemon(584, "Vanilluxe"),
new Pokemon(585, "Deerling"),
new Pokemon(586, "Sawsbuck"),
new Pokemon(587, "Emolga"),
new Pokemon(588, "Karrablast"),
new Pokemon(589, "Escavalier"),
new Pokemon(590, "Foongus"),
new Pokemon(591, "Amoonguss"),
new Pokemon(592, "Frillish"),
new Pokemon(593, "Jellicent"),
new Pokemon(594, "Alomomola"),
new Pokemon(595, "Joltik"),
new Pokemon(596, "Galvantula"),
new Pokemon(597, "Ferroseed"),
new Pokemon(598, "Ferrothorn"),
new Pokemon(599, "Klink"),
new Pokemon(600, "Klang"),
new Pokemon(601, "Klinklang"),
new Pokemon(602, "Tynamo"),
new Pokemon(603, "Eelektrik"),
new Pokemon(604, "Eelektross"),
new Pokemon(605, "Elgyem"),
new Pokemon(606, "Beheeyem"),
new Pokemon(607, "Litwick"),
new Pokemon(608, "Lampent"),
new Pokemon(609, "Chandelure"),
new Pokemon(610, "Axew"),
new Pokemon(611, "Fraxure"),
new Pokemon(612, "Haxorus"),
new Pokemon(613, "Cubchoo"),
new Pokemon(614, "Beartic"),
new Pokemon(615, "Cryogonal"),
new Pokemon(616, "Shelmet"),
new Pokemon(617, "Accelgor"),
new Pokemon(618, "Stunfisk"),
new Pokemon(619, "Mienfoo"),
new Pokemon(620, "Mienshao"),
new Pokemon(621, "Druddigon"),
new Pokemon(622, "Golett"),
new Pokemon(623, "Golurk"),
new Pokemon(624, "Pawniard"),
new Pokemon(625, "Bisharp"),
new Pokemon(626, "Bouffalant"),
new Pokemon(627, "Rufflet"),
new Pokemon(628, "Braviary"),
new Pokemon(629, "Vullaby"),
new Pokemon(630, "Mandibuzz"),
new Pokemon(631, "Heatmor"),
new Pokemon(632, "Durant"),
new Pokemon(633, "Deino"),
new Pokemon(634, "Zweilous"),
new Pokemon(635, "Hydreigon"),
new Pokemon(636, "Larvesta"),
new Pokemon(637, "Volcarona"),
new Pokemon(638, "Cobalion"),
new Pokemon(639, "Terrakion"),
new Pokemon(640, "Virizion"),
new Pokemon(641, "Tornadus"),
new Pokemon(642, "Thundurus"),
new Pokemon(643, "Reshiram"),
new Pokemon(644, "Zekrom"),
new Pokemon(645, "Landorus"),
new Pokemon(646, "Kyurem"),
new Pokemon(647, "Keldeo"),
new Pokemon(648, "Meloetta"),
new Pokemon(649, "Genesect"),
new Pokemon(650, "Chespin"),
new Pokemon(651, "Quilladin"),
new Pokemon(652, "Chesnaught"),
new Pokemon(653, "Fennekin"),
new Pokemon(654, "Braixen"),
new Pokemon(655, "Delphox"),
new Pokemon(656, "Froakie"),
new Pokemon(657, "Frogadier"),
new Pokemon(658, "Greninja"),
new Pokemon(659, "Bunnelby"),
new Pokemon(660, "Diggersby"),
new Pokemon(661, "Fletchling"),
new Pokemon(662, "Fletchinder"),
new Pokemon(663, "Talonflame"),
new Pokemon(664, "Scatterbug"),
new Pokemon(665, "Spewpa"),
new Pokemon(666, "Vivillon"),
new Pokemon(667, "Litleo"),
new Pokemon(668, "Pyroar"),
new Pokemon(669, "Flabébé"),
new Pokemon(670, "Floette"),
new Pokemon(671, "Florges"),
new Pokemon(672, "Skiddo"),
new Pokemon(673, "Gogoat"),
new Pokemon(674, "Pancham"),
new Pokemon(675, "Pangoro"),
new Pokemon(676, "Furfrou"),
new Pokemon(677, "Espurr"),
new Pokemon(678, "Meowstic"),
new Pokemon(679, "Honedge"),
new Pokemon(680, "Doublade"),
new Pokemon(681, "Aegislash"),
new Pokemon(682, "Spritzee"),
new Pokemon(683, "Aromatisse"),
new Pokemon(684, "Swirlix"),
new Pokemon(685, "Slurpuff"),
new Pokemon(686, "Inkay"),
new Pokemon(687, "Malamar"),
new Pokemon(688, "Binacle"),
new Pokemon(689, "Barbaracle"),
new Pokemon(690, "Skrelp"),
new Pokemon(691, "Dragalge"),
new Pokemon(692, "Clauncher"),
new Pokemon(693, "Clawitzer"),
new Pokemon(694, "Helioptile"),
new Pokemon(695, "Heliolisk"),
new Pokemon(696, "Tyrunt"),
new Pokemon(697, "Tyrantrum"),
new Pokemon(698, "Amaura"),
new Pokemon(699, "Aurorus"),
new Pokemon(700, "Sylveon"),
new Pokemon(701, "Hawlucha"),
new Pokemon(702, "Dedenne"),
new Pokemon(703, "Carbink"),
new Pokemon(704, "Goomy"),
new Pokemon(705, "Sliggoo"),
new Pokemon(706, "Goodra"),
new Pokemon(707, "Klefki"),
new Pokemon(708, "Phantump"),
new Pokemon(709, "Trevenant"),
new Pokemon(710, "Pumpkaboo"),
new Pokemon(711, "Gourgeist"),
new Pokemon(712, "Bergmite"),
new Pokemon(713, "Avalugg"),
new Pokemon(714, "Noibat"),
new Pokemon(715, "Noivern"),
new Pokemon(716, "Xerneas"),
new Pokemon(717, "Yveltal"),
new Pokemon(718, "Zygarde"),
new Pokemon(719, "Diancie"),
new Pokemon(720, "Hoopa"),
new Pokemon(721, "Volcanion"),
new Pokemon(722, "Rowlet"),
new Pokemon(723, "Dartrix"),
new Pokemon(724, "Decidueye"),
new Pokemon(725, "Litten"),
new Pokemon(726, "Torracat"),
new Pokemon(727, "Incineroar"),
new Pokemon(728, "Popplio"),
new Pokemon(729, "Brionne"),
new Pokemon(730, "Primarina"),
new Pokemon(731, "Pikipek"),
new Pokemon(732, "Trumbeak"),
new Pokemon(733, "Toucannon"),
new Pokemon(734, "Yungoos"),
new Pokemon(735, "Gumshoos"),
new Pokemon(736, "Grubbin"),
new Pokemon(737, "Charjabug"),
new Pokemon(738, "Vikavolt"),
new Pokemon(739, "Crabrawler"),
new Pokemon(740, "Crabominable"),
new Pokemon(741, "Oricorio"),
new Pokemon(742, "Cutiefly"),
new Pokemon(743, "Ribombee"),
new Pokemon(744, "Rockruff"),
new Pokemon(745, "Lycanroc"),
new Pokemon(746, "Wishiwashi"),
new Pokemon(747, "Mareanie"),
new Pokemon(748, "Toxapex"),
new Pokemon(749, "Mudbray"),
new Pokemon(750, "Mudsdale"),
new Pokemon(751, "Dewpider"),
new Pokemon(752, "Araquanid"),
new Pokemon(753, "Fomantis"),
new Pokemon(754, "Lurantis"),
new Pokemon(755, "Morelull"),
new Pokemon(756, "Shiinotic"),
new Pokemon(757, "Salandit"),
new Pokemon(758, "Salazzle"),
new Pokemon(759, "Stufful"),
new Pokemon(760, "Bewear"),
new Pokemon(761, "Bounsweet"),
new Pokemon(762, "Steenee"),
new Pokemon(763, "Tsareena"),
new Pokemon(764, "Comfey"),
new Pokemon(765, "Oranguru"),
new Pokemon(766, "Passimian"),
new Pokemon(767, "Wimpod"),
new Pokemon(768, "Golisopod"),
new Pokemon(769, "Sandygast"),
new Pokemon(770, "Palossand"),
new Pokemon(771, "Pyukumuku"),
new Pokemon(772, "Type: Null"),
new Pokemon(773, "Silvally"),
new Pokemon(774, "Minior"),
new Pokemon(775, "Komala"),
new Pokemon(776, "Turtonator"),
new Pokemon(777, "Togedemaru"),
new Pokemon(778, "Mimikyu"),
new Pokemon(779, "Bruxish"),
new Pokemon(780, "Drampa"),
new Pokemon(781, "Dhelmise"),
new Pokemon(782, "Jangmo-o"),
new Pokemon(783, "Hakamo-o"),
new Pokemon(784, "Kommo-o"),
new Pokemon(785, "Tapu Koko"),
new Pokemon(786, "Tapu Lele"),
new Pokemon(787, "Tapu Bulu"),
new Pokemon(788, "Tapu Fini"),
new Pokemon(789, "Cosmog"),
new Pokemon(790, "Cosmoem"),
new Pokemon(791, "Solgaleo"),
new Pokemon(792, "Lunala"),
new Pokemon(793, "Nihilego"),
new Pokemon(794, "Buzzwole"),
new Pokemon(795, "Pheromosa"),
new Pokemon(796, "Xurkitree"),
new Pokemon(797, "Celesteela"),
new Pokemon(798, "Kartana"),
new Pokemon(799, "Guzzlord"),
new Pokemon(800, "Necrozma"),
new Pokemon(801, "Magearna"),
new Pokemon(802, "Marshadow")
            };
        }

        #endregion

        #region Public Methods

        public bool ContainsKey(ulong guildId)
        {
            return Servers.Exists(x => x.GuildId == guildId);
        }

        /// <summary>
        /// Saves the configuration file to the default path.
        /// </summary>
        public void Save()
        {
            Save(ConfigFilePath);
        }

        /// <summary>
        /// Saves the configuration file to the specified path.
        /// </summary>
        /// <param name="filePath">The full file path.</param>
        public void Save(string filePath)
        {
            //var serializedData = XmlStringSerializer.Serialize(this);
            var serializedData = JsonStringSerializer.Serialize(this);
            File.WriteAllText(filePath, serializedData);
        }

        /// <summary>
        /// Serializes the Config object to an xml string using
        /// the <seealso cref="XmlStringSerializer"/> class.
        /// </summary>
        /// <returns>Returns an xml string representing this object.</returns>
        public string ToXmlString()
        {
            return XmlStringSerializer.Serialize(this);
        }

        public string ToJsonString()
        {
            return JsonStringSerializer.Serialize(this);
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Loads the configuration file from the default path.
        /// </summary>
        /// <returns>Returns the deserialized Config object.</returns>
        public static Database Load()
        {
            return Load(ConfigFilePath);
        }

        /// <summary>
        /// Loads the configuration file from the specified path.
        /// </summary>
        /// <param name="filePath">The full file path.</param>
        /// <returns>Returns the deserialized Config object.</returns>
        public static Database Load(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var data = File.ReadAllText(filePath);
                    //return XmlStringSerializer.Deserialize<Database>(data);
                    return JsonStringSerializer.Deserialize<Database>(data);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LoadConfig: {ex}");
            }

            return new Database();
        }

        #endregion
    }

    [XmlRoot("server")]
    [JsonObject("server")]
    public class Server
    {
        [XmlElement("guildId")]
        [JsonProperty("guildId")]
        public ulong GuildId { get; set; }

        [XmlArrayItem("lobby")]
        [XmlArray("lobbies")]
        [JsonProperty("lobbies")]
        public List<RaidLobby> Lobbies { get; set; }

        [XmlElement("subscriptions")]
        [JsonProperty("subscriptions")]
        public List<Subscription> Subscriptions { get; set; }

        [XmlIgnore]
        [JsonIgnore]
        public Subscription this[ulong userId]
        {
            get { return Subscriptions.Find(x => x.UserId == userId); }
        }

        public Server()
        {
            Lobbies = new List<RaidLobby>();
            Subscriptions = new List<Subscription>();
        }

        public Server(ulong guildId, List<RaidLobby> lobbies, List<Subscription> subscriptions)
        {
            GuildId = guildId;
            Lobbies = lobbies;
            Subscriptions = subscriptions;
        }

        public bool ContainsKey(ulong userId)
        {
            return this[userId] != null;
        }

        public bool Remove(ulong userId)
        {
            if (ContainsKey(userId))
            {
                return Subscriptions.Remove(this[userId]);
            }

            return false;
        }
    }
}