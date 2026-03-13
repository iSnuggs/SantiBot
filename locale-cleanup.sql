UPDATE GuildConfigs SET Locale = CASE WHEN Locale = 'zh-TW' THEN 'zh-CN' ELSE NULL END WHERE Locale IN ('zh-TW', 'it-IT', 'ro-RO', 'hu-HU', 'sv-SE', 'nl-NL', 'cs-CZ', 'da-DK', 'he-IL', 'nb-NO');
