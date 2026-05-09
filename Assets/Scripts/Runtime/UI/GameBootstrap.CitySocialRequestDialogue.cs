using UnityEngine;

public partial class GameBootstrap
{
    private const string CitySocialTopicHighlightColorHex = "#FFD84A";
    private int citySocialDialogueVariantIndex;

    private readonly struct CitySocialDialogueLine
    {
        public readonly int SpeakerSide;
        public readonly string Text;

        public CitySocialDialogueLine(int speakerSide, string text)
        {
            SpeakerSide = speakerSide;
            Text = text;
        }
    }

    private readonly struct CitySocialDialogueVariant
    {
        public readonly CitySocialDialogueLine[] SuccessLines;
        public readonly CitySocialDialogueLine[] FailureLines;
        public readonly string SuccessResult;
        public readonly string FailureResult;

        public CitySocialDialogueVariant(
            CitySocialDialogueLine[] successLines,
            CitySocialDialogueLine[] failureLines,
            string successResult,
            string failureResult)
        {
            SuccessLines = successLines;
            FailureLines = failureLines;
            SuccessResult = successResult;
            FailureResult = failureResult;
        }
    }

    private static readonly CitySocialDialogueVariant[] CitySocialDialogueVariants =
    {
        new CitySocialDialogueVariant(
            new[]
            {
                new CitySocialDialogueLine(0, "Слушай. Что думаешь насчёт «{topic}»?"),
                new CitySocialDialogueLine(1, "Про «{topic}»? Неожиданно. Но тема нормальная. По крайней мере, не про погоду и не про то, кто кому должен."),
                new CitySocialDialogueLine(0, "Я решил начать с чего-то, что звучит почти осмысленно."),
                new CitySocialDialogueLine(1, "Получилось. У «{topic}» хотя бы есть вес. Можно за него зацепиться."),
                new CitySocialDialogueLine(0, "Отлично. Значит, это уже не просто разговор. Это маленькая попытка не быть мебелью.")
            },
            new[]
            {
                new CitySocialDialogueLine(0, "Слушай. Что думаешь насчёт «{topic}»?"),
                new CitySocialDialogueLine(1, "Про «{topic}»? Сейчас - ничего хорошего."),
                new CitySocialDialogueLine(0, "Так плохо?"),
                new CitySocialDialogueLine(1, "Не сама тема плохая. Просто ты принёс «{topic}» так, будто нашёл её под лавкой и не знал, куда деть."),
                new CitySocialDialogueLine(0, "Понял. В следующий раз хотя бы отряхну."),
                new CitySocialDialogueLine(1, "Вот с этого и начни.")
            },
            "Тема «{topic}» сработала. Разговор стал живее, знакомство — крепче. Симпатия выросла.",
            "Тема «{topic}» не зашла. Знакомство состоялось, но разговор оставил лёгкий осадок."),
        new CitySocialDialogueVariant(
            new[]
            {
                new CitySocialDialogueLine(0, "Можно странный вопрос? Что ты думаешь про «{topic}»?"),
                new CitySocialDialogueLine(1, "Странный можно. Город вообще держится на странных вопросах и дешёвых стульях."),
                new CitySocialDialogueLine(0, "То есть тема не провалилась в подвал?"),
                new CitySocialDialogueLine(1, "Нет. У «{topic}» есть шанс. Она хотя бы не делает вид, что всё нормально."),
                new CitySocialDialogueLine(0, "Тогда продолжим осторожно. Как люди, которые нашли общий край стола в темноте.")
            },
            new[]
            {
                new CitySocialDialogueLine(0, "Можно странный вопрос? Что ты думаешь про «{topic}»?"),
                new CitySocialDialogueLine(1, "Думаю, что вопрос действительно странный. И, возможно, он сам это понимает."),
                new CitySocialDialogueLine(0, "Я надеялся, что это прозвучит лучше."),
                new CitySocialDialogueLine(1, "Надежда - хорошая вещь. Но «{topic}» сейчас вошла без стука."),
                new CitySocialDialogueLine(0, "Ясно. В следующий раз постучу хотя бы я.")
            },
            "«{topic}» дала разговору точку опоры. Знакомство укрепилось, симпатия выросла.",
            "«{topic}» прозвучала не вовремя. Они познакомились, но разговор вышел шероховатым."),
        new CitySocialDialogueVariant(
            new[]
            {
                new CitySocialDialogueLine(0, "Я принёс тему. Называется «{topic}». Звучит как вещь, которая могла выжить в кармане."),
                new CitySocialDialogueLine(1, "Удивительно, но я бы послушал. В «{topic}» есть что-то упрямое."),
                new CitySocialDialogueLine(0, "Упрямое - это почти комплимент."),
                new CitySocialDialogueLine(1, "В этом городе да. Тут даже нормальный разговор сначала получает ушиб."),
                new CitySocialDialogueLine(0, "Тогда будем считать, что ушиб не смертельный.")
            },
            new[]
            {
                new CitySocialDialogueLine(0, "Я принёс тему. Называется «{topic}»."),
                new CitySocialDialogueLine(1, "Она выглядит уставшей ещё до начала разговора."),
                new CitySocialDialogueLine(0, "Может, ей просто нужен человек, который поверит в неё?"),
                new CitySocialDialogueLine(1, "Или тихий ящик, где она спокойно полежит до лучших времён."),
                new CitySocialDialogueLine(0, "Справедливо. Ящик тоже форма заботы.")
            },
            "«{topic}» оказалась достаточно живой темой. Разговор удержался на ногах, знакомство стало теплее.",
            "«{topic}» не удержала беседу. Они всё равно заметили друг друга, но без особого тепла."),
        new CitySocialDialogueVariant(
            new[]
            {
                new CitySocialDialogueLine(0, "Есть тема для гражданского обмена словами: «{topic}»."),
                new CitySocialDialogueLine(1, "Звучит официально. Почти как будто у разговора есть печать."),
                new CitySocialDialogueLine(0, "Печати нет. Есть только смущение и желание не молчать."),
                new CitySocialDialogueLine(1, "Этого достаточно. Про «{topic}» можно говорить без протокола."),
                new CitySocialDialogueLine(0, "Вот и хорошо. Протоколы всё равно первыми начинают скучать.")
            },
            new[]
            {
                new CitySocialDialogueLine(0, "Есть тема для гражданского обмена словами: «{topic}»."),
                new CitySocialDialogueLine(1, "Слишком торжественно для темы, которая пока стоит у двери и мнётся."),
                new CitySocialDialogueLine(0, "Я хотел придать ей достоинства."),
                new CitySocialDialogueLine(1, "Достоинство не приклеивается словами. Особенно к «{topic}»."),
                new CitySocialDialogueLine(0, "Запомню. Меньше церемоний, больше смысла.")
            },
            "«{topic}» выдержала гражданский обмен словами. Разговор стал спокойнее, знакомство заметно укрепилось.",
            "«{topic}» не выдержала торжественного входа. Знакомство появилось, но симпатия просела."),
        new CitySocialDialogueVariant(
            new[]
            {
                new CitySocialDialogueLine(0, "Я тут подумал про «{topic}». Потом испугался, что подумал один."),
                new CitySocialDialogueLine(1, "Одинокая мысль - опасная штука. Её сразу тянет в философию."),
                new CitySocialDialogueLine(0, "Вот поэтому я и пришёл."),
                new CitySocialDialogueLine(1, "Хорошо сделал. Про «{topic}» можно думать вдвоём. Так меньше шансов провалиться в пафос."),
                new CitySocialDialogueLine(0, "Отлично. Значит, мы спасаем город от лишнего пафоса.")
            },
            new[]
            {
                new CitySocialDialogueLine(0, "Я тут подумал про «{topic}». Потом испугался, что подумал один."),
                new CitySocialDialogueLine(1, "Иногда мысль лучше оставить одну. Пусть успокоится."),
                new CitySocialDialogueLine(0, "То есть я принёс её слишком рано?"),
                new CitySocialDialogueLine(1, "Скорее без поводка. «{topic}» сейчас бегает по разговору и сбивает мебель."),
                new CitySocialDialogueLine(0, "Понял. В следующий раз сначала приручу.")
            },
            "«{topic}» помогла им подумать вдвоём. Знакомство стало плотнее, симпатия выросла.",
            "«{topic}» ворвалась слишком рано. Разговор выжил, но симпатия получила синяк."),
        new CitySocialDialogueVariant(
            new[]
            {
                new CitySocialDialogueLine(0, "Скажи честно: «{topic}» - это тема или просто слово с хорошей осанкой?"),
                new CitySocialDialogueLine(1, "С хорошей осанкой. Но это уже больше, чем у половины городских разговоров."),
                new CitySocialDialogueLine(0, "Значит, можно продолжать?"),
                new CitySocialDialogueLine(1, "Можно. У «{topic}» есть походка. Посмотрим, куда она нас заведёт."),
                new CitySocialDialogueLine(0, "Надеюсь, не в бухгалтерию души.")
            },
            new[]
            {
                new CitySocialDialogueLine(0, "Скажи честно: «{topic}» - это тема или просто слово с хорошей осанкой?"),
                new CitySocialDialogueLine(1, "Сейчас похоже на слово, которое переоценило свои возможности."),
                new CitySocialDialogueLine(0, "Жестоко, но ясно."),
                new CitySocialDialogueLine(1, "Не жестоко. Диагностически."),
                new CitySocialDialogueLine(0, "Тогда запишем: пациент разговорный, состояние шаткое.")
            },
            "«{topic}» прошла проверку на осанку. Разговор стал легче, знакомство усилилось.",
            "«{topic}» споткнулась на входе. Знакомство осталось, но разговору понадобилась пауза."),
        new CitySocialDialogueVariant(
            new[]
            {
                new CitySocialDialogueLine(0, "Мне сказали, что людям иногда помогает общая тема. Я выбрал «{topic}»."),
                new CitySocialDialogueLine(1, "Кто сказал?"),
                new CitySocialDialogueLine(0, "Внутренний отдел осторожного оптимизма."),
                new CitySocialDialogueLine(1, "Передай отделу, что на этот раз он не ошибся. Про «{topic}» можно поговорить."),
                new CitySocialDialogueLine(0, "Передам. Он будет невыносимо доволен.")
            },
            new[]
            {
                new CitySocialDialogueLine(0, "Мне сказали, что людям иногда помогает общая тема. Я выбрал «{topic}»."),
                new CitySocialDialogueLine(1, "Иногда помогает. Но «{topic}» сейчас больше похожа на проверку терпения."),
                new CitySocialDialogueLine(0, "Плохой выбор?"),
                new CitySocialDialogueLine(1, "Не смертельный. Просто тему принесли без ручки, и теперь непонятно, за что её держать."),
                new CitySocialDialogueLine(0, "Ладно. В следующий раз приделаю ручку.")
            },
            "«{topic}» оправдала осторожный оптимизм. Разговор пошёл мягче, симпатия выросла.",
            "«{topic}» не помогла осторожному оптимизму. Знакомство появилось, но доверия стало меньше."),
        new CitySocialDialogueVariant(
            new[]
            {
                new CitySocialDialogueLine(0, "Я хотел спросить про «{topic}», но боялся, что это прозвучит как начало собрания."),
                new CitySocialDialogueLine(1, "Не переживай. Собрание обычно пахнет хуже."),
                new CitySocialDialogueLine(0, "Это успокаивает странным образом."),
                new CitySocialDialogueLine(1, "А тема нормальная. В «{topic}» есть место для мнения, и оно не просит отдельный кабинет."),
                new CitySocialDialogueLine(0, "Тогда мнение пусть садится рядом.")
            },
            new[]
            {
                new CitySocialDialogueLine(0, "Я хотел спросить про «{topic}», но боялся, что это прозвучит как начало собрания."),
                new CitySocialDialogueLine(1, "Звучит не как собрание. Скорее как протокол без повода."),
                new CitySocialDialogueLine(0, "Это хуже?"),
                new CitySocialDialogueLine(1, "Это скучнее. А скука в начале знакомства - плохой председатель."),
                new CitySocialDialogueLine(0, "Понял. Разгоняю председателя.")
            },
            "«{topic}» не превратилась в собрание. Беседа ожила, знакомство стало ближе.",
            "«{topic}» принесла в разговор слишком много протокола. Они познакомились, но без искры."),
        new CitySocialDialogueVariant(
            new[]
            {
                new CitySocialDialogueLine(0, "Я не мастер начинать разговоры, поэтому начну с «{topic}»."),
                new CitySocialDialogueLine(1, "Честное предупреждение. Уже лучше, чем бодрая ложь."),
                new CitySocialDialogueLine(0, "Значит, тема проходит?"),
                new CitySocialDialogueLine(1, "Проходит. У «{topic}» есть честный вид. Немного помятый, но честный."),
                new CitySocialDialogueLine(0, "Помятый вид в этом городе почти форма прописки.")
            },
            new[]
            {
                new CitySocialDialogueLine(0, "Я не мастер начинать разговоры, поэтому начну с «{topic}»."),
                new CitySocialDialogueLine(1, "Спасибо за предупреждение. Оно оказалось точнее самой темы."),
                new CitySocialDialogueLine(0, "Звучит как поражение."),
                new CitySocialDialogueLine(1, "Скорее как учебное падение. Без тяжёлых последствий, но с выводами."),
                new CitySocialDialogueLine(0, "Вывод: тема должна приходить не первой, а подготовленной.")
            },
            "«{topic}» помогла начать честный разговор. Знакомство укрепилось, симпатия выросла.",
            "«{topic}» стала учебным падением. Знакомство появилось, но симпатия слегка снизилась."),
        new CitySocialDialogueVariant(
            new[]
            {
                new CitySocialDialogueLine(0, "У меня есть тема, и она пока не сбежала: «{topic}»."),
                new CitySocialDialogueLine(1, "Хороший знак. Беглые темы редко возвращаются с пользой."),
                new CitySocialDialogueLine(0, "Тогда что думаешь?"),
                new CitySocialDialogueLine(1, "Думаю, «{topic}» можно обсудить. Без фанфар, но с человеческим выражением лица."),
                new CitySocialDialogueLine(0, "Без фанфар даже лучше. Они всё равно всегда громче смысла.")
            },
            new[]
            {
                new CitySocialDialogueLine(0, "У меня есть тема, и она пока не сбежала: «{topic}»."),
                new CitySocialDialogueLine(1, "Она не сбежала, потому что не поняла, где выход."),
                new CitySocialDialogueLine(0, "Так заметно?"),
                new CitySocialDialogueLine(1, "Немного. «{topic}» сейчас стоит посреди разговора и ищет взрослого."),
                new CitySocialDialogueLine(0, "Ладно. Я тоже.")
            },
            "«{topic}» осталась в разговоре и принесла пользу. Знакомство стало крепче, симпатия выросла.",
            "«{topic}» заблудилась прямо в беседе. Знакомство случилось, но разговор оставил неловкость.")
    };

