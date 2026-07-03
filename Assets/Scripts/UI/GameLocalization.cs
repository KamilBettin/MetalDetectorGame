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

        { "tutorial.scan_sand", new[] { "Scan sand: {0}/{1}", "Skanuj piasek: {0}/{1}", "Skann sand: {0}/{1}" } },
        { "tutorial.find_first", new[] { "Find and dig up your first treasure", "Znajd\u017a i wykop pierwszy skarb", "Finn og grav opp din f\u00f8rste skatt" } },
        { "tutorial.sell_loot", new[] { "Sell your loot to the trader", "Sprzedaj \u0142up handlarzowi", "Selg funnene dine til handelsmannen" } },
        { "tutorial.complete", new[] { "Tutorial complete. Keep searching and upgrading.", "Samouczek uko\u0144czony. Szukaj dalej i ulepszaj sprz\u0119t.", "Oppl\u00e6ringen er ferdig. Fortsett \u00e5 s\u00f8ke og oppgradere." } },

        { "hud.detector", new[] { "Detector", "Detektor", "Detektor" } },
        { "hud.signal", new[] { "Signal", "Sygna\u0142", "Signal" } },
        { "hud.battery", new[] { "Battery", "Bateria", "Batteri" } },
        { "hud.cash", new[] { "cash", "got\u00f3wka", "penger" } },
        { "hud.cargo", new[] { "Cargo", "\u0141adunek", "Last" } },
        { "hud.tutorial", new[] { "Tutorial", "Samouczek", "Oppl\u00e6ring" } },
        { "hud.no_detector", new[] { "No detector", "Brak detektora", "Ingen detektor" } },
        { "hud.signal_marked", new[] { "target marked", "cel oznaczony", "m\u00e5l markert" } },
        { "hud.signal_scan_now", new[] { "scan now", "skanuj teraz", "skann n\u00e5" } },
        { "hud.hint_default", new[] { "Hold LMB - Scan sand | TAB backpack", "Przytrzymaj LPM - skanuj piasek | TAB plecak", "Hold venstre museknapp - skann sand | TAB ryggsekk" } },
        { "hud.hint_start", new[] { "Hold LMB scan | E dig/talk | TAB backpack", "Przytrzymaj LPM skan | E kop/rozmawiaj | TAB plecak", "Hold venstre museknapp skann | E grav/snakk | TAB ryggsekk" } },
        { "hud.hint_close_trader", new[] { "ESC - Close trader", "ESC - Zamknij handlarza", "ESC - Lukk handelsmann" } },
        { "hud.hint_close_jobs", new[] { "ESC - Close jobs", "ESC - Zamknij zadania", "ESC - Lukk oppdrag" } },
        { "hud.hint_close_backpack", new[] { "TAB / ESC - Close backpack", "TAB / ESC - Zamknij plecak", "TAB / ESC - Lukk ryggsekk" } },
        { "hud.hint_close_home", new[] { "ESC - Close home", "ESC - Zamknij dom", "ESC - Lukk hjem" } },
        { "hud.hint_talk_jobs", new[] { "E - Talk / Jobs", "E - Rozmowa / Zadania", "E - Snakk / Oppdrag" } },
        { "hud.hint_use_home", new[] { "E - Use home", "E - U\u017cyj domu", "E - Bruk hjem" } },
        { "hud.hint_night", new[] { "Night - searching disabled. Sleep at home.", "Noc - szukanie wy\u0142\u0105czone. Prze\u015bpij si\u0119 w domu.", "Natt - s\u00f8king er deaktivert. Sov hjemme." } },
        { "hud.action_jobs", new[] { "Jobs", "Zadania", "Oppdrag" } },
        { "hud.action_use_home", new[] { "Use Home", "U\u017cyj domu", "Bruk hjem" } },
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
        { "shop.detector_next", new[] { "Next: {0} scans {1}x{1} cells.", "Nast\u0119pny: {0} skanuje pola {1}x{1}.", "Neste: {0} skanner {1}x{1} felt." } },
        { "shop.detector_maxed", new[] { "Your detector scans the maximum 6x6 grid.", "Tw\u00f3j detektor skanuje maksymaln\u0105 siatk\u0119 6x6.", "Detektoren din skanner maks 6x6-rutenett." } },
        { "shop.backpack_size", new[] { "Backpack Size", "Rozmiar plecaka", "Ryggsekkst\u00f8rrelse" } },
        { "shop.backpack_description", new[] { "Adds another row and column to your backpack.", "Dodaje kolejny rz\u0105d i kolumn\u0119 w plecaku.", "Legger til en ny rad og kolonne i ryggsekken." } },
        { "shop.clean_shovel", new[] { "Clean Shovel", "Czysta \u0142opata", "Ren spade" } },
        { "shop.clean_shovel_description", new[] { "Replaces the rusty shovel animation with the clean silver shovel.", "Zamienia animacj\u0119 zardzewia\u0142ej \u0142opaty na czyst\u0105 srebrn\u0105.", "Bytter den rustne spaden med den rene s\u00f8lvspaden." } },
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
        { "shop.msg_detector_upgraded", new[] { "Detector upgraded: {0} ({1}x{1}).", "Detektor ulepszony: {0} ({1}x{1}).", "Detektor oppgradert: {0} ({1}x{1})." } },
        { "shop.msg_backpack_maxed", new[] { "Backpack size is maxed.", "Rozmiar plecaka jest maksymalny.", "Ryggsekken er maks st\u00f8rrelse." } },
        { "shop.msg_backpack_upgraded", new[] { "Backpack upgraded to {0}x{0}.", "Plecak ulepszony do {0}x{0}.", "Ryggsekk oppgradert til {0}x{0}." } },
        { "shop.msg_shovel_already", new[] { "Shovel is already upgraded.", "\u0141opata jest ju\u017c ulepszona.", "Spaden er allerede oppgradert." } },
        { "shop.msg_shovel_equipped", new[] { "Clean shovel equipped.", "Czysta \u0142opata za\u0142o\u017cona.", "Ren spade utstyrt." } },
        { "shop.msg_go_trader", new[] { "Go to the trader first.", "Najpierw podejd\u017a do handlarza.", "G\u00e5 til handelsmannen f\u00f8rst." } },
        { "shop.location_hint", new[] { "Buy land at the sign next to each search area.", "Kup ziemi\u0119 przy tabliczce obok ka\u017cdego obszaru.", "Kj\u00f8p land ved skiltet ved hvert s\u00f8keomr\u00e5de." } },
        { "shop.prompt_talk", new[] { "Press E to talk to {0}", "Naci\u015bnij E, aby porozmawia\u0107 z {0}", "Trykk E for \u00e5 snakke med {0}" } },
        { "shop.detector_range", new[] { "Detector range: {0}", "Zasi\u0119g detektora: {0}", "Detektorrekkevidde: {0}" } },
        { "shop.sell_all", new[] { "Sell all treasures (${0})", "Sprzedaj wszystkie skarby (${0})", "Selg alle skatter (${0})" } },
        { "shop.upgrade_detector_to", new[] { "Upgrade detector to {0} {1}x{1} (${2})", "Ulepsz detektor do {0} {1}x{1} (${2})", "Oppgrader detektor til {0} {1}x{1} (${2})" } },
        { "shop.detector_model_maxed", new[] { "Detector model maxed", "Model detektora maks.", "Detektormodell maks" } },
        { "shop.upgrade_backpack_to", new[] { "Upgrade backpack {0}x{0} -> {1}x{1} (${2})", "Ulepsz plecak {0}x{0} -> {1}x{1} (${2})", "Oppgrader ryggsekk {0}x{0} -> {1}x{1} (${2})" } },
        { "shop.backpack_size_maxed", new[] { "Backpack size maxed", "Rozmiar plecaka maks.", "Ryggsekkst\u00f8rrelse maks" } },
        { "shop.upgrade_shovel", new[] { "Upgrade shovel (${0})", "Ulepsz \u0142opat\u0119 (${0})", "Oppgrader spade (${0})" } },
        { "shop.buy_land_signs", new[] { "Buy land at plot signs", "Kup ziemi\u0119 przy tabliczkach", "Kj\u00f8p land ved skiltene" } },

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

        { "quest.jobs", new[] { "Jobs", "Zadania", "Oppdrag" } },
        { "quest.help", new[] { "Bring requested finds from your backpack for cash.", "Przynie\u015b wymagane znaleziska z plecaka za got\u00f3wk\u0119.", "Lever de etterspurte funnene fra ryggsekken for penger." } },
        { "quest.close", new[] { "ESC - Close", "ESC - Zamknij", "ESC - Lukk" } },
        { "quest.talk_to", new[] { "E - Talk to {0}", "E - Porozmawiaj z {0}", "E - Snakk med {0}" } },
        { "quest.already_done", new[] { "This job is already done.", "To zadanie jest ju\u017c zrobione.", "Dette oppdraget er allerede gjort." } },
        { "quest.no_backpack", new[] { "No backpack found.", "Nie znaleziono plecaka.", "Fant ingen ryggsekk." } },
        { "quest.need_items", new[] { "You need {0}x {1}.", "Potrzebujesz {0}x {1}.", "Du trenger {0}x {1}." } },
        { "quest.items_missing", new[] { "Those items are no longer in your backpack.", "Tych przedmiot\u00f3w nie ma ju\u017c w plecaku.", "Disse tingene er ikke lenger i ryggsekken." } },
        { "quest.complete_paid", new[] { "Quest complete. Paid ${0}.", "Zadanie uko\u0144czone. Zap\u0142acono ${0}.", "Oppdrag fullf\u00f8rt. Betalte ${0}." } },
        { "quest.need_reward", new[] { "Need: {0}x {1} ({2}/{0})  Reward: ${3}", "Potrzeba: {0}x {1} ({2}/{0})  Nagroda: ${3}", "Trenger: {0}x {1} ({2}/{0})  Bel\u00f8nning: ${3}" } },
        { "quest.done", new[] { "Done", "Gotowe", "Ferdig" } },
        { "quest.deliver", new[] { "Deliver", "Oddaj", "Lever" } },
        { "quest.missing", new[] { "Missing", "Brakuje", "Mangler" } },

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
