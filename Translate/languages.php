<?php

const LANG_CODES = [
// --- English variants ---
	'en-GB' => 'English (United Kingdom)',
	'en-IE' => 'English (Ireland)',
	'en-GI' => 'English (Gibraltar)',


// --- Romance languages ---
	'es-ES' => 'Español (España) - Spanish (Spain)',
	'es-MX' => 'Español (México) - Spanish (Mexico)',
	'fr-FR' => 'Français (France) - French (France)',
	'fr-CA' => 'Français (Canada) - French (Canada)',
	'fr-BE' => 'Français (Belgique) - French (Belgium)',
	'fr-CH' => 'Français (Suisse) - French (Switzerland)',
	'fr-LU' => 'Français (Luxembourg) - French (Luxembourg)',
	'fr-MC' => 'Français (Monaco) - French (Monaco)',
	'fr-AD' => 'Français (Andorre) - French (Andorra)',
	'it-IT' => 'Italiano (Italia) - Italian',
	'it-CH' => 'Italiano (Svizzera) - Italian (Switzerland)',
	'pt-PT' => 'Português (Portugal) - Portuguese (Portugal)',
	'pt-BR' => 'Português (Brasil) - Portuguese (Brazil)',


// --- Germanic languages ---
	'de-DE' => 'Deutsch (Deutschland) - German',
	'de-AT' => 'Deutsch (Österreich) - German (Austria)',
	'de-CH' => 'Deutsch (Schweiz) - German (Switzerland)',
	'de-LU' => 'Deutsch (Luxemburg) - German (Luxembourg)',
	'de-LI' => 'Deutsch (Liechtenstein) - German (Liechtenstein)',
	'de-BE' => 'Deutsch (Belgien) - German (Belgium)',
	'nl-NL' => 'Nederlands (Nederland) - Dutch (Netherlands)',
	'nl-BE' => 'Nederlands (België) - Dutch (Belgium/Flemish)',
	'fy-NL' => 'Frysk (Nederlân) - Frisian (Netherlands)',
	'sv-SE' => 'Svenska (Sverige) - Swedish (Sweden)',
	'sv-FI' => 'Svenska (Finland) - Swedish (Finland)',
	'da-DK' => 'Dansk (Danmark) - Danish (Denmark)',
	'nb-NO' => 'Norsk bokmål (Norge) - Norwegian Bokmål (Norway)',
	'nn-NO' => 'Norsk nynorsk (Norge) - Norwegian Nynorsk (Norway)',
	'is-IS' => 'Íslenska (Ísland) - Icelandic (Iceland)',


// --- Slavic languages ---
	'ru-RU' => 'Русский (Россия) - Russian',
	'uk-UA' => 'Українська (Україна) - Ukrainian',
	'pl-PL' => 'Polski (Polska) - Polish',
	'cs-CZ' => 'Čeština (Česká republika) - Czech',
	'sk-SK' => 'Slovenčina (Slovensko) - Slovak',
	'hr-HR' => 'Hrvatski (Hrvatska) - Croatian',
	'sr-RS' => 'Српски (Србија) - Serbian',
	'sr-Latn-RS' => 'Srpski (Srbija) - Serbian Latin (Serbia)',
	'bs-BA' => 'Bosanski (Bosna i Hercegovina) - Bosnian',
	'bg-BG' => 'Български (България) - Bulgarian',
	'mk-MK' => 'Македонски (С. Македонија) - Macedonian',
	'sl-SI' => 'Slovenščina (Slovenija) - Slovenian',
	'be-BY' => 'Беларуская (Беларусь) - Belarusian',


// --- Baltic & Uralic ---
	'et-EE' => 'Eesti (Eesti) - Estonian',
	'lv-LV' => 'Latviešu (Latvija) - Latvian',
	'lt-LT' => 'Lietuvių (Lietuva) - Lithuanian',
	'fi-FI' => 'Suomi (Suomi) - Finnish',
	'mt-MT' => 'Malti (Malta) - Maltese',


// --- Greek, Turkish, Caucasus ---
	'el-GR' => 'Ελληνικά (Ελλάδα) - Greek (Greece)',
	'el-CY' => 'Ελληνικά (Κύπρος) - Greek (Cyprus)',
	'tr-TR' => 'Türkçe (Türkiye) - Turkish',
	'ka-GE' => 'ქართული (საქართველო) - Georgian',
	'hy-AM' => 'Հայերեն (Հայաստան) - Armenian',
	'az-AZ' => 'Azərbaycan (Azərbaycan) - Azerbaijani',


// --- Other European regionals ---
	'ca-ES' => 'Català (Espanya) - Catalan',
	'gl-ES' => 'Galego (España) - Galician',
	'eu-ES' => 'Euskara (Espainia) - Basque',
	'oc-FR' => 'Occitan (França) - Occitan',
	'rm-CH' => 'Rumantsch (Svizra) - Romansh (Switzerland)',
	'lb-LU' => 'Lëtzebuergesch (Lëtzebuerg) - Luxembourgish',
	'fo-FO' => 'Føroyskt (Føroyar) - Faroese',
	'se-NO' => 'Davvisámegiella (Norga) - Northern Sámi',
	'ga-IE' => 'Gaeilge (Éire) - Irish',
	'cy-GB' => 'Cymraeg (Cymru) - Welsh',
	'gd-GB' => 'Gàidhlig (Alba) - Scottish Gaelic',
	'kw-GB' => 'Kernewek (Kernow) - Cornish',


// --- Major world languages outside Europe ---
	'zh-CN' => '中文 (简体, 中国) - Chinese (Simplified, China)',
	'zh-TW' => '中文 (繁體, 台灣) - Chinese (Traditional, Taiwan)',
	'zh-HK' => '中文 (香港) - Chinese (Hong Kong)',
	'ja-JP' => '日本語 (日本) - Japanese',
	'ko-KR' => '한국어 (대한민국) - Korean',
	'vi-VN' => 'Tiếng Việt (Việt Nam) - Vietnamese',
	'th-TH' => 'ไทย (ประเทศไทย) - Thai',
	'id-ID' => 'Bahasa Indonesia (Indonesia)',
	'ms-MY' => 'Bahasa Melayu (Malaysia)',
	'fil-PH' => 'Filipino (Pilipinas)',
	'hi-IN' => 'हिन्दी (भारत) - Hindi',
	'bn-IN' => 'বাংলা (ভারত) - Bengali (India)',
	'bn-BD' => 'বাংলা (বাংলাদেশ) - Bengali (Bangladesh)',
	'pa-IN' => 'ਪੰਜਾਬੀ (ਭਾਰਤ) - Punjabi (India)',
	'pa-PK' => 'پنجابی (پاکستان) - Punjabi (Pakistan)',
	'gu-IN' => 'ગુજરાતી (ભારત) - Gujarati',
	'ta-IN' => 'தமிழ் (இந்தியா) - Tamil',
	'te-IN' => 'తెలుగు (భారతదేశం) - Telugu',
	'ml-IN' => 'മലയാളം (ഇന്ത്യ) - Malayalam',
	'kn-IN' => 'ಕನ್ನಡ (ಭಾರತ) - Kannada',
	'mr-IN' => 'मराठी (भारत) - Marathi',
	'or-IN' => 'ଓଡ଼ିଆ (ଭାରତ) - Odia',
	'ne-NP' => 'नेपाली (नेपाल) - Nepali',
	'si-LK' => 'සිංහල (ශ්‍රී ලංකා) - Sinhala',
	'am-ET' => 'አማርኛ (ኢትዮጵያ) - Amharic',
	'yo-NG' => 'Yorùbá (Nigeria)',
	'ig-NG' => 'Igbo (Nigeria)',
	'ha-NG' => 'Hausa (Nigeria)',
	'zu-ZA' => 'Zulu (South Africa)',
	'xh-ZA' => 'Xhosa (South Africa)',
	'af-ZA' => 'Afrikaans (Suid-Afrika)',
	'st-ZA' => 'Sesotho (South Africa)',
	'sn-ZW' => 'Shona (Zimbabwe)',
	'rw-RW' => 'Kinyarwanda (Rwanda)',
	'mg-MG' => 'Malagasy (Madagascar)',


// --- Extras & constructed ---
	'eo-001' => 'Esperanto (World)'
];