    private void SelectCitySocialDialogueVariant()
    {
        citySocialDialogueVariantIndex = CitySocialDialogueVariants.Length > 0
            ? Random.Range(0, CitySocialDialogueVariants.Length)
            : 0;
    }

    private CitySocialDialogueVariant GetCitySocialDialogueVariant()
    {
        if (CitySocialDialogueVariants.Length == 0)
        {
            return default;
        }

        int safeIndex = Mathf.Clamp(citySocialDialogueVariantIndex, 0, CitySocialDialogueVariants.Length - 1);
        return CitySocialDialogueVariants[safeIndex];
    }

    private CitySocialDialogueLine[] GetCitySocialActiveDialogueLines()
    {
        CitySocialDialogueVariant variant = GetCitySocialDialogueVariant();
        return citySocialConversationOutcome == CitySocialConversationOutcome.Success
            ? variant.SuccessLines
            : variant.FailureLines;
    }

    private int GetCitySocialDialogueLineCount()
    {
        return GetCitySocialActiveDialogueLines()?.Length ?? 0;
    }

    private void ShowCitySocialDialogueLine()
    {
        CitySocialDialogueLine[] lines = GetCitySocialActiveDialogueLines();
        if (lines == null || lines.Length == 0)
        {
            ShowCitySocialConversationResult();
            return;
        }

        CitySocialIntroductionRequest request = activeCitySocialIntroductionRequest;
        string requester = request?.RequesterName ?? "Житель";
        string target = request?.TargetName ?? "Житель";
        string topic = string.IsNullOrWhiteSpace(citySocialRequestTopic)
            ? SanitizeCitySocialTopic(string.Empty)
            : citySocialRequestTopic;
        int safeIndex = Mathf.Clamp(citySocialRequestDialogueIndex, 0, lines.Length - 1);
        CitySocialDialogueLine line = lines[safeIndex];

        citySocialSpeakingSide = Mathf.Clamp(line.SpeakerSide, 0, 1);
        citySocialRequestSceneHud.TitleText.text = citySocialSpeakingSide == 0 ? requester : target;
        SetCitySocialBodyText(FormatCitySocialDialogueText(line.Text, requester, target, topic));
        citySocialRequestSceneHud.ActionButtonText.text = safeIndex >= lines.Length - 1 ? "К итогу" : "Дальше";
    }

