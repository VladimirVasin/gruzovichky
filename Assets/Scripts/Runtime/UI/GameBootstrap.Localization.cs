using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private enum GameLanguage
    {
        English,
        Russian
    }

    private static GameLanguage selectedLanguage = GameLanguage.Russian;
    private float localizationRefreshTimer;

    private static readonly Dictionary<string, string> RussianUi = new()
    {
        { "Continue", "Продолжить" },
        { "New Game Debug", "Новая игра: Debug" },
        { "New Game User", "Новая игра" },
        { "Exit", "Выход" },
        { "Language:", "Язык:" },
        { "Loading...", "Загрузка..." },
        { "Camera & lighting...", "Камера и свет..." },
        { "Populating water...", "Заполняем воду..." },
        { "Setting up locations...", "Размещаем локации..." },
        { "Building road network...", "Строим дорожную сеть..." },
        { "Generating terrain...", "Генерируем землю..." },
        { "Smoothing terrain...", "Сглаживаем землю..." },
        { "Applying terrain...", "Применяем рельеф..." },
        { "Setting up ground...", "Настраиваем почву..." },
        { "Building grid...", "Строим сетку..." },
        { "Edge highways...", "Магистраль..." },
        { "Atmosphere...", "Атмосфера..." },
        { "Road lanterns...", "Фонари..." },
        { "Planting trees...", "Сажаем деревья..." },
        { "Placing benches...", "Ставим лавочки..." },
        { "Wildlife...", "Живность..." },
        { "Water effects...", "Эффекты воды..." },
        { "Visual tools...", "Визуальные инструменты..." },
        { "Vehicles...", "Транспорт..." },
        { "Audio...", "Звук..." },
        { "Fleet UI...", "Интерфейс автопарка..." },
        { "Economy UI...", "Интерфейс экономики..." },
        { "Build UI...", "Интерфейс строительства..." },
        { "HUD...", "HUD..." },
        { "Finishing up...", "Завершаем..." },
        { "Done!", "Готово!" },

        { "Welcome to Lo-Fi Delivery Co.", "Добро пожаловать в Lo-Fi Delivery Co." },
        { "Alright, buddy, you've been handed a problem the size of a small town. For some reason, it even comes with a steering wheel.\n\nThe map is empty - right where the road is supposed to be. You'll have to... convince it to appear.\nAnd buildings, unfortunately, won't build themselves.\n\nStart gently: take a look around, fill in what's missing, and try not to go bankrupt before noon.", "Итак, дружище, тебе досталась проблема размером с городок. К ней зачем-то прикручен руль.\n\nНа карте — пусто. Ровно там, где должна быть дорога. Придётся её… убедить появиться.\nДа и здания, к несчастью, сами себя не построят.\n\nНачни мягко: осмотрись, поставь недостающее и постарайся не обанкротиться до обеда." },
        { "You are starting a small logistics town. In this mode the world begins lean: some important buildings must be placed manually from the Build menu.\n\nStart by looking around, planning roads, and deciding where your first service and production buildings should go.", "Ты начинаешь маленький логистический городок. В этом режиме мир стартует проще: часть важных зданий нужно построить вручную через меню строительства.\n\nОсмотрись, спланируй дороги и реши, где появятся первые сервисные и производственные здания." },
        { "Skip tutorial", "Пропустить обучение" },
        { "OK", "OK" },
        { "Build a Motel", "Построй Mотель" },
        { "A motel is a soft landing for people who do not yet understand what they have agreed to.\n\nIn Lo-Fi Delivery Co. it is where drivers are hired, wait between shifts, and pretend the road has not already taken something from them.\n\nOpen the Building menu at the top - or press B - choose Motel, and place it wherever your optimism still fits.", "Мотель — это мягкая посадка для людей, которые ещё не поняли, на что подписались.\n\nВ Lo-Fi Delivery Co. здесь нанимают водителей, они ждут между сменами и делают вид, что дорога ещё ничего у них не забрала.\n\nОткрой меню Building сверху — или нажми B — выбери Motel и поставь его там, где ещё помещается твой оптимизм." },
        { "Open the Workers Panel", "Открой панель Рабочих" },
        { "The motel stands. Structurally sound, morally ambiguous - the usual.\n\nNow it needs a person inside it. Someone to drive the truck, carry the crates, and ask no questions about where the money went.\n\nOpen the Workers panel at the top of the screen.", "Мотель стоит. С точки зрения конструкции — надёжно. Морально — вопросы есть.\n\nТеперь туда нужен человек. Кто-то, кто будет вести грузовик, таскать ящики и не задавать лишних вопросов о том, куда делись деньги.\n\nОткрой панель Workers в верхней части экрана." },
        { "Hire a Worker", "Найми рабочего" },
        { "The Workers panel is open. Good. You are further along than most people get.\n\nThere is a button at the bottom. Hire New Worker. It costs money — some of which you still have.\n\nPress it.", "Панель Workers открыта. Хорошо. Ты уже дальше, чем большинство.\n\nВнизу есть кнопка. Hire New Worker. Она стоит денег — часть которых у тебя ещё есть.\n\nНажми её." },
        { "The Worker is on Their Way!", "Рабочий уже едет!" },
        { "A human being will arrive by bus, already wearing the expression of someone who has made several poor decisions to get here.\n\nThat is your employee now. Treat them accordingly.", "Приедет автобус. Из него выйдет человек с видом того, кто принял несколько плохих решений, чтобы оказаться здесь.\n\nТеперь это твой сотрудник. Обращайся соответственно." },
        { "Your Land is Rich in Timber", "Наш край богат лесами" },
        { "Our region is blessed with magnificent forests that have been patiently waiting to be logged.\n\nA Forest provides raw logs. A Sawmill turns them into boards. That is where the money starts.\n\nPut your new worker to work — assign them a shift at the Forest so the timber starts moving.", "Наш край славится своими прекрасными лесами, которые давно ждут, чтобы их пустили в дело.\n\nЛес даёт брёвна. Лесопилка превращает их в доски. Вот откуда берутся деньги.\n\nПусти нового рабочего в дело — назначь ему смену в Лесу, чтобы лес начал двигаться." },

        { "Fleet", "Автопарк" },
        { "Workers", "Рабочие" },
        { "Drivers", "Водители" },
        { "Shifts", "Смены" },
        { "Resources", "Ресурсы" },
        { "Economy", "Экономика" },
        { "Build", "Стройка" },
        { "Building", "Стройка" },
        { "R - rotate", "R - повернуть" },
        { "Map", "Карта" },
        { "Speed", "Скорость" },
        { "Time", "Время" },
        { "Treasury", "Казна" },
        { "Morning", "Утро" },
        { "Day", "День" },
        { "Evening", "Вечер" },
        { "Night", "Ночь" },
        { "Paused", "Пауза" },
        { "PAUSE", "ПАУЗА" },
        { "Shift Management", "Управление сменами" },
        { "Idle", "Свободен" },
        { "Assigned", "Назначен" },
        { "No drivers assigned", "Водители не назначены" },
        { "Assign", "Назначить" },
        { "Remove", "Убрать" },
        { "Intercity", "Межгород" },
        { "No driver assigned", "Водитель не назначен" },
        { "Assign one dedicated driver to inter city duty", "Назначьте одного водителя на междугородние рейсы" },

        { "Truck Details", "Грузовик" },
        { "Truck Overview", "Обзор грузовика" },
        { "Current State", "Текущее состояние" },
        { "Assigned Driver", "Назначенный водитель" },
        { "Select Driver", "Выбрать водителя" },
        { "No available drivers.", "Нет доступных водителей." },
        { "No truck selected. Select a truck from the fleet list.", "Грузовик не выбран. Выбери грузовик из списка." },
        { "Buy New Truck", "Купить грузовик" },
        { "Fuel", "Топливо" },
        { "Energy", "Энергия" },
        { "Cargo", "Груз" },
        { "Navigation", "Навигация" },
        { "Route", "Маршрут" },
        { "Payout", "Оплата" },
        { "Follow Camera", "Следить камерой" },
        { "Exit Follow", "Выйти из камеры" },

        { "Hire Driver", "Нанять водителя" },
        { "Hire Worker", "Нанять рабочего" },
        { "No workers yet.", "Рабочих пока нет." },
        { "No drivers yet.", "Водителей пока нет." },
        { "Salary", "Зарплата" },
        { "Money", "Деньги" },
        { "Status", "Статус" },
        { "Duty", "Роль" },
        { "Local", "Локальный" },
        { "Logistics", "Логистика" },

        { "Build Menu", "Строительство" },
        { "Road", "Дорога" },
        { "Sawmill", "Лесопилка" },
        { "Motel", "Мотель" },
        { "Bar", "Бар" },
        { "Canteen", "Столовая" },
        { "Furniture Factory", "Мебельная фабрика" },
        { "Open Resources", "Открыть ресурсы" },

        { "Warehouse", "Склад" },
        { "Parking", "Парковка" },
        { "Gas Station", "Заправка" },
        { "Fuel Stop", "Заправка" },
        { "Forest", "Лес" },
        { "Bus Stop", "Автобусная остановка" },
        { "Service Fee", "Стоимость услуги" },
        { "Workers inside", "Рабочих внутри" },
        { "Finished goods storage", "Склад готовой продукции" },
        { "Production paused at night", "Производство остановлено ночью" },

        { "Logs", "Брёвна" },
        { "Boards", "Доски" },
        { "Cotton", "Хлопок" },
        { "Textile", "Ткань" },
        { "Furniture", "Мебель" },

        { "Economy & Trade", "Экономика и торговля" },
        { "Trade Dispatch", "Торговый рейс" },
        { "Resource", "Ресурс" },
        { "Offer", "Предложение" },
        { "Mode", "Режим" },
        { "Buy / Imports", "Покупка / импорт" },
        { "Sell / Exports", "Продажа / экспорт" },
        { "Send on Trade Run", "Отправить в торговый рейс" },
        { "No money movements yet.", "Движений денег пока нет." },
        { "Assign an Intercity driver to unlock trade dispatch.", "Назначьте межгородского водителя, чтобы открыть торговлю." },
        { "Ready to dispatch via edge highway", "Готово к рейсу через магистраль" },
        { "Intercity driver is busy", "Межгородский водитель занят" },

        { "Regional Map", "Карта регионов" },
        { "Open/Close: M", "Открыть/закрыть: M" },
        { "Region Grid", "Сетка регионов" },
        { "Region Details", "Детали региона" },
        { "Current region", "Текущий регион" },
        { "External region", "Внешний регион" },
        { "Unsurveyed region", "Неисследованный регион" },
        { "Produced Resources", "Производимые ресурсы" },
        { "Regional Notes", "Заметки региона" },
        { "Current Trade Context", "Текущий торговый контекст" },
        { "Your Town", "Твой город" },
        { "Textile District", "Текстильный район" },
        { "Cotton Plains", "Хлопковые равнины" },
        { "River Port", "Речной порт" },
        { "North Ridge", "Северный кряж" },
        { "Forest Belt", "Лесной пояс" },
        { "Dry South", "Сухой юг" },
        { "Freight Steppe", "Грузовая степь" },
        { "Coastal Gate", "Прибрежные ворота" },
        { "UNKNOWN", "НЕИЗВЕСТНО" },
        { "LOCAL", "ЛОКАЛЬНО" },
        { "ROUTE", "МАРШРУТ" },
        { "TEXTILE", "ТКАНЬ" },
        { "COTTON", "ХЛОПОК" },

        { "Cell", "Клетка" },
        { "Ground", "Земля" },
        { "Water", "Вода" },
        { "Grass", "Трава" },
        { "Sand", "Песок" },
        { "None", "Нет" },
        { "Unknown", "Неизвестно" },
        { "Location", "Локация" },
        { "Forest -> Sawmill", "Лес -> Лесопилка" },
        { "Sawmill -> Warehouse", "Лесопилка -> Склад" },
        { "Warehouse -> Furniture Factory (Boards)", "Склад -> Мебельная фабрика (Доски)" },
        { "Warehouse -> Furniture Factory (Textile)", "Склад -> Мебельная фабрика (Ткань)" },
        { "Furniture Factory -> Warehouse", "Мебельная фабрика -> Склад" },
        { "Selected building", "Выбранное здание" },
        { "No workers assigned", "Рабочие не назначены" },
        { "No trucks inside", "Грузовиков внутри нет" },
        { "The fleet is currently out on routes.", "Автопарк сейчас на маршрутах." },
        { "Available Routes", "Доступные маршруты" },
        { "No routes available right now.", "Сейчас маршрутов нет." },
        { "Routes appear when cargo and roads exist.", "Маршруты появятся, когда есть груз и дороги." },
        { "Assign route", "Назначить маршрут" },
        { "No trips available.", "Рейсов нет." },
    };

    private static bool IsRussianLanguage() => selectedLanguage == GameLanguage.Russian;

    private static string L(string value)
    {
        if (!IsRussianLanguage() || string.IsNullOrEmpty(value))
        {
            return value;
        }

        if (RussianUi.TryGetValue(value, out string translated))
        {
            return translated;
        }

        return LocalizeCommonFragments(value);
    }

    private static string LocalizeCommonFragments(string value)
    {
        string translated = value;
        foreach (KeyValuePair<string, string> pair in RussianUi)
        {
            if (pair.Key.Length < 4)
            {
                continue;
            }

            translated = translated.Replace(pair.Key, pair.Value);
        }

        translated = translated
            .Replace("Import x1", "Импорт x1")
            .Replace("Export x1", "Экспорт x1")
            .Replace("Treasury", "Казна")
            .Replace("Worker", "Рабочий")
            .Replace("Driver", "Водитель")
            .Replace("Truck", "Грузовик")
            .Replace("Open", "Открыть")
            .Replace("Close", "Закрыть")
            .Replace("Ready", "Готово")
            .Replace("Busy", "Занят")
            .Replace("No ", "Нет ")
            .Replace("Buy", "Купить")
            .Replace("Sell", "Продать");

        return translated;
    }

    private static string ToEnglishIfKnown(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        foreach (KeyValuePair<string, string> pair in RussianUi)
        {
            if (pair.Value == value)
            {
                return pair.Key;
            }
        }

        return value;
    }

    private void SetLanguage(GameLanguage language)
    {
        if (selectedLanguage == language)
        {
            return;
        }

        selectedLanguage = language;
        SessionDebugLogger.Log("UI", $"Language switched to {selectedLanguage}.");
        RefreshLocalizedUi();
        PlayUiSound(uiSelectClip, 0.82f);
    }

    private void RefreshLocalizedUi()
    {
        UpdateMainMenuTexts();
        RefreshAllTextComponentsForLanguage();
        isFleetScreenDirty = true;
        isDriversScreenDirty = true;
        isShiftsScreenDirty = true;
        isEconomyScreenDirty = true;
        isBuildScreenDirty = true;
        isWorldMapScreenDirty = true;
    }

    private void RefreshAllTextComponentsForLanguage()
    {
        Text[] texts = FindObjectsByType<Text>(FindObjectsInactive.Include);
        foreach (Text text in texts)
        {
            string english = ToEnglishIfKnown(text.text);
            text.text = L(english);
        }
    }

    private void UpdateRuntimeLocalizationTick()
    {
        if (!IsRussianLanguage())
        {
            return;
        }

        localizationRefreshTimer -= Time.unscaledDeltaTime;
        if (localizationRefreshTimer > 0f)
        {
            return;
        }

        localizationRefreshTimer = 0.35f;
        Text[] texts = FindObjectsByType<Text>(FindObjectsInactive.Exclude);
        foreach (Text text in texts)
        {
            text.text = L(text.text);
        }
    }
}


