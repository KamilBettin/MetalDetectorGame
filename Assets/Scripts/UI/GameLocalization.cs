using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameLanguage
{
    English,
    Polish,
    Norwegian,
    German,
    Spanish,
    Swedish,
    Danish
}

public static class GameLocalization
{
    private const string LanguagePrefKey = "MetalDetector.Language";
    private const int LanguageCount = 7;

    private static readonly Dictionary<string, string[]> Texts = new Dictionary<string, string[]>
    {
        { "language.english", new[] { "English", "Angielski", "Engelsk", "Englisch", "Ingl\u00e9s", "Engelska", "Engelsk" } },
        { "language.polish", new[] { "Polish", "Polski", "Polsk", "Polnisch", "Polaco", "Polska", "Polsk" } },
        { "language.norwegian", new[] { "Norwegian", "Norweski", "Norsk", "Norwegisch", "Noruego", "Norska", "Norsk" } },
        { "language.german", new[] { "German", "Niemiecki", "Tysk", "Deutsch", "Alem\u00e1n", "Tyska", "Tysk" } },
        { "language.spanish", new[] { "Spanish", "Hiszpa\u0144ski", "Spansk", "Spanisch", "Espa\u00f1ol", "Spanska", "Spansk" } },
        { "language.swedish", new[] { "Swedish", "Szwedzki", "Svensk", "Schwedisch", "Sueco", "Svenska", "Svensk" } },
        { "language.danish", new[] { "Danish", "Du\u0144ski", "Dansk", "D\u00e4nisch", "Dan\u00e9s", "Danska", "Dansk" } },

        { "settings.title", new[] { "SETTINGS", "USTAWIENIA", "INNSTILLINGER", "EINSTELLUNGEN", "AJUSTES", "INST\u00c4LLNINGAR", "INDSTILLINGER" } },
        { "settings.language", new[] { "Language", "J\u0119zyk", "Spr\u00e5k", "Sprache", "Idioma", "Spr\u00e5k", "Sprog" } },
        { "settings.gender", new[] { "Character Gender", "P\u0142e\u0107 postaci", "Figurkj\u00f8nn", "Charaktergeschlecht", "G\u00e9nero del personaje", "Karakt\u00e4rens k\u00f6n", "Figurens k\u00f8n" } },
        { "settings.current", new[] { "Current", "Obecnie", "Valgt spr\u00e5k", "Aktuell", "Actual", "Aktuellt", "Valgt" } },
        { "settings.selected", new[] { "SELECTED", "WYBRANO", "VALGT", "AUSGEW\u00c4HLT", "SELECCIONADO", "VALD", "VALGT" } },
        { "settings.character", new[] { "Character", "Posta\u0107", "Figur", "Charakter", "Personaje", "Karakt\u00e4r", "Figur" } },
        { "settings.close", new[] { "Close", "Zamknij", "Lukk", "Schlie\u00dfen", "Cerrar", "St\u00e4ng", "Luk" } },
        { "settings.changed", new[] { "Language changed.", "J\u0119zyk zmieniony.", "Spr\u00e5k endret.", "Sprache ge\u00e4ndert.", "Idioma cambiado.", "Spr\u00e5k \u00e4ndrat.", "Sprog \u00e6ndret." } },

        { "menu.new_game", new[] { "New Game", "Nowa gra", "Nytt spill", "Neues Spiel", "Nueva partida", "Nytt spel", "Nyt spil" } },
        { "menu.continue", new[] { "Continue", "Kontynuuj", "Fortsett", "Fortsetzen", "Continuar", "Forts\u00e4tt", "Forts\u00e6t" } },
        { "menu.multiplayer", new[] { "Multiplayer", "Multiplayer", "Flerspiller", "Mehrspieler", "Multijugador", "Flerspelare", "Multiplayer" } },
        { "menu.settings", new[] { "Settings", "Ustawienia", "Innstillinger", "Einstellungen", "Ajustes", "Inst\u00e4llningar", "Indstillinger" } },
        { "menu.quit", new[] { "Quit", "Wyjd\u017a", "Avslutt", "Beenden", "Salir", "Avsluta", "Afslut" } },
        { "menu.resume", new[] { "Resume", "Wzn\u00f3w", "Fortsett", "Fortsetzen", "Reanudar", "Forts\u00e4tt", "Forts\u00e6t" } },
        { "menu.invite", new[] { "Invite", "Zapro\u015b", "Inviter", "Einladen", "Invitar", "Bjud in", "Inviter" } },
        { "menu.save", new[] { "Save", "Zapisz", "Lagre", "Speichern", "Guardar", "Spara", "Gem" } },
        { "menu.main_menu", new[] { "Main Menu", "Menu g\u0142\u00f3wne", "Hovedmeny", "Hauptmen\u00fc", "Men\u00fa principal", "Huvudmeny", "Hovedmenu" } },

        { "character.title", new[] { "RANDOM {0}", "LOSOWA POSTA\u0106: {0}", "TILFELDIG {0}", "ZUF\u00c4LLIG {0}", "{0} ALEATORIO", "SLUMPM\u00c4SSIG {0}", "TILF\u00c6LDIG {0}" } },
        { "character.hint", new[] { "New games generate a fresh random avatar", "Nowa gra wygeneruje \u015bwie\u017c\u0105 losow\u0105 posta\u0107", "Nye spill lager en ny tilfeldig figur", "Neue Spiele erzeugen einen frischen zuf\u00e4lligen Avatar", "Las partidas nuevas generan un avatar aleatorio nuevo", "Nya spel skapar en ny slumpm\u00e4ssig avatar", "Nye spil laver en ny tilf\u00e6ldig avatar" } },
        { "character.male", new[] { "MALE", "M\u0118\u017bCZYZNA", "MANN", "M\u00c4NNLICH", "HOMBRE", "MAN", "MAND" } },
        { "character.female", new[] { "FEMALE", "KOBIETA", "KVINNE", "WEIBLICH", "MUJER", "KVINNA", "KVINDE" } },

        { "tutorial.scan_sand", new[] { "Scan ground: {0}/{1}", "Skanuj teren: {0}/{1}", "Skann bakken: {0}/{1}" } },
        { "tutorial.find_first", new[] { "Find and dig up your first treasure", "Znajd\u017a i wykop pierwszy skarb", "Finn og grav opp din f\u00f8rste skatt" } },
        { "tutorial.sell_loot", new[] { "Sell your loot to the trader", "Sprzedaj \u0142up handlarzowi", "Selg funnene dine til handelsmannen" } },
        { "tutorial.complete", new[] { "Tutorial complete. Keep searching and upgrading.", "Samouczek uko\u0144czony. Szukaj dalej i ulepszaj sprz\u0119t.", "Oppl\u00e6ringen er ferdig. Fortsett \u00e5 s\u00f8ke og oppgradere." } },

        { "hud.detector", new[] { "Detector", "Detektor", "Detektor" } },
        { "hud.signal", new[] { "Signal", "Sygna\u0142", "Signal" } },
        { "hud.cash", new[] { "cash", "got\u00f3wka", "penger" } },
        { "hud.cargo", new[] { "Cargo", "\u0141adunek", "Last" } },
        { "hud.tutorial", new[] { "Tutorial", "Samouczek", "Oppl\u00e6ring" } },
        { "hud.no_detector", new[] { "No detector", "Brak detektora", "Ingen detektor" } },
        { "hud.signal_marked", new[] { "target marked", "cel oznaczony", "m\u00e5l markert" } },
        { "hud.signal_scan_now", new[] { "scan now", "skanuj teraz", "skann n\u00e5" } },
        { "hud.hint_default", new[] { "Hold LMB - Scan ground | TAB backpack", "Przytrzymaj LPM - skanuj teren | TAB plecak", "Hold venstre museknapp - skann bakken | TAB ryggsekk" } },
        { "hud.hint_start", new[] { "Hold LMB scan | E dig/talk | TAB backpack", "Przytrzymaj LPM skan | E kop/rozmawiaj | TAB plecak", "Hold venstre museknapp skann | E grav/snakk | TAB ryggsekk" } },
        { "hud.hint_close_trader", new[] { "ESC - Close trader", "ESC - Zamknij handlarza", "ESC - Lukk handelsmann" } },
        { "hud.hint_close_jobs", new[] { "ESC - Close jobs", "ESC - Zamknij zadania", "ESC - Lukk oppdrag" } },
        { "hud.hint_close_backpack", new[] { "TAB / ESC - Close backpack", "TAB / ESC - Zamknij plecak", "TAB / ESC - Lukk ryggsekk" } },
        { "hud.hint_close_home", new[] { "ESC - Close home", "ESC - Zamknij dom", "ESC - Lukk hjem" } },
        { "hud.hint_talk_jobs", new[] { "E - Talk / Jobs", "E - Rozmowa / Zadania", "E - Snakk / Oppdrag" } },
        { "hud.hint_use_home", new[] { "E - Enter house", "E - Wejd\u017a do domku", "E - G\u00e5 inn i huset" } },
        { "hud.hint_night", new[] { "Night - searching disabled. Sleep at home.", "Noc - szukanie wy\u0142\u0105czone. Prze\u015bpij si\u0119 w domu.", "Natt - s\u00f8king er deaktivert. Sov hjemme." } },
        { "hud.action_jobs", new[] { "Jobs", "Zadania", "Oppdrag" } },
        { "hud.action_use_home", new[] { "Enter House", "Wejd\u017a do domku", "G\u00e5 inn" } },
        { "hud.action_sell", new[] { "Sell", "Sprzedaj", "Selg" } },
        { "hud.action_upgrade", new[] { "Upgrade", "Ulepsz", "Oppgrader" } },
        { "hud.action_trade", new[] { "Trade", "Handel", "Handle" } },
        { "hud.action_search", new[] { "Search", "Przeszukaj", "S\u00f8k" } },
        { "hud.action_dig", new[] { "Dig", "Kop", "Grav" } },

        { "inventory.backpack", new[] { "Backpack", "Plecak", "Ryggsekk" } },
        { "shop.trader", new[] { "Trader", "Handlarz", "Handelsmann" } },
        { "shop.sell", new[] { "Sell", "Sprzeda\u017c", "Selg" } },
        { "shop.sell_items", new[] { "Sell Items", "Sprzedaj rzeczy", "Selg ting" } },
        { "shop.upgrades", new[] { "Upgrades", "Ulepszenia", "Oppgraderinger" } },
        { "shop.money_cargo", new[] { "Money: ${0} | Cargo value: ${1}", "Pieni\u0105dze: ${0} | Warto\u015b\u0107 \u0142upu: ${1}", "Penger: ${0} | Lastverdi: ${1}" } },
        { "shop.drop_to_sell", new[] { "DROP TO SELL", "UPU\u015a\u0106, ABY SPRZEDA\u0106", "SLIPP FOR \u00c5 SELGE" } },
        { "shop.release_to_sell", new[] { "RELEASE TO SELL", "PU\u015a\u0106, ABY SPRZEDA\u0106", "SLIPP FOR \u00c5 SELGE" } },
        { "shop.sell_help", new[] { "Drag treasures into the sell box or click an item card.", "Przeci\u0105gnij skarby do pola sprzeda\u017cy albo kliknij kart\u0119 przedmiotu.", "Dra skatter til salgsfeltet eller klikk p\u00e5 et kort." } },
        { "shop.empty_backpack", new[] { "Backpack is empty. Go find something shiny.", "Plecak jest pusty. Id\u017a znale\u017a\u0107 co\u015b b\u0142yszcz\u0105cego.", "Ryggsekken er tom. Finn noe som skinner." } },
        { "shop.upgrade_help", new[] { "Spend cash to expand your search loop.", "Wydaj got\u00f3wk\u0119, aby rozwin\u0105\u0107 poszukiwania.", "Bruk penger for \u00e5 utvide s\u00f8ket ditt." } },
        { "shop.metal_detector", new[] { "Metal Detector", "Wykrywacz metalu", "Metalldetektor" } },
        { "shop.maxed", new[] { "Maxed", "Maks.", "Maks" } },
        { "shop.equipped", new[] { "Equipped", "Za\u0142o\u017cone", "Utstyrt" } },
        { "shop.visit", new[] { "Visit", "Id\u017a", "Bes\u00f8k" } },
        { "shop.detector_next", new[] { "Next: {0} has better range and a wider scan.", "Nast\u0119pny: {0} ma lepszy zasi\u0119g i szersze skanowanie.", "Neste: {0} har bedre rekkevidde og bredere skann." } },
        { "shop.detector_maxed", new[] { "Your detector is fully upgraded.", "Tw\u00f3j wykrywacz jest w pe\u0142ni ulepszony.", "Detektoren din er fullt oppgradert." } },
        { "shop.backpack_size", new[] { "Backpack Size", "Rozmiar plecaka", "Ryggsekkst\u00f8rrelse" } },
        { "shop.backpack_description", new[] { "Adds another row and column to your backpack.", "Dodaje kolejny rz\u0105d i kolumn\u0119 w plecaku.", "Legger til en ny rad og kolonne i ryggsekken." } },
        { "shop.clean_shovel", new[] { "Clean Shovel", "Czysta \u0142opata", "Ren spade" } },
        { "shop.clean_shovel_description", new[] { "Digs with double power and uses the clean silver shovel.", "Kopie z podw\u00f3jn\u0105 si\u0142\u0105 i u\u017cywa czystej srebrnej \u0142opaty.", "Graver med dobbel kraft og bruker den rene s\u00f8lvspaden." } },
        { "shop.crafting_station", new[] { "Crafting Station", "Stacja craftingu", "Arbeidsbenk" } },
        { "shop.crafting_description", new[] { "Unlocks the workbench inside your house.", "Odblokowuje workbench w domku.", "L\u00e5ser opp arbeidsbenken i huset." } },
        { "shop.crafting_unlocked", new[] { "Unlocked", "Odblokowane", "L\u00e5st opp" } },
        { "shop.search_areas", new[] { "Search Areas", "Obszary poszukiwa\u0144", "S\u00f8keomr\u00e5der" } },
        { "shop.search_areas_description", new[] { "Buy land at the sign next to each plot.", "Kup ziemi\u0119 przy tabliczce obok dzia\u0142ki.", "Kj\u00f8p land ved skiltet ved hvert felt." } },
        { "shop.msg_trader_upgrades", new[] { "This trader handles upgrades.", "Ten handlarz zajmuje si\u0119 ulepszeniami.", "Denne handelsmannen tar seg av oppgraderinger." } },
        { "shop.msg_only_buys", new[] { "This trader only buys treasures.", "Ten handlarz tylko kupuje skarby.", "Denne handelsmannen kj\u00f8per bare skatter." } },
        { "shop.msg_nothing_sell", new[] { "You have nothing to sell.", "Nie masz nic do sprzedania.", "Du har ingenting \u00e5 selge." } },
        { "shop.msg_sold_treasures", new[] { "Sold treasures for ${0}.", "Sprzedano skarby za ${0}.", "Solgte skatter for ${0}." } },
        { "shop.msg_choose_item", new[] { "Choose an item to sell.", "Wybierz przedmiot do sprzedania.", "Velg en ting \u00e5 selge." } },
        { "shop.msg_item_missing", new[] { "That item is no longer in your backpack.", "Tego przedmiotu nie ma ju\u017c w plecaku.", "Den tingen er ikke lenger i ryggsekken." } },
        { "shop.msg_sold_item", new[] { "Sold {0} for ${1}.", "Sprzedano {0} za ${1}.", "Solgte {0} for ${1}." } },
        { "shop.msg_not_connected", new[] { "Shop is not connected.", "Sklep nie jest pod\u0142\u0105czony.", "Butikken er ikke koblet til." } },
        { "shop.msg_range_maxed", new[] { "Detector range is maxed.", "Zasi\u0119g detektora jest maksymalny.", "Detektorrekkevidden er maks." } },
        { "shop.msg_not_enough", new[] { "Not enough money. Need ${0}.", "Za ma\u0142o pieni\u0119dzy. Potrzeba ${0}.", "Ikke nok penger. Trenger ${0}." } },
        { "shop.msg_range_upgraded", new[] { "Range upgraded to {0}m.", "Zasi\u0119g ulepszony do {0} m.", "Rekkevidde oppgradert til {0} m." } },
        { "shop.msg_detector_maxed", new[] { "Detector model is maxed.", "Model detektora jest maksymalny.", "Detektormodellen er maks." } },
        { "shop.msg_detector_upgraded", new[] { "Detector upgraded: {0}. Better range and wider scan.", "Detektor ulepszony: {0}. Lepszy zasi\u0119g i szersze skanowanie.", "Detektor oppgradert: {0}. Bedre rekkevidde og bredere skann." } },
        { "shop.msg_backpack_maxed", new[] { "Backpack size is maxed.", "Rozmiar plecaka jest maksymalny.", "Ryggsekken er maks st\u00f8rrelse." } },
        { "shop.msg_backpack_upgraded", new[] { "Backpack upgraded to {0}x{0}.", "Plecak ulepszony do {0}x{0}.", "Ryggsekk oppgradert til {0}x{0}." } },
        { "shop.msg_shovel_already", new[] { "Shovel is already upgraded.", "\u0141opata jest ju\u017c ulepszona.", "Spaden er allerede oppgradert." } },
        { "shop.msg_shovel_equipped", new[] { "Clean shovel equipped. Digging power doubled.", "Czysta \u0142opata za\u0142o\u017cona. Si\u0142a kopania x2.", "Ren spade utstyrt. Gravekraft doblet." } },
        { "shop.msg_crafting_already", new[] { "Crafting station is already unlocked.", "Stacja craftingu jest ju\u017c odblokowana.", "Arbeidsbenken er allerede l\u00e5st opp." } },
        { "shop.msg_crafting_unlocked", new[] { "Crafting station unlocked.", "Stacja craftingu odblokowana.", "Arbeidsbenk l\u00e5st opp." } },
        { "shop.msg_go_trader", new[] { "Go to the trader first.", "Najpierw podejd\u017a do handlarza.", "G\u00e5 til handelsmannen f\u00f8rst." } },
        { "shop.location_hint", new[] { "Buy land at the sign next to each search area.", "Kup ziemi\u0119 przy tabliczce obok ka\u017cdego obszaru.", "Kj\u00f8p land ved skiltet ved hvert s\u00f8keomr\u00e5de." } },
        { "shop.prompt_talk", new[] { "Press E to talk to {0}", "Naci\u015bnij E, aby porozmawia\u0107 z {0}", "Trykk E for \u00e5 snakke med {0}" } },
        { "shop.detector_range", new[] { "Detector range: {0}", "Zasi\u0119g detektora: {0}", "Detektorrekkevidde: {0}" } },
        { "shop.sell_all", new[] { "Sell all treasures (${0})", "Sprzedaj wszystkie skarby (${0})", "Selg alle skatter (${0})" } },
        { "shop.upgrade_detector_to", new[] { "Upgrade detector to {0} (${1})", "Ulepsz detektor do {0} (${1})", "Oppgrader detektor til {0} (${1})" } },
        { "shop.detector_model_maxed", new[] { "Detector model maxed", "Model detektora maks.", "Detektormodell maks" } },
        { "shop.upgrade_backpack_to", new[] { "Upgrade backpack {0}x{0} -> {1}x{1} (${2})", "Ulepsz plecak {0}x{0} -> {1}x{1} (${2})", "Oppgrader ryggsekk {0}x{0} -> {1}x{1} (${2})" } },
        { "shop.backpack_size_maxed", new[] { "Backpack size maxed", "Rozmiar plecaka maks.", "Ryggsekkst\u00f8rrelse maks" } },
        { "shop.upgrade_shovel", new[] { "Upgrade shovel (${0})", "Ulepsz \u0142opat\u0119 (${0})", "Oppgrader spade (${0})" } },
        { "shop.unlock_crafting", new[] { "Unlock crafting (${0})", "Odblokuj crafting (${0})", "L\u00e5s opp arbeidsbenk (${0})" } },
        { "shop.buy_land_signs", new[] { "Buy land at plot signs", "Kup ziemi\u0119 przy tabliczkach", "Kj\u00f8p land ved skiltene" } },

        { "home.crafting_locked", new[] { "Buy the crafting station from the upgrade trader first.", "Najpierw kup stacj\u0119 craftingu u handlarza ulepsze\u0144.", "Kj\u00f8p arbeidsbenken hos oppgraderingshandleren f\u00f8rst." } },
        { "home.crafting_locked_prompt", new[] { "E - Crafting locked", "E - Crafting zablokowany", "E - Arbeidsbenk l\u00e5st" } },
        { "home.exit_prompt", new[] { "E - Leave house", "E - Wyjd\u017a z domku", "E - G\u00e5 ut av huset", "E - Haus verlassen", "E - Salir de la casa", "E - L\u00e4mna huset", "E - Forlad huset" } },
        { "home.sleep_prompt", new[] { "E - Sleep", "E - \u015apij", "E - Sov", "E - Schlafen", "E - Dormir", "E - Sov", "E - Sov" } },
        { "home.crafting_prompt", new[] { "E - Crafting", "E - Crafting", "E - Arbeidsbenk", "E - Herstellen", "E - Fabricar", "E - Tillverkning", "E - Fremstilling" } },
        { "home.storage_prompt", new[] { "E - Storage", "E - Skrzynia", "E - Lager", "E - Lager", "E - Almac\u00e9n", "E - F\u00f6rvaring", "E - Opbevaring" } },
        { "home.confirm_title", new[] { "Confirm Entry", "Potwierd\u017a wej\u015bcie", "Bekreft inngang", "Eintritt best\u00e4tigen", "Confirmar entrada", "Bekr\u00e4fta ing\u00e5ng", "Bekr\u00e6ft adgang" } },
        { "home.confirm_body", new[] { "Enter the house interior now?", "Wej\u015b\u0107 teraz do domku?", "G\u00e5 inn i huset n\u00e5?", "Jetzt das Haus betreten?", "\u00bfEntrar en la casa ahora?", "G\u00e5 in i huset nu?", "G\u00e5 ind i huset nu?" } },
        { "home.confirm_accept", new[] { "Confirm", "Potwierd\u017a", "Bekreft", "Best\u00e4tigen", "Confirmar", "Bekr\u00e4fta", "Bekr\u00e6ft" } },
        { "home.confirm_cancel", new[] { "Cancel", "Anuluj", "Avbryt", "Abbrechen", "Cancelar", "Avbryt", "Annuller" } },
        { "home.no_home", new[] { "No home found.", "Nie znaleziono domu.", "Fant ikke huset." } },
        { "home.no_player", new[] { "No player found.", "Nie znaleziono gracza.", "Fant ingen spiller." } },
        { "home.backpack_empty", new[] { "Backpack is empty.", "Plecak jest pusty.", "Ryggsekken er tom." } },
        { "home.stored_summary", new[] { "Stored {0} item(s), value ${1}.", "Schowano {0} przedmiot\u00f3w o warto\u015bci ${1}.", "Lagret {0} ting med verdi ${1}." } },
        { "home.no_backpack", new[] { "No backpack found.", "Nie znaleziono plecaka.", "Fant ingen ryggsekk." } },
        { "home.storage_empty", new[] { "Storage is empty.", "Skrzynia jest pusta.", "Lageret er tomt." } },
        { "home.backpack_full", new[] { "Backpack is full.", "Plecak jest pe\u0142ny.", "Ryggsekken er full." } },
        { "home.took_summary", new[] { "Took {0} item(s), value ${1}.", "Zabrano {0} przedmiot\u00f3w o warto\u015bci ${1}.", "Tok {0} ting med verdi ${1}." } },
        { "home.sleep_after", new[] { "You can sleep after 20:00.", "Mo\u017cesz spa\u0107 po 20:00.", "Du kan sove etter kl. 20:00." } },
        { "home.slept_reset", new[] { "You slept until morning. Treasures reset.", "Przespa\u0142e\u015b do rana. Skarby zosta\u0142y odnowione.", "Du sov til morgenen. Skattene ble tilbakestilt." } },
        { "home.slept", new[] { "You slept until morning.", "Przespa\u0142e\u015b do rana.", "Du sov til morgenen." } },
        { "home.crafting_grid_full", new[] { "Crafting grid is full.", "Siatka craftingu jest pe\u0142na.", "Arbeidsbenken er full." } },
        { "home.placed_item", new[] { "Placed {0}.", "Umieszczono {0}.", "Plasserte {0}." } },
        { "home.returned_item", new[] { "Returned {0}.", "Zwr\u00f3cono {0}.", "La tilbake {0}." } },
        { "home.storage_full", new[] { "Storage is full.", "Skrzynia jest pe\u0142na.", "Lageret er fullt." } },
        { "home.stored_item", new[] { "Stored {0}.", "Schowano {0}.", "Lagret {0}." } },
        { "home.took_item", new[] { "Took {0}.", "Zabrano {0}.", "Tok {0}." } },
        { "home.backpack_title", new[] { "Backpack", "Plecak", "Ryggsekk" } },
        { "home.storage_title", new[] { "Storage 10x10", "Skrzynia 10x10", "Lager 10x10" } },
        { "home.storage_help", new[] { "Click an item to move | ESC - Close", "Kliknij przedmiot, aby go przenie\u015b\u0107 | ESC - Zamknij", "Klikk p\u00e5 en ting for \u00e5 flytte den | ESC - Lukk" } },
        { "home.crafting_title", new[] { "Crafting", "Crafting", "Arbeidsbenk" } },
        { "home.recipes_title", new[] { "Recipes", "Przepisy", "Oppskrifter" } },
        { "home.close_hint", new[] { "ESC - Close", "ESC - Zamknij", "ESC - Lukk" } },
        { "home.ready", new[] { "Ready", "Gotowe", "Klar" } },
        { "home.missing", new[] { "Missing: {0}", "Brakuje: {0}", "Mangler: {0}" } },
        { "home.slot_occupied", new[] { "Crafting slot is occupied.", "Slot craftingu jest zaj\u0119ty.", "Plassen p\u00e5 arbeidsbenken er opptatt." } },
        { "home.need_space", new[] { "Need {0}x{1} space in backpack.", "Potrzeba miejsca {0}x{1} w plecaku.", "Trenger {0}x{1} plass i ryggsekken." } },
        { "home.crafted_item", new[] { "Crafted {0}!", "Wytworzono {0}!", "Laget {0}!" } },

        { "area.claim_area", new[] { "Claim Area", "Odbierz obszar", "Ta omr\u00e5de" } },
        { "area.buy_area", new[] { "Buy Area", "Kup obszar", "Kj\u00f8p omr\u00e5de" } },
        { "area.claim", new[] { "Claim", "Odbierz", "Ta" } },
        { "area.buy", new[] { "Buy", "Kup", "Kj\u00f8p" } },
        { "area.free", new[] { "Free", "Za darmo", "Gratis" } },
        { "area.sign_not_connected", new[] { "This sign is not connected to a search area.", "Ta tabliczka nie jest pod\u0142\u0105czona do obszaru poszukiwa\u0144.", "Dette skiltet er ikke koblet til et s\u00f8keomr\u00e5de." } },
        { "area.already_unlocked", new[] { "{0} is already unlocked.", "{0} jest ju\u017c odblokowane.", "{0} er allerede l\u00e5st opp." } },
        { "area.no_inventory", new[] { "No player inventory found.", "Nie znaleziono ekwipunku gracza.", "Fant ingen spillerinventar." } },
        { "area.need_money", new[] { "Need ${0} to unlock {1}.", "Potrzeba ${0}, aby odblokowa\u0107 {1}.", "Trenger ${0} for \u00e5 l\u00e5se opp {1}." } },
        { "area.unlocked", new[] { "Unlocked {0}.", "Odblokowano {0}.", "L\u00e5ste opp {0}." } },
        { "area.confirm_title", new[] { "Confirm Plot", "Potwierd\u017a dzia\u0142k\u0119", "Bekreft felt" } },
        { "area.confirm_body_claim", new[] { "Claim {0} for free?", "Odebra\u0107 {0} za darmo?", "Ta {0} gratis?" } },
        { "area.confirm_body_buy", new[] { "Buy {0} for ${1}?", "Kupi\u0107 {0} za ${1}?", "Kj\u00f8p {0} for ${1}?" } },
        { "area.confirm_accept", new[] { "Confirm", "Potwierd\u017a", "Bekreft" } },
        { "area.confirm_cancel", new[] { "Cancel", "Anuluj", "Avbryt" } },
        { "area.forest_sign", new[] { "Forest\nBuy ${0}", "Las\nKup ${0}", "Skog\nKj\u00f8p ${0}", "Wald\nKaufen ${0}", "Bosque\nComprar ${0}", "Skog\nK\u00f6p ${0}", "Skov\nK\u00f8b ${0}" } },

        { "quest.jobs", new[] { "Escape Plan", "Plan ucieczki", "Fluktplan" } },
        { "quest.help", new[] { "Mira has a way off the island. Finish her requests in order: parts first, then money, then the final tide.", "Mira zna drog\u0119 z wyspy. R\u00f3b jej pro\u015bby po kolei: najpierw cz\u0119\u015bci, potem pieni\u0105dze, potem ostatni przyp\u0142yw.", "Mira har en vei bort fra \u00f8ya. Fullf\u00f8r oppdragene i rekkef\u00f8lge: deler f\u00f8rst, s\u00e5 penger, s\u00e5 siste flo." } },
        { "quest.close", new[] { "ESC - Close", "ESC - Zamknij", "ESC - Lukk" } },
        { "quest.talk_to", new[] { "E - Talk to {0}", "E - Porozmawiaj z {0}", "E - Snakk med {0}" } },
        { "quest.already_done", new[] { "This job is already done.", "To zadanie jest ju\u017c zrobione.", "Dette oppdraget er allerede gjort." } },
        { "quest.no_backpack", new[] { "No backpack found.", "Nie znaleziono plecaka.", "Fant ingen ryggsekk." } },
        { "quest.need_items", new[] { "You need {0}x {1}.", "Potrzebujesz {0}x {1}.", "Du trenger {0}x {1}." } },
        { "quest.need_money", new[] { "You need ${0}.", "Potrzebujesz ${0}.", "Du trenger ${0}." } },
        { "quest.items_missing", new[] { "Those items are no longer in your backpack.", "Tych przedmiot\u00f3w nie ma ju\u017c w plecaku.", "Disse tingene er ikke lenger i ryggsekken." } },
        { "quest.complete_paid", new[] { "Quest complete: +${0}.", "Zadanie uko\u0144czone: +${0}.", "Oppdrag fullf\u00f8rt: +${0}." } },
        { "quest.complete_story", new[] { "Quest complete. The escape plan moves forward.", "Zadanie uko\u0144czone. Plan ucieczki idzie dalej.", "Oppdrag fullf\u00f8rt. Fluktplanen g\u00e5r videre." } },
        { "quest.need_reward", new[] { "Need: {0}x {1} ({2}/{0})  Reward: ${3}", "Potrzeba: {0}x {1} ({2}/{0})  Nagroda: ${3}", "Trenger: {0}x {1} ({2}/{0})  Bel\u00f8nning: ${3}" } },
        { "quest.requires", new[] { "Need:", "Potrzeba:", "Trenger:" } },
        { "quest.reward_cash", new[] { "Reward: ${0}", "Nagroda: ${0}", "Bel\u00f8nning: ${0}" } },
        { "quest.reward_story", new[] { "Reward: progress", "Nagroda: post\u0119p", "Bel\u00f8nning: fremgang" } },
        { "quest.reward_escape", new[] { "Reward: leave the island", "Nagroda: ucieczka z wyspy", "Bel\u00f8nning: forlat \u00f8ya" } },
        { "quest.reward_cash_short", new[] { "+${0}", "+${0}", "+${0}" } },
        { "quest.reward_story_short", new[] { "Story", "Fabu\u0142a", "Historie" } },
        { "quest.reward_escape_short", new[] { "Escape", "Ucieczka", "Flukt" } },
        { "quest.confirm_title", new[] { "Confirm Delivery", "Potwierd\u017a oddanie", "Bekreft levering" } },
        { "quest.confirm_body", new[] { "Give Mira the items for \"{0}\"?", "Odda\u0107 Mirze przedmioty do \"{0}\"?", "Gi Mira tingene for \"{0}\"?" } },
        { "quest.confirm_reward", new[] { "Reward: {0}", "Nagroda: {0}", "Bel\u00f8nning: {0}" } },
        { "quest.confirm_accept", new[] { "Confirm", "Potwierd\u017a", "Bekreft" } },
        { "quest.confirm_cancel", new[] { "Cancel", "Anuluj", "Avbryt" } },
        { "quest.locked", new[] { "Finish the previous step first.", "Najpierw sko\u0144cz poprzedni etap.", "Fullf\u00f8r forrige steg f\u00f8rst." } },
        { "quest.locked_short", new[] { "Locked", "Zamkni\u0119te", "L\u00e5st" } },
        { "quest.finish_previous", new[] { "Mira will explain this once the previous step is done.", "Mira wyja\u015bni to po uko\u0144czeniu poprzedniego etapu.", "Mira forklarer dette n\u00e5r forrige steg er gjort." } },
        { "quest.missing_prefix", new[] { "Missing:", "Brakuje:", "Mangler:" } },
        { "quest.done", new[] { "Done", "Gotowe", "Ferdig" } },
        { "quest.deliver", new[] { "Deliver", "Oddaj", "Lever" } },
        { "quest.missing", new[] { "Missing", "Brakuje", "Mangler" } },
        { "quest.ending_title", new[] { "The Tide Takes You Home", "Przyp\u0142yw zabiera ci\u0119 do domu", "Floen tar deg hjem" } },
        { "quest.ending_body", new[] { "At dawn, Mira pushes the patched skiff into the gray water. The beacon clicks, the compass steadies, and the engine finally catches. The island fades behind the fog. You are not rich, not clean, and not entirely sure what you found out there, but you are free.", "O \u015bwicie Mira spycha za\u0142atan\u0105 \u0142\u00f3d\u017a na szar\u0105 wod\u0119. Radiolatarnia tyka, kompas trzyma kierunek, a silnik w ko\u0144cu odpala. Wyspa znika za mg\u0142\u0105. Nie jeste\u015b bogaty, nie jeste\u015b czysty i nie masz pewno\u015bci, co tak naprawd\u0119 znalaz\u0142e\u015b, ale jeste\u015b wolny.", "Ved daggry skyver Mira den lappede b\u00e5ten ut i gr\u00e5tt vann. N\u00f8dpeileren tikker, kompasset holder kursen, og motoren starter endelig. \u00d8ya forsvinner bak t\u00e5ken. Du er ikke rik, ikke ren, og ikke helt sikker p\u00e5 hva du fant der ute, men du er fri." } },
        { "quest.ending_close", new[] { "Continue", "Kontynuuj", "Fortsett" } },

        { "toast.found", new[] { "Found", "Znaleziono", "Funnet" } },
        { "toast.digging", new[] { "Digging", "Kopanie", "Graver" } },
        { "toast.value", new[] { "Value: ${0}", "Warto\u015b\u0107: ${0}", "Verdi: ${0}" } },
        { "toast.reward_jackpot", new[] { "JACKPOT!", "JACKPOT!", "JACKPOT!" } },
        { "toast.reward_valuable", new[] { "Great Find!", "\u015awietne znalezisko!", "Flott funn!" } },
        { "toast.reward_okay", new[] { "Nice Find", "Niez\u0142e znalezisko", "Fint funn" } },
        { "toast.reward_trash", new[] { "Found Something", "Co\u015b znaleziono", "Fant noe" } },
        { "toast.info", new[] { "Info", "Info", "Info" } },
        { "toast.too_dark", new[] { "Too Dark", "Za ciemno", "For m\u00f8rkt" } },
        { "toast.backpack_full", new[] { "Backpack Full", "Plecak pe\u0142ny", "Ryggsekken er full" } },
        { "toast.backpack_full_body", new[] { "Make room, then search the chest.", "Zr\u00f3b miejsce, potem przeszukaj skrzynk\u0119.", "Lag plass, og s\u00f8k i kisten etterp\u00e5." } },
        { "toast.no_target", new[] { "No Target", "Brak celu", "Ingen m\u00e5l" } },
        { "toast.notice", new[] { "Notice", "Uwaga", "Melding" } },
        { "toast.searching_chest", new[] { "Searching chest...", "Przeszukiwanie skrzynki...", "S\u00f8ker i kisten...", "Durchsuche die Truhe...", "Buscando en el cofre...", "S\u00f6ker i kistan...", "S\u00f8ger i kisten..." } },
        { "toast.chest_exposed", new[] { "Chest exposed. Press E to search.", "Skrzynka ods\u0142oni\u0119ta. Naci\u015bnij E, aby przeszuka\u0107.", "Kisten er gravd frem. Trykk E for \u00e5 s\u00f8ke." } },
        { "toast.digging_progress", new[] { "Unearthing target  {0} / {1}", "Odkopywanie celu  {0} / {1}", "Graver frem m\u00e5l  {0} / {1}" } },
        { "toast.scan_first", new[] { "Scan with the detector first.", "Najpierw zeskanuj detektorem.", "Skann med detektoren f\u00f8rst." } },
        { "toast.sleep_morning", new[] { "Sleep at home until morning.", "Prze\u015bpij si\u0119 w domu do rana.", "Sov hjemme til morgenen." } },
        { "toast.subtitle_jackpot", new[] { "Huge treasure found:", "Ogromny skarb znaleziony:", "Stor skatt funnet:" } },
        { "toast.subtitle_valuable", new[] { "You found something valuable:", "Znalaz\u0142e\u015b co\u015b warto\u015bciowego:", "Du fant noe verdifullt:" } },
        { "toast.subtitle_okay", new[] { "You found:", "Znalaz\u0142e\u015b:", "Du fant:" } },
        { "toast.subtitle_trash", new[] { "A tiny find:", "Ma\u0142e znalezisko:", "Et lite funn:" } }
    };