    private string BuildCitySocialConversationResultText(
        string requester,
        string target,
        string topic,
        int familiarityDelta,
        string relationshipDelta,
        bool success)
    {
        CitySocialDialogueVariant variant = GetCitySocialDialogueVariant();
        string summary = success ? variant.SuccessResult : variant.FailureResult;
        if (string.IsNullOrWhiteSpace(summary))
        {
            summary = success
                ? "Разговор сработал. Знакомство стало крепче."
                : "Разговор не сложился. Но ты попытался.";
        }

        string relationshipLine = $"Отношения: знакомство +{familiarityDelta}, симпатия {relationshipDelta}.";
        if (!success)
        {
            relationshipLine += " Но ты попытался.";
        }

        return $"{FormatCitySocialDialogueText(summary, requester, target, topic)}\n{relationshipLine}";
    }

    private static string FormatCitySocialDialogueText(string template, string requester, string target, string topic)
    {
        return (template ?? string.Empty)
            .Replace("{requester}", requester ?? "Житель")
            .Replace("{target}", target ?? "Житель")
            .Replace("{topic}", FormatCitySocialTopicRichText(topic));
    }

    private static string FormatCitySocialTopicRichText(string topic)
    {
        return $"<color={CitySocialTopicHighlightColorHex}><b>{SanitizeRichTextLiteral(topic)}</b></color>";
    }

    private static string SanitizeRichTextLiteral(string text)
    {
        return string.IsNullOrEmpty(text)
            ? string.Empty
            : text.Replace('<', '‹').Replace('>', '›');
    }
}
