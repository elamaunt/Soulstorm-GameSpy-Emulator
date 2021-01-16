namespace GameSpyEmulator
{
    public static class GameSpyHttpDataConstants
    {
        public const string RusNews = @" Привет! 
.
Текущие карты в авто: 
.
1на1
- 2P Fallen City
- 2P Sugar Oasis
- 2p Deadly Fun Archeology
- 2P Shrine of Excellion
- 2P Vortex Plauteau
- 2P Battle Marshes
- 2P Outer Reaches (доработанная Дэвилом)
- 2P Blood River
- 2P Fata Morgana
- 2P Titan Fall (доработанная Дэвилом)
- 2P FraziersDemise (доработанная Дэвилом)
- 2P Moonbase (доработанная Дэвилом)
.
2на2
- 4p gurmuns pass
- 4P Biffys Peril
- 4P Gorhael Crater
- 4P Doom Spiral
- 4p panrea lowlands (доработанная Дэвилом)
- 4P Skerries (доработанная Дэвилом)
- 4p Saints Square 
- 4p Sad Place
- 4p Cold War
.
3на3
- 6p Mortalis
- 6P Alvarus
- 6P Shakun Coast
- 6P Fury Island
- 6p paynes retribution
- 6p parmenian heath
.
4на4
- 8P Oasis of Sharr
- 8P Forbidden Jungle
- 8P Jalaganda Lowlands
- 8p cerulea
- 8p burial grounds
- 8p thurabis plateau
.
.
Сервер активно обсуждается здесь и в Дискорд.";

        public const string EnNews = @" Hello!
.
Current maps in automatch: 
.
1vs1
- 2P Fallen City
- 2P Sugar Oasis
- 2p Deadly Fun Archeology
- 2P Shrine of Excellion
- 2P Vortex Plauteau
- 2P Meeting of Minds
- 2P Battle Marshes
- 2P Outer Reaches (fixed by Devil)
- 2P Blood River
- 2P Fata Morgana
- 2P Titan Fall (fixed by Devil)
- 2P Tranquilitys End (fixed by Devil)
- 2P FraziersDemise (fixed by Devil)
- 2P Moonbase (fixed by Devil)
.
2vs2
- 4p gurmuns pass
- 4P Biffys Peril
- 4P Gorhael Crater
- 4P Doom Spiral
- 4p panrea lowlands (fixed by Devil)
- 4P Skerries (fixed by Devil)
- 4p Saints Square 
- 4p Sad Place
- 4p Cold War
.
3vs3
- 6p Mortalis
- 6P Alvarus
- 6P Shakun Coast
- 6P Fury Island
- 6p paynes retribution
- 6p parmenian heath
.
4vs4
- 8P Oasis of Sharr
- 8P Forbidden Jungle
- 8P Jalaganda Lowlands
- 8p cerulea
- 8p burial grounds
- 8p thurabis plateau
.
.
The server is being actively discussed here and on Discord.";

        public const string RoomPairs = @"
room_pairs = 
{
        ""Room 1"",
		""DowOnline""
}";

        public const string AutomatchDefaults = @"----------------------------------------------------------------------------------------------------------------
-- Default FE Settings
-- (c) 2004 Relic Entertainment Inc.

-- chat spam defined.
chat_options =
{
	maxChat = 30,
	timeInterval = 60,
	timeWait = 60,
}

-- Note: automatch defaults are defined in code for w40k
automatch_defaults =
{
	-- win conditions: IDs listed here
	win_condition_defaults = 
	{
		""Annihilate"",
		""ControlArea"",
		""StrategicObjective"",
		""GameTimer""
	},

	--automatch maps
	automatch_maps2p = 
	{
		""2p_Fallen_City"",
		""2P_Battle_Marshes"",
		""2P_Deadmans_Crossing"",
		""2P_Meeting_of_Minds"",
		""2P_Outer_Reaches"",
		""2P_Valley_of_Khorne"",
		""2P_Blood_River""
	},
	automatch_maps4p = 
	{
		""4P_Biffys_Peril"",
		""4P_Quatra"",
		""4P_Tartarus_Center"",
		""4P_volcanic reaction"",
		""4P_Mountain_Trail"",
		""4P_Tainted_Place"",
		""4P_Tainted_Soul"",
		""4p_Saints_Square""
	},
	automatch_maps6p = 
	{
		""6P_Bloodshed_Alley"",
		""6P_Kasyr_Lutien"",
		""6p_Mortalis"",
		""6P_Testing_Grounds"",
		""6PTeam_Streets_of_Vogen"",
		""6p_crossroads""
	},
	automatch_maps8p = 
	{
		""8p_team_ruins"",
		""8P_Burial_Grounds"",
		""8P_Daturias_Pits"",
		""8P_Doom_Chamber"",
		""8P_Lost_Hope"",
		""8P_Penal_Colony""		
	},
}

-- Note: automatch defaults are defined in code for wxp
automatch_defaults_wxp =
{
	-- win conditions: IDs listed here
	win_condition_defaults = 
	{
		""Annihilate"",
		""ControlArea"",
		""StrategicObjective"",
		""GameTimer""
	},

	--automatch maps
	automatch_maps2p = 
	{
		""2P_Battle_Marshes"",
		""2P_Meeting_of_Minds"",
		""2P_Outer_Reaches"",
		""2P_Blood_River"",
		""2p_Fallen_City"",
		""2P_Shrine_of_Excellion"",
		""4P_Gorhael_Crater"",
		""4P_Tiboraxx"",
		""4P_Dread_Peak"",
		""2P_Quests_Triumph"",
	},
	automatch_maps4p = 
	{
		""4P_Biffys_Peril"",
		""4P_Gorhael_Crater"",
		""4P_Tiboraxx"",
		""4P_Dread_Peak"",
		""4P_Torrents"",
		""4P_Ice_Flow"",
		""4P_Doom_Spiral"",
		
	},
	automatch_maps6p = 
	{
		""6p_Mortalis"",
		""6P_Crozius_Arcanum"",
		""6P_Fury_Island"",
		""6P_Thargorum"",
		""6P_Alvarus"",

	},
	automatch_maps8p = 
	{
		""8P_Oasis_of_Sharr"",
		""8P_Forbidden_Jungle"",
		""8P_Fear of the Darkness"",
	
	},
}

-- Note: automatch defaults are defined in code for dxp2
automatch_defaults_dxp2 =
{
	-- win conditions: IDs listed here
	win_condition_defaults = 
	{
		""Annihilate"",
		""ControlArea"",
		""StrategicObjective"",
		""GameTimer""
	},


	--automatch maps
	automatch_maps2p = 
	{
		""2p_Fallen_City"",
		""2p_SugarOasis"",
        ""2p_Deadly_Fun_Archeology"",
		""2P_Shrine_of_Excellion"",
		""2P_Meeting_of_Minds"",
		""2P_Battle_Marshes"",
		""2P_Blood_River"",
		""2P_Fata_Morgana"",
		""2P_Titan_Fall"",
		""2p_vortex_plateau"",
		""2P_FraziersDemise"",
		""2P_Moonbase""
	},
	automatch_maps4p = 
	{
		""4p_gurmuns_pass"",
		""4P_Biffys_Peril"",
		""4P_Gorhael_Crater"",
		""4P_Doom_Spiral"",
		""4p_panrea_lowlands"",
		""4P_Skerries"",
		""4p_Saints_Square"",
		""4p_Sad_Place"",
        ""4p_cold_war""
	},
	automatch_maps6p = 
	{
		""6p_Mortalis"",
		""6P_Alvarus"",
		""6P_Shakun_Coast"",
		""6P_Fury_Island"",
		""6p_paynes_retribution"",
		""6p_parmenian_heath""
	},
	automatch_maps8p = 
	{
		""8P_Oasis_of_Sharr"",
		""8P_Forbidden_Jungle"",
		""8P_Jalaganda_Lowlands"",
		""8p_cerulea"",
		""8p_burial_grounds"",
		""8p_thurabis_plateau""
	},
}


";
    }
}