    public static event Action LanguageChanged;

    public static GameLanguage CurrentLanguage
    {
        get
        {
            int savedLanguage = PlayerPrefs.GetInt(LanguagePrefKey, (int)GameLanguage.English);
            return (GameLanguage)Mathf.Clamp(savedLanguage, 0, LanguageCount - 1);
        }
    }

    public static void SetLanguage(GameLanguage language)
    {
        if (CurrentLanguage == language)
        {
            return;
        }

        PlayerPrefs.SetInt(LanguagePrefKey, (int)language);
        PlayerPrefs.Save();
        LanguageChanged?.Invoke();
    }

    public static string T(string key)
    {
        if (!Texts.TryGetValue(key, out string[] values) || values == null || values.Length == 0)
        {
            return key;
        }

        int languageIndex = Mathf.Clamp((int)CurrentLanguage, 0, LanguageCount - 1);
        if (languageIndex >= values.Length || string.IsNullOrEmpty(values[languageIndex]))
        {
            return values[0];
        }

        return values[languageIndex];
    }

    public static string TFormat(string key, params object[] args)
    {
        return string.Format(T(key), args);
    }

    public static string GetLanguageName(GameLanguage language)
    {
        switch (language)
        {
            case GameLanguage.Polish:
                return T("language.polish");
            case GameLanguage.Norwegian:
                return T("language.norwegian");
            case GameLanguage.German:
                return T("language.german");
            case GameLanguage.Spanish:
                return T("language.spanish");
            case GameLanguage.Swedish:
                return T("language.swedish");
            case GameLanguage.Danish:
                return T("language.danish");
            default:
                return T("language.english");
        }
    }
}
